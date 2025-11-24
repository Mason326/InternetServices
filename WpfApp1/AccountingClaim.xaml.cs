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
using Excel = Microsoft.Office.Interop.Excel;
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
        private int masterId = -1;
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
            switch (AccountHolder.UserRole)
            {
                case "Менеджер":
                    printAReport.Visibility = Visibility.Collapsed;
                    orderButton.Visibility = Visibility.Collapsed;
                    incomesLabel.Visibility = Visibility.Collapsed;
                    reportVariantsComboBox.Visibility = Visibility.Collapsed;
                    break;
                case "Мастер":
                    printAReport.Visibility = Visibility.Collapsed;
                    incomesLabel.Visibility = Visibility.Collapsed;
                    reportVariantsComboBox.Visibility = Visibility.Collapsed;
                    masterId = AccountHolder.userId;
                    break;
                case "Директор":
                    printAReport.Visibility = Visibility.Visible;
                    reportVariantsComboBox.Visibility = Visibility.Visible;
                    orderButton.Visibility = Visibility.Collapsed;
                    incomesLabel.Visibility = Visibility.Visible;
                    break;
            }
            RefreshDatagrid();
            printAReport.IsEnabled = false;
            allStatuses.IsChecked = true;
            fromDate.DisplayDateStart = DateTime.Today.AddYears(-10);
            fromDate.DisplayDateEnd = DateTime.Today.AddDays(-1);
            toDate.DisplayDateEnd = DateTime.Today;
            reportVariantsComboBox.ItemsSource = new string[] { "Рейтинг менеджеров", "Рейтинг мастеров", "Учет заявок" };
        }

        private void RefreshDatagrid()
        {
            try
            {
                string cmdUpdateExpired = "";
                string filterParams = "";
                if (masterId != -1 && (additionalFilterParams != string.Empty || additionalSearchParams != string.Empty || additionalDateFilterParams != string.Empty))
                {
                    string betweenExpressions1 = additionalDateFilterParams != string.Empty && additionalFilterParams != string.Empty ? " And " : "";
                    string betweenExpressions2 = (additionalDateFilterParams != string.Empty || additionalFilterParams != string.Empty) && additionalSearchParams != string.Empty ? " And " : "";
                    filterParams = $" and {additionalDateFilterParams}{betweenExpressions1}{additionalFilterParams}{betweenExpressions2}{additionalSearchParams}";
                }
                else if (additionalFilterParams != string.Empty || additionalSearchParams != string.Empty || additionalDateFilterParams != string.Empty)
                {
                    string betweenExpressions1 = additionalDateFilterParams != string.Empty && additionalFilterParams != string.Empty ? " And " : "";
                    string betweenExpressions2 = (additionalDateFilterParams != string.Empty || additionalFilterParams != string.Empty) && additionalSearchParams != string.Empty ? " And " : "";
                    filterParams = $" where {additionalDateFilterParams}{betweenExpressions1}{additionalFilterParams}{betweenExpressions2}{additionalSearchParams}";
                }
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    string cmdText = $@"Select `id_claim`, `connection_creationDate`, `mount_date`, `connection_address`, tariff.`tariff_name` as 'tariff', client.full_name as 'client_fio', employees.full_name as 'employee_fio', claim_status.status as 'claim_status', (Select full_name from employees where idemployees = connection_claim.master_id) as 'master_fio', `order`.totalCost as claim_cost
                                                    from `connection_claim`
                                                    inner join `client` on client.idclient = connection_claim.client_id
                                                    inner join `employees` on employees.idemployees = connection_claim.employees_id
                                                    inner join `tariff` on tariff.idtariff = connection_claim.tariff_id
                                                    left join `order` on `order`.idorder = connection_claim.order_id
                                                    inner join `claim_status` on `claim_status`.idclaim_status = connection_claim.claim_status_id {filterParams}{additionalSortParams};";
                    if (masterId != -1)
                        cmdText = $@"Select `id_claim`, `connection_creationDate`, `mount_date`, `connection_address`, tariff.`tariff_name` as 'tariff', client.full_name as 'client_fio', employees.full_name as 'employee_fio', claim_status.status as 'claim_status', (Select full_name from employees where idemployees = connection_claim.master_id) as 'master_fio', `order`.totalCost as claim_cost
                                                    from `connection_claim`
                                                    inner join `client` on client.idclient = connection_claim.client_id
                                                    inner join `employees` on employees.idemployees = connection_claim.employees_id
                                                    inner join `tariff` on tariff.idtariff = connection_claim.tariff_id
                                                    left join `order` on `order`.idorder = connection_claim.order_id
                                                    inner join `claim_status` on `claim_status`.idclaim_status = connection_claim.claim_status_id where master_id = {masterId}{filterParams}{additionalSortParams};";
                    MySqlCommand cmd = new MySqlCommand(cmdText, conn);
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
                    ShowRecordsCount(cmdText);
                    if (AccountHolder.UserRole == "Директор")
                        ShowTotalSum(cmdText);
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
            reportVariantsComboBox.SelectedItem = null;
        }

        private void claimsDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            showClaimButton.IsEnabled = true;
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

        private void ShowTotalSum(string strCmd)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"Select sum(`claim_cost`) from ({strCmd.Replace(";", "")}) as counter_table;", conn);
                    int recordsCount = Convert.ToInt32(cmd.ExecuteScalar());
                    totalSumLabel.Content = recordsCount.ToString();
                }
                catch
                {
                    totalSumLabel.Content = "0";
                }
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

        private void printClaimButton_Click(object sender, RoutedEventArgs e)
        {
            switch (reportVariantsComboBox.SelectedItem.ToString())
            {
                case "Рейтинг менеджеров":
                    PrintTopEmployees(true);
                    break;
                case "Рейтинг мастеров":
                    PrintTopEmployees(false);
                    break;
                case "Учет заявок":
                    PrintClaims();
                    break;
            }

           
        }

        private void chooseAnEmployee_Click(object sender, RoutedEventArgs e)
        {
            var win = new EmployeesViewWindow();
            win.Show();
        }

        private void PrintClaims()
        {
            try
            {
                if (claimsDG.Items.Count < 1)
                {
                    MessageBox.Show($"В отчете отсутствуют записи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                var application = new Excel.Application();
                var workbook = application.Workbooks.Add();
                var worksheet = workbook.Worksheets[1] as Excel.Worksheet;

                int rowCount = claimsDG.Items.Count;
                int colCount = claimsDG.Columns.Count;

                var data = new List<object[]>();
                var cols = new List<object>();
                foreach (var col in claimsDG.Columns)
                    cols.Add(col.Header);
                data.Add(cols.ToArray());
                string filterParams = "";
                if (masterId != -1 && (additionalFilterParams != string.Empty || additionalSearchParams != string.Empty || additionalDateFilterParams != string.Empty))
                {
                    string betweenExpressions1 = additionalDateFilterParams != string.Empty && additionalFilterParams != string.Empty ? " And " : "";
                    string betweenExpressions2 = (additionalDateFilterParams != string.Empty || additionalFilterParams != string.Empty) && additionalSearchParams != string.Empty ? " And " : "";
                    filterParams = $" and {additionalDateFilterParams}{betweenExpressions1}{additionalFilterParams}{betweenExpressions2}{additionalSearchParams}";
                }
                else if (additionalFilterParams != string.Empty || additionalSearchParams != string.Empty || additionalDateFilterParams != string.Empty)
                {
                    string betweenExpressions1 = additionalDateFilterParams != string.Empty && additionalFilterParams != string.Empty ? " And " : "";
                    string betweenExpressions2 = (additionalDateFilterParams != string.Empty || additionalFilterParams != string.Empty) && additionalSearchParams != string.Empty ? " And " : "";
                    filterParams = $" where {additionalDateFilterParams}{betweenExpressions1}{additionalFilterParams}{betweenExpressions2}{additionalSearchParams}";
                }
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    string cmdText = $@"Select `id_claim`, `connection_creationDate`, `mount_date`, `connection_address`, tariff.`tariff_name` as 'tariff', client.full_name as 'client_fio', employees.full_name as 'employee_fio', claim_status.status as 'claim_status', (Select full_name from employees where idemployees = connection_claim.master_id) as 'master_fio', `order`.totalCost as claim_cost
                                                    from `connection_claim`
                                                    inner join `client` on client.idclient = connection_claim.client_id
                                                    inner join `employees` on employees.idemployees = connection_claim.employees_id
                                                    inner join `tariff` on tariff.idtariff = connection_claim.tariff_id
                                                    left join `order` on `order`.idorder = connection_claim.order_id
                                                    inner join `claim_status` on `claim_status`.idclaim_status = connection_claim.claim_status_id {filterParams}{additionalSortParams};";
                    MySqlCommand cmd = new MySqlCommand(cmdText, conn);
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        object[] record = new object[dr.FieldCount + 1];
                        while (dr.Read())
                        {
                            dr.GetValues(record);
                            record[1] = ((DateTime)record[1]).ToString("dd.MM.yyyy");
                            record[2] = ((DateTime)record[2]).ToString("dd.MM.yyyy");
                            object[] valuesRightOrder = new object[] { record[0], record[1], record[2], record[3], record[4], record[5], record[6], record[8], record[9], record[7], record[10] };
                            data.Add(valuesRightOrder);
                            record = new object[dr.FieldCount + 1];
                        }

                        Excel.Range startCell = worksheet.Range["A1"];
                        Excel.Range endCell = worksheet.Cells[rowCount + 1, colCount];

                        Excel.Range writeRange = worksheet.Range[startCell, endCell];
                        object[,] dataArray = new object[rowCount + 1, colCount];

                        for (int i = 0; i < rowCount + 1; i++)
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
                            if (cell.Text == "Закрыта")
                                cell.Interior.Color = Excel.XlRgbColor.rgbDarkMagenta;
                            else if (cell.Text == "Входящая")
                                cell.Interior.Color = Excel.XlRgbColor.rgbForestGreen;
                            else if (cell.Text == "В работе")
                                cell.Interior.Color = Excel.XlRgbColor.rgbCoral;
                            else
                                cell.Interior.Color = Excel.XlRgbColor.rgbDarkRed;
                        }

                        Excel.ListObject table = worksheet.ListObjects.Add(
                            Excel.XlListObjectSourceType.xlSrcRange,
                            worksheet.Range[startCell, endCell],
                            Type.Missing,
                            Excel.XlYesNoGuess.xlYes,
                            Type.Missing);
                        table.Name = "Claims";

                        Excel.Range recordCount = worksheet.Cells[1][rowCount + 3];
                        recordCount.Value = $"Количество заявок: {recordsCountLabel.Content}";
                        recordCount.Font.Bold = true;
                        recordCount.Font.Size = 16;

                        Excel.Range incomes = worksheet.Cells[1][rowCount + 5];
                        incomes.Value = $"Общий доход от реализации заявок: {totalSumLabel.Content}";
                        incomes.Font.Bold = true;
                        incomes.Font.Size = 16;

                        if (fromDate.SelectedDate != null && toDate.SelectedDate != null)
                        {
                            Excel.Range period = worksheet.Cells[1][rowCount + 7];
                            period.Value = $"За период: {fromDate.SelectedDate.Value.ToString("dd.MM.yyyy")} - {toDate.SelectedDate.Value.ToString("dd.MM.yyyy")}";
                            period.Font.Bold = true;
                            period.Font.Size = 12;
                        }
                        else if (fromDate.SelectedDate != null && toDate.SelectedDate == null)
                        {
                            Excel.Range period = worksheet.Cells[1][rowCount + 7];
                            period.Value = $"За период {fromDate.SelectedDate.Value.ToString("dd.MM.yyyy")} - {DateTime.Now.ToString("dd.MM.yyyy")}";
                            period.Font.Bold = true;
                            period.Font.Size = 12;
                        }
                        else if (fromDate.SelectedDate == null && toDate.SelectedDate != null)
                        {
                            Excel.Range period = worksheet.Cells[1][rowCount + 7];
                            period.Value = $"За период до {toDate.SelectedDate.Value.ToString("dd.MM.yyyy")}";
                            period.Font.Bold = true;
                            period.Font.Size = 12;
                        }
                        application.Visible = true;
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось подготовить отчет к печати\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintTopEmployees(bool isManager)
        {
            try
            {
                if (claimsDG.Items.Count < 1)
                {
                    MessageBox.Show($"В отчете отсутствуют записи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string bestMaster = "";
                var application = new Excel.Application();
                var workbook = application.Workbooks.Add();
                var worksheet = workbook.Worksheets[1] as Excel.Worksheet;
                string roleName = "Мастер";
                if (isManager)
                    roleName = "Менеджер";
                var data = new List<object[]>();
                var cols = new List<object>() { "ФИО", "Количество заявок" };
                data.Add(cols.ToArray());
                string filterParams = "";
                if (additionalDateFilterParams != string.Empty)
                {
                    filterParams = $" and {additionalDateFilterParams}";
                }
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    string id = isManager ? "employees_id" : "master_id";
                    string cmdText = $@"SELECT (select full_name from employees where idemployees = {id}) as fio, Count(*) as count
                                    FROM connection_claim where claim_status_id != (Select idclaim_status from claim_status where `status` = 'Отменена')
                                    {filterParams} group by {id} order by count desc;";
                    MySqlCommand cmd = new MySqlCommand(cmdText, conn);
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        object[] record = new object[dr.FieldCount];
                        int cntr = 0;
                        while (dr.Read())
                        {
                            dr.GetValues(record);
                            if (cntr == 0)
                                bestMaster = record[0].ToString();
                            data.Add(record);
                            record = new object[dr.FieldCount];
                            cntr++;

                        }
                        int rowCount = data.Count;
                        int colCount = cols.Count;

                        Excel.Range startCell = worksheet.Range["A1"];
                        Excel.Range endCell = worksheet.Cells[rowCount, colCount];

                        Excel.Range writeRange = worksheet.Range[startCell, endCell];
                        object[,] dataArray = new object[rowCount, colCount];

                        for (int i = 0; i < rowCount; i++)
                        {
                            for (int j = 0; j < colCount; j++)
                            {
                                dataArray[i, j] = data[i][j];
                            }
                        }

                        writeRange.Value2 = dataArray;
                        writeRange.Columns.AutoFit();

                        Excel.ListObject table = worksheet.ListObjects.Add(
                            Excel.XlListObjectSourceType.xlSrcRange,
                            worksheet.Range[startCell, endCell],
                            Type.Missing,
                            Excel.XlYesNoGuess.xlYes,
                            Type.Missing);
                        table.Name = "Rating";

                        Excel.Range recordCount = worksheet.Cells[1][rowCount + 3];
                        recordCount.Value = $"Лучший {roleName}: {bestMaster}";
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
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось подготовить отчет к печати\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void reportVariantsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(reportVariantsComboBox.SelectedItem != null)
                printAReport.IsEnabled = true;
            else
                printAReport.IsEnabled = false;
        }
    }
}
