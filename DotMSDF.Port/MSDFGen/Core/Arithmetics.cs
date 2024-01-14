using System;

namespace DotMSDF.Port;

public static class Arithmetics
{
    public static T Min<T>(T a, T b) where T : IComparable<T> => a.CompareTo(b) < 0 ? a : b;

    public static T Max<T>(T a, T b) where T : IComparable<T> => a.CompareTo(b) > 0 ? a : b;

    public static T Median<T>(T a, T b, T c) where T : IComparable<T> => Max(Min(a, b), Min(Max(a, b), c));

    public static T Mix<T, S>(T a, T b, S weight) where T : IComparable<T> where S : IComparable<S> => (dynamic)a * (1 - (dynamic)weight) + (dynamic)b * (dynamic)weight;

    public static T Clamp<T>(T n) where T : IComparable<T> => n >= (dynamic)0 && n <= (dynamic)1 ? n : (n > (dynamic)0 ? (dynamic)1 : (dynamic)0);

    public static T Clamp<T>(T n, T b) where T : IComparable<T> => n >= (dynamic)0 && n <= (dynamic)b ? n : (n > (dynamic)0 ? b : (dynamic)0);

    public static T Clamp<T>(T n, T a, T b) where T : IComparable<T> => n >= (dynamic)a && n <= (dynamic)b ? n : (n > (dynamic)a ? b : a);

    public static int Sign<T>(T n) where T : IComparable<T> => (n > (dynamic)0 ? (dynamic)1 : (dynamic)0) - (n < (dynamic)0 ? (dynamic)1 : (dynamic)0);

    public static int NonZeroSign<T>(T n) where T : IComparable<T> => 2 * (n > (dynamic)0 ? (dynamic)1 : (dynamic)0) - 1;
}