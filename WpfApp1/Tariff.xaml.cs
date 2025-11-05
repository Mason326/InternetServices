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
using MySql.Data.MySqlClient;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Window12.xaml
    /// </summary>
    public partial class Tariff : Window
    {
        int tariffId = -1;
        public Tariff()
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
            editTariffButton.IsEnabled = false;
            deleteTariffButton.IsEnabled = false;
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            { 
                Regex regex = new Regex(@"[0-9A-Za-zА-Яа-я.,\b\s]");
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

        private void TextBox_PreviewTextInput_1(object sender, TextCompositionEventArgs e)
        {
            try
            { 
                Regex regex = new Regex(@"[+/0-9A-Za-zА-Яа-я,\b\s]");
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

        private void TextBox_PreviewTextInput_2(object sender, TextCompositionEventArgs e)
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
                if (monthFeeTextBox.Text.Length > 0)
                {
                    if (monthFeeTextBox.Text.Count(c => c == ',') > 0)
                        e.Handled = true;
                    else
                        e.Handled = false;
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled = tariffNameTextBox.Text.Length > 0 && tariffDescriptionTextBox.Text.Length > 0 && monthFeeTextBox.Text.Length > 0;


            if (requiredFieldsIsFilled)
            {
                if (!CheckDuplicateUtil.HasNoDuplicate("tariff", "tariff_name", tariffNameTextBox.Text))
                {
                    MessageBox.Show($"Не удалось добавить тариф. Обнаружен дубликат наименования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        MySqlCommand cmd = new MySqlCommand($@"Insert into `tariff`(tariff_name, tariff_details, monthly_fee) 
                                                            value(
                                                                '{tariffNameTextBox.Text}',
                                                                '{tariffDescriptionTextBox.Text}',
                                                                 {monthFeeTextBox.Text.Replace(',', '.')}
                                                            );", conn);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Тариф добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearInputData();
                    }

                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось добавить тариф\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                RefreshDataGrid();
            }
            else
                MessageBox.Show("Все поля помеченные \"*\" обязательны для заполнения", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);

        }

        private void RefreshDataGrid()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand("Select * from `tariff` order by idtariff desc", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);
                    tariffDG.ItemsSource = dt.AsDataView();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearInputData()
        {
            tariffNameTextBox.Text = "";
            tariffDescriptionTextBox.Text = "";
            monthFeeTextBox.Text = "";
        }

        private void TextBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        private void PrepareToEdit()
        {
            if (tariffDG.SelectedItem != null)
            {
                DataRowView drv = tariffDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;

                addTariffButton.Visibility = Visibility.Collapsed;
                editTariffButton.Visibility = Visibility.Collapsed;
                deleteTariffButton.Visibility = Visibility.Collapsed;
                toMainButton.Visibility = Visibility.Collapsed;

                tariffId = Convert.ToInt32(fieldValuesOfARecord[0]);
                tariffNameTextBox.Text = fieldValuesOfARecord[2].ToString().Trim();
                tariffDescriptionTextBox.Text = fieldValuesOfARecord[3].ToString().Trim();
                monthFeeTextBox.Text = fieldValuesOfARecord[1].ToString().Trim();

                tariffDG.IsEnabled = false;

                endEditButton.Visibility = Visibility.Visible;
                cancelEditButton.Visibility = Visibility.Visible;
            }
        }

        private void CloseEdition()
        {
            addTariffButton.Visibility = Visibility.Visible;
            editTariffButton.Visibility = Visibility.Visible;
            deleteTariffButton.Visibility = Visibility.Visible;
            toMainButton.Visibility = Visibility.Visible;

            endEditButton.Visibility = Visibility.Collapsed;
            cancelEditButton.Visibility = Visibility.Collapsed;

            ClearInputData();

            tariffDG.SelectedItem = null;
            tariffId = -1;
            editTariffButton.IsEnabled = false;
            deleteTariffButton.IsEnabled = false;
            tariffDG.IsEnabled = true;
        }

        private void endEditButton_Click(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled;
            try
            {
                requiredFieldsIsFilled = tariffNameTextBox.Text.Length > 0
                    && tariffDescriptionTextBox.Text.Length > 0
                    && monthFeeTextBox.Text.Length > 0;
            }
            catch
            {
                MessageBox.Show("Некорректно заполнены обязательные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (requiredFieldsIsFilled)
            {
                int duplicateNameService = CheckDuplicateUtil.HasNoDuplicate("tariff", "tariff_name", tariffNameTextBox.Text, false);

                if (duplicateNameService != tariffId && duplicateNameService != -1)
                {
                    MessageBox.Show($"Не удалось обновить данные тарифа. Обнаружен дубликат наименования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            string query = $@"Update `tariff` 
                                                set tariff_name = '{tariffNameTextBox.Text.Trim()}',
                                                monthly_fee = {monthFeeTextBox.Text.Trim()},
                                                tariff_details = '{tariffDescriptionTextBox.Text.Trim()}'
                                                where idtariff = {tariffId}";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Данные тарифа успешно обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            CloseEdition();
                            RefreshDataGrid();
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show($"Не удалось обновить данные тарифа\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось установить подключение\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show($"Необходимо заполнить поля помеченные \"*\"", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            CloseEdition();
        }

        private void editTariffButton_Click(object sender, RoutedEventArgs e)
        {
            PrepareToEdit();
        }

        private void tariffDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            editTariffButton.IsEnabled = true;
            deleteTariffButton.IsEnabled = true;
        }

        private void deleteTariffButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show($"Вы уверены, что хотите удалить тариф?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes)
                return;

            if (tariffDG.SelectedItem != null)
            {
                DataRowView drv = tariffDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            string query = $@"Delete from `tariff`
                                                where idtariff = {fieldValuesOfARecord[0]}";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Тариф успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            RefreshDataGrid();
                            tariffDG.SelectedItem = null;
                            editTariffButton.IsEnabled = false;
                            deleteTariffButton.IsEnabled = false;
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show($"Не удалось удалить тариф\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось установить подключение\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
