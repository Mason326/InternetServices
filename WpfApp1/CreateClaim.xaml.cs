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
using System.Windows.Threading;

namespace WpfApp1
{
    /// <summary>
    /// Форма "Создание заявки" - основная форма для создания и редактирования заявок на подключение
    /// Позволяет: создавать новую заявку, редактировать существующую, выбирать мастера,
    /// выбирать тариф, дату и время выполнения, подключать дополнительные услуги
    /// </summary>
    public partial class CreateClaim : Window
    {
        // Словарь тарифов (ID - название)
        Dictionary<int, string> tariffs = new Dictionary<int, string>();
        // ID статуса "Входящая" (константа)
        const int INCOMING_CLAIM_STATUS_ID = 1;
        // Количество заявок у мастера на выбранную дату
        int recordsCount = 0;
        // Хранение даты редактируемой заявки (для восстановления)
        string[] currEditClaimDate;
        // Условие фильтрации для SQL-запроса
        string filterOption = "";
        // Флаг режима редактирования
        bool isEditing = false;
        // Таймер автоматического обновления
        DispatcherTimer timerRef;
        // Флаг просроченности заявки
        bool isExpired = false;
        // DataTable для хранения дополнительных услуг
        DataTable dtAddServices = new DataTable();
        // Таймер неактивности (автоматический выход)
        private DispatcherTimer inactivityTimer;

        /// <summary>
        /// Конструктор формы - инициализация компонентов, настройка таймеров
        /// </summary>
        public CreateClaim()
        {
            InitializeComponent();

            // Таймер неактивности: 2 минуты бездействия -> возврат на форму авторизации
            inactivityTimer = new DispatcherTimer();
            inactivityTimer.Interval = TimeSpan.FromMinutes(2);
            inactivityTimer.Tick += CheckInactivity;

            // Таймер автоматического обновления DataGrid (каждые 5 минут)
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(300);
            timer.Tick += Timer_Tick;
            timer.Start();

            // Инициализация DataTable для доп. услуг
            dtAddServices.Columns.Add("additional_service_name", typeof(string));
            dtAddServices.Columns.Add("cost", typeof(double));
            dtAddServices.Columns.Add("additional_service_id", typeof(int));

            timerRef = timer;
        }

        /// <summary>
        /// Проверка неактивности - выход из учетной записи
        /// </summary>
        private void CheckInactivity(object sender, EventArgs e)
        {
            inactivityTimer.Stop();
            Auth.BackToAuth();
        }

        /// <summary>
        /// Сброс таймера неактивности при движении мыши
        /// </summary>
        private void HandleActivity(object sender, MouseEventArgs e)
        {
            inactivityTimer.Stop();
            inactivityTimer.Start();
        }

        /// <summary>
        /// Сброс таймера неактивности при нажатии клавиш
        /// </summary>
        private void HandleActivity(object sender, KeyEventArgs e)
        {
            inactivityTimer.Stop();
            inactivityTimer.Start();
        }

