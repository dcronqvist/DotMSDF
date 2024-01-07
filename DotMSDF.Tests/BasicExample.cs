namespace DotMSDF.Tests;

public class BasicExample
{
    [Fact]
    public void SimpleTest()
    {
        /*

        using freeTypeHandle = MSDFGen.InitializeFreeType();
        //using fontHandle = MSDFGen.LoadFont(freeTypeHandle, inMemoryTTFbytes);
        using fontHandle = MSDFGen.LoadFont(freeTypeHandle, "path/to/font.ttf");

        IGlyphStorage glyphs = new GlyphStorage();
        
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