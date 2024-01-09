using System;
using System.IO;
using System.Runtime.InteropServices;
using FreeTypeSharp;
using FreeTypeSharp.Native;
using static FreeTypeSharp.Native.FT;

namespace DotMSDF.Port;

public record Library(FreeTypeLibrary LibraryFacade);
public record Face(FreeTypeFaceFacade FaceFacade, Library Library);

public static class ImportFont
{
    public static Library InitializeFreetype()
    {
        return new(new FreeTypeLibrary());
    }

    public static void DeinitializeFreetype(Library library)
    {
        library.LibraryFacade.Dispose();
    }

    public static unsafe Face LoadFont(Library library, string filename)
    {
        IntPtr face;

        using var binaryReader = new BinaryReader(File.OpenRead(filename));
        var ttfData = binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);
        binaryReader.Dispose();

        fixed (byte* p = &ttfData[0])
        {
            if (FT_New_Memory_Face(library.LibraryFacade.Native, new nint(p), ttfData.Length, 0, out face) != FT_Error.FT_Err_Ok)
                throw new Exception("Failed to load font");
        }

        return new(new FreeTypeFaceFacade(library.LibraryFacade, face), library);
    }

    public static void DestroyFont(Face font)
    {
        FT_Done_Face(font.FaceFacade.Face);
    }

    public unsafe static double GetFontScale(FreeTypeFaceFacade font)
    {
        return font.FaceRec->units_per_EM / 64.0;
    }

    // public static void GetFontWhitespaceWidth(ref double spaceAdvance, ref double tabAdvance, Face font)
    // {
    //     font.LoadChar(' ', LoadFlags.NoScale, LoadTarget.Normal);
    //     spaceAdvance = font.Glyph.Advance.X.Value / 64.0;
    //     font.LoadChar('\t', LoadFlags.NoScale, LoadTarget.Normal);
    //     tabAdvance = font.Glyph.Advance.X.Value / 64.0;
    // }

    public static unsafe Shape LoadGlyph(Face font, uint unicode, ref double advance)
    {
        var result = new Shape();

        FT_Load_Char(font.FaceFacade.Face, unicode, FT_LOAD_NO_SCALE);
        result.InverseYAxis = false;
        advance = font.FaceFacade.GlyphMetricHorizontalAdvance / 64.0;
        var context = new FtContext(result);

        IntPtr moveTo = Marshal.GetFunctionPointerForDelegate(context.FtMoveTo);
        IntPtr lineTo = Marshal.GetFunctionPointerForDelegate(context.FtLineTo);
        IntPtr conicTo = Marshal.GetFunctionPointerForDelegate(context.FtConicTo);
        IntPtr cubicTo = Marshal.GetFunctionPointerForDelegate(context.FtCubicTo);

        var ftFunctions = new FT_Outline_Funcs
        {
            moveTo = moveTo,
            lineTo = lineTo,
            conicTo = conicTo,
            cubicTo = cubicTo,
            shift = 0
        };

        var outline = font.FaceFacade.GlyphSlot->outline;
        var outlinePtr = &outline;

        FT_Outline_Decompose((nint)outlinePtr, ref ftFunctions, IntPtr.Zero);

        return result;
    }

    public static double GetKerning(Face font, uint unicode1, uint unicode2)
    {
        FT_Get_Kerning(font.FaceFacade.Face, unicode1, unicode2, (uint)FT_Kerning_Mode.FT_KERNING_UNSCALED, out var kerning);

        return kerning.x / 64.0;
    }

    public delegate int FT_Outline_MoveToFunc(IntPtr to, IntPtr user);
    public delegate int FT_Outline_LineToFunc(IntPtr to, IntPtr user);
    public delegate int FT_Outline_ConicToFunc(IntPtr control, IntPtr to, IntPtr user);
    public delegate int FT_Outline_CubicToFunc(IntPtr control1, IntPtr control2, IntPtr to, IntPtr user);

    private class FtContext
    {
        private readonly Shape _shape;
        private Contour _contour;
        private Vector2 _position;

        public FT_Outline_MoveToFunc FtMoveTo => _FtMoveTo;
        public FT_Outline_LineToFunc FtLineTo => _FtLineTo;
        public FT_Outline_ConicToFunc FtConicTo => _FtConicTo;
        public FT_Outline_CubicToFunc FtCubicTo => _FtCubicTo;

        public FtContext(Shape output)
        {
            _shape = output;
        }

        private static Vector2 FtPoint2(ref FT_Vector vector)
        {
            return new Vector2(vector.x / 128.0, vector.y / 128.0);
        }

        internal int _FtMoveTo(IntPtr to, IntPtr user)
        {
            var vector = Marshal.PtrToStructure<FT_Vector>(to);

            _contour = new Contour();
            _shape.Add(_contour);
            _position = FtPoint2(ref vector);
            return 0;
        }

        internal int _FtLineTo(IntPtr to, IntPtr user)
        {
            var vector = Marshal.PtrToStructure<FT_Vector>(to);

            _contour.Add(new LinearSegment(_position, FtPoint2(ref vector)));
            _position = FtPoint2(ref vector);
            return 0;
        }

        internal int _FtConicTo(IntPtr control, IntPtr to, IntPtr user)
        {
            var cVector = Marshal.PtrToStructure<FT_Vector>(control);
            var tVector = Marshal.PtrToStructure<FT_Vector>(to);

            _contour.Add(new QuadraticSegment(EdgeColor.White, _position, FtPoint2(ref cVector), FtPoint2(ref tVector)));
            _position = FtPoint2(ref tVector);
            return 0;
        }

        internal int _FtCubicTo(IntPtr control1, IntPtr control2, IntPtr to, IntPtr user)
        {
            var c1Vector = Marshal.PtrToStructure<FT_Vector>(control1);
            var c2Vector = Marshal.PtrToStructure<FT_Vector>(control2);
            var tVector = Marshal.PtrToStructure<FT_Vector>(to);

            _contour.Add(new CubicSegment(EdgeColor.White, _position, FtPoint2(ref c1Vector), FtPoint2(ref c2Vector), FtPoint2(ref tVector)));
            _position = FtPoint2(ref tVector);
            return 0;
        }
    }
}