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
    /// Interaction logic for Window23.xaml
    /// </summary>
    public partial class AdministratorMain : Window
    {
        public AdministratorMain()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var win = new AdditionalServices();
            win.ShowDialog();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var win = new Services();
            win.ShowDialog();
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            var win = new Tariff();
            win.ShowDialog();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var win = new Materials();
            win.ShowDialog();

        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            var win = new Company();
            win.ShowDialog();
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            var win = new Roles();
            win.ShowDialog();
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            var win = new CreateUser();
            win.ShowDialog();
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            userName.Content = FullNameSplitter.MakeShortName(AccountHolder.FIO);
        }
    }
}
