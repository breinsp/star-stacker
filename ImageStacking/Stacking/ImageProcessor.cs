using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageStacking.Stacking
{
    public enum StackingType
    {
        Median,
        Mean
    }
    public class ImageProcessor
    {
        public const float CORNER_AREA_SIZE = 0.4f;
        public const int PIXEL_RADIUS = 4;
        public const float SEARCH_RADIUS = 0.03f;
        public const float ANGLE_THRESHOLD = 15;
        public const int SAMPLE_COUNT = 3 * 3; // n² (4 samples per corner)
        public const StackingType STACKING_TYPE = StackingType.Mean;

        public static Image TransformImage(Image targetImage, Image image, out bool valid)
        {
            if (targetImage.TopLeft == null)
                throw new Exception("First Image doesn't have fixating points");
            try
            {
                FindNearestPoints(targetImage, image);
                valid = true; //TODO validate
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("no points found");
                Console.ForegroundColor = ConsoleColor.White;
                valid = false;
            }

            if (valid)
            {
                Image transformed = ImageScaler.HomographyTransform(targetImage, image);
                return transformed;
            }
            return targetImage;
        }

        public static void FindCornerPoints(Image image)
        {
            double sample = Math.Sqrt(SAMPLE_COUNT);
            sample = sample - (int)sample;
            if (sample != 0) throw new Exception("Sample Count must be n²");

            int count = (int)Math.Sqrt(SAMPLE_COUNT);

            int padding = (int)Math.Round((image.Width + image.Height) / 2 * SEARCH_RADIUS);

            int x = (int)(image.Width * CORNER_AREA_SIZE);
            int y = (int)(image.Height * CORNER_AREA_SIZE);

            int sx = image.Width;
            int sy = image.Height;

            image.TopLeft = FindCornerPoints(image, 0, x, 0, y, count, padding);
            image.TopRight = FindCornerPoints(image, sx - x, sx, 0, y, count, padding);
            image.BottomLeft = FindCornerPoints(image, 0, x, sy - y, sy, count, padding);
            image.BottomRight = FindCornerPoints(image, sx - x, sx, sy - y, sy, count, padding);
        }

        public static Point[] FindCornerPoints(Image image, int xmin, int xmax, int ymin, int ymax, int count, int padding)
        {
            Point[] points = new Point[count * count];
            xmin += padding;
            ymin += padding;
            xmax -= padding;
            ymax -= padding;
            int xspan = xmax - xmin;
            int yspan = ymax - ymin;

            int sizex = xspan / count;
            int sizey = yspan / count;

            int index = 0;

            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    int x = sizex * i + xmin;
                    int y = sizey * j + ymin;

                    points[index++] = FindCornerPoint(image, x, x + sizex, y, y + sizey);
                }
            }

            return points;
        }

        public static Point FindCornerPoint(Image image, int xmin, int xmax, int ymin, int ymax)
        {
            const int padding = 5;
            xmin += padding;
            ymin += padding;
            xmax -= padding;
            ymax -= padding;
            byte maxBrightness = 0;
            int max_i = 0;
            int max_j = 0;
            Pixel max_pixel = null;

            for (int i = xmin; i <= xmax; i++)
            {
                for (int j = ymin; j <= ymax; j++)
                {
                    var pixel = GetAverageInRadius(image, i, j, PIXEL_RADIUS);
                    if (pixel != null)
                    {
                        byte brightness = pixel.GetBrightness();

                        if (brightness > maxBrightness)
                        {
                            maxBrightness = brightness;
                            max_i = i;
                            max_j = j;
                            max_pixel = image.GetPixelAt(i, j);
                        }
                    }
                }
            }

            return new Point()
            {
                pixel = max_pixel,
                x = max_i,
                y = max_j
            };
        }

        public static void FindNearestPoints(Image targetImage, Image image)
        {
            image.TopLeft = FindNearestPoints(targetImage, image, targetImage.TopLeft);
            image.TopRight = FindNearestPoints(targetImage, image, targetImage.TopRight);
            image.BottomLeft = FindNearestPoints(targetImage, image, targetImage.BottomLeft);
            image.BottomRight = FindNearestPoints(targetImage, image, targetImage.BottomRight);
        }

        public static Point[] FindNearestPoints(Image targetImage, Image image, Point[] source)
        {
            int searchRadius = (int)Math.Round((image.Width + image.Height) / 2 * SEARCH_RADIUS);
            Point[] destination = new Point[SAMPLE_COUNT];
            for (int i = 0; i < SAMPLE_COUNT; i++)
            {
                var fix = source[i];
                if (fix == null) continue;

                var point = FindNearPoint(targetImage, image, fix.x, fix.y, searchRadius);
                if (point == null) continue;
                destination[i] = point;
            }
            return destination;
        }

        public static Point FindNearPoint(Image targetImage, Image image, int x, int y, int radius)
        {
            var target = GetArea(targetImage, x, y, PIXEL_RADIUS);

            float smallest_delta = float.MaxValue;
            int closest_i = 0;
            int closest_j = 0;
            Pixel closest_pixel = null;


            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    int xi = x + i;
                    int yj = y + j;

                    var area = GetArea(image, xi, yj, PIXEL_RADIUS);
                    float delta = GetAreaDelta(target, area);

                    if (delta < smallest_delta)
                    {
                        smallest_delta = delta;
                        closest_i = xi;
                        closest_j = yj;
                        closest_pixel = image.Pixels[xi, yj];
                    }
                }
            }


            if (smallest_delta > 1)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(smallest_delta + "   ");
                Console.ForegroundColor = ConsoleColor.White;
                return null;
            }
            Console.WriteLine(smallest_delta + "   ");

            return new Point()
            {
                pixel = closest_pixel,
                x = closest_i,
                y = closest_j,
                delta = smallest_delta
            };
        }

        public static float GetPointDelta(Image target, Image image, int x, int y)
        {
            return GetAreaDelta(GetArea(target, x, y, PIXEL_RADIUS), GetArea(image, x, y, PIXEL_RADIUS));
        }

        public static int[,,] GetArea(Image image, int x, int y, int radius)
        {
            int size = radius * 2 + 1;
            int[,,] area = new int[size, size, 3];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    int xi = x + i - radius;
                    int yj = y + j - radius;
                    Pixel p = image.GetPixelAt(xi, yj);
                    if (p != null)
                    {
                        area[i, j, 0] = p.r;
                        area[i, j, 1] = p.g;
                        area[i, j, 2] = p.b;
                    }
                }
            }
            return area;
        }

        public static float GetAreaDelta(int[,,] l1, int[,,] l2)
        {
            float sum = 0;
            int count = 0;
            for (int i = 0; i < l1.GetLength(0); i++)
            {
                for (int j = 0; j < l1.GetLength(1); j++)
                {
                    int r1 = l1[i, j, 0];
                    int g1 = l1[i, j, 1];
                    int b1 = l1[i, j, 2];

                    int r2 = l2[i, j, 0];
                    int g2 = l2[i, j, 1];
                    int b2 = l2[i, j, 2];

                    int rd = r1 - r2;
                    int gd = g1 - g2;
                    int bd = b1 - b2;

                    int delta = rd + gd + bd;
                    sum += delta / 3f;
                    count++;
                }
            }
            return Math.Abs(sum / count);
        }

        private class PointValidation
        {
            public float angle;
            public List<int> indexes;

            public PointValidation(float a, int index)
            {
                angle = a;
                indexes = new List<int>();
                indexes.Add(index);
            }
        }

        public static float GetAngleBetweenPoints(Point p1, Point p2)
        {
            float x = p2.x - p1.x;
            float y = p2.y - p1.y;

            double rad = Math.Atan2(y, x);
            double deg = rad * (180.0 / Math.PI);
            return (float)deg;
        }

        public static Pixel GetAverageInRadius(Image image, int x, int y, int radius)
        {
            int count = 0;
            int rsum = 0;
            int gsum = 0;
            int bsum = 0;

            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    int xi = x + i;
                    int yj = y + j;

                    Pixel pixel = image.GetPixelAt(xi, yj);
                    if (pixel != null)
                    {
                        rsum += pixel.r;
                        gsum += pixel.g;
                        bsum += pixel.b;
                        count++;
                    }
                }
            }

            if (count == 0) return null;

            byte r = (byte)Math.Max(Math.Min(rsum / (float)count, 255), 0);
            byte g = (byte)Math.Max(Math.Min(gsum / (float)count, 255), 0);
            byte b = (byte)Math.Max(Math.Min(bsum / (float)count, 255), 0);

            return new Pixel(r, g, b);
        }

        public static Image StackResult(List<Image> images)
        {
            if (images.Count == 0) throw new Exception("No images to stack");

            Console.WriteLine("Stacking images...");
            Stopwatch sw = Stopwatch.StartNew();

            Image first = images[0];
            Image result = new Image(first.Width, first.Height);

            for (int x = 0; x < result.Width; x++)
            {
                for (int y = 0; y < result.Height; y++)
                {
                    List<Pixel> pixels = images.Select(i => i.Pixels[x, y]).Where(p => p != null).ToList();

                    result.Pixels[x, y] = GetAggregate(pixels, STACKING_TYPE);
                }
            }
            Console.WriteLine("Images stacked in " + sw.ElapsedMilliseconds + " ms.");
            return result;
        }

        public static Pixel GetAggregate(List<Pixel> pixels, StackingType type)
        {
            if (type == StackingType.Mean)
            {
                return GetPixelAverage(pixels);
            }
            if (type == StackingType.Median)
            {
                pixels = pixels.OrderBy(p => p.GetLuma()).ToList();
                float indexf = pixels.Count / 2f;
                int index = (int)indexf;
                if (index != indexf)
                {
                    List<Pixel> input = new List<Pixel>();
                    input.Add(pixels[index]);
                    input.Add(pixels[index + 1]);
                    return GetPixelAverage(input);
                }
                else
                {
                    return pixels[index];
                }
            }
            return null;
        }

        public static Pixel GetPixelAverage(List<Pixel> pixels)
        {
            int rsum = 0;
            int gsum = 0;
            int bsum = 0;
            for (int i = 0; i < pixels.Count; i++)
            {
                rsum += pixels[i].r;
                gsum += pixels[i].g;
                bsum += pixels[i].b;
            }
            byte r = (byte)(rsum / (float)pixels.Count);
            byte g = (byte)(gsum / (float)pixels.Count);
            byte b = (byte)(bsum / (float)pixels.Count);
            return new Pixel(r, g, b);
        }

    }
}
