using System;
using System.Text;

namespace ResonanceTools.Utility;

public class GMFEncoder
{
    /// <summary>
    /// Reimplementation of the GMF decoder
    /// </summary>
    
    public const int TYPE_END = 0;
    public const int TYPE_BOOLEAN = 1;
    public const int TYPE_INT = 2;
    public const int TYPE_UINT = 3;
    public const int TYPE_DOUBLE = 4;
    public const int TYPE_STRING = 5;
    public const int TYPE_ARRAY = 6;
    public const int TYPE_OBJECT = 7;
    public const int TYPE_NULL = 8;
    public const int TYPE_BYTE_ARRAY = 10;
    public const int TYPE_VECTOR_OBJECT = 11;
    public const int TYPE_VECTOR_INT = 12;
    public const int TYPE_FLOAT = 101;
    public const int TYPE_VECTOR2 = 202;
    public const int TYPE_VECTOR3 = 203;
    public const int TYPE_QUATERNION = 204;

    public static object DecodeFromStream(BinaryReader reader)
    {
        byte flag = reader.ReadByte();

        switch (flag)
        {
            case TYPE_END: return 0;
            case TYPE_BOOLEAN: return reader.ReadBoolean();
            case TYPE_INT: return reader.ReadInt32();
            case TYPE_UINT: return reader.ReadUInt32();
            case TYPE_DOUBLE: return reader.ReadDouble();
            case TYPE_STRING: return ReadGMFString(reader);
            case TYPE_ARRAY: return ReadArray(reader);
            case TYPE_OBJECT: return ReadObject(reader);
            case TYPE_NULL: return 0;
            case 9:
                throw new NotSupportedException("Unsupported flag 9 encountered.");
            case TYPE_BYTE_ARRAY: return ReadByteArray(reader);
            case TYPE_VECTOR_OBJECT: return ReadVectorObject(reader);
            case TYPE_VECTOR_INT: return ReadVectorInt(reader);
            case TYPE_FLOAT: return reader.ReadSingle();
            case TYPE_VECTOR2:
                throw new NotImplementedException($"Flag {flag} is not implemented.");
            case TYPE_VECTOR3:
                throw new NotImplementedException($"Flag {flag} is not implemented.");
            case TYPE_QUATERNION:
                throw new NotImplementedException($"Flag {flag} is not implemented.");
            default:
                throw new NotSupportedException($"Unknown type flag: {flag}");
        }
    }

    private static string ReadGMFString(BinaryReader reader)
    {
        // Decompiled logic:
        // ushort len = ReadUInt16();
        // if len == 0 -> empty
        // if len == 0xFFFF then read Int32 realLen
        // then read that many raw bytes (UTF8) WITHOUT extra prefix (unlike BinaryReader.ReadString)
        ushort len = reader.ReadUInt16();
        if (len == 0) return string.Empty;
        int realLen = len == 0xFFFF ? reader.ReadInt32() : len;
        if (realLen < 0) throw new InvalidDataException($"Negative string length {realLen}");
        byte[] data = reader.ReadBytes(realLen);
        if (data.Length != realLen) throw new EndOfStreamException("Unexpected EOF while reading GMF string");
        return Encoding.UTF8.GetString(data);
    }

    private static AS3Array ReadArray(BinaryReader reader)
    {
        var array = new AS3Array();
        int sizeInBytes = reader.ReadInt32();
        long endPos = reader.BaseStream.Position + sizeInBytes;

        while (reader.BaseStream.Position < endPos)
        {
            var element = DecodeFromStream(reader);
            array.Push(element);
        }

        return array;
    }

    private static AS3DynamicObject ReadObject(BinaryReader reader)
    {
        var obj = new AS3DynamicObject();

        int sizeInBytes = reader.ReadInt32();
        long endPos = reader.BaseStream.Position + sizeInBytes;

        while (reader.BaseStream.Position < endPos)
        {
            ushort keyLen = reader.ReadUInt16();
            string key = keyLen > 0
                ? Encoding.UTF8.GetString(reader.ReadBytes(keyLen))
                : string.Empty;

            object value = DecodeFromStream(reader);
            obj.Set(key, value);
        }

        return obj;
    }

    private static ByteArray ReadByteArray(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        if (length > 0)
        {
            byte[] data = reader.ReadBytes(length);
            return new ByteArray(data, "bigEndian");
        }

        return new ByteArray();
    }

    private static Seq<object> ReadVectorObject(BinaryReader reader)
    {
        reader.ReadInt32(); // element type, ignored
        int count = reader.ReadInt32();
        var seq = new Seq<object>(count, fixedLength: true);

        for (int i = 0; i < count; i++)
            seq[i] = DecodeFromStream(reader);

        return seq;
    }

    private static Seq<int> ReadVectorInt(BinaryReader reader)
    {
        int byteLength = reader.ReadInt32();
        int count = byteLength / 4;
        var seq = new Seq<int>(count, fixedLength: true);

        for (int i = 0; i < count; i++)
            seq[i] = reader.ReadInt32();

        return seq;
    }
}
