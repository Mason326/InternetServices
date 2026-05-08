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
    /// Interaction logic for Window9.xaml
    /// </summary>
    public partial class Auth : Window
    {
        int authAttempsCounter = 0;
        string captchaCompare = "";
        public static bool locker = false;
        public Auth()
        {
            InitializeComponent();
            ShowCaptcha(authAttempsCounter);
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(180);
            timer.Tick += new EventHandler(MakeABackupEvent);
            timer.Start();
        }


        public static void BackToAuth()
        {
            locker = true;
            var windows = App.Current.Windows;
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
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            MessageBoxResult resDialog = MessageBox.Show("Вы действительно хотите выйти из приложения?", "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resDialog == MessageBoxResult.Yes)
                this.Close();
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendAuthАttempt();
            }
        }

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
                catch(Exception)
                {
                    MessageBoxResult result = MessageBox.Show($"Ошибка подключения к базе данных. Хотите настроить параметры подключения?", "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                        OpenSettingsForm();
                    return;
                }
                
                if (userLogin == "" || userPassword == "" || (authAttempsCounter > 1 && captchaInput == ""))
                {
                    MessageBox.Show($"Необходимо заполнить поля помеченные \"*\"", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    if (captchaInput != captchaCompare && authAttempsCounter > 1) 
                    {
                        MessageBox.Show($"Капча заполнена неверно. Возможность авторизации заблокируется на 10 секунд", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        authAttempsCounter++;
                        if (authAttempsCounter > 2)
                            FrezeForm();
                        captchaTextbox.Clear();
                        ShowCaptcha(authAttempsCounter);
                        return;
                    }
                    captchaTextbox.Clear();
                    try
                    {
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

                        StringBuilder Sb = new StringBuilder();
                        using (SHA256 hash = SHA256Managed.Create())
                        {
                            Encoding enc = Encoding.UTF8;
                            Byte[] result = hash.ComputeHash(enc.GetBytes(userPassword));
                            foreach (Byte b in result)
                                Sb.Append(b.ToString("x2"));
                        }
                        string hashedPassword = Sb.ToString();

                        MySqlCommand cmd = new MySqlCommand($"Select `idemployees`, `full_name`, `login`, `password`, `roles`.role_name from `employees` INNER JOIN `roles` on `employees`.roles_Id = `roles`.idroles WHERE `login` = '{userLogin}' AND `password` = '{hashedPassword}'", conn);
                        using (MySqlDataReader rdr = cmd.ExecuteReader())
                        {
                            if (rdr.HasRows)
                            {
                                authAttempsCounter = 0;
                                ShowCaptcha(authAttempsCounter);
                                rdr.Read();
                                object[] accountData = new object[rdr.FieldCount];
                                rdr.GetValues(accountData);
                                AccountHolder.userId = (int)accountData[0];
                                AccountHolder.FIO = (string)accountData[1];
                                AccountHolder.UserLogin = (string)accountData[2];
                                AccountHolder.UserPassword = (string)accountData[3];
                                AccountHolder.UserRole = ((string)accountData[4]).Replace("\r", "").Replace("\n", "");
                                this.Hide();
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
                                MessageBox.Show("Неверные данные пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                ShowCaptcha(authAttempsCounter);
                                authAttempsCounter++;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TestConnection();
            LoginTextbox.Focus();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            OpenSettingsForm();
        }

        private void OpenSettingsForm()
        {
            this.Hide();
            object[] needShutdown = new object[1];
            var win = new Settings(needShutdown);
            win.ShowDialog();
            if (Convert.ToBoolean(needShutdown[0]))
            {
                this.Close();
                Application.Current.Shutdown();
                Process.Start(Application.ResourceAssembly.Location);
            }
            else
                this.ShowDialog();
        }

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

        private void ShowCaptcha(int attempsCount)
        {
            if (attempsCount > 0)
            {
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
                captchaImage.Visibility = Visibility.Hidden;
                refreshButton.Visibility = Visibility.Hidden;
                CaptchaPanel.Visibility = Visibility.Collapsed;
                captchaLabel.Visibility = Visibility.Hidden;
                captchaTextbox.Visibility = Visibility.Hidden;
                captchaAsterisk.Visibility = Visibility.Hidden;
                captchaCompare = "";
            }
        }

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
                dc.PushTransform(new RotateTransform(angle, coordX, coordY));
                dc.DrawText(new FormattedText($"{text}", CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Consolas"), 11, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point(coordX, coordY));
                dc.Pop();
                for (int i = 0; i <= 100; i++)
                {
                    dc.DrawEllipse(Brushes.Black, drawingpen, new Point(random.Next(0, 140), random.Next(0, 70)), 0.5, 0.5);
                }

                for (int i = 0; i <= 10; i++)
                {
                    dc.DrawLine(drawingpen, new Point(random.Next(0, 140), random.Next(0, 70)), new Point(random.Next(0, 70), random.Next(0, 70)));
                }
            }
            DrawingImage drawingImage = new DrawingImage(visual.Drawing);
            drawingImage.Freeze();
            captchaImage.Source = drawingImage;
        }

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

        private void AuthButton_Click(object sender, RoutedEventArgs e)
        {
            SendAuthАttempt();
        }

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
