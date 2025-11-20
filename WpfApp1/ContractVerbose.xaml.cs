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
    /// Interaction logic for ContractVerbose.xaml
    /// </summary>
    public partial class ContractVerbose : Window
    {
        string currentStatus;
        int contactId;
        Action RefreshDG;
        public ContractVerbose(object[] selectedItems, Action refresh)
        {
            InitializeComponent();
            ContractGroupBox.Header += $"{selectedItems[0]} от {((DateTime)selectedItems[1]).ToString("dd.MM.yyyy")}";
            contactId = Convert.ToInt32(selectedItems[0]);
            ClientLabel.Content += selectedItems[2].ToString();
            ClaimNumberLabel.Content += selectedItems[3].ToString();
            TariffNameLabel.Content += selectedItems[6].ToString();
            ClaimDateLabel.Content += $"{((DateTime)selectedItems[7]).ToString("dd.MM.yyyy")}";
            string address = string.Join(", ", selectedItems[8].ToString().Split(new string[] { ", ", "\t,", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(el => el.Trim()));
            address = address.Replace(",,", ",");
            AddressTextBox.Text += address;
            currentStatus = selectedItems[5].ToString();
            RefreshDG += refresh;
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
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand("SELECT `status` FROM contract_status;", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);
                    statusComboBox.ItemsSource = dt.AsEnumerable().Select(dr => dr.ItemArray[0]);
                    statusComboBox.SelectedItem = currentStatus;
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось загрузить статусы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"Update contract Set contract_status_id = (Select idcontract_status from contract_status where `status` = '{statusComboBox.SelectedItem}') where idcontract = {contactId};", conn);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show("Статус успешно обновлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshDG();
                    this.Close();
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось обновить статус\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
