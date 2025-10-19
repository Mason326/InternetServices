using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CreateUser : Window
    {
        public CreateUser()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(@"SELECT idemployees, full_name, login, password, photo, roles.role_name, phoneNumber
                                                        FROM employees
                                                        inner join `roles` on employees.roles_id = roles.idroles;", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);
                    userDG.ItemsSource = dt.AsDataView();
                }
                phoneTextBox.Text = "+7(___)-___-__-__";
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(@"SELECT role_name FROM `roles`;", conn);
                    MySqlDataReader dr2 = cmd.ExecuteReader();
                    List<string> roles = new List<string>();
                    while (dr2.Read())
                    {
                        roles.Add(dr2.GetValue(0).ToString());
                    }
                    rolesComboBox.ItemsSource = roles;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка загрузки ролей\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }


        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {

        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[А-Яа-я-\b\s]");
            try
            {
                if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                    e.Handled = false;
                else
                    e.Handled = true;
                string[] arr = fioTextBox.Text.Split(' ');
                if (arr.Length > 0)
                    fioTextBox.Text = string.Join(" ", arr.Select(s => $"{s[0].ToString().ToUpper()}{s.Substring(1)}"));
                fioTextBox.CaretIndex = fioTextBox.Text.Length;
            }
            catch
            {
                ;
            }
        }

        private void phoneTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            phoneTextBox.CaretIndex = phoneTextBox.Text.IndexOf('_') == -1 ? phoneTextBox.Text.Length : phoneTextBox.Text.IndexOf('_');
        }

        private void phoneTextBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            phoneTextBox.CaretIndex = phoneTextBox.Text.IndexOf('_') == -1 ? phoneTextBox.Text.Length : phoneTextBox.Text.IndexOf('_');
        }

        private void phoneTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Key[] keys = new Key[] { Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9, Key.D0, Key.Back };
            if (keys.Contains(e.Key))
            {
                e.Handled = false;
            }
            else
                e.Handled = true;
        }

        private void phoneTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            Key[] keys = new Key[] { Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9, Key.D0 };
            if (keys.Contains(e.Key))
            {
                phoneTextBox.Text = phoneTextBox.Text.Remove(phoneTextBox.Text.IndexOf('_'), 1);
                phoneTextBox.CaretIndex = phoneTextBox.Text.IndexOf('_') == -1 ? phoneTextBox.Text.Length : phoneTextBox.Text.IndexOf('_');
            }
            if (e.Key == Key.Back)
            {
                phoneTextBox.Text = "+7(___)-___-__-__";
                phoneTextBox.CaretIndex = phoneTextBox.Text.IndexOf('_');
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            long idEmpl;
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    try
                    {
                        MySqlCommand cmd = new MySqlCommand("SELECT max(`idemployees`) FROM employees;", conn);
                        idEmpl = long.Parse(cmd.ExecuteScalar().ToString());
                    }
                    catch
                    {
                        idEmpl = 1;
                    }

                }
                string loginGenerated = ComputeSha256Hash($"login{DateTime.Now}{idEmpl}{new Random().Next(0, 100000)}").Substring(6, 14);
                string passwordGenerated = ComputeSha256Hash($"password{DateTime.Now}{idEmpl}{new Random().Next(0, 100000)}").Substring(6, 14);
                //string targetEng = "qwertyuiopasdfghjklzxcvbnm";
                //int[] indexes = loginGenerated.Where(c => targetEng.Contains(c)).Select(c => loginGenerated.ToString().IndexOf(c)).ToArray();
                //for (int i = 0; i < 3; i++)
                //{
                //    int idx = new Random().Next(0, indexes.Length - 1);
                //    loginGenerated[idx] = Convert.ToChar(loginGenerated[idx].ToString().ToUpper());
                //}
                //Random rand = new Random();
                loginTextBox.Text = loginGenerated;
                passwordTextBox.Text = passwordGenerated;
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка генерации логина и пароля\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256 hash algorithm instance.
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Compute the hash from the input string (converted to bytes).
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert the byte array to a hexadecimal string.
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2")); // "x2" formats as two lowercase hexadecimal digits
                }
                return builder.ToString();
            }
        }

    }
}
