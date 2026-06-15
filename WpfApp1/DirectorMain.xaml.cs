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
using WpfApp1.Utils;

namespace WpfApp1
{
    /// <summary>
    /// Главная форма директора - центральное окно для управления деятельностью компании
    /// Предоставляет доступ к основным разделам: договоры и заявки
    /// Директор имеет полный доступ ко всем функциям системы
    /// </summary>
    public partial class DirectorMain : Window
    {
        /// <summary>
        /// Конструктор формы - инициализация компонентов
        /// </summary>
        public DirectorMain()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка "Договоры" - открытие формы учета договоров
        /// Директор может просматривать и управлять всеми договорами
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new AccountingContract();
            win.ShowDialog();
            this.ShowDialog();
        }

        /// <summary>
        /// Кнопка "Заявки" - открытие формы учета заявок
        /// Директор может просматривать и управлять всеми заявками
        /// </summary>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new AccountingClaim();
            win.ShowDialog();
            this.ShowDialog();
        }

        /// <summary>
        /// Кнопка "Выход" - выход из учетной записи с подтверждением
        /// </summary>
        private void Button_Click_3(object sender, RoutedEventArgs e)
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
        /// Адаптация интерфейса при изменении размера окна
        /// Изменяется размер шрифта кнопок для лучшей читаемости
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double windowHeight = e.NewSize.Height;
            double baseFontSize = 14;

            // Увеличение шрифта при высоте окна более 600 пикселей
            if (windowHeight > 600)
            {
                double scale = windowHeight / 500;
                int newFontSize = (int)(baseFontSize * Math.Min(scale, 1.5));
                UpdateButtonsFontSize(newFontSize);
            }
            // Уменьшение шрифта при высоте окна менее 450 пикселей
            else if (windowHeight < 450)
            {
                double scale = windowHeight / 500;
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
        /// Обновление размера шрифта для всех кнопок главного меню директора
        /// </summary>
        /// <param name="fontSize">Новый размер шрифта в пикселях</param>
        private void UpdateButtonsFontSize(int fontSize)
        {
            var buttons = new[] { ClaimsButton, ContractsButton, LogoutButton };
            foreach (var button in buttons)
            {
                if (button != null)
                    button.FontSize = fontSize;
            }
        }
    }
}