using System;

namespace DotMSDF.Port;

public struct Vector2 : IComparable<Vector2>
{
    public double X { get; set; }
    public double Y { get; set; }

    public Vector2(double val) => X = Y = val;
    public Vector2(double x, double y) => (X, Y) = (x, y);

    public void Reset() => (X, Y) = (0, 0);
    public void Set(double x, double y) => (X, Y) = (x, y);
    public double SquaredLength() => X * X + Y * Y;
    public double Length() => Math.Sqrt(SquaredLength());
    public Vector2 Normalize(bool allowZero = false)
    {
        double len = Length();
        if (len != 0)
            return new Vector2(X / len, Y / len);
        return new Vector2(0, allowZero ? 0 : 1);
    }
    public Vector2 GetOrthogonal(bool polarity = true) => polarity ? new Vector2(-Y, X) : new Vector2(Y, -X);
    public Vector2 GetOrthonormal(bool polarity = true, bool allowZero = false)
    {
        double len = Length();
        if (len != 0)
            return polarity ? new Vector2(-Y / len, X / len) : new Vector2(Y / len, -X / len);
        return polarity ? new Vector2(0, allowZero ? 0 : 1) : new Vector2(0, allowZero ? 0 : -1);
    }

    public static explicit operator bool(Vector2 v) => v.X != 0 || v.Y != 0;
    public static Vector2 operator +(Vector2 v) => v;
    public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator -(Vector2 v) => new Vector2(-v.X, -v.Y);
    public static Vector2 operator *(Vector2 v, double s) => new Vector2(v.X * s, v.Y * s);
    public static Vector2 operator *(double s, Vector2 v) => new Vector2(v.X * s, v.Y * s);
    public static Vector2 operator *(Vector2 a, Vector2 b) => new Vector2(a.X * b.X, a.Y * b.Y);
    public static Vector2 operator /(Vector2 v, double s) => new Vector2(v.X / s, v.Y / s);
    public static Vector2 operator /(Vector2 a, Vector2 b) => new Vector2(a.X / b.X, a.Y / b.Y);
    public static Vector2 operator /(double value, Vector2 vector) => new Vector2(value / vector.X, value / vector.Y);
    public static bool operator ==(Vector2 a, Vector2 b) => a.X == b.X && a.Y == b.Y;
    public static bool operator !=(Vector2 a, Vector2 b) => a.X != b.X || a.Y != b.Y;
    public static bool operator !(Vector2 v) => v.X == 0 && v.Y == 0;

    public static bool operator <(Vector2 a, Vector2 b) => a.X < b.X && a.Y < b.Y;
    public static bool operator >(Vector2 a, Vector2 b) => a.X > b.X && a.Y > b.Y;


    public static double DotProduct(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;
    public static double CrossProduct(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    public override bool Equals(object obj) => obj is Vector2 v && this == v;
    public override int GetHashCode() => HashCode.Combine(X, Y);

    public int CompareTo(Vector2 other) => this > other ? 1 : this < other ? -1 : 0;
}