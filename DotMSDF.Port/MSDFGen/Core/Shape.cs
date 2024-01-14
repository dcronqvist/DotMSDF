using System;
using System.Collections.Generic;
using System.Linq;
using static DotMSDF.Port.Arithmetics;
using static DotMSDF.Port.Vector2;

namespace DotMSDF.Port;

public partial class Constants
{
    public const double MSDFGEN_CORNER_DOT_EPSILON = 0.000001;
    public const double MSDFGEN_DECONVERGENCE_FACTOR = 0.000001;
}

public class Shape
{
    public struct Bounds
    {
        public double l, b, r, t;
    }

    public List<Contour> Contours { get; set; } = [];
    public bool InverseYAxis { get; set; }

    public Shape()
    {
        InverseYAxis = false;
    }

    public void AddContour(Contour contour)
    {
        Contours.Add(contour);
    }

    public Contour AddContour()
    {
        var contour = new Contour();
        AddContour(contour);
        return contour;
    }

    public bool Validate()
    {
        foreach (var contour in Contours)
        {
            if (contour.Edges.Count != 0)
            {
                Point2 corner = contour.Edges.Last().EdgeSegment.Point(1);
                foreach (var edge in contour.Edges)
                {
                    if (edge.EdgeSegment == null)
                        return false;
                    if (edge.EdgeSegment.Point(0) != corner)
                        return false;
                    corner = edge.EdgeSegment.Point(1);
                }
            }
        }
        return true;
    }

    private static void DeconvergeEdge(ref EdgeHolder edgeHolder, int param)
    {
        IEdgeSegment edgeSegment = edgeHolder.EdgeSegment;

        switch (edgeSegment)
        {
            case QuadraticSegment when edgeSegment is QuadraticSegment qs:
                edgeHolder.EdgeSegment = qs.ConvertToCubic();
                (edgeHolder.EdgeSegment as CubicSegment)!.Deconverge(param, Constants.MSDFGEN_DECONVERGENCE_FACTOR);
                break;

            case CubicSegment when edgeSegment is CubicSegment cs:
                cs.Deconverge(param, Constants.MSDFGEN_DECONVERGENCE_FACTOR);
                break;
        }
    }

    public void Normalize()
    {
        for (int i = 0; i < Contours.Count; i++)
        {
            var contour = Contours[i];

            if (contour.Edges.Count == 1)
            {
                contour.Edges[0].EdgeSegment.SplitInThirds(out var part0, out var part1, out var part2);
                contour.ClearEdges();
                contour.AddEdge(new EdgeHolder(part0));
                contour.AddEdge(new EdgeHolder(part1));
                contour.AddEdge(new EdgeHolder(part2));
            }
            else
            {
                EdgeHolder prevEdge = contour.Edges.Last();
                foreach (var edge in contour.Edges)
                {
                    Vector2 prevDir = prevEdge.EdgeSegment.Direction(1).Normalize();
                    Vector2 curDir = edge.EdgeSegment.Direction(0).Normalize();
                    var e = edge;
                    if (DotProduct(prevDir, curDir) < Constants.MSDFGEN_CORNER_DOT_EPSILON - 1)
                    {
                        DeconvergeEdge(ref prevEdge, 1);
                        DeconvergeEdge(ref e, 0);
                    }
                    prevEdge = e;
                }
            }
        }
    }

    public void Bound(ref double l, ref double b, ref double r, ref double t)
    {
        foreach (var contour in Contours)
            contour.Bound(ref l, ref b, ref r, ref t);
    }

    public void BoundMiters(ref double l, ref double b, ref double r, ref double t, double border, double miterLimit, int polarity)
    {
        foreach (var contour in Contours)
            contour.BoundMiters(ref l, ref b, ref r, ref t, border, miterLimit, polarity);
    }

