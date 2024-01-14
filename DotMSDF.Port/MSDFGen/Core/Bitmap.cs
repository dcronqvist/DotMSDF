namespace DotMSDF.Port;

public struct Float3
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
}

public class Bitmap<T> where T : unmanaged
{
    public int Width { get; set; }
    public int Height { get; set; }
    public T[] Pixels { get; set; }

    public Bitmap(T[] pixels, int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public Bitmap(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new T[width * height];
    }

    public Bitmap(Bitmap<T> bitmap)
    {
        Width = bitmap.Width;
        Height = bitmap.Height;
        Pixels = bitmap.Pixels;
    }

    public ref T this[int x, int y] => ref Pixels[y * Width + x];

    public BitmapRef<T> AsRef() => new BitmapRef<T>(Pixels, Width, Height);
}