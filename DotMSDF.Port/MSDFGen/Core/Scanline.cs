using System.Collections.Generic;
using System.Linq;
using static DotMSDF.Port.Arithmetics;

namespace DotMSDF.Port;

public enum FillRule
{
    FILL_NONZERO,
    FILL_ODD,
    FILL_POSITIVE,
    FILL_NEGATIVE
}

public class Scanline
{
    private static int CompareIntersections(Intersection a, Intersection b)
    {
        return Sign(a.X - b.X);
    }

    public static bool InterpretFillRule(int intersections, FillRule fillRule)
    {
        switch (fillRule)
        {
            case FillRule.FILL_NONZERO:
                return intersections != 0;
            case FillRule.FILL_ODD:
                return (intersections & 1) == 0 ? false : true;
            case FillRule.FILL_POSITIVE:
                return intersections > 0;
            case FillRule.FILL_NEGATIVE:
                return intersections < 0;
        }
        return false;
    }

    public struct Intersection
    {
        public double X { get; set; }
        public int Direction { get; set; }
    }

    public static double Overlap(Scanline a, Scanline b, double xFrom, double xTo, FillRule fillRule)
    {
        double total = 0;
        bool aInside = false, bInside = false;
        int ai = 0, bi = 0;
        double ax = a._intersections.Any() ? a._intersections[ai].X : xTo;
        double bx = b._intersections.Any() ? b._intersections[bi].X : xTo;
        while (ax < xFrom || bx < xFrom)
        {
            double xNext = Min(ax, bx);
            if (ax == xNext && ai < a._intersections.Count)
            {
                aInside = InterpretFillRule(a._intersections[ai].Direction, fillRule);
                ax = ++ai < a._intersections.Count ? a._intersections[ai].X : xTo;
            }
            if (bx == xNext && bi < b._intersections.Count)
            {
                bInside = InterpretFillRule(b._intersections[bi].Direction, fillRule);
                bx = ++bi < b._intersections.Count ? b._intersections[bi].X : xTo;
            }
        }
        double x = xFrom;
        while (ax < xTo || bx < xTo)
        {
            double xNext = Min(ax, bx);
            if (aInside == bInside)
                total += xNext - x;
            if (ax == xNext && ai < a._intersections.Count)
            {
                aInside = InterpretFillRule(a._intersections[ai].Direction, fillRule);
                ax = ++ai < a._intersections.Count ? a._intersections[ai].X : xTo;
            }
            if (bx == xNext && bi < b._intersections.Count)
            {
                bInside = InterpretFillRule(b._intersections[bi].Direction, fillRule);
                bx = ++bi < b._intersections.Count ? b._intersections[bi].X : xTo;
            }
            x = xNext;
        }
        if (aInside == bInside)
            total += xTo - x;
        return total;
    }

    private List<Intersection> _intersections = [];
    private int _lastIndex;

    public void Preprocess()
    {
        _lastIndex = 0;
        if (_intersections.Any())
        {
            _intersections.Sort(CompareIntersections);
            int totalDirection = 0;
            for (int i = 0; i < _intersections.Count; i++)
            {
                Intersection intersection = _intersections[i];

                totalDirection += intersection.Direction;
                intersection.Direction = totalDirection;
            }
        }
    }

    public void SetIntersections(List<Intersection> intersections)
    {
        _intersections = intersections;
        Preprocess();
    }

    public int MoveTo(double x)
    {
        if (_intersections.Count == 0)
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
            while (index < _intersections.Count - 1 && x >= _intersections[index + 1].X)
                ++index;
        }
        _lastIndex = index;
        return index;
    }

    public int CountIntersections(double x)
    {
        return MoveTo(x) + 1;
    }

    public int SumIntersections(double x)
    {
        int index = MoveTo(x);
        if (index >= 0)
            return _intersections[index].Direction;
        return 0;
    }

    public bool Filled(double x, FillRule fillRule)
    {
        return InterpretFillRule(SumIntersections(x), fillRule);
    }
}