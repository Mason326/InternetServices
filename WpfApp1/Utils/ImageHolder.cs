using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WpfApp1.Utils
{
    public class ImageHolder
    {
        private static string currentDirectory = string.Join("\\", Directory.GetCurrentDirectory().Split('\\').TakeWhile(item => item != "bin"));
        public static string sourcePath = $"{currentDirectory}\\Resources\\8782201770.jpg";
        public static BitmapImage sourceImage = new BitmapImage(new Uri(sourcePath));
        public static BitmapImage destinationImage = new BitmapImage(new Uri($"{currentDirectory}\\Resources\\8782201770.jpg"));

        public static void BackToDefaultValues()
        {
            sourcePath = $"{currentDirectory}\\Resources\\8782201770.jpg";
            sourceImage = new BitmapImage(new Uri(sourcePath));
            destinationImage = new BitmapImage(new Uri($"{currentDirectory}\\Resources\\8782201770.jpg"));
        }

        public static byte[] GetBitmapImageBytes(BitmapImage bitmapImage)
        {
            byte[] result;
            string ext = Path.GetExtension(sourcePath);
            switch (ext)
            {
                case ".png":
                    PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
                    pngEncoder.Frames.Add(BitmapFrame.Create(bitmapImage));

                    using (MemoryStream stream = new MemoryStream())
                    {
                        pngEncoder.Save(stream);
                        result = stream.ToArray();
                    }

                    return result;
                case ".jpg":
                    JpegBitmapEncoder jpegEncoder = new JpegBitmapEncoder();
                    jpegEncoder.Frames.Add(BitmapFrame.Create(bitmapImage));

                    using (MemoryStream stream = new MemoryStream())
                    {
                        jpegEncoder.Save(stream);
                        result = stream.ToArray();
                    }

                    return result;
                default:
                    return new byte[0];
            }
        }

        //public static bool isDefault(BitmapImage image)
        //{
        //    if(image.UriSource != null )
        //    return false;
        //}
    }
}
