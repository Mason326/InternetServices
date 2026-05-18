using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfApp1.Utils;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for ImageCompressionWindow.xaml
    /// </summary>
    public partial class ImageCompressionWindow : Window
    {
        Dictionary<string, int> compressionDegreeDict = new Dictionary<string, int>();
        public ImageCompressionWindow()
        {
            InitializeComponent();
            srcImage.Source = ImageHolder.sourceImage;
            compressionDegreeDict.Add("Ультра", 10);
            compressionDegreeDict.Add("Максимальный", 20);
            compressionDegreeDict.Add("Сильный", 30);
            compressionDegreeDict.Add("Нормальный", 40);
            compressionDegreeDict.Add("Быстрый", 50);
            compressionDegreeDict.Add("Легкий", 60);
            compressionDegreeDict.Add("Минимальный", 70);
            compressionDegreeComboBox.ItemsSource = compressionDegreeDict.Keys;
            compressionDegreeComboBox.SelectedItem = "Минимальный";
            var sizeActual = File.ReadAllBytes(ImageHolder.sourcePath).Length;
            FormatSize(sourceSizeLabel, sizeActual);
        }

        private void FormatSize(Label label, double size)
        {
            int kiloByteBlock = 1024;
            int megaByteBlock = 1024 * 1024;
            int gigaByteBlock = 1024 * 1024 * 1024;

            if ((size / gigaByteBlock) >= 1)
                label.Content = $"{Math.Round(size / gigaByteBlock, 2)} ГБ";
            else if ((size / megaByteBlock) >= 1)
                label.Content = $"{Math.Round(size / megaByteBlock, 2)} МБ";
            else if ((size / kiloByteBlock) >= 1)
                label.Content = $"{Math.Ceiling(size / kiloByteBlock)} КБ";
        }

        public static long GetActualBitmapImageSize(BitmapImage bitmapImage)
        {
            if (bitmapImage.StreamSource != null && bitmapImage.StreamSource.CanSeek)
            {
                return bitmapImage.StreamSource.Length;
            }

            byte[] result;
            string ext = System.IO.Path.GetExtension(bitmapImage.UriSource.AbsoluteUri);
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

                    return result.Length;
                case ".jpg":
                    JpegBitmapEncoder jpegEncoder = new JpegBitmapEncoder();
                    jpegEncoder.Frames.Add(BitmapFrame.Create(bitmapImage));

                    using (MemoryStream stream = new MemoryStream())
                    {
                        jpegEncoder.Save(stream);
                        result = stream.ToArray();
                    }

                    return result.Length;
                default:
                    return 0;
            }
        }


        public static MemoryStream CompressImage(string sourceImagePath, long quality)
        {
            try
            {
                using (Bitmap sourceImage = new Bitmap(sourceImagePath))
                {
                    string ext = System.IO.Path.GetExtension(sourceImagePath);
                    ImageCodecInfo imageEncoder;
                    switch (ext)
                    {
                        case ".png":
                            imageEncoder = GetEncoder(ImageFormat.Png);
                            break;
                        case ".jpg":
                            imageEncoder = GetEncoder(ImageFormat.Jpeg);
                            break;
                        default:
                            return null;
                    }

                    EncoderParameters encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                    MemoryStream memoryStream = new MemoryStream();
                    sourceImage.Save(memoryStream, imageEncoder, encoderParameters);
                    memoryStream.Position = 0;
                    return memoryStream;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось сжать картинку\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public static BitmapImage ConvertToBitmapImage(MemoryStream stream)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            return bitmapImage;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int getValue;
                bool compressionLevel = compressionDegreeDict.TryGetValue(compressionDegreeComboBox.SelectedItem.ToString(), out getValue);
                MemoryStream compressedImageStream = CompressImage(ImageHolder.sourcePath, Convert.ToInt64(getValue));
                BitmapImage compressedImage = ConvertToBitmapImage(compressedImageStream);
                byte[] bitmapBytes = ImageHolder.GetBitmapImageBytes(compressedImage);
                destImage.Source = compressedImage;
                FormatSize(destinationSizeLabel, Convert.ToDouble(bitmapBytes.Length));
                MessageBox.Show("Изображение успешно сжато", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось сжать картинку\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ImageHolder.isCanceled = true;
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Title += $" ({AccountHolder.UserRole}: {FullNameSplitter.MakeShortName(AccountHolder.FIO)})";
            }
            catch
            {
                ;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ImageHolder.destinationImage = destImage.Source as BitmapImage;
            this.Close();
        }
    }
}
