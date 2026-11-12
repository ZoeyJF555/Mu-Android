using System.Numerics;
using System.Text;
using Mu.Core.IO;

namespace Mu.Core.Formats.Bmd;

/// <summary>Lector del formato BMD (modelos y animaciones del cliente).</summary>
public static class BmdReader
{
    private const int HeaderLength = 4;
    private const int NameLength = 32;

    /// <summary>Lee un BMD desde disco.</summary>
    public static BmdModel ReadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Read(File.ReadAllBytes(path), Path.GetFileName(path));
    }

    /// <summary>Lee un BMD desde memoria.</summary>
    public static BmdModel Read(ReadOnlyMemory<byte> raw, string? sourceName = null)
    {
        var buffer = raw.ToArray();
        if (buffer.Length < HeaderLength)
        {
            throw new InvalidDataException($"'{sourceName ?? "BMD"}': el fichero está vacío o truncado.");
        }

        if (Encoding.ASCII.GetString(buffer, 0, 3) != "BMD")
        {
            throw new InvalidDataException(
                $"'{sourceName ?? "BMD"}': cabecera incorrecta, se esperaba \"BMD\".");
        }

        var version = buffer[3];
        if (BmdPayloadDecoder.IsEncrypted(version))
        {
            DecryptBody(buffer, version, sourceName);
        }

        using var reader = new BinaryReader(new MemoryStream(buffer, writable: false));
        reader.BaseStream.Position = HeaderLength;

        var name = reader.ReadFixedString(NameLength);
        var meshCount = reader.ReadUInt16();
        var boneCount = reader.ReadUInt16();
        var actionCount = reader.ReadUInt16();

        var meshes = ReadMeshes(reader, meshCount);
        var actions = ReadActions(reader, actionCount);
        var bones = ReadBones(reader, boneCount, actions);

        return new BmdModel
        {
            Version = version,
            Name = string.IsNullOrWhiteSpace(name) ? sourceName ?? string.Empty : name,
            Meshes = meshes,
            Actions = actions,
            Bones = bones,
        };
    }

    private static void DecryptBody(byte[] buffer, byte version, string? sourceName)
    {
        if (buffer.Length < 8)
        {
            throw new InvalidDataException($"'{sourceName ?? "BMD"}': falta la longitud del cuerpo.");
        }

        var bodyLength = BitConverter.ToInt32(buffer, 4);
        if (bodyLength < 0 || 8 + bodyLength > buffer.Length)
        {
            throw new InvalidDataException(
                $"'{sourceName ?? "BMD"}': longitud de cuerpo inválida ({bodyLength}).");
        }

        var body = buffer.AsSpan(8, bodyLength);
        BmdPayloadDecoder.Decode(body, version);
        body.CopyTo(buffer.AsSpan(HeaderLength, bodyLength));
    }

    private static BmdMesh[] ReadMeshes(BinaryReader reader, int count)
    {
        var meshes = new BmdMesh[count];

        for (var i = 0; i < meshes.Length; i++)
        {
            var vertexCount = reader.ReadInt16();
            var normalsCount = reader.ReadInt16();
            var texCoordsCount = reader.ReadInt16();
            var triangleCount = reader.ReadInt16();
            var textureId = reader.ReadInt16();

            if (vertexCount < 0 || normalsCount < 0 || texCoordsCount < 0 || triangleCount < 0)
            {
                throw new InvalidDataException($"Malla {i}: contadores negativos, fichero corrupto.");
            }

            var vertices = reader.ReadStructArray<BmdVertexRecord>(vertexCount);
            var normals = reader.ReadStructArray<BmdNormalRecord>(normalsCount);
            var texCoords = reader.ReadStructArray<BmdTexCoordRecord>(texCoordsCount);
            var triangles = reader.ReadStructArray<BmdTriangleRecord>(triangleCount);
            var texturePath = reader.ReadFixedString(NameLength);

            meshes[i] = new BmdMesh
            {
                TextureId = textureId,
                TexturePath = texturePath,
                Vertices = Array.ConvertAll(vertices, v => new BmdVertex(v.Node, v.Position)),
                Normals = Array.ConvertAll(normals, n => new BmdNormal(n.Node, n.Normal, n.BindVertex)),
                TexCoords = Array.ConvertAll(texCoords, t => new Vector2(t.U, t.V)),
                Triangles = Array.ConvertAll(triangles, ToTriangle),
            };
        }

        return meshes;
    }

    private static BmdTriangle ToTriangle(BmdTriangleRecord record) => new()
    {
        Polygon = record.Polygon,
        VertexIndices = record.VertexIndex ?? [],
        NormalIndices = record.NormalIndex ?? [],
        TexCoordIndices = record.TexCoordIndex ?? [],
        LightMapIndex = record.LightMapIndex,
    };

    private static BmdAction[] ReadActions(BinaryReader reader, int count)
    {
        var actions = new BmdAction[count];

        for (var i = 0; i < actions.Length; i++)
        {
            var animationKeys = reader.ReadInt16();
            var lockPositions = reader.ReadBoolean();

            if (animationKeys < 0)
            {
                throw new InvalidDataException($"Acción {i}: número de fotogramas negativo.");
            }

            actions[i] = new BmdAction
            {
                AnimationKeys = animationKeys,
                LockPositions = lockPositions,
                Positions = lockPositions
                    ? reader.ReadStructArray<Vector3>(animationKeys)
                    : [],
            };
        }

        return actions;
    }

    private static BmdBone[] ReadBones(BinaryReader reader, int count, BmdAction[] actions)
    {
        var bones = new BmdBone[count];

        for (var i = 0; i < bones.Length; i++)
        {
            if (reader.ReadBoolean())
            {
                bones[i] = new BmdBone
                {
                    Name = "Dummy",
                    Parent = 0,
                    IsDummy = true,
                    Tracks = [],
                };
                continue;
            }

            var name = reader.ReadFixedString(NameLength);
            var parent = reader.ReadInt16();
            var tracks = new BmdBoneTrack[actions.Length];

            for (var a = 0; a < actions.Length; a++)
            {
                var keys = actions[a].AnimationKeys;
                tracks[a] = new BmdBoneTrack
                {
                    Positions = reader.ReadStructArray<Vector3>(keys),
                    Rotations = reader.ReadStructArray<Vector3>(keys),
                };
            }

            bones[i] = new BmdBone
            {
                Name = name,
                Parent = parent,
                IsDummy = false,
                Tracks = tracks,
            };
        }

        return bones;
    }
}
