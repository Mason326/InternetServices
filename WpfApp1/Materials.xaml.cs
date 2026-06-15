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
    /// Форма "Материалы" - управление справочником материалов для заказ-нарядов
    /// Позволяет:
    /// - Добавлять новые материалы (наименование, единицы измерения, стоимость)
    /// - Редактировать существующие материалы
    /// - Удалять материалы (если они не используются в заказ-нарядах)
    /// </summary>
    public partial class Materials : Window
    {
        // ID редактируемого материала (-1 означает, что материал не выбран или создается новый)
        int materialId = -1;

        /// <summary>
        /// Конструктор формы - инициализация компонентов
        /// </summary>
        public Materials()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка "На главную" - закрытие формы
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Событие загрузки формы - отображение роли пользователя,
        /// загрузка списка материалов, блокировка кнопок редактирования/удаления
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Отображение роли и сокращенного ФИО в заголовке окна
                this.Title += $" ({AccountHolder.UserRole}: {FullNameSplitter.MakeShortName(AccountHolder.FIO)})";
            }
            catch
            {
                ;
            }

            RefreshDataGrid(true);           // Загрузка материалов (сортировка по наименованию)
            editMaterialButton.IsEnabled = false;   // Кнопка редактирования неактивна до выбора
            deleteMaterialButton.IsEnabled = false; // Кнопка удаления неактивна до выбора
        }

        /// <summary>
        /// Валидация ввода наименования материала - разрешены:
        /// Цифры, буквы (рус/англ), дефис, точка, скобки, пробел, Backspace
        /// </summary>
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

        /// <summary>
        /// Валидация ввода единиц измерения - только русские буквы, пробел, Backspace
        /// (например: "шт", "м", "кг", "упак" и т.д.)
        /// </summary>
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

        /// <summary>
        /// Валидация ввода стоимости - разрешены цифры, запятая (десятичный разделитель), Backspace
        /// Автоматически ограничивает ввод до 2 знаков после запятой
        /// </summary>
        private void TextBox_PreviewTextInput_2(object sender, TextCompositionEventArgs e)
        {
            try
            {
                Regex regex = new Regex(@"[0-9,\b]");
                if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                {
                    e.Handled = false;
                    // Ограничение: не более 2 знаков после запятой
                    int commaIndex = materialCostTextBox.Text.IndexOf(',');
                    if (commaIndex != -1)
                    {
                        int costLength = materialCostTextBox.Text.Length;
                        materialCostTextBox.Text = materialCostTextBox.Text.Substring(0, commaIndex + 2);
                        materialCostTextBox.CaretIndex = materialCostTextBox.Text.Length;
                    }
                }
                else
                    e.Handled = true;
            }
            catch
            {
                ;
            }
        }

        /// <summary>
        /// Обработка нажатий клавиш при вводе стоимости
        /// Запрещает пробел, ограничивает ввод только одной запятой
        /// </summary>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;

            if (e.Key == Key.OemComma)  // Клавиша запятой
            {
                if (materialCostTextBox.Text.Length > 0)
                {
                    // Проверка: если запятая уже есть, запрещаем ввод еще одной
                    if (materialCostTextBox.Text.Count(c => c == ',') > 0)
                        e.Handled = true;
                    else
                        e.Handled = false;
                }
            }
        }

        /// <summary>
        /// Кнопка "Добавить материал" - создание нового материала в БД
        /// Проверяет заполнение обязательных полей и отсутствие дубликатов
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled = materialNameTextBox.Text.Length > 0
                && materialUnitTextBox.Text.Length > 0
                && materialCostTextBox.Text.Length > 0;

            if (requiredFieldsIsFilled)
            {
                // Проверка на дубликат наименования материала
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
                        // SQL-запрос на вставку нового материала
                        // Замена запятой на точку для корректного сохранения десятичной дроби
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
                RefreshDataGrid(false);  // Обновление таблицы (сортировка по ID - новые сверху)
            }
            else
                MessageBox.Show("Все поля помеченные \"*\" обязательны для заполнения", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Обновление DataGrid со списком материалов
        /// </summary>
        /// <param name="isInitial">true - сортировка по наименованию (при загрузке),
        /// false - сортировка по ID (новые сверху)</param>
        private void RefreshDataGrid(bool isInitial)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    string cmdText = "Select idmaterials, material_name, units, cost from `materials` order by idmaterials desc";
                    if (isInitial)
                    {
                        // При начальной загрузке - сортировка по алфавиту
                        cmdText = "Select idmaterials, material_name, units, cost from `materials` order by material_name";
                    }
                    MySqlCommand cmd = new MySqlCommand(cmdText, conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);
                    materialsDG.ItemsSource = dt.AsDataView();
                    // Отображение общего количества материалов
                    countRecordsLabel.Content = RecordsCounter.CountRecords("materials");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Очистка полей ввода
        /// </summary>
        private void ClearInputData()
        {
            materialNameTextBox.Text = "";
            materialUnitTextBox.Text = "";
            materialCostTextBox.Text = "";
        }

        /// <summary>
        /// Запрет вставки текста из буфера обмена в поля ввода
        /// </summary>
        private void TextBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Подготовка к редактированию материала
        /// Заполняет поля формы данными выбранного материала
        /// </summary>
        private void PrepareToEdit()
        {
            if (materialsDG.SelectedItem != null)
            {
                DataRowView drv = materialsDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;

                // Переключение UI в режим редактирования
                addMaterialButton.Visibility = Visibility.Collapsed;
                editMaterialButton.Visibility = Visibility.Collapsed;
                deleteMaterialButton.Visibility = Visibility.Collapsed;
                toMainButton.Visibility = Visibility.Collapsed;

                // Запись ID и данных выбранного материала
                materialId = Convert.ToInt32(fieldValuesOfARecord[0]);
                materialNameTextBox.Text = fieldValuesOfARecord[1].ToString().Trim();
                materialUnitTextBox.Text = fieldValuesOfARecord[2].ToString().Trim();
                materialCostTextBox.Text = fieldValuesOfARecord[3].ToString().Trim();

                // Блокировка таблицы на время редактирования
                materialsDG.IsEnabled = false;

                // Показ кнопок режима редактирования
                endEditButton.Visibility = Visibility.Visible;
                cancelEditButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Выход из режима редактирования, возврат в стандартный режим
        /// </summary>
        private void CloseEdition()
        {
            // Возврат кнопок режима просмотра
            addMaterialButton.Visibility = Visibility.Visible;
            editMaterialButton.Visibility = Visibility.Visible;
            deleteMaterialButton.Visibility = Visibility.Visible;
            toMainButton.Visibility = Visibility.Visible;

            // Скрытие кнопок режима редактирования
            endEditButton.Visibility = Visibility.Collapsed;
            cancelEditButton.Visibility = Visibility.Collapsed;

            ClearInputData();

            materialsDG.SelectedItem = null;
            materialId = -1;
            editMaterialButton.IsEnabled = false;
            deleteMaterialButton.IsEnabled = false;
            materialsDG.IsEnabled = true;
        }

        /// <summary>
        /// Кнопка "Завершить редактирование" - сохранение изменений материала
        /// </summary>
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
                // Проверка на дубликат наименования (исключая текущую запись)
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
                            // SQL-запрос на обновление данных материала
                            string query = $@"Update `materials` 
                                                set material_name = '{materialNameTextBox.Text.Trim()}',
                                                cost = '{materialCostTextBox.Text.Trim().Replace(',', '.')}',
                                                units = '{materialUnitTextBox.Text.Trim()}'
                                                where idmaterials = {materialId}";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Данные материала успешно обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            CloseEdition();          // Возврат в режим просмотра
                            RefreshDataGrid(false); // Обновление таблицы
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

        /// <summary>
        /// Кнопка "Отмена редактирования" - выход без сохранения изменений
        /// </summary>
        private void cancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            CloseEdition();
        }

        /// <summary>
        /// Кнопка "Редактировать материал" - переход в режим редактирования
        /// </summary>
        private void editMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            PrepareToEdit();
        }

        /// <summary>
        /// При выборе строки в DataGrid активируются кнопки редактирования и удаления
        /// </summary>
        private void materialsDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            editMaterialButton.IsEnabled = true;
            deleteMaterialButton.IsEnabled = true;
        }

        /// <summary>
        /// Кнопка "Удалить материал" - удаление выбранного материала из базы данных
        /// Предварительно запрашивает подтверждение у пользователя
        /// </summary>
        private void deleteMaterialButton_Click(object sender, RoutedEventArgs e)
        {
            // Подтверждение удаления
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
                            // SQL-запрос на удаление материала
                            string query = $@"Delete from `materials`
                                                where idmaterials = {fieldValuesOfARecord[0]}";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Материал успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            RefreshDataGrid(false);  // Обновление таблицы
                            materialsDG.SelectedItem = null;
                            editMaterialButton.IsEnabled = false;
                            deleteMaterialButton.IsEnabled = false;
                        }
                        catch
                        {
                            // Ошибка удаления - материал используется в заказ-нарядах (внешний ключ)
                            MessageBox.Show($"Не удалось удалить материал\nОшибка: Материал используется в заказ-нарядах", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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