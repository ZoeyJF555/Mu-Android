namespace Mu.Core.Assets;

/// <summary>
/// Recorre un árbol de recursos del que solo se sabe listar carpetas. Es lo que
/// hace falta dentro de un APK, donde no se puede preguntar al sistema si una
/// entrada es carpeta o fichero.
///
/// El detalle importante: listar un fichero no falla siempre. Android puede
/// devolver una lista vacía en vez de dar un error, y entonces un fichero se
/// confundiría con una carpeta. Por eso solo se considera carpeta lo que tiene
/// hijos; una carpeta vacía no aporta nada y se puede ignorar sin perder nada.
/// </summary>
public static class AssetTree
{
    /// <summary>
    /// Recoge los nombres de todos los recursos que hay bajo una carpeta.
    /// </summary>
    /// <param name="root">Carpeta por la que empezar, sin barra final.</param>
    /// <param name="list">
    /// Lista los hijos de una ruta, o devuelve null si no es una carpeta. Puede
    /// devolver una lista vacía si la carpeta no tiene nada o si la ruta es un
    /// fichero, según cómo se comporte la plataforma.
    /// </param>
    /// <returns>Nombres relativos a <paramref name="root"/>, sin normalizar.</returns>
    public static IReadOnlyList<string> Collect(string root, Func<string, string[]?> list)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentNullException.ThrowIfNull(list);

        var names = new List<string>();
        Walk(root, string.Empty, list, names);
        return names;
    }

    /// <summary>
    /// Cierto si una ruta es una carpeta, es decir, si tiene algún hijo. Un
    /// fichero nunca tiene hijos, aunque la plataforma responda con una lista
    /// vacía en lugar de con un error.
    /// </summary>
    public static bool IsFolder(string path, Func<string, string[]?> list)
    {
        ArgumentNullException.ThrowIfNull(list);
        var children = list(path);
        return children is not null && children.Length > 0;
    }

    private static void Walk(
        string path,
        string relative,
        Func<string, string[]?> list,
        List<string> names)
    {
        var children = list(path);
        if (children is null)
        {
            return;
        }

        foreach (var child in children)
        {
            var childPath = Combine(path, child);
            var childRelative = relative.Length == 0 ? child : $"{relative}/{child}";
            if (IsFolder(childPath, list))
            {
                Walk(childPath, childRelative, list, names);
            }
            else
            {
                names.Add(childRelative);
            }
        }
    }

    private static string Combine(string left, string right) =>
        left.Length == 0 ? right : $"{left}/{right}";
}
