namespace DotMSDF.Port;

public class EdgeHolder
{
    private IEdgeSegment _edgeSegment;

    public EdgeHolder() { }
    public EdgeHolder(IEdgeSegment edgeSegment) => _edgeSegment = edgeSegment;
    public EdgeHolder(Point2 p0, Point2 p1, EdgeColor edgeColor = EdgeColor.WHITE) => _edgeSegment = IEdgeSegment.Create(p0, p1, edgeColor);
    public EdgeHolder(Point2 p0, Point2 p1, Point2 p2, EdgeColor edgeColor = EdgeColor.WHITE) => _edgeSegment = IEdgeSegment.Create(p0, p1, p2, edgeColor);
    public EdgeHolder(Point2 p0, Point2 p1, Point2 p2, Point2 p3, EdgeColor edgeColor = EdgeColor.WHITE) => _edgeSegment = IEdgeSegment.Create(p0, p1, p2, p3, edgeColor);

    public IEdgeSegment EdgeSegment
    {
        get => _edgeSegment;
        set => _edgeSegment = value;
    }

    public static void Swap(ref EdgeHolder a, ref EdgeHolder b)
    {
        var temp = a._edgeSegment;
        a._edgeSegment = b._edgeSegment;
        b._edgeSegment = temp;
    }
}