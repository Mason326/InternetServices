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

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Window14.xaml
    /// </summary>
    public partial class CreateClient : Window
    {

        public CreateClient(bool isSelectClient)
        {
            InitializeComponent();
            if (isSelectClient)
            {
                inClaimButton.IsEnabled = false;
            }
            else
            {
                inClaimButton.IsEnabled = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(@"Select idclient, full_name, email, phone_number, place_of_residence, birthdate, subscriber_login, subscriber_password, passport_series, passport_number, issued_by, issue_date, department_code, client_status.status_name as 'client_status' from `client` inner join `client_status` on `client`.client_status_id = client_status.idclient_status;", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);
                    clientsDG.ItemsSource = dt.AsDataView();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void fioTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[А-Яа-я-\b\s]");
            try
            {
                if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                    e.Handled = false;
                else
                    e.Handled = true;
                string[] arr = fioTextBox.Text.Split(' ');
                if (arr.Length > 0)
                    fioTextBox.Text = string.Join(" ", arr.Select(s => $"{s[0].ToString().ToUpper()}{s.Substring(1)}"));
                fioTextBox.CaretIndex = fioTextBox.Text.Length;
            }
            catch
            {
                ;
            }
        }

        private void clientsDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            inClaimButton.IsEnabled = true;
        }

        private void inClaimButton_Click(object sender, RoutedEventArgs e)
        {
            if (clientsDG.SelectedItem != null)
            {
                DataRowView drv = clientsDG.SelectedItem as DataRowView;
                ClientHolder.data = drv.Row.ItemArray;
                this.Close();
            }
        }
    }
}
