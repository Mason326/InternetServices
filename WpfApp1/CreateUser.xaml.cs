using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
        bool isEdit = false;
        int IMAGE_MAX_BYTE_SIZE = 2097152;
        bool isGenerateNewCredentials;
        int userId = -1;
        string filePath;
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
            editUserButton.IsEnabled = false;
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
            if (isEdit)
            {
                MessageBoxResult res = MessageBox.Show("Вы уверены, что хотите изменить пароль пользователя?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes)
                    return;
                else
                    isGenerateNewCredentials = true;
            }
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
                        MySqlCommand cmd = new MySqlCommand();
                        cmd.Connection = conn;
                        string cmdText = $@"Insert into `employees`(full_name, `login`, `password`, phoneNumber, roles_id) 
                                                            value(
                                                                '{fioTextBox.Text}',
                                                                '{loginTextBox.Text}',
                                                                '{CreateChecksum(passwordTextBox.Text)}',
                                                                '{phoneTextBox.Text}',
                                                                 (Select idroles from `roles` where `role_name` = '{rolesComboBox.SelectedItem}')
                                                            );";
                        cmd.CommandText = cmdText;
                        if (filePath != null)
                        {
                            byte[] imageBytes = File.ReadAllBytes(filePath);
                            bool imageSizeIsInvalid = ImageIsTooLarge(imageBytes);
                            if (imageSizeIsInvalid)
                            {
                                MessageBox.Show($"Размер картинки превышает допустимые значения. Выберите другую", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            cmdText = $@"Insert into `employees`(full_name, `login`, `password`, phoneNumber, roles_id, photo) 
                                                            value(
                                                                '{fioTextBox.Text}',
                                                                '{loginTextBox.Text}',
                                                                '{CreateChecksum(passwordTextBox.Text)}',
                                                                '{phoneTextBox.Text}',
                                                                 (Select idroles from `roles` where `role_name` = '{rolesComboBox.SelectedItem}'),
                                                                 @File
                                                            );";
                            cmd.CommandText = cmdText;
                            cmd.Parameters.AddWithValue("@File", imageBytes);
                        }
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
                    MySqlCommand cmd = new MySqlCommand(@"SELECT idemployees, full_name, login, password, roles.role_name, phoneNumber, photo
                                                          FROM employees
                                                          inner join `roles` on employees.roles_id = roles.idroles order by idemployees desc;", conn);
                    DataTable dt = new DataTable();
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        DataColumn[] columns = new DataColumn[dr.FieldCount];
                        for (int i = 0; i < columns.Length; i++)
                        {
                            columns[i] = new DataColumn(dr.GetName(i), dr.GetFieldType(i));
                        }

                        dt.Columns.AddRange(columns);
                        BitmapImage image = new BitmapImage();
                        Type type = image.GetType();
                        dt.Columns.Add("UserPhoto", type);
                        object[] record = new object[dr.FieldCount + 1];
                        while (dr.Read())
                        {
                            dr.GetValues(record);
                            byte[] imageBytes = record[6] as byte[];
                            record[7] = LoadImage(imageBytes);
                            dt.LoadDataRow(record, true);
                        }
                    }

                    userDG.ItemsSource = dt.AsDataView();
                    countRecordsLabel.Content = RecordsCounter.CountRecords("employees");
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
            editUserButton.IsEnabled = true;
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

        private void editUserButton_Click(object sender, RoutedEventArgs e)
        {
            PrepareToEdit();
        }

        private void PrepareToEdit()
        {
            if (userDG.SelectedItem != null)
            {
                DataRowView drv = userDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;

                createUserButton.Visibility = Visibility.Collapsed;
                editUserButton.Visibility = Visibility.Collapsed;
                deleteUserButton.Visibility = Visibility.Collapsed;
                toMainButton.Visibility = Visibility.Collapsed;
                userImage.Source = fieldValuesOfARecord[7] as BitmapImage;

                passwordTextBox.Clear();

                userId = Convert.ToInt32(fieldValuesOfARecord[0]);
                int currentUserId = AccountHolder.userId;
                if (currentUserId == userId)
                    rolesComboBox.IsEnabled = false;
                else
                    rolesComboBox.IsEnabled = true;
                fioTextBox.Text = fieldValuesOfARecord[1].ToString().Trim();
                loginTextBox.Text = fieldValuesOfARecord[2].ToString().Trim();
                phoneTextBox.Text = fieldValuesOfARecord[5].ToString().Trim();
                rolesComboBox.SelectedItem = fieldValuesOfARecord[4].ToString();

                userDG.IsEnabled = false;
                isGenerateNewCredentials = false;
                isEdit = true;

                endEditingButton.Visibility = Visibility.Visible;
                cancelChangesButton.Visibility = Visibility.Visible;
            }
        }

        private void CloseEdition()
        {
            createUserButton.Visibility = Visibility.Visible;
            editUserButton.Visibility = Visibility.Visible;
            deleteUserButton.Visibility = Visibility.Visible;
            toMainButton.Visibility = Visibility.Visible;

            endEditingButton.Visibility = Visibility.Collapsed;
            cancelChangesButton.Visibility = Visibility.Collapsed;
            userImage.Source = LoadImage(null);
            filePath = null;

            ClearInputData();

            userDG.SelectedItem = null;
            userId = -1;
            //searchByPassportSeriesAndNumber.IsEnabled = true;
            editUserButton.IsEnabled = false;
            deleteUserButton.IsEnabled = false;
            userDG.IsEnabled = true;
            isEdit = false;
        }

        private void cancelChangesButton_Click(object sender, RoutedEventArgs e)
        {
            CloseEdition();
        }

        private void endEditingButton_Click(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled;
            try
            {
                requiredFieldsIsFilled = fioTextBox.Text.Split(' ').Length >= 1
                    && regexForPhoneNumber.IsMatch(phoneTextBox.Text)
                    && rolesComboBox.SelectedItem != null
                    && loginTextBox.Text.Length > 0;
            }
            catch
            {
                MessageBox.Show("Некорректно заполнены обязательные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (requiredFieldsIsFilled)
            {
                int duplicatePhoneUserId = CheckDuplicateUtil.HasNoDuplicate("employees", "phoneNumber", phoneTextBox.Text, false);
                int duplicateLoginUserId = CheckDuplicateUtil.HasNoDuplicate("employees", "login", loginTextBox.Text, true);

                if (duplicatePhoneUserId != userId && duplicatePhoneUserId != -1)
                {
                    MessageBox.Show($"Не удалось обновить пользователя. Обнаружен дубликат номера телефона", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (duplicateLoginUserId != userId && duplicateLoginUserId != -1)
                {
                    MessageBox.Show($"Не удалось добавить клиента. Обнаружен дубликат логина пользователя", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            MySqlCommand cmd = new MySqlCommand();
                            cmd.Connection = conn;
                            cmd.CommandText = $@"Update `employees` 
                                                set full_name = '{fioTextBox.Text}',
                                                `login` = '{loginTextBox.Text}',
                                                phoneNumber = '{phoneTextBox.Text}',
                                                roles_id = (SELECT idroles FROM `roles` where `role_name` = '{rolesComboBox.SelectedItem}')";
                            if (isGenerateNewCredentials && passwordTextBox.Text.Length > 0)
                                cmd.CommandText += $", `password` = '{CreateChecksum(passwordTextBox.Text)}'";
                            if (filePath != null)
                            {
                                cmd.CommandText += ", photo = @File";
                                byte[] imageBytes = File.ReadAllBytes(filePath);
                                bool imageSizeIsInvalid = ImageIsTooLarge(imageBytes);
                                if (imageSizeIsInvalid)
                                {
                                    MessageBox.Show($"Размер картинки превышает допустимые значения. Выберите другую", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }
                                cmd.Parameters.AddWithValue("@File", imageBytes);
                            }
                            cmd.CommandText += $" where idemployees = {userId}";
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Данные пользователя успешно обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            CloseEdition();
                            RefreshDataGrid();
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show($"Не удалось обновить данные пользователя\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось установить подключение\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show($"Необходимо заполнить поля помеченные \"*\"", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void passwordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isGenerateNewCredentials = true;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.FileName = "UserImage"; // имя файла по умолчанию 
            dialog.Filter = "Images (.jpg)|*.jpg";

            if (dialog.ShowDialog() == true)
            {
                filePath = dialog.FileName;
                userImage.Source = new BitmapImage(new Uri(filePath));
            }
        }

        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return new BitmapImage(new Uri("pack://application:,,,/Resources/Images/user.png"));
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        private bool ImageIsTooLarge(byte[] imageBytes)
        {
            return imageBytes.Length > IMAGE_MAX_BYTE_SIZE;
        }
    }
}
