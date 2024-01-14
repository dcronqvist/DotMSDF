using System;

namespace DotMSDF.Port;

public class SignedDistance
{
    public static readonly SignedDistance Infinite = new SignedDistance(-1e240, 1);

    public double Distance { get; set; }
    public double Dot { get; set; }

    public SignedDistance() => (Distance, Dot) = (-double.MaxValue, 0);
    public SignedDistance(double distance, double dot) => (Distance, Dot) = (distance, dot);

    public static bool operator <(SignedDistance a, SignedDistance b) => Math.Abs(a.Distance) < Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot < b.Dot);
    public static bool operator >(SignedDistance a, SignedDistance b) => Math.Abs(a.Distance) > Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot > b.Dot);
    public static bool operator <=(SignedDistance a, SignedDistance b) => Math.Abs(a.Distance) < Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot <= b.Dot);
    public static bool operator >=(SignedDistance a, SignedDistance b) => Math.Abs(a.Distance) > Math.Abs(b.Distance) || (Math.Abs(a.Distance) == Math.Abs(b.Distance) && a.Dot >= b.Dot);
}