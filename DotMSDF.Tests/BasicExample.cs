using DotMSDF.Port;

namespace DotMSDF.Tests;

public class BasicExample
{
    [Fact]
    public void SimpleTest()
    {
        var ft = ImportFont.InitializeFreetype();

        var font = ImportFont.LoadFont(ft, @"C:\Users\Daniel\winrepos\DotMSDF\DotMSDF.Tests\opensans.ttf");

        ImportFont.LoadGlyph(out var shape, font, 'A', out var advance);

        shape.Normalize();
        EdgeColoring.EdgeColoringSimple(ref shape, 3.0);

        var msdf = new Bitmap<Float3>(32, 32);

        var generator = Generate.Msdf();

        generator.Output = msdf;
        generator.Shape = shape;
        generator.Range = 4;
        generator.Scale = new Vector2(1.0);
        generator.Translate = new Vector2(4, 2);
        generator.Compute();

        Render.Simulate8Bit(msdf);
    }
}