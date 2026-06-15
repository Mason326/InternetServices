using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using WpfApp1.Utils;

namespace WpfApp1
{
    /// <summary>
    /// Форма "Создание пользователя" - управление учетными записями сотрудников
    /// Позволяет: создавать новых пользователей, редактировать существующих,
    /// удалять пользователей, назначать роли, загружать фото профиля,
    /// генерировать пароли, изменять учетные данные
    /// </summary>
    public partial class CreateUser : Window
    {
        // Флаг для отслеживания нажатия Backspace при форматировании ФИО
        bool prevBack = false;
        // Флаг режима редактирования
        bool isEdit = false;
        // Максимальный размер изображения (2 МБ)
        int IMAGE_MAX_BYTE_SIZE = 2097152;
        // Флаг генерации новых учетных данных
        bool isGenerateNewCredentials;
        // ID редактируемого пользователя
        int userId = -1;
        // Путь к файлу изображения
        string filePath;
        // Регулярное выражение для проверки номера телефона
        Regex regexForPhoneNumber = new Regex(@"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$");

        // WinAPI для управления раскладкой клавиатуры
        [DllImport("user32.dll")]
        static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);

        [DllImport("user32.dll")]
        static extern IntPtr GetKeyboardLayout(uint idThread);

        // Коды раскладок: русская и английская
        private static readonly IntPtr RussianLayout = new IntPtr(0x04190419);
        private static readonly IntPtr EnglishLayout = new IntPtr(0x04090409);

