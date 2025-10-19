using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
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
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                }
                catch
                {
                    MessageBox.Show($"Не удалось установить соединение", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    catch(Exception exc)
                    {
                        MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                LoginTextbox.Text = "";
                PasswordTextBox.Password = "";
            }

        }

    }
}
