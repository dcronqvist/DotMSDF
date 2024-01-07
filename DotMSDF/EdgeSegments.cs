using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;

namespace DotMSDF;

internal class Helpers
{
    public static float NonZeroSign(float value) => value < 0 ? -1 : 1;

    public static RectangleF PointBounds(RectangleF bounds, Vector2 point)
    {
        float l = bounds.Left;
        float r = bounds.Right;
        float t = bounds.Top;
        float b = bounds.Bottom;
        if (point.X < bounds.Left) l = point.X;
        if (point.X > bounds.Right) r = point.X;
        if (point.Y < bounds.Top) t = point.Y;
        if (point.Y > bounds.Bottom) b = point.Y;
        return new RectangleF(l, t, r - l, b - t);
    }

    public static IEnumerable<float> SolveQuadratic(float a, float b, float c)
    {
        // a == 0 -> linear equation
        if (a == 0 || MathF.Abs(b) > 1e12 * MathF.Abs(a))
        {
            // a == 0, b == 0 -> no solution
            if (b == 0)
            {
                if (c == 0)
                    yield break;
                yield break;
            }
            yield return -c / b;
            yield break;
        }
        float dscr = b * b - 4 * a * c;
        if (dscr > 0)
        {
            dscr = MathF.Sqrt(dscr);
            yield return (-b + dscr) / (2 * a);
            yield return (-b - dscr) / (2 * a);
            yield break;
        }
        else if (dscr == 0)
        {
            yield return -b / (2 * a);
            yield break;
        }
        else
        {
            yield break;
        }
    }

    public static IEnumerable<float> SolveCubicNormed(float a, float b, float c)
    {
        float a2 = a * a;
        float q = 1 / 9f * (a2 - 3 * b);
        float r = 1 / 54f * (a * (2 * a2 - 9 * b) + 27 * c);
        float r2 = r * r;
        float q3 = q * q * q;
        a *= 1 / 3f;
        if (r2 < q3)
        {
            float t = r / MathF.Abs(q3);
            if (t < -1) t = -1;
            if (t > 1) t = 1;
            t = MathF.Acos(t);
            q = -2 * MathF.Sqrt(q);
            yield return q * MathF.Cos(t / 3f) - a;
            yield return q * MathF.Acos(1 / 3f * (t + 2 * MathF.PI)) - a;
            yield return q * MathF.Acos(1 / 3f * (t - 2 * MathF.PI)) - a;
            yield break;
        }
        else
        {
            float u = (r < 0 ? 1 : -1) * MathF.Pow(MathF.Abs(r) + MathF.Sqrt(r2 - q3), 1 / 3f);
            float v = u == 0 ? 0 : q / u;
            yield return (u + v) - a;
            if (u == v || MathF.Abs(u - v) < 1e-12 * MathF.Abs(u + v))
            {
                yield return -.5f * (u + v) - a;
            }
            yield break;
        }
    }

    public static IEnumerable<float> SolveCubic(float a, float b, float c, float d)
    {
        if (a != 0)
        {
            float bn = b / a;
            if (MathF.Abs(bn) < 1e6) // Above this ratio, the numerical error gets larger than if we treated a as zero
                return SolveCubicNormed(bn, c / a, d / a);
        }
        return SolveQuadratic(b, c, d);
    }
}

public interface IEdgeSegment
{
    static IEdgeSegment Create(Vector2 p0, Vector2 p1, EdgeColor edgeColor) => new LinearSegment(p0, p1, edgeColor);
    static IEdgeSegment Create(Vector2 p0, Vector2 p1, Vector2 p2, EdgeColor edgeColor) => new QuadraticSegment(p0, p1, p2, edgeColor);
    static IEdgeSegment Create(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, EdgeColor edgeColor) => new CubicSegment(p0, p1, p2, p3, edgeColor);

    IEdgeSegment Clone();
    IEnumerable<Vector2> ControlPoints();
    Vector2 Point(float param);
    Vector2 Direction(float param);
    Vector2 DirectionChange(float param);
    SignedDistance SignedDistance(Vector2 origin, ref float param);

