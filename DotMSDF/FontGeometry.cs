using System;
using System.Collections.Generic;
using System.Linq;

namespace DotMSDF;

public record FontMetrics(float EmSize, float AscenderY, float DescenderY, float LineHeight, float UnderlineY, float UnderlineThickness)
{
    public FontMetrics Scale(float scaler)
    {
        return this with
        {
            EmSize = EmSize * scaler,
            AscenderY = AscenderY * scaler,
            DescenderY = DescenderY * scaler,
            LineHeight = LineHeight * scaler,
            UnderlineY = UnderlineY * scaler,
            UnderlineThickness = UnderlineThickness * scaler
        };
    }
}

public class FontGeometry
{
    private float _geometryScale = 1;
    private FontMetrics _fontMetrics;
    private GlyphIdentifierType _preferredIdentifierType;
    private List<GlyphGeometry> _glyphs = [];
    private Dictionary<int, int> _glyphsByIndex = [];
    private Dictionary<uint, int> _glyphsByCodepoint = [];
    private Dictionary<(int, int), float> _kerningPairs = [];
    private List<GlyphGeometry> _ownGlyphs = [];
    private string _name;

    public int LoadGlyphSet(MSDFGenFontHandle font, float fontScale, Charset glyphset, bool preprocessGeometry = true, bool enableKerning = true)
    {
        if (!LoadMetrics(font, fontScale))
            return -1;
        int loaded = 0;
        foreach (uint index in glyphset)
        {
            GlyphGeometry glyph = new GlyphGeometry();
            if (glyph.Load(font, _geometryScale, glyphIndex: (int)index, preprocessGeometry))
            {
                AddGlyph(glyph);
                ++loaded;
            }
        }
        if (enableKerning)
            LoadKerning(font);
        _preferredIdentifierType = GlyphIdentifierType.Index;
        return loaded;
    }

    public int LoadCharset(MSDFGenFontHandle font, float fontScale, Charset charset, bool preprocessGeometry = true, bool enableKerning = true)
    {
        if (!LoadMetrics(font, fontScale))
            return -1;
        int loaded = 0;
        foreach (uint codePoint in charset)
        {
            GlyphGeometry glyph = new GlyphGeometry();
            if (glyph.Load(font, _geometryScale, codePoint: codePoint, preprocessGeometry))
            {
                AddGlyph(glyph);
                ++loaded;
            }
        }
        if (enableKerning)
            LoadKerning(font);
        _preferredIdentifierType = GlyphIdentifierType.Codepoint;
        return loaded;
    }

    public bool LoadMetrics(MSDFGenFontHandle font, float fontScale)
    {
        var metrics = MSDFGen.GetFontMetrics(font);

        if (metrics.EmSize <= 0)
            metrics = metrics with { EmSize = 32 };

        _geometryScale = fontScale / metrics.EmSize;
        _fontMetrics = metrics.Scale(_geometryScale);
        return true;
    }

    public bool AddGlyph(GlyphGeometry glyph)
    {
        _glyphsByIndex.Add((int)glyph.GetIndex(), _glyphs.Count);
        if (glyph.GetCodepoint() != 0)
            _glyphsByCodepoint.Add(glyph.GetCodepoint(), _glyphs.Count);
        _glyphs.Add(glyph);
        return true;
    }

    private IEnumerable<(GlyphGeometry, GlyphGeometry)> GenerateAllKerningPairs()
    {
        for (int i = 0; i < _glyphs.Count; ++i)
            for (int j = 0; j < _glyphs.Count; ++j)
                yield return (_glyphs[i], _glyphs[j]);
    }

    public int LoadKerning(MSDFGenFontHandle font)
    {
        int loaded = 0;

        foreach (var pair in GenerateAllKerningPairs())
        {
            float advance = MSDFGen.GetKerning(font, (int)pair.Item1.GetIndex(), (int)pair.Item2.GetIndex());
            _kerningPairs[((int)pair.Item1.GetIndex(), (int)pair.Item2.GetIndex())] = advance;
            ++loaded;
        }

        return loaded;
    }

    public void SetName(string name)
    {
        _name = name;
    }

    public float GetGeometryScale() => _geometryScale;
    public FontMetrics GetMetrics() => _fontMetrics;
    public GlyphIdentifierType GetPreferredIdentifierType() => _preferredIdentifierType;
    public IEnumerable<GlyphGeometry> GetGlyphs() => _glyphs;

    public GlyphGeometry GetGlyphByIndex(int index) => _glyphs.First(x => x.GetIndex() == index);
    public GlyphGeometry GetGlyph(uint codePoint) => _glyphs.First(x => x.GetCodepoint() == codePoint);

    public float GetAdvanceByIndex(int index1, int index2)
    {
        float advance = 0f;
        var glyph1 = GetGlyphByIndex(index1);
        var glyph2 = GetGlyphByIndex(index2);

        advance += glyph1.Advance;

        if (_kerningPairs.TryGetValue((index1, index2), out var kerning))
        {
            advance += kerning;
        }

        return advance;
    }

    public float GetAdvance(uint codePoint1, uint codePoint2)
    {
        float advance = 0f;
        var glyph1 = GetGlyph(codePoint1);
        var glyph2 = GetGlyph(codePoint2);

        advance += glyph1.Advance;

        if (_kerningPairs.TryGetValue(((int)glyph1.GetIndex(), (int)glyph2.GetIndex()), out var kerning))
        {
            advance += kerning;
        }

        return advance;
    }

    public IReadOnlyDictionary<(int, int), float> GetKerningPairs() => _kerningPairs;

    public string GetName() => _name;
}