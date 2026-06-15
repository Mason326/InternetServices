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
    /// Форма "Учет услуг" - главная форма для работы с заявками на подключение
    /// </summary>
    public partial class AccountingClaim : Window
    {
        // Дополнительные условия для SQL-запроса (фильтрация по статусу)
        private string additionalFilterParams = "";
        // Дополнительные условия для фильтрации по дате
        private string additionalDateFilterParams = "";
        // Дополнительные условия для сортировки
        private string additionalSortParams = "";
        // Дополнительные условия для поиска (по номеру заявки или ФИО клиента)
        private string additionalSearchParams = "";
        // ID текущего мастера (используется при роли "Мастер" для ограничения доступа)
        private int masterId = -1;
        // Таймер для автоматического обновления DataGrid
        DispatcherTimer timerRef;
        // Хранилище всех строк данных (для пагинации)
        private List<DataRow> _allRows = new List<DataRow>();
        // Текущая страница пагинации
        private int _currentPage = 1;
        // Количество записей на одной странице
        private int _pageSize = 3;

        /// <summary>
        /// Конструктор формы - инициализация компонентов и запуск таймера автообновления
        /// </summary>
        public AccountingClaim()
        {
            InitializeComponent();
            DispatcherTimer timer = new DispatcherTimer();
            // Интервал обновления - каждые 5 минут (300 секунд)
            timer.Interval = TimeSpan.FromSeconds(300);
            timer.Tick += Timer_Tick;
            timer.Start();
            timerRef = timer;
        }

        /// <summary>
        /// Обработчик тика таймера - вызывает обновление таблицы заявок
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            RefreshDatagrid();
        }

        /// <summary>
        /// Кнопка "На главную" - закрытие формы с остановкой таймера
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            timerRef.Stop();
            timerRef.Tick -= Timer_Tick;
            this.Close();
        }

        /// <summary>
        /// Кнопка "Просмотр заявки" - открывает детальную информацию о выбранной заявке
        /// </summary>
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

        /// <summary>
        /// Кнопка "Заказ-наряд" - формирование заказ-наряда для выбранной заявки
        /// Проверяет статус заявки перед формированием
        /// </summary>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (claimsDG.SelectedItem != null)
            {
                var drv = claimsDG.SelectedItem as DataRowView;
                object[] claimDescription = drv.Row.ItemArray;
                string currStatus = claimDescription[7].ToString();
                bool isExpired = Convert.ToBoolean(claimDescription[11]);
                string message = "";

                // Проверка статуса заявки для определения возможности создания заказ-наряда
                switch (currStatus)
                {
                    case "Входящая":
                        if (isExpired)
                            message = "Дата выполнения заявки просрочена, необходим перенос заявки на другое время";
                        else
                            message = "Заявку изначально необходимо взять в работу";
                        break;
                    case "Закрыта":
                        message = "Заявка закрыта, формирование заказ-наряда не требуется";
                        break;
                    case "Отменена":
                        message = "Заявка отменена, формирование заказ-наряда не производится";
                        break;
                }
                if (message == "")
                {
                    this.Hide();
                    var win = new Order(Convert.ToInt32(drv.Row.ItemArray[0]), RefreshDatagrid);
                    win.ShowDialog();
                    this.ShowDialog();
                }
                else
                    MessageBox.Show(message, "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Событие загрузки формы - настройка UI в зависимости от роли пользователя,
        /// установка ограничений на даты, загрузка данных
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Отображение роли и сокращенного ФИО в заголовке
                this.Title += $" ({AccountHolder.UserRole}: {FullNameSplitter.MakeShortName(AccountHolder.FIO)})";
            }
            catch
            {
                ;
            }

            // Настройка видимости элементов управления в зависимости от роли пользователя
            switch (AccountHolder.UserRole)
            {
                case "Менеджер":
                    // Менеджер не видит отчеты, диаграммы и заказ-наряды
                    printAReport.Visibility = Visibility.Collapsed;
                    orderButton.Visibility = Visibility.Collapsed;
                    diagram.Visibility = Visibility.Collapsed;
                    incomesLabel.Visibility = Visibility.Collapsed;
                    reportVariantsComboBox.Visibility = Visibility.Collapsed;
                    break;
                case "Мастер":
                    // Мастер видит только свои заявки
                    printAReport.Visibility = Visibility.Collapsed;
                    incomesLabel.Visibility = Visibility.Collapsed;
                    diagram.Visibility = Visibility.Collapsed;
                    reportVariantsComboBox.Visibility = Visibility.Collapsed;
                    masterId = AccountHolder.userId;
                    break;
                case "Директор":
                    // Директор имеет полный доступ
                    printAReport.Visibility = Visibility.Visible;
                    reportVariantsComboBox.Visibility = Visibility.Visible;
                    orderButton.Visibility = Visibility.Collapsed;
                    diagram.Visibility = Visibility.Visible;
                    incomesLabel.Visibility = Visibility.Visible;
                    break;
            }

            RefreshDatagrid();

            // Настройка ограничений для кнопок и дат
            printAReport.IsEnabled = false;
            allStatuses.IsChecked = true;
            fromDate.DisplayDateStart = DateTime.Today.AddYears(-10);  // От -10 лет от сегодня
            fromDate.DisplayDateEnd = DateTime.Today.AddDays(-1);      // До вчерашнего дня
            toDate.DisplayDateEnd = DateTime.Today;                    // До сегодня

            // Варианты отчетов для печати
            reportVariantsComboBox.ItemsSource = new string[] { "Рейтинг менеджеров", "Рейтинг мастеров", "Учет заявок" };
        }

        /// <summary>
        /// Обновление DataGrid - основной метод загрузки данных из БД
        /// Формирует SQL-запрос с учетом всех фильтров, роли пользователя
        /// </summary>
        /// <param name="initial">Флаг первичной загрузки (не используется в текущей реализации)</param>
        private void RefreshDatagrid(bool initial = true)
        {
            try
            {
                string cmdUpdateExpired = "";
                string filterParams = "";

                // Формирование WHERE-условия для SQL-запроса в зависимости от роли и активных фильтров
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
                    // Основной SQL-запрос для получения всех заявок со связанными данными
                    string cmdText = $@"Select `id_claim`, `connection_creationDate`, `mount_date`, `connection_address`, tariff.`tariff_name` as 'tariff', client.full_name as 'client_fio', employees.full_name as 'employee_fio', claim_status.status as 'claim_status', (Select full_name from employees where idemployees = connection_claim.master_id) as 'master_fio', `order`.totalCost as claim_cost, concat('Дата заявки: ', connection_creationDate, '\nДата выполнения: ', mount_date,'\nАдрес монтирования: ', connection_address, '\nТариф: ', tariff.`tariff_name`) as claimDetails
                                                from `connection_claim`
                                                inner join `client` on client.idclient = connection_claim.client_id
                                                inner join `employees` on employees.idemployees = connection_claim.employees_id
                                                inner join `tariff` on tariff.idtariff = connection_claim.tariff_id
                                                left join `order` on `order`.idorder = connection_claim.order_id
                                                inner join `claim_status` on `claim_status`.idclaim_status = connection_claim.claim_status_id {filterParams}{additionalSortParams};";

                    // Для мастера - дополнительный фильтр по master_id
                    if (masterId != -1)
                        cmdText = $@"Select `id_claim`, `connection_creationDate`, `mount_date`, `connection_address`, tariff.`tariff_name` as 'tariff', client.full_name as 'client_fio', employees.full_name as 'employee_fio', claim_status.status as 'claim_status', (Select full_name from employees where idemployees = connection_claim.master_id) as 'master_fio', `order`.totalCost as claim_cost, concat('Дата заявки: ', connection_creationDate, '\nДата выполнения: ', mount_date,'\nАдрес монтирования: ', connection_address, '\nТариф: ', tariff.`tariff_name`) as claimDetails
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
                        // Создание структуры DataTable на основе метаданных результата запроса
                        DataColumn[] columns = new DataColumn[dr.FieldCount];
                        for (int i = 0; i < columns.Length; i++)
                        {
                            columns[i] = new DataColumn(dr.GetName(i), dr.GetFieldType(i));
                        }
                        dt.Columns.AddRange(columns);
                        // Добавление колонки для отметки просроченных заявок
                        dt.Columns.Add("isExpired", Type.GetType("System.Boolean"));

                        object[] record = new object[dr.FieldCount + 1];
                        while (dr.Read())
                        {
                            dr.GetValues(record);
                            DateTime executionDate = (DateTime)record[2];

                            // Логика определения просроченной заявки
                            if (executionDate < DateTime.Now && record[7].ToString() == "Входящая")
                                record[record.Length - 1] = true;
                            // Автоматическая отмена заявок со статусом "В работе", у которых истек срок выполнения
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

                    // Выполнение автоматических обновлений статусов просроченных заявок
                    if (cmdUpdateExpired != string.Empty)
                    {
                        MySqlCommand cmd2 = new MySqlCommand(cmdUpdateExpired, conn);
                        cmd2.ExecuteNonQuery();
                    }

                    // Сохранение всех строк для пагинации
                    _allRows.Clear();
                    foreach (DataRow row in dt.Rows)
                    {
                        _allRows.Add(row);
                    }

                    ShowRecordsCount(cmdText);

                    // Для директора отображается общая сумма доходов
                    if (AccountHolder.UserRole == "Директор")
                        ShowTotalSum(cmdText);
                    else
                        totalSumDock.Visibility = Visibility.Collapsed;

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

            // Корректировка текущей страницы, если она выходит за границы
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
                claimsDG.ItemsSource = null;
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

            claimsDG.ItemsSource = pageTable.AsDataView();
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
        /// Обработчик ручного ввода номера страницы
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
        /// Валидация ввода - только цифры для поля номера страницы
        /// </summary>
        private void OnlyNumbers_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]");
            e.Handled = regex.IsMatch(e.Text);
        }

        /// <summary>
        /// Фильтрация заявок по статусу (RadioButton)
        /// </summary>
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

        /// <summary>
        /// Поиск по номеру заявки или ФИО клиента
        /// Активируется при длине поискового запроса более 3 символов или при вводе цифр
        /// </summary>
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

        /// <summary>
        /// Фильтрация по диапазону дат создания заявки
        /// </summary>
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

            // Ограничение выбора дат (начало не может быть позже конца и наоборот)
            fromDate.DisplayDateEnd = toDate.SelectedDate == null || toDate?.SelectedDate.Value > DateTime.Now ? DateTime.Now : toDate.SelectedDate.Value.AddDays(-1);
            toDate.DisplayDateStart = fromDate.SelectedDate == null ? fromDate.DisplayDateStart : fromDate.SelectedDate.Value.AddDays(1);
            RefreshDatagrid();
        }

        /// <summary>
        /// Валидация ввода дат - разрешаем только управляющие символы (Backspace, пробел)
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
        /// Валидация ввода поискового запроса - разрешены буквы, цифры, дефис, пробел, Backspace
        /// </summary>
        private void searchByContractNumAndFio_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[0-9A-Za-zА-Яа-я-\b\s]");
            if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                e.Handled = false;
            else
                e.Handled = true;
        }

        /// <summary>
        /// Кнопка сброса всех фильтров
        /// </summary>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            fromDate.Text = "";
            toDate.Text = "";
            allStatuses.IsChecked = true;
            searchByContractNumAndFio.Text = "";
            reportVariantsComboBox.SelectedItem = null;

            _currentPage = 1;
            _pageSize = 3;
        }

        /// <summary>
        /// При выборе строки в DataGrid активируется кнопка просмотра заявки
        /// </summary>
        private void claimsDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            showClaimButton.IsEnabled = true;
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
        /// Отображение общей суммы доходов от заявок (только для директора)
        /// </summary>
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
                    totalSumDock.Visibility = Visibility.Visible;
                }
                catch
                {
                    totalSumLabel.Content = "0";
                }
            }
        }

        /// <summary>
        /// Сокрытие части ФИО (остается фамилия и инициалы) - в текущей версии не используется
        /// </summary>
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

        /// <summary>
        /// Кнопка печати отчета - выбор типа отчета из выпадающего списка
        /// </summary>
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

        /// <summary>
        /// Кнопка выбора сотрудника - открывает окно просмотра сотрудников
        /// </summary>
        private void chooseAnEmployee_Click(object sender, RoutedEventArgs e)
        {
            var win = new EmployeesViewWindow();
            win.Show();
        }

        /// <summary>
        /// Печать отчета "Учет заявок" в Excel
        /// Формирует таблицу со всеми заявками, применяет цветовое выделение статусов,
        /// добавляет итоговую информацию (количество заявок, общий доход, период)
        /// </summary>
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

                int colCount = claimsDG.Columns.Count - 1;

                var data = new List<object[]>();
                var cols = new List<object>();
                // Сбор заголовков колонок (исключая колонку "Описание")
                foreach (var col in claimsDG.Columns)
                {
                    if (col.Header.ToString() != "Описание")
                    {
                        cols.Add(col.Header);
                    }
                }
                data.Add(cols.ToArray());

                // Формирование условий фильтрации для отчета
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
                        int rowCount = 0;
                        while (dr.Read())
                        {
                            rowCount++;
                            dr.GetValues(record);
                            // Форматирование дат
                            record[1] = ((DateTime)record[1]).ToString("dd.MM.yyyy");
                            record[2] = ((DateTime)record[2]).ToString("dd.MM.yyyy");
                            object[] valuesRightOrder = new object[] { record[0], record[1], record[2], record[3], record[4], record[5], record[6], record[8], record[9], record[7], record[10] };
                            data.Add(valuesRightOrder);
                            record = new object[dr.FieldCount + 1];
                        }

                        Excel.Range startCell = worksheet.Range["A2"];
                        Excel.Range endCell = worksheet.Cells[rowCount + 2, colCount];

                        Excel.Range writeRange = worksheet.Range[startCell, endCell];
                        object[,] dataArray = new object[rowCount + 1, colCount];

                        // Заполнение массива данных
                        for (int i = 0; i < rowCount + 1; i++)
                        {
                            for (int j = 0; j < colCount; j++)
                            {
                                dataArray[i, j] = data[i][j];
                            }
                        }

                        writeRange.Value2 = dataArray;
                        writeRange.Columns.AutoFit();

                        // Цветовое выделение статусов заявок
                        for (int i = 2; i <= rowCount + 2; i++)
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
                            else if (cell.Text == "Отменена")
                                cell.Interior.Color = Excel.XlRgbColor.rgbDarkRed;
                        }

                        // Создание форматированной таблицы Excel
                        Excel.ListObject table = worksheet.ListObjects.Add(
                            Excel.XlListObjectSourceType.xlSrcRange,
                            worksheet.Range[startCell, endCell],
                            Type.Missing,
                            Excel.XlYesNoGuess.xlYes,
                            Type.Missing);
                        table.Name = "Claims";

                        // Добавление информации о количестве заявок
                        Excel.Range recordCount = worksheet.Cells[1][rowCount + 3];
                        recordCount.Value = $"Количество заявок: {recordsCountLabel.Content}";
                        recordCount.Font.Bold = true;
                        recordCount.Font.Size = 16;

                        // Добавление информации об общем доходе
                        Excel.Range incomes = worksheet.Cells[1][rowCount + 5];
                        incomes.Value = $"Общий доход от реализации заявок: {totalSumLabel.Content}";
                        incomes.Font.Bold = true;
                        incomes.Font.Size = 16;

                        // Добавление информации о периоде отчета
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

        /// <summary>
        /// Печать рейтинга сотрудников (менеджеров или мастеров)
        /// </summary>
        /// <param name="isManager">true - рейтинг менеджеров, false - рейтинг мастеров</param>
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
                    // Выбор поля для группировки: employees_id (менеджер) или master_id (мастер)
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
                                bestMaster = record[0].ToString();  // Лучший сотрудник (с максимальным количеством заявок)
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

                        // Создание таблицы Excel
                        Excel.ListObject table = worksheet.ListObjects.Add(
                            Excel.XlListObjectSourceType.xlSrcRange,
                            worksheet.Range[startCell, endCell],
                            Type.Missing,
                            Excel.XlYesNoGuess.xlYes,
                            Type.Missing);
                        table.Name = "Rating";

                        // Информация о лучшем сотруднике
                        Excel.Range recordCount = worksheet.Cells[1][rowCount + 3];
                        recordCount.Value = $"Лучший {roleName}: {bestMaster}";
                        recordCount.Font.Bold = true;
                        recordCount.Font.Size = 16;

                        // Информация о периоде
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

        /// <summary>
        /// Включение/выключение кнопки печати отчета в зависимости от выбора типа отчета
        /// </summary>
        private void reportVariantsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (reportVariantsComboBox.SelectedItem != null)
                printAReport.IsEnabled = true;
            else
                printAReport.IsEnabled = false;
        }

        /// <summary>
        /// Адаптация интерфейса при изменении размера окна
        /// Изменяется размер шрифта кнопок и количество записей на странице
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double windowHeight = e.NewSize.Height;
            double baseFontSize = 14;
            int newPageSize = _pageSize;

            // Изменение количества записей на странице в зависимости от высоты окна
            if (windowHeight > 800)
            {
                newPageSize = 4;
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

        /// <summary>
        /// Обновление размера шрифта для кнопок и ComboBox
        /// </summary>
        private void UpdateButtonsFontSize(int fontSize)
        {
            var buttons = new[] { showClaimButton, orderButton, toMainButton, printAReport, clearFiltersButton, btnPrev, btnNext };
            foreach (var button in buttons)
            {
                if (button != null)
                    button.FontSize = fontSize;
            }

            if (reportVariantsComboBox != null)
                reportVariantsComboBox.FontSize = fontSize;
        }

        /// <summary>
        /// Открытие окна с диаграммой статистики заявок
        /// </summary>
        private void diagram_Click(object sender, RoutedEventArgs e)
        {
            // Формирование фильтра по датам для диаграммы
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
                additionalDateFilterParams = "1 > 0";
            }

            this.Hide();
            var win = new Diagram(additionalDateFilterParams);
            win.ShowDialog();
            this.ShowDialog();
        }
    }
}