    SignedDistance DistanceToPseudoDistance(SignedDistance distance, Vector2 origin, float param)
    {
        if (param < 0.0f)
        {
            Vector2 dir = Vector2.Normalize(Direction(0.0f));
            Vector2 aq = origin - Point(0.0f);
            float ts = Vector2.Dot(aq, dir);
            if (ts < 0.0f)
            {
                float pseudoDistance = aq.Cross(dir);
                if (MathF.Abs(pseudoDistance) < MathF.Abs(distance.Distance))
                {
                    return new SignedDistance(pseudoDistance, 0);
                }
            }
        }
        else if (param > 1.0f)
        {
            Vector2 dir = Vector2.Normalize(Direction(1.0f));
            Vector2 bq = origin - Point(1.0f);
            float ts = Vector2.Dot(bq, dir);
            if (ts > 0.0f)
            {
                float pseudoDistance = bq.Cross(dir);
                if (MathF.Abs(pseudoDistance) < MathF.Abs(distance.Distance))
                {
                    return new SignedDistance(pseudoDistance, 0);
                }
            }
        }

        return distance;
    }

    IEnumerable<Vector2> ScanlineIntersections(float y);
    void Bound(ref RectangleF bounds);

    void Reverse();
    void MoveStartPoint(Vector2 point);
    void MoveEndPoint(Vector2 point);
    (IEdgeSegment, IEdgeSegment, IEdgeSegment) SplitInThirds();
}

public class LinearSegment(Vector2 p0, Vector2 p1, EdgeColor edgeColor) : IEdgeSegment
{
    public Vector2 P0 { get; private set; } = p0;
    public Vector2 P1 { get; private set; } = p1;
    public EdgeColor EdgeColor { get; private set; } = edgeColor;

    public IEdgeSegment Clone() => new LinearSegment(P0, P1, EdgeColor);

    public IEnumerable<Vector2> ControlPoints() => [P0, P1];

    public Vector2 Point(float param) => P0 + (P1 - P0) * param;

    public Vector2 Direction(float param) => P1 - P0;

    public Vector2 DirectionChange(float param) => Vector2.Zero;

    public float Length() => Vector2.Distance(P0, P1);

    public SignedDistance SignedDistance(Vector2 origin, ref float param)
    {
        var aq = origin - P0;
        var ab = P1 - P0;
        param = Vector2.Dot(aq, ab) / Vector2.Dot(ab, ab);
        var eq = ((param > 0.5f) ? P1 : P0) - origin;
        float endPointDistance = eq.Length();
        if (param > 0.0f && param < 1.0f)
        {
            float orthoDistance = Vector2.Dot(ab.GetOrthonormal(), aq);
            if (MathF.Abs(orthoDistance) < endPointDistance)
                return new SignedDistance(orthoDistance, 0);
        }
        return new SignedDistance(Helpers.NonZeroSign(aq.Cross(ab)) * endPointDistance, MathF.Abs(Vector2.Dot(Vector2.Normalize(ab), Vector2.Normalize(eq))));
    }

    public IEnumerable<Vector2> ScanlineIntersections(float y)
    {
        if ((y >= P0.Y && y < P1.Y) || (y >= P1.Y && y < P0.Y))
        {
            float param = (y - P0.Y) / (P1.Y - P0.Y);
            float x0 = P0.X + param * (P1.X - P0.X);
            float dy0 = MathF.Sign(P1.Y - P0.Y);
            yield return new Vector2(x0, dy0);
        }

        yield break;
    }

    public void Bound(ref RectangleF bounds)
    {
        bounds = Helpers.PointBounds(bounds, P0);
        bounds = Helpers.PointBounds(bounds, P1);
    }

    public void Reverse()
    {
        var temp = P0;
        P0 = P1;
        P1 = temp;
    }

    public void MoveStartPoint(Vector2 point) => P0 = point;

    public void MoveEndPoint(Vector2 point) => P1 = point;