        /// <summary>
        /// Конструктор формы - инициализация компонентов
        /// </summary>
        public CreateUser()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка "На главную" - закрытие формы
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Событие загрузки формы - отображение роли пользователя,
        /// загрузка списка ролей, загрузка данных пользователей
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Title += $" ({AccountHolder.UserRole}: {FullNameSplitter.MakeShortName(AccountHolder.FIO)})";
            }
            catch
            {
                ;
            }

            RefreshDataGrid(true);
            editUserButton.IsEnabled = false;

            // Загрузка ролей из БД
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

        /// <summary>
        /// Валидация ввода ФИО - русские буквы, дефис, пробел, Backspace
        /// Автоматическое форматирование: первая буква заглавная
        /// </summary>
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[А-Яа-я-\b\s]");
            try
            {
                if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                    e.Handled = false;
                else
                    e.Handled = true;

                // Автоматическое преобразование к верхнему регистру первой буквы
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

        /// <summary>
        /// При получении фокуса полем телефона - установка курсора на первый символ маски "_"
        /// </summary>
        private void phoneTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var phoneTextBox = sender as TextBox;
            int targetIndex = phoneTextBox.Text.IndexOf("_");
            if (targetIndex != -1)
                phoneTextBox.CaretIndex = targetIndex;
        }

        /// <summary>
        /// При захвате мыши полем телефона - установка курсора на первый символ маски "_"
        /// </summary>
        private void phoneTextBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            var phoneTextBox = sender as TextBox;
            int targetIndex = phoneTextBox.Text.IndexOf("_");
            if (targetIndex != -1)
                phoneTextBox.CaretIndex = targetIndex;
        }

        /// <summary>
        /// Автоматическое форматирование номера телефона при вводе
        /// </summary>
        private void phoneTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var phoneTextBox = sender as TextBox;
            int currentPos = phoneTextBox.CaretIndex;
            try
            {
                // Пропуск служебных клавиш
                if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.LeftAlt || e.Key == Key.LeftShift ||
                    e.Key == Key.LeftCtrl || e.Key == Key.CapsLock || e.Key == Key.System)
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
                                string fpart, spart, tpart;
                                fpart = part.Substring(0, 3);
                                spart = part.Substring(4, 2);
                                tpart = part.Substring(7, 2);
                                phoneByParts[i] = $"{fpart}-{spart}-{tpart}";
                                break;
                        }
                    }

                    string[] lastNumsOfThirdPart = phoneByParts[2].Split('-');
                    phoneTextBox.Text = string.Join(" ", phoneByParts);

                    // Корректировка позиции курсора после форматирования
                    if (!phoneByParts[1].Contains("_") && currentPos < 9)
                        phoneTextBox.CaretIndex = currentPos + 2;
                    else if (!lastNumsOfThirdPart[0].Contains("_") && currentPos < 13)
                        phoneTextBox.CaretIndex = currentPos + 1;
                    else if (!lastNumsOfThirdPart[1].Contains("_") && currentPos < 17)
                        phoneTextBox.CaretIndex = currentPos + 1;
                    else if (!lastNumsOfThirdPart[2].Contains("_") && currentPos < 21)
                        phoneTextBox.CaretIndex = currentPos + 1;
                    else
                        phoneTextBox.CaretIndex = currentPos;
                }
            }
            catch
            {
                ;
            }
        }

        /// <summary>
        /// Кнопка "Сгенерировать пароль" - создание случайного пароля
        /// </summary>
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

            // Генерация пароля из перемешанных символов
            char[] targetCharsPassword = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM0123456789".ToCharArray();
            char[] mixedCharsPassword = CredentialsGenerator.MixChars(targetCharsPassword);
            string generatePassword = CredentialsGenerator.GenerateCredential(mixedCharsPassword);
            passwordTextBox.Text = generatePassword;
        }

        /// <summary>
        /// Валидация ввода телефона - только цифры, пробел, Backspace
        /// </summary>
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

        /// <summary>
        /// Автоматическое форматирование ФИО: первая буква каждого слова заглавная
        /// </summary>
        private void fioTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.LeftAlt || e.Key == Key.LeftShift ||
                    e.Key == Key.LeftCtrl || e.Key == Key.CapsLock || e.Key == Key.System)
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

        /// <summary>
        /// Валидация ввода ФИО - русские буквы, дефис, пробел, Backspace
        /// </summary>
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

        /// <summary>
        /// Ограничение ввода: не более 2 пробелов в ФИО
        /// </summary>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (fioTextBox.Text.Length > 0)
                {
                    if (fioTextBox.Text.Count(c => c == ' ') > 1)
                        e.Handled = true;
                    else
                        e.Handled = false;
                }
            }
        }

        /// <summary>
        /// Преобразование строки в формат "Заглавная + строчные"
        /// </summary>
        private string ToTitle(string text)
        {
            return $"{text[0].ToString().ToUpper()}{text.Substring(1, text.Length - 1)}";
        }

        /// <summary>
        /// Кнопка "Создать пользователя" - добавление нового пользователя в БД
        /// </summary>
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
                // Проверка на дубликаты логина и телефона
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

                // Проверка: может быть только один директор
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

                        // Обработка загрузки фото (с проверкой размера и возможным сжатием)
                        if (filePath != null)
                        {
                            byte[] imageBytes = File.ReadAllBytes(filePath);
                        compressionLabel:
                            bool imageSizeIsInvalid = ImageIsTooLarge(imageBytes);
                            if (imageSizeIsInvalid)
                            {
                                MessageBoxResult res = MessageBox.Show($"Размер картинки превышает допустимые значения. Выберите другую картинку или используйте сжатие. \nИспользовать сжатие картинки?", "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                if (res == MessageBoxResult.Yes)
                                {
                                    var win = new ImageCompressionWindow();
                                    win.ShowDialog();
                                    if (ImageHolder.isCanceled || ImageHolder.destinationImage == null)
                                    {
                                        ImageHolder.isCanceled = false;
                                        return;
                                    }
                                    imageBytes = ImageHolder.GetBitmapImageBytes(ImageHolder.destinationImage);
                                    goto compressionLabel;
                                }
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
                        userImage.Source = ImageUtils.LoadImage(null);
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось создать нового пользователя\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                RefreshDataGrid(false);
            }
            else
                MessageBox.Show("Все поля помеченные \"*\" обязательны для заполнения", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Очистка всех полей ввода
        /// </summary>
        private void ClearInputData()
        {
            fioTextBox.Text = "";
            phoneTextBox.Text = "+7 (___) ___-__-__";
            rolesComboBox.SelectedItem = null;
            loginTextBox.Text = "";
            passwordTextBox.Text = "";
        }

        /// <summary>
        /// Обновление DataGrid со списком пользователей
        /// </summary>
        /// <param name="isInitial">true - начальная загрузка, false - обновление с сортировкой по убыванию ID</param>
        private void RefreshDataGrid(bool isInitial)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    string cmdText = @"SELECT idemployees, full_name, login, password, roles.role_name, phoneNumber, photo, concat('ФИО: ', full_name, '\nРоль: ', role_name, '\nТелефон: ', phoneNumber, '\nЛогин: ', `login`) as userData
                                                          FROM employees
                                                          inner join `roles` on employees.roles_id = roles.idroles order by idemployees desc;";
                    if (isInitial)
                    {
                        cmdText = @"SELECT idemployees, full_name, login, password, roles.role_name, phoneNumber, photo, concat('ФИО: ', full_name, '\nРоль: ', role_name, '\nТелефон: ', phoneNumber, '\nЛогин: ', `login`) as userData
                                                          FROM employees
                                                          inner join `roles` on employees.roles_id = roles.idroles order by idemployees;";
                    }

                    MySqlCommand cmd = new MySqlCommand(cmdText, conn);
                    DataTable dt = new DataTable();
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        // Создание структуры таблицы
                        DataColumn[] columns = new DataColumn[dr.FieldCount];
                        for (int i = 0; i < columns.Length; i++)
                        {
                            columns[i] = new DataColumn(dr.GetName(i), dr.GetFieldType(i));
                        }
                        dt.Columns.AddRange(columns);

                        // Добавление колонки для фото
                        BitmapImage image = new BitmapImage();
                        Type type = image.GetType();
                        dt.Columns.Add("UserPhoto", type);

                        object[] record = new object[dr.FieldCount + 1];
                        while (dr.Read())
                        {
                            dr.GetValues(record);
                            byte[] imageBytes = record[6] as byte[];
                            record[8] = ImageUtils.LoadImage(imageBytes);  // Загрузка фото из БД
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

        /// <summary>
        /// Создание SHA256 хеша пароля
        /// </summary>
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
            return Sb.ToString();
        }

        /// <summary>
        /// Кнопка "Удалить пользователя" - удаление выбранного пользователя из БД
        /// </summary>
        private void deleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (userDG.SelectedItem == null)
                return;

            DataRowView recordView = userDG.SelectedItem as DataRowView;
            object[] recordValues = recordView.Row.ItemArray;

            MessageBoxResult result = MessageBox.Show($"Вы действительно хотите удалить пользователя '{recordValues[1]}'?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No || result == MessageBoxResult.Cancel)
                return;

            // Запрет удаления текущего пользователя
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
            catch
            {
                MessageBox.Show($"Не удалось удалить пользователя\nОшибка: Пользователь используется в связанных таблицах", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            RefreshDataGrid(false);
        }

        /// <summary>
        /// Активация кнопок при выборе пользователя в DataGrid
        /// </summary>
        private void userDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            deleteUserButton.IsEnabled = true;
            editUserButton.IsEnabled = true;
        }

        /// <summary>
        /// Проверка наличия учетной записи директора в системе
        /// </summary>
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

        /// <summary>
        /// Кнопка "Редактировать пользователя" - переход в режим редактирования
        /// </summary>
        private void editUserButton_Click(object sender, RoutedEventArgs e)
        {
            PrepareToEdit();
        }

        /// <summary>
        /// Подготовка к редактированию - заполнение полей данными выбранного пользователя
        /// </summary>
        private void PrepareToEdit()
        {
            if (userDG.SelectedItem != null)
            {
                DataRowView drv = userDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;

                // Переключение UI в режим редактирования
                createUserButton.Visibility = Visibility.Collapsed;
                editUserButton.Visibility = Visibility.Collapsed;
                deleteUserButton.Visibility = Visibility.Collapsed;
                toMainButton.Visibility = Visibility.Collapsed;
                userImage.Source = fieldValuesOfARecord[8] as BitmapImage;

                passwordTextBox.Clear();

                userId = Convert.ToInt32(fieldValuesOfARecord[0]);
                int currentUserId = AccountHolder.userId;

                // Запрет изменения роли текущего пользователя
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

        /// <summary>
        /// Выход из режима редактирования, возврат в режим просмотра/создания
        /// </summary>
        private void CloseEdition()
        {
            createUserButton.Visibility = Visibility.Visible;
            editUserButton.Visibility = Visibility.Visible;
            deleteUserButton.Visibility = Visibility.Visible;
            toMainButton.Visibility = Visibility.Visible;

            endEditingButton.Visibility = Visibility.Collapsed;
            cancelChangesButton.Visibility = Visibility.Collapsed;
            userImage.Source = ImageUtils.LoadImage(null);
            filePath = null;

            ClearInputData();

            userDG.SelectedItem = null;
            userId = -1;
            editUserButton.IsEnabled = false;
            deleteUserButton.IsEnabled = false;
            userDG.IsEnabled = true;
            isEdit = false;
        }

        /// <summary>
        /// Кнопка "Отмена редактирования"
        /// </summary>
        private void cancelChangesButton_Click(object sender, RoutedEventArgs e)
        {
            CloseEdition();
        }

        /// <summary>
        /// Кнопка "Завершить редактирование" - сохранение изменений пользователя
        /// </summary>
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
                // Проверка на дубликаты при редактировании (исключая текущего пользователя)
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

                            // Обновление пароля, если были сгенерированы новые учетные данные
                            if (isGenerateNewCredentials && passwordTextBox.Text.Length > 0)
                                cmd.CommandText += $", `password` = '{CreateChecksum(passwordTextBox.Text)}'";

                            // Обновление фото, если был выбран новый файл
                            if (filePath != null)
                            {
                                cmd.CommandText += ", photo = @File";
                                byte[] imageBytes = File.ReadAllBytes(filePath);
                            compressionLabel2:
                                bool imageSizeIsInvalid = ImageIsTooLarge(imageBytes);
                                if (imageSizeIsInvalid)
                                {
                                    MessageBoxResult res = MessageBox.Show($"Размер картинки превышает допустимые значения. Выберите другую картинку или используйте сжатие. \nИспользовать сжатие картинки?", "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                                    if (res == MessageBoxResult.Yes)
                                    {
                                        var win = new ImageCompressionWindow();
                                        win.ShowDialog();
                                        if (ImageHolder.isCanceled || ImageHolder.destinationImage == null)
                                        {
                                            ImageHolder.isCanceled = false;
                                            return;
                                        }
                                        imageBytes = ImageHolder.GetBitmapImageBytes(ImageHolder.destinationImage);
                                        goto compressionLabel2;
                                    }
                                    return;
                                }
                                cmd.Parameters.AddWithValue("@File", imageBytes);
                            }
                            cmd.CommandText += $" where idemployees = {userId}";
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Данные пользователя успешно обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            CloseEdition();
                            RefreshDataGrid(false);
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

        /// <summary>
        /// При изменении текста в поле пароля - установка флага генерации новых учетных данных
        /// </summary>
        private void passwordTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isGenerateNewCredentials = true;
        }

        /// <summary>
        /// Кнопка "Загрузить фото" - выбор изображения для профиля пользователя
        /// </summary>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.FileName = "UserImage";
            dialog.Filter = "JPG-images (.jpg)|*.jpg| PNG-images (.png)|*.png";

            if (dialog.ShowDialog() == true)
            {
                ImageHolder.BackToDefaultValues();
                filePath = dialog.FileName;
                ImageHolder.sourcePath = filePath;
                ImageHolder.sourceImage = new BitmapImage(new Uri(filePath));
                userImage.Source = new BitmapImage(new Uri(filePath));
            }
        }

        /// <summary>
        /// Проверка, превышает ли размер изображения допустимый лимит
        /// </summary>
        private bool ImageIsTooLarge(byte[] imageBytes)
        {
            return imageBytes.Length > IMAGE_MAX_BYTE_SIZE;
        }

        /// <summary>
        /// Адаптация интерфейса при изменении размера окна
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double windowHeight = e.NewSize.Height;
            double baseFontSize = 14;

            if (windowHeight > 700)
            {
                double scale = windowHeight / 600;
                int newFontSize = (int)(baseFontSize * Math.Min(scale, 1.5));
                UpdateButtonsFontSize(newFontSize);
            }
            else if (windowHeight < 550)
            {
                double scale = windowHeight / 600;
                int newFontSize = (int)Math.Max(baseFontSize * scale, 10);
                UpdateButtonsFontSize(newFontSize);
            }
            else
            {
                UpdateButtonsFontSize((int)baseFontSize);
            }
        }

        /// <summary>
        /// Обновление размера шрифта для кнопок и DataGrid
        /// </summary>
        private void UpdateButtonsFontSize(int fontSize)
        {
            var buttons = new[] { createUserButton, editUserButton, deleteUserButton,
                          toMainButton, endEditingButton, cancelChangesButton,
                          generateButton, uploadImageButton };
            foreach (var button in buttons)
            {
                if (button != null)
                    button.FontSize = fontSize;
            }

            if (userDG != null)
                userDG.FontSize = fontSize;
        }

        /// <summary>
        /// При получении фокуса полем ФИО - переключение на русскую раскладку клавиатуры
        /// </summary>
        private void fioTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ActivateKeyboardLayout(RussianLayout, 0);
        }
    }
}