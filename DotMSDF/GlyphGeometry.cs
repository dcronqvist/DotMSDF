using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using FreeTypeSharp.Native;

namespace DotMSDF;

public enum GlyphIdentifierType
{
    Codepoint,
    Index
}

public class GlyphGeometry
{
    public delegate void EdgeColoringMethod(ref Shape shape, float angleThreshold, int seed = 0);

    private static bool IsCorner(Vector2 aDir, Vector2 bDir, float crossThreshold)
    {
        return Vector2.Dot(aDir, bDir) <= 0 || MathF.Abs(aDir.Cross(bDir)) > crossThreshold;
    }

    private static float EstimateEdgeLength(IEdgeSegment edge)
    {
        float len = 0;
        Vector2 prev = edge.Point(0);
        for (int i = 1; i <= 4; ++i)
        {
            Vector2 cur = edge.Point(1f / 4 * i);
            len += (cur - prev).Length();
            prev = cur;
        }
        return len;
    }

    private static Dictionary<int, Random> _rngsPerSeed = [];
    private static T ChooseRandomly<T>(int seed, params T[] values)
    {
        if (!_rngsPerSeed.TryGetValue(seed, out var rng))
            _rngsPerSeed[seed] = new Random(seed);

        var random = _rngsPerSeed[seed];
        return values[random.NextInt64(0, values.Length)];
    }

    private static void SwitchColor(EdgeColor color, int seed, EdgeColor banned = EdgeColor.Black)
    {
        EdgeColor combined = color & banned;
        if (combined == EdgeColor.Red || combined == EdgeColor.Green || combined == EdgeColor.Blue)
        {
            color = combined ^ EdgeColor.White;
            return;
        }
        if (color == EdgeColor.Black || color == EdgeColor.White)
        {
            color = ChooseRandomly(seed, EdgeColor.Cyan, EdgeColor.Magenta, EdgeColor.Yellow);
            return;
        }
        int shifted = (int)color << (1 + (seed & 1));
        color = (EdgeColor)((shifted | shifted >> 3) & (int)EdgeColor.White);
        seed >>= 1;
    }

    public static void EdgeColoringSimple(ref Shape shape, float angleThreshold, int seed = 0)
    {
        float crossThreshold = MathF.Sin(angleThreshold);
        List<int> corners = [];
        foreach (var contour in shape.Contours)
        {
            // Identify corners
            corners.Clear();
            if (contour.Edges.Any())
            {
                Vector2 prevDirection = contour.Edges.Last().Direction(1);
                int index = 0;
                foreach (var edge in contour.Edges)
                {
                    if (IsCorner(prevDirection.Normalize(), edge.Direction(0).Normalize(), crossThreshold))
                        corners.Add(index);

                    prevDirection = edge.Direction(1);
                    index++;
                }
            }

            // Smooth contour
            if (corners.empty())
                for (std::vector<EdgeHolder>::iterator edge = contour->edges.begin(); edge != contour->edges.end(); ++edge)
                    (*edge)->color = WHITE;
            // "Teardrop" case
            else if (corners.size() == 1)
            {
                EdgeColor colors[3] = { WHITE, WHITE };
                switchColor(colors[0], seed);
                switchColor(colors[2] = colors[0], seed);
                int corner = corners[0];
                if (contour->edges.size() >= 3)
                {
                    int m = (int)contour->edges.size();
                    for (int i = 0; i < m; ++i)
                        contour->edges[(corner + i) % m]->color = (colors + 1)[int(3 + 2.875 * i / (m - 1) - 1.4375 + .5) - 3];
                }
                else if (contour->edges.size() >= 1)
                {
                    // Less than three edge segments for three colors => edges must be split
                    EdgeSegment* parts[7] = { };
                    contour->edges[0]->splitInThirds(parts[0 + 3 * corner], parts[1 + 3 * corner], parts[2 + 3 * corner]);
                    if (contour->edges.size() >= 2)
                    {
                        contour->edges[1]->splitInThirds(parts[3 - 3 * corner], parts[4 - 3 * corner], parts[5 - 3 * corner]);
                        parts[0]->color = parts[1]->color = colors[0];
                        parts[2]->color = parts[3]->color = colors[1];
                        parts[4]->color = parts[5]->color = colors[2];
                    }
                    else
                    {
                        parts[0]->color = colors[0];
                        parts[1]->color = colors[1];
                        parts[2]->color = colors[2];
                    }
                    contour->edges.clear();
                    for (int i = 0; parts[i]; ++i)
                        contour->edges.push_back(EdgeHolder(parts[i]));
                }
            }
            // Multiple corners
            else
            {
                int cornerCount = (int)corners.size();
                int spline = 0;
                int start = corners[0];
                int m = (int)contour->edges.size();
                EdgeColor color = WHITE;
                switchColor(color, seed);
                EdgeColor initialColor = color;
                for (int i = 0; i < m; ++i)
                {
                    int index = (start + i) % m;
                    if (spline + 1 < cornerCount && corners[spline + 1] == index)
                    {
                        ++spline;
                        switchColor(color, seed, EdgeColor((spline == cornerCount - 1) * initialColor));
                    }
                    contour->edges[index]->color = color;
                }
            }
        }
    }

