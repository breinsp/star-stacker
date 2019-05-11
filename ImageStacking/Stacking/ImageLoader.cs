using System;
using System.Drawing;

namespace ImageStacking.Stacking
{
    public class ImageLoader
    {
        public static Image LoadImage(string fileName)
        {
            Bitmap bitmap = new Bitmap(fileName);
            Image image = new Image(bitmap.Width, bitmap.Height);
            image.Filename = fileName;

            for (int x = 0; x < bitmap.Width; x++)
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    Color color = bitmap.GetPixel(x, y);
                    Pixel pixel = new Pixel(color);
                    image.Pixels[x, y] = pixel;
                }
            }
            Console.WriteLine("Image " + fileName + " loaded");
            return image;
        }

        public static void WriteImage(string fileName, Image image)
        {
            Bitmap bitmap = new Bitmap(image.Width, image.Height);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Pixel p = image.GetPixelAt(x, y);
                    Color color = p == null ? Color.Pink : p.GetColor();
                    bitmap.SetPixel(x, y, color);
                }
            }
            Console.WriteLine("Image " + fileName + " saved");
            bitmap.Save(fileName);
        }
    }
}
