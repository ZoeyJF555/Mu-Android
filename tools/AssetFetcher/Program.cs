using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mu.Tools.AssetFetcher;

/// <summary>Un grupo de recursos del índice del cliente web.</summary>
public sealed class AssetGroup
{
    [JsonPropertyName("id")] public required string Id { get; init; }
    [JsonPropertyName("kind")] public required string Kind { get; init; }
    [JsonPropertyName("name")] public string? Name { get; init; }
    [JsonPropertyName("files")] public int Files { get; init; }
    [JsonPropertyName("dirs")] public required List<JsonElement> Dirs { get; init; }
}

/// <summary>El índice completo que publica el cliente web.</summary>
public sealed class AssetIndex
{
    [JsonPropertyName("built")] public string? Built { get; init; }
    [JsonPropertyName("sidecars")] public bool Sidecars { get; init; }
    [JsonPropertyName("groups")] public required List<AssetGroup> Groups { get; init; }
}

/// <summary>
/// Descarga los recursos del cliente web de OpenMU al árbol de datos local.
/// Los ficheros se piden por su ruta relativa (por ejemplo
/// <c>game-assets/World1/TerrainHeight.OZB</c>) y se guardan con la misma
/// estructura, de modo que <c>AssetStore</c> los encuentra sin cambios.
/// </summary>
public static class Program
{
    private const string DefaultBase = "https://play.openmu.com.br/";

