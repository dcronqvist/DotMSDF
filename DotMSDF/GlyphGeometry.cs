using System;
using System.Drawing;
using System.Numerics;

namespace DotMSDF;

public enum GlyphIdentifierType
{
    Codepoint,
    Index
}

public class GlyphGeometry
{
    public delegate void EdgeColoringMethod(ref Shape shape, float angleThreshold, int seed = 0);

    private record GlyphBox(RectangleF Rect, float Range, float Scale, Vector2 Translate);

    private int _index;
    private uint _codepoint;
    private double _geometryScale;
    private Shape _shape;
    private RectangleF _bounds;
    float _advance;
    private GlyphBox _box;

    public GlyphGeometry() { }

    public bool Load(MSDFGenFontHandle font, float geometryScale, uint codePoint, bool preprocessGeometry = true)
    {

    }

    public void EdgeColoring(EdgeColoringMethod edgeColoringMethod, float angleThreshold, int seed)
    {

    }

    public void WrapBox(float scale, float range, float miterLimit)
    {

    }

    public void PlaceBox(int x, int y)
    {

    }

    public void SetBoxRect(RectangleF rect)
    {

    }

    public uint GetIndex()
    {

    }

    public uint GetCodepoint()
    {

    }

    public uint GetIdentifier(GlyphIdentifierType type)
    {

    }
}