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
    /// Interaction logic for PickAdditionalServices.xaml
    /// </summary>
    public partial class PickAdditionalServices : Window
    {
        DataTable dtAddServices = new DataTable();
        Dictionary<string, DataRowView> addServicesDictionary = new Dictionary<string, DataRowView>();
        public PickAdditionalServices()
        {
            InitializeComponent();
            dtAddServices.Columns.Add("additional_service_name", typeof(string));
            dtAddServices.Columns.Add("cost", typeof(double));
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if (orderAddServiceDG.SelectedItem != null)
            {
                var drv = orderAddServiceDG.SelectedItem as DataRowView;
                var items = drv.Row.ItemArray;
                if (addServicesDictionary.ContainsKey(items[0].ToString()))
                {
                    orderAddServiceDG.Items.Remove(drv);
                    addServicesDictionary.Remove(items[0].ToString());
                }
                double cost = addServicesTotalCostLabel.Content.ToString() != string.Empty ? Convert.ToDouble(addServicesTotalCostLabel.Content) : 0;
                if (cost != 0)
                {
                    cost -= Convert.ToDouble(items[1]);
                    if (cost == 0)
                        addServicesTotalCostLabel.Content = "";
                    else
                        addServicesTotalCostLabel.Content = cost;
                }
            }
        }

        private void searchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            Action<string> targetName;
            targetName = FillAdditionalServicesDG;
            if (textBox.Text.Length > 3)
                targetName(textBox.Text);
            else if (textBox.Text.Length == 0)
                targetName("");
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (additionalServicesDG.SelectedItem != null)
            {
                var drv = additionalServicesDG.SelectedItem as DataRowView;
                var items = drv.Row.ItemArray;
                if (addServicesDictionary.ContainsKey(items[1].ToString()))
                {
                    MessageBox.Show("Не удалось добавить услугу. Обнаружен дубликат", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    DataRow dr = dtAddServices.NewRow();
                    dr.ItemArray = new object[] { items[1].ToString(), items[2] };
                    dtAddServices.Rows.Add(dr);
                    DataRowView addedToOrderDg = dtAddServices.DefaultView[dtAddServices.Rows.IndexOf(dr)];
                    addServicesDictionary.Add(items[1].ToString(), addedToOrderDg);
                    orderAddServiceDG.Items.Add(addedToOrderDg);
                    double cost = addServicesTotalCostLabel.Content.ToString() != string.Empty ? Convert.ToDouble(addServicesTotalCostLabel.Content) : 0;
                    cost += Convert.ToDouble(items[2]);
                    addServicesTotalCostLabel.Content = cost;
                }
            }
        }

        private void FillAdditionalServicesDG(string searchWord)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"SELECT * FROM `additional_services` where `additional_service_name` LIKE '%{searchWord}%';;", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    cmd.ExecuteNonQuery();
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    additionalServicesDG.ItemsSource = dt.AsDataView();
                }

            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить дополнительные услуги\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            FillAdditionalServicesDG("");
        }
    }
}
