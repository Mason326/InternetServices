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
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Window14.xaml
    /// </summary>
    public partial class CreateClient : Window
    {
        bool prevBack = false;
        int clientId;
        bool isEdit = false;
        bool isGenerateNewCredentials;
        Regex regexForEmail = new Regex("^[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?$");
        Regex regexForPhoneNumber = new Regex(@"^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$");
        Regex regexForPassportSeries = new Regex(@"^[0-9]{4}$");
        Regex regexForPassportNumber = new Regex(@"^[0-9]{6}$");
        Regex regexForDepartmentCode = new Regex(@"^\d{3}-\d{3}$");
        string filterOption = "";

        public CreateClient(bool isSelectClient)
        {
            InitializeComponent();
            if (!isSelectClient)
            {
                inClaimButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshDataGrid();
                LoadClientStatuses();
                phoneTextBox.Text = "+7 (___) ___-__-__";
                dateOfBirthDatePicker.DisplayDateEnd = DateTime.Now;
                issueDate.DisplayDateEnd = DateTime.Now;
                departmentCodeTextBox.Text = "___-___";
                passportSeriesTextBox.Text = "____";
                passportNumberTextBox.Text = "______";
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

        private void CancelEdit(object sender, RoutedEventArgs e)
        {
            CloseEdition();
        }

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

        private void clientsDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            inClaimButton.IsEnabled = true;
            editClientButton.IsEnabled = true;
            showClientButton.IsEnabled = true;
        }

        private void inClaimButton_Click(object sender, RoutedEventArgs e)
        {
            if (clientsDG.SelectedItem != null)
            {
                DataRowView drv = clientsDG.SelectedItem as DataRowView;
                ClientHolder.data = drv.Row.ItemArray;
                this.Close();
            }
        }

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
                        clientStatusCombobox.SelectedIndex = 0;
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

        private string ToTitle(string text)
        {
            return $"{text[0].ToString().ToUpper()}{text.Substring(1, text.Length - 1)}";
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

        private void phoneTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var phoneTextBox = sender as TextBox;
            int targetIndex = phoneTextBox.Text.IndexOf("_");
            if (targetIndex != -1)
                phoneTextBox.CaretIndex = targetIndex;
            else
                phoneTextBox.CaretIndex = phoneTextBox.Text.Length;
        }

        private void phoneTextBox_GotMouseCapture(object sender, MouseEventArgs e)
        {
            var phoneTextBox = sender as TextBox;
            int targetIndex = phoneTextBox.Text.IndexOf("_");
            if (targetIndex != -1)
                phoneTextBox.CaretIndex = targetIndex;
            else
                phoneTextBox.CaretIndex = phoneTextBox.Text.Length;
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

        private void emailTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var emailTextBox = sender as TextBox;
            string email = emailTextBox.Text;
            if (e.Key == Key.D2)
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

        private bool LimitOneLetterInput(TextBox textBox, char letter)
        {
            return textBox.Text.Count(c => c == letter) > 0;
        }

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
            char[] targetCharsPassword = "qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM0123456789".ToCharArray();
            char[] mixedCharsPassword = CredentialsGenerator.MixChars(targetCharsPassword);
            string generatePassword = CredentialsGenerator.GenerateCredential(mixedCharsPassword);

            abonentPasswordTextBox.Text = generatePassword;
            string[] loginPart = new string[] { "apple","bridge","cloud", "dream","eagle","forest", "garden","horizon","island","jungle","kite","lion","mountain","night","ocean","pencil","queen","river","sunshine","tree","umbrella","violet","window","xylophone","yellow","zebra","adventure","butterfly","castle","desert","elephant","flower","guitar","honey","iceberg","jewel","kangaroo","lake","meadow","nectar","orchid","penguin","quartz","rainbow","star","tiger","universe","valley","whisper","yacht","zeppelin"};
            StringBuilder sb = new StringBuilder();
            sb.Append(loginPart[new Random().Next(0, loginPart.Length - 1)]);
            sb.Append(new Random().Next(10000, 999999));
            abonentLoginTextBox.Text = sb.ToString();
        }

        
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

        private void Date_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[\b\s]");
            if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                e.Handled = false;
            else
                e.Handled = true;
        }

        private void departmentCodeTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var departmentCodeTextBox = sender as TextBox;
            try
            {
                if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.LeftAlt || e.Key == Key.LeftShift || e.Key == Key.LeftCtrl || e.Key == Key.CapsLock || e.Key == Key.System)
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
                        string fpart;
                        string spart;
                        switch (i)
                        {
                            case 0:
                                fpart = part.Substring(0, 3);
                                departmentCodeByParts[i] = fpart;
                                break;
                            case 1:
                                spart = part.Substring(0, 3);
                                departmentCodeByParts[i] = spart;
                                break;
                        }
                    }
                    departmentCodeTextBox.Text = string.Join("-", departmentCodeByParts);
                    if(caretIndex == 3)
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
                    RefreshDataGrid();
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

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            ClearInputData();
        }

        private void passportSeriesTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var passportSeriesTextBox = sender as TextBox;
            try
            {
                if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.LeftAlt || e.Key == Key.LeftShift || e.Key == Key.LeftCtrl || e.Key == Key.CapsLock || e.Key == Key.System)
                    return;
                if (e.Key == Key.Back)
                {
                    passportSeriesTextBox.Text = "____";
                    passportSeriesTextBox.CaretIndex = 0;
                    return;
                }
                int caretIndex = passportSeriesTextBox.CaretIndex;
                int passportSeriesLength = passportSeriesTextBox.Text.Length;
                if (passportSeriesLength > 0)
                {
                    for (int i = 0; i < passportSeriesLength; i++)
                    {
                        string part = passportSeriesTextBox.Text;
                        string fpart;
                        fpart = part.Substring(0, 4);
                        passportSeriesTextBox.Text = fpart;
                    }
                    //departmentCodeTextBox.Text = string.Join("-", passportSeriesByParts);
                    passportSeriesTextBox.CaretIndex = caretIndex;
                }
            }
            catch
            {
                ;
            }
        }

        private void passportNumberTextBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var passportNumberTextBox = sender as TextBox;
            try
            {
                if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.LeftAlt || e.Key == Key.LeftShift || e.Key == Key.LeftCtrl || e.Key == Key.CapsLock || e.Key == Key.System)
                    return;
                if (e.Key == Key.Back)
                {
                    passportNumberTextBox.Text = "______";
                    passportNumberTextBox.CaretIndex = 0;
                    return;
                }
                int caretIndex = passportNumberTextBox.CaretIndex;
                int passportNumbersLength = passportNumberTextBox.Text.Length;
                if (passportNumbersLength > 0)
                {
                    for (int i = 0; i < passportNumbersLength; i++)
                    {
                        string part = passportNumberTextBox.Text;
                        string fpart;
                        fpart = part.Substring(0, 6);
                        passportNumberTextBox.Text = fpart;
                    }
                    //departmentCodeTextBox.Text = string.Join("-", passportSeriesByParts);
                    passportNumberTextBox.CaretIndex = caretIndex;
                }
            }
            catch
            {
                ;
            }
        }

        private void RefreshDataGrid() {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand($@"Select idclient, full_name, email, phone_number, place_of_residence, birthdate, subscriber_login, subscriber_password, passport_series, passport_number, issued_by, issue_date, department_code, client_status.status_name as 'client_status' from `client` inner join `client_status` on `client`.client_status_id = client_status.idclient_status  {filterOption} order by idclient desc;", conn);
                MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                cmd.ExecuteNonQuery();
                da.Fill(dt);
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
                clientsDG.ItemsSource = dt.AsDataView();
            }
        }

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
            RefreshDataGrid();
        }

        private void showClient_Click(object sender, RoutedEventArgs e)
        {
            if (clientsDG.SelectedItem != null)
            {
                DataRowView drv = clientsDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;
                this.Hide();
                var win = new ClientVerbose(fieldValuesOfARecord, RefreshDataGrid);
                win.ShowDialog();
                this.ShowDialog();
            }
        }

        private void editClientButton_Click(object sender, RoutedEventArgs e)
        {
            PrepareToEdit();
        }

        private void PrepareToEdit()
        {
            if (clientsDG.SelectedItem != null)
            {
                DataRowView drv = clientsDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;

                createClientButton.Visibility = Visibility.Collapsed;
                inClaimButton.Visibility = Visibility.Collapsed;
                editClientButton.Visibility = Visibility.Collapsed;
                showClientButton.Visibility = Visibility.Collapsed;
                toMenuButton.Visibility = Visibility.Collapsed;

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
                clientStatusCombobox.Text = fieldValuesOfARecord[13].ToString().Trim();

                searchByPassportSeriesAndNumber.IsEnabled = false;
                clientStatusCombobox.IsEnabled = true;
                clientsDG.IsEnabled = false;
                isGenerateNewCredentials = false;
                isEdit = true;

                endEditingButton.Visibility = Visibility.Visible;
                cancelChangesButton.Visibility = Visibility.Visible;
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
                            if (isGenerateNewCredentials)
                            {
                                mainQuery += $@", subscriber_login = '{abonentLoginTextBox.Text}',
                                                  subscriber_password = '{abonentPasswordTextBox.Text}'";
                            }
                            MySqlCommand cmd2 = new MySqlCommand($@"{mainQuery} where idclient = {clientId};", conn);
                            cmd2.ExecuteNonQuery();
                            MessageBox.Show($"Данные клента успешно обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            CloseEdition();
                            RefreshDataGrid();
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
    }
}
