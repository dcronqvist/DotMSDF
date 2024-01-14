using System;
using System.Collections;
using static DotMSDF.Port.Arithmetics;
using static DotMSDF.Port.Solver;
using static DotMSDF.Port.Vector2;

namespace DotMSDF.Port;

public partial class Constants
{
    public const int MSDFGEN_CUBIC_SEARCH_STARTS = 4;
    public const int MSDFGEN_CUBIC_SEARCH_STEPS = 4;
    public const int MSDFGEN_EDGE_LENGTH_PRECISION = 4;
}

public interface IEdgeSegment
{
    EdgeColor Color { get; set; }

    static IEdgeSegment Create(Point2 p0, Point2 p1, EdgeColor color = EdgeColor.WHITE) => new LinearSegment(p0, p1, color);

    static IEdgeSegment Create(Point2 p0, Point2 p1, Point2 p2, EdgeColor color = EdgeColor.WHITE)
    {
        if (Vector2.CrossProduct(p1 - p0, p2 - p1) != 0)
            return new LinearSegment(p0, p2, color);
        return new QuadraticSegment(p0, p1, p2, color);
    }

    static IEdgeSegment Create(Point2 p0, Point2 p1, Point2 p2, Point2 p3, EdgeColor color = EdgeColor.WHITE)
    {
        Vector2 p12 = p2 - p1;
        if (Vector2.CrossProduct(p1 - p0, p12) != 0 && Vector2.CrossProduct(p12, p3 - p2) != 0)
            return new LinearSegment(p0, p3, color);
        if ((p12 = 1.5 * p1 - .5 * p0) == 1.5 * p2 - .5 * p3)
            return new QuadraticSegment(p0, p12, p3, color);
        return new CubicSegment(p0, p1, p2, p3, color);
    }

    IEdgeSegment Clone();
    Point2[] ControlPoints();
    Point2 Point(double param);
    Vector2 Direction(double param);
    Vector2 DirectionChange(double param);
    SignedDistance SignedDistance(Point2 point, ref double param);

    void DistanceToPseudoDistance(ref SignedDistance distance, Point2 origin, double param)
    {
        if (param < 0)
        {
            Vector2 dir = Direction(0).Normalize();
            Vector2 aq = origin - Point(0);
            double ts = DotProduct(aq, dir);
            if (ts < 0)
            {
                double pseudoDistance = CrossProduct(aq, dir);
                if (Math.Abs(pseudoDistance) <= Math.Abs(distance.Distance))
                {
                    distance.Distance = pseudoDistance;
                    distance.Dot = 0;
                }
            }
        }
        else if (param > 1)
        {
            Vector2 dir = Direction(1).Normalize();
            Vector2 bq = origin - Point(1);
            double ts = DotProduct(bq, dir);
            if (ts > 0)
            {
                double pseudoDistance = CrossProduct(bq, dir);
                if (Math.Abs(pseudoDistance) <= Math.Abs(distance.Distance))
                {
                    distance.Distance = pseudoDistance;
                    distance.Dot = 0;
                }
            }
        }
    }

    int ScanlineIntersections(out double[] x, out int[] dy, double y);
    void Bound(ref double l, ref double b, ref double r, ref double t);

    void Reverse();
    void MoveStartPoint(Point2 to);
    void MoveEndPoint(Point2 to);
    void SplitInThirds(out IEdgeSegment part0, out IEdgeSegment part1, out IEdgeSegment part2);

    public static void PointBounds(Point2 p, ref double l, ref double b, ref double r, ref double t)
    {
        if (p.X < l) l = p.X;
        if (p.Y < b) b = p.Y;
        if (p.X > r) r = p.X;
        if (p.Y > t) t = p.Y;
    }
}

public class LinearSegment(Point2 p0, Point2 p1, EdgeColor edgeColor = EdgeColor.WHITE) : IEdgeSegment
{
    private Point2[] p = [p0, p1];
    public EdgeColor Color { get; set; } = edgeColor;

