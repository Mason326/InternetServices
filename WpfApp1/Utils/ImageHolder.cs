using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WpfApp1.Utils
{
    /// <summary>
    /// Класс-держатель картинок
    /// </summary>
    public class ImageHolder
    {
        // Вычисление пути до проекта
        private static string currentDirectory = string.Join("\\", Directory.GetCurrentDirectory().Split('\\').TakeWhile(item => item != "bin"));
        public static string sourcePath = $"{currentDirectory}\\Resources\\8782201770.jpg";
        // Создаем BitmapImage из пути до картинки
        public static BitmapImage sourceImage = new BitmapImage(new Uri(sourcePath));
        public static BitmapImage destinationImage = new BitmapImage(new Uri($"{currentDirectory}\\Resources\\8782201770.jpg"));
        // Флаг отмены
        public static bool isCanceled = false;

        /// <summary>
        /// Метод сброса изменений при сжатии
        /// </summary>
        public static void BackToDefaultValues()
        {
            // Перезатирание стандартными значениями
            sourcePath = $"{currentDirectory}\\Resources\\8782201770.jpg";
            sourceImage = new BitmapImage(new Uri(sourcePath));
            destinationImage = new BitmapImage(new Uri($"{currentDirectory}\\Resources\\8782201770.jpg"));
            isCanceled = false;
        }

        /// <summary>
        /// Метод для получения байтов картинки
        /// </summary>
        /// <param name="bitmapImage">Передаваемая картинка</param>
        /// <returns>Массив байтов картинки</returns>
        public static byte[] GetBitmapImageBytes(BitmapImage bitmapImage)
        {
            byte[] result;
            // Получение расширения файла
            string ext = Path.GetExtension(sourcePath);
            switch (ext)
            {
                case ".png":
                    // Кодировка PNG картинок
                    PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
                    pngEncoder.Frames.Add(BitmapFrame.Create(bitmapImage));

                    // Сохраняем в памяти
                    using (MemoryStream stream = new MemoryStream())
                    {
                        pngEncoder.Save(stream);
                        result = stream.ToArray();
                    }

                    return result;
                case ".jpg":
                    // Кодировка JPEG картинок
                    JpegBitmapEncoder jpegEncoder = new JpegBitmapEncoder();
                    jpegEncoder.Frames.Add(BitmapFrame.Create(bitmapImage));

                    // Сохраняем в памяти
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
    }
}
