namespace Mu.Client.Rendering;

/// <summary>Un dedo apoyado en la pantalla, ya traducido a coordenadas del lienzo.</summary>
/// <param name="Id">Número que da el sistema a cada dedo mientras está apoyado.</param>
/// <param name="X">Coordenada X en el lienzo.</param>
/// <param name="Y">Coordenada Y en el lienzo.</param>
public readonly record struct TouchPoint(int Id, float X, float Y);