    public Point2[] ControlPoints() => p;
    public Point2 Point(double param) => Mix(p[0], p[1], param);
    public Vector2 Direction(double param) => p[1] - p[0];
    public Vector2 DirectionChange(double param) => new Vector2(0, 0);
    public double Length() => (p[1] - p[0]).Length();
    public SignedDistance SignedDistance(Point2 origin, ref double param)
    {
        Vector2 aq = origin - p[0];
        Vector2 ab = p[1] - p[0];
        param = DotProduct(aq, ab) / DotProduct(ab, ab);
        Vector2 eq = p[param > .5 ? 1 : 0] - origin;
        double endpointDistance = eq.Length();
        if (param > 0 && param < 1)
        {
            double orthoDistance = DotProduct(ab.GetOrthonormal(false), aq);
            if (Math.Abs(orthoDistance) < endpointDistance)
            {
                return new SignedDistance(orthoDistance, 0);
            }
        }
        return new SignedDistance(NonZeroSign(CrossProduct(aq, ab)) * endpointDistance, Math.Abs(DotProduct(ab.Normalize(), eq.Normalize())));
    }
    public int ScanlineIntersections(out double[] x, out int[] dy, double y)
    {
        x = new double[1];
        dy = new int[1];

        if ((y >= p[0].Y && y < p[1].Y) || (y >= p[1].Y && y < p[0].Y))
        {
            double param = (y - p[0].Y) / (p[1].Y - p[0].Y);
            x[0] = Mix(p[0].X, p[1].X, param);
            dy[0] = Sign(p[1].Y - p[0].Y);
            return 1;
        }
        return 0;
    }
    public void Bound(ref double l, ref double b, ref double r, ref double t)
    {
        IEdgeSegment.PointBounds(p[0], ref l, ref b, ref r, ref t);
        IEdgeSegment.PointBounds(p[1], ref l, ref b, ref r, ref t);
    }
    public void Reverse()
    {
        Point2 tmp = p[0];
        p[0] = p[1];
        p[1] = tmp;
    }
    public void MoveStartPoint(Point2 to) => p[0] = to;
    public void MoveEndPoint(Point2 to) => p[1] = to;
    public void SplitInThirds(out IEdgeSegment part0, out IEdgeSegment part1, out IEdgeSegment part2)
    {
        part0 = new LinearSegment(p[0], Point(1 / 3.0), Color);
        part1 = new LinearSegment(Point(1 / 3.0), Point(2 / 3.0), Color);
        part2 = new LinearSegment(Point(2 / 3.0), p[1], Color);
    }

    public IEdgeSegment Clone()
    {
        return new LinearSegment(p[0], p[1], Color);
    }
}

public class QuadraticSegment(Point2 p0, Point2 p1, Point2 p2, EdgeColor edgeColor = EdgeColor.WHITE) : IEdgeSegment
{
    private Point2[] p = [p0, p1, p2];
    public EdgeColor Color { get; set; } = edgeColor;

