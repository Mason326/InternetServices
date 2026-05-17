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
    }
}
