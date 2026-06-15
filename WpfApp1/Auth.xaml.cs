using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using System.Windows.Threading;
using WpfApp1.Utils;

namespace WpfApp1
{
    /// <summary>
    /// Форма авторизации пользователя - точка входа в приложение
    /// Обеспечивает аутентификацию с проверкой логина и пароля,
    /// включает защиту от подбора пароля (CAPTCHA и блокировка после неудачных попыток)
    /// </summary>
    public partial class Auth : Window
    {
        // Счетчик неудачных попыток авторизации
        int authAttempsCounter = 0;
        // Текст CAPTCHA для сравнения с вводом пользователя
        string captchaCompare = "";
        // Флаг блокировки навигации между окнами
        public static bool locker = true;

        /// <summary>
        /// Конструктор формы - инициализация компонентов, запуск таймера резервного копирования
        /// </summary>
        public Auth()
        {
            InitializeComponent();
            ShowCaptcha(authAttempsCounter);
            DispatcherTimer timer = new DispatcherTimer();
            // Резервное копирование каждые 3 часа (180 минут)
            timer.Interval = TimeSpan.FromMinutes(180);
            timer.Tick += new EventHandler(MakeABackupEvent);
            timer.Start();
        }

        /// <summary>
        /// Возврат к форме авторизации - закрытие всех дочерних окон
        /// Статический метод для вызова из любого места приложения
        /// </summary>
        public static void BackToAuth()
        {
            locker = true;
            var windows = App.Current.Windows;
            // Закрываем все окна, кроме главного
            for (int i = windows.Count - 1; i >= 0; i--)
            {
                if (windows[i] != App.Current.MainWindow && windows[i].Name != "")
                {
                    if (!windows[i].IsVisible && windows[i].Name != "")
                    {
                        windows[i].Show();
                    }
                    windows[i].Close();
                }
            }
            locker = false;
        }

        /// <summary>
        /// Кнопка закрытия окна (не используется)
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Кнопка "Выход" - выход из приложения с подтверждением
        /// </summary>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            MessageBoxResult resDialog = MessageBox.Show("Вы действительно хотите выйти из приложения?", "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resDialog == MessageBoxResult.Yes)
                this.Close();
        }

