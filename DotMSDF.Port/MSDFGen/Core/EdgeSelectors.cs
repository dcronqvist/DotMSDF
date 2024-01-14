using DistanceType = double;

using static DotMSDF.Port.Arithmetics;
using static DotMSDF.Port.Vector2;
using System;

namespace DotMSDF.Port;

public interface IDistance
{

}

public record MultiDistance(double R, double G, double B);
public record MultiAndTrueDistance(double R, double G, double B, double A) : MultiDistance(R, G, B);

public partial class Constants
{
    public const double DISTANCE_DELTA_FACTOR = 1.001;
}

public interface IEdgeSelector
{
    void Reset(Point2 p);
    void Merge(IEdgeSelector other);
    dynamic Distance();
}

public abstract class EdgeSelector<T> : IEdgeSelector where T : IEdgeSelector
{
    public abstract void Reset(Point2 p);

    protected abstract void Merge(T other);

    public void Merge(IEdgeSelector other)
    {
        Merge((T)other);
    }

    public abstract dynamic Distance();
}

public class TrueDistanceSelector : EdgeSelector<TrueDistanceSelector>
{
    public struct EdgeCache
    {
        public Point2 Point { get; set; }
        public double AbsDistance { get; set; }
    }

    private Point2 _p;
    private SignedDistance _minDistance;

    public override void Reset(Point2 p)
    {
        double delta = Constants.DISTANCE_DELTA_FACTOR * (p - _p).Length();
        _minDistance.Distance += NonZeroSign(_minDistance.Distance) * delta;
        _p = p;
    }

    public void AddEdge(ref EdgeCache cache, IEdgeSegment prevEdge, IEdgeSegment edge, IEdgeSegment nextEdge)
    {
        double delta = Constants.DISTANCE_DELTA_FACTOR * (_p - cache.Point).Length();
        if (cache.AbsDistance - delta <= Math.Abs(_minDistance.Distance))
        {
            double dummy = 0;
            SignedDistance distance = edge.SignedDistance(_p, ref dummy);
            if (distance < _minDistance)
                _minDistance = distance;
            cache.Point = _p;
            cache.AbsDistance = Math.Abs(distance.Distance);
        }
    }

    protected override void Merge(TrueDistanceSelector other)
    {
        if (other._minDistance < _minDistance)
            _minDistance = other._minDistance;
    }

    public override dynamic Distance()
    {
        return _minDistance.Distance;
    }
}

public abstract class PseudoDistanceSelectorBase : EdgeSelector<PseudoDistanceSelectorBase>
{
    public struct EdgeCache
    {
        public Point2 Point { get; set; }
        public double AbsDistance { get; set; }
        public double ADomainDistance { get; set; }
        public double BDomainDistance { get; set; }
        public double APseudoDistance { get; set; }
        public double BPseudoDistance { get; set; }
    }

    private SignedDistance _minTrueDistance;
    private double _minNegativePseudoDistance;
    private double _minPositivePseudoDistance;
    private IEdgeSegment _nearEdge;
    private double _nearEdgeParam;

    public PseudoDistanceSelectorBase(SignedDistance minTrueDistance)
    {
        _minTrueDistance = minTrueDistance;
        _minNegativePseudoDistance = -Math.Abs(_minTrueDistance.Distance);
        _minPositivePseudoDistance = Math.Abs(_minTrueDistance.Distance);
        _nearEdge = null;
        _nearEdgeParam = 0;
    }

    public static bool GetPseudoDistance(ref double distance, Vector2 ep, Vector2 edgeDir)
    {
        double ts = DotProduct(ep, edgeDir);
        if (ts > 0)
        {
            double pseudoDistance = CrossProduct(ep, edgeDir);
            if (Math.Abs(pseudoDistance) < Math.Abs(distance))
            {
                distance = pseudoDistance;
                return true;
            }
        }
        return false;
    }

    public abstract override void Reset(Point2 p);

    public void Reset(double delta)
    {
        _minTrueDistance.Distance += NonZeroSign(_minTrueDistance.Distance) * delta;
        _minNegativePseudoDistance = -Math.Abs(_minTrueDistance.Distance);
        _minPositivePseudoDistance = Math.Abs(_minTrueDistance.Distance);
        _nearEdge = null;
        _nearEdgeParam = 0;
    }