    public (IEdgeSegment, IEdgeSegment, IEdgeSegment) SplitInThirds()
    {
        var part0 = new LinearSegment(P0, Point(1f / 3f), EdgeColor);
        var part1 = new LinearSegment(Point(1f / 3f), Point(2f / 3f), EdgeColor);
        var part2 = new LinearSegment(Point(2f / 3f), P1, EdgeColor);
        return (part0, part1, part2);
    }
}

public class QuadraticSegment(Vector2 p0, Vector2 p1, Vector2 p2, EdgeColor edgeColor) : IEdgeSegment
{
    public Vector2 P0 { get; private set; } = p0;
    public Vector2 P1 { get; private set; } = p1;
    public Vector2 P2 { get; private set; } = p2;
    public EdgeColor EdgeColor { get; private set; } = edgeColor;

    public IEdgeSegment Clone() => new QuadraticSegment(P0, P1, P2, EdgeColor);

    public IEnumerable<Vector2> ControlPoints() => [P0, P1, P2];

    public Vector2 Point(float param) =>
        Vector2.Lerp(
            Vector2.Lerp(P0, P1, param),
            Vector2.Lerp(P1, P2, param),
            param);

    public Vector2 Direction(float param)
    {
        var tangent = Vector2.Lerp(P1 - P0, P2 - P1, param);
        if (tangent.X == 0 && tangent.Y == 0)
            return P2 - P0;
        return tangent;
    }

    public Vector2 DirectionChange(float param) => (P2 - P1) - (P1 - P0);

    public float Length()
    {
        var ab = P1 - P0;
        var br = P2 - P1 - ab;
        float abab = Vector2.Dot(ab, ab);
        float abbr = Vector2.Dot(ab, br);
        float brbr = Vector2.Dot(br, br);
        float abLen = MathF.Sqrt(abab);
        float brLen = MathF.Sqrt(brbr);
        float crs = ab.Cross(br);
        float h = MathF.Sqrt(abab + abbr + abbr + brbr);
        return (
            brLen * ((abbr + brbr) * h - abbr * abLen) +
            crs * crs * MathF.Log((brLen * h + abbr + brbr) / (brLen * abLen + abbr))
        ) / (brbr * brLen);
    }

    public SignedDistance SignedDistance(Vector2 origin, ref float param)
    {
        var qa = P0 - origin;
        var ab = P1 - P0;
        var br = P2 - P1 - ab;
        float a = Vector2.Dot(br, br);
        float b = 3 * Vector2.Dot(ab, br);
        float c = 2 * Vector2.Dot(ab, ab) + Vector2.Dot(qa, br);
        float d = Vector2.Dot(qa, ab);
        var solutions = Helpers.SolveCubic(a, b, c, d).ToArray();

        Vector2 epDir = Direction(0);
        float minDistance = Helpers.NonZeroSign(epDir.Cross(qa)) * qa.Length();
        param = -Vector2.Dot(qa, epDir) / Vector2.Dot(epDir, epDir);

        {
            epDir = Direction(1);
            float distance = (P2 - origin).Length();
            if (distance < MathF.Abs(minDistance))
            {
                minDistance = Helpers.NonZeroSign(epDir.Cross(P2 - origin)) * distance;
                param = Vector2.Dot(origin - P1, epDir) / Vector2.Dot(epDir, epDir);
            }
        }

        for (int i = 0; i < solutions.Length; ++i)
        {
            if (solutions[i] > 0 && solutions[i] < 1)
            {
                Vector2 qe = qa + 2 * solutions[i] * ab + solutions[i] * solutions[i] * br;
                float distance = qe.Length();
                if (distance <= MathF.Abs(minDistance))
                {
                    minDistance = Helpers.NonZeroSign((ab + solutions[i] * br).Cross(qe)) * distance;
                    param = solutions[i];
                }
            }
        }

        if (param >= 0 && param <= 1)
            return new SignedDistance(minDistance, 0);
        if (param < 0.5f)
            return new SignedDistance(minDistance, MathF.Abs(Vector2.Dot(Vector2.Normalize(Direction(0)), Vector2.Normalize(qa))));
        else
            return new SignedDistance(minDistance, MathF.Abs(Vector2.Dot(Vector2.Normalize(Direction(1)), Vector2.Normalize(P2 - origin))));
    }

