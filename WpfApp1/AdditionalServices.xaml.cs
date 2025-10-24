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
    /// Interaction logic for Window10.xaml
    /// </summary>
    public partial class AdditionalServices : Window
    {
        public AdditionalServices()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshDataGrid();
        }

        private void serviceTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            { 
                Regex regex = new Regex(@"[0-9A-Za-zА-Яа-я-()\b\s]");
                if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                    e.Handled = false;
                else
                    e.Handled = true;
            }
            catch
            {
                ;
            }
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            { 
                Regex regex = new Regex(@"[0-9,\b]");
                if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                    e.Handled = false;
                else
                    e.Handled = true;
            }
            catch
            {
                ;
            }
        }

        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
            if (e.Key == Key.OemComma)
            {
                if (monthFee.Text.Length > 0)
                {
                    if (monthFee.Text.Count(c => c == ',') > 0)
                        e.Handled = true;
                    else
                        e.Handled = false;
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled = serviceTextBox.Text.Length > 0 && monthFee.Text.Length > 0;


            if (requiredFieldsIsFilled)
            {
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        MySqlCommand cmd = new MySqlCommand($@"Insert into additional_services(additional_service_name, monthly_fee) 
                                                            value(
                                                                '{serviceTextBox.Text}',
                                                                 {monthFee.Text.Replace(',', '.')}
                                                            );", conn);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Услуга создана", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearInputData();
                    }

                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось создать услугу\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                RefreshDataGrid();
            }
            else
                MessageBox.Show("Все поля помеченные \"*\" обязательны для заполнения", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void ClearInputData()
        {
            serviceTextBox.Text = "";
            monthFee.Text = "";
        }

        private void RefreshDataGrid()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(@"SELECT * FROM additional_services order by idadditional_service desc;", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);
                    addServicesDG.ItemsSource = dt.AsDataView();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
