namespace Mu.Core.Formats.Terrain;

/// <summary>Un objeto colocado sobre el mapa (un árbol, una casa, una estatua...).</summary>
public readonly record struct TerrainObject
{
    /// <summary>Tipo de objeto: dice qué modelo usar.</summary>
    public required short Type { get; init; }

    /// <summary>Posición en el mundo (en centímetros).</summary>
    public required System.Numerics.Vector3 Position { get; init; }

    /// <summary>Rotación en grados.</summary>
    public required System.Numerics.Vector3 Rotation { get; init; }

    /// <summary>Escala uniforme.</summary>
    public required float Scale { get; init; }
}

/// <summary>
/// Lee la lista de objetos de un mapa (`World{n}/EncTerrain{n}.obj`). El
/// fichero va cifrado y trae una cabecera de 4 bytes seguida de registros de
/// 30 bytes: tipo, posición, rotación y escala.
/// </summary>
public static class TerrainObjectReader
{
    private const int HeaderSize = 4;
    private const int RecordSize = 30;

    /// <summary>Lee los objetos de un fichero .obj (en memoria).</summary>
    public static TerrainObject[] Read(byte[] obj)
    {
        var plain = MapFileEncryption.Decrypt(obj);
        if (plain.Length < HeaderSize)
        {
            throw new InvalidDataException("El fichero .obj es demasiado pequeño.");
        }

        var count = BitConverter.ToInt16(plain, 2);
        if (count < 0 || HeaderSize + count * RecordSize > plain.Length)
        {
            throw new InvalidDataException("El fichero .obj declara más objetos de los que caben.");
        }

        var objects = new TerrainObject[count];
        for (var i = 0; i < count; i++)
        {
            var at = HeaderSize + i * RecordSize;
            objects[i] = new TerrainObject
            {
                Type = BitConverter.ToInt16(plain, at),
                Position = ReadVector(plain, at + 2),
                Rotation = ReadVector(plain, at + 14),
                Scale = BitConverter.ToSingle(plain, at + 26),
            };
        }

        return objects;
    }

    private static System.Numerics.Vector3 ReadVector(byte[] buffer, int offset) => new(
        BitConverter.ToSingle(buffer, offset),
        BitConverter.ToSingle(buffer, offset + 4),
        BitConverter.ToSingle(buffer, offset + 8));
}
