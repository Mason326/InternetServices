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
using MySql.Data.MySqlClient;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for ExportImportForm.xaml
    /// </summary>
    public partial class ExportImportForm : Window
    {
        public ExportImportForm()
        {
            InitializeComponent();
        }

        private void tablesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                switch (tablesComboBox.SelectedItem)
                {
                    case "- -":
                        datagrid.ItemsSource = null;
                        break;
                    default:
                        FillDatagrid(tablesComboBox.SelectedItem.ToString());
                        break;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить колонки таблицы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillDatagrid(string columnName)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    List<string> headers = new List<string>();
                    MySqlCommand cmd = new MySqlCommand($"show columns from `{columnName}`;", conn);
                    DataTable dt = new DataTable();
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            dt.Columns.Add(dr.GetValue(0).ToString());
                        }
                    }
                    datagrid.ItemsSource = dt.AsDataView();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить таблицы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                List<string> tables = new List<string>();
                tables.Add("- -");
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand("SHOW TABLES;", conn);
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            tables.Add(dr.GetValue(0).ToString());
                        }
                    }
                }
                tablesComboBox.ItemsSource = tables;
                tablesComboBox.SelectedItem = "- -";
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить таблицы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
