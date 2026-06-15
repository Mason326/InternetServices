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
    /// Форма "Сжатие изображения" - инструмент для уменьшения размера изображений
    /// Позволяет:
    /// - Просматривать исходное и сжатое изображение
    /// - Выбирать степень сжатия (от Ультра до Минимального)
    /// - Видеть исходный и итоговый размер файла
    /// - Сохранять сжатое изображение или отменять операцию
    /// </summary>
    public partial class ImageCompressionWindow : Window
    {
        // Словарь соответствия: название степени сжатия -> значение качества (1-100)
        // Чем меньше значение, тем сильнее сжатие и хуже качество
        Dictionary<string, int> compressionDegreeDict = new Dictionary<string, int>();

        /// <summary>
        /// Конструктор формы - инициализация компонентов, загрузка исходного изображения,
        /// настройка списка степеней сжатия
        /// </summary>
        public ImageCompressionWindow()
        {
            InitializeComponent();

            // Загрузка исходного изображения из статического хранилища ImageHolder
            srcImage.Source = ImageHolder.sourceImage;

            // Настройка степеней сжатия (значения качества от 10 до 70)
            compressionDegreeDict.Add("Ультра", 10);        // Максимальное сжатие (10)
            compressionDegreeDict.Add("Максимальный", 20);
            compressionDegreeDict.Add("Сильный", 30);
            compressionDegreeDict.Add("Нормальный", 40);
            compressionDegreeDict.Add("Быстрый", 50);
            compressionDegreeDict.Add("Легкий", 60);
            compressionDegreeDict.Add("Минимальный", 70);   // Минимальное сжатие (70) - по умолчанию

            compressionDegreeComboBox.ItemsSource = compressionDegreeDict.Keys;
            compressionDegreeComboBox.SelectedItem = "Минимальный";

            // Отображение исходного размера файла
            var sizeActual = File.ReadAllBytes(ImageHolder.sourcePath).Length;
            FormatSize(sourceSizeLabel, sizeActual);
        }

        /// <summary>
        /// Форматирование размера файла в удобочитаемый вид (Б, КБ, МБ, ГБ)
        /// </summary>
        /// <param name="label">Label для отображения результата</param>
        /// <param name="size">Размер в байтах</param>
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
            // Если размер меньше 1 КБ, остается как есть (в байтах)
        }

        /// <summary>
        /// Получение фактического размера BitmapImage в байтах
        /// </summary>
        public static long GetActualBitmapImageSize(BitmapImage bitmapImage)
        {
            // Если изображение из потока - получаем длину потока
            if (bitmapImage.StreamSource != null && bitmapImage.StreamSource.CanSeek)
            {
                return bitmapImage.StreamSource.Length;
            }

            // Иначе кодируем изображение в соответствующий формат
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

        /// <summary>
        /// Сжатие изображения с заданным качеством
        /// </summary>
        /// <param name="sourceImagePath">Путь к исходному изображению</param>
        /// <param name="quality">Качество сжатия (1-100, где 1 - худшее качество, 100 - лучшее)</param>
        /// <returns>MemoryStream с сжатым изображением</returns>
        public static MemoryStream CompressImage(string sourceImagePath, long quality)
        {
            try
            {
                using (Bitmap sourceImage = new Bitmap(sourceImagePath))
                {
                    string ext = System.IO.Path.GetExtension(sourceImagePath);
                    ImageCodecInfo imageEncoder;

                    // Выбор кодека в зависимости от формата файла
                    switch (ext)
                    {
                        case ".png":
                            imageEncoder = GetEncoder(ImageFormat.Png);
                            break;
                        case ".jpg":
                            imageEncoder = GetEncoder(ImageFormat.Jpeg);
                            break;
                        default:
                            return null;  // Неподдерживаемый формат
                    }

                    // Настройка параметров сжатия
                    EncoderParameters encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

                    MemoryStream memoryStream = new MemoryStream();
                    sourceImage.Save(memoryStream, imageEncoder, encoderParameters);
                    memoryStream.Position = 0;  // Сброс позиции для чтения
                    return memoryStream;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось сжать картинку\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return null;
            }
        }

        /// <summary>
        /// Получение кодера изображения для указанного формата
        /// </summary>
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

        /// <summary>
        /// Конвертация MemoryStream в BitmapImage для отображения в WPF
        /// </summary>
        public static BitmapImage ConvertToBitmapImage(MemoryStream stream)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;  // Загрузка сразу в память
            bitmapImage.EndInit();
            bitmapImage.Freeze();  // Заморозка для безопасного использования в UI
            return bitmapImage;
        }

        /// <summary>
        /// Кнопка "Сжать" - выполнение сжатия изображения с выбранной степенью
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получение значения качества для выбранной степени сжатия
                int getValue;
                bool compressionLevel = compressionDegreeDict.TryGetValue(compressionDegreeComboBox.SelectedItem.ToString(), out getValue);

                // Сжатие изображения
                MemoryStream compressedImageStream = CompressImage(ImageHolder.sourcePath, Convert.ToInt64(getValue));
                BitmapImage compressedImage = ConvertToBitmapImage(compressedImageStream);
                byte[] bitmapBytes = ImageHolder.GetBitmapImageBytes(compressedImage);

                // Отображение сжатого изображения и его размера
                destImage.Source = compressedImage;
                FormatSize(destinationSizeLabel, Convert.ToDouble(bitmapBytes.Length));

                MessageBox.Show("Изображение успешно сжато", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось сжать картинку\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Кнопка "Отмена" - закрытие формы без сохранения
        /// Устанавливает флаг отмены в ImageHolder
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ImageHolder.isCanceled = true;
            this.Close();
        }

        /// <summary>
        /// Событие загрузки формы - отображение роли пользователя в заголовке
        /// </summary>
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

        /// <summary>
        /// Кнопка "Сохранить" - сохранение сжатого изображения и закрытие формы
        /// Сохраняет изображение в статическом хранилище ImageHolder
        /// </summary>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ImageHolder.destinationImage = destImage.Source as BitmapImage;
            this.Close();
        }
    }
}