    public bool IsEdgeRelevant(EdgeCache cache, IEdgeSegment edge, Point2 p)
    {
        double delta = Constants.DISTANCE_DELTA_FACTOR * (p - cache.Point).Length();
        return (
            cache.AbsDistance - delta <= Math.Abs(_minTrueDistance.Distance) ||
            Math.Abs(cache.ADomainDistance) < delta ||
            Math.Abs(cache.BDomainDistance) < delta ||
            (cache.ADomainDistance > 0 && (cache.APseudoDistance < 0 ?
                cache.APseudoDistance + delta >= _minNegativePseudoDistance :
                cache.APseudoDistance - delta <= _minPositivePseudoDistance
            )) ||
            (cache.BDomainDistance > 0 && (cache.BPseudoDistance < 0 ?
                cache.BPseudoDistance + delta >= _minNegativePseudoDistance :
                cache.BPseudoDistance - delta <= _minPositivePseudoDistance
            ))
        );
    }

    public void AddEdgeTrueDistance(IEdgeSegment edge, SignedDistance distance, double param)
    {
        if (distance < _minTrueDistance)
        {
            _minTrueDistance = distance;
            _nearEdge = edge;
            _nearEdgeParam = param;
        }
    }

    public void AddEdgePseudoDistance(double distance)
    {
        if (distance <= 0 && distance > _minNegativePseudoDistance)
            _minNegativePseudoDistance = distance;
        if (distance >= 0 && distance < _minPositivePseudoDistance)
            _minPositivePseudoDistance = distance;
    }

    protected override void Merge(PseudoDistanceSelectorBase other)
    {
        if (other._minTrueDistance < _minTrueDistance)
        {
            _minTrueDistance = other._minTrueDistance;
            _nearEdge = other._nearEdge;
            _nearEdgeParam = other._nearEdgeParam;
        }
        if (other._minNegativePseudoDistance > _minNegativePseudoDistance)
            _minNegativePseudoDistance = other._minNegativePseudoDistance;
        if (other._minPositivePseudoDistance < _minPositivePseudoDistance)
            _minPositivePseudoDistance = other._minPositivePseudoDistance;
    }

    public double ComputeDistance(Point2 p)
    {
        double minDistance = _minTrueDistance.Distance < 0 ? _minNegativePseudoDistance : _minPositivePseudoDistance;
        if (_nearEdge != null)
        {
            SignedDistance distance = _minTrueDistance;
            _nearEdge.DistanceToPseudoDistance(ref distance, p, _nearEdgeParam);
            if (Math.Abs(distance.Distance) < Math.Abs(minDistance))
                minDistance = distance.Distance;
        }
        return minDistance;
    }

    public SignedDistance TrueDistance()
    {
        return _minTrueDistance;
    }
}

public class PseudoDistanceSelector : PseudoDistanceSelectorBase
{
    private Point2 _p;

    public PseudoDistanceSelector(SignedDistance minTrueDistance, Point2 p) : base(minTrueDistance)
    {
        _p = p;
    }

    public override void Reset(Point2 p)
    {
        double delta = Constants.DISTANCE_DELTA_FACTOR * (p - _p).Length();
        base.Reset(delta);
        _p = p;
    }

    public void AddEdge(ref EdgeCache cache, IEdgeSegment prevEdge, IEdgeSegment edge, IEdgeSegment nextEdge)
    {
        if (IsEdgeRelevant(cache, edge, _p))
        {
            double param = 0;
            SignedDistance distance = edge.SignedDistance(_p, ref param);
            AddEdgeTrueDistance(edge, distance, param);
            cache.Point = _p;
            cache.AbsDistance = Math.Abs(distance.Distance);

            Vector2 ap = _p - edge.Point(0);
            Vector2 bp = _p - edge.Point(1);
            Vector2 aDir = edge.Direction(0).Normalize(true);
            Vector2 bDir = edge.Direction(1).Normalize(true);
            Vector2 prevDir = prevEdge.Direction(1).Normalize(true);
            Vector2 nextDir = nextEdge.Direction(0).Normalize(true);
            double add = DotProduct(ap, (prevDir + aDir).Normalize(true));
            double bdd = -DotProduct(bp, (bDir + nextDir).Normalize(true));
            if (add > 0)
            {
                double pd = distance.Distance;
                if (GetPseudoDistance(ref pd, ap, -aDir))
                    AddEdgePseudoDistance(pd = -pd);
                cache.APseudoDistance = pd;
            }
            if (bdd > 0)
            {
                double pd = distance.Distance;
                if (GetPseudoDistance(ref pd, bp, bDir))
                    AddEdgePseudoDistance(pd);
                cache.BPseudoDistance = pd;
            }
            cache.ADomainDistance = add;
            cache.BDomainDistance = bdd;
        }
    }

