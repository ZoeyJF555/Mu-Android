namespace Mu.Core.Assets;

/// <summary>
/// De dónde salen los ficheros de datos del cliente. En escritorio son ficheros
/// normales; en Android van dentro del APK, así que el juego lee a través de
/// esta interfaz y no le importa de dónde vengan.
/// </summary>
public interface IAssetSource
{
    /// <summary>Lee un recurso completo, o devuelve null si no existe.</summary>
    byte[]? Read(string relativePath);

    /// <summary>Comprueba si un recurso existe.</summary>
    bool Exists(string relativePath);

    /// <summary>Lista los recursos que empiezan por un prefijo (para buscar piezas).</summary>
    IEnumerable<string> Enumerate(string prefix);
}