    public IEnumerable<Vector2> ScanlineIntersections(float y)
    {
        float[] x = new float[3];
        float[] dy = new float[3];

        Vector2[] p = [P0, P1, P2];

        int total = 0;
        int nextDY = y > p[0].Y ? 1 : -1;
        x[total] = p[0].X;
        if (p[0].Y == y)
        {
            if (p[0].Y < p[1].Y || (p[0].Y == p[1].Y && p[0].Y < p[2].Y))
                dy[total++] = 1;
            else
                nextDY = 1;
        }
        {
            Vector2 ab = p[1] - p[0];
            Vector2 br = p[2] - p[1] - ab;
            float[] t = new float[2];
            var solutions = Helpers.SolveQuadratic(br.Y, 2 * ab.Y, p[0].Y - y).ToArray();
            t[0] = solutions[0];
            t[1] = solutions[1];
            // Sort solutions
            float tmp;
            if (solutions.Length >= 2 && t[0] > t[1])
            {
                tmp = t[0];
                t[0] = t[1];
                t[1] = tmp;
            }
            for (int i = 0; i < solutions.Length && total < 2; ++i)
            {
                if (t[i] >= 0 && t[i] <= 1)
                {
                    x[total] = p[0].X + 2 * t[i] * ab.X + t[i] * t[i] * br.X;
                    if (nextDY * (ab.Y + t[i] * br.Y) >= 0)
                    {
                        dy[total++] = nextDY;
                        nextDY = -nextDY;
                    }
                }
            }
        }
        if (p[2].Y == y)
        {
            if (nextDY > 0 && total > 0)
            {
                --total;
                nextDY = -1;
            }
            if ((p[2].Y < p[1].Y || (p[2].Y == p[1].Y && p[2].Y < p[0].Y)) && total < 2)
            {
                x[total] = p[2].X;
                if (nextDY < 0)
                {
                    dy[total++] = -1;
                    nextDY = 1;
                }
            }
        }
        if (nextDY != (y >= p[2].Y ? 1 : -1))
        {
            if (total > 0)
                --total;
            else
            {
                if (MathF.Abs(p[2].Y - y) < MathF.Abs(p[0].Y - y))
                    x[total] = p[2].X;
                dy[total++] = nextDY;
            }
        }
        return Enumerable.Repeat(0, total).Select(i => new Vector2(x[i], dy[i]));
    }

    public void Bound(ref RectangleF bounds)
    {
        bounds = Helpers.PointBounds(bounds, P0);
        bounds = Helpers.PointBounds(bounds, P2);
        Vector2 bot = (P1 - P0) - (P2 - P1);
        if (bot.X != 0)
        {
            float param = (P1.X - P0.X) / bot.X;
            if (param > 0 && param < 1)
                bounds = Helpers.PointBounds(bounds, Point(param));
        }
        if (bot.Y != 0)
        {
            float param = (P1.Y - P0.Y) / bot.Y;
            if (param > 0 && param < 1)
                bounds = Helpers.PointBounds(bounds, Point(param));
        }
    }

    public void Reverse()
    {
        var temp = P0;
        P0 = P2;
        P2 = temp;
    }

    public void MoveStartPoint(Vector2 point)
    {
        Vector2 origSDir = P0 - P1;
        Vector2 origP1 = P1;
        P1 += (P0 - P1).Cross(point - P0) / (P0 - P1).Cross(P2 - P1) * (P2 - P1);
        P0 = point;
        if (Vector2.Dot(origSDir, P0 - P1) < 0)
        {
            P1 = origP1;
        }
    }

