using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Window16.xaml
    /// </summary>
    public partial class CreateClaim : Window
    {
        Dictionary<int, string> tariffs = new Dictionary<int, string>();
        const int INCOMING_CLAIM_STATUS_ID = 1;
        int recordsCount = 0;
        string filterOption = "";
        bool isEditing = false;
        public CreateClaim()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ClearSelected();
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //this.Hide();
            var win = new CreateClient(true);
            win.ShowDialog();
            //this.ShowDialog();
            if (ClientHolder.data != null)
            {
                object[] client = ClientHolder.data;
                string fioWithHiddenSurname = client[1].ToString();
                string hiddenPhoneNumber = HidePhoneNumber(client[3].ToString());
                clientTextBox.Text = $"{fioWithHiddenSurname}, {hiddenPhoneNumber}";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshData();
            dateOfExecution.IsEnabled = false;
            timeOfExecution.IsEnabled = false;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();

                    MySqlCommand cmd = new MySqlCommand(@"SELECT idtariff, tariff_name FROM tariff;", conn);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                    {
                        tariffs.Add(Convert.ToInt32(dr.GetValue(0)), dr.GetValue(1).ToString());
                    }
                    dr.Close();
                }
                tariffComboBox.ItemsSource = tariffs.Values;
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить тарифы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            UseStatusAsIncoming();
            editButton.IsEnabled = false;
            dateOfExecution.DisplayDateStart = DateTime.Now.AddDays(1);
        }

        private void UseStatusAsIncoming()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"SELECT `status` FROM claim_status where idclaim_status = {INCOMING_CLAIM_STATUS_ID};", conn);
                    string statusName = cmd.ExecuteScalar().ToString();
                    claimStatusComboBox.SelectedItem = statusName;
                }
                tariffComboBox.ItemsSource = tariffs.Values;
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить статус\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            claimStatusComboBox.ItemsSource = new string[] { "Входящая" };
            claimStatusComboBox.SelectedItem = "Входящая";
        }

        private void dateOfExecution_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            List<string> times = ShowAvailableTime();
            if (times.Count > 0)
                timeOfExecution.ItemsSource = times;
        }

        private List<string> ShowAvailableTime()
        {
            if (dateOfExecution.SelectedDate == null || MasterHolder.data == null)
            {
                timeOfExecution.IsEnabled = false;
                return new List<string>();
            }
            timeOfExecution.IsEnabled = true;
            try
            {
                recordsCount = 0;
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"SELECT mount_date FROM connection_claim where mount_date like '%{((DateTime)dateOfExecution.SelectedDate).ToString("yyyy-MM-dd")}%' and master_id = {MasterHolder.data[0]};", conn);
                    List<string> armoredTime = new List<string>();
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            armoredTime.Add(DateTime.Parse(dr.GetValue(0).ToString()).ToString("HH:mm"));
                            recordsCount++;
                        }
                    }
                    List<string> timePeriodArr = new List<string>() { "09:00", "09:30", "10:00", "10:30", "11:00", "11:30", "12:00", "12:30", "13:00", "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30", "17:00" };
                    List<string> res = new List<string>();
                    if (armoredTime.Count > 0)
                        res = timePeriodArr.Where(el => !armoredTime.Contains(el)).ToList();
                    else
                        res = timePeriodArr;
                    return res;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<string>();
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                Regex regex = new Regex(@"[0-9А-Яа-я-.,\b\s]");
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

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (mountAddressTextBox.Text.Length > 0 && clientTextBox.Text.Length > 0 && dateOfExecution.SelectedDate != null && timeOfExecution.SelectedItem != null && tariffComboBox.SelectedItem != null && masterTextBox.Text.Length > 0)
            {
                try
                {
                    if (recordsCount > 6)
                    {
                        MessageBox.Show($"Не удалось добавить заявку. Указанный мастер превысил количество взятых заявок в сутки", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            MySqlCommand cmd = new MySqlCommand($@"Insert into connection_claim(id_claim, connection_address, mount_date, employees_id, client_id, claim_status_id, connection_creationDate, tariff_id, master_id)
                                                               value (
                                                                {claimNumber.Content},
                                                                '{mountAddressTextBox.Text}',
                                                                '{((DateTime)dateOfExecution.SelectedDate).ToString("yyyy-MM-dd")} {timeOfExecution.SelectedItem}',
                                                                {AccountHolder.userId},
                                                                {ClientHolder.data[0]},
                                                                (Select `idclaim_status` from claim_status where `status` = '{claimStatusComboBox.SelectedItem}'),
                                                                '{DateTime.Parse(creationDate.Content.ToString()).ToString("yyyy-MM-dd")}',
                                                                {tariffs.Where(pair => pair.Value == tariffComboBox.SelectedItem.ToString()).Select(pair => pair.Key).Single()},
                                                                {MasterHolder.data[0]}
                                                                 );", conn);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Заявка успешно создана", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            RefreshData();
                            ClearSelected();
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show($"Не удалось создать заявку\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void RefreshData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"Select `id_claim`, `connection_creationDate`, `mount_date`, `connection_address`, tariff.`tariff_name` as 'tariff', client.full_name as 'client_fio', employees.full_name as 'employee_fio', claim_status.status as 'claim_status', (Select full_name from employees where idemployees = connection_claim.master_id) as 'master_fio'
                                                        from `connection_claim`
                                                        inner join `client` on client.idclient = connection_claim.client_id
                                                        inner join `employees` on employees.idemployees = connection_claim.employees_id
                                                        inner join `tariff` on tariff.idtariff = connection_claim.tariff_id
                                                        inner join `claim_status` on `claim_status`.idclaim_status = connection_claim.claim_status_id {filterOption};", conn);
                    DataTable dt = new DataTable();
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        DataColumn[] columns = new DataColumn[dr.FieldCount];
                        for (int i = 0; i < columns.Length; i++)
                        {
                            columns[i] = new DataColumn(dr.GetName(i), dr.GetFieldType(i));
                        }

                        dt.Columns.AddRange(columns);
                        dt.Columns.Add("isExpired", Type.GetType("System.Boolean"));
                        object[] record = new object[dr.FieldCount + 1];
                        while (dr.Read())
                        {
                            dr.GetValues(record);
                            DateTime executionDate = (DateTime)record[2];
                            if ((executionDate < DateTime.Now && record[7].ToString() == "Входящая") || (executionDate < DateTime.Today.AddDays(1) && record[7].ToString() == "В работе"))
                                record[record.Length - 1] = true;
                            else
                                record[record.Length - 1] = false;
                            dt.LoadDataRow(record, true);
                        }
                    }
                   

                    foreach (DataRow row in dt.Rows)
                    {
                        string fio = row.ItemArray[5].ToString();
                        row.SetField<string>(5, HideName(fio));
                    }

                    claimsDG.ItemsSource = dt.AsDataView();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            UseNewClaimNumber();
        }

        private void UseNewClaimNumber()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    int claimNum;
                    try
                    {
                        MySqlCommand cmd = new MySqlCommand(@"SELECT max(`id_claim`) FROM connection_claim;", conn);
                        claimNum = int.Parse(cmd.ExecuteScalar().ToString()) + 1;
                    }
                    catch
                    {
                        claimNum = 1;
                    }
                    claimNumber.Content = claimNum.ToString();
                    creationDate.Content = DateTime.Now.ToString("dd.MM.yyyy");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearSelected()
        {
            dateOfExecution.SelectedDate = null;
            tariffComboBox.SelectedItem = null;
            ClientHolder.data = null;
            timeOfExecution.SelectedItem = null;
            recordsCount = 0;
            masterTextBox.Clear();
            MasterHolder.data = null;
            searchByClaimNumAndFio.Clear();
            if (!isEditing)
            {
                clientTextBox.Clear();
                mountAddressTextBox.Clear();
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            ClearSelected();
        }

        private void dateOfExecution_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[\b\s]");
            if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                e.Handled = false;
            else
                e.Handled = true;
        }

        private string HideName(string fullName)
        {
            try
            {
                string[] clientFio = fullName.Split(' ');
                string hidden_name = $"{clientFio[1]} {clientFio[2]} {clientFio[0][0]}.";
                return hidden_name;
            }
            catch
            {
                return fullName;
            }
        }

        private string HidePhoneNumber(string phoneNumber)
        {
            char[] phoneNumberByLetters = phoneNumber.ToCharArray().Where(c => c != ' ').ToArray();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < phoneNumberByLetters.Length; i++)
            {
                if (i == 2 || i == 7)
                    sb.Append(' ');
                if (i >= 3 && i <= 5 || i >= 7 && i <= 9)
                {
                    sb.Append('#');
                    continue;
                }
                sb.Append(phoneNumberByLetters[i]);
            }
            return sb.ToString();
        }

        private void ShowClaimVerbose(object sender, RoutedEventArgs e)
        {
            if (claimsDG.SelectedItem != null)
            {
                DataRowView drv = claimsDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;
                this.Hide();
                var win = new ClaimVerbose(fieldValuesOfARecord, true, RefreshData, RereleaseClaim);
                win.ShowDialog();
                this.ShowDialog();
            }
        }

        private void PrepareToEditClaim(object sender, RoutedEventArgs e)
        {
            if (claimsDG.SelectedItem != null)
            {
                DataRowView drv = claimsDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;
                if (fieldValuesOfARecord[7].ToString() == "Закрыта" || fieldValuesOfARecord[7].ToString() == "В работе")
                {
                    MessageBox.Show($"Заявки с такими статусами недоступны для редактирования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (fieldValuesOfARecord[7].ToString() == "Отменена")
                {
                    MessageBoxResult res = MessageBox.Show($"Заявка отменена и не подлежит изменению. Хотите перевыпустить заявку?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                    if (res == MessageBoxResult.Yes)
                    {
                        RereleaseClaim(fieldValuesOfARecord);
                    }
                    return;
                }
                string[] dateByParts = fieldValuesOfARecord[2].ToString().Split(' ');
                createClaimButton.Visibility = Visibility.Collapsed;
                showClaimButton.Visibility = Visibility.Collapsed;
                editButton.Visibility = Visibility.Collapsed;
                toMainButton.Visibility = Visibility.Collapsed;
                claimNumber.Content = fieldValuesOfARecord[0];
                creationDate.Content = ((DateTime)fieldValuesOfARecord[1]).ToString("dd.MM.yyyy");
                FillMasterObject(Convert.ToInt32(fieldValuesOfARecord[0]));
                
                if (!(DateTime.Parse(dateByParts[0]) < DateTime.Now))
                { 
                    dateOfExecution.SelectedDate = DateTime.Parse(dateByParts[0]);
                    string time = DateTime.Parse(dateByParts[1].ToString()).ToString("HH:mm");
                    List<string> times = ShowAvailableTime();
                    times.Add(time);
                    timeOfExecution.IsEnabled = true;
                    timeOfExecution.ItemsSource = times;
                    timeOfExecution.SelectedItem = time;
                }

                chooseAClientButton.IsEnabled = false;
                claimStatusComboBox.IsEnabled = true;
                mountAddressTextBox.IsEnabled = false;
                claimsDG.IsEnabled = false;
                mountAddressTextBox.Text = string.Join(" ", fieldValuesOfARecord[3].ToString().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
                FillComboBoxStatusesManager();
                claimStatusComboBox.SelectedItem = fieldValuesOfARecord[7];
                tariffComboBox.SelectedItem = fieldValuesOfARecord[4];
                clientTextBox.Text = fieldValuesOfARecord[5].ToString();
                masterTextBox.Text = MasterHolder.data[1].ToString();
                isEditing = true;

                endEditingButton.Visibility = Visibility.Visible;
                cancelChangesButton.Visibility = Visibility.Visible;
            }
        }

        private void CancelEdit(object sender, RoutedEventArgs e)
        {
            CloseEdition();
        }

        private void CloseEdition()
        {
            createClaimButton.Visibility = Visibility.Visible;
            showClaimButton.Visibility = Visibility.Visible;
            editButton.Visibility = Visibility.Visible;
            toMainButton.Visibility = Visibility.Visible;

            endEditingButton.Visibility = Visibility.Collapsed;
            cancelChangesButton.Visibility = Visibility.Collapsed;
            creationDate.Content = DateTime.Now.ToString("dd.MM.yyyy");
            isEditing = false;

            UseNewClaimNumber();
            UseStatusAsIncoming();
            ClearSelected();

            claimsDG.SelectedItem = null;
            mountAddressTextBox.IsEnabled = true;
            chooseAClientButton.IsEnabled = true;
            claimsDG.IsEnabled = true;
            timeOfExecution.IsEnabled = false;
            showClaimButton.IsEnabled = false;
            editButton.IsEnabled = false;
            claimStatusComboBox.IsEnabled = false;
        }

        private void EditClaim(object sender, RoutedEventArgs e)
        {
            if (mountAddressTextBox.Text.Length > 5 && clientTextBox.Text.Length > 0 && dateOfExecution.SelectedDate != null && timeOfExecution.SelectedItem != null && tariffComboBox.SelectedItem != null && masterTextBox.Text.Length > 0)
            {
                try
                {
                    ShowAvailableTime();
                    if (recordsCount > 6 && claimStatusComboBox.SelectedItem.ToString() != "Отменена")
                    {
                        MessageBox.Show($"Не удалось обновить заявку. Указанный мастер превысил количество взятых заявок в сутки", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            string fullExectionDate = $"{((DateTime)dateOfExecution.SelectedDate).ToString("yyyy-MM-dd")} {timeOfExecution.SelectedItem.ToString()}:00";
                            MySqlCommand cmd = new MySqlCommand($@"Update `connection_claim` 
                                                                   set connection_address = '{mountAddressTextBox.Text}',
                                                                   mount_date = '{fullExectionDate}',
                                                                   claim_status_id = (SELECT idclaim_status FROM claim_status where `status` = '{claimStatusComboBox.SelectedItem}'),
                                                                   tariff_id = (SELECT idtariff FROM tariff where `tariff_name` = '{tariffComboBox.SelectedItem}'),
                                                                   master_id = {MasterHolder.data[0]}
                                                                   where id_claim = {claimNumber.Content};", conn);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Заявка успешно обновлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            CloseEdition();
                            RefreshData();
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show($"Не удалось обновить заявку\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void FillComboBoxStatusesManager()
        {
            try
            {
                List<string> claimStatuses = new List<string>();
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"SELECT `status` FROM claim_status where `status` != 'В работе' && `status` != 'Закрыта';", conn);
                    MySqlDataReader dr = cmd.ExecuteReader();
                    while (dr.Read())
                        claimStatuses.Add(dr.GetValue(0).ToString());
                }
                claimStatusComboBox.ItemsSource = claimStatuses;
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось получить информацию о мастере\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillMasterObject(int claimId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"Select * from employees where idemployees = (SELECT master_id FROM connection_claim where id_claim = {claimId});", conn);
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    { 
                        object[] fieldValues = new object[dr.FieldCount];
                        while (dr.Read())
                            dr.GetValues(fieldValues);
                        MasterHolder.data = fieldValues;
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось получить информацию о мастере\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void claimsDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            showClaimButton.IsEnabled = true;
            editButton.IsEnabled = true;
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            var win = new EmployeesViewWindow();
            win.ShowDialog();

            if (MasterHolder.data != null)
            {
                object[] master = MasterHolder.data;
                string fio = master[1].ToString();
                masterTextBox.Text = fio;
            }
        }

        private void masterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (masterTextBox.Text.Length > 0)
            {
                dateOfExecution.IsEnabled = true;
                if(isEditing)
                {
                    dateOfExecution.SelectedDate = null;
                    timeOfExecution.IsEnabled = false;
                    timeOfExecution.SelectedItem = null;
                }
            }
            else
                dateOfExecution.IsEnabled = false;
        }

        private void searchByClaimNumAndFio_TextChanged(object sender, TextChangedEventArgs e)
        {
            int claimNum;
            string target = searchByClaimNumAndFio.Text;
            if (target.Length >= 3 || int.TryParse(target, out claimNum) && target.Length > 0)
            {
                filterOption = $"where `client`.full_name like '%{target}%' or id_claim = '{target}'";
            }
            else
                filterOption = "";
            RefreshData();
        }

        private void RereleaseClaim(object[] fieldValuesOfARecord)
        {
            ClearSelected();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"SELECT `client`.* FROM connection_claim inner join `client` on `client`.idclient = connection_claim.client_id where id_claim = {fieldValuesOfARecord[0]};", conn);
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        object[] clientData = new object[dr.FieldCount];
                        while (dr.Read())
                        {
                            dr.GetValues(clientData);
                        }
                        ClientHolder.data = clientData;
                        clientTextBox.Text = HideName(clientData[1].ToString());
                    }
                    mountAddressTextBox.Text = string.Join(" ", fieldValuesOfARecord[3].ToString().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)); ;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void claimsDG_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

    }
}