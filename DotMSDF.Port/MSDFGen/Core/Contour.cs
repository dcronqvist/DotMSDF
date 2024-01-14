using System;
using System.Collections.Generic;
using System.Linq;
using static DotMSDF.Port.Arithmetics;
using static DotMSDF.Port.Vector2;

namespace DotMSDF.Port;

public class Contour
{
    private readonly List<EdgeHolder> _edges = [];

    public IReadOnlyList<EdgeHolder> Edges => _edges;

    public void ClearEdges() => _edges.Clear();

    public void AddEdge(EdgeHolder edge) => _edges.Add(edge);

    private static double Shoelace(Point2 a, Point2 b) => (b.X - a.X) * (a.Y + b.Y);

    public void Bound(ref double l, ref double b, ref double r, ref double t)
    {
        foreach (var edge in _edges)
        {
            edge.EdgeSegment.Bound(ref l, ref b, ref r, ref t);
        }
    }

    public void BoundMiters(ref double l, ref double b, ref double r, ref double t, double border, double miterLimit, int polarity)
    {
        if (!_edges.Any())
            return;
        Vector2 prevDir = _edges.Last().EdgeSegment.Direction(1).Normalize(true);
        foreach (var edge in _edges)
        {
            Vector2 dir = -edge.EdgeSegment.Direction(0).Normalize(true);
            if (polarity * CrossProduct(prevDir, dir) >= 0)
            {
                double miterLength = miterLimit;
                double q = .5 * (1 - DotProduct(prevDir, dir));
                if (q > 0)
                    miterLength = Min(1 / Math.Sqrt(q), miterLimit);
                Point2 miter = edge.EdgeSegment.Point(0) + border * miterLength * (prevDir + dir).Normalize(true);
                IEdgeSegment.PointBounds(miter, ref l, ref b, ref r, ref t);
            }
            prevDir = edge.EdgeSegment.Direction(1).Normalize(true);
        }
    }

    public int Winding()
    {
        if (!_edges.Any())
            return 0;
        double total = 0;
        if (_edges.Count == 1)
        {
            Point2 a = _edges[0].EdgeSegment.Point(0);
            Point2 b = _edges[0].EdgeSegment.Point(1 / 3.0);
            Point2 c = _edges[0].EdgeSegment.Point(2 / 3.0);
            total += Shoelace(a, b);
            total += Shoelace(b, c);
            total += Shoelace(c, a);
        }
        else if (_edges.Count == 2)
        {
            Point2 a = _edges[0].EdgeSegment.Point(0);
            Point2 b = _edges[0].EdgeSegment.Point(.5);
            Point2 c = _edges[1].EdgeSegment.Point(0);
            Point2 d = _edges[1].EdgeSegment.Point(.5);
            total += Shoelace(a, b);
            total += Shoelace(b, c);
            total += Shoelace(c, d);
            total += Shoelace(d, a);
        }
        else
        {
            Point2 prev = _edges.Last().EdgeSegment.Point(0);
            foreach (var edge in _edges)
            {
                Point2 cur = edge.EdgeSegment.Point(0);
                total += Shoelace(prev, cur);
                prev = cur;
            }
        }
        return Sign(total);
    }

    public void Reverse()
    {
        for (int i = _edges.Count / 2; i > 0; --i)
        {
            EdgeHolder t1 = _edges[i - 1];
            EdgeHolder t2 = _edges[_edges.Count - i];
            EdgeHolder.Swap(ref t1, ref t2);
        }
        foreach (var edge in _edges)
            edge.EdgeSegment.Reverse();
    }
}