    private const double LARGE_VALUE = 1e240;
    public Bounds GetBounds(double border, double miterLimit, int polarity)
    {
        Bounds bounds = new()
        {
            l = +LARGE_VALUE,
            b = +LARGE_VALUE,
            r = -LARGE_VALUE,
            t = -LARGE_VALUE
        };
        Bound(ref bounds.l, ref bounds.b, ref bounds.r, ref bounds.t);
        if (border > 0)
        {
            bounds.l -= border;
            bounds.b -= border;
            bounds.r += border;
            bounds.t += border;
            if (miterLimit > 0)
                BoundMiters(ref bounds.l, ref bounds.b, ref bounds.r, ref bounds.t, border, miterLimit, polarity);
        }
        return bounds;
    }

    public void Scanline(ref Scanline line, double y)
    {
        List<Scanline.Intersection> intersections = [];
        foreach (var contour in Contours)
        {
            foreach (var edge in contour.Edges)
            {
                int n = edge.EdgeSegment.ScanlineIntersections(out var x, out var dy, y);
                for (int i = 0; i < n; ++i)
                {
                    Scanline.Intersection intersection = new Scanline.Intersection
                    {
                        X = x[i],
                        Direction = dy[i]
                    };
                    intersections.Add(intersection);
                }
            }
        }
        line.SetIntersections(intersections);
    }

    public int EdgeCount()
    {
        int total = 0;
        foreach (var contour in Contours)
            total += contour.Edges.Count;
        return total;
    }

    struct OrientIntersection
    {
        public double X { get; set; }
        public int Direction { get; set; }
        public int ContourIndex { get; set; }

        public static int Compare(OrientIntersection a, OrientIntersection b) => Sign(a.X - b.X);

        public void SetX(double x) => X = x;
        public void SetDirection(int direction) => Direction = direction;
        public void SetContourIndex(int contourIndex) => ContourIndex = contourIndex;
    }

    public void OrientContours()
    {
        double ratio = .5 * (Math.Sqrt(5) - 1); // an irrational number to minimize chance of intersecting a corner or other point of interest
        List<int> orientations = [];
        List<OrientIntersection> intersections = [];
        for (int i = 0; i < Contours.Count; ++i)
        {
            if (orientations[i] != 0 && Contours[i].Edges.Count != 0)
            {
                // Find an Y that crosses the contour
                double y0 = Contours[i].Edges.First().EdgeSegment.Point(0).Y;
                double y1 = y0;
                for (int j = 0; j < Contours[i].Edges.Count && y0 == y1; ++j)
                    y1 = Contours[i].Edges[j].EdgeSegment.Point(1).Y;
                for (int j = 0; j < Contours[i].Edges.Count && y0 == y1; ++j)
                    y1 = Contours[i].Edges[j].EdgeSegment.Point(ratio).Y; // in case all endpoints are in a horizontal line
                double y = Mix(y0, y1, ratio);
                // Scanline through whole shape at Y
                for (int j = 0; j < Contours.Count; ++j)
                {
                    foreach (var edge in Contours[j].Edges)
                    {
                        int n = edge.EdgeSegment.ScanlineIntersections(out var x, out var dy, y);
                        for (int k = 0; k < n; ++k)
                        {
                            OrientIntersection intersection = new OrientIntersection
                            {
                                X = x[k],
                                Direction = dy[k],
                                ContourIndex = j
                            };
                            intersections.Add(intersection);
                        }
                    }
                }
                if (intersections.Count > 0)
                {
                    intersections.Sort(OrientIntersection.Compare);
                    // Disqualify multiple intersections
                    for (int j = 1; j < intersections.Count; ++j)
                    {
                        if (intersections[j].X == intersections[j - 1].X)
                        {
                            intersections[j - 1].SetDirection(0);
                            intersections[j].SetDirection(0);
                        }
                    }
                    // Inspect scanline and deduce orientations of intersected contours
                    for (int j = 0; j < intersections.Count; ++j)
                    {
                        if (intersections[j].Direction != 0)
                        {
                            orientations[intersections[j].ContourIndex] += 2 * ((j & 1) ^ (intersections[j].Direction > 0 ? 1 : 0)) - 1;
                        }
                    }
                    intersections.Clear();
                }
            }
        }
        // Reverse contours that have the opposite orientation
        for (int i = 0; i < Contours.Count; ++i)
            if (orientations[i] < 0)
                Contours[i].Reverse();
    }
}