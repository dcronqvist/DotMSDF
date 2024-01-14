using System;
using System.Runtime.InteropServices;
using FreeTypeSharp;
using FreeTypeSharp.Native;
using static FreeTypeSharp.Native.FT;
using static DotMSDF.Port.Arithmetics;
using static DotMSDF.Port.Vector2;
using System.Linq;

namespace DotMSDF.Port;

public record FreetypeHandle(FreeTypeLibrary Library);
public record FontHandle(FreetypeHandle Freetype, FreeTypeFaceFacade Face);

public record GlyphIndex(uint Index)
{
    public static implicit operator GlyphIndex(uint index) => new(index);
    public static implicit operator uint(GlyphIndex index) => index.Index;
}

public record FontMetrics(double EmSize, double AscenderY, double DescenderY, double LineHeight, double UnderlineY, double UnderlineTickness);

public partial class Constants
{
    public static double F26DOT6_TO_DOUBLE(int x) => x / 64.0;
    public static double F16DOT16_TO_DOUBLE(int x) => x / 65536.0;
    public static int DOUBLE_TO_F16DOT16(double x) => (int)(x * 65536.0);

    public static Point2 FTPoint2(FT_Vector vector) => new(F26DOT6_TO_DOUBLE((int)vector.x), F26DOT6_TO_DOUBLE((int)vector.y));
}

public class FtContext
{
    public delegate int FT_Outline_MoveToFunc(IntPtr to, IntPtr user);
    public delegate int FT_Outline_LineToFunc(IntPtr to, IntPtr user);
    public delegate int FT_Outline_ConicToFunc(IntPtr control, IntPtr to, IntPtr user);
    public delegate int FT_Outline_CubicToFunc(IntPtr control1, IntPtr control2, IntPtr to, IntPtr user);

    public Point2 Position { get; set; }
    public Shape Shape { get; set; }
    public Contour Contour { get; set; }

    public FT_Outline_MoveToFunc FtMoveTo => _FTMoveTo;
    public FT_Outline_LineToFunc FtLineTo => _FTLineTo;
    public FT_Outline_ConicToFunc FtConicTo => _FTConicTo;
    public FT_Outline_CubicToFunc FtCubicTo => _FTCubicTo;

    public int _FTMoveTo(IntPtr to, IntPtr user)
    {
        var vTo = Marshal.PtrToStructure<FT_Vector>(to);

        if (!(Contour != null && Contour.Edges.Count == 0))
        {
            Contour = Shape.AddContour();
        }

        Position = Constants.FTPoint2(vTo);
        return 0;
    }

    public int _FTLineTo(IntPtr to, IntPtr user)
    {
        var vTo = Marshal.PtrToStructure<FT_Vector>(to);

        Point2 endpoint = Constants.FTPoint2(vTo);
        if (endpoint != Position)
        {
            Contour.AddEdge(new EdgeHolder(Position, endpoint));
            Position = endpoint;
        }
        return 0;
    }

    public int _FTConicTo(IntPtr control, IntPtr to, IntPtr user)
    {
        var vControl = Marshal.PtrToStructure<FT_Vector>(control);
        var vTo = Marshal.PtrToStructure<FT_Vector>(to);

        Point2 endPoint = Constants.FTPoint2(vTo);
        if (endPoint != Position)
        {
            Contour.AddEdge(new EdgeHolder(Position, Constants.FTPoint2(vControl), endPoint));
            Position = endPoint;
        }
        return 0;
    }

    public int _FTCubicTo(IntPtr control1, IntPtr control2, IntPtr to, IntPtr user)
    {
        var vControl1 = Marshal.PtrToStructure<FT_Vector>(control1);
        var vControl2 = Marshal.PtrToStructure<FT_Vector>(control2);
        var vTo = Marshal.PtrToStructure<FT_Vector>(to);

        Point2 endPoint = Constants.FTPoint2(vTo);
        if (endPoint != Position || CrossProduct(Constants.FTPoint2(vControl1) - endPoint, Constants.FTPoint2(vControl2) - endPoint) != 0)
        {
            Contour.AddEdge(new EdgeHolder(Position, Constants.FTPoint2(vControl1), Constants.FTPoint2(vControl2), endPoint));
            Position = endPoint;
        }
        return 0;
    }
}

public static class ImportFont
{
    public static FreetypeHandle InitializeFreetype()
    {
        var library = new FreeTypeLibrary();
        return new FreetypeHandle(library);
    }

    public static void DeinitializeFreetype(FreetypeHandle freetype)
    {
        FT_Done_FreeType(freetype.Library.Native);
    }

    private unsafe static FT_Error ReadFreeTypeOutline(ref Shape output, FT_Outline outline)
    {
        output.Contours.Clear();
        output.InverseYAxis = false;
        FtContext context = new();
        context.Shape = output;
        FT_Outline_Funcs ftFuncs = new()
        {
            moveTo = Marshal.GetFunctionPointerForDelegate(context.FtMoveTo),
            lineTo = Marshal.GetFunctionPointerForDelegate(context.FtLineTo),
            conicTo = Marshal.GetFunctionPointerForDelegate(context.FtConicTo),
            cubicTo = Marshal.GetFunctionPointerForDelegate(context.FtCubicTo),
            shift = 0,
            delta = 0
        };
        FT_Error error = FT_Outline_Decompose((nint)(&outline), ref ftFuncs, IntPtr.Zero);
        if (output.Contours.Count != 0 && output.Contours.Last().Edges.Count == 0)
        {
            output.Contours.RemoveAt(output.Contours.Count - 1);
        }
        return error;
    }

