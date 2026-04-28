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
    /// Interaction logic for Window22.xaml
    /// </summary>
    public partial class ManagerMain : Window
    {
        public ManagerMain()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new CreateClaim();
            win.ShowDialog();
            this.ShowDialog();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new AccountingClaim();
            win.ShowDialog();
            this.ShowDialog();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new CreateClient(false);
            win.ShowDialog();
            this.ShowDialog();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new AccountingContract();
            win.ShowDialog();
            this.ShowDialog();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            MessageBoxResult resDialog = MessageBox.Show("Вы действительно хотите выйти из учётной записи?", "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resDialog == MessageBoxResult.Yes)
                this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Title += $" ({AccountHolder.UserRole}: {FullNameSplitter.MakeShortName(AccountHolder.FIO)})";
            }
            catch
            {
                ;
            }
            try
            {
                ImageUtils.LoadUserImage(userImage);
            }
            catch
            {
                MessageBox.Show("Не удалось загрузить картинку", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Адаптивный размер шрифта для кнопок в зависимости от высоты окна
            double windowHeight = e.NewSize.Height;
            double baseFontSize = 16;

            if (windowHeight > 600)
            {
                // При большом окне увеличиваем шрифт
                double scale = windowHeight / 500; // 500 - базовая высота
                int newFontSize = (int)(baseFontSize * Math.Min(scale, 1.5)); // максимум x1.5
                UpdateButtonsFontSize(newFontSize);
            }
            else if (windowHeight < 450)
            {
                // При маленьком окне уменьшаем шрифт
                double scale = windowHeight / 500;
                int newFontSize = (int)Math.Max(baseFontSize * scale, 10); // минимум 10
                UpdateButtonsFontSize(newFontSize);
            }
            else
            {
                UpdateButtonsFontSize(Convert.ToInt32(baseFontSize));
            }
        }

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
