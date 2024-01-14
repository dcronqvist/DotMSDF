using System;
using System.Collections.Generic;
using System.Linq;
using static DotMSDF.Port.Arithmetics;
using static DotMSDF.Port.Vector2;

namespace DotMSDF.Port;

public static class EdgeColoring
{
    private static bool IsCorner(Vector2 aDir, Vector2 bDir, double crossThreshold)
    {
        return DotProduct(aDir, bDir) <= 0 || Math.Abs(CrossProduct(aDir, bDir)) > crossThreshold;
    }

    private static double EstimateEdgeLength(IEdgeSegment edge)
    {
        double len = 0;
        Point2 prev = edge.Point(0);
        for (int i = 1; i <= Constants.MSDFGEN_EDGE_LENGTH_PRECISION; ++i)
        {
            Point2 cur = edge.Point(1.0 / Constants.MSDFGEN_EDGE_LENGTH_PRECISION * i);
            len += (cur - prev).Length();
            prev = cur;
        }
        return len;
    }

    private static void SwitchColor(ref EdgeColor color, ref int seed, EdgeColor banned = EdgeColor.BLACK)
    {
        EdgeColor combined = color & banned;
        if (combined == EdgeColor.RED || combined == EdgeColor.GREEN || combined == EdgeColor.BLUE)
        {
            color = combined ^ EdgeColor.WHITE;
            return;
        }
        if (color == EdgeColor.BLACK || color == EdgeColor.WHITE)
        {
            EdgeColor[] start = [EdgeColor.CYAN, EdgeColor.MAGENTA, EdgeColor.YELLOW];
            color = start[seed % 3];
            seed /= 3;
            return;
        }
        int shifted = (int)color << (1 + (seed & 1));
        color = (EdgeColor)(shifted | shifted >> 3) & EdgeColor.WHITE;
        seed >>= 1;
    }

    public unsafe static void EdgeColoringSimple(ref Shape shape, double angleThreshold, int seed = 0)
    {
        double crossThreshold = Math.Sin(angleThreshold);
        List<int> corners = [];
        foreach (var contour in shape.Contours)
        {
            // Identify corners
            corners.Clear();
            if (contour.Edges.Count != 0)
            {
                Vector2 prevDirection = contour.Edges.Last().EdgeSegment.Direction(1);
                int index = 0;
                foreach (var edge in contour.Edges)
                {
                    if (IsCorner(prevDirection.Normalize(), edge.EdgeSegment.Direction(0).Normalize(), crossThreshold))
                        corners.Add(index);
                    prevDirection = edge.EdgeSegment.Direction(1);
                    index++;
                }
            }

            // Smooth contour
            if (corners.Count == 0)
                for (int i = 0; i < contour.Edges.Count; i++)
                    contour.Edges[i].EdgeSegment.Color = EdgeColor.WHITE;

            // "Teardrop" case
            else if (corners.Count == 1)
            {
                var colors = stackalloc[] { EdgeColor.WHITE, EdgeColor.WHITE, EdgeColor.BLACK };
                SwitchColor(ref colors[0], ref seed);
                colors[2] = colors[0];
                SwitchColor(ref colors[2], ref seed);
                int corner = corners[0];
                if (contour.Edges.Count >= 3)
                {
                    int m = contour.Edges.Count;
                    for (int i = 0; i < m; ++i)
                        contour.Edges[(corner + i) % m].EdgeSegment.Color = (colors + 1)[(int)(3 + 2.875 * i / (m - 1) - 1.4375 + .5) - 3];
                }
                else if (contour.Edges.Count >= 1)
                {
                    // Less than three edge segments for three colors => edges must be split
                    var parts = new IEdgeSegment[7];
                    contour.Edges[0].EdgeSegment.SplitInThirds(out parts[0 + 3 * corner], out parts[1 + 3 * corner], out parts[2 + 3 * corner]);
                    if (contour.Edges.Count >= 2)
                    {
                        contour.Edges[1].EdgeSegment.SplitInThirds(out parts[3 - 3 * corner], out parts[4 - 3 * corner], out parts[5 - 3 * corner]);
                        parts[0].Color = parts[1].Color = colors[0];
                        parts[2].Color = parts[3].Color = colors[1];
                        parts[4].Color = parts[5].Color = colors[2];
                    }
                    else
                    {
                        parts[0].Color = colors[0];
                        parts[1].Color = colors[1];
                        parts[2].Color = colors[2];
                    }
                    contour.ClearEdges();
                    for (int i = 0; parts[i] != null; ++i)
                        contour.AddEdge(new EdgeHolder(parts[i]));
                }
            }
            // Multiple corners
            else
            {
                int cornerCount = corners.Count;
                int spline = 0;
                int start = corners[0];
                int m = contour.Edges.Count;
                EdgeColor color = EdgeColor.WHITE;
                SwitchColor(ref color, ref seed);
                EdgeColor initialColor = color;
                for (int i = 0; i < m; ++i)
                {
                    int index = (start + i) % m;
                    if (spline + 1 < cornerCount && corners[spline + 1] == index)
                    {
                        ++spline;
                        SwitchColor(ref color, ref seed, (EdgeColor)((spline == cornerCount - 1 ? 1 : 0) * (int)initialColor));
                    }
                    contour.Edges[index].EdgeSegment.Color = color;
                }
            }
        }
    }
}