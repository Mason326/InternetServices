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
    /// Interaction logic for ExportAs.xaml
    /// </summary>
    public partial class ExportAs : Window
    {
        public ExportAs()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ExportHolder.exportOptions = ExportHolder.ExportOptions.ExportWord;
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ExportHolder.exportOptions = ExportHolder.ExportOptions.ExportPdf;
            this.Close();
        }
    }
}
