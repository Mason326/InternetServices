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
        Action<object[]> ReReleaseClaim;
        object[] fieldValuesOfARecord;
        bool isAccounting = true;
        bool isExpired;
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
            isAccounting = true;
            formAContractButton.Visibility = Visibility.Collapsed;
            isEdit = isEditStatus;
            Refresh = RefreshDG;
            isExpired = Convert.ToBoolean(selectedItems[10]);
            fieldValuesOfARecord = selectedItems;
            claimId = Convert.ToInt32(selectedItems[0]);
        }

        public ClaimVerbose(object[] selectedItems, bool isEditStatus, Action RefreshDG, Action<object[]> RereleaseClaim)
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
            formAContractButton.Visibility = Visibility.Collapsed;
            ReReleaseClaim = RereleaseClaim;
            claimId = Convert.ToInt32(selectedItems[0]);
            fieldValuesOfARecord = selectedItems;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            rereleaseClaimButton.Visibility = Visibility.Collapsed;
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    string statusQuery = "SELECT `status` FROM claim_status;";
                    if (AccountHolder.UserRole == "Менеджер")
                    {
                        formAContractButton.Visibility = Visibility.Visible;
                        saveChangesButton.Visibility = Visibility.Collapsed;
                        statusComboBox.IsEnabled = true;
                        statusQuery = "SELECT `status` FROM claim_status where `status` != 'В работе' && `status` != 'Закрыта';";
                        if (currStatus != "Входящая")
                        {
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
                            if(isExpired) 
                                formAContractButton.Visibility = Visibility.Collapsed;
                            saveChangesButton.Visibility = Visibility.Visible;
                        }
                    }
                    else if (AccountHolder.UserRole == "Мастер")
                    {
                        formAContractButton.Visibility = Visibility.Collapsed;
                        saveChangesButton.Visibility = Visibility.Collapsed;
                        switch (currStatus)
                        {
                            case "Входящая":
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
                    else if (AccountHolder.UserRole == "Директор")
                    {
                        saveChangesButton.Visibility = Visibility.Collapsed;
                        statusComboBox.IsEnabled = false;
                    }
                    if (!(currStatus == "В работе" || currStatus == "Закрыта") || AccountHolder.UserRole == "Мастер")
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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ReReleaseClaim(fieldValuesOfARecord);
            this.Close();
        }

        private void formAContractButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new Contract(fieldValuesOfARecord);
            win.ShowDialog();
            this.ShowDialog();
        }
    }
}
