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
    /// Форма "Тарифы" - управление справочником тарифов на подключение
    /// Позволяет:
    /// - Добавлять новые тарифы (название, описание, абонентская плата)
    /// - Редактировать существующие тарифы
    /// - Удалять тарифы (если они не используются в заявках)
    /// </summary>
    public partial class Tariff : Window
    {
        // ID редактируемого тарифа (-1 означает, что тариф не выбран или создается новый)
        int tariffId = -1;

        /// <summary>
        /// Конструктор формы - инициализация компонентов
        /// </summary>
        public Tariff()
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
        /// загрузка списка тарифов, блокировка кнопок редактирования/удаления
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

            RefreshDataGrid(true);              // Загрузка тарифов (сортировка по названию)
            editTariffButton.IsEnabled = false; // Кнопка редактирования неактивна до выбора
            deleteTariffButton.IsEnabled = false; // Кнопка удаления неактивна до выбора
        }

        /// <summary>
        /// Валидация ввода названия тарифа - разрешены:
        /// Цифры, буквы (рус/англ), точка, запятая, пробел, Backspace
        /// </summary>
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

        /// <summary>
        /// Валидация ввода описания тарифа - разрешены:
        /// Цифры, буквы (рус/англ), знаки +/-, запятая, пробел, Backspace
        /// </summary>
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

        /// <summary>
        /// Валидация ввода абонентской платы - разрешены цифры, запятая (десятичный разделитель), Backspace
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
                    int commaIndex = monthFeeTextBox.Text.IndexOf(',');
                    if (commaIndex != -1)
                    {
                        int costLength = monthFeeTextBox.Text.Length;
                        monthFeeTextBox.Text = monthFeeTextBox.Text.Substring(0, commaIndex + 2);
                        monthFeeTextBox.CaretIndex = monthFeeTextBox.Text.Length;
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

            if (e.Key == Key.OemComma)  // Клавиша запятой
            {
                if (monthFeeTextBox.Text.Length > 0)
                {
                    // Проверка: если запятая уже есть, запрещаем ввод еще одной
                    if (monthFeeTextBox.Text.Count(c => c == ',') > 0)
                        e.Handled = true;
                    else
                        e.Handled = false;
                }
            }
        }

        /// <summary>
        /// Кнопка "Добавить тариф" - создание нового тарифа в БД
        /// Проверяет заполнение обязательных полей и отсутствие дубликатов
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            bool requiredFieldsIsFilled = tariffNameTextBox.Text.Length > 0
                && tariffDescriptionTextBox.Text.Length > 0
                && monthFeeTextBox.Text.Length > 0;

            if (requiredFieldsIsFilled)
            {
                // Проверка на дубликат наименования тарифа
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
                        // SQL-запрос на вставку нового тарифа
                        // Замена запятой на точку для корректного сохранения десятичной дроби
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
                RefreshDataGrid(false);  // Обновление таблицы (сортировка по ID - новые сверху)
            }
            else
                MessageBox.Show("Все поля помеченные \"*\" обязательны для заполнения", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Обновление DataGrid со списком тарифов
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
                    string cmdText = "Select idtariff, monthly_fee, tariff_name, tariff_details from `tariff` order by idtariff desc";
                    if (isInitial)
                    {
                        // При начальной загрузке - сортировка по алфавиту
                        cmdText = "Select idtariff, monthly_fee, tariff_name, tariff_details from `tariff` order by tariff_name";
                    }
                    MySqlCommand cmd = new MySqlCommand(cmdText, conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    cmd.ExecuteNonQuery();
                    da.Fill(dt);
                    tariffDG.ItemsSource = dt.AsDataView();
                    // Отображение общего количества тарифов
                    countRecordsLabel.Content = RecordsCounter.CountRecords("tariff");
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
            tariffNameTextBox.Text = "";
            tariffDescriptionTextBox.Text = "";
            monthFeeTextBox.Text = "";
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
        /// Подготовка к редактированию тарифа
        /// Заполняет поля формы данными выбранного тарифа
        /// </summary>
        private void PrepareToEdit()
        {
            if (tariffDG.SelectedItem != null)
            {
                DataRowView drv = tariffDG.SelectedItem as DataRowView;
                object[] fieldValuesOfARecord = drv.Row.ItemArray;

                // Переключение UI в режим редактирования
                addTariffButton.Visibility = Visibility.Collapsed;
                editTariffButton.Visibility = Visibility.Collapsed;
                deleteTariffButton.Visibility = Visibility.Collapsed;
                toMainButton.Visibility = Visibility.Collapsed;

                // Запись ID и данных выбранного тарифа
                tariffId = Convert.ToInt32(fieldValuesOfARecord[0]);
                tariffNameTextBox.Text = fieldValuesOfARecord[2].ToString().Trim();
                tariffDescriptionTextBox.Text = fieldValuesOfARecord[3].ToString().Trim();
                monthFeeTextBox.Text = fieldValuesOfARecord[1].ToString().Trim();

                // Блокировка таблицы на время редактирования
                tariffDG.IsEnabled = false;

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
            addTariffButton.Visibility = Visibility.Visible;
            editTariffButton.Visibility = Visibility.Visible;
            deleteTariffButton.Visibility = Visibility.Visible;
            toMainButton.Visibility = Visibility.Visible;

            // Скрытие кнопок режима редактирования
            endEditButton.Visibility = Visibility.Collapsed;
            cancelEditButton.Visibility = Visibility.Collapsed;

            ClearInputData();

            tariffDG.SelectedItem = null;
            tariffId = -1;
            editTariffButton.IsEnabled = false;
            deleteTariffButton.IsEnabled = false;
            tariffDG.IsEnabled = true;
        }

        /// <summary>
        /// Кнопка "Завершить редактирование" - сохранение изменений тарифа
        /// </summary>
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
                // Проверка на дубликат наименования (исключая текущую запись)
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
                            // SQL-запрос на обновление данных тарифа
                            string query = $@"Update `tariff` 
                                                set tariff_name = '{tariffNameTextBox.Text.Trim()}',
                                                monthly_fee = {monthFeeTextBox.Text.Trim().Replace(',', '.')},
                                                tariff_details = '{tariffDescriptionTextBox.Text.Trim()}'
                                                where idtariff = {tariffId}";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Данные тарифа успешно обновлены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            CloseEdition();          // Возврат в режим просмотра
                            RefreshDataGrid(false); // Обновление таблицы
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

        /// <summary>
        /// Кнопка "Отмена редактирования" - выход без сохранения изменений
        /// </summary>
        private void cancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            CloseEdition();
        }

        /// <summary>
        /// Кнопка "Редактировать тариф" - переход в режим редактирования
        /// </summary>
        private void editTariffButton_Click(object sender, RoutedEventArgs e)
        {
            PrepareToEdit();
        }

        /// <summary>
        /// При выборе строки в DataGrid активируются кнопки редактирования и удаления
        /// </summary>
        private void tariffDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            editTariffButton.IsEnabled = true;
            deleteTariffButton.IsEnabled = true;
        }

        /// <summary>
        /// Кнопка "Удалить тариф" - удаление выбранного тарифа из базы данных
        /// Предварительно запрашивает подтверждение у пользователя
        /// </summary>
        private void deleteTariffButton_Click(object sender, RoutedEventArgs e)
        {
            // Подтверждение удаления
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
                            // SQL-запрос на удаление тарифа
                            string query = $@"Delete from `tariff`
                                                where idtariff = {fieldValuesOfARecord[0]}";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.ExecuteNonQuery();
                            MessageBox.Show($"Тариф успешно удален", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                            RefreshDataGrid(false);  // Обновление таблицы
                            tariffDG.SelectedItem = null;
                            editTariffButton.IsEnabled = false;
                            deleteTariffButton.IsEnabled = false;
                        }
                        catch
                        {
                            // Ошибка удаления - тариф используется в заявках (внешний ключ)
                            MessageBox.Show($"Не удалось удалить тариф\nОшибка: Тариф используется в заявках", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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