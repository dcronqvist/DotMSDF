using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using FreeTypeSharp;
using FreeTypeSharp.Native;

namespace DotMSDF;

public record MSDFGenFreeTypeHandle(FreeTypeLibrary Library);
public record MSDFGenFontHandle(FreeTypeFaceFacade Face, FreeTypeLibrary Library);

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

    public unsafe static MSDFGenFontHandle LoadFont(
        MSDFGenFreeTypeHandle freeTypeHandle,
        byte[] inMemoryTTFbytes)
    {
        fixed (byte* inMemoryTTFbytesPtr = &inMemoryTTFbytes[0])
        {
            FT_Error error = FT.FT_New_Memory_Face(freeTypeHandle.Library.Native, (nint)inMemoryTTFbytesPtr, inMemoryTTFbytes.Length, 0, out var face);
            if (error != FT_Error.FT_Err_Ok)
            {
                return null;
            }
            return new MSDFGenFontHandle(new FreeTypeFaceFacade(freeTypeHandle.Library, face), freeTypeHandle.Library);
        }
    }

    internal static bool LoadGlyph(ref Shape output, MSDFGenFontHandle fontHandle, uint codePoint, ref float advance) =>
        LoadGlyph(ref output, fontHandle, FT.FT_Get_Char_Index(fontHandle.Face.Face, codePoint), ref advance);

    internal unsafe static bool LoadGlyph(ref Shape output, MSDFGenFontHandle fontHandle, int glyphIndex, ref float advance)
    {
        FT_Error error = FT.FT_Load_Glyph(fontHandle.Face.Face, (uint)glyphIndex, FT.FT_LOAD_NO_SCALE);
        if (error != FT_Error.FT_Err_Ok)
        {
            return false;
        }

        advance = fontHandle.Face.GlyphMetricHorizontalAdvance;

        return ReadFreeTypeOutline(ref output, ref fontHandle.Face.GlyphSlot->outline) == FT_Error.FT_Err_Ok;
    }

    private class FTContext(Shape shape)
    {
        public Vector2 Position { get; set; } = Vector2.Zero;
        public Shape Shape { get; set; } = shape;
        public Contour Contour { get; set; } = null;

        public delegate int FT_Outline_MoveToFunc(IntPtr to, IntPtr user);
        public delegate int FT_Outline_LineToFunc(IntPtr to, IntPtr user);
        public delegate int FT_Outline_ConicToFunc(IntPtr control, IntPtr to, IntPtr user);
        public delegate int FT_Outline_CubicToFunc(IntPtr control1, IntPtr control2, IntPtr to, IntPtr user);

        private static Vector2 FTVectorToVector2(FT_Vector v) => new Vector2(v.x, v.y);

        public int FT_Outline_MoveToFuncImpl(IntPtr to, IntPtr user)
        {
            FT_Vector vto = Marshal.PtrToStructure<FT_Vector>(to);
            Contour = new Contour([]);
            Shape.AddContour(Contour);
            Position = FTVectorToVector2(vto);
            return 0;
        }

        public int FT_Outline_LineToFuncImpl(IntPtr to, IntPtr user)
        {
            FT_Vector vto = Marshal.PtrToStructure<FT_Vector>(to);
            Contour.AddEdge(new LinearSegment(Position, FTVectorToVector2(vto), EdgeColor.White));
            Position = FTVectorToVector2(vto);
            return 0;
        }

        public int FT_Outline_ConicToFuncImpl(IntPtr control, IntPtr to, IntPtr user)
        {
            FT_Vector vcontrol = Marshal.PtrToStructure<FT_Vector>(control);
            FT_Vector vto = Marshal.PtrToStructure<FT_Vector>(to);
            Contour.AddEdge(new QuadraticSegment(Position, FTVectorToVector2(vcontrol), FTVectorToVector2(vto), EdgeColor.White));
            Position = FTVectorToVector2(vto);
            return 0;
        }

        public int FT_Outline_CubicToFuncImpl(IntPtr control1, IntPtr control2, IntPtr to, IntPtr user)
        {
            FT_Vector vcontrol1 = Marshal.PtrToStructure<FT_Vector>(control1);
            FT_Vector vcontrol2 = Marshal.PtrToStructure<FT_Vector>(control2);
            FT_Vector vto = Marshal.PtrToStructure<FT_Vector>(to);
            Contour.AddEdge(new CubicSegment(Position, FTVectorToVector2(vcontrol1), FTVectorToVector2(vcontrol2), FTVectorToVector2(vto), EdgeColor.White));
            Position = FTVectorToVector2(vto);
            return 0;
        }
    }

    internal unsafe static FT_Error ReadFreeTypeOutline(ref Shape output, ref FT_Outline outline)
    {
        output.ClearContours();
        output.InverseAxis = false;
        FTContext ftContext = new FTContext(output);

        var moveTo = Marshal.GetFunctionPointerForDelegate<FTContext.FT_Outline_MoveToFunc>(ftContext.FT_Outline_MoveToFuncImpl);
        var lineTo = Marshal.GetFunctionPointerForDelegate<FTContext.FT_Outline_LineToFunc>(ftContext.FT_Outline_LineToFuncImpl);
        var conicTo = Marshal.GetFunctionPointerForDelegate<FTContext.FT_Outline_ConicToFunc>(ftContext.FT_Outline_ConicToFuncImpl);
        var cubicTo = Marshal.GetFunctionPointerForDelegate<FTContext.FT_Outline_CubicToFunc>(ftContext.FT_Outline_CubicToFuncImpl);

        FT_Outline_Funcs funcs = new FT_Outline_Funcs
        {
            moveTo = moveTo,
            lineTo = lineTo,
            conicTo = conicTo,
            cubicTo = cubicTo,
            shift = 0,
            delta = 0
        };

        fixed (FT_Outline* outlinePtr = &outline)
        {
            FT_Error error = FT.FT_Outline_Decompose((nint)outlinePtr, ref funcs, IntPtr.Zero);
            if (output.Contours.Any() && !output.Contours.Last().Edges.Any())
            {
                output.RemoveContour(output.Contours.Last());
            }
            return error;
        }
    }

    internal static unsafe FontMetrics GetFontMetrics(MSDFGenFontHandle font)
    {
        float emSize = font.Face.FaceRec->units_per_EM;
        float ascenderY = font.Face.FaceRec->ascender;
        float descenderY = font.Face.FaceRec->descender;
        float lineHeight = font.Face.FaceRec->height;
        float underlineY = font.Face.FaceRec->underline_position;
        float underlineThickness = font.Face.FaceRec->underline_thickness;
        return new FontMetrics(emSize, ascenderY, descenderY, lineHeight, underlineY, underlineThickness);
    }

    internal static float GetKerning(MSDFGenFontHandle font, int glyphIndex1, int glyphIndex2)
    {
        var error = FT.FT_Get_Kerning(font.Face.Face, (uint)glyphIndex1, (uint)glyphIndex2, (uint)FT_Kerning_Mode.FT_KERNING_UNSCALED, out FT_Vector kerning);

        if (error != FT_Error.FT_Err_Ok)
            throw new ExternalException("Could not load kerning.");

        return kerning.x;
    }
}