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
    /// Interaction logic for EmployeesViewWindow.xaml
    /// </summary>
    public partial class EmployeesViewWindow : Window
    {
        public EmployeesViewWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"SELECT * FROM employees where roles_id = (select idroles from `roles` where role_name = 'Мастер');", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    cmd.ExecuteNonQuery();
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    employeesDG.ItemsSource = dt.AsDataView();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить сотрудников\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            inClaimButton.IsEnabled = false;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (employeesDG.SelectedItem != null)
            {
                DataRowView drv = employeesDG.SelectedItem as DataRowView;
                MasterHolder.data = drv.Row.ItemArray;
                this.Close();
            }
        }

        private void employeesDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            inClaimButton.IsEnabled = true;
        }
    }
}
