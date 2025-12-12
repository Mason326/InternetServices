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
    /// Interaction logic for Window25.xaml
    /// </summary>
    public partial class MasterMain : Window
    {
        public MasterMain()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var win = new AccountingClaim();
            win.ShowDialog();
            this.ShowDialog();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            MessageBoxResult resDialog = MessageBox.Show("Вы действительно хотите выйти из учётной записи?", "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resDialog == MessageBoxResult.Yes)
                this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            userName.Content = FullNameSplitter.MakeShortName(AccountHolder.FIO);
        }
    }
}
