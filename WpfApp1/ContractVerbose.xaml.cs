using System;
using System.Collections.Generic;
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
using MySql.Data.MySqlClient;
using System.Windows.Shapes;
using System.Data;

namespace WpfApp1
{
    /// <summary>
    /// Форма "Просмотр договора" - детальная информация о договоре
    /// Отображает данные договора, позволяет изменять его статус (для ролей, не являющихся директором)
    /// </summary>
    public partial class ContractVerbose : Window
    {
        // Текущий статус договора
        string currentStatus;
        // ID договора
        int contactId;
        // Делегат для обновления родительской формы (DataGrid)
        Action RefreshDG;

        /// <summary>
        /// Конструктор формы - инициализация компонентов, заполнение полей данными договора
        /// </summary>
        /// <param name="selectedItems">Массив данных выбранного договора</param>
        /// <param name="refresh">Метод обновления DataGrid в родительской форме</param>
        public ContractVerbose(object[] selectedItems, Action refresh)
        {
            InitializeComponent();

            // Заголовок группы содержит номер договора и дату заключения
            ContractGroupBox.Header += $"{selectedItems[0]} от {((DateTime)selectedItems[1]).ToString("dd.MM.yyyy")}";

            contactId = Convert.ToInt32(selectedItems[0]);          // ID договора
            ClientLabel.Content += selectedItems[2].ToString();     // ФИО клиента
            ClaimNumberLabel.Content += selectedItems[3].ToString(); // Номер связанной заявки
            TariffNameLabel.Content += selectedItems[5].ToString();  // Название тарифа
            ClaimDateLabel.Content += $"{((DateTime)selectedItems[6]).ToString("dd.MM.yyyy")}"; // Дата заявки

            // Очистка адреса от лишних символов
            string address = string.Join(", ", selectedItems[7].ToString().Split(new string[] { ", ", "\t,", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(el => el.Trim()));
            address = address.Replace(",,", ",");
            AddressTextBox.Text += address;

            currentStatus = selectedItems[4].ToString();  // Текущий статус договора
            RefreshDG += refresh;

            // По умолчанию кнопки активны
            statusComboBox.IsEnabled = true;
            saveChangesButton.Visibility = Visibility.Visible;

            // Для директора - только просмотр, без возможности изменения
            if (AccountHolder.UserRole == "Директор")
            {
                statusComboBox.IsEnabled = false;          // Блокировка выбора статуса
                saveChangesButton.Visibility = Visibility.Collapsed;  // Скрытие кнопки сохранения
            }
        }

        /// <summary>
        /// Кнопка закрытия формы
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Событие загрузки формы - отображение роли пользователя,
        /// загрузка списка возможных статусов договора (исключая "Не заключен")
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

            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    // Загрузка статусов договора (исключая "Не заключен", так как договор уже заключен)
                    MySqlCommand cmd = new MySqlCommand("SELECT `status` FROM contract_status where `status` != 'Не заключен';", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);

                    // Заполнение выпадающего списка и установка текущего статуса
                    statusComboBox.ItemsSource = dt.AsEnumerable().Select(dr => dr.ItemArray[0]);
                    statusComboBox.SelectedItem = currentStatus;
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось загрузить статусы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Кнопка "Сохранить изменения" - обновление статуса договора в базе данных
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    // Обновление статуса договора на выбранный
                    MySqlCommand cmd = new MySqlCommand($"Update contract Set contract_status_id = (Select idcontract_status from contract_status where `status` = '{statusComboBox.SelectedItem}') where idcontract = {contactId};", conn);
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Статус успешно обновлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshDG();    // Обновление родительского DataGrid
                    this.Close();    // Закрытие формы
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось обновить статус\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}