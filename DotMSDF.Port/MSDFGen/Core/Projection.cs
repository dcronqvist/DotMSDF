namespace DotMSDF.Port;

public class Projection
{
    private Vector2 _scale;
    private Vector2 _translate;

    public Projection()
    {
        _scale = new Vector2(1);
        _translate = new Vector2(0);
    }

    public Projection(Vector2 scale, Vector2 translate)
    {
        _scale = scale;
        _translate = translate;
    }

    public Point2 Project(Point2 coord)
    {
        return _scale * (coord + _translate);
    }

    public Point2 Unproject(Point2 coord)
    {
        return (coord / _scale) - _translate;
    }

    public Vector2 ProjectVector(Vector2 vector)
    {
        return _scale * vector;
    }

    public Vector2 UnprojectVector(Vector2 vector)
    {
        return vector / _scale;
    }

    public double ProjectX(double x)
    {
        return _scale.X * (x + _translate.X);
    }

    public double ProjectY(double y)
    {
        return _scale.Y * (y + _translate.Y);
    }

    public double UnprojectX(double x)
    {
        return (x / _scale.X) - _translate.X;
    }

    public double UnprojectY(double y)
    {
        return (y / _scale.Y) - _translate.Y;
    }
}