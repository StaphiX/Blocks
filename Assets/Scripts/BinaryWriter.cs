using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public struct SceneObjectData
{
    public const UInt16 version = 1000;

    public UInt16 id;
    public Vector3 position;
}

public interface IWriter
{
    void WriteObject(SceneObjectData obj);
    void WriteObjectArray(SceneObjectData[] sceneObjs);
    void WriteObjectArrayToFile(SceneObjectData[] sceneObjs, string path);
}

public static class SpanWriter
{
    internal static void WriteUInt16(ref Span<byte> span, UInt16 uint16, ref int bytesWritten)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(span, uint16);
        span = span.Slice(sizeof(UInt16));
        bytesWritten += sizeof(UInt16);
    }

    internal static void WriteUInt32(ref Span<byte> span, UInt32 uint32, ref int bytesWritten)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(span, uint32);
        span = span.Slice(sizeof(UInt32));
        bytesWritten += sizeof(UInt32);
    }

    internal static void WriteInt32(ref Span<byte> span, Int32 int32, ref int bytesWritten)
    {
        BinaryPrimitives.WriteInt32LittleEndian(span, int32);
        span = span.Slice(sizeof(Int32));
        bytesWritten += sizeof(Int32);
    }

    internal static void WriteFloat(ref Span<byte> span, float floatVal, ref int bytesWritten)
    {
        Int32 floatAsInt = BitConverter.SingleToInt32Bits(floatVal);
        WriteInt32(ref span, floatAsInt, ref bytesWritten);
    }

    internal static void WriteVector3(ref Span<byte> span, Vector3 vec, ref int bytesWritten)
    {
        WriteFloat(ref span, vec.x, ref bytesWritten);
        WriteFloat(ref span, vec.y, ref bytesWritten);
        WriteFloat(ref span, vec.z, ref bytesWritten);
    }
}

public class BinaryFileWriter : IWriter
{
    byte[] buffer;
    int bufferLength = 0;
    int bytesWritten = 0;

    public void WriteObjectArrayToFile(SceneObjectData[] sceneObjs, string path)
    {
        int objArrayLen = sceneObjs.Length;
        if (objArrayLen < 1)
            return;

        FileStream fs = File.Open(path, FileMode.Create);
        CreateBuffer(sceneObjs);

        WriteObjectArray(sceneObjs);

        fs.Write(buffer);
    }

    public void WriteObject(SceneObjectData obj)
    {
        Span<byte> span = buffer.AsSpan(bytesWritten, bufferLength - bytesWritten);
        WriteObject(obj, ref span);
    }

    public void WriteObject(SceneObjectData obj, ref Span<byte> span)
    {
        SpanWriter.WriteUInt16(ref span, SceneObjectData.version, ref bytesWritten);
        SpanWriter.WriteUInt16(ref span, obj.id, ref bytesWritten);
        SpanWriter.WriteVector3(ref span, obj.position, ref bytesWritten);
    }

    public void WriteObjectArray(SceneObjectData[] sceneObjs)
    {
        int objArrayLen = sceneObjs.Length;
        Span<byte> span = buffer.AsSpan(bytesWritten, bufferLength - bytesWritten);
        SpanWriter.WriteInt32(ref span, objArrayLen, ref bytesWritten);
        for (int objIdx = 0; objIdx < sceneObjs.Length; ++objIdx)
        {
            SceneObjectData obj = sceneObjs[objIdx];
            WriteObject(obj, ref span);
        }
    }

    private void CreateBuffer(SceneObjectData[] sceneObjs)
    {
        int objSize = Marshal.SizeOf(typeof(SceneObjectData));
        bufferLength = objSize * sceneObjs.Length;
        buffer = new byte[bufferLength];
    }
}
