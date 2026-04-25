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
    /// Interaction logic for ClientVerbose.xaml
    /// </summary>
    public partial class ClientVerbose : Window
    {
        int clientId;
        string currStatus;
        Action<bool> RefreshDG;
        public ClientVerbose(object[] selectedItems, Action<bool> refresh)
        {
            InitializeComponent();
            clientId = Convert.ToInt32(selectedItems[0]);
            LoadClientData(clientId);
            RefreshDG += refresh;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
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
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT `status_name` FROM client_status;", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);
                    statusComboBox.ItemsSource = dt.AsEnumerable().Select(dr => dr.ItemArray[0]);
                    statusComboBox.SelectedItem = currStatus;
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось загрузить статусы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"SELECT full_name FROM `client` Where idclient = {clientId};", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    fioLabel.Content = cmd.ExecuteScalar().ToString();
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось загрузить статусы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void saveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"Update `client` set `client_status_id` = (select idclient_status from client_status where status_name = '{statusComboBox.SelectedItem}') where idclient = {clientId};", conn);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Статус успешно обновлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshDG(false);
                    this.Close();
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось обновить статус\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadClientData(int clientId)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"Select idclient, full_name, email, phone_number, place_of_residence, birthdate, subscriber_login, subscriber_password, passport_series, passport_number, issued_by, issue_date, department_code, client_status.status_name as 'client_status' from `client` inner join `client_status` on `client`.client_status_id = client_status.idclient_status where idclient = {clientId};", conn);
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            if (dr.GetValue(2) != null)
                            {
                                emailLabel.Content = dr.GetValue(2).ToString();
                            }
                            phoneLabel.Content = dr.GetString("phone_number");
                            placeOfResidenceLabel.Text = dr.GetString("place_of_residence");
                            dateOfBirthLabel.Content = dr.GetDateTime("birthdate").ToString("dd.MM.yyyy");
                            abonentLoginLabel.Content = dr.GetString("subscriber_login");
                            abonentPasswordLabel.Content = dr.GetString("subscriber_password");
                            passportSeriesLabel.Content = dr.GetString("passport_series");
                            passportNumberLabel.Content = dr.GetString("passport_number");
                            issuedByLabel.Text = dr.GetString("issued_by");
                            issueDateLabel.Content = dr.GetDateTime("issue_date").ToString("dd.MM.yyyy");
                            departmentCodeLabel.Content = dr.GetString("department_code");
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