    public void MoveEndPoint(Vector2 point)
    {
        Vector2 origEDir = P2 - P1;
        Vector2 origP1 = P1;
        P1 += (P2 - P1).Cross(point - P2) / (P2 - P1).Cross(P0 - P1) * (P0 - P1);
        P2 = point;
        if (Vector2.Dot(origEDir, P2 - P1) < 0)
        {
            P1 = origP1;
        }
    }

    public (IEdgeSegment, IEdgeSegment, IEdgeSegment) SplitInThirds()
    {
        var part0 = new QuadraticSegment(P0, Vector2.Lerp(P0, P1, 1f / 3f), Point(1 / 3f), EdgeColor);
        var part1 = new QuadraticSegment(Point(1 / 3f), Vector2.Lerp(Vector2.Lerp(P0, P1, 5f / 9f), Vector2.Lerp(P1, P2, 4f / 9f), 0.5f), Point(2f / 3f), EdgeColor);
        var part2 = new QuadraticSegment(Point(2f / 3f), Vector2.Lerp(P1, P2, 2f / 3f), P2, EdgeColor);
        return (part0, part1, part2);
    }
}

public class CubicSegment(Vector2 P0, Vector2 P1, Vector2 P2, Vector2 P3, EdgeColor edgeColor) : IEdgeSegment
{
    public Vector2 P0 { get; private set; } = P0;
    public Vector2 P1 { get; private set; } = P1;
    public Vector2 P2 { get; private set; } = P2;
    public Vector2 P3 { get; private set; } = P3;
    public EdgeColor EdgeColor { get; private set; } = edgeColor;

    private Vector2[] p => [P0, P1, P2, P3];

    public IEdgeSegment Clone() => new CubicSegment(P0, P1, P2, P3, EdgeColor);

    public IEnumerable<Vector2> ControlPoints() => [P0, P1, P2, P3];

    public Vector2 Point(float param) =>
        Vector2.Lerp(
            Vector2.Lerp(
                Vector2.Lerp(P0, P1, param),
                Vector2.Lerp(P1, P2, param),
                param),
            Vector2.Lerp(
                Vector2.Lerp(P1, P2, param),
                Vector2.Lerp(P2, P3, param),
                param),
            param);

    public Vector2 Direction(float param)
    {
        Vector2 tangent = Vector2.Lerp(Vector2.Lerp(p[1] - p[0], p[2] - p[1], param), Vector2.Lerp(p[2] - p[1], p[3] - p[2], param), param);
        if (tangent.X == 0 && tangent.Y == 0)
        {
            if (param == 0) return p[2] - p[0];
            if (param == 1) return p[3] - p[1];
        }
        return tangent;
    }

    public Vector2 DirectionChange(float param)
    {
        return Vector2.Lerp((p[2] - p[1]) - (p[1] - p[0]), (p[3] - p[2]) - (p[2] - p[1]), param);
    }

    public SignedDistance SignedDistance(Vector2 origin, ref float param)
    {
        Vector2 qa = p[0] - origin;
        Vector2 ab = p[1] - p[0];
        Vector2 br = p[2] - p[1] - ab;
        Vector2 @as = (p[3] - p[2]) - (p[2] - p[1]) - br;

        Vector2 epDir = Direction(0);
        float minDistance = Helpers.NonZeroSign(epDir.Cross(qa)) * qa.Length(); // distance from A
        param = -Vector2.Dot(qa, epDir) / Vector2.Dot(epDir, epDir);
        {
            epDir = Direction(1);
            float distance = (p[3] - origin).Length(); // distance from B
            if (distance < MathF.Abs(minDistance))
            {
                minDistance = Helpers.NonZeroSign(epDir.Cross(p[3] - origin)) * distance;
                param = Vector2.Dot(epDir - (p[3] - origin), epDir) / Vector2.Dot(epDir, epDir);
            }
        }
        // Iterative minimum distance search
        var msdfgen_cubic_search_starts = 4;
        var msdfgen_cubic_search_steps = 4;

        for (int i = 0; i <= msdfgen_cubic_search_starts; ++i)
        {
            float t = (float)i / msdfgen_cubic_search_starts;
            Vector2 qe = qa + 3 * t * ab + 3 * t * t * br + t * t * t * @as;
            for (int step = 0; step < msdfgen_cubic_search_steps; ++step)
            {
                // Improve t
                Vector2 d1 = 3 * ab + 6 * t * br + 3 * t * t * @as;
                Vector2 d2 = 6 * br + 6 * t * @as;
                t -= Vector2.Dot(qe, d1) / (Vector2.Dot(d1, d1) + Vector2.Dot(qe, d2));
                if (t <= 0 || t >= 1)
                    break;
                qe = qa + 3 * t * ab + 3 * t * t * br + t * t * t * @as;
                float distance = qe.Length();
                if (distance < MathF.Abs(minDistance))
                {
                    minDistance = Helpers.NonZeroSign(Vector2.Dot(d1, qe)) * distance;
                    param = t;
                }
            }
        }

        if (param >= 0 && param <= 1)
            return new SignedDistance(minDistance, 0);
        if (param < .5f)
            return new SignedDistance(minDistance, MathF.Abs(Vector2.Dot(Vector2.Normalize(Direction(0)), Vector2.Normalize(qa))));
        else
            return new SignedDistance(minDistance, MathF.Abs(Vector2.Dot(Vector2.Normalize(Direction(1)), Vector2.Normalize((p[3] - origin)))));
    }

