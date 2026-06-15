using System;
using System.Collections.Generic;
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
using MySql.Data.MySqlClient;
using WpfApp1.Utils;

namespace WpfApp1
{
    /// <summary>
    /// Главная форма администратора - центральное окно для управления справочной информацией
    /// Предоставляет доступ к разделам: доп. услуги, услуги, тарифы, материалы, организация, пользователи
    /// </summary>
    public partial class AdministratorMain : Window
    {
        /// <summary>
        /// Конструктор формы - инициализация компонентов
        /// </summary>
        public AdministratorMain()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка "Дополнительные услуги" - открытие формы управления дополнительными услугами
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new AdditionalServices();
            win.ShowDialog();
            this.ShowDialog();
        }

        /// <summary>
        /// Кнопка "Услуги" - открытие формы управления основными услугами
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new Services();
            win.ShowDialog();
            this.ShowDialog();
        }

        /// <summary>
        /// Кнопка "Тарифы" - открытие формы управления тарифами
        /// </summary>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new Tariff();
            win.ShowDialog();
            this.ShowDialog();
        }

        /// <summary>
        /// Кнопка "Материалы" - открытие формы управления складом материалов
        /// </summary>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new Materials();
            win.ShowDialog();
            this.ShowDialog();
        }

        /// <summary>
        /// Кнопка "Организация" - открытие формы с информацией о компании
        /// </summary>
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new Company();
            win.ShowDialog();
            this.ShowDialog();
        }

        /// <summary>
        /// Кнопка "Пользователи" - открытие формы создания и управления пользователями
        /// </summary>
        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new CreateUser();
            win.ShowDialog();
            this.ShowDialog();
        }

        /// <summary>
        /// Кнопка "Выход" - выход из учетной записи с подтверждением
        /// </summary>
        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            MessageBoxResult resDialog = MessageBox.Show("Вы действительно хотите выйти из учётной записи?", "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resDialog == MessageBoxResult.Yes)
                this.Close();
        }

        /// <summary>
        /// Событие загрузки формы - отображение роли пользователя и загрузка аватара
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
                // Загрузка и отображение изображения пользователя (аватар)
                ImageUtils.LoadUserImage(userImage);
            }
            catch
            {
                MessageBox.Show("Не удалось загрузить картинку", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Событие закрытия окна - отмена закрытия (запрет на закрытие формы стандартным способом)
        /// Пользователь должен выходить только через кнопку "Выход"
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        /// <summary>
        /// Адаптация интерфейса при изменении размера окна
        /// Изменяется размер шрифта кнопок для лучшей читаемости
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double windowHeight = e.NewSize.Height;
            double baseFontSize = 14;

            // Увеличение шрифта при высоте окна более 650 пикселей
            if (windowHeight > 650)
            {
                double scale = windowHeight / 550;
                int newFontSize = (int)(baseFontSize * Math.Min(scale, 1.5));
                UpdateButtonsFontSize(newFontSize);
            }
            // Уменьшение шрифта при высоте окна менее 480 пикселей
            else if (windowHeight < 480)
            {
                double scale = windowHeight / 550;
                int newFontSize = (int)Math.Max(baseFontSize * scale, 10);
                UpdateButtonsFontSize(newFontSize);
            }
            // Стандартный размер шрифта
            else
            {
                UpdateButtonsFontSize((int)baseFontSize);
            }
        }

        /// <summary>
        /// Обновление размера шрифта для всех кнопок главного меню администратора
        /// </summary>
        private void UpdateButtonsFontSize(int fontSize)
        {
            var buttons = new[] { AdditionalServicesButton, ServicesButton, TariffsButton,
                          MaterialsButton, UsersButton, OrganizationButton, LogoutButton };
            foreach (var button in buttons)
            {
                if (button != null)
                    button.FontSize = fontSize;
            }
        }
    }
}