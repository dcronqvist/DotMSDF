using System;
using System.IO;

namespace DotMSDF;

public record MSDFGenFreeTypeHandle(IntPtr Handle);
public record MSDFGenFontHandle(IntPtr Handle);

public static class MSDFGen
{
    public static MSDFGenFreeTypeHandle InitializeFreeType()
    {
        throw new NotImplementedException();
    }

    public static MSDFGenFontHandle LoadFont(
        MSDFGenFreeTypeHandle freeTypeHandle,
        string pathToFontFile)
    {
        using var binaryReader = new BinaryReader(File.OpenRead(pathToFontFile));
        var inMemoryTTFbytes = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);
        binaryReader.Dispose();
        return LoadFont(freeTypeHandle, inMemoryTTFbytes);
    }

    public static MSDFGenFontHandle LoadFont(
        MSDFGenFreeTypeHandle freeTypeHandle,
        byte[] inMemoryTTFbytes)
    {
        throw new NotImplementedException();
    }
}