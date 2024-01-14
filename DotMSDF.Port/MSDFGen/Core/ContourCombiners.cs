using System;
using System.Collections.Generic;
using static DotMSDF.Port.Arithmetics;
using static DotMSDF.Port.Vector2;

namespace DotMSDF.Port;

public class OverlappingContourCombiner<TEdgeSelector> where TEdgeSelector : IEdgeSelector, new()
{
    private Point2 _p;
    private List<int> _windings = [];
    private List<TEdgeSelector> _edgeSelectors = [];

    public OverlappingContourCombiner(Shape shape)
    {
        _windings = new List<int>(shape.Contours.Count);
        for (int i = 0; i < shape.Contours.Count; i++)
        {
            _windings.Add(shape.Contours[i].Winding());
        }
        _edgeSelectors = new List<TEdgeSelector>(shape.Contours.Count);
    }

    public void Reset(Point2 p)
    {
        _p = p;
        for (int i = 0; i < _edgeSelectors.Count; i++)
        {
            _edgeSelectors[i].Reset(p);
        }
    }

    public TEdgeSelector EdgeSelector(int i) => _edgeSelectors[i];

    public dynamic Distance()
    {
        int contourCount = _edgeSelectors.Count;
        TEdgeSelector shapeEdgeSelector = new();
        TEdgeSelector innerEdgeSelector = new();
        TEdgeSelector outerEdgeSelector = new();
        shapeEdgeSelector.Reset(_p);
        innerEdgeSelector.Reset(_p);
        outerEdgeSelector.Reset(_p);
        for (int i = 0; i < contourCount; ++i)
        {
            dynamic edgeDistance = _edgeSelectors[i].Distance();
            shapeEdgeSelector.Merge(_edgeSelectors[i]);
            if (_windings[i] > 0 && ResolveDistance(edgeDistance) >= 0)
                innerEdgeSelector.Merge(_edgeSelectors[i]);
            if (_windings[i] < 0 && ResolveDistance(edgeDistance) <= 0)
                outerEdgeSelector.Merge(_edgeSelectors[i]);
        }

        dynamic shapeDistance = shapeEdgeSelector.Distance();
        dynamic innerDistance = innerEdgeSelector.Distance();
        dynamic outerDistance = outerEdgeSelector.Distance();
        double innerScalarDistance = ResolveDistance(innerDistance);
        double outerScalarDistance = ResolveDistance(outerDistance);
        var distance = Activator.CreateInstance(shapeDistance.GetType());
        InitDistance(distance);

        int winding = 0;
        if (innerScalarDistance >= 0 && Math.Abs(innerScalarDistance) <= Math.Abs(outerScalarDistance))
        {
            distance = innerDistance;
            winding = 1;
            for (int i = 0; i < contourCount; ++i)
                if (_windings[i] > 0)
                {
                    dynamic contourDistance = _edgeSelectors[i].Distance();
                    if (Math.Abs(ResolveDistance(contourDistance)) < Math.Abs(outerScalarDistance) && ResolveDistance(contourDistance) > ResolveDistance(distance))
                        distance = contourDistance;
                }
        }
        else if (outerScalarDistance <= 0 && Math.Abs(outerScalarDistance) < Math.Abs(innerScalarDistance))
        {
            distance = outerDistance;
            winding = -1;
            for (int i = 0; i < contourCount; ++i)
                if (_windings[i] < 0)
                {
                    dynamic contourDistance = _edgeSelectors[i].Distance();
                    if (Math.Abs(ResolveDistance(contourDistance)) < Math.Abs(innerScalarDistance) && ResolveDistance(contourDistance) < ResolveDistance(distance))
                        distance = contourDistance;
                }
        }
        else
            return shapeDistance;

        for (int i = 0; i < contourCount; ++i)
            if (_windings[i] != winding)
            {
                dynamic contourDistance = _edgeSelectors[i].Distance();
                if (ResolveDistance(contourDistance) * ResolveDistance(distance) >= 0 && Math.Abs(ResolveDistance(contourDistance)) < Math.Abs(ResolveDistance(distance)))
                    distance = contourDistance;
            }
        if (ResolveDistance(distance) == ResolveDistance(shapeDistance))
            distance = shapeDistance;
        return distance;
    }

    private static void InitDistance(ref double distance)
    {
        distance = -double.MaxValue;
    }

    private static void InitDistance(ref MultiDistance distance)
    {
        distance = distance with
        {
            R = -double.MaxValue,
            G = -double.MaxValue,
            B = -double.MaxValue
        };
    }

    private static double ResolveDistance(double distance) => distance;

    private static double ResolveDistance(MultiDistance distance) => Median(distance.R, distance.G, distance.B);
}