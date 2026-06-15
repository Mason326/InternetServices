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
    /// Форма "Учет договоров" - окно для просмотра и управления договорами на подключение
    /// </summary>
    public partial class AccountingContract : Window
    {
        // Дополнительные условия для SQL-запроса (фильтрация по статусу договора)
        private string additionalFilterParams = "";
        // Дополнительные условия для фильтрации по дате договора
        private string additionalDateFilterParams = "";
        // Дополнительные условия для сортировки (по номеру договора)
        private string additionalSortParams = "";
        // Дополнительные условия для поиска (по номеру договора или ФИО клиента)
        private string additionalSearchParams = "";
        // Хранилище всех строк данных (для пагинации)
        private List<DataRow> _allRows = new List<DataRow>();
        // Текущая страница пагинации
        private int _currentPage = 1;
        // Количество записей на одной странице
        private int _pageSize = 3;
        
        /// <summary>
        /// Конструктор формы - инициализация компонентов
        /// </summary>
        public AccountingContract()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка "Просмотр договора" - открывает детальную информацию о выбранном договоре
        /// </summary>
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

        /// <summary>
        /// Кнопка "На главную" - закрытие текущей формы
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Событие загрузки формы - настройка UI, отображение роли пользователя,
        /// установка ограничений на даты, загрузка данных
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Отображение роли и сокращенного ФИО в заголовке окна
                this.Title += $" ({AccountHolder.UserRole}: {FullNameSplitter.MakeShortName(AccountHolder.FIO)})";
            }
            catch
            {
                ;
            }
            
            RefreshDataGrid();
            
            // Настройка начальных состояний элементов управления
            noSort.IsChecked = true;           // Сортировка по умолчанию отключена
            allContracts.IsChecked = true;      // Показаны все договоры
            fromDate.DisplayDateStart = DateTime.Today.AddYears(-10);  // От -10 лет от сегодня
            fromDate.DisplayDateEnd = DateTime.Today.AddDays(-1);      // До вчерашнего дня
            toDate.DisplayDateEnd = DateTime.Today;                    // До сегодня
            showContractVerbose.IsEnabled = false;  // Кнопка просмотра неактивна до выбора договора
            printAReportButton.Visibility = Visibility.Collapsed;
            
            // Только директор может печатать отчеты
            if (AccountHolder.UserRole == "Директор")
            {
                printAReportButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Фильтрация договоров по статусу (RadioButton)
        /// </summary>
        private void FilterByStatus_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            switch (rb.Name)
            {
                case "allContracts":
                    additionalFilterParams = "";                    // Все договоры
                    break;
                case "currentContracts":
                    additionalFilterParams = "`status` = 'Заключен'"; // Только действующие
                    break;
                case "terminatedContracts":
                    additionalFilterParams = "`status` = 'Расторгнут'"; // Расторгнутые
                    break;
            }
            RefreshDataGrid();
        }

        /// <summary>
        /// Сортировка по номеру договора (возрастание/убывание/без сортировки)
        /// </summary>
        private void SortByContractNumber_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            switch (rb.Name)
            {
                case "noSort":
                    additionalSortParams = "";                      // Без сортировки
                    break;
                case "asc":
                    additionalSortParams = " order by `idcontract`"; // По возрастанию
                    break;
                case "desc":
                    additionalSortParams = " order by `idcontract` desc"; // По убыванию
                    break;
            }
            RefreshDataGrid();
        }

        /// <summary>
        /// Основной метод обновления DataGrid - загрузка данных о договорах из БД
        /// Асинхронный для предотвращения блокировки UI
        /// </summary>
        private async void RefreshDataGrid()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    string filterParams = "";
                    
                    // Формирование WHERE-условия из активных фильтров
                    if (additionalFilterParams != string.Empty || additionalSearchParams != string.Empty || additionalDateFilterParams != string.Empty)
                    {
                        string betweenExpressions1 = additionalDateFilterParams != string.Empty && additionalFilterParams != string.Empty ? " And " : "";
                        string betweenExpressions2 = (additionalDateFilterParams != string.Empty || additionalFilterParams != string.Empty) && additionalSearchParams != string.Empty ? " And " : "";
                        filterParams = $" where {additionalDateFilterParams}{betweenExpressions1}{additionalFilterParams}{betweenExpressions2}{additionalSearchParams}";
                    }

                    // SQL-запрос для получения данных о договорах с информацией о связанной заявке
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

                    // Сохранение всех строк для пагинации
                    _allRows.Clear();
                    foreach (DataRow row in dt.Rows)
                    {
                        _allRows.Add(row);
                    }

                    // Отображение количества записей
                    ShowRecordsCount(cmd.CommandText);

                    // Обновление элементов пагинации
                    UpdatePagination();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обновление элементов управления пагинации (кнопки "Вперед/Назад", номер страницы)
        /// </summary>
        private void UpdatePagination()
        {
            int totalPages = (int)Math.Ceiling((double)_allRows.Count / _pageSize);
            lblTotalPages.Text = totalPages.ToString();

            // Корректировка текущей страницы, если она выходит за допустимые границы
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
        /// Отображение данных текущей страницы в DataGrid
        /// </summary>
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
                // Копирование структуры таблицы
                foreach (DataColumn col in _allRows[0].Table.Columns)
                {
                    pageTable.Columns.Add(col.ColumnName, col.DataType);
                }

                // Добавление строк только для текущей страницы
                for (int i = startIndex; i < endIndex; i++)
                {
                    pageTable.ImportRow(_allRows[i]);
                }
            }

            contractsDG.ItemsSource = pageTable.AsDataView();
        }

        /// <summary>
        /// Обработчик кнопки "Предыдущая страница"
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
        /// Обработчик кнопки "Следующая страница"
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
        /// Обработчик ручного ввода номера страницы в текстовое поле
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
        /// Валидация ввода - разрешены только цифры для поля номера страницы
        /// </summary>
        private void OnlyNumbers_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Поиск по номеру договора или ФИО клиента
        /// Активируется при длине запроса более 3 символов или при вводе цифр
        /// </summary>
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

        /// <summary>
        /// Фильтрация договоров по диапазону дат заключения
        /// </summary>
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

            // Ограничение выбора дат (начало не может быть позже конца и наоборот)
            fromDate.DisplayDateEnd = toDate.SelectedDate == null || toDate?.SelectedDate.Value > DateTime.Now ? DateTime.Now : toDate.SelectedDate.Value.AddDays(-1);
            toDate.DisplayDateStart = fromDate.SelectedDate == null ? fromDate.DisplayDateStart : fromDate.SelectedDate.Value.AddDays(1);
            RefreshDataGrid();
        }

        /// <summary>
        /// Валидация ввода дат - разрешены только управляющие символы (Backspace, пробел)
        /// </summary>
        private void Dates_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[\b\s]");
            if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                e.Handled = false;
            else
                e.Handled = true;
        }

        /// <summary>
        /// Кнопка сброса всех фильтров и настроек
        /// </summary>
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

        /// <summary>
        /// Валидация ввода поискового запроса - разрешены буквы, цифры, дефис, пробел, Backspace
        /// </summary>
        private void searchByContractNumAndFio_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[0-9A-Za-zА-Яа-я-\b\s]");
            if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                e.Handled = false;
            else
                e.Handled = true;
        }

        /// <summary>
        /// Активация кнопки просмотра договора при выборе строки в DataGrid
        /// </summary>
        private void contractsDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            showContractVerbose.IsEnabled = true;
        }

        /// <summary>
        /// Отображение общего количества записей, соответствующих текущим фильтрам
        /// </summary>
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

        /// <summary>
        /// Кнопка печати отчета - формирование и экспорт отчета о договорах в Excel
        /// Доступна только для роли "Директор"
        /// </summary>
        private void printAReportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (contractsDG.Items.Count < 1)
                {
                    MessageBox.Show($"В отчете отсутствуют записи", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                
                // Создание Excel-приложения и книги
                var application = new Excel.Application();
                var workbook = application.Workbooks.Add();
                var worksheet = workbook.Worksheets[1] as Excel.Worksheet;

                int rowCount = contractsDG.Items.Count;
                int colCount = contractsDG.Columns.Count - 1;  // Исключаем колонку "Описание"

                var data = new List<object[]>();
                var cols = new List<object>();
                
                // Сбор заголовков колонок (исключая колонку "Описание")
                foreach (var col in contractsDG.Columns)
                {
                    if(col.Header != null && col.Header.ToString() != "Описание")
                        cols.Add(col.Header);
                }
                data.Add(cols.ToArray());
                
                // Загрузка актуальных данных из БД для отчета
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
                        
                        // SQL-запрос для получения данных об отфильтрованных договорах
                        MySqlCommand cmd = new MySqlCommand($@"SELECT idcontract, contract_date, (Select full_name from `client`
                                                        where idclient = connection_claim.client_id) as 'client',
                                                        connection_claim_id, `contract_status`.`status` as 'status',
                                                        (Select `tariff_name` FROM `tariff` Where idtariff = `connection_claim`.tariff_id) as 'tariff',
                                                        `connection_claim`.connection_creationDate as 'claimDate',
                                                        `connection_claim`.connection_address as 'connection_address'
                                                        FROM contract
                                                        inner join `connection_claim` on contract.connection_claim_id = connection_claim.id_claim
                                                        inner join contract_status on contract_status.idcontract_status = contract.contract_status_id {filterParams}{additionalSortParams};", conn);
                        cmd.ExecuteNonQuery();
                        
                        using (MySqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                // Формирование строки данных с форматированием дат
                                data.Add(new object[] {
                                    dr["idcontract"],
                                    dr["connection_claim_id"],
                                    dr["client"],
                                    dr["tariff"],
                                    dr["connection_address"],
                                    DateTime.Parse(dr["claimDate"].ToString()).ToString("dd.MM.yyyy"),
                                    DateTime.Parse(dr["contract_date"].ToString()).ToString("dd.MM.yyyy"),
                                    dr["status"]
                                });
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось загрузить данные для отчета\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // Определение диапазона для записи данных
                Excel.Range startCell = worksheet.Range["A1"];
                Excel.Range endCell = worksheet.Cells[rowCount + 2, colCount];

                Excel.Range writeRange = worksheet.Range[startCell, endCell];
                object[,] dataArray = new object[rowCount + 2, colCount];

                // Заполнение массива данными
                for (int i = 0; i <= rowCount + 1; i++)
                {
                    for (int j = 0; j < colCount; j++)
                    {
                        dataArray[i, j] = data[i][j];
                    }
                }

                writeRange.Value2 = dataArray;
                writeRange.Columns.AutoFit();

                // Цветовое выделение строк в зависимости от статуса договора
                for (int i = 2; i <= rowCount + 2; i++)
                {
                    Excel.Range cell = writeRange.Cells[colCount][i];
                    cell.Font.Bold = true;
                    cell.Font.Color = Excel.XlRgbColor.rgbWhite;
                    if (cell.Text == "Заключен")
                        cell.Interior.Color = Excel.XlRgbColor.rgbDarkGreen;  // Действующий - зеленый
                    else
                        cell.Interior.Color = Excel.XlRgbColor.rgbDarkRed;    // Расторгнутый - красный
                }

                // Создание форматированной таблицы Excel
                Excel.ListObject table = worksheet.ListObjects.Add(
                    Excel.XlListObjectSourceType.xlSrcRange,
                    worksheet.Range[startCell, endCell],
                    Type.Missing,
                    Excel.XlYesNoGuess.xlYes,
                    Type.Missing);
                table.Name = "Contracts";

                // Добавление информации о количестве договоров
                Excel.Range recordCount = worksheet.Cells[1][rowCount + 3];
                recordCount.Value = $"Количество договоров: {recordsCountLabel.Content}";
                recordCount.Font.Bold = true;
                recordCount.Font.Size = 16;

                // Добавление информации о периоде отчета
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
                
                // Отображение Excel с отчетом
                application.Visible = true;
            }
            catch(Exception exc)
            {
                MessageBox.Show($"Не удалось распечатать акт\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Адаптация интерфейса при изменении размера окна
        /// Изменяется размер шрифта кнопок (количество записей на странице фиксировано - 3)
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double windowHeight = e.NewSize.Height;
            double baseFontSize = 14;
            int newPageSize = _pageSize;

            // Изменение размера шрифта в зависимости от высоты окна
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

            // Если размер страницы изменился, обновляем пагинацию
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

        /// <summary>
        /// Обновление размера шрифта для кнопок управления
        /// </summary>
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