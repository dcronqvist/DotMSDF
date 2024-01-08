using System.Numerics;

namespace DotMSDF;

public class Projection
{
    private Vector2 _scale;
    private Vector2 _translate;

    public Projection()
    {
        _scale = Vector2.One;
        _translate = Vector2.Zero;
    }

    public Projection(Vector2 scale, Vector2 translate)
    {
        _scale = scale;
        _translate = translate;
    }

    public Vector2 Project(Vector2 coord) => _scale * (coord + _translate);
    public Vector2 Unproject(Vector2 coord) => (coord / _scale) - _translate;
    public Vector2 ProjectVector(Vector2 vector) => _scale * vector;
    public Vector2 UnprojectVector(Vector2 vector) => vector / _scale;
    public float ProjectX(float x) => _scale.X * (x + _translate.X);
    public float ProjectY(float y) => _scale.Y * (y + _translate.Y);
    public float UnprojectX(float x) => (x / _scale.X) - _translate.X;
    public float UnprojectY(float y) => (y / _scale.Y) - _translate.Y;
}