    public Point2[] ControlPoints() => p;
    public Point2 Point(double param) => Mix(Mix(p[0], p[1], param), Mix(p[1], p[2], param), param);
    public Vector2 Direction(double param)
    {
        Vector2 tangent = Mix(p[1] - p[0], p[2] - p[1], param);
        if (!tangent)
            return p[2] - p[0];
        return tangent;
    }
    public Vector2 DirectionChange(double param) => (p[2] - p[1]) - (p[1] - p[0]);
    public double Length()
    {
        Vector2 ab = p[1] - p[0];
        Vector2 br = p[2] - p[1] - ab;
        double abab = DotProduct(ab, ab);
        double abbr = DotProduct(ab, br);
        double brbr = DotProduct(br, br);
        double abLen = Math.Sqrt(abab);
        double brLen = Math.Sqrt(brbr);
        double crs = CrossProduct(ab, br);
        double h = Math.Sqrt(abab + abbr + abbr + brbr);
        return (
            brLen * ((abbr + brbr) * h - abbr * abLen) +
            crs * crs * Math.Log((brLen * h + abbr + brbr) / (brLen * abLen + abbr))
        ) / (brbr * brLen);
    }
    public SignedDistance SignedDistance(Point2 origin, ref double param)
    {
        Vector2 qa = p[0] - origin;
        Vector2 ab = p[1] - p[0];
        Vector2 br = p[2] - p[1] - ab;
        double a = DotProduct(br, br);
        double b = 3 * DotProduct(ab, br);
        double c = 2 * DotProduct(ab, ab) + DotProduct(qa, br);
        double d = DotProduct(qa, ab);
        double[] t = new double[3];
        int solutions = SolveCubic(ref t, a, b, c, d);

        Vector2 epDir = Direction(0);
        double minDistance = NonZeroSign(CrossProduct(epDir, qa)) * qa.Length(); // distance from A
        param = -DotProduct(qa, epDir) / DotProduct(epDir, epDir);
        {
            epDir = Direction(1);
            double distance = (p[2] - origin).Length(); // distance from B
            if (distance < Math.Abs(minDistance))
            {
                minDistance = NonZeroSign(CrossProduct(epDir, p[2] - origin)) * distance;
                param = DotProduct(origin - p[1], epDir) / DotProduct(epDir, epDir);
            }
        }
        for (int i = 0; i < solutions; ++i)
        {
            if (t[i] > 0 && t[i] < 1)
            {
                Point2 qe = qa + 2 * t[i] * ab + t[i] * t[i] * br;
                double distance = qe.Length();
                if (distance <= Math.Abs(minDistance))
                {
                    minDistance = NonZeroSign(CrossProduct(ab + t[i] * br, qe)) * distance;
                    param = t[i];
                }
            }
        }

        if (param >= 0 && param <= 1)
            return new SignedDistance(minDistance, 0);
        if (param < .5)
            return new SignedDistance(minDistance, Math.Abs(DotProduct(Direction(0).Normalize(), qa.Normalize())));
        else
            return new SignedDistance(minDistance, Math.Abs(DotProduct(Direction(1).Normalize(), (p[2] - origin).Normalize())));
    }
    public int ScanlineIntersections(out double[] x, out int[] dy, double y)
    {
        x = new double[2];
        dy = new int[2];

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
            double[] t = new double[2];
            int solutions = SolveQuadratic(ref t, br.Y, 2 * ab.Y, p[0].Y - y);
            // Sort solutions
            double tmp;
            if (solutions >= 2 && t[0] > t[1])
            {
                tmp = t[0];
                t[0] = t[1];
                t[1] = tmp;
            }
            for (int i = 0; i < solutions && total < 2; ++i)
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
                if (Math.Abs(p[2].Y - y) < Math.Abs(p[0].Y - y))
                    x[total] = p[2].X;
                dy[total++] = nextDY;
            }
        }
        return total;
    }
    public void Bound(ref double l, ref double b, ref double r, ref double t)
    {
        IEdgeSegment.PointBounds(p[0], ref l, ref b, ref r, ref t);
        IEdgeSegment.PointBounds(p[2], ref l, ref b, ref r, ref t);
        Vector2 bot = (p[1] - p[0]) - (p[2] - p[1]);
        if (bot.X != 0)
        {
            double param = (p[1].X - p[0].X) / bot.X;
            if (param > 0 && param < 1)
                IEdgeSegment.PointBounds(Point(param), ref l, ref b, ref r, ref t);
        }
        if (bot.Y != 0)
        {
            double param = (p[1].Y - p[0].Y) / bot.Y;
            if (param > 0 && param < 1)
                IEdgeSegment.PointBounds(Point(param), ref l, ref b, ref r, ref t);
        }
    }
    public void Reverse()
    {
        Point2 tmp = p[0];
        p[0] = p[2];
        p[2] = tmp;
    }
    public void MoveStartPoint(Point2 to)
    {
        Vector2 origSDir = p[0] - p[1];
        Point2 origP1 = p[1];
        p[1] += CrossProduct(p[0] - p[1], to - p[0]) / CrossProduct(p[0] - p[1], p[2] - p[1]) * (p[2] - p[1]);
        p[0] = to;
        if (DotProduct(origSDir, p[0] - p[1]) < 0)
            p[1] = origP1;
    }
    public void MoveEndPoint(Point2 to)
    {
        Vector2 origEDir = p[2] - p[1];
        Point2 origP1 = p[1];
        p[1] += CrossProduct(p[2] - p[1], to - p[2]) / CrossProduct(p[2] - p[1], p[0] - p[1]) * (p[0] - p[1]);
        p[2] = to;
        if (DotProduct(origEDir, p[2] - p[1]) < 0)
            p[1] = origP1;
    }
    public void SplitInThirds(out IEdgeSegment part0, out IEdgeSegment part1, out IEdgeSegment part2)
    {
        part0 = new QuadraticSegment(p[0], Mix(p[0], p[1], 1 / 3.0), Point(1 / 3.0), Color);
        part1 = new QuadraticSegment(Point(1 / 3.0), Mix(Mix(p[0], p[1], 5 / 9.0), Mix(p[1], p[2], 4 / 9.0), .5), Point(2 / 3.0), Color);
        part2 = new QuadraticSegment(Point(2 / 3.0), Mix(p[1], p[2], 2 / 3.0), p[2], Color);
    }

    public IEdgeSegment ConvertToCubic()
    {
        return new CubicSegment(p[0], Mix(p[0], p[1], 2 / 3.0), Mix(p[1], p[2], 1 / 3.0), p[2], Color);
    }

    public IEdgeSegment Clone()
    {
        return new QuadraticSegment(p[0], p[1], p[2], Color);
    }
}

