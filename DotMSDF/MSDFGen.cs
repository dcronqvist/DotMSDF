using System;
using System.IO;
using FreeTypeSharp;

namespace DotMSDF;

public record MSDFGenFreeTypeHandle(FreeTypeLibrary Library);
public record MSDFGenFontHandle(FreeTypeFaceFacade Face);

public static class MSDFGen
{
    public static MSDFGenFreeTypeHandle InitializeFreeType()
    {
        return new MSDFGenFreeTypeHandle(new FreeTypeLibrary());
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