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
using Microsoft.Win32;
using System.IO;
using WpfApp1.Utils;

namespace WpfApp1
{
    /// <summary>
    /// Форма "Экспорт/Импорт данных" - управление обменом данными с CSV-файлами
    /// Позволяет:
    /// - Экспортировать данные из любой таблицы БД в CSV-файл
    /// - Импортировать данные из CSV-файла в любую таблицу БД
    /// - Настраивать разделитель полей (; , | :)
    /// - Пропускать заголовок при импорте
    /// </summary>
    public partial class ExportImportForm : Window
    {
        // Флаг режима: true - экспорт, false - импорт
        bool isExport = false;

        /// <summary>
        /// Конструктор формы - настройка интерфейса в зависимости от типа операции
        /// </summary>
        /// <param name="type">Тип операции: Export или Import</param>
        public ExportImportForm(DataManagement.DataOperationType type)
        {
            InitializeComponent();

            // Настройка видимости кнопок в зависимости от режима
            switch (type)
            {
                case DataManagement.DataOperationType.Export:
                    importButton.Visibility = Visibility.Collapsed;   // Скрыть кнопку импорта
                    exportButton.Visibility = Visibility.Visible;     // Показать кнопку экспорта
                    skipHeaderButton.Visibility = Visibility.Collapsed; // Скрыть чекбокс пропуска заголовка
                    isExport = true;
                    break;
                case DataManagement.DataOperationType.Import:
                    importButton.Visibility = Visibility.Visible;     // Показать кнопку импорта
                    exportButton.Visibility = Visibility.Collapsed;   // Скрыть кнопку экспорта
                    skipHeaderButton.Visibility = Visibility.Visible; // Показать чекбокс пропуска заголовка
                    isExport = false;
                    break;
            }
        }

        /// <summary>
        /// При изменении выбранной таблицы - загрузка структуры ее колонок
        /// </summary>
        private void tablesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                switch (tablesComboBox.SelectedItem)
                {
                    case "- -":
                        datagrid.ItemsSource = null;  // Очистка DataGrid
                        break;
                    default:
                        FillDatagrid(tablesComboBox.SelectedItem.ToString());  // Загрузка структуры таблицы
                        break;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить колонки таблицы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Заполнение DataGrid списком колонок выбранной таблицы
        /// </summary>
        /// <param name="columnName">Имя таблицы</param>
        private void FillDatagrid(string columnName)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    List<string> headers = new List<string>();
                    // SQL-запрос для получения информации о колонках таблицы
                    MySqlCommand cmd = new MySqlCommand($"show columns from `{columnName}`;", conn);
                    DataTable dt = new DataTable();
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            // Добавление имени колонки как заголовка в DataTable
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

        /// <summary>
        /// Событие загрузки формы - инициализация списка таблиц и разделителей
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Настройка списка разделителей полей
                fieldTerminatorComboBox.ItemsSource = new List<string>() { ";", ",", "|", ":" };
                fieldTerminatorComboBox.SelectedItem = ";";  // Разделитель по умолчанию - точка с запятой

                // Загрузка списка всех таблиц из БД
                List<string> tables = new List<string>();
                tables.Add("- -");  // Пустой элемент-разделитель
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
                tablesComboBox.SelectedItem = "- -";  // По умолчанию ничего не выбрано
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить таблицы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Кнопка "Импорт" - загрузка данных из CSV-файла в выбранную таблицу
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tablesComboBox.SelectedItem != null && filePathTextBox.Text != string.Empty)
                {
                    using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                    {
                        conn.Open();

                        // Настройка загрузчика данных
                        MySqlBulkLoader loader = new MySqlBulkLoader(conn);
                        loader.Local = true;  // Файл находится на клиентской машине
                        loader.TableName = tablesComboBox.SelectedItem.ToString();  // Целевая таблица
                        loader.FileName = filePathTextBox.Text;  // Путь к CSV-файлу
                        loader.FieldTerminator = fieldTerminatorComboBox.SelectedItem.ToString();  // Разделитель полей
                        loader.LineTerminator = "\n";  // Разделитель строк

                        // Пропуск заголовка (первой строки) если выбран соответствующий чекбокс
                        if (skipHeaderButton.IsChecked != null && skipHeaderButton.IsChecked.Value)
                        {
                            loader.NumberOfLinesToSkip = 1;
                        }
                        else
                        {
                            loader.NumberOfLinesToSkip = 0;
                        }

                        int uploadedRows = loader.Load();  // Выполнение загрузки
                        MessageBox.Show($"Загружено строк: {uploadedRows}", "Процесс", MessageBoxButton.OK, MessageBoxImage.Information);
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

        /// <summary>
        /// Кнопка "Экспорт" - выгрузка данных из выбранной таблицы в CSV-файл
        /// </summary>
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

                        // Получение всех данных из таблицы
                        MySqlCommand cmd = new MySqlCommand($"SELECT * FROM `{tableName}`", conn);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            using (StreamWriter writer = new StreamWriter(filePathTextBox.Text, false, Encoding.UTF8))
                            {
                                writer.NewLine = "\n";

                                // Запись заголовков колонок
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    writer.Write($"{reader.GetName(i)}");
                                    if (i < reader.FieldCount - 1) writer.Write(fieldTerminator);
                                }
                                writer.WriteLine();

                                // Запись данных построчно
                                while (reader.Read())
                                {
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        string value;

                                        // Обработка NULL значений
                                        if (reader.IsDBNull(i))
                                        {
                                            value = "";
                                        }
                                        // Форматирование дат в стандартный формат
                                        else if (reader.GetFieldType(i) == typeof(DateTime))
                                        {
                                            value = DateTime.Parse(reader.GetValue(i).ToString()).ToString("yyyy-MM-dd HH:mm");
                                        }
                                        // Пропуск бинарных данных (фото и т.п.)
                                        else if (reader.GetFieldType(i) == typeof(byte[]))
                                        {
                                            value = "";
                                            continue;
                                        }
                                        else
                                        {
                                            value = reader.GetValue(i).ToString();
                                        }

                                        // Удаление символов переноса строки из данных
                                        writer.Write($"{value}".Replace("\r", ""));

                                        if (i < reader.FieldCount - 1) writer.Write(fieldTerminator);
                                    }
                                    writer.WriteLine();
                                }
                            }
                        }
                    }

                    MessageBox.Show($"Файл сохранён: {filePathTextBox.Text}", "Процесс", MessageBoxButton.OK, MessageBoxImage.Information);
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

        /// <summary>
        /// Кнопка "Обзор" - выбор файла для импорта или экспорта
        /// </summary>
        private void browseFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isExport)
                {
                    // Режим экспорта - выбор места сохранения
                    SaveFileDialog save = new SaveFileDialog();
                    save.Filter = "CSV-файлы (*.csv)|*.csv";
                    save.Title = "Выберите файл для импорта";

                    save.ShowDialog();

                    if (save.FileName != string.Empty)
                    {
                        filePathTextBox.Text = save.FileName;
                    }
                }
                else
                {
                    // Режим импорта - выбор файла для загрузки
                    OpenFileDialog dialog = new OpenFileDialog();
                    dialog.Filter = "CSV-файлы (*.csv)|*.csv";
                    dialog.Title = "Выберите файл для экспорта";
                    dialog.ShowDialog();

                    if (dialog.FileName != string.Empty)
                    {
                        filePathTextBox.Text = dialog.FileName;
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить файл\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Кнопка "На главную" - закрытие формы
        /// </summary>
        private void toMenuButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}