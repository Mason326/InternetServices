using System;
using System.Collections.Generic;
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

namespace WpfApp1
{
    /// <summary>
    /// Форма "Организация" - просмотр и редактирование информации о компании
    /// Данные сохраняются в локальных настройках приложения (Properties.Settings)
    /// Используется для отображения информации о компании в документах и отчетах
    /// </summary>
    public partial class Company : Window
    {
        /// <summary>
        /// Конструктор формы - инициализация компонентов
        /// </summary>
        public Company()
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
        /// Событие загрузки формы - отображение роли пользователя в заголовке
        /// и загрузка сохраненных данных о компании из настроек приложения
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

            // Загрузка данных о компании из сохраненных настроек
            companyNameTextBox.Text = Properties.Settings.Default.companyName;            // Название организации
            companyDirectorTextBox.Text = Properties.Settings.Default.companyDirector;    // ФИО директора
            companyDescriptionTextBox.Text = Properties.Settings.Default.companyDescription; // Описание/реквизиты компании
        }

        /// <summary>
        /// Кнопка "Сохранить изменения" - сохранение введенных данных о компании
        /// Проверяет заполнение всех обязательных полей (отмеченных *)
        /// Сохраняет данные в локальные настройки приложения
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            // Проверка заполнения всех обязательных полей
            if (companyNameTextBox.Text.Length > 0 && companyDescriptionTextBox.Text.Length > 0 && companyDirectorTextBox.Text.Length > 0)
            {
                // Сохранение данных в настройки
                Properties.Settings.Default.companyName = companyNameTextBox.Text;
                Properties.Settings.Default.companyDirector = companyDirectorTextBox.Text;
                Properties.Settings.Default.companyDescription = companyDescriptionTextBox.Text;
                Properties.Settings.Default.Save();  // Сохранение настроек на диск

                MessageBox.Show($"Изменения сохранены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Необходимо заполнить поля помеченные \"*\"", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}