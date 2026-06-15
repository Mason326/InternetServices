using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
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
    /// Форма "Просмотр заявки" - детальная информация о заявке на подключение
    /// Позволяет просматривать и изменять статус заявки в зависимости от роли пользователя
    /// </summary>
    public partial class ClaimVerbose : Window
    {
        // Текущий статус заявки
        string currStatus;
        // ID заявки
        int claimId;
        // Делегат для обновления родительской формы (DataGrid)
        Action<bool> Refresh;
        // Делегат для переоформления заявки
        Action<object[]> ReReleaseClaim;
        // Массив значений полей выбранной заявки
        object[] fieldValuesOfARecord;
        // Флаг: true - из формы учета услуг, false - из другой формы
        bool isAccounting = true;
        // Флаг просроченности заявки
        bool isExpired;
        // Флаг возможности редактирования
        bool isEdit;

        /// <summary>
        /// Конструктор для формы из раздела учета услуг
        /// </summary>
        /// <param name="selectedItems">Данные выбранной заявки</param>
        /// <param name="isEditStatus">Разрешено ли редактирование статуса</param>
        /// <param name="RefreshDG">Метод обновления DataGrid</param>
        public ClaimVerbose(object[] selectedItems, bool isEditStatus, Action<bool> RefreshDG)
        {
            InitializeComponent();
            // Заголовок группы содержит номер заявки и дату создания
            ClaimGroupBox.Header += $" {selectedItems[0]} от {((DateTime)selectedItems[1]).ToString("dd.MM.yyyy")}";
            dateOfExecutionLabel.Content = selectedItems[2].ToString();
            managerNameLabel.Content = selectedItems[6].ToString();

            // Очистка адреса от лишних символов
            string address = string.Join(", ", selectedItems[3].ToString().Split(new string[] { ", ", "\t,", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(el => el.Trim()));
            address = address.Replace(",,", ",");
            mountAddressTextBox.Text = address;

            currStatus = selectedItems[7].ToString();
            isAccounting = true;
            formAContractButton.Visibility = Visibility.Collapsed;
            isEdit = isEditStatus;
            Refresh = RefreshDG;
            isExpired = Convert.ToBoolean(selectedItems[11]);

            // Кнопка формирования договора недоступна для просроченных заявок
            if (isExpired)
            {
                formAContractButton.IsEnabled = false;
            }
            else
            {
                formAContractButton.IsEnabled = true;
            }

            fieldValuesOfARecord = selectedItems;
            claimId = Convert.ToInt32(selectedItems[0]);
        }

        /// <summary>
        /// Конструктор для переоформления заявки (из других форм)
        /// </summary>
        public ClaimVerbose(object[] selectedItems, bool isEditStatus, Action<bool> RefreshDG, Action<object[]> RereleaseClaim)
        {
            InitializeComponent();
            ClaimGroupBox.Header += $" {selectedItems[0]} от {((DateTime)selectedItems[1]).ToString("dd.MM.yyyy")}";
            dateOfExecutionLabel.Content = selectedItems[2].ToString();
            managerNameLabel.Content = selectedItems[6].ToString();

            string address = string.Join(", ", selectedItems[3].ToString().Split(new string[] { ", ", "\t,", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(el => el.Trim()));
            address = address.Replace(",,", ",");
            mountAddressTextBox.Text = address;

            currStatus = selectedItems[7].ToString();
            isEdit = isEditStatus;
            Refresh = RefreshDG;
            isAccounting = false;
            isExpired = Convert.ToBoolean(selectedItems[10]);

            if (isExpired)
            {
                formAContractButton.IsEnabled = false;
            }
            else
            {
                formAContractButton.IsEnabled = true;
            }

            formAContractButton.Visibility = Visibility.Collapsed;
            ReReleaseClaim = RereleaseClaim;
            claimId = Convert.ToInt32(selectedItems[0]);
            fieldValuesOfARecord = selectedItems;
        }

        /// <summary>
        /// Кнопка закрытия формы
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Событие загрузки формы - настройка UI в зависимости от роли и статуса заявки
        /// Загружает список возможных статусов, информацию о клиенте
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

            rereleaseClaimButton.Visibility = Visibility.Collapsed;

            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    string statusQuery = "SELECT `status` FROM claim_status;";

                    // === Настройка для МЕНЕДЖЕРА ===
                    if (AccountHolder.UserRole == "Менеджер")
                    {
                        formAContractButton.Visibility = Visibility.Visible;
                        saveChangesButton.Visibility = Visibility.Collapsed;
                        statusComboBox.IsEnabled = true;
                        // Менеджер не может перевести заявку в статусы "В работе" или "Закрыта"
                        statusQuery = "SELECT `status` FROM claim_status where `status` != 'В работе' && `status` != 'Закрыта';";

                        if (currStatus != "Входящая")
                        {
                            // Для не входящих заявок менеджер может только просматривать
                            if (currStatus == "Отменена")
                            {
                                formAContractButton.Visibility = Visibility.Collapsed;
                                if (!isAccounting)
                                    rereleaseClaimButton.Visibility = Visibility.Visible;
                            }
                            else
                                rereleaseClaimButton.Visibility = Visibility.Collapsed;

                            statusComboBox.ItemsSource = new string[] { currStatus };
                            statusComboBox.SelectedItem = currStatus;
                            statusComboBox.IsEnabled = false;
                            saveChangesButton.IsEnabled = false;
                        }
                        else
                        {
                            // Для входящих заявок можно менять статус
                            if (isExpired)
                                formAContractButton.Visibility = Visibility.Collapsed;
                            saveChangesButton.Visibility = Visibility.Visible;
                        }
                    }
                    // === Настройка для МАСТЕРА ===
                    else if (AccountHolder.UserRole == "Мастер")
                    {
                        formAContractButton.Visibility = Visibility.Collapsed;
                        saveChangesButton.Visibility = Visibility.Collapsed;

                        switch (currStatus)
                        {
                            case "Входящая":
                                // Мастер может взять заявку в работу (кроме просроченных)
                                statusQuery = "SELECT `status` FROM claim_status where `status` != 'Отменена' AND `status` != 'Закрыта';";
                                if (isExpired)
                                    statusComboBox.IsEnabled = false;
                                else
                                {
                                    statusComboBox.IsEnabled = true;
                                    saveChangesButton.Visibility = Visibility.Visible;
                                }
                                break;
                            case "В работе":
                                // Мастер может закрыть или отменить заявку в работе
                                statusQuery = "SELECT `status` FROM claim_status where `status` != 'Входящая' AND `status` != 'Закрыта';";
                                statusComboBox.IsEnabled = true;
                                saveChangesButton.Visibility = Visibility.Visible;
                                break;
                            case "Отменена":
                                statusQuery = "SELECT `status` FROM claim_status where `status` = 'Отменена';";
                                statusComboBox.IsEnabled = false;
                                break;
                            case "Закрыта":
                                statusQuery = "SELECT `status` FROM claim_status where `status` = 'Закрыта';";
                                statusComboBox.IsEnabled = false;
                                break;
                        }
                    }
                    // === Настройка для ДИРЕКТОРА ===
                    else if (AccountHolder.UserRole == "Директор")
                    {
                        statusQuery = "SELECT `status` FROM claim_status;";
                        saveChangesButton.Visibility = Visibility.Collapsed;
                        statusComboBox.IsEnabled = false;
                    }

                    // Загрузка списка статусов (кроме особых случаев)
                    if (!(currStatus == "В работе" || currStatus == "Закрыта") || AccountHolder.UserRole == "Мастер" || AccountHolder.UserRole == "Директор")
                    {
                        conn.Open();
                        MySqlCommand cmd = new MySqlCommand(statusQuery, conn);
                        MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        cmd.ExecuteNonQuery();
                        da.Fill(dt);
                        statusComboBox.ItemsSource = dt.AsEnumerable().Select(dr => dr.ItemArray[0]);
                        statusComboBox.SelectedItem = currStatus;
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось загрузить статусы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Включение/выключение кнопки сохранения в зависимости от прав редактирования
                if (!isEdit)
                    saveChangesButton.IsEnabled = false;
                else
                    saveChangesButton.IsEnabled = true;
            }

            // Загрузка информации о клиенте
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    // Получение ID клиента по ID заявки
                    MySqlCommand cmd = new MySqlCommand($"SELECT `client_id` FROM connection_claim where `id_claim` = {claimId};", conn);
                    int clientId = Convert.ToInt32(cmd.ExecuteScalar());
                    // Получение ФИО клиента
                    MySqlCommand cmd2 = new MySqlCommand($"SELECT `full_name` FROM `client` where `idclient` = {clientId};", conn);
                    string fullName = cmd2.ExecuteScalar().ToString();
                    clientLabel.Content = fullName;
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось загрузить клиента\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Кнопка "Сохранить изменения" - обновление статуса заявки в БД
        /// При отмене заявки также расторгает связанный договор
        /// </summary>
        private void saveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            int contractId = -1;
            // Проверка наличия договора, связанного с заявкой
            try
            {
                contractId = CheckDuplicateUtil.HasNoDuplicate("contract", "connection_claim_id", $"{claimId}", false);
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить договор\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();  // Транзакция для согласованного обновления
                try
                {
                    // Обновление статуса заявки
                    MySqlCommand cmd = new MySqlCommand($"Update `connection_claim` SET `claim_status_id` = (SELECT idclaim_status from claim_status where `status` = '{statusComboBox.SelectedItem}') where id_claim = {claimId};", conn);

                    // Если заявка отменяется и есть договор - расторгаем договор
                    if (contractId != -1 && statusComboBox.SelectedItem.ToString() == "Отменена")
                        cmd.CommandText += $"update contract set contract_status_id = (Select idcontract_status from contract_status where `status` = 'Расторгнут') where idcontract = {contractId};";

                    cmd.Transaction = transaction;
                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    MessageBox.Show("Статус успешно обновлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    Refresh(true);  // Обновление родительского DataGrid
                    this.Close();
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось обновить статусы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    transaction.Rollback();  // Откат транзакции при ошибке
                }
            }
        }

        /// <summary>
        /// Кнопка переоформления заявки (вызов делегата из родительской формы)
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ReReleaseClaim(fieldValuesOfARecord);
            this.Close();
        }

        /// <summary>
        /// Кнопка "Сформировать договор" - открытие формы создания договора
        /// </summary>
        private void formAContractButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new Contract(fieldValuesOfARecord);
            win.ShowDialog();
            this.ShowDialog();
        }

        /// <summary>
        /// При изменении выбранного статуса - блокировка кнопки формирования договора для отмененных заявок
        /// </summary>
        private void statusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (statusComboBox.SelectedItem != null && statusComboBox.SelectedItem.ToString() == "Отменена")
            {
                formAContractButton.IsEnabled = false;  // Для отмененной заявки договор не формируется
            }
            else
            {
                formAContractButton.IsEnabled = true;
            }
        }
    }
}