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
    /// Interaction logic for Window18.xaml
    /// </summary>
    public partial class ClaimVerbose : Window
    {
        string currStatus;
        int claimId;
        Action Refresh;
        bool isEdit;
        public ClaimVerbose(object[] selectedItems, bool isEditStatus, Action RefreshDG)
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
            claimId = Convert.ToInt32(selectedItems[0]);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    string statusQuery = "SELECT `status` FROM claim_status;";
                    if (AccountHolder.UserRole == "Менеджер")
                    {
                        statusQuery = "SELECT `status` FROM claim_status where `status` != 'В работе' && `status` != 'Закрыта';";
                        if (currStatus == "В работе" || currStatus == "Закрыта" || currStatus == "Отменена")
                        {
                            statusComboBox.ItemsSource = new string[] { currStatus };
                            statusComboBox.SelectedItem = currStatus;
                            statusComboBox.IsEnabled = false;
                            saveChangesButton.IsEnabled =  false;
                            return;
                        }
                    }
                    else if (AccountHolder.UserRole == "Мастер")
                    {
                        statusQuery = "SELECT `status` FROM claim_status where `status` != 'Входящая';";
                    }
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(statusQuery, conn);
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

                if (!isEdit)
                    saveChangesButton.IsEnabled = false;
                else
                    saveChangesButton.IsEnabled = true;
            }

            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"SELECT `client_id` FROM connection_claim where `id_claim` = {claimId};", conn);
                    int clientId = Convert.ToInt32(cmd.ExecuteScalar());
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

        private void saveChangesButton_Click(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"Update `connection_claim` SET `claim_status_id` = (SELECT idclaim_status from claim_status where `status` = '{statusComboBox.SelectedItem}') where id_claim = {claimId}", conn);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Статус успешно обновлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    Refresh();
                    this.Close();
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось обновить статусы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
