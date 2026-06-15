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
    /// Форма "Дополнительные услуги" - управление справочником дополнительных услуг
    /// Позволяет добавлять, редактировать, удалять услуги и их абонентскую плату
    /// </summary>
    public partial class AdditionalServices : Window
    {
        // ID выбранной услуги для редактирования (-1 означает, что услуга не выбрана)
        int serviceId = -1;

        /// <summary>
        /// Конструктор формы - инициализация компонентов
        /// </summary>
        public AdditionalServices()
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
            // Загрузка данных (начальная сортировка по названию)
            RefreshDataGrid(true);
            // Кнопки редактирования и удаления неактивны до выбора услуги
            editServiceButton.IsEnabled = false;
            deleteServiceButton.IsEnabled = false;
        }

        /// <summary>
        /// Валидация ввода названия услуги - разрешены буквы (рус/англ), цифры, дефис, скобки, пробел, Backspace
        /// </summary>
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

        /// <summary>
        /// Валидация ввода абонентской платы - разрешены только цифры и запятая (десятичный разделитель)
        /// Автоматически ограничивает ввод до 2 знаков после запятой
        /// </summary>
        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                Regex regex = new Regex(@"[0-9,\b]");
                if (regex.IsMatch(e.Text[e.Text.Length - 1].ToString()))
                {
                    e.Handled = false;
                    // Ограничение: не более 2 знаков после запятой
                    int commaIndex = monthFee.Text.IndexOf(',');
                    if (commaIndex != -1)
                    {
                        int costLength = monthFee.Text.Length;
                        monthFee.Text = monthFee.Text.Substring(0, commaIndex + 2);
                        monthFee.CaretIndex = monthFee.Text.Length;
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
        /// Обработка нажатий клавиш при вводе абонентской платы
        /// Запрещает пробел, ограничивает ввод только одной запятой
        /// </summary>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
            if (e.Key == Key.OemComma)
            {
                if (monthFee.Text.Length > 0)
                {
                    // Проверка: если запятая уже есть, запрещаем ввод еще одной
                    if (monthFee.Text.Count(c => c == ',') > 0)
                        e.Handled = true;
                    else
                        e.Handled = false;
                }
            }
        }

        /// <summary>
        /// Кнопка "Создать услугу" - добавление новой услуги в базу данных
        /// Проверяет заполнение обязательных полей и отсутствие дубликатов
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled = serviceTextBox.Text.Length > 0 && monthFee.Text.Length > 0;

            if (requiredFieldsIsFilled)
            {
                // Проверка на дубликат названия услуги
                if (!CheckDuplicateUtil.HasNoDuplicate("additional_services", "additional_service_name", serviceTextBox.Text))
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
                        MySqlCommand cmd = new MySqlCommand($@"Insert into additional_services(additional_service_name, monthly_fee) 
                                                            value(
                                                                '{serviceTextBox.Text}',
                                                                 {monthFee.Text.Replace(',', '.')}
                                                            );", conn);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Услуга создана", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        ClearInputData();  // Очистка полей ввода
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось создать услугу\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                // Обновление таблицы (без начальной сортировки)
                RefreshDataGrid(false);
            }
            else
                MessageBox.Show("Все поля помеченные \"*\" обязательны для заполнения", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Очистка полей ввода названия и абонентской платы
        /// </summary>
        private void ClearInputData()
        {
            serviceTextBox.Text = "";
            monthFee.Text = "";
        }

        /// <summary>
        /// Обновление DataGrid со списком услуг
        /// </summary>
        /// <param name="isInitial">true - сортировка по названию (при загрузке),
        /// false - сортировка по ID (новые сверху)</param>
        private void RefreshDataGrid(bool isInitial)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    string cmdText = "SELECT idadditional_service, additional_service_name, monthly_fee FROM additional_services order by idadditional_service desc;";
                    if (isInitial)
                    {
                        // При начальной загрузке - сортировка по алфавиту
                        cmdText = "SELECT idadditional_service, additional_service_name, monthly_fee FROM additional_services order by additional_service_name;";
                    }

                    MySqlCommand cmd = new MySqlCommand(cmdText, conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);
                    addServicesDG.ItemsSource = dt.AsDataView();
                    // Отображение общего количества услуг
                    countRecordsLabel.Content = RecordsCounter.CountRecords("additional_services");
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Ошибка подключения\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        /// Кнопка "Завершить редактирование" - сохранение изменений услуги
        /// </summary>
        private void endEditButton_Click(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled;
            try
            {
                requiredFieldsIsFilled = serviceTextBox.Text.Length > 0
                    && monthFee.Text.Length > 0;
            }
            catch
            {
                MessageBox.Show("Некорректно заполнены обязательные поля", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (requiredFieldsIsFilled)
            {
                // Проверка на дубликат названия (исключая текущую запись)
                int duplicateNameService = CheckDuplicateUtil.HasNoDuplicate("additional_services", "additional_service_name", serviceTextBox.Text, false);

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
                            string query = $@"Update `additional_services` 
                                                set additional_service_name = '{serviceTextBox.Text.Trim()}',
                                                monthly_fee = '{monthFee.Text.Trim().Replace(',', '.')}'
                                                where idadditional_service = {serviceId}";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Данные услуги успешно обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            CloseEdition();      // Возврат в режим просмотра
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
        /// Подготовка к редактированию услуги - заполнение полей выбранными данными
        /// Скрывает кнопки режима просмотра, показывает кнопки режима редактирования
        /// </summary>
        private void PrepareToEdit()
        {
            if (addServicesDG.SelectedItem != null)
            {
                DataRowView drv = addServicesDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;

                // Скрытие кнопок режима просмотра
                addServiceButton.Visibility = Visibility.Collapsed;
                editServiceButton.Visibility = Visibility.Collapsed;
                deleteServiceButton.Visibility = Visibility.Collapsed;
                toMainButton.Visibility = Visibility.Collapsed;

                // Запись ID и данных выбранной услуги
                serviceId = Convert.ToInt32(fieldValuesOfARecord[0]);
                serviceTextBox.Text = fieldValuesOfARecord[1].ToString().Trim();
                monthFee.Text = fieldValuesOfARecord[2].ToString().Trim();

                // Блокировка таблицы на время редактирования
                addServicesDG.IsEnabled = false;

                // Показ кнопок режима редактирования
                endEditButton.Visibility = Visibility.Visible;
                cancelEditButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Выход из режима редактирования - возврат к стандартному режиму
        /// Показывает скрытые кнопки, очищает поля, разблокирует таблицу
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

            addServicesDG.SelectedItem = null;
            serviceId = -1;
            editServiceButton.IsEnabled = false;
            deleteServiceButton.IsEnabled = false;
            addServicesDG.IsEnabled = true;
        }

        /// <summary>
        /// Кнопка "Отмена редактирования" - выход без сохранения изменений
        /// </summary>
        private void cancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            CloseEdition();
        }

        /// <summary>
        /// Кнопка "Редактировать услугу" - переход в режим редактирования
        /// </summary>
        private void editServiceButton_Click(object sender, RoutedEventArgs e)
        {
            PrepareToEdit();
        }

        /// <summary>
        /// При выборе строки в DataGrid активируются кнопки редактирования и удаления
        /// </summary>
        private void addServicesDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

            if (addServicesDG.SelectedItem != null)
            {
                DataRowView drv = addServicesDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;
                try
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();
                        try
                        {
                            // SQL-запрос на удаление услуги
                            string query = $@"Delete from `additional_services`
                                                where idadditional_service = {fieldValuesOfARecord[0]}";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Данные услуги успешно удалены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            RefreshDataGrid(false);  // Обновление таблицы
                            addServicesDG.SelectedItem = null;
                            editServiceButton.IsEnabled = false;
                            deleteServiceButton.IsEnabled = false;
                        }
                        catch
                        {
                            // Ошибка удаления - скорее всего услуга используется в заказах (внешний ключ)
                            MessageBox.Show($"Не удалось удалить услугу\nОшибка: Услуга используется в заказах", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

        /// <summary>
        /// Адаптация интерфейса при изменении размера окна
        /// Изменяется размер шрифта кнопок и DataGrid
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double windowHeight = e.NewSize.Height;
            double baseFontSize = 14;

            if (windowHeight > 700)
            {
                double scale = windowHeight / 600;
                int newFontSize = (int)(baseFontSize * Math.Min(scale, 1.5));
                UpdateButtonsFontSize(newFontSize);
            }
            else if (windowHeight < 550)
            {
                double scale = windowHeight / 600;
                int newFontSize = (int)Math.Max(baseFontSize * scale, 10);
                UpdateButtonsFontSize(newFontSize);
            }
            else
            {
                UpdateButtonsFontSize((int)baseFontSize);
            }
        }

        /// <summary>
        /// Обновление размера шрифта для кнопок и DataGrid
        /// </summary>
        private void UpdateButtonsFontSize(int fontSize)
        {
            var buttons = new[] { addServiceButton, editServiceButton, deleteServiceButton,
                          toMainButton, endEditButton, cancelEditButton };
            foreach (var button in buttons)
            {
                if (button != null)
                    button.FontSize = fontSize;
            }

            if (addServicesDG != null)
                addServicesDG.FontSize = fontSize;
        }
    }
}