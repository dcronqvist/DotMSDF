using System.Runtime.CompilerServices;

namespace DotMSDF;

public record SignedDistance(float Distance, float Dot)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(SignedDistance left, SignedDistance right)
    {
        return left.Distance < right.Distance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(SignedDistance left, SignedDistance right)
    {
        return left.Distance > right.Distance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(SignedDistance left, SignedDistance right)
    {
        return left.Distance <= right.Distance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(SignedDistance left, SignedDistance right)
    {
        return left.Distance >= right.Distance;
    }
}