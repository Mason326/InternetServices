using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
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
    /// Форма "Системные инструменты" - утилиты для администрирования базы данных
    /// Предоставляет доступ к следующим функциям:
    /// - Создание резервной копии базы данных (Backup)
    /// - Восстановление базы данных из резервной копии (Restore)
    /// - Импорт данных из CSV-файла в таблицы
    /// - Экспорт данных из таблиц в CSV-файл
    /// Доступна только для служебной учетной записи (serviceLogin/servicePassword)
    /// </summary>
    public partial class SystemTools : Window
    {
        /// <summary>
        /// Конструктор формы - инициализация компонентов
        /// </summary>
        public SystemTools()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка "На главную" - закрытие формы
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Кнопка "Создать резервную копию БД" - экспорт всей базы данных в SQL-файл
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                string pathToFile = Backup.MakeABackup();  // Создание резервной копии
                MessageBox.Show($"Резервная копия создана по пути: {pathToFile}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось создать резервную копию\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Кнопка "Восстановить БД из копии" - импорт SQL-файла для восстановления базы данных
        /// </summary>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                // Диалог выбора SQL-файла для восстановления
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "Выберите файл восстановления базы данных";
                dialog.Filter = "SQL-скрипты (*.sql)|*.sql";
                dialog.ShowDialog();

                if (dialog.FileName != string.Empty)
                {
                    string filePath = dialog.FileName;
                    // Подтверждение операции (действие необратимо)
                    MessageBoxResult res = MessageBox.Show($"Вы уверены что хотите восстановить базу данных из файла: \"{filePath}\"? Действие не может быть отменено", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

                    if (res == MessageBoxResult.Yes)
                    {
                        using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                        {
                            using (MySqlCommand cmd = conn.CreateCommand())
                            {
                                using (MySqlBackup mb = new MySqlBackup(cmd))
                                {
                                    conn.Open();
                                    mb.ImportFromFile(filePath);  // Восстановление БД из файла
                                }
                            }
                        }
                        MessageBox.Show("База данных успешно восстановлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось восстановить базу данных\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Кнопка "Импорт данных" - открытие формы импорта данных из CSV-файла
        /// </summary>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new ExportImportForm(DataManagement.DataOperationType.Import);
            win.ShowDialog();
            this.ShowDialog();
        }

        /// <summary>
        /// Кнопка "Экспорт данных" - открытие формы экспорта данных в CSV-файл
        /// </summary>
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new ExportImportForm(DataManagement.DataOperationType.Export);
            win.ShowDialog();
            this.ShowDialog();
        }
    }
}