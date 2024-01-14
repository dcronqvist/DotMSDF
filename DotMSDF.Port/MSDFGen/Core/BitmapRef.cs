using System;

namespace DotMSDF.Port;

public ref struct BitmapRef<T> where T : unmanaged
{
    public int Width { get; set; }
    public int Height { get; set; }
    public Span<T> Pixels { get; set; }

    public BitmapRef(Span<T> pixels, int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public readonly ref T this[int x, int y] => ref Pixels[y * Width + x];
}