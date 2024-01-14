using System.Drawing;

namespace DotMSDF.Port;

public class GlyphBox
{
    public int Index { get; set; }
    public double Advance { get; set; }
    public double BoundsL { get; set; }
    public double BoundsB { get; set; }
    public double BoundsR { get; set; }
    public double BoundsT { get; set; }
    public Rectangle Rect { get; set; }
}