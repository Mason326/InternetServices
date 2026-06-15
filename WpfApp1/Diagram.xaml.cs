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
    /// Структура данных о сотруднике: ФИО и сумма выручки
    /// </summary>
    struct EmployeesData
    {
        public string FullName;   // Полное ФИО сотрудника
        public double Revenue;    // Сумма выручки (доход от заявок)
    }

    /// <summary>
    /// Структура данных о помесячной выручке
    /// </summary>
    struct MonthlyRevenue
    {
        public string Year;       // Год
        public string MonthName;  // Название месяца (Январь, Февраль и т.д.)
        public double Revenue;    // Сумма выручки за месяц
    }

    /// <summary>
    /// Форма "Диаграмма" - визуализация статистических данных
    /// Отображает три типа диаграмм:
    /// 1. Круговая диаграмма - распределение заявок по статусам
    /// 2. Столбчатая диаграмма - выручка по сотрудникам (мастерам)
    /// 3. Столбчатая диаграмма - помесячная выручка за текущий год
    /// </summary>
    public partial class Diagram : Window
    {
        // Условие фильтрации по дате для SQL-запросов
        string dateLimit = "";

        /// <summary>
        /// Конструктор формы - принимает фильтр по дате из родительской формы
        /// </summary>
        /// <param name="dateFilter">Условие фильтрации для SQL (например: "date between ... and ...")</param>
        public Diagram(string dateFilter)
        {
            InitializeComponent();
            dateLimit = dateFilter;
        }

        /// <summary>
        /// Событие загрузки формы - построение всех трех диаграмм
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

            DrawACircleDiagram();                    // Круговая диаграмма статусов
            List<EmployeesData> revenueHist = GetEmployeesRating();  // Данные по сотрудникам
            DrawRevenueChart(revenueHist);           // Столбчатая диаграмма выручки сотрудников
            List<MonthlyRevenue> monthlyRevenueHist = GetMonhtlyRevenueReport();  // Данные по месяцам
            BuildMonthlyRevenueChart(monthlyRevenueHist);  // Столбчатая диаграмма помесячной выручки
        }

        /// <summary>
        /// Построение круговой диаграммы распределения заявок по статусам
        /// </summary>
        private void DrawACircleDiagram()
        {
            double incoming = 0;    // Количество входящих заявок
            double canceled = 0;    // Количество отмененных заявок
            double inProgress = 0;  // Количество заявок в работе
            double closed = 0;      // Количество закрытых заявок

            GetStatusCount(ref incoming, ref canceled, ref inProgress, ref closed);

            // Данные для диаграммы
            double[] values = new double[] { incoming, canceled, inProgress, closed };
            string[] names = new string[] { "Входящие", "Отмененные", "В работе", "Закрытые" };

            // Цвета секторов
            System.Drawing.Color[] colors = new System.Drawing.Color[]
            {
                System.Drawing.Color.FromArgb(76, 175, 80),   // Зеленый - входящие
                System.Drawing.Color.FromArgb(244, 67, 54),   // Красный - отмененные
                System.Drawing.Color.FromArgb(255, 152, 0),   // Оранжевый - в работе
                System.Drawing.Color.FromArgb(156, 39, 176)   // Фиолетовый - закрытые
            };

            chart.Series.Clear();

            // Создание серии для круговой диаграммы
            Series series = new Series("Статусы заявок");
            series.Points.DataBindXY(names, values);
            series.ChartType = SeriesChartType.Pie;
            series.Color = System.Drawing.Color.Transparent;

            // Настройка отображения меток
            series["PieLabelStyle"] = "Outside";      // Метки снаружи
            series["PieLineColor"] = "DarkGray";      // Цвет линий от секторов к меткам
            series.IsValueShownAsLabel = true;        // Показывать значения на метках
            series.LabelForeColor = System.Drawing.Color.DarkSlateGray;
            series.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
            series.LabelFormat = "N0";                // Формат чисел (без десятичных)
            series.ToolTip = "#VALX: #VAL{0} задач (#PERCENT{P0})";  // Всплывающая подсказка

            // Применение цветов к секторам
            for (int i = 0; i < series.Points.Count; i++)
            {
                series.Points[i].Color = colors[i];
                if (i == 0) series.Points[i]["Exploded"] = "true";  // Первый сектор слегка отделен
            }

            chart.Series.Add(series);

            // Настройка области диаграммы
            if (chart.ChartAreas.Count == 0)
                chart.ChartAreas.Add(new ChartArea());

            var area = chart.ChartAreas[0];

            // Отключение осей (для круговой диаграммы они не нужны)
            area.AxisX.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.False;
            area.AxisY.Enabled = System.Windows.Forms.DataVisualization.Charting.AxisEnabled.False;
            area.Area3DStyle.Enable3D = false;  // Плоский вид

            area.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            area.BackSecondaryColor = System.Drawing.Color.White;

            // Заголовок диаграммы
            chart.Titles.Clear();
            Title title = new Title("Статистика заявок", Docking.Top,
                new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold),
                System.Drawing.Color.DarkSlateGray);
            chart.Titles.Add(title);

            // Легенда (справочник цветов)
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

        /// <summary>
        /// Получение количества заявок по каждому статусу из базы данных
        /// </summary>
        private void GetStatusCount(ref double incoming, ref double canceled, ref double inProgress, ref double closed)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    // Группировка заявок по статусу с учетом фильтра по дате
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
                        while (dr.Read())
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

        /// <summary>
        /// Построение столбчатой диаграммы выручки по сотрудникам (мастерам)
        /// </summary>
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

            // Создание серии для столбчатой диаграммы
            Series series = new Series("Выручка");
            series.ChartType = SeriesChartType.Column;
            series.Color = System.Drawing.Color.FromArgb(54, 162, 235);  // Голубой цвет

            series["PointWidth"] = "0.7";  // Ширина столбцов

            // Настройка отображения значений на столбцах
            series.IsValueShownAsLabel = true;
            series.LabelForeColor = System.Drawing.Color.DarkSlateGray;
            series.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
            series.LabelFormat = "C0";  // Формат валюты
            series.ToolTip = "#VALX: #VAL{0:C}";

            // Добавление данных
            foreach (var employee in data)
            {
                string shortName = employee.FullName;
                try
                {
                    shortName = FullNameSplitter.MakeShortName(employee.FullName);  // Сокращение ФИО
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

            // Настройка области диаграммы
            ChartArea area = new ChartArea();
            chart2.ChartAreas.Add(area);

            // Настройка оси X (Сотрудники)
            area.AxisX.Title = "Сотрудники";
            area.AxisX.TitleFont = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold);
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 7);
            area.AxisX.LabelStyle.Angle = -30;  // Наклон подписей для читаемости
            area.AxisX.LabelStyle.Interval = 1;
            area.AxisX.Interval = 1;

            if (data.Count > 5)
            {
                area.AxisX.LabelStyle.Interval = 1;
                area.AxisX.LabelStyle.Angle = -45;  // Больший наклон при большом количестве сотрудников
            }

            // Настройка оси Y (Выручка)
            area.AxisY.Title = "Выручка (руб.)";
            area.AxisY.TitleFont = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold);
            area.AxisY.MajorGrid.LineColor = System.Drawing.Color.FromArgb(224, 224, 224);
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            area.AxisY.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 9);
            area.AxisY.LabelStyle.Format = "C0";
            area.AxisY.Minimum = 0;

            // Фоновое оформление
            area.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            area.BackSecondaryColor = System.Drawing.Color.White;
            area.BorderColor = System.Drawing.Color.Gray;
            area.BorderDashStyle = ChartDashStyle.Solid;
            area.BorderWidth = 1;

            // Заголовок
            chart2.Titles.Clear();
            Title title = new Title("Выручка по сотрудникам", Docking.Top,
                new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold),
                System.Drawing.Color.DarkSlateGray);
            chart2.Titles.Add(title);

            // Легенда
            chart2.Legends.Clear();
            Legend legend = new Legend();
            legend.Docking = Docking.Top;
            legend.Alignment = StringAlignment.Center;
            legend.Font = new System.Drawing.Font("Segoe UI", 9);
            legend.BackColor = System.Drawing.Color.Transparent;
            chart2.Legends.Add(legend);
        }

        /// <summary>
        /// Построение столбчатой диаграммы помесячной выручки за текущий год
        /// </summary>
        private void BuildMonthlyRevenueChart(List<MonthlyRevenue> data)
        {
            if (data == null || data.Count == 0)
            {
                MessageBox.Show("Нет данных для отображения", "Информация",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Словарь для быстрого доступа к данным по месяцам
            Dictionary<string, MonthlyRevenue> monthDataDict = new Dictionary<string, MonthlyRevenue>();
            foreach (var item in data)
            {
                monthDataDict[item.MonthName] = item;
            }

            // Все месяцы в правильном порядке (с января по декабрь)
            List<string> allMonths = new List<string>
            {
                "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
                "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"
            };

            // Создание полного списка данных за все месяцы (с нулями для отсутствующих)
            List<MonthlyRevenue> fullYearData = new List<MonthlyRevenue>();
            foreach (string month in allMonths)
            {
                if (monthDataDict.ContainsKey(month))
                {
                    fullYearData.Add(monthDataDict[month]);
                }
                else
                {
                    fullYearData.Add(new MonthlyRevenue
                    {
                        MonthName = month,
                        Revenue = 0,
                        Year = DateTime.Now.Year.ToString(),
                    });
                }
            }

            // Очистка диаграммы
            chart3.Series.Clear();
            chart3.ChartAreas.Clear();

            // Создание серии для столбчатой диаграммы
            Series series = new Series("Выручка");
            series.ChartType = SeriesChartType.Column;
            series.Color = System.Drawing.Color.FromArgb(76, 175, 80);  // Зеленый цвет

            series["PointWidth"] = "0.7";

            // Настройка отображения значений
            series.IsValueShownAsLabel = true;
            series.LabelForeColor = System.Drawing.Color.DarkSlateGray;
            series.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold);
            series.LabelFormat = "C0";
            series.ToolTip = "#VALX: #VAL{0:C}";

            // Добавление данных за все месяцы
            for (int i = 0; i < fullYearData.Count; i++)
            {
                var monthData = fullYearData[i];
                DataPoint point = series.Points.Add(Convert.ToDouble(monthData.Revenue));

                point.AxisLabel = monthData.MonthName;
                point.Label = monthData.Revenue > 0 ? monthData.Revenue.ToString("C0") : "";

                if (monthData.Revenue > 0)
                {
                    point.ToolTip = $"{monthData.MonthName} {monthData.Year}\nВыручка: {monthData.Revenue:C}";
                }
                else
                {
                    point.ToolTip = $"{monthData.MonthName}\nНет данных";
                }

                // Цветовая схема по сезонам
                if (monthData.MonthName == "Декабрь" || monthData.MonthName == "Январь" || monthData.MonthName == "Февраль")
                    point.Color = System.Drawing.Color.FromArgb(100, 181, 246);  // Зима - голубой
                else if (monthData.MonthName == "Март" || monthData.MonthName == "Апрель" || monthData.MonthName == "Май")
                    point.Color = System.Drawing.Color.FromArgb(129, 199, 132);  // Весна - зеленый
                else if (monthData.MonthName == "Июнь" || monthData.MonthName == "Июль" || monthData.MonthName == "Август")
                    point.Color = System.Drawing.Color.FromArgb(255, 152, 0);    // Лето - оранжевый
                else
                    point.Color = System.Drawing.Color.FromArgb(156, 39, 176);   // Осень - фиолетовый

                if (monthData.Revenue == 0)
                {
                    point.Color = System.Drawing.Color.FromArgb(200, 200, 200);  // Серый для нулевых значений
                }
            }

            chart3.Series.Add(series);

            // Настройка области диаграммы
            ChartArea area = new ChartArea();
            chart3.ChartAreas.Add(area);

            // Настройка оси X (Месяцы)
            area.AxisX.Title = "Месяцы";
            area.AxisX.TitleFont = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold);
            area.AxisX.MajorGrid.Enabled = false;
            area.AxisX.LabelStyle.Enabled = true;
            area.AxisX.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Regular);
            area.AxisX.LabelStyle.Angle = -45;        // Наклон для читаемости
            area.AxisX.LabelStyle.Interval = 1;       // Каждая метка

            // Настройка оси Y (Выручка)
            area.AxisY.Title = "Выручка (руб.)";
            area.AxisY.TitleFont = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold);
            area.AxisY.MajorGrid.LineColor = System.Drawing.Color.FromArgb(224, 224, 224);
            area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
            area.AxisY.LabelStyle.Font = new System.Drawing.Font("Segoe UI", 9);
            area.AxisY.LabelStyle.Format = "C0";
            area.AxisY.Minimum = 0;

            if (fullYearData.Any(x => x.Revenue > 0))
            {
                double maxRevenue = fullYearData.Max(x => x.Revenue);
                area.AxisY.Maximum = maxRevenue * 1.1;  // Запас сверху 10%
            }

            // Фоновое оформление
            area.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            area.BackSecondaryColor = System.Drawing.Color.White;
            area.BorderColor = System.Drawing.Color.Gray;
            area.BorderDashStyle = ChartDashStyle.Solid;
            area.BorderWidth = 1;

            // Заголовок с итоговой суммой за год
            chart3.Titles.Clear();
            int currentYear = DateTime.Now.Year;
            double totalRevenue = fullYearData.Sum(x => x.Revenue);
            Title title = new Title($"Выручка по месяцам за {currentYear} год\n" +
                                   $"Итого: {totalRevenue:C}",
                                   Docking.Top,
                                   new System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold),
                                   System.Drawing.Color.DarkSlateGray);
            chart3.Titles.Add(title);

            // Легенда (очищаем, так как информация передается через заголовок)
            chart3.Legends.Clear();

            // Принудительное обновление диаграммы
            chart3.Invalidate();
            chart3.Update();
        }

        /// <summary>
        /// Преобразование номера месяца в название месяца на русском языке
        /// </summary>
        private string ConvertMonthNumberToName(int monthNumber)
        {
            string[] monthNames =
            {
                "Январь", "Февраль", "Март", "Апрель", "Май", "Июнь",
                "Июль", "Август", "Сентябрь", "Октябрь", "Ноябрь", "Декабрь"
            };

            if (monthNumber >= 1 && monthNumber <= 12)
                return monthNames[monthNumber - 1];

            return "Неизвестно";
        }

        /// <summary>
        /// Получение отчета о помесячной выручке за текущий год
        /// </summary>
        private List<MonthlyRevenue> GetMonhtlyRevenueReport()
        {
            List<MonthlyRevenue> monthlyRevenue = new List<MonthlyRevenue>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    // Группировка по годам и месяцам с суммированием выручки из заказов
                    MySqlCommand cmd = new MySqlCommand($@"SELECT 
                                                            YEAR(connection_creationDate) AS currentYear,
                                                            MONTH(connection_creationDate) AS monthNumber,
                                                            SUM(totalCost) AS monthlyRevenue,
                                                            COUNT(*) AS count
                                                        FROM
                                                            connection_claim
                                                                LEFT JOIN
                                                            `order` ON idorder = order_id
                                                        WHERE
                                                            YEAR(connection_creationDate) = YEAR(NOW())
                                                        GROUP BY YEAR(connection_creationDate) , MONTH(connection_creationDate);", conn);
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var item = new MonthlyRevenue();
                            item.MonthName = ConvertMonthNumberToName(Convert.ToInt32(dr["monthNumber"]));
                            item.Year = dr["currentYear"].ToString();
                            item.Revenue = Convert.ToDouble(dr["monthlyRevenue"]);
                            monthlyRevenue.Add(item);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить диаграмму\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return monthlyRevenue;
        }

        /// <summary>
        /// Получение рейтинга сотрудников (мастеров) по выручке
        /// Группировка по мастерам, исключая отмененные заявки
        /// </summary>
        private List<EmployeesData> GetEmployeesRating()
        {
            List<EmployeesData> data = new List<EmployeesData>();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    // Запрос: количество заявок и сумма выручки по каждому мастеру
                    MySqlCommand cmd = new MySqlCommand($@"SELECT (select full_name from employees where idemployees = master_id) as fio, Count(*) as count, sum(totalcost) as revenue
                                    FROM connection_claim inner join `order` on idorder = order_id where claim_status_id != (Select idclaim_status from claim_status where `status` = 'Отменена') AND {dateLimit}
                                    group by master_id order by count desc;", conn);
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
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

        /// <summary>
        /// Кнопка "На главную" - закрытие формы
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}