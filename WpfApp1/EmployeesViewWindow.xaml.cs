using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
                    MySqlCommand cmd = new MySqlCommand($@"SELECT idemployees, full_name, login, password, roles_id, phoneNumber, photo FROM employees where roles_id = (select idroles from `roles` where role_name = 'Мастер');", conn);
                    DataTable dt = new DataTable();
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        DataColumn[] columns = new DataColumn[dr.FieldCount];
                        for (int i = 0; i < columns.Length; i++)
                        {
                            columns[i] = new DataColumn(dr.GetName(i), dr.GetFieldType(i));
                        }

                        dt.Columns.AddRange(columns);
                        BitmapImage image = new BitmapImage();
                        Type type = image.GetType();
                        dt.Columns.Add("UserPhoto", type);
                        object[] record = new object[dr.FieldCount + 1];
                        while (dr.Read())
                        {
                            dr.GetValues(record);
                            byte[] imageBytes = record[6] as byte[];
                            record[7] = LoadImage(imageBytes);
                            dt.LoadDataRow(record, true);
                        }
                    }

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

        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return new BitmapImage(new Uri("pack://application:,,,/Resources/Images/user.png"));
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }
    }
}
