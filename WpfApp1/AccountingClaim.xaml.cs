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
using System.Windows.Threading;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Window17.xaml
    /// </summary>
    public partial class AccountingClaim : Window
    {
        private string additionalFilterParams = "";
        private string additionalDateFilterParams = "";
        private string additionalSortParams = "";
        private string additionalSearchParams = "";
        DispatcherTimer timerRef;
        public AccountingClaim()
        {
            InitializeComponent();
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(300);
            timer.Tick += Timer_Tick;
            timer.Start();

            timerRef = timer;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            RefreshDatagrid();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            timerRef.Stop();
            timerRef.Tick -= Timer_Tick;
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (claimsDG.SelectedItem != null)
            {
                DataRowView drv = claimsDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;
                this.Hide();
                var win = new ClaimVerbose(fieldValuesOfARecord, true, RefreshDatagrid);
                win.ShowDialog();
                this.ShowDialog();
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new Order();
            win.ShowDialog();
            this.ShowDialog();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDatagrid();
            allStatuses.IsChecked = true;
            fromDate.DisplayDateStart = DateTime.Today.AddYears(-10);
            fromDate.DisplayDateEnd = DateTime.Today.AddDays(-1);
            toDate.DisplayDateEnd = DateTime.Today;

            switch (AccountHolder.UserRole)
            {
                case "Менеджер":
                    printClaimButton.Visibility = Visibility.Collapsed;
                    orderButton.Visibility = Visibility.Collapsed;
                    break;
                case "Мастер":
                    printClaimButton.Visibility = Visibility.Collapsed;
                    break;
                case "Директор":
                    orderButton.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void RefreshDatagrid()
        {
            try
            {
                string cmdUpdateExpired = "";
                string filterParams = "";
                if (additionalFilterParams != string.Empty || additionalSearchParams != string.Empty || additionalDateFilterParams != string.Empty)
                {
                    string betweenExpressions1 = additionalDateFilterParams != string.Empty && additionalFilterParams != string.Empty ? " And " : "";
                    string betweenExpressions2 = (additionalDateFilterParams != string.Empty || additionalFilterParams != string.Empty) && additionalSearchParams != string.Empty ? " And " : "";
                    filterParams = $" where {additionalDateFilterParams}{betweenExpressions1}{additionalFilterParams}{betweenExpressions2}{additionalSearchParams}";
                }
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"Select `id_claim`, `connection_creationDate`, `mount_date`, `connection_address`, tariff.`tariff_name` as 'tariff', client.full_name as 'client_fio', employees.full_name as 'employee_fio', claim_status.status as 'claim_status', (Select full_name from employees where idemployees = connection_claim.master_id) as 'master_fio'
                                                    from `connection_claim`
                                                    inner join `client` on client.idclient = connection_claim.client_id
                                                    inner join `employees` on employees.idemployees = connection_claim.employees_id
                                                    inner join `tariff` on tariff.idtariff = connection_claim.tariff_id
                                                    inner join `claim_status` on `claim_status`.idclaim_status = connection_claim.claim_status_id {filterParams}{additionalSortParams};", conn);
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
                            if (executionDate < DateTime.Now && record[7].ToString() == "Входящая")
                                record[record.Length - 1] = true;
                            else if (executionDate < DateTime.Today.AddDays(1) && record[7].ToString() == "В работе")
                            {
                                //record[record.Length - 1] = true;
                                record[7] = "Отменена";
                                cmdUpdateExpired += $"Update `connection_claim` set claim_status_id = (select idclaim_status from claim_status where `status` = 'Отменена') where id_claim = {record[0]};";
                            }
                            else
                                record[record.Length - 1] = false;
                            dt.LoadDataRow(record, true);
                        }
                    }

                    if (cmdUpdateExpired != string.Empty)
                    {
                        MySqlCommand cmd2 = new MySqlCommand(cmdUpdateExpired, conn);
                        cmd2.ExecuteNonQuery();
                    }

                    foreach (DataRow row in dt.Rows)
                    {
                        string fio = row.ItemArray[5].ToString();
                        row.SetField<string>(5, HideName(fio));
                    }

                    claimsDG.ItemsSource = dt.AsDataView();
                }
                ShowRecordsCount();
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterByStatus_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            switch (rb.Name)
            {
                case "allStatuses":
                    additionalFilterParams = "";
                    break;
                case "incoming":
                    additionalFilterParams = "`status` = 'Входящая'";
                    break;
                case "inProgress":
                    additionalFilterParams = "`status` = 'В работе'";
                    break;
                case "closed":
                    additionalFilterParams = "`status` = 'Закрыта'";
                    break;
                case "canceled":
                    additionalFilterParams = "`status` = 'Отменена'";
                    break;
            }
            RefreshDatagrid();
        }

        private void searchByContractNumAndFio_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchPrompt = searchByContractNumAndFio.Text;
            long searchTry;

            if (searchPrompt.Length > 3 || (long.TryParse(searchPrompt, out searchTry) && searchPrompt.Length > 0))
                additionalSearchParams = $@" ((Select full_name from `client`
                                                where idclient = connection_claim.client_id) 
                                                LIKE '%{searchPrompt.Trim()}%' OR `id_claim` = '{searchPrompt}')";
            else
                additionalSearchParams = "";
            RefreshDatagrid();
        }

        private void dates_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fromDate.SelectedDate.HasValue && toDate.SelectedDate.HasValue)
            {
                additionalDateFilterParams = $"connection_creationDate between '{fromDate.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss")}' and '{toDate.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }
            else if (fromDate.SelectedDate.HasValue && !toDate.SelectedDate.HasValue)
            {
                additionalDateFilterParams = $"connection_creationDate between '{fromDate.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss")}' and '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }
            else if (!fromDate.SelectedDate.HasValue && toDate.SelectedDate.HasValue)
            {
                additionalDateFilterParams = $"connection_creationDate between '{DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss")}' and '{toDate.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }
            else
            {
                additionalDateFilterParams = "";
            }

            fromDate.DisplayDateEnd = toDate.SelectedDate == null || toDate?.SelectedDate.Value > DateTime.Now ? DateTime.Now : toDate.SelectedDate.Value.AddDays(-1);
            toDate.DisplayDateStart = fromDate.SelectedDate == null ? fromDate.DisplayDateStart : fromDate.SelectedDate.Value.AddDays(1);
            RefreshDatagrid();
        }

        private void Dates_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[\b\s]");
            if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                e.Handled = false;
            else
                e.Handled = true;
        }

        private void searchByContractNumAndFio_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[0-9A-Za-zА-Яа-я-\b\s]");
            if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                e.Handled = false;
            else
                e.Handled = true;
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            fromDate.Text = "";
            toDate.Text = "";
            allStatuses.IsChecked = true;
            searchByContractNumAndFio.Text = "";
        }

        private void claimsDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            showClaimButton.IsEnabled = true;
        }

        private void ShowRecordsCount()
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand($@"Select Count(*) from `connection_claim`;", conn);
                int recordsCount = Convert.ToInt32(cmd.ExecuteScalar());
                recordsCountLabel.Content = recordsCount.ToString();
            }
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
    }
}
