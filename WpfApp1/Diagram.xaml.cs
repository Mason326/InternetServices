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

    struct EmployeesData
    {
        public string FullName;
        public double Revenue;
    }
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

            DrawACircleDiagram();
            List<EmployeesData> revenueGist = GetEmployeesRating();
            DrawRevenueChart(revenueGist);
        }

        private void DrawACircleDiagram()
        {
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

        private void DrawRevenueChart(List<EmployeesData> data)
        {
            if (data == null || data.Count == 0)
            {
                MessageBox.Show("Нет данных для отображения", "Информация",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            chart2.Series.Clear();
            chart2.ChartAreas.Clear();

            Series series = new Series("Выручка");
            series.ChartType = SeriesChartType.Column;
            series.Color = System.Drawing.Color.FromArgb(54, 162, 235);

            series["PointWidth"] = "0.7";

            series.IsValueShownAsLabel = true;
            series.LabelForeColor = System.Drawing.Color.DarkSlateGray;
            series.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
            series.LabelFormat = "C0";
            series.ToolTip = "#VALX: #VAL{0:C}";

            foreach (var employee in data)
            {
                string shortName = employee.FullName;
                try
                {
                    shortName = FullNameSplitter.MakeShortName(employee.FullName);
                }
                catch
                {
                    ;
                }

                DataPoint point = series.Points.Add(Convert.ToDouble(employee.Revenue));
                point.AxisLabel = shortName;
                point.ToolTip = $"{employee.FullName}\nВыручка: {employee.Revenue:C}";
            }

            chart2.Series.Add(series);

            ChartArea area = new ChartArea();
            chart2.ChartAreas.Add(area);

            area.AxisX.Title = "Сотрудники";
            area.AxisX.TitleFont = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold);
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 9);
            area.AxisX.LabelStyle.Angle = -30;
            area.AxisX.LabelStyle.Interval = 1;
            area.AxisX.Interval = 1;

            if (data.Count > 5)
            {
                area.AxisX.LabelStyle.Interval = 1;
                area.AxisX.LabelStyle.Angle = -45;
            }

            area.AxisY.Title = "Выручка (руб.)";
            area.AxisY.TitleFont = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold);
            area.AxisY.MajorGrid.LineColor = System.Drawing.Color.FromArgb(224, 224, 224);
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            area.AxisY.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 9);
            area.AxisY.LabelStyle.Format = "C0";

            area.AxisY.Minimum = 0;

            area.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            area.BackSecondaryColor = System.Drawing.Color.White;
            area.BorderColor = System.Drawing.Color.Gray;
            area.BorderDashStyle = ChartDashStyle.Solid;
            area.BorderWidth = 1;

            chart2.Titles.Clear();
            Title title = new Title("Выручка по сотрудникам", Docking.Top,
                new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold),
                System.Drawing.Color.DarkSlateGray);
            chart2.Titles.Add(title);

            chart2.Legends.Clear();
            Legend legend = new Legend();
            legend.Docking = Docking.Top;
            legend.Alignment = StringAlignment.Center;
            legend.Font = new System.Drawing.Font("Segoe UI", 9);
            legend.BackColor = System.Drawing.Color.Transparent;
            chart2.Legends.Add(legend);

        }


        private List<EmployeesData> GetEmployeesRating()
        {
            List<EmployeesData> data = new List<EmployeesData>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"SELECT (select full_name from employees where idemployees = master_id) as fio, Count(*) as count, sum(totalcost) as revenue
                                    FROM connection_claim inner join `order` on idorder = order_id where claim_status_id != (Select idclaim_status from claim_status where `status` = 'Отменена') AND {dateLimit}
                                    group by master_id order by count desc;", conn);
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while(dr.Read())
                        {
                            var item = new EmployeesData();
                            item.FullName = dr["fio"].ToString();
                            item.Revenue = Convert.ToDouble(dr["revenue"]);

                            data.Add(item);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить диаграмму\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return data;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
