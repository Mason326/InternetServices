using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApp1
{
    /// <summary>
    /// Форма "Создание клиента" - управление справочником клиентов
    /// Позволяет: создавать новых клиентов, редактировать существующих,
    /// просматривать детальную информацию, выбирать клиента для заявки,
    /// генерировать учетные данные абонента
    /// </summary>
    public partial class CreateClient : Window
    {
        // Флаг для отслеживания нажатия Backspace при форматировании ФИО
        bool prevBack = false;
        // ID редактируемого клиента
        int clientId;
        // Флаг режима редактирования
        bool isEdit = false;
        // Флаг генерации новых учетных данных (логин/пароль)
        bool isGenerateNewCredentials;

        // Регулярные выражения для валидации полей
        Regex regexForEmail = new Regex("^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$");
        Regex regexForPhoneNumber = new Regex(@"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$");
        Regex regexForPassportSeries = new Regex(@"^[0-9]{4}$");
        Regex regexForPassportNumber = new Regex(@"^[0-9]{6}$");
        Regex regexForDepartmentCode = new Regex(@"^\d{3}-\d{3}$");

        // Условие фильтрации для SQL-запроса
        string filterOption = "";

        // Данные для пагинации
        private List<DataRow> _allRows = new List<DataRow>();
        private int _currentPage = 1;
        private int _pageSize = 2;

        // Таймер неактивности (автоматический выход)
        private DispatcherTimer inactivityTimer;

        // WinAPI для управления раскладкой клавиатуры
        [DllImport("user32.dll")]
        static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);

        [DllImport("user32.dll")]
        static extern IntPtr GetKeyboardLayout(uint idThread);

        // Коды раскладок: русская и английская
        private static readonly IntPtr RussianLayout = new IntPtr(0x04190419);
        private static readonly IntPtr EnglishLayout = new IntPtr(0x04090409);

        /// <summary>
        /// Конструктор формы
        /// </summary>
        /// <param name="isSelectClient">true - режим выбора клиента для заявки, false - режим управления клиентами</param>
        public CreateClient(bool isSelectClient)
        {
            InitializeComponent();

            // Таймер неактивности: 2 минуты бездействия -> возврат на форму авторизации
            inactivityTimer = new DispatcherTimer();
            inactivityTimer.Interval = TimeSpan.FromMinutes(2);
            inactivityTimer.Tick += CheckInactivity;

            if (!isSelectClient)
            {
                inClaimButton.Visibility = Visibility.Collapsed;  // Скрыть кнопку выбора для заявки
                editClientButton.Visibility = Visibility.Visible;  // Показать кнопку редактирования
            }
            else
            {
                editClientButton.Visibility = Visibility.Collapsed;  // Скрыть кнопку редактирования
            }
        }

        /// <summary>
        /// Проверка неактивности - выход из учетной записи
        /// </summary>
        private void CheckInactivity(object sender, EventArgs e)
        {
            inactivityTimer.Stop();
            Auth.BackToAuth();
        }

        /// <summary>
        /// Сброс таймера неактивности при движении мыши
        /// </summary>
        private void HandleActivity(object sender, MouseEventArgs e)
        {
            inactivityTimer.Stop();
            inactivityTimer.Start();
        }

        /// <summary>
        /// Сброс таймера неактивности при нажатии клавиш
        /// </summary>
        private void HandleActivity(object sender, KeyEventArgs e)
        {
            inactivityTimer.Stop();
            inactivityTimer.Start();
        }

        /// <summary>
        /// Кнопка "На главную" - закрытие формы
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            inactivityTimer.Stop();
            Auth.locker = true;
            this.Close();
        }

        /// <summary>
        /// Событие загрузки формы - инициализация UI, загрузка статусов клиентов,
        /// настройка масок ввода, запуск таймера неактивности
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Title += $" ({AccountHolder.UserRole}: {FullNameSplitter.MakeShortName(AccountHolder.FIO)})";
                inactivityTimer.Start();
            }
            catch
            {
                ;
            }

            try
            {
                RefreshDataGrid(true);           // Загрузка списка клиентов
                LoadClientStatuses();            // Загрузка статусов в ComboBox

                // Установка масок ввода
                phoneTextBox.Text = "+7 (___) ___-__-__";
                dateOfBirthDatePicker.DisplayDateEnd = DateTime.Now;  // Дата рождения не позже сегодня
                issueDate.DisplayDateEnd = DateTime.Now;              // Дата выдачи паспорта не позже сегодня
                departmentCodeTextBox.Text = "___-___";
                passportSeriesTextBox.Text = "____";
                passportNumberTextBox.Text = "______";

                // Начальное состояние кнопок
                inClaimButton.IsEnabled = false;
                editClientButton.IsEnabled = false;
                showClientButton.IsEnabled = false;
                endEditingButton.Visibility = Visibility.Collapsed;
                cancelChangesButton.Visibility = Visibility.Collapsed;
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Валидация ввода ФИО - разрешены русские буквы, дефис, пробел, Backspace
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
        /// Отмена редактирования
        /// </summary>
        private void CancelEdit(object sender, RoutedEventArgs e)
        {
            CloseEdition();
        }

        /// <summary>
        /// Выход из режима редактирования, возврат в режим просмотра/создания
        /// </summary>
        private void CloseEdition()
        {
            createClientButton.Visibility = Visibility.Visible;
            showClientButton.Visibility = Visibility.Visible;
            editClientButton.Visibility = Visibility.Visible;
            toMenuButton.Visibility = Visibility.Visible;

            endEditingButton.Visibility = Visibility.Collapsed;
            cancelChangesButton.Visibility = Visibility.Collapsed;

            ClearInputData();

            clientsDG.SelectedItem = null;
            searchByPassportSeriesAndNumber.IsEnabled = true;
            clientStatusCombobox.IsEnabled = false;
            showClientButton.IsEnabled = false;
            editClientButton.IsEnabled = false;
            clientStatusCombobox.SelectedItem = "Активный";
            clientsDG.IsEnabled = true;
            isEdit = false;
        }

        /// <summary>
        /// Активация кнопок при выборе клиента в DataGrid
        /// </summary>
        private void clientsDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            inClaimButton.IsEnabled = true;
            editClientButton.IsEnabled = true;
            showClientButton.IsEnabled = true;
        }

        /// <summary>
        /// Кнопка "Выбрать для заявки" - передача данных выбранного клиента в форму заявки
        /// </summary>
        private void inClaimButton_Click(object sender, RoutedEventArgs e)
        {
            if (clientsDG.SelectedItem != null)
            {
                DataRowView drv = clientsDG.SelectedItem as DataRowView;
                object[] clientData = drv.Row.ItemArray;

                // Проверка: клиент должен быть активным
                if (clientData[clientData.Length - 1].ToString() != "Активный")
                {
                    MessageBox.Show($"Не удалось добавить клиента в заявку, статус клиента не активный", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                ClientHolder.data = clientData;  // Сохранение данных в статическом хранилище
                inactivityTimer.Stop();
                this.Close();
            }
        }

        /// <summary>
        /// Загрузка статусов клиентов из БД
        /// </summary>
        private void LoadClientStatuses()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(@"Select `status_name` from client_status;", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);

                    try
                    {
                        List<string> statuses = new List<string>();
                        foreach (DataRow record in dt.Rows)
                        {
                            statuses.Add(record.ItemArray[0].ToString());
                        }
                        clientStatusCombobox.ItemsSource = statuses;
                        clientStatusCombobox.SelectedIndex = 0;  // Первый статус (обычно "Активный")
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show($"Не удалось загрузить статусы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Автоматическое форматирование ФИО: первая буква каждого слова заглавная, остальные строчные
        /// </summary>
        private void fioTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                // Пропуск служебных клавиш
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

                    // Восстановление позиции курсора
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
        /// Автоматическое форматирование номера телефона при вводе
        /// </summary>
        private void phoneTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var phoneTextBox = sender as TextBox;
            int currentPos = phoneTextBox.CaretIndex;
            try
            {
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
        /// При получении фокуса - установка курсора на первый символ маски "_"
        /// </summary>
        private void phoneTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var phoneTextBox = sender as TextBox;
            int targetIndex = phoneTextBox.Text.IndexOf("_");
            if (targetIndex != -1)
                phoneTextBox.CaretIndex = targetIndex;
            else
                phoneTextBox.CaretIndex = phoneTextBox.Text.Length;
        }

        /// <summary>
        /// При захвате мыши - установка курсора на первый символ маски "_"
        /// </summary>
        private void phoneTextBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            var phoneTextBox = sender as TextBox;
            int targetIndex = phoneTextBox.Text.IndexOf("_");
            if (targetIndex != -1)
                phoneTextBox.CaretIndex = targetIndex;
            else
                phoneTextBox.CaretIndex = phoneTextBox.Text.Length;
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
        /// Ограничение ввода email: только один символ '@'
        /// </summary>
        private void emailTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var emailTextBox = sender as TextBox;
            string email = emailTextBox.Text;
            if (e.Key == Key.D2)  // Клавиша '@' (для русской раскладки D2)
            {
                if (emailTextBox.Text.Length > 0)
                {
                    if (LimitOneLetterInput(emailTextBox, '@'))
                        e.Handled = true;
                    else
                        e.Handled = false;
                }
            }
        }

        /// <summary>
        /// Проверка на наличие символа в тексте (для ограничения количества)
        /// </summary>
        private bool LimitOneLetterInput(TextBox textBox, char letter)
        {
            return textBox.Text.Count(c => c == letter) > 0;
        }

        /// <summary>
        /// Валидация ввода адреса проживания
        /// </summary>
        private void placeOfResidenceTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                Regex regex = new Regex(@"[0-9А-Яа-я-/.,\b\s]");
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
        /// Кнопка "Сгенерировать" - генерация логина и пароля абонента
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (isEdit)
            {
                MessageBoxResult res = MessageBox.Show("Вы уверены, что хотите изменить пароль и логин абонента?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes)
                    return;
                else
                    isGenerateNewCredentials = true;
            }

            // Генерация пароля
            char[] targetCharsPassword = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM0123456789".ToCharArray();
            char[] mixedCharsPassword = CredentialsGenerator.MixChars(targetCharsPassword);
            string generatePassword = CredentialsGenerator.GenerateCredential(mixedCharsPassword);
            abonentPasswordTextBox.Text = generatePassword;

            // Генерация логина: случайное слово + случайное число
            string[] loginPart = new string[] { "apple","bridge","cloud", "dream","eagle","forest", "garden","horizon",
                "island","jungle","kite","lion","mountain","night","ocean","pencil","queen","river","sunshine","tree",
                "umbrella","violet","window","xylophone","yellow","zebra","adventure","butterfly","castle","desert",
                "elephant","flower","guitar","honey","iceberg","jewel","kangaroo","lake","meadow","nectar","orchid",
                "penguin","quartz","rainbow","star","tiger","universe","valley","whisper","yacht","zeppelin"};
            StringBuilder sb = new StringBuilder();
            sb.Append(loginPart[new Random().Next(0, loginPart.Length - 1)]);
            sb.Append(new Random().Next(10000, 999999));
            abonentLoginTextBox.Text = sb.ToString();
        }

        /// <summary>
        /// Валидация ввода паспортных данных (серия, номер, код подразделения)
        /// </summary>
        private void seriesAndNumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                Regex regex = new Regex(@"[0-9\b\s]");
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
        /// Валидация ввода "Кем выдан" - русские буквы, цифры, дефис, точка, запятая, пробел
        /// </summary>
        private void issuedByTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                Regex regex = new Regex(@"[0-9А-Яа-я-/.,\b\s]");
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
        /// Валидация ввода полей с маской - только цифры
        /// </summary>
        private void departmentCodeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                Regex regex = new Regex(@"[0-9\b\s]");
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
        /// Валидация ввода дат - разрешены только Backspace и пробел
        /// </summary>
        private void Date_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[\b\s]");
            if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                e.Handled = false;
            else
                e.Handled = true;
        }

        /// <summary>
        /// Автоматическое форматирование кода подразделения (формат: XXX-XXX)
        /// </summary>
        private void departmentCodeTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var departmentCodeTextBox = sender as TextBox;
            try
            {
                if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.LeftAlt || e.Key == Key.LeftShift ||
                    e.Key == Key.LeftCtrl || e.Key == Key.CapsLock || e.Key == Key.System)
                    return;

                if (e.Key == Key.Back)
                {
                    departmentCodeTextBox.Text = "___-___";
                    departmentCodeTextBox.CaretIndex = 0;
                    return;
                }

                int caretIndex = departmentCodeTextBox.CaretIndex;
                int departmentCodeLength = departmentCodeTextBox.Text.Length;
                if (departmentCodeLength > 0)
                {
                    string[] departmentCodeByParts = departmentCodeTextBox.Text.Split('-');
                    for (int i = 0; i < departmentCodeByParts.Length; i++)
                    {
                        string part = departmentCodeByParts[i];
                        switch (i)
                        {
                            case 0:
                                departmentCodeByParts[i] = part.Substring(0, 3);
                                break;
                            case 1:
                                departmentCodeByParts[i] = part.Substring(0, 3);
                                break;
                        }
                    }
                    departmentCodeTextBox.Text = string.Join("-", departmentCodeByParts);
                    if (caretIndex == 3)
                        departmentCodeTextBox.CaretIndex = ++caretIndex;
                    else
                        departmentCodeTextBox.CaretIndex = caretIndex;
                }
            }
            catch
            {
                ;
            }
        }

        /// <summary>
        /// Кнопка "Создать клиента" - добавление нового клиента в БД
        /// </summary>
        private void createClientButton_Click(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled;
            try
            {
                requiredFieldsIsFilled = fioTextBox.Text.Split(' ').Length >= 1
                    && regexForPhoneNumber.IsMatch(phoneTextBox.Text)
                    && dateOfBirthDatePicker.SelectedDate != null
                    && placeOfResidenceTextBox.Text.Length > 0
                    && abonentLoginTextBox.Text.Length > 0
                    && regexForPassportSeries.IsMatch(passportSeriesTextBox.Text)
                    && regexForPassportNumber.IsMatch(passportNumberTextBox.Text)
                    && issuedByTextBox.Text.Length > 0
                    && issueDate.SelectedDate != null
                    && regexForDepartmentCode.IsMatch(departmentCodeTextBox.Text);
            }
            catch
            {
                MessageBox.Show("Некорректно заполнены обязательные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (requiredFieldsIsFilled)
            {
                // Проверка на дубликаты: паспорт, телефон, логин
                if (!CheckDuplicateUtil.HasNoDuplicate("client", "concat_ws(' ', passport_series, passport_number)", $"{passportSeriesTextBox.Text} {passportNumberTextBox.Text}"))
                {
                    MessageBox.Show($"Не удалось добавить клиента. Обнаружен дубликат серии и номера паспорта", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (!CheckDuplicateUtil.HasNoDuplicate("client", "`phone_number`", phoneTextBox.Text))
                {
                    MessageBox.Show($"Не удалось добавить клиента. Обнаружен дубликат номера телефона", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (!CheckDuplicateUtil.HasNoDuplicate("client", "`subscriber_login`", abonentLoginTextBox.Text))
                {
                    MessageBox.Show($"Не удалось добавить клиента. Обнаружен дубликат логина абонента", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Формирование SQL-запроса (email опционален)
                string emailFieldName = "";
                string emailFieldValue = "";
                string singleQuote = "";
                string hasComma = "";
                if (regexForEmail.IsMatch(emailTextBox.Text))
                {
                    emailFieldName = " , email";
                    emailFieldValue = $"{emailTextBox.Text}";
                    hasComma = ", ";
                    singleQuote = "'";
                }

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        MySqlCommand cmd = new MySqlCommand($@"Insert into `client`(full_name, phone_number, place_of_residence, birthdate, subscriber_login, subscriber_password, passport_series, passport_number, issued_by, issue_date, department_code, client_status_id{emailFieldName}) 
                                                            value(
                                                                '{fioTextBox.Text}',
                                                                '{phoneTextBox.Text}',
                                                                '{placeOfResidenceTextBox.Text}',
                                                                '{((DateTime)dateOfBirthDatePicker.SelectedDate).ToString("yyyy-MM-dd")}',
                                                                '{abonentLoginTextBox.Text}',
                                                                '{abonentPasswordTextBox.Text}',
                                                                 {passportSeriesTextBox.Text},
                                                                 {passportNumberTextBox.Text},
                                                                '{issuedByTextBox.Text}',
                                                                '{((DateTime)issueDate.SelectedDate).ToString("yyyy-MM-dd")}',
                                                                '{departmentCodeTextBox.Text}',
                                                                 (SELECT `idclient_status` FROM `client_status` where `status_name` = '{clientStatusCombobox.SelectedItem.ToString()}'){hasComma}
                                                                {singleQuote}{emailFieldValue}{singleQuote}
                                                            );", conn);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Клиент создан", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearInputData();
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось создать нового клиента\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                try
                {
                    RefreshDataGrid(false);
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось обновить отображение\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
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
            emailTextBox.Text = "";
            dateOfBirthDatePicker.SelectedDate = null;
            placeOfResidenceTextBox.Text = "";
            abonentLoginTextBox.Text = "";
            abonentPasswordTextBox.Text = "";
            issuedByTextBox.Text = "";
            issueDate.SelectedDate = null;
            departmentCodeTextBox.Text = "___-___";
            searchByPassportSeriesAndNumber.Text = "";
            passportSeriesTextBox.Text = "____";
            passportNumberTextBox.Text = "______";
        }

        /// <summary>
        /// Кнопка "Очистить поля" - сброс всех введенных данных
        /// </summary>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ClearInputData();
            _currentPage = 1;
            _pageSize = 2;
        }

        /// <summary>
        /// Автоматическое форматирование серии паспорта (4 цифры)
        /// </summary>
        private void passportSeriesTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var passportSeriesTextBox = sender as TextBox;
            try
            {
                if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.LeftAlt || e.Key == Key.LeftShift ||
                    e.Key == Key.LeftCtrl || e.Key == Key.CapsLock || e.Key == Key.System)
                    return;

                if (e.Key == Key.Back)
                {
                    passportSeriesTextBox.Text = "____";
                    passportSeriesTextBox.CaretIndex = 0;
                    return;
                }

                int caretIndex = passportSeriesTextBox.CaretIndex;
                if (passportSeriesTextBox.Text.Length > 0)
                {
                    string part = passportSeriesTextBox.Text;
                    passportSeriesTextBox.Text = part.Substring(0, 4);
                    passportSeriesTextBox.CaretIndex = caretIndex;
                }
            }
            catch
            {
                ;
            }
        }

        /// <summary>
        /// Автоматическое форматирование номера паспорта (6 цифр)
        /// </summary>
        private void passportNumberTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var passportNumberTextBox = sender as TextBox;
            try
            {
                if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.LeftAlt || e.Key == Key.LeftShift ||
                    e.Key == Key.LeftCtrl || e.Key == Key.CapsLock || e.Key == Key.System)
                    return;

                if (e.Key == Key.Back)
                {
                    passportNumberTextBox.Text = "______";
                    passportNumberTextBox.CaretIndex = 0;
                    return;
                }

                int caretIndex = passportNumberTextBox.CaretIndex;
                if (passportNumberTextBox.Text.Length > 0)
                {
                    string part = passportNumberTextBox.Text;
                    passportNumberTextBox.Text = part.Substring(0, 6);
                    passportNumberTextBox.CaretIndex = caretIndex;
                }
            }
            catch
            {
                ;
            }
        }

        /// <summary>
        /// Обновление DataGrid со списком клиентов
        /// </summary>
        /// <param name="isInitial">true - начальная сортировка по ФИО, false - сортировка по ID (новые сверху)</param>
        private void RefreshDataGrid(bool isInitial)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                conn.Open();

                // SQL-запрос с маскированием конфиденциальных данных
                string cmdText = $@"Select idclient, full_name, email, phone_number, place_of_residence, birthdate, 
                                          subscriber_login, subscriber_password, passport_series, passport_number, 
                                          issued_by, issue_date, department_code, 
                                          concat('ФИО: ', full_name, '\nТелефон: ', concat('+7 (***) ***', substring(phone_number, 13)),'  |  ', 'Email: ', IFNULL(email, ''), 
                                          '\nАдрес проживания: ', place_of_residence, '\nДата рождения: ', REPEAT('*', CHAR_LENGTH(birthdate)), 
                                          '\nСерия паспорта: ', concat('**', substring(passport_series, 3)), '  |  ', 'Номер паспорта: ', concat('****', substring(passport_number, 5))) as clientDetails, 
                                          client_status.status_name as 'client_status' 
                                   from `client` 
                                   inner join `client_status` on `client`.client_status_id = client_status.idclient_status {filterOption} order by idclient desc;";

                if (isInitial)
                {
                    cmdText = $@"Select idclient, full_name, email, phone_number, place_of_residence, birthdate, 
                                       subscriber_login, subscriber_password, passport_series, passport_number, 
                                       issued_by, issue_date, department_code, 
                                       concat('ФИО: ', full_name, '\nТелефон: ', concat('+7 (***) ***', substring(phone_number, 13)),'  |  ', 'Email: ', IFNULL(email, ''), 
                                       '\nАдрес проживания: ', place_of_residence, '\nДата рождения: ', REPEAT('*', CHAR_LENGTH(birthdate)), 
                                       '\nСерия паспорта: ', concat('**', substring(passport_series, 3)), '  |  ', 'Номер паспорта: ', concat('****', substring(passport_number, 5))) as clientDetails, 
                                       client_status.status_name as 'client_status' 
                                from `client` 
                                inner join `client_status` on `client`.client_status_id = client_status.idclient_status order by full_name;";
                }

                MySqlCommand cmd = new MySqlCommand(cmdText, conn);
                MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                cmd.ExecuteNonQuery();
                da.Fill(dt);

                // Сокрытие части ФИО в таблице
                foreach (DataRow row in dt.Rows)
                {
                    string fio = row.ItemArray[1].ToString();
                    try
                    {
                        row.SetField<string>(1, FullNameSplitter.HideClientName(fio));
                    }
                    catch
                    {
                        ;
                    }
                }

                // Сохранение данных для пагинации
                _allRows.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    _allRows.Add(row);
                }

                UpdatePagination();

                // Подсчет активных клиентов
                countRecordsLabel.Content = RecordsCounter.CountRecords("client", "where client_status_id = (Select idclient_status from client_status where status_name = 'Активный')");
            }
        }

        /// <summary>
        /// Обновление элементов управления пагинации
        /// </summary>
        private void UpdatePagination()
        {
            int totalPages = (int)Math.Ceiling((double)_allRows.Count / _pageSize);
            lblTotalPages.Text = totalPages.ToString();

            if (_currentPage > totalPages && totalPages > 0)
                _currentPage = totalPages;
            if (_currentPage < 1)
                _currentPage = 1;

            txtPageNum.Text = _currentPage.ToString();

            btnPrev.IsEnabled = _currentPage > 1;
            btnNext.IsEnabled = _currentPage < totalPages;

            DisplayCurrentPage();
        }

        /// <summary>
        /// Отображение данных текущей страницы
        /// </summary>
        private void DisplayCurrentPage()
        {
            if (_allRows.Count == 0)
            {
                clientsDG.ItemsSource = null;
                return;
            }

            int startIndex = (_currentPage - 1) * _pageSize;
            int endIndex = Math.Min(startIndex + _pageSize, _allRows.Count);

            DataTable pageTable = new DataTable();

            if (_allRows.Count > 0)
            {
                foreach (DataColumn col in _allRows[0].Table.Columns)
                {
                    pageTable.Columns.Add(col.ColumnName, col.DataType);
                }

                for (int i = startIndex; i < endIndex; i++)
                {
                    pageTable.ImportRow(_allRows[i]);
                }
            }

            clientsDG.ItemsSource = pageTable.AsDataView();
        }

        /// <summary>
        /// Кнопка "Предыдущая страница"
        /// </summary>
        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdatePagination();
            }
        }

        /// <summary>
        /// Кнопка "Следующая страница"
        /// </summary>
        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)_allRows.Count / _pageSize);
            if (_currentPage < totalPages)
            {
                _currentPage++;
                UpdatePagination();
            }
        }

        /// <summary>
        /// Ручной ввод номера страницы
        /// </summary>
        private void txtPageNum_LostFocus(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)_allRows.Count / _pageSize);
            if (int.TryParse(txtPageNum.Text, out int newPage))
            {
                if (newPage >= 1 && newPage <= totalPages)
                {
                    _currentPage = newPage;
                    UpdatePagination();
                }
                else
                {
                    txtPageNum.Text = _currentPage.ToString();
                }
            }
            else
            {
                txtPageNum.Text = _currentPage.ToString();
            }
        }

        /// <summary>
        /// Валидация - только цифры для номера страницы
        /// </summary>
        private void OnlyNumbers_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Поиск клиента по паспортным данным или ФИО
        /// </summary>
        private void searchByPassportSeriesAndNumber_TextChanged(object sender, TextChangedEventArgs e)
        {
            int passportNum;
            string target = searchByPassportSeriesAndNumber.Text;
            if (target.Length >= 3 || int.TryParse(target, out passportNum) && target.Length > 0)
            {
                filterOption = $"where full_name like '%{target}%' or passport_series = '{target}' or passport_number = '{target}'";
            }
            else
                filterOption = "";
            RefreshDataGrid(false);
        }

        /// <summary>
        /// Просмотр клиента (открытие формы ClientVerbose)
        /// </summary>
        private void showClient_Click(object sender, RoutedEventArgs e)
        {
            if (clientsDG.SelectedItem != null)
            {
                DataRowView drv = clientsDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;
                inactivityTimer.Stop();
                this.Hide();
                var win = new ClientVerbose(fieldValuesOfARecord, RefreshDataGrid);
                win.ShowDialog();
                inactivityTimer.Start();
                this.ShowDialog();
            }
        }

        /// <summary>
        /// Кнопка "Редактировать клиента" - переход в режим редактирования
        /// </summary>
        private void editClientButton_Click(object sender, RoutedEventArgs e)
        {
            PrepareToEdit();
        }

        /// <summary>
        /// Подготовка к редактированию - заполнение полей данными выбранного клиента
        /// </summary>
        private void PrepareToEdit()
        {
            if (clientsDG.SelectedItem != null)
            {
                DataRowView drv = clientsDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;

                // Переключение UI в режим редактирования
                createClientButton.Visibility = Visibility.Collapsed;
                inClaimButton.Visibility = Visibility.Collapsed;
                editClientButton.Visibility = Visibility.Collapsed;
                showClientButton.Visibility = Visibility.Collapsed;
                toMenuButton.Visibility = Visibility.Collapsed;

                // Загрузка данных клиента
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"Select full_name from `client` where idclient = {fieldValuesOfARecord[0]};", conn);
                    fioTextBox.Text = cmd.ExecuteScalar().ToString().Trim();
                }

                abonentLoginTextBox.Clear();
                abonentPasswordTextBox.Clear();

                emailTextBox.Text = fieldValuesOfARecord[2].ToString().Trim();
                phoneTextBox.Text = fieldValuesOfARecord[3].ToString().Trim();
                placeOfResidenceTextBox.Text = fieldValuesOfARecord[4].ToString().Trim();
                dateOfBirthDatePicker.SelectedDate = DateTime.Parse(((DateTime)fieldValuesOfARecord[5]).ToString("dd.MM.yyyy"));
                passportSeriesTextBox.Text = fieldValuesOfARecord[8].ToString().Trim();
                passportNumberTextBox.Text = fieldValuesOfARecord[9].ToString().Trim();
                issuedByTextBox.Text = fieldValuesOfARecord[10].ToString().Trim();
                issueDate.SelectedDate = DateTime.Parse(((DateTime)fieldValuesOfARecord[11]).ToString("dd.MM.yyyy"));
                departmentCodeTextBox.Text = fieldValuesOfARecord[12].ToString().Trim();
                clientStatusCombobox.Text = fieldValuesOfARecord[14].ToString().Trim();

                // Блокировка UI во время редактирования
                searchByPassportSeriesAndNumber.IsEnabled = false;
                clientStatusCombobox.IsEnabled = true;
                clientsDG.IsEnabled = false;
                isGenerateNewCredentials = false;
                isEdit = true;

                endEditingButton.Visibility = Visibility.Visible;
                cancelChangesButton.Visibility = Visibility.Visible;

                // Сохранение ID клиента
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        MySqlCommand cmd = new MySqlCommand($"SELECT idclient FROM `client` where concat_ws(' ', passport_series, passport_number) = '{passportSeriesTextBox.Text} {passportNumberTextBox.Text}';", conn);
                        clientId = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось обновить данные клиента\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
        }

        /// <summary>
        /// Сохранение изменений при редактировании клиента
        /// </summary>
        private void EditClaim(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled;
            try
            {
                requiredFieldsIsFilled = fioTextBox.Text.Split(' ').Length >= 1
                    && regexForPhoneNumber.IsMatch(phoneTextBox.Text)
                    && dateOfBirthDatePicker.SelectedDate != null
                    && placeOfResidenceTextBox.Text.Length > 0
                    && regexForPassportSeries.IsMatch(passportSeriesTextBox.Text)
                    && regexForPassportNumber.IsMatch(passportNumberTextBox.Text)
                    && issuedByTextBox.Text.Length > 0
                    && issueDate.SelectedDate != null
                    && regexForDepartmentCode.IsMatch(departmentCodeTextBox.Text);
            }
            catch
            {
                MessageBox.Show("Некорректно заполнены обязательные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (requiredFieldsIsFilled)
            {
                // Проверка на дубликаты при редактировании (исключая текущего клиента)
                int duplicatePassportClientId = CheckDuplicateUtil.HasNoDuplicate("client", "concat_ws(' ', passport_series, passport_number)", $"{passportSeriesTextBox.Text} {passportNumberTextBox.Text}", true);
                int duplicatePhoneClientId = CheckDuplicateUtil.HasNoDuplicate("client", "phone_number", phoneTextBox.Text, false);
                int duplicateLoginClientId = CheckDuplicateUtil.HasNoDuplicate("client", "subscriber_login", abonentLoginTextBox.Text, false);

                if (duplicatePassportClientId != clientId && duplicatePassportClientId != -1)
                {
                    MessageBox.Show($"Не удалось редактировать клиента. Обнаружен дубликат серии и номера паспорта", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (duplicatePhoneClientId != clientId && duplicatePhoneClientId != -1)
                {
                    MessageBox.Show($"Не удалось редактировать клиента. Обнаружен дубликат номера телефона", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (duplicateLoginClientId != clientId && duplicateLoginClientId != -1 && isGenerateNewCredentials)
                {
                    MessageBox.Show($"Не удалось редактировать клиента. Обнаружен дубликат логина абонента", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            string mainQuery = $@"Update `client` 
                                                set full_name = '{fioTextBox.Text}',
                                                `email` = '{emailTextBox.Text}',
                                                phone_number = '{phoneTextBox.Text}',
                                                place_of_residence = '{placeOfResidenceTextBox.Text}',
                                                birthdate = '{((DateTime)dateOfBirthDatePicker.SelectedDate).ToString("yyyy-MM-dd")}',
                                                passport_series = { passportSeriesTextBox.Text},
                                                passport_number = { passportNumberTextBox.Text},
                                                issued_by = '{issuedByTextBox.Text}',
                                                issue_date = '{((DateTime)issueDate.SelectedDate).ToString("yyyy-MM-dd")}',
                                                department_code = '{departmentCodeTextBox.Text}',
                                                client_status_id = (SELECT idclient_status FROM client_status where `status_name` = '{clientStatusCombobox.SelectedItem}')";

                            // Обновление учетных данных, если были сгенерированы новые
                            if (isGenerateNewCredentials)
                            {
                                mainQuery += $@", subscriber_login = '{abonentLoginTextBox.Text}',
                                                  subscriber_password = '{abonentPasswordTextBox.Text}'";
                            }

                            MySqlCommand cmd2 = new MySqlCommand($@"{mainQuery} where idclient = {clientId};", conn);
                            cmd2.ExecuteNonQuery();
                            MessageBox.Show($"Данные клиента успешно обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            CloseEdition();
                            RefreshDataGrid(false);
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show($"Не удалось обновить данные клиента\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
        /// Адаптация интерфейса при изменении размера окна
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double windowHeight = e.NewSize.Height;
            double baseFontSize = 14;
            int newPageSize = _pageSize;

            if (windowHeight > 800)
            {
                newPageSize = 2;
                double scale = windowHeight / 700;
                int newFontSize = (int)(baseFontSize * Math.Min(scale, 1.5));
                UpdateButtonsFontSize(newFontSize);
            }
            else if (windowHeight < 600)
            {
                newPageSize = 2;
                double scale = windowHeight / 700;
                int newFontSize = (int)Math.Max(baseFontSize * scale, 10);
                UpdateButtonsFontSize(newFontSize);
            }
            else
            {
                newPageSize = 2;
                UpdateButtonsFontSize((int)baseFontSize);
            }

            if (newPageSize != _pageSize)
            {
                _pageSize = newPageSize;
                int totalPages = (int)Math.Ceiling((double)_allRows.Count / _pageSize);
                if (_currentPage > totalPages && totalPages > 0)
                    _currentPage = totalPages;
                else if (_currentPage < 1)
                    _currentPage = 1;
                UpdatePagination();
                DisplayCurrentPage();
            }
        }

        /// <summary>
        /// Обновление размера шрифта для кнопок и элементов управления
        /// </summary>
        private void UpdateButtonsFontSize(int fontSize)
        {
            var buttons = new[] { showClientButton, createClientButton, editClientButton, inClaimButton,
                                  toMenuButton, endEditingButton, cancelChangesButton };
            foreach (var button in buttons)
            {
                if (button != null)
                    button.FontSize = fontSize;
            }

            if (clientStatusCombobox != null)
                clientStatusCombobox.FontSize = fontSize;

            if (clientsDG != null)
                clientsDG.FontSize = fontSize;
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