    /// <summary>Extensiones que entiende este cliente (BMD y compañía).</summary>
    private static readonly HashSet<string> NativeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "bmd", "ozj", "ozt", "ozb", "att", "map", "obj",
    };

    private const string UserAgent =
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120 Safari/537.36";

    public static async Task<int> Main(string[] args)
    {
        var options = Options.Parse(args);
        if (options.ShowHelp)
        {
            Options.PrintHelp();
            return 0;
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);

        Console.WriteLine($"Índice: {options.BaseUrl}assets-index.json");
        var index = await http.GetFromJsonAsync<AssetIndex>($"{options.BaseUrl}assets-index.json")
            ?? throw new InvalidOperationException("El índice de recursos está vacío.");

        var groups = index.Groups
            .Where(g => options.Groups.Count == 0 || options.Groups.Contains(g.Id))
            .ToList();

        if (options.ListOnly)
        {
            foreach (var group in index.Groups)
            {
                Console.WriteLine($"{group.Id,-12} {group.Kind,-9} {group.Files,6} ficheros  {group.Name}");
            }

            return 0;
        }

        var wanted = Select(groups, options).ToList();
        Console.WriteLine($"{wanted.Count} ficheros a descargar ({options.Groups.Count switch { 0 => "todos los grupos", _ => string.Join(',', options.Groups) }})");

        var downloaded = 0;
        var skipped = 0;
        var failed = 0;
        var gate = new SemaphoreSlim(options.Concurrency);

        var tasks = wanted.Select(async file =>
        {
            await gate.WaitAsync();
            try
            {
                var target = Path.Combine(options.Output, file.LocalPath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(target) && new FileInfo(target).Length == file.Bytes)
                {
                    Interlocked.Increment(ref skipped);
                    return;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                await Download(http, $"{options.BaseUrl}{file.RemotePath}", target);
                var done = Interlocked.Increment(ref downloaded);
                if (done % 100 == 0)
                {
                    Console.WriteLine($"  {done}/{wanted.Count}...");
                }
            }
            catch (Exception error)
            {
                Interlocked.Increment(ref failed);
                Console.Error.WriteLine($"  FALLO {file.RemotePath}: {error.Message}");
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);

        Console.WriteLine($"Listo: {downloaded} descargados, {skipped} ya estaban, {failed} fallos.");
        return failed == 0 ? 0 : 1;
    }

    private static IEnumerable<AssetFile> Select(IEnumerable<AssetGroup> groups, Options options)
    {
        foreach (var group in groups)
        {
            foreach (var entry in group.Dirs)
            {
                if (entry.ValueKind != JsonValueKind.Array || entry.GetArrayLength() < 2)
                {
                    continue;
                }

                var remote = (entry[0].GetString() ?? string.Empty).TrimStart('.', '/');
                var local = StripPrefix(remote);

                foreach (var file in FilesOf(entry[1]))
                {
                    if (options.NativeOnly && !NativeExtensions.Contains(file.Extension))
                    {
                        continue;
                    }

                    yield return new AssetFile(local + file.Name, remote + file.Name, file.Bytes);
                }
            }
        }
    }

    /// <summary>
    /// El índice publica dos árboles (<c>Data/</c> y <c>game-assets/</c>) con
    /// los mismos nombres de carpeta dentro. Este cliente lee un solo árbol, así
    /// que se quita el prefijo para que ambos se junten en el mismo sitio.
    /// </summary>
    private static string StripPrefix(string directory)
    {
        foreach (var prefix in (string[])["Data/", "game-assets/"])
        {
            if (directory.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return directory[prefix.Length..];
            }
        }

        return directory;
    }

    /// <summary>El índice guarda cada carpeta como pares [nombre, bytes].</summary>
    private static IEnumerable<(string Name, string Extension, long Bytes)> FilesOf(JsonElement packed)
    {
        foreach (var item in packed.EnumerateArray())
        {
            var name = item[0].GetString() ?? string.Empty;
            var bytes = item[1].GetInt64();
            var dot = name.LastIndexOf('.');
            var extension = dot >= 0 ? name[(dot + 1)..] : string.Empty;
            yield return (name, extension, bytes);
        }
    }

    private static async Task Download(HttpClient http, string url, string target)
    {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var temporary = target + ".part";
        await using (var file = File.Create(temporary))
        {
            await response.Content.CopyToAsync(file);
        }

        File.Move(temporary, target, overwrite: true);
    }

    private readonly record struct AssetFile(string LocalPath, string RemotePath, long Bytes);

    private sealed class Options
    {
        public string BaseUrl { get; private set; } = DefaultBase;
        public string Output { get; private set; } = "Data";
        public HashSet<string> Groups { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool NativeOnly { get; private set; } = true;
        public bool ListOnly { get; private set; }
        public bool ShowHelp { get; private set; }
        public int Concurrency { get; private set; } = 6;

        public static Options Parse(string[] args)
        {
            var options = new Options();
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--list":
                        options.ListOnly = true;
                        break;
                    case "--all":
                        options.NativeOnly = false;
                        break;
                    case "--out" when i + 1 < args.Length:
                        options.Output = args[++i];
                        break;
                    case "--base" when i + 1 < args.Length:
                        options.BaseUrl = args[++i].TrimEnd('/') + "/";
                        break;
                    case "--only" when i + 1 < args.Length:
                        foreach (var id in args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries))
                        {
                            options.Groups.Add(id);
                        }

                        break;
                    case "--jobs" when i + 1 < args.Length:
                        options.Concurrency = int.Parse(args[++i]);
                        break;
                    case "-h" or "--help":
                        options.ShowHelp = true;
                        break;
                    default:
                        Console.Error.WriteLine($"Opción desconocida: {args[i]}");
                        options.ShowHelp = true;
                        break;
                }
            }

            return options;
        }

        public static void PrintHelp()
        {
            Console.WriteLine("""
                Descarga los recursos del cliente web de OpenMU.

                Uso: AssetFetcher [opciones]

                  --list            Muestra los grupos del índice y sale.
                  --only id[,id]    Solo estos grupos (core, monsters, icons, map:1...).
                  --out carpeta     Dónde guardar (por defecto Data).
                  --all             Descarga todo, no solo los formatos nativos.
                  --jobs n          Descargas en paralelo (por defecto 6).
                  --base url        Origen de los recursos.

                Por defecto solo se descargan los formatos que lee este cliente
                (BMD, OZJ, OZT, OZB, ATT, MAP, OBJ), que son los que reutiliza el
                cliente web; el resto (GLB, audio, iconos) se omite.
                """);
        }
    }
}
