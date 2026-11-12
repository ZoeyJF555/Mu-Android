using System.Numerics;
using System.Runtime.InteropServices;

namespace Mu.Core.Formats.Bmd;

// Las estructuras de disco del formato BMD se leen con el mismo empaquetado
// que usa el cliente original (Pack = 4), de modo que los tamaños calculados
// coinciden byte a byte con el fichero.

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct BmdVertexRecord
{
    public short Node;
    public Vector3 Position;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct BmdNormalRecord
{
    public short Node;
    public Vector3 Normal;
    public short BindVertex;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct BmdTexCoordRecord
{
    public float U;
    public float V;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct BmdTriangleRecord
{
    public byte Polygon;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public short[] VertexIndex;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public short[] NormalIndex;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public short[] TexCoordIndex;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public BmdTexCoordRecord[] LightMapCoord;

    public short LightMapIndex;
}
