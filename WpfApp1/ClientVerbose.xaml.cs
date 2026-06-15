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
    /// Форма "Просмотр клиента" - детальная информация о клиенте
    /// Отображает личные данные, паспортную информацию, учетные данные абонента
    /// Позволяет изменять статус клиента
    /// </summary>
    public partial class ClientVerbose : Window
    {
        // ID клиента
        int clientId;
        // Текущий статус клиента
        string currStatus;
        // Делегат для обновления родительской формы (DataGrid)
        Action<bool> RefreshDG;

        /// <summary>
        /// Конструктор формы - инициализация компонентов, загрузка данных клиента
        /// </summary>
        /// <param name="selectedItems">Массив данных выбранного клиента</param>
        /// <param name="refresh">Метод обновления DataGrid в родительской форме</param>
        public ClientVerbose(object[] selectedItems, Action<bool> refresh)
        {
            InitializeComponent();
            clientId = Convert.ToInt32(selectedItems[0]);  // ID клиента - первый элемент массива
            LoadClientData(clientId);                      // Загрузка детальной информации
            RefreshDG += refresh;
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
        /// загрузка списка возможных статусов клиента
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
                    // Получение всех возможных статусов клиента из справочника
                    MySqlCommand cmd = new MySqlCommand("SELECT `status_name` FROM client_status;", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);
                    // Заполнение выпадающего списка статусов
                    statusComboBox.ItemsSource = dt.AsEnumerable().Select(dr => dr.ItemArray[0]);
                    statusComboBox.SelectedItem = currStatus;  // Выбор текущего статуса
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось загрузить статусы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Отображение ФИО клиента в отдельном элементе
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"SELECT full_name FROM `client` Where idclient = {clientId};", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    fioLabel.Content = cmd.ExecuteScalar().ToString();  // Установка ФИО в Label
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось загрузить статусы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Кнопка "Сохранить изменения" - обновление статуса клиента в базе данных
        /// </summary>
        private void saveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    // Обновление статуса клиента по выбранному значению из ComboBox
                    MySqlCommand cmd = new MySqlCommand($"Update `client` set `client_status_id` = (select idclient_status from client_status where status_name = '{statusComboBox.SelectedItem}') where idclient = {clientId};", conn);
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Статус успешно обновлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshDG(false);  // Обновление родительского DataGrid
                    this.Close();       // Закрытие формы
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось обновить статус\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Загрузка детальной информации о клиенте из базы данных
        /// Заполняет все поля формы: контактные данные, паспорт, абонентские данные
        /// </summary>
        /// <param name="clientId">ID клиента для загрузки</param>
        private void LoadClientData(int clientId)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    // SQL-запрос на получение всех данных клиента с JOIN на таблицу статусов
                    MySqlCommand cmd = new MySqlCommand($@"Select idclient, full_name, email, phone_number, place_of_residence, 
                                                                  birthdate, subscriber_login, subscriber_password, 
                                                                  passport_series, passport_number, issued_by, issue_date, 
                                                                  department_code, client_status.status_name as 'client_status' 
                                                           from `client` 
                                                           inner join `client_status` 
                                                           on `client`.client_status_id = client_status.idclient_status 
                                                           where idclient = {clientId};", conn);

                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            // Email (может быть NULL)
                            if (dr.GetValue(2) != null)
                            {
                                emailLabel.Content = dr.GetValue(2).ToString();
                            }

                            // Контактная информация
                            phoneLabel.Content = dr.GetString("phone_number");
                            placeOfResidenceLabel.Text = dr.GetString("place_of_residence");
                            dateOfBirthLabel.Content = dr.GetDateTime("birthdate").ToString("dd.MM.yyyy");

                            // Учетные данные абонента (логин/пароль для личного кабинета)
                            abonentLoginLabel.Content = dr.GetString("subscriber_login");
                            abonentPasswordLabel.Content = dr.GetString("subscriber_password");

                            // Паспортные данные
                            passportSeriesLabel.Content = dr["passport_series"];
                            passportNumberLabel.Content = dr["passport_number"];
                            issuedByLabel.Text = dr.GetString("issued_by");
                            issueDateLabel.Content = dr.GetDateTime("issue_date").ToString("dd.MM.yyyy");
                            departmentCodeLabel.Content = dr.GetString("department_code");

                            // Текущий статус клиента
                            currStatus = dr.GetString("client_status");
                        }
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось загрузить данные клиента\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}