    public IEnumerable<Vector2> ScanlineIntersections(float y)
    {
        float[] x = new float[3];
        float[] dy = new float[3];

        int total = 0;
        int nextDY = y > p[0].Y ? 1 : -1;
        x[total] = p[0].Y;
        if (p[0].Y == y)
        {
            if (p[0].Y < p[1].Y || (p[0].Y == p[1].Y && (p[0].Y < p[2].Y || (p[0].Y == p[2].Y && p[0].Y < p[3].Y))))
                dy[total++] = 1;
            else
                nextDY = 1;
        }
        {
            Vector2 ab = p[1] - p[0];
            Vector2 br = p[2] - p[1] - ab;
            Vector2 @as = (p[3] - p[2]) - (p[2] - p[1]) - br;
            var t = Helpers.SolveCubic(@as.Y, 3 * br.Y, 3 * ab.Y, p[0].Y - y).ToArray();
            int solutions = t.Length;
            // Sort solutions
            float tmp;
            if (solutions >= 2)
            {
                if (t[0] > t[1])
                {
                    tmp = t[0];
                    t[0] = t[1];
                    t[1] = tmp;
                }
                if (solutions >= 3 && t[1] > t[2])
                {
                    tmp = t[1];
                    t[1] = t[2];
                    t[2] = tmp;
                    if (t[0] > t[1])
                    {
                        tmp = t[0];
                        t[0] = t[1];
                        t[1] = tmp;
                    }
                }
            }
            for (int i = 0; i < solutions && total < 3; ++i)
            {
                if (t[i] >= 0 && t[i] <= 1)
                {
                    x[total] = p[0].Y + 3 * t[i] * ab.X + 3 * t[i] * t[i] * br.X + t[i] * t[i] * t[i] * @as.X;
                    if (nextDY * (ab.Y + 2 * t[i] * br.Y + t[i] * t[i] * @as.Y) >= 0)
                    {
                        dy[total++] = nextDY;
                        nextDY = -nextDY;
                    }
                }
            }
        }
        if (p[3].Y == y)
        {
            if (nextDY > 0 && total > 0)
            {
                --total;
                nextDY = -1;
            }
            if ((p[3].Y < p[2].Y || (p[3].Y == p[2].Y && (p[3].Y < p[1].Y || (p[3].Y == p[1].Y && p[3].Y < p[0].Y)))) && total < 3)
            {
                x[total] = p[3].X;
                if (nextDY < 0)
                {
                    dy[total++] = -1;
                    nextDY = 1;
                }
            }
        }
        if (nextDY != (y >= p[3].Y ? 1 : -1))
        {
            if (total > 0)
                --total;
            else
            {
                if (MathF.Abs(p[3].Y - y) < MathF.Abs(p[0].Y - y))
                    x[total] = p[3].X;
                dy[total++] = nextDY;
            }
        }
        return Enumerable.Repeat(0, total).Select(i => new Vector2(x[i], dy[i]));
    }

