// using System.Collections.Generic;
// using DotMSDF.Port;

// namespace DotMSDF;

// public class FontInfo
// {
//     public record FontInfoAtlas(string Type, int DistanceRange, int Size, int Width, int Height, string YOrigin);
//     public record FontInfoMetrics(int EmSize, float LineHeight, float Ascender, float Descender, float UnderlineY, float UnderlineThickness);
//     public record FontInfoGlyphBounds(float Left, float Right, float Bottom, float Top);
//     public record FontInfoGlyph(int Unicode, float Advance, FontInfoGlyphBounds PlaneBounds, FontInfoGlyphBounds AtlasBounds);

//     public FontInfoAtlas Atlas { get; init; }
//     public string Name { get; init; }
//     public FontInfoMetrics Metrics { get; init; }
//     public IEnumerable<FontInfoGlyph> Glyphs { get; init; }
// }

// public static class MSDF
// {
//     public static (byte[] pixels, FontInfo fontInfo) GenerateFontAtlas(byte[] ttfData)
//     {
//         var library = ImportFont.InitializeFreetype();
//         var face = ImportFont.LoadFontFromMemory(library, ttfData);


//     }

//     private (Bitmap<FloatRgb>, FontInfo.FontInfoGlyph) GenerateAtlasForGlyph(Face face, char c)
//     {
//         double advance = 0;
//         var shape = ImportFont.LoadGlyph(face, c, ref advance);

//         shape.Normalize();
//         Coloring.EdgeColoringSimple(shape, 3.0);

//         var msdf = new Bitmap<FloatRgb>(32, 32);

//         var generator = Generate.Msdf();

//         generator.Output = msdf;
//         generator.Shape = shape;
//         generator.Range = 4;
//         generator.Scale = new Vector2(1.0);
//         generator.Translate = new Vector2(4, 2);
//         generator.Compute();

//         Render.Simulate8Bit(msdf);

//         var planeBounds =
//     }

//     private static FontInfo.FontInfoGlyphBounds GetPlaneBounds(Shape shape)
//     {
//         var bounds = shape.Bounds;
//         return new FontInfo.FontInfoGlyphBounds(bounds.Left, bounds.Right, bounds.Bottom, bounds.Top);
//     }
// }