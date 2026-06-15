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
    /// Главная форма менеджера - центральное окно для управления деятельностью
    /// Предоставляет доступ к основным разделам работы менеджера:
    /// - Создание заявок
    /// - Просмотр и управление заявками
    /// - Управление клиентами
    /// - Управление договорами
    /// </summary>
    public partial class ManagerMain : Window
    {
        /// <summary>
        /// Конструктор формы - инициализация компонентов
        /// </summary>
        public ManagerMain()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Кнопка "Создать заявку" - открытие формы создания новой заявки
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var win = new CreateClaim();
            win.ShowDialog();
        }

        /// <summary>
        /// Кнопка "Заявки" - открытие формы учета и управления заявками
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new AccountingClaim();
            win.ShowDialog();
            this.ShowDialog();
        }

        /// <summary>
        /// Кнопка "Клиенты" - открытие формы управления справочником клиентов
        /// Параметр false означает режим управления клиентами (не выбор для заявки)
        /// </summary>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new CreateClient(false);
            win.ShowDialog();
            if (Auth.locker)
                this.ShowDialog();
        }

        /// <summary>
        /// Кнопка "Договоры" - открытие формы учета и управления договорами
        /// </summary>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new AccountingContract();
            win.ShowDialog();
            this.ShowDialog();
        }

        /// <summary>
        /// Кнопка "Выход" - выход из учетной записи с подтверждением
        /// </summary>
        private void Button_Click_4(object sender, RoutedEventArgs e)
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
        /// Базовый размер шрифта для менеджера: 16 пикселей
        /// </summary>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double windowHeight = e.NewSize.Height;
            double baseFontSize = 16;  // Базовый размер шрифта для менеджера больше, чем для других ролей

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
                UpdateButtonsFontSize(Convert.ToInt32(baseFontSize));
            }
        }

        /// <summary>
        /// Обновление размера шрифта для всех кнопок главного меню менеджера
        /// </summary>
        /// <param name="fontSize">Новый размер шрифта в пикселях</param>
        private void UpdateButtonsFontSize(int fontSize)
        {
            var buttons = new[] { CreateRequestButton, RequestsButton, ClientsButton, ContractsButton, LogoutButton };
            foreach (var button in buttons)
            {
                if (button != null)
                    button.FontSize = fontSize;
            }
        }
    }
}