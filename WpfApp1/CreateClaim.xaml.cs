using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
    /// Interaction logic for Window16.xaml
    /// </summary>
    public partial class CreateClaim : Window
    {
        Dictionary<int, string> tariffs = new Dictionary<int, string>();
        const int INCOMING_CLAIM_STATUS_ID = 1;
        public CreateClaim()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var win = new CreateClient(true);
            win.ShowDialog();
            if (ClientHolder.data != null)
            {
                object[] client = ClientHolder.data;
                string fioWithHiddenSurname = HideClientName(client[1].ToString());
                string hiddenPhoneNumber = HideClientPhoneNumber(client[3].ToString());
                clientTextBox.Text = $"{fioWithHiddenSurname}, {hiddenPhoneNumber}";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshData();

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
            dateOfExecution.DisplayDateStart = DateTime.Now.AddDays(1);
        }

        private void dateOfExecution_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {

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
            if (mountAddressTextBox.Text.Length > 5 && clientTextBox.Text.Length > 0 && dateOfExecution.SelectedDate != null && timeOfExecution.SelectedItem != null && tariffComboBox.SelectedItem != null)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            MySqlCommand cmd = new MySqlCommand($@"Insert into connection_claim(id_claim, connection_address, mount_date, employees_id, client_id, claim_status_id, connection_creationDate, tariff_id)
                                                               value (
                                                                {claimNumber.Content},
                                                                '{mountAddressTextBox.Text}',
                                                                '{((DateTime)dateOfExecution.SelectedDate).ToString("yyyy-MM-dd")} {timeOfExecution.SelectedItem}',
                                                                {AccountHolder.userId},
                                                                {ClientHolder.data[0]},
                                                                (Select `idclaim_status` from claim_status where `status` = '{claimStatusComboBox.SelectedItem}'),
                                                                '{DateTime.Parse(creationDate.Content.ToString()).ToString("yyyy-MM-dd")}',
                                                                {tariffs.Where(pair => pair.Value == tariffComboBox.SelectedItem.ToString()).Select(pair => pair.Key).Single()}
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
            string[] timePeriodArr = new string[] { "9:00", "9:30", "10:00", "10:30", "11:00", "11:30", "13:00", "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30", "17:00" };
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(@"Select `id_claim`, `connection_creationDate`, `mount_date`, `connection_address`, tariff.`tariff_name` as 'tariff', client.full_name as 'client_fio', employees.full_name as 'employee_fio', claim_status.status as 'claim_status'
                                                        from `connection_claim`
                                                        inner join `client` on client.idclient = connection_claim.client_id
                                                        inner join `employees` on employees.idemployees = connection_claim.employees_id
                                                        inner join `tariff` on tariff.idtariff = connection_claim.tariff_id
                                                        inner join `claim_status` on `claim_status`.idclaim_status = connection_claim.claim_status_id;", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    cmd.ExecuteNonQuery();
                    DataTable dt =  new DataTable();
                    da.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        string fio = row.ItemArray[5].ToString();
                        row.SetField<string>(5, HideClientName(fio));
                    }

                    claimsDG.ItemsSource = dt.AsDataView();
                    timeOfExecution.ItemsSource = timePeriodArr;
                }
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
            mountAddressTextBox.Clear();
            dateOfExecution.SelectedDate = null;
            tariffComboBox.SelectedItem = null;
            clientTextBox.Clear();
            ClientHolder.data = null;
            timeOfExecution.SelectedItem = null;
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

        private string HideClientName(string fullName)
        {
            string[] clientFio = fullName.Split(' ');
            return $"{clientFio[1]} {clientFio[2]} {clientFio[0][0]}.";
        }

        private string HideClientPhoneNumber(string phoneNumber)
        {
            char[] phoneNumberByLetters = phoneNumber.ToCharArray().Where(c => c != ' ').ToArray();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < phoneNumberByLetters.Length; i++)
            {
                if(i == 2 || i == 7)
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
                var win = new ClaimVerbose(fieldValuesOfARecord, false);
                win.ShowDialog();
            }
        }

        private void claimsDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            showClaimButton.IsEnabled = true;
        }
    }
}