using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageStacking.Stacking
{
    public class Image
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public Pixel[,] Pixels { get; set; }
        public Point[] TopLeft { get; set; }
        public Point[] TopRight { get; set; }
        public Point[] BottomLeft { get; set; }
        public Point[] BottomRight { get; set; }
        public int Id { get; set; }
        public string Filename { get; set; }



        public Image(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new Pixel[width, height];
        }

        public Pixel GetPixelAt(int x, int y)
        {
            if (x >= 0 && y >= 0 && x < Width && y < Height)
            {
                return Pixels[x, y];
            }
            return null;
        }

        public List<Point2d> GetPoint2Ds()
        {
            List<Point2d> points = new List<Point2d>();
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    points.Add(new Point2d(i, j));
                }
            }
            return points;
        }
    }

    public class Pixel
    {
        public byte r;
        public byte g;
        public byte b;

        public Pixel(Color color)
        {
            r = color.R;
            g = color.G;
            b = color.B;
        }

        public Pixel(byte r, byte g, byte b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public Color GetColor()
        {
            return Color.FromArgb(255, r, g, b);
        }

        public byte GetBrightness()
        {
            return (byte)((r + g + b) / 3);
        }

        public float GetLuma()
        {
            float R = (int)r;
            float G = (int)g;
            float B = (int)b;
            return 0.2126f * R + 0.7152f * G + 0.0722f * B;
        }

        public static Pixel operator +(Pixel A, Pixel B)
        {
            if (A == null || B == null) return null;
            return new Pixel((byte)(A.r + B.r), (byte)(A.g + B.g), (byte)(A.b + B.b));
        }

        public static Pixel operator *(Pixel A, float factor)
        {
            if (A == null) return null;
            return new Pixel((byte)(A.r * factor), (byte)(A.g * factor), (byte)(A.b * factor));
        }
    }

    public class Point
    {
        public int x;
        public int y;
        public Pixel pixel;
        public float delta;
        public int index;
        public bool used;

        public override bool Equals(object obj)
        {
            return this.GetHashCode() == obj.GetHashCode();
        }

        public override int GetHashCode()
        {
            return x + 10000 * y;
        }

        public static Point operator -(Point a, Point b)
        {
            return new Point { x = a.x - b.x, y = a.y - b.y };
        }

        public override string ToString()
        {
            return "x: " + x + ", y:" + y;
        }
    }
}
