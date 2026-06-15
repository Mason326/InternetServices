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
    /// Форма "Услуги" - управление справочником услуг
    /// Позволяет:
    /// - Добавлять новые услуги (наименование, единицы измерения, стоимость)
    /// - Редактировать существующие услуги
    /// - Удалять услуги (если они не используются в заказ-нарядах)
    /// </summary>
    public partial class Services : Window
    {
        // ID редактируемой услуги (-1 означает, что услуга не выбрана или создается новая)
        int serviceId = -1;

        /// <summary>
        /// Конструктор формы - инициализация компонентов
        /// </summary>
        public Services()
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
        /// загрузка списка услуг, блокировка кнопок редактирования/удаления
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

            RefreshDataGrid(true);           // Загрузка услуг (сортировка по наименованию)
            editServiceButton.IsEnabled = false;   // Кнопка редактирования неактивна до выбора
            deleteServiceButton.IsEnabled = false; // Кнопка удаления неактивна до выбора
        }

        /// <summary>
        /// Валидация ввода наименования услуги - разрешены:
        /// Цифры, буквы (рус/англ), дефис, запятая, пробел, Backspace
        /// </summary>
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                Regex regex = new Regex(@"[/0-9A-Za-zА-Яа-я-,\b\s]");
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
        private void cost_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                Regex regex = new Regex(@"[0-9,\b]");
                if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                {
                    e.Handled = false;
                    // Ограничение: не более 2 знаков после запятой
                    int commaIndex = costTextBox.Text.IndexOf(',');
                    if (commaIndex != -1)
                    {
                        int costLength = costTextBox.Text.Length;
                        costTextBox.Text = costTextBox.Text.Substring(0, commaIndex + 2);
                        costTextBox.CaretIndex = costTextBox.Text.Length;
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
        private void cost_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;

            if (e.Key == Key.OemComma)  // Клавиша запятой
            {
                if (costTextBox.Text.Length > 0)
                {
                    // Проверка: если запятая уже есть, запрещаем ввод еще одной
                    if (costTextBox.Text.Count(c => c == ',') > 0)
                        e.Handled = true;
                    else
                        e.Handled = false;
                }
            }
        }

        /// <summary>
        /// Валидация ввода единиц измерения - только русские буквы, пробел, Backspace
        /// (например: "шт", "час", "компл", "мес" и т.д.)
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
        /// Обновление DataGrid со списком услуг
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
                    string cmdText = "Select idservice, service_name, units, service_cost from `services` order by idservice desc";
                    if (isInitial)
                    {
                        // При начальной загрузке - сортировка по алфавиту
                        cmdText = "Select idservice, service_name, units, service_cost from `services` order by service_name";
                    }

                    MySqlCommand cmd = new MySqlCommand(cmdText, conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);
                    servicesDG.ItemsSource = dt.AsDataView();
                    // Отображение общего количества услуг
                    countRecordsLabel.Content = RecordsCounter.CountRecords("services");
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
            serviceTextBox.Text = "";
            costTextBox.Text = "";
            unitsTextBox.Text = "";
        }

        /// <summary>
        /// Кнопка "Создать услугу" - добавление новой услуги в БД
        /// Проверяет заполнение обязательных полей и отсутствие дубликатов
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled = serviceTextBox.Text.Length > 0 && costTextBox.Text.Length > 0 && unitsTextBox.Text.Length > 0;

            if (requiredFieldsIsFilled)
            {
                // Проверка на дубликат наименования услуги
                if (!CheckDuplicateUtil.HasNoDuplicate("services", "service_name", serviceTextBox.Text))
                {
                    MessageBox.Show($"Не удалось добавить услугу. Обнаружен дубликат наименования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        // SQL-запрос на вставку новой услуги
                        // Замена запятой на точку для корректного сохранения десятичной дроби
                        MySqlCommand cmd = new MySqlCommand($@"Insert into `services`(service_name, units, service_cost) 
                                                            value(
                                                                '{serviceTextBox.Text}',
                                                                '{unitsTextBox.Text}',
                                                                 {costTextBox.Text.Replace(',', '.')}
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
                RefreshDataGrid(false);  // Обновление таблицы (сортировка по ID - новые сверху)
            }
            else
                MessageBox.Show("Все поля помеченные \"*\" обязательны для заполнения", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
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
        /// Подготовка к редактированию услуги
        /// Заполняет поля формы данными выбранной услуги
        /// </summary>
        private void PrepareToEdit()
        {
            if (servicesDG.SelectedItem != null)
            {
                DataRowView drv = servicesDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;

                // Переключение UI в режим редактирования
                addServiceButton.Visibility = Visibility.Collapsed;
                editServiceButton.Visibility = Visibility.Collapsed;
                deleteServiceButton.Visibility = Visibility.Collapsed;
                toMainButton.Visibility = Visibility.Collapsed;

                // Запись ID и данных выбранной услуги
                serviceId = Convert.ToInt32(fieldValuesOfARecord[0]);
                serviceTextBox.Text = fieldValuesOfARecord[1].ToString().Trim();
                unitsTextBox.Text = fieldValuesOfARecord[2].ToString().Trim();
                costTextBox.Text = fieldValuesOfARecord[3].ToString().Trim();

                // Блокировка таблицы на время редактирования
                servicesDG.IsEnabled = false;

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
            addServiceButton.Visibility = Visibility.Visible;
            editServiceButton.Visibility = Visibility.Visible;
            deleteServiceButton.Visibility = Visibility.Visible;
            toMainButton.Visibility = Visibility.Visible;

            // Скрытие кнопок режима редактирования
            endEditButton.Visibility = Visibility.Collapsed;
            cancelEditButton.Visibility = Visibility.Collapsed;

            ClearInputData();

            servicesDG.SelectedItem = null;
            serviceId = -1;
            editServiceButton.IsEnabled = false;
            deleteServiceButton.IsEnabled = false;
            servicesDG.IsEnabled = true;
        }

        /// <summary>
        /// Кнопка "Редактировать услугу" - переход в режим редактирования
        /// </summary>
        private void editServiceButton_Click(object sender, RoutedEventArgs e)
        {
            PrepareToEdit();
        }

        /// <summary>
        /// Кнопка "Отмена редактирования" - выход без сохранения изменений
        /// </summary>
        private void cancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            CloseEdition();
        }

        /// <summary>
        /// Кнопка "Завершить редактирование" - сохранение изменений услуги
        /// </summary>
        private void endEditButton_Click(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled;
            try
            {
                requiredFieldsIsFilled = serviceTextBox.Text.Length > 0
                    && costTextBox.Text.Length > 0
                    && unitsTextBox.Text.Length > 0;
            }
            catch
            {
                MessageBox.Show("Некорректно заполнены обязательные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (requiredFieldsIsFilled)
            {
                // Проверка на дубликат наименования (исключая текущую запись)
                int duplicateNameService = CheckDuplicateUtil.HasNoDuplicate("services", "service_name", serviceTextBox.Text, false);

                if (duplicateNameService != serviceId && duplicateNameService != -1)
                {
                    MessageBox.Show($"Не удалось обновить данные услуги. Обнаружен дубликат наименования", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            // SQL-запрос на обновление данных услуги
                            string query = $@"Update `services` 
                                                set service_name = '{serviceTextBox.Text.Trim()}',
                                                service_cost = '{costTextBox.Text.Trim().Replace(',', '.')}',
                                                units = '{unitsTextBox.Text.Trim()}'
                                                where idservice = {serviceId}";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Данные услуги успешно обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            CloseEdition();          // Возврат в режим просмотра
                            RefreshDataGrid(false); // Обновление таблицы
                        }
                        catch (Exception exc)
                        {
                            MessageBox.Show($"Не удалось обновить данные услуги\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
        /// При выборе строки в DataGrid активируются кнопки редактирования и удаления
        /// </summary>
        private void servicesDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            editServiceButton.IsEnabled = true;
            deleteServiceButton.IsEnabled = true;
        }

        /// <summary>
        /// Кнопка "Удалить услугу" - удаление выбранной услуги из базы данных
        /// Предварительно запрашивает подтверждение у пользователя
        /// </summary>
        private void deleteServiceButton_Click(object sender, RoutedEventArgs e)
        {
            // Подтверждение удаления
            MessageBoxResult res = MessageBox.Show($"Вы уверены, что хотите удалить эту услугу?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes)
                return;

            if (servicesDG.SelectedItem != null)
            {
                DataRowView drv = servicesDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            // SQL-запрос на удаление услуги
                            string query = $@"Delete from `services`
                                                where idservice = {fieldValuesOfARecord[0]}";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Данные услуги успешно удалены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            RefreshDataGrid(false);  // Обновление таблицы
                            servicesDG.SelectedItem = null;
                            editServiceButton.IsEnabled = false;
                            deleteServiceButton.IsEnabled = false;
                        }
                        catch
                        {
                            // Ошибка удаления - услуга используется в заказ-нарядах (внешний ключ)
                            MessageBox.Show($"Не удалось удалить услугу\nОшибка: Услуга используется в заказ-нарядах", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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