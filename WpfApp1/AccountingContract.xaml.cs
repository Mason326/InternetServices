using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Excel = Microsoft.Office.Interop.Excel;

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
        private List<DataRow> _allRows = new List<DataRow>();
        private int _currentPage = 1;
        private int _pageSize = 3;
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
            try
            {
                this.Title += $" ({AccountHolder.UserRole}: {FullNameSplitter.MakeShortName(AccountHolder.FIO)})";
            }
            catch
            {
                ;
            }
            RefreshDataGrid();
            noSort.IsChecked = true;
            allContracts.IsChecked = true;
            fromDate.DisplayDateStart = DateTime.Today.AddYears(-10);
            fromDate.DisplayDateEnd = DateTime.Today.AddDays(-1);
            toDate.DisplayDateEnd = DateTime.Today;
            showContractVerbose.IsEnabled = false;
            printAReportButton.Visibility = Visibility.Collapsed;
            if (AccountHolder.UserRole == "Директор")
            {
                printAReportButton.Visibility = Visibility.Visible;
            }
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
                                                    connection_claim_id, `contract_status`.`status` as 'status',
                                                    (Select `tariff_name` FROM `tariff` Where idtariff = `connection_claim`.tariff_id) as 'tariff',
                                                    `connection_claim`.connection_creationDate as 'claimDate',
                                                    `connection_claim`.connection_address as 'connection_address',
                                                    concat('№ Заявки: ', connection_claim_id, '\nКлиент: ', (Select full_name from `client`
                                                    where idclient = connection_claim.client_id), '\nТариф: ', (Select `tariff_name` FROM `tariff` Where idtariff = `connection_claim`.tariff_id), '\nАдрес: ', `connection_claim`.connection_address, '\nДата заявки: ', `connection_claim`.connection_creationDate) as contractDetails
                                                    FROM contract
                                                    inner join `connection_claim` on contract.connection_claim_id = connection_claim.id_claim
                                                    inner join contract_status on contract_status.idcontract_status = contract.contract_status_id
                                                    {filterParams}{additionalSortParams};", conn);

                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    await cmd.ExecuteNonQueryAsync();
                    da.Fill(dt);

                    _allRows.Clear();
                    foreach (DataRow row in dt.Rows)
                    {
                        _allRows.Add(row);
                    }

                    ShowRecordsCount(cmd.CommandText);

                    UpdatePagination();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

        private void DisplayCurrentPage()
        {
            if (_allRows.Count == 0)
            {
                contractsDG.ItemsSource = null;
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

            contractsDG.ItemsSource = pageTable.AsDataView();
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                UpdatePagination();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            int totalPages = (int)Math.Ceiling((double)_allRows.Count / _pageSize);
            if (_currentPage < totalPages)
            {
                _currentPage++;
                UpdatePagination();
            }
        }

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

        private void OnlyNumbers_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
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

            _currentPage = 1;
            _pageSize = 3;
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

        private void ShowRecordsCount(string strCmd)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand($@"Select Count(*) from ({strCmd.Replace(";", "")}) as counter_table;", conn);
                int recordsCount = Convert.ToInt32(cmd.ExecuteScalar());
                recordsCountLabel.Content = recordsCount.ToString();
            }
        }

        private void printAReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (contractsDG.Items.Count < 1)
                {
                    MessageBox.Show($"В отчете отсутствуют записи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var application = new Excel.Application();
                var workbook = application.Workbooks.Add();
                var worksheet = workbook.Worksheets[1] as Excel.Worksheet;

                int rowCount = contractsDG.Items.Count;
                int colCount = contractsDG.Columns.Count - 1;

                var data = new List<object[]>();
                var cols = new List<object>();
                foreach (var col in contractsDG.Columns)
                {
                    if(col.Header != null && col.Header.ToString() != "Описание")
                        cols.Add(col.Header);
                }
                data.Add(cols.ToArray());
                foreach (DataRowView row in contractsDG.Items)
                {
                    object[] values = row.Row.ItemArray;

                    values[1] = ((DateTime)values[1]).ToString("dd.MM.yyyy");
                    values[6] = ((DateTime)values[6]).ToString("dd.MM.yyyy");
                    object[] valuesRightOrder = new object[] { values[0], values[3], values[2], values[5], values[7], values[6], values[1], values[4] };
                    data.Add(valuesRightOrder);
                }
                Excel.Range startCell = worksheet.Range["A1"];
                Excel.Range endCell = worksheet.Cells[rowCount + 1, colCount];

                Excel.Range writeRange = worksheet.Range[startCell, endCell];
                object[,] dataArray = new object[rowCount + 1, colCount];

                for (int i = 0; i <= rowCount + 1; i++)
                {
                    for (int j = 0; j < colCount; j++)
                    {
                        dataArray[i, j] = data[i][j];
                    }
                }

                writeRange.Value2 = dataArray;
                writeRange.Columns.AutoFit();

                for (int i = 2; i <= rowCount + 1; i++)
                {
                    Excel.Range cell = writeRange.Cells[colCount][i];
                    cell.Font.Bold = true;
                    cell.Font.Color = Excel.XlRgbColor.rgbWhite;
                    if (cell.Text == "Заключен")
                        cell.Interior.Color = Excel.XlRgbColor.rgbDarkGreen;
                    else
                        cell.Interior.Color = Excel.XlRgbColor.rgbDarkRed;
                }

                Excel.ListObject table = worksheet.ListObjects.Add(
                    Excel.XlListObjectSourceType.xlSrcRange,
                    worksheet.Range[startCell, endCell],
                    Type.Missing,
                    Excel.XlYesNoGuess.xlYes,
                    Type.Missing);
                table.Name = "Contracts";

                Excel.Range recordCount = worksheet.Cells[1][rowCount + 3];
                recordCount.Value = $"Количество договоров: {recordsCountLabel.Content}";
                recordCount.Font.Bold = true;
                recordCount.Font.Size = 16;

                if (fromDate.SelectedDate != null && toDate.SelectedDate != null)
                {
                    Excel.Range period = worksheet.Cells[1][rowCount + 5];
                    period.Value = $"За период: {fromDate.SelectedDate.Value.ToString("dd.MM.yyyy")} - {toDate.SelectedDate.Value.ToString("dd.MM.yyyy")}";
                    period.Font.Bold = true;
                    period.Font.Size = 12;
                }
                else if (fromDate.SelectedDate != null && toDate.SelectedDate == null)
                {
                    Excel.Range period = worksheet.Cells[1][rowCount + 5];
                    period.Value = $"За период {fromDate.SelectedDate.Value.ToString("dd.MM.yyyy")} - {DateTime.Now.ToString("dd.MM.yyyy")}";
                    period.Font.Bold = true;
                    period.Font.Size = 12;
                }
                else if (fromDate.SelectedDate == null && toDate.SelectedDate != null)
                {
                    Excel.Range period = worksheet.Cells[1][rowCount + 5];
                    period.Value = $"За период до {toDate.SelectedDate.Value.ToString("dd.MM.yyyy")}";
                    period.Font.Bold = true;
                    period.Font.Size = 12;
                }
                application.Visible = true;
            }
            catch(Exception exc)
            {
                MessageBox.Show($"Не удалось распечатать акт\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double windowHeight = e.NewSize.Height;
            double baseFontSize = 14;
            int newPageSize = _pageSize;

            if (windowHeight > 800)
            {
                newPageSize = 3;
                double scale = windowHeight / 700;
                int newFontSize = (int)(baseFontSize * Math.Min(scale, 1.5));
                UpdateButtonsFontSize(newFontSize);
            }
            else if (windowHeight < 600)
            {
                newPageSize = 3;
                double scale = windowHeight / 700;
                int newFontSize = (int)Math.Max(baseFontSize * scale, 10);
                UpdateButtonsFontSize(newFontSize);
            }
            else
            {
                newPageSize = 3;
                UpdateButtonsFontSize((int)baseFontSize);
            }

            if (newPageSize != _pageSize)
            {
                _pageSize = newPageSize;

                int totalPages = (int)Math.Ceiling((double)_allRows.Count / _pageSize);

                if (_currentPage > totalPages && totalPages > 0)
                {
                    _currentPage = totalPages;
                }
                else if (_currentPage < 1)
                {
                    _currentPage = 1;
                }

                UpdatePagination();

                DisplayCurrentPage();
            }
        }

        private void UpdateButtonsFontSize(int fontSize)
        {
            var buttons = new[] { clearFilters, showContractVerbose, toMain, printAReportButton };
            foreach (var button in buttons)
            {
                if (button != null)
                    button.FontSize = fontSize;
            }
        }
    }
}
