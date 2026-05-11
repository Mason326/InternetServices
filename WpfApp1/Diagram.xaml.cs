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
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;
using MySql.Data.MySqlClient;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Diagram.xaml
    /// </summary>
    public partial class Diagram : Window
    {

        public Diagram()
        {
            InitializeComponent();
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
            double incoming = 0;
            double canceled = 0;
            double inProgress = 0;
            double closed = 0;
            GetStatusCount(ref incoming, ref canceled, ref inProgress, ref closed);

            double[] ys1 = new double[] { incoming };
            double[] ys2 = new double[] { canceled };
            double[] ys3 = new double[] { inProgress };
            double[] ys4 = new double[] { closed };

            var seriesList = new[]
            {
        new { Name = "Входящие", Color = System.Drawing.Color.FromArgb(76, 175, 80), Values = ys1 },
        new { Name = "Отмененные", Color = System.Drawing.Color.FromArgb(244, 67, 54), Values = ys2 },
        new { Name = "В работе", Color = System.Drawing.Color.FromArgb(255, 152, 0), Values = ys3 },
        new { Name = "Закрытые", Color = System.Drawing.Color.FromArgb(156, 39, 176), Values = ys4 }
    };

            chart.Series.Clear();

            foreach (var item in seriesList)
            {
                Series series = new Series(item.Name);
                series.Points.DataBindY(item.Values);
                series.ChartType = SeriesChartType.Column;
                series.Color = item.Color;

                series["PointWidth"] = "0.5";

                series.IsValueShownAsLabel = true;
                series.LabelForeColor = System.Drawing.Color.DarkSlateGray;
                series.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
                series.LabelFormat = "N0";
                series.ToolTip = "#VALX: #VAL{0} задач";

                chart.Series.Add(series);
            }

            if (chart.ChartAreas.Count == 0)
                chart.ChartAreas.Add(new ChartArea());

            var area = chart.ChartAreas[0];

            area.AxisX.Title = "Статусы заявок";
            area.AxisX.TitleFont = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold);
            area.AxisX.MajorGrid.Enabled = false; 
            area.AxisX.Minimum = 0.5;
            area.AxisX.Maximum = 1.5;
            area.AxisX.Interval = 1;
            area.AxisX.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 9);

            area.AxisY.Title = "Количество заявок";
            area.AxisY.TitleFont = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold);
            area.AxisY.MajorGrid.LineColor = System.Drawing.Color.FromArgb(224, 224, 224);
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            area.AxisY.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 9);

            area.BorderColor = System.Drawing.Color.Gray;
            area.BorderDashStyle = ChartDashStyle.Solid;
            area.BorderWidth = 1;

            area.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            area.BackSecondaryColor = System.Drawing.Color.White;

            chart.Titles.Clear();
            Title title = new Title("Статистика заявок", Docking.Top, new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold), System.Drawing.Color.DarkSlateGray);
            chart.Titles.Add(title);

            chart.Legends.Clear();
            Legend legend = new Legend();
            legend.Docking = Docking.Bottom;
            legend.Alignment = StringAlignment.Center;
            legend.Font = new System.Drawing.Font("Segoe UI", 9);
            legend.BackColor = System.Drawing.Color.Transparent;
            legend.LegendStyle = LegendStyle.Table;
            legend.TableStyle =  LegendTableStyle.Wide;
            chart.Legends.Add(legend);

            chart.ResetAutoValues();
        }

        private void GetStatusCount(ref double incoming, ref double canceled, ref double inProgress, ref double closed) 
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(@"SELECT 
                                                            (SELECT 
                                                                    status
                                                                FROM
                                                                    claim_status
                                                                WHERE
                                                                    claim_status_id = idclaim_status) AS statusName,
                                                            COUNT(*) AS amount
                                                            FROM
                                                            connection_claim
                                                            GROUP BY claim_status_id;", conn);
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while(dr.Read())
                        {
                            switch (dr.GetValue(0))
                            {
                                case "Входящая":
                                    incoming = Convert.ToDouble(dr.GetValue(1));
                                    break;
                                case "Отменена":
                                    canceled = Convert.ToDouble(dr.GetValue(1));
                                    break;
                                case "В работе":
                                    inProgress = Convert.ToDouble(dr.GetValue(1));
                                    break;
                                case "Закрыта":
                                    closed = Convert.ToDouble(dr.GetValue(1));
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить диаграмму\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
