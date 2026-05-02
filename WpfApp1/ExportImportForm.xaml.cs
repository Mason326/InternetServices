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
using Vit.Db.Util.Data;
using MySql.Data.MySqlClient;
using Microsoft.Win32;
using System.IO;
using WpfApp1.Utils;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for ExportImportForm.xaml
    /// </summary>
    public partial class ExportImportForm : Window
    {
        public ExportImportForm(DataManagement.DataOperationType type)
        {
            InitializeComponent();
            switch (type)
            {
                case DataManagement.DataOperationType.Export:
                    importButton.Visibility = Visibility.Collapsed;
                    exportButton.Visibility = Visibility.Visible;
                    skipHeaderButton.Visibility = Visibility.Collapsed;
                    break;
                case DataManagement.DataOperationType.Import:
                    importButton.Visibility = Visibility.Visible;
                    exportButton.Visibility = Visibility.Collapsed;
                    skipHeaderButton.Visibility = Visibility.Visible;
                    break;
            }
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
                fieldTerminatorComboBox.ItemsSource = new List<string>() { ";", ",", "|", ":" };
                fieldTerminatorComboBox.SelectedItem = ";";

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tablesComboBox.SelectedItem != null && filePathTextBox.Text != string.Empty)
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();

                        MySqlBulkLoader loader = new MySqlBulkLoader(conn);
                        loader.Local = true;
                        loader.TableName = tablesComboBox.SelectedItem.ToString();
                        loader.FileName = filePathTextBox.Text;
                        loader.FieldTerminator = fieldTerminatorComboBox.SelectedItem.ToString();
                        loader.LineTerminator = "\n";
                        if (skipHeaderButton.IsChecked != null && skipHeaderButton.IsChecked.Value)
                        {
                            loader.NumberOfLinesToSkip = 1;
                        }
                        else
                        {
                            loader.NumberOfLinesToSkip = 0;
                        }

                        int uploadedRows = loader.Load();
                        MessageBox.Show($"Загружено строк: {uploadedRows}");
                    }
                }
                else
                {
                    MessageBox.Show($"Заполните поля помеченные *", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить данные\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void exportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tablesComboBox.SelectedItem != null && filePathTextBox.Text != string.Empty)
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        string tableName = tablesComboBox.SelectedItem.ToString();
                        string fieldTerminator = fieldTerminatorComboBox.SelectedItem.ToString();

                        MySqlCommand cmd = new MySqlCommand($"SELECT * FROM `{tableName}`", conn);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            using (StreamWriter writer = new StreamWriter("D:\\test.csv", false, Encoding.UTF8))
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    writer.Write($"{reader.GetName(i)}");
                                    if (i < reader.FieldCount - 1) writer.Write(fieldTerminator);
                                }
                                writer.WriteLine();

                                while (reader.Read())
                                {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        string value;

                                        if (reader.IsDBNull(i))
                                        {
                                            value = "";
                                        }
                                        else if (reader.GetFieldType(i) == typeof(DateTime))
                                        {
                                            value = DateTime.Parse(reader.GetValue(i).ToString()).ToString("yyyy-MM-dd HH:mm");
                                        }
                                        else if (reader.GetFieldType(i) == typeof(byte[]))
                                        {
                                            byte[] blobData = (byte[])reader.GetValue(i);
                                            value = $"0x{BitConverter.ToString(blobData).Replace("-", "")}";
                                        }
                                        else
                                        {
                                            value = reader.GetValue(i).ToString();
                                        }
    ;
                                        writer.Write($"{value}");

                                        if (i < reader.FieldCount - 1) writer.Write(fieldTerminator);
                                    }
                                    writer.WriteLine();
                                }
                            }
                        }
                    }

                    MessageBox.Show($"Файл сохранён: D:\\test.csv ");
                }
                else
                {
                    MessageBox.Show($"Заполните поля помеченные *", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить таблицы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void browseFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "CSV-файлы (*.csv)|*.csv";
                dialog.Title = "Выберите файл";
                dialog.ShowDialog();

                if (dialog.FileName != string.Empty)
                {
                    filePathTextBox.Text = dialog.FileName;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить файл\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void toMenuButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
