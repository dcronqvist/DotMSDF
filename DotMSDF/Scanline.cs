using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DotMSDF;

public enum FillRule
{
    NonZero,
    EvenOdd,
    Positive,
    Negative
}

public record Intersection(float X, int Direction);

public class Scanline
{
    private List<Intersection> _intersections = [];
    private int _lastIndex;

    private static bool InterpretFillRule(int intersections, FillRule fillRule)
    {
        switch (fillRule)
        {
            case FillRule.NonZero:
                return intersections != 0;
            case FillRule.EvenOdd:
                return (intersections & 1) != 0;
            case FillRule.Positive:
                return intersections > 0;
            case FillRule.Negative:
                return intersections < 0;
            default:
                return false;
        }
    }

    private static int CompareIntersections(Intersection a, Intersection b)
    {
        return MathF.Sign(a.X - b.X);
    }

    public static float Overlap(Scanline a, Scanline b, float xFrom, float xTo, FillRule fillRule)
    {
        float total = 0;
        bool aInside = false, bInside = false;
        int ai = 0, bi = 0;
        float ax = a._intersections.Any() ? a._intersections[ai].X : xTo;
        float bx = b._intersections.Any() ? b._intersections[bi].X : xTo;
        while (ax < xFrom || bx < xFrom)
        {
            float xNext = MathF.Min(ax, bx);
            if (ax == xNext && ai < (int)a._intersections.Count)
            {
                aInside = InterpretFillRule(a._intersections[ai].Direction, fillRule);
                ax = ++ai < (int)a._intersections.Count() ? a._intersections[ai].X : xTo;
            }
            if (bx == xNext && bi < (int)b._intersections.Count)
            {
                bInside = InterpretFillRule(b._intersections[bi].Direction, fillRule);
                bx = ++bi < (int)b._intersections.Count ? b._intersections[bi].X : xTo;
            }
        }
        float x = xFrom;
        while (ax < xTo || bx < xTo)
        {
            float xNext = MathF.Min(ax, bx);
            if (aInside == bInside)
                total += xNext - x;
            if (ax == xNext && ai < (int)a._intersections.Count)
            {
                aInside = InterpretFillRule(a._intersections[ai].Direction, fillRule);
                ax = ++ai < (int)a._intersections.Count ? a._intersections[ai].X : xTo;
            }
            if (bx == xNext && bi < (int)b._intersections.Count)
            {
                bInside = InterpretFillRule(b._intersections[bi].Direction, fillRule);
                bx = ++bi < (int)b._intersections.Count ? b._intersections[bi].X : xTo;
            }
            x = xNext;
        }
        if (aInside == bInside)
            total += xTo - x;
        return total;
    }

    public void SetIntersections(IEnumerable<Intersection> intersections)
    {
        _intersections = intersections.ToList();
        Preprocess();
    }

    public int CountIntersections(float x)
    {
        return MoveTo(x) + 1;
    }

    public int SumIntersections(float x)
    {
        int index = MoveTo(x);
        if (index >= 0)
            return _intersections[index].Direction;
        return 0;
    }

    public bool Filled(float x, FillRule fillRule)
    {
        return InterpretFillRule(SumIntersections(x), fillRule);
    }

    private void Preprocess()
    {
        _lastIndex = 0;
        if (_intersections.Any())
        {
            _intersections.Sort(CompareIntersections);
            int totalDirection = 0;
            for (int i = 0; i < _intersections.Count; i++)
            {
                totalDirection += _intersections[i].Direction;
                _intersections[i] = _intersections[i] with { Direction = totalDirection };
            }
        }
    }

    private int MoveTo(float x)
    {
        if (!_intersections.Any())
            return -1;
        int index = _lastIndex;
        if (x < _intersections[index].X)
        {
            do
            {
                if (index == 0)
                {
                    _lastIndex = 0;
                    return -1;
                }
                --index;
            } while (x < _intersections[index].X);
        }
        else
        {
            while (index < (int)_intersections.Count - 1 && x >= _intersections[index + 1].X)
                ++index;
        }
        _lastIndex = index;
        return index;
    }
}