using DuckGame;
using StbImageSharp;
using StbImageWriteSharp;
using System.IO;

namespace HatsPlusPlus;

public class Bitmap {
    public int Width { get; private set; }
    public int Height { get; private set; }
    public byte[] Data { get; private set; }

    public Option<string> Path { get; private set; }

    public static Bitmap FromMemory(byte[] data, int width, int height) =>
        new Bitmap(data, width, height);

    public static Bitmap Empty(int width, int height) {
        var data = new byte[height * width * 4];
        return new Bitmap(data, width, height);
    }

    public static Bitmap FromPath(string path) {
        var image = ImageResult.FromStream(File.OpenRead(path), StbImageSharp.ColorComponents.RedGreenBlueAlpha);
        var bitmap = Bitmap.FromMemory(image.Data, image.Width, image.Height);
        bitmap.Path = path;
        return bitmap;
    }

    private Bitmap(byte[] data, int width, int height) {
        Data = data;
        Width = width;
        Height = height;
        Path = None;
    }

    public bool IsEqualTo(Bitmap other) {
        if (other.Width != Width || other.Height != Height) {
            return false;
        }

        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                var pixel = GetPixel(IVector2.New(x, y)).Unwrap();
                var other_pixel = other.GetPixel(IVector2.New(x, y)).Unwrap();
                if (pixel != other_pixel) {
                    return false;
                }
            }
        }

        return true;
    }

    public void Save(string path) {
        using FileStream fs = new(path, FileMode.OpenOrCreate);
        var writer = new ImageWriter();
        writer.WritePng(Data, Width, Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, fs);
    }

    public void Empty(IVector2 position, IVector2 size) {
        var originalPosition = position;
        for (; position.X < size.X + originalPosition.X; position.X++) {
            for (; position.Y < size.Y + originalPosition.Y; position.Y++)
                SetPixel(position, new Color());

            position.Y = 0;
        }
    }

    public Bitmap Clone() {
        var clonedBitmap = Bitmap.Empty(Width, Height);

        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                var pixel_pos = IVector2.New(x, y);
                clonedBitmap.SetPixel(pixel_pos, GetPixel(pixel_pos).Unwrap());
            }
        }

     ; return clonedBitmap;
    }

    public Bitmap ClonePart(IVector2 position, IVector2 size) {
        var clonedBitmap = Bitmap.Empty(size.X, size.Y);

        for (int x = 0; x < size.X; x++) {
            for (int y = 0; y < size.Y; y++) {
                var offset = new IVector2(x, y);
                GetPixel(offset + position).IfSome((p) => clonedBitmap.SetPixel(offset, p));
            }
        }

        return clonedBitmap;
    }

    public void Draw(Bitmap bitmap, IVector2 position) {
        for (int x = 0; x < bitmap.Width; x++) {
            for (int y = 0; y < bitmap.Height; y++) {
                var offset = new IVector2(x, y);
                bitmap.GetPixel(offset).IfSome((p) => SetPixel(position + offset, p));
            }
        }
    }
    public static int Array2DPositionToIndex(IVector2 position, int width) =>
        position.Y * width + position.X;

    public static IVector2 IndexToArray2DPosition(int index, int width) {
        int x = index % width;
        int y = (index - x) / width;

        return new IVector2(x, y);
    }

    public Option<Color> GetPixel(IVector2 position) {
        if (position.X < 0 || position.Y < 0 || position.X >= Width || position.Y >= Height)
            return None;

        var index = Array2DPositionToIndex(position, Width);

        return new Color(
            Data[index * 4],
            Data[index * 4 + 1],
            Data[index * 4 + 2],
            Data[index * 4 + 3]);
    }

    public void SetPixel(IVector2 position, Color Color) {
        if (position.X < 0 || position.Y < 0 || position.X >= Width || position.Y >= Height)
            return;

        var index = Array2DPositionToIndex(position, Width);

        Data[index * 4] = Color.r;
        Data[index * 4 + 1] = Color.g;
        Data[index * 4 + 2] = Color.b;
        Data[index * 4 + 3] = Color.a;
    }
}