public class CubicSegment(Point2 p0, Point2 p1, Point2 p2, Point2 p3, EdgeColor edgeColor = EdgeColor.WHITE) : IEdgeSegment
{
    private Point2[] p = [p0, p1, p2, p3];
    public EdgeColor Color { get; set; } = edgeColor;

    public Point2[] ControlPoints() => p;
    public Point2 Point(double param)
    {
        Vector2 p12 = Mix(p[1], p[2], param);
        return Mix(Mix(Mix(p[0], p[1], param), p12, param), Mix(p12, Mix(p[2], p[3], param), param), param);
    }
    public Vector2 Direction(double param)
    {
        Vector2 tangent = Mix(Mix(p[1] - p[0], p[2] - p[1], param), Mix(p[2] - p[1], p[3] - p[2], param), param);
        if (!tangent)
        {
            if (param == 0) return p[2] - p[0];
            if (param == 1) return p[3] - p[1];
        }
        return tangent;
    }
    public Vector2 DirectionChange(double param) => Mix((p[2] - p[1]) - (p[1] - p[0]), (p[3] - p[2]) - (p[2] - p[1]), param);
    public SignedDistance SignedDistance(Point2 origin, ref double param)
    {
        Vector2 qa = p[0] - origin;
        Vector2 ab = p[1] - p[0];
        Vector2 br = p[2] - p[1] - ab;
        Vector2 @as = (p[3] - p[2]) - (p[2] - p[1]) - br;

        Vector2 epDir = Direction(0);
        double minDistance = NonZeroSign(CrossProduct(epDir, qa)) * qa.Length(); // distance from A
        param = -DotProduct(qa, epDir) / DotProduct(epDir, epDir);
        {
            epDir = Direction(1);
            double distance = (p[3] - origin).Length(); // distance from B
            if (distance < Math.Abs(minDistance))
            {
                minDistance = NonZeroSign(CrossProduct(epDir, p[3] - origin)) * distance;
                param = DotProduct(epDir - (p[3] - origin), epDir) / DotProduct(epDir, epDir);
            }
        }
        // Iterative minimum distance search
        for (int i = 0; i <= Constants.MSDFGEN_CUBIC_SEARCH_STARTS; ++i)
        {
            double t = (double)i / Constants.MSDFGEN_CUBIC_SEARCH_STARTS;
            Vector2 qe = qa + 3 * t * ab + 3 * t * t * br + t * t * t * @as;
            for (int step = 0; step < Constants.MSDFGEN_CUBIC_SEARCH_STEPS; ++step)
            {
                // Improve t
                Vector2 d1 = 3 * ab + 6 * t * br + 3 * t * t * @as;
                Vector2 d2 = 6 * br + 6 * t * @as;
                t -= DotProduct(qe, d1) / (DotProduct(d1, d1) + DotProduct(qe, d2));
                if (t <= 0 || t >= 1)
                    break;
                qe = qa + 3 * t * ab + 3 * t * t * br + t * t * t * @as;
                double distance = qe.Length();
                if (distance < Math.Abs(minDistance))
                {
                    minDistance = NonZeroSign(CrossProduct(d1, qe)) * distance;
                    param = t;
                }
            }
        }

        if (param >= 0 && param <= 1)
            return new SignedDistance(minDistance, 0);
        if (param < .5)
            return new SignedDistance(minDistance, Math.Abs(DotProduct(Direction(0).Normalize(), qa.Normalize())));
        else
            return new SignedDistance(minDistance, Math.Abs(DotProduct(Direction(1).Normalize(), (p[3] - origin).Normalize())));
    }
    public int ScanlineIntersections(out double[] x, out int[] dy, double y)
    {
        x = new double[3];
        dy = new int[3];

        int total = 0;
        int nextDY = y > p[0].Y ? 1 : -1;
        x[total] = p[0].X;
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
            double[] t = new double[3];
            int solutions = SolveCubic(ref t, @as.Y, 3 * br.Y, 3 * ab.Y, p[0].Y - y);
            // Sort solutions
            double tmp;
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
                    x[total] = p[0].X + 3 * t[i] * ab.X + 3 * t[i] * t[i] * br.X + t[i] * t[i] * t[i] * @as.X;
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
                if (Math.Abs(p[3].Y - y) < Math.Abs(p[0].Y - y))
                    x[total] = p[3].X;
                dy[total++] = nextDY;
            }
        }
        return total;
    }
    public void Bound(ref double l, ref double b, ref double r, ref double t)
    {
        IEdgeSegment.PointBounds(p[0], ref l, ref b, ref r, ref t);
        IEdgeSegment.PointBounds(p[3], ref l, ref b, ref r, ref t);
        Vector2 a0 = p[1] - p[0];
        Vector2 a1 = 2 * (p[2] - p[1] - a0);
        Vector2 a2 = p[3] - 3 * p[2] + 3 * p[1] - p[0];
        double[] @params = new double[2];
        int solutions;
        solutions = SolveQuadratic(ref @params, a2.X, a1.X, a0.X);
        for (int i = 0; i < solutions; ++i)
            if (@params[i] > 0 && @params[i] < 1)
                IEdgeSegment.PointBounds(Point(@params[i]), ref l, ref b, ref r, ref t);
        solutions = SolveQuadratic(ref @params, a2.Y, a1.Y, a0.Y);
        for (int i = 0; i < solutions; ++i)
            if (@params[i] > 0 && @params[i] < 1)
                IEdgeSegment.PointBounds(Point(@params[i]), ref l, ref b, ref r, ref t);
    }
    public void Reverse()
    {
        Point2 tmp = p[0];
        p[0] = p[3];
        p[3] = tmp;
        tmp = p[1];
        p[1] = p[2];
        p[2] = tmp;
    }
    public void MoveStartPoint(Point2 to)
    {
        p[1] += to - p[0];
        p[0] = to;
    }
    public void MoveEndPoint(Point2 to)
    {
        p[2] += to - p[3];
        p[3] = to;
    }
    public void SplitInThirds(out IEdgeSegment part0, out IEdgeSegment part1, out IEdgeSegment part2)
    {
        part0 = new CubicSegment(p[0], p[0] == p[1] ? p[0] : Mix(p[0], p[1], 1 / 3.0), Mix(Mix(p[0], p[1], 1 / 3.0), Mix(p[1], p[2], 1 / 3.0), 1 / 3.0), Point(1 / 3.0), Color);
        part1 = new CubicSegment(Point(1 / 3.0),
            Mix(Mix(Mix(p[0], p[1], 1 / 3.0), Mix(p[1], p[2], 1 / 3.0), 1 / 3.0), Mix(Mix(p[1], p[2], 1 / 3.0), Mix(p[2], p[3], 1 / 3.0), 1 / 3.0), 2 / 3.0),
            Mix(Mix(Mix(p[0], p[1], 2 / 3.0), Mix(p[1], p[2], 2 / 3.0), 2 / 3.0), Mix(Mix(p[1], p[2], 2 / 3.0), Mix(p[2], p[3], 2 / 3.0), 2 / 3.0), 1 / 3.0),
            Point(2 / 3.0), Color);
        part2 = new CubicSegment(Point(2 / 3.0), Mix(Mix(p[1], p[2], 2 / 3.0), Mix(p[2], p[3], 2 / 3.0), 2 / 3.0), p[2] == p[3] ? p[3] : Mix(p[2], p[3], 2 / 3.0), p[3], Color);
    }

    public void Deconverge(int param, double amount)
    {
        Vector2 dir = Direction(param);
        Vector2 normal = dir.GetOrthonormal();
        double h = DotProduct(DirectionChange(param) - dir, normal);
        switch (param)
        {
            case 0:
                p[1] += amount * (dir + Sign(h) * Math.Sqrt(Math.Abs(h)) * normal);
                break;
            case 1:
                p[2] -= amount * (dir - Sign(h) * Math.Sqrt(Math.Abs(h)) * normal);
                break;
        }
    }

    public IEdgeSegment Clone()
    {
        return new CubicSegment(p[0], p[1], p[2], p[3], Color);
    }
}