        /// <summary>
        /// Таймер обновления данных (если не в режиме редактирования)
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!isEditing)
                RefreshData();
        }

        /// <summary>
        /// Кнопка "На главную" - очистка выбранных данных, остановка таймеров, закрытие формы
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ClearSelected();
            timerRef.Stop();
            timerRef.Tick -= Timer_Tick;
            inactivityTimer.Stop();
            Auth.locker = true;
            this.Close();
        }

        /// <summary>
        /// Кнопка выбора клиента - открытие формы создания/выбора клиента
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            inactivityTimer.Stop();
            var win = new CreateClient(true);
            win.ShowDialog();
            inactivityTimer.Start();

            if (ClientHolder.data != null)
            {
                object[] client = ClientHolder.data;
                string fioWithHiddenSurname = client[1].ToString();
                string hiddenPhoneNumber = HidePhoneNumber(client[3].ToString());
                clientTextBox.Text = $"{fioWithHiddenSurname}, {hiddenPhoneNumber}";
            }
        }

        /// <summary>
        /// Событие загрузки формы - инициализация UI, загрузка тарифов, настройка дат
        /// </summary>
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

            RefreshData();
            dateOfExecution.IsEnabled = false;
            timeOfExecution.IsEnabled = false;

            // Загрузка списка тарифов из БД
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
            dateOfExecution.DisplayDateStart = DateTime.Now.AddDays(1);  // Минимум завтрашний день
        }

        /// <summary>
        /// Установка статуса заявки как "Входящая" (для новых заявок)
        /// </summary>
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

        /// <summary>
        /// При изменении даты выполнения - обновление доступного времени для мастера
        /// </summary>
        private void dateOfExecution_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            List<string> times = ShowAvailableTime();
            if (times.Count > 0)
                timeOfExecution.ItemsSource = times;
        }

        /// <summary>
        /// Получение доступного времени для мастера на выбранную дату
        /// Возвращает список свободных 30-минутных интервалов с 09:00 до 17:00
        /// </summary>
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
                    // Поиск уже занятых временных слотов у мастера
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

                    // Все возможные временные интервалы
                    List<string> timePeriodArr = new List<string>() { "09:00", "09:30", "10:00", "10:30", "11:00", "11:30", "12:00", "12:30", "13:00", "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30", "17:00" };
                    List<string> res = new List<string>();

                    // Исключаем занятые слоты
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

        /// <summary>
        /// Валидация ввода адреса: разрешены цифры, русские буквы, дефис, точка, запятая, пробел, Backspace
        /// </summary>
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

        /// <summary>
        /// Кнопка "Создать заявку" - добавление новой заявки в базу данных
        /// </summary>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            // Проверка заполнения всех обязательных полей
            if (mountAddressTextBox.Text.Length > 0 && clientTextBox.Text.Length > 0 &&
                dateOfExecution.SelectedDate != null && timeOfExecution.SelectedItem != null &&
                tariffComboBox.SelectedItem != null && masterTextBox.Text.Length > 0)
            {
                try
                {
                    // Проверка лимита заявок для мастера (не более 7 в день)
                    if (recordsCount > 6)
                    {
                        MessageBox.Show($"Не удалось добавить заявку. Указанный мастер превысил количество взятых заявок в сутки", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        MySqlTransaction transaction = conn.BeginTransaction();
                        try
                        {
                            // Основной запрос на вставку заявки
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

                            // Добавление дополнительных услуг, если выбраны
                            if (AdditionalServicesHolder.additionalServices.Count > 0)
                            {
                                cmd.CommandText += "Insert into additional_service_pack Values ";
                                foreach (var el in AdditionalServicesHolder.additionalServices)
                                {
                                    cmd.CommandText += $"({claimNumber.Content}, {el.Value.Row.ItemArray[2]}),";
                                }
                                cmd.CommandText = cmd.CommandText.TrimEnd(new char[] { ',' });
                                cmd.CommandText += ";";
                            }

                            cmd.Transaction = transaction;
                            cmd.ExecuteNonQuery();
                            transaction.Commit();

                            MessageBox.Show($"Заявка успешно создана", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            RefreshData(false);
                            ClearSelected();
                        }
                        catch (Exception exc)
                        {
                            transaction.Rollback();
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

        /// <summary>
        /// Обновление DataGrid со списком заявок
        /// </summary>
        /// <param name="initial">true - начальная загрузка (без сортировки), false - с сортировкой по убыванию ID</param>
        private void RefreshData(bool initial = true)
        {
            // Формирование SQL-запроса в зависимости от параметра
            string cmdString = $@"Select `id_claim`, `connection_creationDate`, `mount_date`, `connection_address`, tariff.`tariff_name` as 'tariff', client.full_name as 'client_fio', employees.full_name as 'employee_fio', claim_status.status as 'claim_status', (Select full_name from employees where idemployees = connection_claim.master_id) as 'master_fio', concat('Дата заявки: ', connection_creationDate, '\nДата выполнения: ', mount_date,'\nАдрес монтирования: ', connection_address, '\nТариф: ', tariff.`tariff_name`) as claimDetails
                                                        from `connection_claim`
                                                        inner join `client` on client.idclient = connection_claim.client_id
                                                        inner join `employees` on employees.idemployees = connection_claim.employees_id
                                                        inner join `tariff` on tariff.idtariff = connection_claim.tariff_id
                                                        inner join `claim_status` on `claim_status`.idclaim_status = connection_claim.claim_status_id {filterOption} order by id_claim desc;";
            if (initial)
            {
                cmdString = $@"Select `id_claim`, `connection_creationDate`, `mount_date`, `connection_address`, tariff.`tariff_name` as 'tariff', client.full_name as 'client_fio', employees.full_name as 'employee_fio', claim_status.status as 'claim_status', (Select full_name from employees where idemployees = connection_claim.master_id) as 'master_fio', concat('Дата заявки: ', connection_creationDate, '\nДата выполнения: ', mount_date,'\nАдрес монтирования: ', connection_address, '\nТариф: ', tariff.`tariff_name`) as claimDetails
                                                        from `connection_claim`
                                                        inner join `client` on client.idclient = connection_claim.client_id
                                                        inner join `employees` on employees.idemployees = connection_claim.employees_id
                                                        inner join `tariff` on tariff.idtariff = connection_claim.tariff_id
                                                        inner join `claim_status` on `claim_status`.idclaim_status = connection_claim.claim_status_id {filterOption};";
            }

            try
            {
                string cmdUpdateExpired = "";
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(cmdString, conn);
                    DataTable dt = new DataTable();

                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        // Создание структуры таблицы
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

                            // Логика определения просроченных и отмены заявок "В работе" с истекшим сроком
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

                    // Автоматическая отмена просроченных заявок
                    if (cmdUpdateExpired != string.Empty)
                    {
                        MySqlCommand cmd2 = new MySqlCommand(cmdUpdateExpired, conn);
                        cmd2.ExecuteNonQuery();
                    }

                    claimsDG.ItemsSource = dt.AsDataView();
                    countRecordsLabel.Content = RecordsCounter.CountRecords("connection_claim");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            UseNewClaimNumber();
        }

        /// <summary>
        /// Генерация нового номера заявки (максимальный ID + 1)
        /// </summary>
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

        /// <summary>
        /// Очистка всех выбранных полей и данных
        /// </summary>
        private void ClearSelected()
        {
            dateOfExecution.SelectedDate = null;
            tariffComboBox.SelectedItem = null;
            ClientHolder.data = null;
            timeOfExecution.SelectedItem = null;
            recordsCount = 0;
            masterTextBox.Clear();
            MasterHolder.data = null;
            AdditionalServicesHolder.additionalServices.Clear();
            if (!isEditing)
            {
                clientTextBox.Clear();
                mountAddressTextBox.Clear();
            }
        }

        /// <summary>
        /// Кнопка "Очистить поля" - сброс всех введенных данных
        /// </summary>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            ClearSelected();
        }

        /// <summary>
        /// Валидация ввода даты - разрешены только Backspace и пробел
        /// </summary>
        private void dateOfExecution_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[\b\s]");
            if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                e.Handled = false;
            else
                e.Handled = true;
        }

        /// <summary>
        /// Сокрытие части ФИО (остается фамилия и инициалы)
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
        /// Сокрытие части номера телефона (маскирование)
        /// </summary>
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

        /// <summary>
        /// Просмотр выбранной заявки (открытие формы ClaimVerbose)
        /// </summary>
        private void ShowClaimVerbose(object sender, RoutedEventArgs e)
        {
            if (claimsDG.SelectedItem != null)
            {
                DataRowView drv = claimsDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;
                inactivityTimer.Stop();
                this.Hide();
                var win = new ClaimVerbose(fieldValuesOfARecord, true, RefreshData, RereleaseClaim);
                win.ShowDialog();
                inactivityTimer.Start();
                this.ShowDialog();
            }
        }

        /// <summary>
        /// Подготовка к редактированию заявки
        /// </summary>
        private void PrepareToEditClaim(object sender, RoutedEventArgs e)
        {
            PrepateToEditClaimMethod(false);
        }

        /// <summary>
        /// Загрузка дополнительных услуг для заявки
        /// </summary>
        private void FillAdditionalServicesHolder(int claimId)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"SELECT additional_services.additional_service_name,
                                                        additional_services.monthly_fee,
                                                        id_additional_service 
                                                        FROM additional_service_pack
                                                        inner join additional_services
                                                        on id_additional_service = additional_services.idadditional_service
                                                        where idclaim = {claimId};", conn);
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        object[] values = new object[dr.FieldCount];
                        while (dr.Read())
                        {
                            dr.GetValues(values);
                            DataRow drow = dtAddServices.NewRow();
                            drow.ItemArray = new object[] { values[0].ToString(), values[1], values[2] };
                            dtAddServices.Rows.Add(drow);
                            DataRowView addedToOrderDg = dtAddServices.DefaultView[dtAddServices.Rows.IndexOf(drow)];
                            AdditionalServicesHolder.additionalServices.Add(values[0].ToString(), addedToOrderDg);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось получить дополнительные услуги заявки\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                AdditionalServicesHolder.additionalServices.Clear();
            }
        }

        /// <summary>
        /// Основной метод подготовки к редактированию заявки
        /// Заполняет поля формы данными выбранной заявки
        /// </summary>
        private void PrepateToEditClaimMethod(bool isCanceled)
        {
            if (claimsDG.SelectedItem != null)
            {
                DataRowView drv = claimsDG.SelectedItem as DataRowView;
                AdditionalServicesHolder.additionalServices.Clear();
                object[] fieldValuesOfARecord = drv.Row.ItemArray;

                // Проверка возможности редактирования по статусу
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

                // Переключение в режим редактирования
                string[] dateByParts = fieldValuesOfARecord[2].ToString().Split(' ');
                createClaimButton.Visibility = Visibility.Collapsed;
                showClaimButton.Visibility = Visibility.Collapsed;
                editButton.Visibility = Visibility.Collapsed;
                toMainButton.Visibility = Visibility.Collapsed;

                claimNumber.Content = fieldValuesOfARecord[0];
                creationDate.Content = ((DateTime)fieldValuesOfARecord[1]).ToString("dd.MM.yyyy");
                FillMasterObject(Convert.ToInt32(fieldValuesOfARecord[0]));

                // Восстановление даты и времени выполнения
                if (!((DateTime)fieldValuesOfARecord[2] < DateTime.Now))
                {
                    dateOfExecution.SelectedDate = DateTime.Parse(dateByParts[0]);
                    string time = DateTime.Parse(dateByParts[1].ToString()).ToString("HH:mm");
                    List<string> times = ShowAvailableTime();
                    times.Add(time);
                    timeOfExecution.IsEnabled = true;
                    timeOfExecution.ItemsSource = times;
                    timeOfExecution.SelectedItem = time;
                }

                // Настройка UI для режима редактирования
                chooseAClientButton.IsEnabled = false;
                claimStatusComboBox.IsEnabled = true;
                currEditClaimDate = dateByParts;
                mountAddressTextBox.IsEnabled = false;
                claimsDG.IsEnabled = false;
                isExpired = Convert.ToBoolean(fieldValuesOfARecord[fieldValuesOfARecord.Length - 1]);
                mountAddressTextBox.Text = string.Join(" ", fieldValuesOfARecord[3].ToString().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
                FillComboBoxStatusesManager();
                FillAdditionalServicesHolder(Convert.ToInt32(claimNumber.Content));

                if (isCanceled)
                    claimStatusComboBox.SelectedItem = "Отменена";
                else
                    claimStatusComboBox.SelectedItem = fieldValuesOfARecord[7];

                tariffComboBox.SelectedItem = fieldValuesOfARecord[4];
                clientTextBox.Text = fieldValuesOfARecord[5].ToString();
                masterTextBox.Text = MasterHolder.data[1].ToString();
                isEditing = true;

                endEditingButton.Visibility = Visibility.Visible;
                cancelChangesButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Отмена редактирования
        /// </summary>
        private void CancelEdit(object sender, RoutedEventArgs e)
        {
            CloseEdition();
        }

        /// <summary>
        /// Выход из режима редактирования, возврат в режим создания
        /// </summary>
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
            currEditClaimDate = null;
            isExpired = false;
            timeOfExecution.IsEnabled = false;
            showClaimButton.IsEnabled = false;
            editButton.IsEnabled = false;
            claimStatusComboBox.IsEnabled = false;
        }

        /// <summary>
        /// Сохранение изменений при редактировании заявки
        /// </summary>
        private void EditClaim(object sender, RoutedEventArgs e)
        {
            // Проверка заполнения полей
            if (mountAddressTextBox.Text.Length > 0 && clientTextBox.Text.Length > 0 &&
                dateOfExecution.SelectedDate != null && timeOfExecution.SelectedItem != null &&
                tariffComboBox.SelectedItem != null && masterTextBox.Text.Length > 0)
            {
                try
                {
                    ShowAvailableTime();
                    if (recordsCount > 6 && claimStatusComboBox.SelectedItem.ToString() != "Отменена")
                    {
                        MessageBox.Show($"Не удалось обновить заявку. Указанный мастер превысил количество взятых заявок в сутки", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int contractId = CheckDuplicateUtil.HasNoDuplicate("contract", "connection_claim_id", $"{claimNumber.Content}", false);

                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        MySqlTransaction transaction = conn.BeginTransaction();
                        try
                        {
                            string fullExectionDate = $"{((DateTime)dateOfExecution.SelectedDate).ToString("yyyy-MM-dd")} {timeOfExecution.SelectedItem.ToString()}:00";

                            // Обновление данных заявки
                            MySqlCommand cmd = new MySqlCommand($@"Update `connection_claim` 
                                                                   set connection_address = '{mountAddressTextBox.Text}',
                                                                   mount_date = '{fullExectionDate}',
                                                                   claim_status_id = (SELECT idclaim_status FROM claim_status where `status` = '{claimStatusComboBox.SelectedItem}'),
                                                                   tariff_id = (SELECT idtariff FROM tariff where `tariff_name` = '{tariffComboBox.SelectedItem}'),
                                                                   master_id = {MasterHolder.data[0]}
                                                                   where id_claim = {claimNumber.Content};", conn);

                            // Удаление старых дополнительных услуг
                            cmd.CommandText += $"Delete from `additional_service_pack` where `idclaim` = {claimNumber.Content};";

                            // Если заявка отменяется - расторгаем договор
                            if (contractId != -1 && claimStatusComboBox.SelectedItem.ToString() == "Отменена")
                            {
                                cmd.CommandText += $"update contract set contract_status_id = (Select idcontract_status from contract_status where `status` = 'Расторгнут') where idcontract = {contractId};";
                            }

                            // Добавление новых дополнительных услуг
                            if (AdditionalServicesHolder.additionalServices.Count > 0)
                            {
                                cmd.CommandText += "Insert into additional_service_pack Values ";
                                foreach (var el in AdditionalServicesHolder.additionalServices)
                                {
                                    cmd.CommandText += $"({claimNumber.Content}, {el.Value.Row.ItemArray[2]}),";
                                }
                                cmd.CommandText = cmd.CommandText.TrimEnd(new char[] { ',' });
                                cmd.CommandText += ";";
                            }

                            cmd.Transaction = transaction;
                            cmd.ExecuteNonQuery();
                            transaction.Commit();

                            MessageBox.Show($"Заявка успешно обновлена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            CloseEdition();
                            RefreshData();
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show($"Не удалось обновить заявку\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            transaction.Rollback();
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

        /// <summary>
        /// Заполнение ComboBox статусов для менеджера (исключая "В работе" и "Закрыта")
        /// </summary>
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

        /// <summary>
        /// Заполнение объекта мастера данными из БД
        /// </summary>
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

        /// <summary>
        /// Активация кнопок при выборе заявки в DataGrid
        /// </summary>
        private void claimsDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            showClaimButton.IsEnabled = true;
            editButton.IsEnabled = true;
        }

        /// <summary>
        /// Выбор мастера (открытие формы выбора сотрудника)
        /// </summary>
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            inactivityTimer.Stop();
            var win = new EmployeesViewWindow();
            win.ShowDialog();
            inactivityTimer.Start();

            if (MasterHolder.data != null)
            {
                object[] master = MasterHolder.data;
                string fio = master[1].ToString();
                masterTextBox.Text = fio;
            }
        }

        /// <summary>
        /// При изменении текста в поле мастера - включение выбора даты
        /// </summary>
        private void masterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (masterTextBox.Text.Length > 0)
            {
                dateOfExecution.IsEnabled = true;
                if (isEditing)
                {
                    dateOfExecution.SelectedDate = null;
                    timeOfExecution.IsEnabled = false;
                    timeOfExecution.SelectedItem = null;
                }
            }
            else
                dateOfExecution.IsEnabled = false;
        }

        /// <summary>
        /// Поиск по номеру заявки или ФИО клиента
        /// </summary>
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

        /// <summary>
        /// Перевыпуск заявки (создание копии с новым номером)
        /// </summary>
        private void RereleaseClaim(object[] fieldValuesOfARecord)
        {
            ClearSelected();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    // Копирование данных клиента
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
                    mountAddressTextBox.Text = string.Join(" ", fieldValuesOfARecord[3].ToString().Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик изменения статуса заявки (для режима редактирования)
        /// </summary>
        private void claimStatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Логика изменения UI в зависимости от выбранного статуса при редактировании
            if (currEditClaimDate != null && claimStatusComboBox.SelectedItem.ToString() == "Отменена")
            {
                PrepateToEditClaimMethod(true);
                dateOfExecution.SelectedDate = DateTime.Parse(currEditClaimDate[0]);
                timeOfExecution.ItemsSource = new string[] { DateTime.Parse(currEditClaimDate[1].ToString()).ToString("HH:mm") };
                timeOfExecution.SelectedItem = DateTime.Parse(currEditClaimDate[1].ToString()).ToString("HH:mm");

                dateOfExecution.IsEnabled = false;
                timeOfExecution.IsEnabled = false;
                chooseAMasterButton.IsEnabled = false;
                tariffComboBox.IsEnabled = false;
                clearFieldsButton.IsEnabled = false;
                additServiceButton.IsEnabled = false;
            }
            else if (currEditClaimDate != null && claimStatusComboBox.SelectedItem.ToString() == "Входящая" && isExpired)
            {
                dateOfExecution.SelectedDate = null;
                timeOfExecution.SelectedItem = null;
                dateOfExecution.IsEnabled = true;
                chooseAMasterButton.IsEnabled = true;
                tariffComboBox.IsEnabled = true;
                clearFieldsButton.IsEnabled = true;
                additServiceButton.IsEnabled = true;
            }
            else if (currEditClaimDate != null && claimStatusComboBox.SelectedItem.ToString() == "Входящая" && !isExpired)
            {
                dateOfExecution.IsEnabled = true;
                timeOfExecution.IsEnabled = true;
                string currTime = DateTime.Parse(currEditClaimDate[1].ToString()).ToString("HH:mm");
                List<string> times = ShowAvailableTime();
                times.Add(currTime);
                timeOfExecution.ItemsSource = times;
                timeOfExecution.SelectedItem = currTime;
                chooseAMasterButton.IsEnabled = true;
                tariffComboBox.IsEnabled = true;
                clearFieldsButton.IsEnabled = true;
                additServiceButton.IsEnabled = true;
            }
            else
            {
                chooseAMasterButton.IsEnabled = true;
                tariffComboBox.IsEnabled = true;
                clearFieldsButton.IsEnabled = true;
                additServiceButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Кнопка выбора дополнительных услуг
        /// </summary>
        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            inactivityTimer.Stop();
            this.Hide();
            var win = new PickAdditionalServices();
            win.ShowDialog();
            inactivityTimer.Start();
            this.ShowDialog();
        }
    }
}