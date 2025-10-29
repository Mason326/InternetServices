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
        bool prevBack = false;
        Regex regexForPhoneNumber = new Regex(@"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$");
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
            RefreshDataGrid();

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
            deleteUserButton.IsEnabled = false;
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
            var phoneTextBox = sender as TextBox;
            int targetIndex = phoneTextBox.Text.IndexOf("_");
            if (targetIndex != -1)
                phoneTextBox.CaretIndex = targetIndex;
        }

        private void phoneTextBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            var phoneTextBox = sender as TextBox;
            int targetIndex = phoneTextBox.Text.IndexOf("_");
            if (targetIndex != -1)
                phoneTextBox.CaretIndex = targetIndex;
        }

        private void phoneTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var phoneTextBox = sender as TextBox;
            int currentPos = phoneTextBox.CaretIndex;
            try
            {
                if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.LeftAlt || e.Key == Key.LeftShift || e.Key == Key.LeftCtrl || e.Key == Key.CapsLock || e.Key == Key.System)
                    return;
                if (e.Key == Key.Back)
                {
                    phoneTextBox.Text = "+7 (___) ___-__-__";
                    phoneTextBox.CaretIndex = 4;
                    return;
                }
                int fioLength = phoneTextBox.Text.Length;
                if (fioLength > 0)
                {
                    string[] phoneByParts = phoneTextBox.Text.Split(' ');
                    for (int i = 0; i < phoneByParts.Length; i++)
                    {
                        string part = phoneByParts[i];
                        switch (i)
                        {
                            case 0:
                                phoneByParts[i] = "+7";
                                break;
                            case 1:
                                phoneByParts[i] = $"({part.Substring(1, 3)})";
                                break;
                            case 2:
                                string fpart;
                                string spart;
                                string tpart;
                                fpart = part.Substring(0, 3);
                                spart = part.Substring(4, 2);
                                tpart = part.Substring(7, 2);
                                if (currentPos > 13 && currentPos <= 16)
                                    tpart = part.Substring(8, 2);
                                else if (currentPos > 9 && currentPos <= 13)
                                {
                                    spart = part.Substring(5, 2);
                                    tpart = part.Substring(8, 2);
                                }
                                phoneByParts[i] = $"{fpart}-{spart}-{tpart}";
                                break;
                        }
                    }
                    string[] lastNumsOfThirdPart = phoneByParts[2].Split('-');
                    phoneTextBox.Text = string.Join(" ", phoneByParts);
                    if (!phoneByParts[1].Contains("_") && currentPos < 9)
                        phoneTextBox.CaretIndex = currentPos + 2;
                    else if (!lastNumsOfThirdPart[0].Contains("_") && currentPos < 13)
                    {
                        phoneTextBox.CaretIndex = currentPos + 1;
                    }
                    else if (!lastNumsOfThirdPart[1].Contains("_") && currentPos < 17)
                    {
                        phoneTextBox.CaretIndex = currentPos + 1;
                    }
                    else if (!lastNumsOfThirdPart[2].Contains("_") && currentPos < 21)
                    {
                        phoneTextBox.CaretIndex = currentPos + 1;
                    }
                    else
                        phoneTextBox.CaretIndex = currentPos;
                }
            }
            catch
            {
                ;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            char[] targetCharsPassword = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM0123456789".ToCharArray();
            char[] mixedCharsPassword = CredentialsGenerator.MixChars(targetCharsPassword);
            string generatePassword = CredentialsGenerator.GenerateCredential(mixedCharsPassword);

            passwordTextBox.Text = generatePassword;
        }

        private void phoneTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[0-9\b\s]");
            try
            {
                if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                    e.Handled = false;
                else
                    e.Handled = true;
            }
            catch
            {
                ;
            }
        }

        private void fioTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.LeftAlt || e.Key == Key.LeftShift || e.Key == Key.LeftCtrl || e.Key == Key.CapsLock || e.Key == Key.System)
                    return;
                if (e.Key == Key.Back)
                {
                    prevBack = true;
                    return;
                }
                int fioLength = fioTextBox.Text.Length;
                if (fioLength > 0)
                {
                    string[] fioByParts = fioTextBox.Text.Split(' ');
                    for (int i = 0; i < fioByParts.Length; i++)
                    {
                        string part = fioByParts[i];

                        if (part.Length > 0)
                            fioByParts[i] = ToTitle(part);
                        if (part.Contains("-"))
                        {
                            string[] arr = fioByParts[i].Split(new char[] { '-' });
                            if (arr[1].Length > 0)
                            {
                                arr[1] = ToTitle(arr[1]);
                                fioByParts[i] = string.Join("-", arr);
                            }
                        }
                    }
                    int currentPos = fioTextBox.CaretIndex;
                    fioTextBox.Text = string.Join(" ", fioByParts);
                    if (prevBack)
                    {
                        fioTextBox.CaretIndex = currentPos;
                        prevBack = false;
                    }
                    else
                    {
                        if (currentPos != fioTextBox.Text.Length)
                            fioTextBox.CaretIndex = currentPos;
                        else
                            fioTextBox.CaretIndex = ++currentPos;
                    }
                }
            }
            catch
            {
                ;
            }
        }

        private void fioTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[А-Яа-я- \b\s]");
            try
            {
                if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                    e.Handled = false;
                else
                    e.Handled = true;
            }
            catch
            {
                ;
            }
        }

        private string ToTitle(string text)
        {
            return $"{text[0].ToString().ToUpper()}{text.Substring(1, text.Length - 1)}";
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled;
            try
            {

                requiredFieldsIsFilled = fioTextBox.Text.Split(' ').Length >= 1
                   && regexForPhoneNumber.IsMatch(phoneTextBox.Text)
                   && rolesComboBox.SelectedItem != null
                   && loginTextBox.Text.Length > 0
                   && passwordTextBox.Text.Length > 0;

                if (loginTextBox.Text.Length < 6)
                {
                    MessageBox.Show("Необходимо создать более сложный логин", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (passwordTextBox.Text.Length < 8)
                {
                    MessageBox.Show("Необходимо создать более сложный пароль", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            catch
            {
                MessageBox.Show("Некорректно заполнены обязательные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            if (requiredFieldsIsFilled)
            {
                if (!CheckDuplicateUtil.HasNoDuplicate("employees", "login", loginTextBox.Text))
                {
                    MessageBox.Show($"Не удалось добавить клиента. Обнаружен дубликат логина пользователя", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (!CheckDuplicateUtil.HasNoDuplicate("employees", "phoneNumber", phoneTextBox.Text))
                {
                    MessageBox.Show($"Не удалось добавить клиента. Обнаружен дубликат номера телефона", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (HasDirectorAccount() && rolesComboBox.SelectedItem.ToString() == "Директор")
                { 
                    MessageBox.Show("В системе уже существует учетная запись директора", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        MySqlCommand cmd = new MySqlCommand($@"Insert into `employees`(full_name, `login`, `password`, phoneNumber, roles_id) 
                                                            value(
                                                                '{fioTextBox.Text}',
                                                                '{loginTextBox.Text}',
                                                                '{CreateChecksum(passwordTextBox.Text)}',
                                                                '{phoneTextBox.Text}',
                                                                 (Select idroles from `roles` where `role_name` = '{rolesComboBox.SelectedItem}')
                                                            );", conn);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Пользователь создан", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearInputData();
                    }

                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось создать нового пользователя\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                RefreshDataGrid();
            }
            else
                MessageBox.Show("Все поля помеченные \"*\" обязательны для заполнения", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ClearInputData()
        {
            fioTextBox.Text = "";
            phoneTextBox.Text = "+7 (___) ___-__-__";
            rolesComboBox.SelectedItem = null;
            loginTextBox.Text = "";
            passwordTextBox.Text = "";
        }

        private void RefreshDataGrid()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(@"SELECT idemployees, full_name, login, password, photo, roles.role_name, phoneNumber
                                                        FROM employees
                                                        inner join `roles` on employees.roles_id = roles.idroles order by idemployees desc;", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);
                    userDG.ItemsSource = dt.AsDataView();
                }
                phoneTextBox.Text = "+7 (___) ___-__-__";
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public string CreateChecksum(string password)
        {
            StringBuilder Sb = new StringBuilder();
            using (SHA256 hash = SHA256Managed.Create())
            {
                Encoding enc = Encoding.UTF8;
                Byte[] result = hash.ComputeHash(enc.GetBytes(password));
                foreach (Byte b in result)
                    Sb.Append(b.ToString("x2"));
            }
            string hashedPassword = Sb.ToString();
            return hashedPassword;
        }

        private void deleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (userDG.SelectedItem == null)
                return;

            DataRowView recordView = userDG.SelectedItem as DataRowView;
            object[] recordValues = recordView.Row.ItemArray;

            MessageBoxResult result = MessageBox.Show($"Вы действительно хотите удалить пользователя '{recordValues[1]}'?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No || result == MessageBoxResult.Cancel)
                return;

            int currentUserId = AccountHolder.userId;
            if (currentUserId == Convert.ToInt32(recordValues[0]))
            {
                MessageBox.Show($"Пользователь не может быть удален так как является текущей учетной записью", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"Delete from `employees` where `idemployees` = {recordValues[0]};", conn);
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Пользователь удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearInputData();
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось удалить пользователя\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            RefreshDataGrid();
        }

        private void userDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            deleteUserButton.IsEnabled = true;
        }

        private bool HasDirectorAccount()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"Select idemployees from `employees` where `roles_id` = (Select idroles from `roles` where role_name = 'Директор');", conn);
                    object res = cmd.ExecuteScalar();
                    if (res != null)
                        return true;
                    return false;
                }
            }
            catch
            { 
                return false;
            }
        }
    }
}
