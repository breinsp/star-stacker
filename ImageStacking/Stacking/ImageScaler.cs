using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace ImageStacking.Stacking
{
    public class ImageScaler
    {
        /// <summary>
        /// Scales second image to fit on first image
        /// </summary>
        /// <param name="image1"></param>
        /// <param name="image2"></param>
        /// <returns></returns>
        public static Image HomographyTransform(Image image1, Image image2)
        {
            List<Point2d> src = null;
            List<Point2d> dst = null;
            GetFixatingPoints2D(image1, image2, out src, out dst);

            Mat homography = Cv2.FindHomography(src, dst);

            List<Point2d> srcPoints = image1.GetPoint2Ds();
            Point2d[] dstPoints = Cv2.PerspectiveTransform(srcPoints, homography);

            Image result = new Image(image1.Width, image1.Height);

            for (int x = 0; x < result.Width; x++)
            {
                for (int y = 0; y < result.Height; y++)
                {
                    int index = x * result.Height + y;
                    Point2d point = dstPoints[index];

                    int x_trunc = (int)point.X;
                    int y_trunc = (int)point.Y;
                    float x_lerp = (float)point.X - x_trunc;
                    float y_lerp = (float)point.Y - y_trunc;


                    result.Pixels[x, y] = InterpPixel(image2, x_trunc, y_trunc, x_lerp, y_lerp); ;
                }
            }

            return result;
        }

        public static List<Point2d> ToPoint2d(List<Point> points)
        {
            var list = new List<Point2d>();
            foreach (var p in points) list.Add(new Point2d(p.x, p.y));
            return list;
        }

        public static Pixel InterpPixel(Image image, int x_trunc, int y_trunc, float x_lerp, float y_lerp)
        {

            Pixel x1 = image.GetPixelAt(x_trunc, y_trunc);
            Pixel x2 = image.GetPixelAt(x_trunc + 1, y_trunc);
            Pixel x12 = x1 * (1f - x_lerp) + x2 * x_lerp;

            Pixel x3 = image.GetPixelAt(x_trunc, y_trunc + 1);
            Pixel x4 = image.GetPixelAt(x_trunc + 1, y_trunc + 1);
            Pixel x34 = x3 * (1f - x_lerp) + x4 * x_lerp;

            return x12 * (1 - y_lerp) + x34 * y_lerp;
        }

        public static int GetClosestPoint(Image image, List<Point> points, int x, int y)
        {
            Point min = null;
            float minDelta = float.MaxValue;

            for (int i = 0; i < points.Count; i++)
            {
                Point p = points[i];
                if (p == null) continue;
                int xd = Math.Abs(x - p.x);
                int yd = Math.Abs(y - p.y);
                int delta = xd * xd + yd * yd;

                if (xd > image.Width / 4 || yd > image.Height / 4) continue;

                Point n = new Point { x = p.x, y = p.y };

                if (delta < minDelta && !p.used)
                {
                    minDelta = delta;
                    min = p;
                }
            }

            if (min != null)
            {
                min.used = true;
                return min.index;
            }
            throw new Exception("point is null");
        }

        private static void GetFixatingPoints2D(Image image1, Image image2, out List<Point2d> src, out List<Point2d> dst)
        {
            int TopLeft = FindBestPoint(image1, image2, image1.TopLeft, image2.TopLeft);
            int TopRight = FindBestPoint(image1, image2, image1.TopRight, image2.TopRight);
            int BottomLeft = FindBestPoint(image1, image2, image1.BottomLeft, image2.BottomLeft);
            int BottomRight = FindBestPoint(image1, image2, image1.BottomRight, image2.BottomRight);

            src = new List<Point2d>();
            dst = new List<Point2d>();

            Point p1tl = image1.TopLeft[TopLeft];
            Point p1tr = image1.TopRight[TopRight];
            Point p1bl = image1.BottomLeft[BottomLeft];
            Point p1br = image1.BottomRight[BottomRight];

            Point p2tl = image2.TopLeft[TopLeft];
            Point p2tr = image2.TopRight[TopRight];
            Point p2bl = image2.BottomLeft[BottomLeft];
            Point p2br = image2.BottomRight[BottomRight];

            Console.Write(p1tl - p2tl + "\t");
            Console.Write(p1tr - p2tr + "\t");
            Console.Write(p1bl - p2bl + "\t");
            Console.Write(p1br - p2br + "\t");
            Console.WriteLine();

            src.Add(new Point2d(p1tl.x, p1tl.y));
            src.Add(new Point2d(p1tr.x, p1tr.y));
            src.Add(new Point2d(p1bl.x, p1bl.y));
            src.Add(new Point2d(p1br.x, p1br.y));

            dst.Add(new Point2d(p2tl.x, p2tl.y));
            dst.Add(new Point2d(p2tr.x, p2tr.y));
            dst.Add(new Point2d(p2bl.x, p2bl.y));
            dst.Add(new Point2d(p2br.x, p2br.y));

            Console.WriteLine(p1tl.index + " - " + p2tl.index);
            Console.WriteLine(p1tr.index + " - " + p2tr.index);
            Console.WriteLine(p1bl.index + " - " + p2bl.index);
            Console.WriteLine(p1br.index + " - " + p2br.index);

        }

        private static int FindBestPoint(Image image1, Image image2, Point[] fix1, Point[] fix2)
        {
            List<int> indexes = new List<int>();
            for (int i = 0; i < ImageProcessor.SAMPLE_COUNT; i++)
            {
                if (fix1[i] != null && fix2[i] != null)
                {
                    fix1[i].index = i;
                    fix2[i].index = i;
                    indexes.Add(i);
                }
            }
            if (indexes.Count == 0) throw new Exception("no point found");
            return indexes.OrderBy(i => fix2[i].delta).First();
        }
    }
}
