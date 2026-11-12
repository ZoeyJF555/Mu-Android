using System.Runtime.InteropServices;
using System.Text;

namespace Mu.Core.IO;

/// <summary>
/// Lectura de los tipos binarios que usan los ficheros de datos del cliente.
/// Todas las lecturas comprueban límites para que un fichero corrupto no
/// provoque lecturas fuera de rango.
/// </summary>
public static class BinaryReaderExtensions
{
    private static readonly Encoding NameEncoding = Encoding.Latin1;

    /// <summary>
    /// Lee una cadena de longitud fija terminada en NUL (nombres de mallas,
    /// huesos y texturas).
    /// </summary>
    public static string ReadFixedString(this BinaryReader reader, int length)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var bytes = reader.ReadBytes(length);
        if (bytes.Length != length)
        {
            throw new EndOfStreamException(
                $"Se esperaban {length} bytes de texto y solo quedaban {bytes.Length}.");
        }

        var terminator = Array.IndexOf(bytes, (byte)0);
        return NameEncoding.GetString(terminator >= 0 ? bytes.AsSpan(0, terminator) : bytes).Trim();
    }

    /// <summary>Tamaño en bytes de un tipo tal y como se serializa en disco.</summary>
    public static int SizeOf<T>() where T : struct => Marshal.SizeOf<T>();

    /// <summary>Lee una estructura respetando el empaquetado de disco.</summary>
    public static T ReadStruct<T>(this BinaryReader reader) where T : struct
    {
        ArgumentNullException.ThrowIfNull(reader);
        var size = SizeOf<T>();
        var bytes = reader.ReadBytes(size);
        if (bytes.Length != size)
        {
            throw new EndOfStreamException(
                $"Se esperaban {size} bytes para {typeof(T).Name} y solo quedaban {bytes.Length}.");
        }

        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    /// <summary>Lee un array consecutivo de estructuras del fichero.</summary>
    public static T[] ReadStructArray<T>(this BinaryReader reader, int count) where T : struct
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var size = SizeOf<T>();
        var bytes = reader.ReadBytes(size * count);
        if (bytes.Length != size * count)
        {
            throw new EndOfStreamException(
                $"Se esperaban {size * count} bytes para {count} × {typeof(T).Name} " +
                $"y solo quedaban {bytes.Length}.");
        }

        var result = new T[count];
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            var address = handle.AddrOfPinnedObject();
            for (var i = 0; i < count; i++)
            {
                result[i] = Marshal.PtrToStructure<T>(address + (i * size))!;
            }
        }
        finally
        {
            handle.Free();
        }

        return result;
    }

    /// <summary>Comprueba que quedan al menos <paramref name="count"/> bytes por leer.</summary>
    public static void EnsureRemaining(this BinaryReader reader, long count, string context)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var remaining = reader.BaseStream.Length - reader.BaseStream.Position;
        if (remaining < count)
        {
            throw new InvalidDataException(
                $"{context}: se necesitan {count} bytes y solo quedan {remaining}.");
        }
    }
}
