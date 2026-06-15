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
    /// Форма "Настройки подключения" - управление параметрами подключения к базе данных
    /// Позволяет:
    /// - Настроить сервер, логин и пароль для подключения к MySQL
    /// - Проверить соединение с указанными параметрами
    /// - Сохранить настройки в локальное хранилище приложения
    /// - Перезапустить приложение после изменения настроек
    /// </summary>
    public partial class Settings : Window
    {
        // Массив для передачи сигнала о необходимости перезапуска приложения родительскому окну
        object[] closeApp;

        /// <summary>
        /// Конструктор формы - инициализация компонентов
        /// </summary>
        /// <param name="closeAppSignal">Массив, в который будет записан флаг перезапуска</param>
        public Settings(object[] closeAppSignal)
        {
            closeApp = closeAppSignal;
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка закрытия формы
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Кнопка "Проверить соединение" - асинхронная проверка возможности подключения к БД
        /// </summary>
        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            string server = ServerTextbox.Text.Trim();
            string user = UserTextbox.Text.Trim();
            string password = PasswordTextbox.Password.Trim();
            string db = Properties.Settings.Default.database;

            // Проверка заполнения обязательных полей
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(user))
            {
                MessageBox.Show($"Необходимо заполнить поля помеченные \"*\"", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Попытка асинхронного подключения к базе данных
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

        /// <summary>
        /// Асинхронное подключение к MySQL базе данных
        /// </summary>
        /// <param name="connectionString">Строка подключения</param>
        /// <returns>Кортеж (сообщение, успешность подключения)</returns>
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
                catch (MySqlException exc)
                {
                    // Обработка различных кодов ошибок MySQL
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

        /// <summary>
        /// Событие загрузки формы - загрузка сохраненных настроек подключения
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Загрузка сохраненных данных из настроек приложения
            ServerTextbox.Text = Properties.Settings.Default.server;
            UserTextbox.Text = Properties.Settings.Default.user;
            PasswordTextbox.Password = Properties.Settings.Default.password;
        }

        /// <summary>
        /// Кнопка "Сохранить изменения" - сохранение настроек подключения
        /// </summary>
        private void SaveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            string server = ServerTextbox.Text.Trim();
            string user = UserTextbox.Text.Trim();
            string password = PasswordTextbox.Password.Trim();

            // Проверка заполнения обязательных полей
            if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(user))
            {
                MessageBox.Show($"Необходимо заполнить поля помеченные \"*\"", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Сохранение настроек в локальное хранилище
            Properties.Settings.Default.server = server;
            Properties.Settings.Default.user = user;
            Properties.Settings.Default.password = password;
            Properties.Settings.Default.Save();

            MessageBox.Show($"Изменения успешно сохранены. Необходима перезагрузка", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            // Сигнал родительскому окну о необходимости перезапуска приложения
            closeApp[0] = true;
            this.Close();
        }
    }
}