    public override dynamic Distance()
    {
        return ComputeDistance(_p);
    }
}

public class MultiDistanceSelector : EdgeSelector<MultiDistanceSelector>
{
    private Point2 _p;
    private PseudoDistanceSelectorBase _r, _g, _b;

    public override void Reset(Point2 p)
    {
        double delta = Constants.DISTANCE_DELTA_FACTOR * (p - _p).Length();
        _r.Reset(delta);
        _g.Reset(delta);
        _b.Reset(delta);
        _p = p;
    }

    public void AddEdge(ref PseudoDistanceSelectorBase.EdgeCache cache, IEdgeSegment prevEdge, IEdgeSegment edge, IEdgeSegment nextEdge)
    {
        if (
                ((edge.Color & EdgeColor.RED) != 0 && _r.IsEdgeRelevant(cache, edge, _p)) ||
                ((edge.Color & EdgeColor.GREEN) != 0 && _g.IsEdgeRelevant(cache, edge, _p)) ||
                ((edge.Color & EdgeColor.BLUE) != 0 && _b.IsEdgeRelevant(cache, edge, _p))
            )
        {
            double param = 0;
            SignedDistance distance = edge.SignedDistance(_p, ref param);
            if ((edge.Color & EdgeColor.RED) != 0)
                _r.AddEdgeTrueDistance(edge, distance, param);
            if ((edge.Color & EdgeColor.GREEN) != 0)
                _g.AddEdgeTrueDistance(edge, distance, param);
            if ((edge.Color & EdgeColor.BLUE) != 0)
                _b.AddEdgeTrueDistance(edge, distance, param);
            cache.Point = _p;
            cache.AbsDistance = Math.Abs(distance.Distance);

            Vector2 ap = _p - edge.Point(0);
            Vector2 bp = _p - edge.Point(1);
            Vector2 aDir = edge.Direction(0).Normalize(true);
            Vector2 bDir = edge.Direction(1).Normalize(true);
            Vector2 prevDir = prevEdge.Direction(1).Normalize(true);
            Vector2 nextDir = nextEdge.Direction(0).Normalize(true);
            double add = DotProduct(ap, (prevDir + aDir).Normalize(true));
            double bdd = -DotProduct(bp, (bDir + nextDir).Normalize(true));
            if (add > 0)
            {
                double pd = distance.Distance;
                if (PseudoDistanceSelectorBase.GetPseudoDistance(ref pd, ap, -aDir))
                {
                    pd = -pd;
                    if ((edge.Color & EdgeColor.RED) != 0)
                        _r.AddEdgePseudoDistance(pd);
                    if ((edge.Color & EdgeColor.GREEN) != 0)
                        _g.AddEdgePseudoDistance(pd);
                    if ((edge.Color & EdgeColor.BLUE) != 0)
                        _b.AddEdgePseudoDistance(pd);
                }
                cache.APseudoDistance = pd;
            }
            if (bdd > 0)
            {
                double pd = distance.Distance;
                if (PseudoDistanceSelectorBase.GetPseudoDistance(ref pd, bp, bDir))
                {
                    if ((edge.Color & EdgeColor.RED) != 0)
                        _r.AddEdgePseudoDistance(pd);
                    if ((edge.Color & EdgeColor.GREEN) != 0)
                        _g.AddEdgePseudoDistance(pd);
                    if ((edge.Color & EdgeColor.BLUE) != 0)
                        _b.AddEdgePseudoDistance(pd);
                }
                cache.BPseudoDistance = pd;
            }
            cache.ADomainDistance = add;
            cache.BDomainDistance = bdd;
        }
    }

    protected override void Merge(MultiDistanceSelector other)
    {
        _r.Merge(other._r);
        _g.Merge(other._g);
        _b.Merge(other._b);
    }

    public override dynamic Distance()
    {
        return new MultiDistance(
            _r.ComputeDistance(_p),
            _g.ComputeDistance(_p),
            _b.ComputeDistance(_p)
        );
    }

    public SignedDistance TrueDistance()
    {
        SignedDistance distance = _r.TrueDistance();
        if (_g.TrueDistance() < distance)
            distance = _g.TrueDistance();
        if (_b.TrueDistance() < distance)
            distance = _b.TrueDistance();
        return distance;
    }
}