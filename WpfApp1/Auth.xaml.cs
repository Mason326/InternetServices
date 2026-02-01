using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Window9.xaml
    /// </summary>
    public partial class Auth : Window
    {
        public Auth()
        {
            InitializeComponent();
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SendAuthАttempt();
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
                string userLogin = LoginTextbox.Text;
                string userPassword = PasswordTextBox.Password;
                if (userLogin == "" || userPassword == "")
                {
                    MessageBox.Show($"Необходимо заполнить поля Логин и Пароль", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else
                {
                    try
                    {
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
                                rdr.Read();
                                object[] accountData = new object[rdr.FieldCount];
                                rdr.GetValues(accountData);
                                AccountHolder.userId = (int)accountData[0];
                                AccountHolder.FIO = (string)accountData[1];
                                AccountHolder.UserLogin = (string)accountData[2];
                                AccountHolder.UserPassword = (string)accountData[3];
                                AccountHolder.UserRole = (string)accountData[4];
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
    }
}
