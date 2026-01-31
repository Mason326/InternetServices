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
using MySql.Data.MySqlClient;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            string server = ServerTextbox.Text.Trim();
            string user = UserTextbox.Text.Trim();
            string password = PasswordTextbox.Password.Trim();
            string db = Properties.Settings.Default.database;

            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(user))
            {
                MessageBox.Show($"Необходимо заполнить поля помеченные \"*\"", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var connectWithDB = ConnectToMySqlAsync($"server={server};user={user};password={password};database={db}");
                (string responseMessage, bool isEstablished) = await connectWithDB;
                string title = isEstablished ? "Успех" : "Внимание";
                var messageImage = isEstablished ? MessageBoxImage.Information : MessageBoxImage.Warning;
                MessageBox.Show(responseMessage, title, MessageBoxButton.OK, messageImage);
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения: {exc.Message}", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async Task<(string, bool)> ConnectToMySqlAsync(string connectionString)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                var taskConnect = conn.OpenAsync();
                string message = "";
                try
                {
                    await taskConnect;
                }
                catch(MySqlException exc)
                {
                    switch (exc.Number)
                    {
                        case 0:
                            message = "Указанный сервер недоступен";
                            break;
                        case 1042:
                            message = "Адрес сервера не удалось разрешить";
                            break;
                        case 1045:
                            message = $"Некорректные данные пользователя";
                            break;
                        case 1049:
                            message = $"База данных не найдена. Авторизуйтесь администратором системы для создания базы данных и импорта данных";
                            break;
                        default:
                            message = $"Ошибка подключения: {exc.Message}";
                            break;
                    }
                    return (message, false);
                }
                message = "Подключение установлено успешно";
                return (message, true);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ServerTextbox.Text = Properties.Settings.Default.server;
            UserTextbox.Text = Properties.Settings.Default.user;
            PasswordTextbox.Password = Properties.Settings.Default.password;
        }
    }
}