    public static FontHandle LoadFont(FreetypeHandle library, string filename)
    {
        FT_Error error = FT_New_Face(library.Library.Native, filename, 0, out var aface);
        if (error != FT_Error.FT_Err_Ok)
        {
            return null;
        }
        else
        {
            return new FontHandle(library, new FreeTypeFaceFacade(library.Library, aface));
        }
    }

    public unsafe static FontHandle LoadFont(FreetypeHandle library, byte[] data)
    {
        fixed (byte* ptr = &data[0])
        {
            FT_Error error = FT_New_Memory_Face(library.Library.Native, (nint)ptr, data.Length, 0, out var aface);
            if (error != FT_Error.FT_Err_Ok)
            {
                return null;
            }
            else
            {
                return new FontHandle(library, new FreeTypeFaceFacade(library.Library, aface));
            }
        }
    }

    public static void DestroyFont(FontHandle font)
    {
        FT_Done_Face(font.Face.Face);
    }

    public unsafe static bool GetFontMetrics(out FontMetrics metrics, FontHandle font)
    {
        metrics = new FontMetrics(
            EmSize: Constants.F26DOT6_TO_DOUBLE(font.Face.FaceRec->units_per_EM),
            AscenderY: Constants.F26DOT6_TO_DOUBLE(font.Face.FaceRec->ascender),
            DescenderY: Constants.F26DOT6_TO_DOUBLE(font.Face.FaceRec->descender),
            LineHeight: Constants.F26DOT6_TO_DOUBLE(font.Face.FaceRec->height),
            UnderlineY: Constants.F26DOT6_TO_DOUBLE(font.Face.FaceRec->underline_position),
            UnderlineTickness: Constants.F26DOT6_TO_DOUBLE(font.Face.FaceRec->underline_thickness)
        );

        return true;
    }

    public unsafe static bool GetFontWhitespaceWidth(out double spaceAdvance, out double tabAdvance, FontHandle font)
    {
        spaceAdvance = 0;
        tabAdvance = 0;

        FT_Error error = FT_Load_Char(font.Face.Face, ' ', FT_LOAD_NO_SCALE);
        if (error != FT_Error.FT_Err_Ok)
            return false;
        spaceAdvance = Constants.F26DOT6_TO_DOUBLE((int)font.Face.FaceRec->glyph->advance.x);

        error = FT_Load_Char(font.Face.Face, '\t', FT_LOAD_NO_SCALE);
        if (error != FT_Error.FT_Err_Ok)
            return false;

        tabAdvance = Constants.F26DOT6_TO_DOUBLE((int)font.Face.FaceRec->glyph->advance.x);
        return true;
    }

    public static bool GetGlyphIndex(out GlyphIndex glyphIndex, FontHandle font, unicode_t unicode)
    {
        glyphIndex = FT_Get_Char_Index(font.Face.Face, unicode);
        return glyphIndex != 0;
    }

    public unsafe static bool LoadGlyph(out Shape output, FontHandle font, GlyphIndex glyphIndex, out double advance)
    {
        output = new Shape();
        advance = 0;

        FT_Error error = FT_Load_Glyph(font.Face.Face, glyphIndex.Index, FT_LOAD_NO_SCALE);
        if (error != FT_Error.FT_Err_Ok)
            return false;

        advance = Constants.F26DOT6_TO_DOUBLE((int)font.Face.FaceRec->glyph->advance.x);

        return ReadFreeTypeOutline(ref output, font.Face.FaceRec->glyph->outline) != FT_Error.FT_Err_Ok;
    }

    public unsafe static bool LoadGlyph(out Shape output, FontHandle font, unicode_t unicode, out double advance)
    {
        return LoadGlyph(out output, font, new GlyphIndex(FT_Get_Char_Index(font.Face.Face, unicode)), out advance);
    }

    public unsafe static bool LoadGlyph(out Shape output, FontHandle font, char character, out double advance)
    {
        return LoadGlyph(out output, font, new GlyphIndex(FT_Get_Char_Index(font.Face.Face, character)), out advance);
    }

    public unsafe static bool GetKerning(out double output, FontHandle font, GlyphIndex glyphIndex1, GlyphIndex glyphIndex2)
    {
        output = 0;

        FT_Error error = FT_Get_Kerning(font.Face.Face, glyphIndex1, glyphIndex2, (uint)FT_Kerning_Mode.FT_KERNING_UNSCALED, out var kerning);
        if (error != FT_Error.FT_Err_Ok)
            return false;

        output = Constants.F26DOT6_TO_DOUBLE((int)kerning.x);
        return true;
    }

    public unsafe static bool GetKerning(out double output, FontHandle font, unicode_t unicode1, unicode_t unicode2)
    {
        return GetKerning(out output, font, new GlyphIndex(FT_Get_Char_Index(font.Face.Face, unicode1)), new GlyphIndex(FT_Get_Char_Index(font.Face.Face, unicode2)));
    }
}