    public void Bound(ref RectangleF bounds)
    {
        bounds = Helpers.PointBounds(bounds, P0);
        bounds = Helpers.PointBounds(bounds, P3);
        Vector2 a0 = p[1] - p[0];
        Vector2 a1 = 2 * (p[2] - p[1] - a0);
        Vector2 a2 = p[3] - 3 * p[2] + 3 * p[1] - p[0];
        float[] @params = Helpers.SolveQuadratic(a2.X, a1.X, a0.X).ToArray();
        int solutions = @params.Length;
        for (int i = 0; i < solutions; ++i)
            if (@params[i] > 0 && @params[i] < 1)
                bounds = Helpers.PointBounds(bounds, Point(@params[i]));
        @params = Helpers.SolveQuadratic(a2.Y, a1.Y, a0.Y).ToArray();
        solutions = @params.Length;
        for (int i = 0; i < solutions; ++i)
            if (@params[i] > 0 && @params[i] < 1)
                bounds = Helpers.PointBounds(bounds, Point(@params[i]));
    }

    public void Reverse()
    {
        var temp = P0;
        P0 = P3;
        P3 = temp;
        temp = P1;
        P1 = P2;
        P2 = temp;
    }

    public void MoveStartPoint(Vector2 point)
    {
        P1 += (point - P0);
        P0 = point;
    }

    public void MoveEndPoint(Vector2 point)
    {
        P2 += (point - P3);
        P3 = point;
    }

    public (IEdgeSegment, IEdgeSegment, IEdgeSegment) SplitInThirds()
    {
        var part0 = new CubicSegment(p[0], p[0] == p[1] ? p[0] : Vector2.Lerp(p[0], p[1], 1 / 3f), Vector2.Lerp(Vector2.Lerp(p[0], p[1], 1 / 3f), Vector2.Lerp(p[1], p[2], 1 / 3f), 1 / 3f), Point(1 / 3f), EdgeColor);
        var part1 = new CubicSegment(Point(1 / 3f),
            Vector2.Lerp(Vector2.Lerp(Vector2.Lerp(p[0], p[1], 1 / 3f), Vector2.Lerp(p[1], p[2], 1 / 3f), 1 / 3f), Vector2.Lerp(Vector2.Lerp(p[1], p[2], 1 / 3f), Vector2.Lerp(p[2], p[3], 1 / 3f), 1 / 3f), 2 / 3f),
            Vector2.Lerp(Vector2.Lerp(Vector2.Lerp(p[0], p[1], 2 / 3f), Vector2.Lerp(p[1], p[2], 2 / 3f), 2 / 3f), Vector2.Lerp(Vector2.Lerp(p[1], p[2], 2 / 3f), Vector2.Lerp(p[2], p[3], 2 / 3f), 2 / 3f), 1 / 3f),
            Point(2 / 3f), EdgeColor);
        var part2 = new CubicSegment(Point(2 / 3f), Vector2.Lerp(Vector2.Lerp(p[1], p[2], 2 / 3f), Vector2.Lerp(p[2], p[3], 2 / 3f), 2 / 3f), p[2] == p[3] ? p[3] : Vector2.Lerp(p[2], p[3], 2 / 3f), p[3], EdgeColor);
        return (part0, part1, part2);
    }

    public void Deconverge(int param, float amount)
    {
        Vector2 dir = Direction(param);
        Vector2 normal = dir.GetOrthonormal();
        float h = Vector2.Dot(DirectionChange(param) - dir, normal);
        switch (param)
        {
            case 0:
                P1 += amount * (dir + MathF.Sign(h) * MathF.Sqrt(MathF.Abs(h)) * normal);
                break;

            case 1:
                P2 -= amount * (dir - MathF.Sign(h) * MathF.Sqrt(MathF.Abs(h)) * normal);
                break;
        }
    }
}