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
        public ClientVerbose(object[] selectedItems)
        {
            InitializeComponent();
            clientId = Convert.ToInt32(selectedItems[0]);
            if (selectedItems[2] != null)
                emailLabel.Content = selectedItems[2].ToString();
            phoneLabel.Content = selectedItems[3].ToString();
            placeOfResidenceLabel.Text = selectedItems[4].ToString();
            dateOfBirthLabel.Content = selectedItems[5].ToString();
            abonentLoginLabel.Content = selectedItems[6].ToString();
            abonentPasswordLabel.Content = selectedItems[7].ToString();
            passportSeriesLabel.Content = selectedItems[8].ToString();
            passportNumberLabel.Content = selectedItems[9].ToString();
            issuedByLabel.Text = selectedItems[10].ToString();
            issueDateLabel.Content = selectedItems[11].ToString();
            departmentCodeLabel.Content = selectedItems[12].ToString();
            currStatus = selectedItems[13].ToString();
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
    }
}