    public class GlyphBox(RectangleF rect, float range, float scale, Vector2 translate)
    {
        public RectangleF Rect { get; set; } = rect;
        public float Range { get; set; } = range;
        public float Scale { get; set; } = scale;
        public Vector2 Translate { get; set; } = translate;
    }

    private int _index;
    private uint _codepoint;

    private char _codePointAsChar => (char)_codepoint;

    private float _geometryScale;

    private Shape _shape;
    public Shape Shape => _shape;

    private RectangleF _bounds;

    float _advance;
    public float Advance => _advance;

    private GlyphBox _box;
    public GlyphBox Box => _box;

    public GlyphGeometry()
    {
        _shape = new Shape();
        _box = new GlyphBox(new RectangleF(0, 0, 0, 0), 0, 0, Vector2.Zero);
    }

    private static bool GetGlyphIndex(ref int glyphIndex, MSDFGenFontHandle fontHandle, uint unicode)
    {
        glyphIndex = (int)FT.FT_Get_Char_Index(fontHandle.Face.Face, unicode);
        return glyphIndex != 0;
    }

    public bool Load(MSDFGenFontHandle font, float geometryScale, uint codePoint, bool preprocessGeometry = true)
    {
        if (!GetGlyphIndex(ref _index, font, codePoint))
            return false;

        if (!Load(font, geometryScale, _index, preprocessGeometry))
            return false;

        _codepoint = codePoint;

        return true;
    }

    public bool Load(MSDFGenFontHandle font, float geometryScale, int glyphIndex, bool preprocessGeometry = true)
    {
        if (!MSDFGen.LoadGlyph(ref _shape, font, glyphIndex, ref _advance))
            return false;

        _index = glyphIndex;
        _geometryScale = geometryScale;
        _codepoint = 0;
        _advance *= geometryScale;
        _shape.Normalize();
        _bounds = _shape.GetBounds();

        return true;
    }

    public void EdgeColoring(EdgeColoringMethod edgeColoringMethod, float angleThreshold, int seed)
    {
        edgeColoringMethod(ref _shape, angleThreshold, seed);
    }

    public void WrapBox(float scale, float range, float miterLimit)
    {
        scale *= _geometryScale;
        range /= _geometryScale;
        Box.Range = range;
        Box.Scale = scale;
        if (_bounds.Left < _bounds.Right && _bounds.Bottom < _bounds.Top)
        {
            float l = _bounds.Left, b = _bounds.Bottom, r = _bounds.Right, t = _bounds.Top;
            l -= .5f * range;
            b -= .5f * range;
            r += .5f * range;
            t += .5f * range;
            var rect = RectangleF.FromLTRB(l, b, r, t);
            if (miterLimit > 0)
                _shape.BoundMiters(ref rect, .5f * range, miterLimit, 1);

            l = rect.Left;
            b = rect.Bottom;
            r = rect.Right;
            t = rect.Top;

            float w = scale * (r - l);
            float h = scale * (t - b);

            _box.Rect = new RectangleF(_box.Rect.X, _box.Rect.Y, (int)MathF.Ceiling(w) + 1, (int)MathF.Ceiling(h) + 1);
            _box.Translate = new Vector2(
                -l + .5f * (_box.Rect.Width - w) / scale,
                -b + .5f * (_box.Rect.Height - h) / scale
            );
        }
        else
        {
            _box.Rect = new RectangleF(_box.Rect.X, _box.Rect.Y, 0, 0);
            _box.Translate = Vector2.Zero;
        }
    }

    public void PlaceBox(int x, int y)
    {
        _box.Translate = new Vector2(x, y);
    }

    public void SetBoxRect(RectangleF rect)
    {
        _box.Rect = rect;
    }

    public uint GetIndex()
    {
        return (uint)_index;
    }

    public uint GetCodepoint()
    {
        return _codepoint;
    }

    public uint GetIdentifier(GlyphIdentifierType type)
    {
        switch (type)
        {
            case GlyphIdentifierType.Codepoint:
                return GetCodepoint();
            case GlyphIdentifierType.Index:
                return GetIndex();
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public Projection GetBoxProjection()
    {
        return new Projection(Vector2.One * _box.Scale, _box.Translate);
    }

    public bool IsWhitespace => !_shape.Contours.Any();
}