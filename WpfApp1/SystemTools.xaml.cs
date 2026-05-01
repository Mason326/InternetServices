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
    /// Interaction logic for SystemTools.xaml
    /// </summary>
    public partial class SystemTools : Window
    {
        public SystemTools()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                string pathToFile = Backup.MakeABackup();
                MessageBox.Show($"Резервная копия создана по пути: {pathToFile}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось создать резервную копию\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Title = "Выберите файл восстановления базы данных";
                dialog.Filter = "SQL-скрипты (*.sql)|*.sql";
                dialog.ShowDialog();
                if (dialog.FileName != string.Empty)
                {
                    string filePath = dialog.FileName;
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
                                    mb.ImportFromFile(filePath);
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

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            var win = new ExportImportForm();
            win.ShowDialog();
        }
    }
}
