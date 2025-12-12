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
    /// Interaction logic for Company.xaml
    /// </summary>
    public partial class Company : Window
    {
        public Company()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            companyNameTextBox.Text = Properties.Settings.Default.companyName;
            companyDirectorTextBox.Text = Properties.Settings.Default.companyDirector;
            companyDescriptionTextBox.Text = Properties.Settings.Default.companyDescription;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (companyNameTextBox.Text.Length > 0 && companyDescriptionTextBox.Text.Length > 0 && companyDirectorTextBox.Text.Length > 0)
            {
                Properties.Settings.Default.companyName = companyNameTextBox.Text;
                Properties.Settings.Default.companyDirector = companyDirectorTextBox.Text;
                Properties.Settings.Default.companyDescription = companyDescriptionTextBox.Text;
                Properties.Settings.Default.Save();
                MessageBox.Show($"Изменения сохранены", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Необходимо заполнить поля помеченные \"*\"", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
