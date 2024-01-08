using System.Numerics;

namespace DotMSDF;

public static class ExtensionsVector2
{
    public static Vector2 GetOrthonormal(this Vector2 vector2)
    {
        var len = vector2.Length();
        return len == 0 ? new Vector2(0, -1f) : new Vector2(vector2.Y / len, -vector2.X / len);
    }

    public static float Cross(this Vector2 vector2, Vector2 other)
    {
        return vector2.X * other.Y - vector2.Y * other.X;
    }

    public static Vector2 Normalize(this Vector2 v) => Vector2.Normalize(v);
}
