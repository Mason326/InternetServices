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
        string dateLimit = "";

        public Diagram(string dateFilter)
        {
            InitializeComponent();
            dateLimit = dateFilter;
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

            double[] values = new double[] { incoming, canceled, inProgress, closed };
            string[] names = new string[] { "Входящие", "Отмененные", "В работе", "Закрытые" };
            System.Drawing.Color[] colors = new System.Drawing.Color[]
            {
    System.Drawing.Color.FromArgb(76, 175, 80),
    System.Drawing.Color.FromArgb(244, 67, 54),
    System.Drawing.Color.FromArgb(255, 152, 0),
    System.Drawing.Color.FromArgb(156, 39, 176)
            };

            chart.Series.Clear();

            Series series = new Series("Статусы заявок");
            series.Points.DataBindXY(names, values);
            series.ChartType = SeriesChartType.Pie;
            series.Color = System.Drawing.Color.Transparent; 

            series["PieLabelStyle"] = "Outside";
            series["PieLineColor"] = "DarkGray";
            series.IsValueShownAsLabel = true;
            series.LabelForeColor = System.Drawing.Color.DarkSlateGray;
            series.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
            series.LabelFormat = "N0";
            series.ToolTip = "#VALX: #VAL{0} задач (#PERCENT{P0})";

            for (int i = 0; i < series.Points.Count; i++)
            {
                series.Points[i].Color = colors[i];
                
                if (i == 0) series.Points[i]["Exploded"] = "true";
            }

            chart.Series.Add(series);

            if (chart.ChartAreas.Count == 0)
                chart.ChartAreas.Add(new ChartArea());

            var area = chart.ChartAreas[0];

            area.AxisX.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.False;
            area.AxisY.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.False;
            area.Area3DStyle.Enable3D = false;

            area.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            area.BackSecondaryColor = System.Drawing.Color.White;

            chart.Titles.Clear();
            Title title = new Title("Статистика заявок", Docking.Top,
                new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold),
                System.Drawing.Color.DarkSlateGray);
            chart.Titles.Add(title);

            chart.Legends.Clear();
            Legend legend = new Legend();
            legend.Docking = Docking.Bottom;
            legend.Alignment = StringAlignment.Center;
            legend.Font = new System.Drawing.Font("Segoe UI", 9);
            legend.BackColor = System.Drawing.Color.Transparent;
            legend.LegendStyle = LegendStyle.Table;
            legend.TableStyle = LegendTableStyle.Wide;
            chart.Legends.Add(legend);


        }

        private void GetStatusCount(ref double incoming, ref double canceled, ref double inProgress, ref double closed) 
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"SELECT 
                                                            (SELECT 
                                                                    status
                                                                FROM
                                                                    claim_status
                                                                WHERE
                                                                    claim_status_id = idclaim_status) AS statusName,
                                                            COUNT(*) AS amount
                                                            FROM
                                                            connection_claim
                                                         WHERE {dateLimit}
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
