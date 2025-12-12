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
    /// Interaction logic for Window13.xaml
    /// </summary>
    public partial class Materials : Window
    {
        int materialId = -1;
        public Materials()
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
            editMaterialButton.IsEnabled = false;
            deleteMaterialButton.IsEnabled = false;
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            { 
                Regex regex = new Regex(@"[0-9A-Za-zА-Яа-я-.«»()\b\s]");
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
                Regex regex = new Regex(@"[А-Яа-я\b\s]");
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
                if (materialCostTextBox.Text.Length > 0)
                {
                    if (materialCostTextBox.Text.Count(c => c == ',') > 0)
                        e.Handled = true;
                    else
                        e.Handled = false;
                }
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled = materialNameTextBox.Text.Length > 0 && materialUnitTextBox.Text.Length > 0 && materialCostTextBox.Text.Length > 0;


            if (requiredFieldsIsFilled)
            {
                if (!CheckDuplicateUtil.HasNoDuplicate("materials", "material_name", materialNameTextBox.Text))
                {
                    MessageBox.Show($"Не удалось добавить материал. Обнаружен дубликат наименования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        MySqlCommand cmd = new MySqlCommand($@"Insert into `materials`(material_name, units, cost) 
                                                            value(
                                                                '{materialNameTextBox.Text}',
                                                                '{materialUnitTextBox.Text}',
                                                                 {materialCostTextBox.Text.Replace(',', '.')}
                                                            );", conn);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Материал добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearInputData();
                    }

                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось добавить Материал\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MySqlCommand cmd = new MySqlCommand("Select * from `materials` order by idmaterials desc", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);
                    materialsDG.ItemsSource = dt.AsDataView();
                    countRecordsLabel.Content = RecordsCounter.CountRecords("materials");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearInputData()
        {
            materialNameTextBox.Text = "";
            materialUnitTextBox.Text = "";
            materialCostTextBox.Text = "";
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
            if (materialsDG.SelectedItem != null)
            {
                DataRowView drv = materialsDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;

                addMaterialButton.Visibility = Visibility.Collapsed;
                editMaterialButton.Visibility = Visibility.Collapsed;
                deleteMaterialButton.Visibility = Visibility.Collapsed;
                toMainButton.Visibility = Visibility.Collapsed;

                materialId = Convert.ToInt32(fieldValuesOfARecord[0]);
                materialNameTextBox.Text = fieldValuesOfARecord[1].ToString().Trim();
                materialUnitTextBox.Text = fieldValuesOfARecord[2].ToString().Trim();
                materialCostTextBox.Text = fieldValuesOfARecord[3].ToString().Trim();

                materialsDG.IsEnabled = false;

                endEditButton.Visibility = Visibility.Visible;
                cancelEditButton.Visibility = Visibility.Visible;
            }
        }

        private void CloseEdition()
        {
            addMaterialButton.Visibility = Visibility.Visible;
            editMaterialButton.Visibility = Visibility.Visible;
            deleteMaterialButton.Visibility = Visibility.Visible;
            toMainButton.Visibility = Visibility.Visible;

            endEditButton.Visibility = Visibility.Collapsed;
            cancelEditButton.Visibility = Visibility.Collapsed;

            ClearInputData();

            materialsDG.SelectedItem = null;
            materialId = -1;
            editMaterialButton.IsEnabled = false;
            deleteMaterialButton.IsEnabled = false;
            materialsDG.IsEnabled = true;
        }

        private void endEditButton_Click(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled;
            try
            {
                requiredFieldsIsFilled = materialNameTextBox.Text.Length > 0
                    && materialUnitTextBox.Text.Length > 0
                    && materialCostTextBox.Text.Length > 0;
            }
            catch
            {
                MessageBox.Show("Некорректно заполнены обязательные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (requiredFieldsIsFilled)
            {
                int duplicateMaterialName = CheckDuplicateUtil.HasNoDuplicate("materials", "material_name", materialNameTextBox.Text, false);

                if (duplicateMaterialName != materialId && duplicateMaterialName != -1)
                {
                    MessageBox.Show($"Не удалось обновить данные материала. Обнаружен дубликат наименования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            string query = $@"Update `materials` 
                                                set material_name = '{materialNameTextBox.Text.Trim()}',
                                                cost = '{materialCostTextBox.Text.Trim()}',
                                                units = '{materialUnitTextBox.Text.Trim()}'
                                                where idmaterials = {materialId}";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Данные материала успешно обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            CloseEdition();
                            RefreshDataGrid();
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show($"Не удалось обновить данные материала\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void editMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            PrepareToEdit();
        }

        private void materialsDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            editMaterialButton.IsEnabled = true;
            deleteMaterialButton.IsEnabled = true;
        }

        private void deleteMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show($"Вы уверены, что хотите удалить материал?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes)
                return;

            if (materialsDG.SelectedItem != null)
            {
                DataRowView drv = materialsDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            string query = $@"Delete from `materials`
                                                where idmaterials = {fieldValuesOfARecord[0]}";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Материал успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            RefreshDataGrid();
                            materialsDG.SelectedItem = null;
                            editMaterialButton.IsEnabled = false;
                            deleteMaterialButton.IsEnabled = false;
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show($"Не удалось удалить материал\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
