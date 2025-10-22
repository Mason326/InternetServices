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
    /// Interaction logic for Window17.xaml
    /// </summary>
    public partial class AccountingClaim : Window
    {
        private string additionalFilterParams = "";
        private string additionalDateFilterParams = "";
        private string additionalSortParams = "";
        private string additionalSearchParams = "";
        public AccountingClaim()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (claimsDG.SelectedItem != null)
            {
                DataRowView drv = claimsDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;
                this.Hide();
                var win = new ClaimVerbose(fieldValuesOfARecord, true);
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

        private async void RefreshDatagrid()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    string filterParams = "";
                    if (additionalFilterParams != string.Empty || additionalSearchParams != string.Empty || additionalDateFilterParams != string.Empty)
                    {
                        string betweenExpressions1 = additionalDateFilterParams != string.Empty && additionalFilterParams != string.Empty ? " And " : "";
                        string betweenExpressions2 = (additionalDateFilterParams != string.Empty || additionalFilterParams != string.Empty) && additionalSearchParams != string.Empty ? " And " : "";
                        filterParams = $" where {additionalDateFilterParams}{betweenExpressions1}{additionalFilterParams}{betweenExpressions2}{additionalSearchParams}";
                    }
                    MySqlCommand cmd = new MySqlCommand($@"Select `id_claim`, `connection_creationDate`, `mount_date`, `connection_address`, tariff.`tariff_name` as 'tariff', client.full_name as 'client_fio', employees.full_name as 'employee_fio', claim_status.status as 'claim_status'
                                                        from `connection_claim`
                                                        inner join `client` on client.idclient = connection_claim.client_id
                                                        inner join `employees` on employees.idemployees = connection_claim.employees_id
                                                        inner join `tariff` on tariff.idtariff = connection_claim.tariff_id
                                                        inner join `claim_status` on `claim_status`.idclaim_status = connection_claim.claim_status_id
                                                        {filterParams}{additionalSortParams};", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    await cmd.ExecuteNonQueryAsync();
                    da.Fill(dt);
                    foreach (DataRow row in dt.Rows)
                    {
                        string fio = row.ItemArray[5].ToString();
                        try
                        {
                            row.SetField<string>(5, FullNameSplitter.HideClientName(fio));
                        }
                        catch
                        {
                            ;
                        }
                    }
                    claimsDG.ItemsSource = dt.AsDataView();
                    ShowRecordsCount();
                }
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
                    additionalFilterParams = "`status` = 'Входящие'";
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
    }
}
