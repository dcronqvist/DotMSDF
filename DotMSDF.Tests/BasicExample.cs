using DotMSDF.Port;

namespace DotMSDF.Tests;

public class BasicExample
{
    [Fact]
    public void SimpleTest()
    {
        var ft = ImportFont.InitializeFreetype();

        var font = ImportFont.LoadFont(ft, @"C:\Users\Daniel\winrepos\DotMSDF\DotMSDF.Tests\opensans.ttf");

        double advance = 0;
        var shape = ImportFont.LoadGlyph(font, 'A', ref advance);

        shape.Normalize();
        Coloring.EdgeColoringSimple(shape, 3.0);

        var msdf = new Bitmap<FloatRgb>(32, 32);

        var generator = Generate.Msdf();

        generator.Output = msdf;
        generator.Shape = shape;
        generator.Range = 4;
        generator.Scale = new Vector2(1.0);
        generator.Translate = new Vector2(4, 2);
        generator.Compute();

        Render.Simulate8Bit(msdf);
        // var freeTypeHandle = MSDFGen.InitializeFreeType();
        // //using fontHandle = MSDFGen.LoadFont(freeTypeHandle, inMemoryTTFbytes);
        // var fontHandle = MSDFGen.LoadFont(freeTypeHandle, @"C:\Users\Daniel\winrepos\DotMSDF\DotMSDF.Tests\opensans.ttf");

        // FontGeometry fontGeometry = new FontGeometry();
        // fontGeometry.LoadCharset(fontHandle, 1.0f, Charset.ASCII);

        // float maxCornerAngle = 3.0f;
        // foreach (var glyph in fontGeometry.GetGlyphs())
        // {
        //     glyph.EdgeColoring(GlyphGeometry.EdgeColoringSimple, maxCornerAngle, 0);
        // }

        // var packer = new SimplePacker();
        // var (atlasWidth, atlasHeight) = packer.Pack(ref fontGeometry);

        /*
        
        FontGeometry.LoadCharSet(ref glyphs, fontHandle, 1.0f, FontGeometry.CharSet.ASCII);

        var maxCornerAngle = 3.0f;
        foreach (var glyph in glyphs)
            glyph.EdgeColoring(MSDFGen.EdgeColoringInkTrap, maxCornerAngle, 0);

        var packer = new SimplePacker();
        var (atlasWidth, atlasHeight) = packer.Pack(ref glyphs);

        //IGenerator - interface with
        var generator = new ImmediateAtlasGenerator<(float, float, float)>(
            atlasWidth, 
            atlasHeight,
            (bitmap, glyph) => {
                // complex glyph rendering
            });

        var atlas = generator.Generate(glyphs);
        */
    }
}