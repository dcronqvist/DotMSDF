using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;

namespace DotMSDF;

public class Shape
{
    public RectangleF Bounds { get; set; }
    private List<Contour> _contours = [];
    public IEnumerable<Contour> Contours => _contours;
    public bool InverseAxis { get; set; }

    public void AddContour(Contour contour)
    {
        _contours.Add(contour);
    }

    public Contour AddContour()
    {
        var contour = new Contour([]);
        _contours.Add(contour);
        return contour;
    }

    public void Normalize()
    {
        foreach (var contour in _contours)
        {
            if (contour.Edges.Count() == 1)
            {
                IEdgeSegment[] parts = new IEdgeSegment[3];

                var (part1, part2, part3) = contour.Edges.First().SplitInThirds();

                contour.ClearEdges();
                contour.AddEdge(part1);
                contour.AddEdge(part2);
                contour.AddEdge(part3);
            }
            else
            {
                IEdgeSegment prevEdge = contour.Edges.Last();
                for (int i = 0; i < contour.Edges.Count(); i++)
                {
                    var edge = contour.Edges.ElementAt(i);
                    Vector2 prevDir = Vector2.Normalize(prevEdge.Direction(1));
                    Vector2 curDir = Vector2.Normalize(edge.Direction(0));
                    if (Vector2.Dot(prevDir, curDir) < .000001f - 1)
                    {
                        DeconvergeEdge(ref prevEdge, 1);
                        DeconvergeEdge(ref edge, 0);
                    }
                    prevEdge = edge;
                }
            }
        }
    }

    public bool Validate()
    {
        foreach (var contour in _contours)
        {
            if (contour.Edges.Any())
            {
                Vector2 corner = contour.Edges.Last().Point(1);
                foreach (var edge in contour.Edges)
                {
                    if (edge.Point(0) != corner)
                        return false;
                    corner = edge.Point(1);
                }
            }
        }
        return true;
    }

    public static void DeconvergeEdge(ref IEdgeSegment edge, int param)
    {
        switch (edge)
        {
            case QuadraticSegment qudratic:
                edge = qudratic.ConvertToCubic();
                (edge as CubicSegment).Deconverge(param, .000001f);
                break;

            case CubicSegment cubic:
                cubic.Deconverge(param, .000001f);
                break;
        }
    }

    public void Bound(ref RectangleF bounds)
    {
        foreach (var contour in _contours)
            contour.Bound(ref bounds);
    }

    public void BoundMiters(ref RectangleF bounds, float border, float miterLimit, int polarity)
    {
        foreach (var contour in _contours)
            contour.BoundMiters(ref bounds, border, miterLimit, polarity);
    }

    const float LARGE_VALUE = float.MaxValue;
    public RectangleF GetBounds(float border = 0, float miterLimit = 0, int polarity = 0)
    {
        float l = +LARGE_VALUE, b = +LARGE_VALUE, r = -LARGE_VALUE, t = -LARGE_VALUE;

        RectangleF bounds = RectangleF.FromLTRB(l, b, r, t);
        Bound(ref bounds);
        if (border > 0)
        {
            l -= border;
            b -= border;
            r += border;
            t += border;
            bounds = RectangleF.FromLTRB(l, b, r, t);
            if (miterLimit > 0)
                BoundMiters(ref bounds, border, miterLimit, polarity);
        }
        return bounds;
    }

    public void Scanline(ref Scanline line, float y)
    {
        List<Intersection> intersections = [];
        foreach (var contour in _contours)
        {
            foreach (var edge in contour.Edges)
            {
                var ints = edge.ScanlineIntersections(y).ToArray();
                int n = ints.Length;
                for (int i = 0; i < n; ++i)
                {
                    var intersection = new Intersection(ints[i].X, (int)ints[i].Y);
                    intersections.Add(intersection);
                }
            }
        }
        line.SetIntersections(intersections);
    }

    public int EdgeCount()
    {
        return _contours.Sum(contour => contour.Edges.Count());
    }

    private record ShapeIntersection(float X, int Direction, int ContourIndex)
    {
        public static int Compare(ShapeIntersection a, ShapeIntersection b)
        {
            return MathF.Sign(a.X - b.X);
        }
    }

    public void OrientContours()
    {
        float ratio = .5f * (MathF.Sqrt(5) - 1); // an irrational number to minimize chance of intersecting a corner or other point of interest
        List<int> orientations = new List<int>(_contours.Count);
        List<ShapeIntersection> intersections = [];
        for (int i = 0; i < _contours.Count; ++i)
        {
            if (orientations[i] == 0 && _contours[i].Edges.Any())
            {
                // Find an Y that crosses the contour
                float y0 = _contours[i].Edges.First().Point(0).Y;
                float y1 = y0;
                for (int j = 0; j < _contours[i].Edges.Count() && y0 == y1; ++j)
                    y1 = _contours[i].Edges.ElementAt(j).Point(1).Y;
                for (int j = 0; j < _contours[i].Edges.Count() && y0 == y1; ++j)
                    y1 = _contours[i].Edges.ElementAt(j).Point(ratio).Y; // in case all endpoints are in a horizontal line
                float y = float.Lerp(y0, y1, ratio);
                // Scanline through whole shape at Y
                float[] x = new float[3];
                int[] dy = new int[3];
                for (int j = 0; j < _contours.Count; ++j)
                {
                    foreach (var edge in _contours[j].Edges)
                    {
                        var ints = edge.ScanlineIntersections(y).ToArray();
                        int n = ints.Length;
                        for (int k = 0; k < n; ++k)
                        {
                            var intersection = new ShapeIntersection(ints[k].X, (int)ints[k].Y, j);
                            intersections.Add(intersection);
                        }
                    }
                }
                if (intersections.Any())
                {
                    intersections.Sort(ShapeIntersection.Compare);
                    // Disqualify multiple intersections
                    for (int j = 1; j < intersections.Count; ++j)
                        if (intersections[j].X == intersections[j - 1].X)
                        {
                            intersections[j] = intersections[j] with { Direction = 0 };
                            intersections[j - 1] = intersections[j - 1] with { Direction = 0 };
                        }
                    // Inspect scanline and deduce orientations of intersected contours
                    for (int j = 0; j < intersections.Count; ++j)
                        if (intersections[j].Direction != 0)
                            orientations[intersections[j].ContourIndex] += 2 * ((j & 1) ^ (intersections[j].Direction > 0 ? 1 : 0)) - 1;
                    intersections.Clear();
                }
            }
        }
        // Reverse contours that have the opposite orientation
        for (int i = 0; i < _contours.Count; ++i)
            if (orientations[i] < 0)
                _contours[i].Reverse();
    }
}