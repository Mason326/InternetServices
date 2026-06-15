using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace WpfApp1.Utils
{
    /// <summary>
    /// Класс загрузки картинок
    /// </summary>
    public class ImageUtils
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageData">Массив байтов картинки</param>
        /// <returns>Экземпляр BitmapImage</returns>
        public static BitmapImage LoadImage(byte[] imageData)
        {
            // Если не нашли картинку, то загружаем стандартную
            if (imageData == null || imageData.Length == 0) return new BitmapImage(new Uri("pack://application:,,,/Resources/Images/user.png"));
            // Создаем новый BitmapImage
            var image = new BitmapImage();
            // Загружаем байты картинки в память
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                // Делаем поток из памяти источником данных
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            // Возвращаем картинку
            return image;
        }

        /// <summary>
        /// Метод безопасно загружает картинку как источник для элемента на форме
        /// </summary>
        /// <param name="userImage">Элемент формы Image</param>
        public static void LoadUserImage(Image userImage)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                conn.Open();
                // Пытаемся получить фото пользователя
                MySqlCommand cmd = new MySqlCommand($"SELECT photo FROM employees WHERE idemployees = {AccountHolder.userId};", conn);
                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    // Если фото есть
                    if (dr.Read())
                    {
                        // Получаем байты
                        byte[] imageBytes = dr.GetValue(0) as byte[];
                        if (imageBytes != null && imageBytes.Length > 0)
                        {
                            // Безопасно загружаем картинки
                            userImage.Source = ImageUtils.LoadImage(imageBytes);
                        }
                    }
                }
            }
        }
    }
}
