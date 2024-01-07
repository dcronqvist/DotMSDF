using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace DotMSDF;

public class Contour
{
    private List<IEdgeSegment> _edges = [];
    public IEnumerable<IEdgeSegment> Edges => _edges;

    public Contour(IEnumerable<IEdgeSegment> edges)
    {
        _edges = edges.ToList();
    }

    public void AddEdge(IEdgeSegment edge)
    {
        _edges.Add(edge);
    }

    public void Bound(ref RectangleF bounds)
    {
        foreach (var edge in _edges)
        {
            edge.Bound(ref bounds);
        }
    }

    public void BoundMiters(ref RectangleF bounds, float border, float miterLimit, int polarity)
    {
        if (!_edges.Any())
            return;

        Vector2 prevDir = Vector2.Normalize(_edges.Last().Direction(1));
        foreach (var edge in _edges)
        {
            var dir = -Vector2.Normalize(edge.Direction(0));
            if (polarity * prevDir.Cross(dir) >= 0)
            {
                float miterLength = miterLimit;
                float q = .5f * (1 - Vector2.Dot(prevDir, dir));
                if (q > 0)
                    miterLength = MathF.Min(1f / MathF.Sqrt(q), miterLimit);
                Vector2 miter = edge.Point(0) + border * miterLength * Vector2.Normalize(prevDir + dir);
                bounds = Helpers.PointBounds(bounds, miter);
            }
            prevDir = Vector2.Normalize(edge.Direction(1));
        }
    }

    private float Shoelace(Vector2 a, Vector2 b)
    {
        return (b.X - a.X) * (a.Y + b.Y);
    }

    public int Winding()
    {
        if (!_edges.Any())
            return 0;

        float total = 0f;
        if (_edges.Count == 1)
        {
            Vector2 a = _edges.First().Point(0), b = _edges.First().Point(1 / 3f), c = _edges.First().Point(2 / 3f);
            total += Shoelace(a, b);
            total += Shoelace(b, c);
            total += Shoelace(c, a);
        }
        else if (_edges.Count == 2)
        {
            Vector2 a = _edges.First().Point(0), b = _edges.First().Point(.5f), c = _edges[1].Point(0), d = _edges[1].Point(.5f);
            total += Shoelace(a, b);
            total += Shoelace(b, c);
            total += Shoelace(c, d);
            total += Shoelace(d, a);
        }
        else
        {
            Vector2 prev = _edges.Last().Point(0);
            foreach (var edge in _edges)
            {
                Vector2 cur = edge.Point(0);
                total += Shoelace(prev, cur);
                prev = cur;
            }
        }
        return MathF.Sign(total);
    }

    public void Reverse()
    {
        _edges.Reverse();
        foreach (var edge in _edges)
        {
            edge.Reverse();
        }
    }
}