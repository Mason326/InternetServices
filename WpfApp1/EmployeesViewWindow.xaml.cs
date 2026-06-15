using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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
using WpfApp1.Utils;

namespace WpfApp1
{
    /// <summary>
    /// Форма "Просмотр сотрудников" - окно для выбора мастера при создании/редактировании заявки
    /// Отображает список всех сотрудников с ролью "Мастер" с их фотографиями
    /// Позволяет выбрать мастера для назначения на заявку
    /// </summary>
    public partial class EmployeesViewWindow : Window
    {
        /// <summary>
        /// Конструктор формы - инициализация компонентов
        /// </summary>
        public EmployeesViewWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Событие загрузки формы - отображение роли пользователя и загрузка списка мастеров
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

            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    // Запрос на получение всех сотрудников с ролью "Мастер"
                    MySqlCommand cmd = new MySqlCommand($@"SELECT idemployees, full_name, login, password, roles_id, phoneNumber, photo FROM employees where roles_id = (select idroles from `roles` where role_name = 'Мастер');", conn);
                    DataTable dt = new DataTable();

                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        // Создание структуры таблицы на основе метаданных результата запроса
                        DataColumn[] columns = new DataColumn[dr.FieldCount];
                        for (int i = 0; i < columns.Length; i++)
                        {
                            columns[i] = new DataColumn(dr.GetName(i), dr.GetFieldType(i));
                        }
                        dt.Columns.AddRange(columns);

                        // Добавление дополнительной колонки для отображения фотографии пользователя
                        BitmapImage image = new BitmapImage();
                        Type type = image.GetType();
                        dt.Columns.Add("UserPhoto", type);

                        object[] record = new object[dr.FieldCount + 1];
                        while (dr.Read())
                        {
                            dr.GetValues(record);
                            // Преобразование массива байтов (фото) в BitmapImage для отображения
                            byte[] imageBytes = record[6] as byte[];
                            record[7] = ImageUtils.LoadImage(imageBytes);
                            dt.LoadDataRow(record, true);
                        }
                    }

                    employeesDG.ItemsSource = dt.AsDataView();  // Привязка данных к DataGrid
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить сотрудников\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            inClaimButton.IsEnabled = false;  // Кнопка выбора мастера неактивна до выбора сотрудника
        }

        /// <summary>
        /// Кнопка "На главную" - закрытие формы без выбора мастера
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Кнопка "Выбрать" - сохранение выбранного мастера и закрытие формы
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (employeesDG.SelectedItem != null)
            {
                DataRowView drv = employeesDG.SelectedItem as DataRowView;
                MasterHolder.data = drv.Row.ItemArray;  // Сохранение данных о мастере в статическом хранилище
                this.Close();
            }
        }

        /// <summary>
        /// При выборе строки в DataGrid активируется кнопка выбора мастера
        /// </summary>
        private void employeesDG_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            inClaimButton.IsEnabled = true;
        }
    }
}