        /// <summary>
        /// Обработчик нажатия клавиш в окне - авторизация по клавише Enter
        /// </summary>
        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendAuthАttempt();
            }
        }

        /// <summary>
        /// Основной метод авторизации - проверка учетных данных пользователя
        /// Содержит логику: проверка подключения к БД, валидация полей, проверка CAPTCHA,
        /// аутентификация по хешу пароля, определение роли и открытие соответствующей главной формы
        /// </summary>
        private void SendAuthАttempt()
        {
            string serviceLogin = Properties.Settings.Default.serviceLogin;
            string servicePassword = Properties.Settings.Default.servicePassword;
            string userLogin = LoginTextbox.Text;
            string userPassword = PasswordTextBox.Password;
            string captchaInput = captchaTextbox.Text;

            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                }
                catch (Exception)
                {
                    MessageBoxResult result = MessageBox.Show($"Ошибка подключения к базе данных. Хотите настроить параметры подключения?", "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                        OpenSettingsForm();
                    return;
                }

                // Проверка заполнения обязательных полей
                if (userLogin == "" || userPassword == "" || (authAttempsCounter >= 1 && captchaInput == ""))
                {
                    MessageBox.Show($"Необходимо заполнить поля помеченные \"*\"", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    // Проверка CAPTCHA при неудачных попытках
                    if (captchaInput != captchaCompare && authAttempsCounter >= 1)
                    {
                        MessageBox.Show($"Капча заполнена неверно. Возможность авторизации заблокируется на 10 секунд", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        authAttempsCounter++;
                        if (authAttempsCounter >= 2)
                            FrezeForm();  // Блокировка формы на 10 секунд
                        LoginTextbox.Clear();
                        PasswordTextBox.Clear();
                        captchaTextbox.Clear();
                        ShowCaptcha(authAttempsCounter);
                        return;
                    }
                    captchaTextbox.Clear();

                    try
                    {
                        // Проверка системного логина (сервисная учетная запись)
                        if (serviceLogin == userLogin && servicePassword == userPassword)
                        {
                            LoginTextbox.Text = "";
                            PasswordTextBox.Password = "";
                            captchaTextbox.Clear();
                            authAttempsCounter = 0;
                            ShowCaptcha(authAttempsCounter);
                            this.Hide();
                            var win = new SystemTools();
                            win.ShowDialog();
                            this.ShowDialog();
                            return;
                        }

                        // Хеширование введенного пароля с помощью SHA256
                        StringBuilder Sb = new StringBuilder();
                        using (SHA256 hash = SHA256Managed.Create())
                        {
                            Encoding enc = Encoding.UTF8;
                            Byte[] result = hash.ComputeHash(enc.GetBytes(userPassword));
                            foreach (Byte b in result)
                                Sb.Append(b.ToString("x2"));
                        }
                        string hashedPassword = Sb.ToString();

                        // Поиск пользователя в БД по логину и хешу пароля
                        MySqlCommand cmd = new MySqlCommand($"Select `idemployees`, `full_name`, `login`, `password`, `roles`.role_name from `employees` INNER JOIN `roles` on `employees`.roles_Id = `roles`.idroles WHERE `login` = '{userLogin}' AND `password` = '{hashedPassword}'", conn);
                        using (MySqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                // Успешная авторизация - сброс счетчика попыток
                                authAttempsCounter = 0;
                                ShowCaptcha(authAttempsCounter);
                                rdr.Read();
                                object[] accountData = new object[rdr.FieldCount];
                                rdr.GetValues(accountData);

                                // Сохранение данных пользователя в статическом классе AccountHolder
                                AccountHolder.userId = (int)accountData[0];
                                AccountHolder.FIO = (string)accountData[1];
                                AccountHolder.UserLogin = (string)accountData[2];
                                AccountHolder.UserPassword = (string)accountData[3];
                                AccountHolder.UserRole = ((string)accountData[4]).Replace("\r", "").Replace("\n", "");

                                this.Hide();
                                // Открытие главного окна в зависимости от роли пользователя
                                switch (AccountHolder.UserRole)
                                {
                                    case "Менеджер":
                                        new ManagerMain().ShowDialog();
                                        break;
                                    case "Мастер":
                                        new MasterMain().ShowDialog();
                                        break;
                                    case "Администратор":
                                        new AdministratorMain().ShowDialog();
                                        break;
                                    case "Директор":
                                        new DirectorMain().ShowDialog();
                                        break;
                                }
                                LoginTextbox.Text = "";
                                PasswordTextBox.Password = "";
                                this.ShowDialog();
                            }
                            else
                            {
                                // Неудачная попытка авторизации
                                authAttempsCounter++;
                                if (authAttempsCounter >= 2)
                                {
                                    MessageBox.Show("Неверные данные пользователя. Возможность авторизации заблокируется на 10 секунд", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    FrezeForm();  // Блокировка формы
                                }
                                else
                                {
                                    MessageBox.Show("Неверные данные пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                                ShowCaptcha(authAttempsCounter);  // Показ CAPTCHA при неудачах
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                LoginTextbox.Text = "";
                PasswordTextBox.Password = "";
            }
        }

        /// <summary>
        /// Событие загрузки формы - проверка подключения к БД и установка фокуса на поле логина
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TestConnection();
            LoginTextbox.Focus();
        }

        /// <summary>
        /// Кнопка настроек подключения к БД
        /// </summary>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            OpenSettingsForm();
        }

        /// <summary>
        /// Открытие формы настроек подключения к базе данных
        /// </summary>
        private void OpenSettingsForm()
        {
            this.Hide();
            object[] needShutdown = new object[1];
            var win = new Settings(needShutdown);
            win.ShowDialog();
            // Если требуется перезапуск приложения
            if (Convert.ToBoolean(needShutdown[0]))
            {
                this.Close();
                Application.Current.Shutdown();
                Process.Start(Application.ResourceAssembly.Location);
            }
            else
                this.ShowDialog();
        }

        /// <summary>
        /// Асинхронная проверка подключения к базе данных
        /// </summary>
        private async void TestConnection()
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    var taskOpen = conn.OpenAsync();
                    await taskOpen;
                }
                catch (Exception)
                {
                    MessageBoxResult result = MessageBox.Show($"Ошибка подключения к базе данных. Хотите настроить параметры подключения?", "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                        OpenSettingsForm();
                    return;
                }
            }
        }

        /// <summary>
        /// Обновление CAPTCHA (генерация нового текста и изображения)
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                captchaCompare = GenerateCaptchaText(6);
                RefreshCaptchaImage(captchaCompare);
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось обновить картинку капчи", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Отображение CAPTCHA при неудачных попытках авторизации
        /// </summary>
        /// <param name="attempsCount">Количество неудачных попыток</param>
        private void ShowCaptcha(int attempsCount)
        {
            if (attempsCount > 0)
            {
                // Показ элементов CAPTCHA
                captchaImage.Visibility = Visibility.Visible;
                refreshButton.Visibility = Visibility.Visible;
                CaptchaPanel.Visibility = Visibility.Visible;
                captchaLabel.Visibility = Visibility.Visible;
                captchaTextbox.Visibility = Visibility.Visible;
                captchaAsterisk.Visibility = Visibility.Visible;
                captchaCompare = GenerateCaptchaText(6);
                RefreshCaptchaImage(captchaCompare);
            }
            else
            {
                // Скрытие элементов CAPTCHA
                captchaImage.Visibility = Visibility.Hidden;
                refreshButton.Visibility = Visibility.Hidden;
                CaptchaPanel.Visibility = Visibility.Collapsed;
                captchaLabel.Visibility = Visibility.Hidden;
                captchaTextbox.Visibility = Visibility.Hidden;
                captchaAsterisk.Visibility = Visibility.Hidden;
                captchaCompare = "";
            }
        }

        /// <summary>
        /// Генерация изображения CAPTCHA с наложением шума (точки и линии)
        /// </summary>
        /// <param name="text">Текст для отображения на CAPTCHA</param>
        private void RefreshCaptchaImage(string text)
        {
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen())
            {
                Pen drawingpen = new Pen(Brushes.Gray, 0.7);
                Random random = new Random();
                int coordX = random.Next(0, 90);
                int coordY = random.Next(0, 40);
                int angle = random.Next(0, 70);

                // Применение случайного поворота текста
                dc.PushTransform(new RotateTransform(angle, coordX, coordY));
                dc.DrawText(new FormattedText($"{text}", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Consolas"), 11, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(coordX, coordY));
                dc.Pop();

                // Добавление шума - случайные точки
                for (int i = 0; i <= 100; i++)
                {
                    dc.DrawEllipse(Brushes.Black, drawingpen, new Point(random.Next(0, 140), random.Next(0, 70)), 0.5, 0.5);
                }

                // Добавление шума - случайные линии
                for (int i = 0; i <= 10; i++)
                {
                    dc.DrawLine(drawingpen, new Point(random.Next(0, 140), random.Next(0, 70)), new Point(random.Next(0, 70), random.Next(0, 70)));
                }
            }
            DrawingImage drawingImage = new DrawingImage(visual.Drawing);
            drawingImage.Freeze();
            captchaImage.Source = drawingImage;
        }

        /// <summary>
        /// Генерация случайного текста для CAPTCHA
        /// </summary>
        /// <param name="lettersCount">Количество символов</param>
        /// <returns>Случайная строка заданной длины</returns>
        private string GenerateCaptchaText(int lettersCount)
        {
            const string sourceLetters = "qwertyuiopasdfghjklzxcvbnm1234567890!@#$%&*()QWERTYUIOPASDFGHJKLZXCVBNM";
            StringBuilder sb = new StringBuilder();
            Random random = new Random();
            for (int i = 0; i < lettersCount; i++)
            {
                sb.Append(sourceLetters[random.Next(0, sourceLetters.Length - 1)]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Блокировка формы авторизации на 10 секунд после нескольких неудачных попыток
        /// Отображает обратный отсчет на кнопке авторизации
        /// </summary>
        private async void FrezeForm()
        {
            AuthButton.IsEnabled = false;
            for (int i = 10; i >= 0; i--)
            {
                await Task.Delay(1000);
                AuthButton.Content = $"{i}";
                AuthButton.Foreground = Brushes.Black;
            }
            AuthButton.Foreground = Brushes.White;
            AuthButton.IsEnabled = true;
            AuthButton.Content = "Авторизоваться";
        }

        /// <summary>
        /// Кнопка авторизации - запуск процесса аутентификации
        /// </summary>
        private void AuthButton_Click(object sender, RoutedEventArgs e)
        {
            SendAuthАttempt();
        }

        /// <summary>
        /// Событие таймера - создание резервной копии базы данных
        /// </summary>
        private void MakeABackupEvent(object sender, EventArgs e)
        {
            try
            {
                Backup.MakeABackup();
            }
            catch
            {
                ;
            }
        }

        /// <summary>
        /// Событие закрытия окна - создание резервной копии перед выходом
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Backup.MakeABackup();
            }
            catch
            {
                ;
            }
        }
    }
}