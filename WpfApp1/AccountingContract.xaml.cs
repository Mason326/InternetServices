using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Window21.xaml
    /// </summary>
    public partial class AccountingContract : Window
    {
        private string additionalFilterParams = "";
        private string additionalDateFilterParams = "";
        private string additionalSortParams = "";
        private string additionalSearchParams = "";
        public AccountingContract()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (contractsDG.SelectedItem != null)
            {
                DataRowView selectedContractView = contractsDG.SelectedItem as DataRowView;
                object[] selectedContractoItemsArray = selectedContractView.Row.ItemArray;
                this.Hide();
                var form = new ContractVerbose(selectedContractoItemsArray, RefreshDataGrid);
                form.ShowDialog();
                this.ShowDialog();
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDataGrid();
            noSort.IsChecked = true;
            allContracts.IsChecked = true;
            fromDate.DisplayDateStart = DateTime.Today.AddYears(-10);
            fromDate.DisplayDateEnd = DateTime.Today.AddDays(-1);
            toDate.DisplayDateEnd = DateTime.Today;
            showContractVerbose.IsEnabled = false;

        }

        private void FilterByStatus_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            switch (rb.Name)
            {
                case "allContracts":
                    additionalFilterParams = "";
                    break;
                case "currentContracts":
                    additionalFilterParams = "`status` = 'Заключен'";
                    break;
                case "terminatedContracts":
                    additionalFilterParams = "`status` = 'Расторгнут'";
                    break;
            }
            RefreshDataGrid();
        }

        private void SortByContractNumber_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            switch (rb.Name)
            {
                case "noSort":
                    additionalSortParams = "";
                    break;
                case "asc":
                    additionalSortParams = " order by `idcontract`";
                    break;
                case "desc":
                    additionalSortParams = " order by `idcontract` desc";
                    break;
            }
            RefreshDataGrid();
        }

        private async void RefreshDataGrid()
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
                    MySqlCommand cmd = new MySqlCommand($@"SELECT idcontract, contract_date, (Select full_name from `client`
                                                        where idclient = connection_claim.client_id) as 'client',
                                                        connection_claim_id, order_id, `contract_status`.`status` as 'status',
                                                        (Select `tariff_name` FROM `tariff` Where idtariff = `connection_claim`.tariff_id) as 'tariff',
                                                        `connection_claim`.connection_creationDate as 'claimDate',
                                                        `connection_claim`.connection_address as 'connection_address'
                                                        FROM contract
                                                        inner join `connection_claim` on contract.connection_claim_id = connection_claim.id_claim
                                                        inner join contract_status on contract_status.idcontract_status = contract.contract_status_id
                                                        {filterParams}{additionalSortParams};", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    await cmd.ExecuteNonQueryAsync();
                    da.Fill(dt);
                    contractsDG.ItemsSource = dt.AsDataView();
                    ShowRecordsCount();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void searchByContractNumAndFio_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchPrompt = searchByContractNumAndFio.Text;
            long searchTry;
           
            if (searchPrompt.Length > 3 || (long.TryParse(searchPrompt, out searchTry) && searchPrompt.Length > 0))
                additionalSearchParams = $@" ((Select full_name from `client`
                                                where idclient = connection_claim.client_id) 
                                                LIKE '%{searchPrompt.Trim()}%' OR `idcontract` = '{searchPrompt}')";
            else
                additionalSearchParams = "";
            RefreshDataGrid();
        }


        private void dates_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fromDate.SelectedDate.HasValue && toDate.SelectedDate.HasValue)
            {
                additionalDateFilterParams = $"`contract_date` between '{fromDate.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss")}' and '{toDate.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }
            else if (fromDate.SelectedDate.HasValue && !toDate.SelectedDate.HasValue)
            {
                additionalDateFilterParams = $"`contract_date` between '{fromDate.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss")}' and '{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }
            else if (!fromDate.SelectedDate.HasValue && toDate.SelectedDate.HasValue)
            {
                additionalDateFilterParams = $"`contract_date` between '{DateTime.MinValue.ToString("yyyy-MM-dd HH:mm:ss")}' and '{toDate.SelectedDate.Value.ToString("yyyy-MM-dd HH:mm:ss")}'";
            }
            else
            {
                additionalDateFilterParams = "";
            }

            fromDate.DisplayDateEnd = toDate.SelectedDate == null || toDate?.SelectedDate.Value > DateTime.Now ? DateTime.Now : toDate.SelectedDate.Value.AddDays(-1);
            toDate.DisplayDateStart = fromDate.SelectedDate == null ? fromDate.DisplayDateStart : fromDate.SelectedDate.Value.AddDays(1);
            RefreshDataGrid();
        }

        private void Dates_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[\b\s]");
            if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                e.Handled = false;
            else
                e.Handled = true;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            fromDate.Text = "";
            toDate.Text = "";
            noSort.IsChecked = true;
            allContracts.IsChecked = true;
            searchByContractNumAndFio.Text = "";
        }

        private void searchByContractNumAndFio_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[0-9A-Za-zА-Яа-я-\b\s]");
            if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                e.Handled = false;
            else
                e.Handled = true;

        }

        private void contractsDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            showContractVerbose.IsEnabled = true;
        }

        private void ShowRecordsCount()
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand($@"Select Count(*) from `contract`;", conn);
                int recordsCount = Convert.ToInt32(cmd.ExecuteScalar());
                recordsCountLabel.Content = recordsCount.ToString();
            }
        }
    }
}
