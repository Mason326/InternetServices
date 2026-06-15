using System;
using System.Collections.Generic;
using System.Data;
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
using Word = Microsoft.Office.Interop.Word;
using MySql.Data.MySqlClient;
using System.IO;
using System.Reflection;

namespace WpfApp1
{
    /// <summary>
    /// Форма "Заказ-наряд" - создание и закрытие заказ-наряда на выполненные работы
    /// Позволяет:
    /// - Выбирать услуги и материалы для заказ-наряда
    /// - Управлять количеством услуг/материалов
    /// - Рассчитывать общую стоимость с учетом скидки
    /// - Сохранять заказ-наряд в БД
    /// - Печатать акт выполненных работ в Word
    /// </summary>
    public partial class Order : Window
    {
        int claimId;                                           // ID заявки, для которой создается заказ-наряд
        DataTable dtServices = new DataTable();                // Таблица для хранения выбранных услуг
        Dictionary<string, DataRowView> servicesDictionary = new Dictionary<string, DataRowView>();  // Словарь услуг для быстрого доступа
        DataTable dtMaterials = new DataTable();               // Таблица для хранения выбранных материалов
        Dictionary<string, DataRowView> materialsDictionary = new Dictionary<string, DataRowView>(); // Словарь материалов для быстрого доступа
        Action<bool> RefreshDG;                               // Делегат для обновления родительского DataGrid

        /// <summary>
        /// Конструктор формы - инициализация таблиц и словарей
        /// </summary>
        /// <param name="claimIdentifier">ID заявки</param>
        /// <param name="refreshDG">Метод обновления DataGrid</param>
        public Order(int claimIdentifier, Action<bool> refreshDG)
        {
            InitializeComponent();
            claimId = claimIdentifier;

            // Структура таблицы услуг: название, количество, стоимость, ID, единицы измерения
            dtServices.Columns.Add("service_name", typeof(string));
            dtServices.Columns.Add("count", typeof(int));
            dtServices.Columns.Add("cost", typeof(double));
            dtServices.Columns.Add("service_id", typeof(int));
            dtServices.Columns.Add("units", typeof(string));

            // Структура таблицы материалов: название, количество, стоимость, ID, единицы измерения
            dtMaterials.Columns.Add("material_name", typeof(string));
            dtMaterials.Columns.Add("count", typeof(int));
            dtMaterials.Columns.Add("cost", typeof(double));
            dtMaterials.Columns.Add("material_id", typeof(int));
            dtMaterials.Columns.Add("units", typeof(string));

            // Инициализация сумм
            materialsTotalCostLabel.Content = 0;
            servicesTotalCostLabel.Content = 0;
            orderTotalCostLabel.Content = 0;
            discountAmountLabel.Content = 0;
            RefreshDG += refreshDG;
        }

        /// <summary>
        /// Кнопка "На главную" - закрытие формы
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Событие загрузки формы - загрузка данных заявки, услуг и материалов
        /// </summary>
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

            numberOrderLabel.Content = GetOrderNumber();  // Получение номера заказ-наряда

            // Загрузка данных заявки из БД
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"Select `id_claim`, `connection_creationDate`, `mount_date`, `connection_address`, tariff.`tariff_name` as 'tariff', client.full_name as 'client_fio', employees.full_name as 'employee_fio', claim_status.status as 'claim_status', (Select full_name from employees where idemployees = connection_claim.master_id) as 'master_fio' from connection_claim inner join `client` on client.idclient = connection_claim.client_id
													        inner join `employees` on employees.idemployees = connection_claim.employees_id
													        inner join `tariff` on tariff.idtariff = connection_claim.tariff_id
													        inner join `claim_status` on `claim_status`.idclaim_status = connection_claim.claim_status_id where id_claim = {claimId}", conn);
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        object[] values = new object[dr.FieldCount];
                        while (dr.Read())
                        {
                            dr.GetValues(values);
                        }
                        numberClaimLabel.Content = values[0];
                        creationDateLabel.Content = ((DateTime)values[1]).ToString("dd.MM.yyyy");
                        executionDateLabel.Content = ((DateTime)values[2]).ToString("dd.MM.yyyy HH:mm");

                        // Очистка адреса от лишних символов
                        string address = string.Join(", ", values[3].ToString().Split(new string[] { ", ", "\t,", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(el => el.Trim()));
                        address = address.Replace(",,", ",");
                        mountAddressTextBox.Text = address;
                        tariffLabel.Content = values[4];
                        clientLabel.Content = values[5];
                        managerLabel.Content = values[6];
                        statusLabel.Content = values[7];
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить заказ-наряд\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            FillServicesDG("");      // Загрузка списка услуг
            FillMaterialsDG("");     // Загрузка списка материалов
        }

        /// <summary>
        /// Получение номера нового заказ-наряда (максимальный ID + 1)
        /// </summary>
        private int GetOrderNumber()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"SELECT max(idorder) FROM `order`;", conn);
                    int orderNumber = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
                    return orderNumber;
                }
            }
            catch
            {
                return 1;  // Если таблица пуста, начинаем с 1
            }
        }

        /// <summary>
        /// Заполнение DataGrid списком услуг (с фильтрацией по поисковому запросу)
        /// </summary>
        private void FillServicesDG(string searchWord)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"SELECT * FROM `services` where `service_name` LIKE '%{searchWord}%';", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    cmd.ExecuteNonQuery();
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    servicesDG.ItemsSource = dt.AsDataView();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить услуги\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Заполнение DataGrid списком материалов (с фильтрацией по поисковому запросу)
        /// </summary>
        private void FillMaterialsDG(string searchWord)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"SELECT * FROM `materials` where `material_name` LIKE '%{searchWord}%';;", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    cmd.ExecuteNonQuery();
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    materialsDG.ItemsSource = dt.AsDataView();
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить материалы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Поиск услуг/материалов при вводе текста в поле поиска
        /// Выполняется при длине запроса более 3 символов
        /// </summary>
        private void searchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            var textBox = sender as TextBox;
            Action<string> targetName;

            switch (textBox.Name)
            {
                case "searchServiceTextBox":
                    targetName = FillServicesDG;
                    break;
                case "searchMaterialTextBox":
                    targetName = FillMaterialsDG;
                    break;
                default:
                    targetName = (string str) => { };
                    break;
            }

            if (textBox.Text.Length > 3)
                targetName(textBox.Text);
            else if (textBox.Text.Length == 0)
                targetName("");
        }

        /// <summary>
        /// Кнопка "Добавить услугу" - добавление услуги в заказ-наряд
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (servicesDG.SelectedItem != null)
            {
                var drv = servicesDG.SelectedItem as DataRowView;
                var items = drv.Row.ItemArray;

                // Если услуга уже есть в наряде - увеличиваем количество
                if (servicesDictionary.ContainsKey(items[1].ToString()))
                {
                    DataRowView values;
                    servicesDictionary.TryGetValue(items[1].ToString(), out values);
                    orderServiceDG.Items.Remove(values);
                    DataRow dr = dtServices.NewRow();
                    int newCount = Convert.ToInt32(values[1]);
                    dr.ItemArray = new object[] { items[1].ToString(), ++newCount, items[3].ToString(), items[0], items[2].ToString() };
                    dtServices.Rows.Add(dr);
                    DataRowView addedToOrderDg = dtServices.DefaultView[dtServices.Rows.IndexOf(dr)];
                    servicesDictionary.Remove(items[1].ToString());
                    servicesDictionary.Add(items[1].ToString(), addedToOrderDg);
                    orderServiceDG.Items.Add(addedToOrderDg);
                    dtServices.Rows.Remove(values.Row);
                }
                else  // Новая услуга
                {
                    DataRow dr = dtServices.NewRow();
                    dr.ItemArray = new object[] { items[1].ToString(), 1, items[3], items[0], items[2].ToString() };
                    dtServices.Rows.Add(dr);
                    DataRowView addedToOrderDg = dtServices.DefaultView[dtServices.Rows.IndexOf(dr)];
                    servicesDictionary.Add(items[1].ToString(), addedToOrderDg);
                    orderServiceDG.Items.Add(addedToOrderDg);
                }

                // Пересчет общей стоимости услуг
                double servicesCost = Convert.ToDouble(servicesTotalCostLabel.Content);
                servicesCost += Convert.ToDouble(items[3]);
                servicesTotalCostLabel.Content = Math.Round(servicesCost, 2);

                double materialsCost = Convert.ToDouble(materialsTotalCostLabel.Content);
                orderTotalCostLabel.Content = Math.Round(servicesCost + materialsCost, 2);
                RefreshDiscountLabel();  // Пересчет скидки
            }
        }

        /// <summary>
        /// Кнопка "Добавить материал" - добавление материала в заказ-наряд
        /// </summary>
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (materialsDG.SelectedItem != null)
            {
                var drv = materialsDG.SelectedItem as DataRowView;
                var items = drv.Row.ItemArray;

                // Если материал уже есть в наряде - увеличиваем количество
                if (materialsDictionary.ContainsKey(items[1].ToString()))
                {
                    DataRowView values;
                    materialsDictionary.TryGetValue(items[1].ToString(), out values);
                    orderMaterials.Items.Remove(values);
                    DataRow dr = dtMaterials.NewRow();
                    int newCount = Convert.ToInt32(values[1]);
                    dr.ItemArray = new object[] { items[1].ToString(), ++newCount, items[3], items[0], items[2].ToString() };
                    dtMaterials.Rows.Add(dr);
                    DataRowView addedToOrderDg = dtMaterials.DefaultView[dtMaterials.Rows.IndexOf(dr)];
                    materialsDictionary.Remove(items[1].ToString());
                    materialsDictionary.Add(items[1].ToString(), addedToOrderDg);
                    orderMaterials.Items.Add(addedToOrderDg);
                    dtMaterials.Rows.Remove(values.Row);
                }
                else  // Новый материал
                {
                    DataRow dr = dtMaterials.NewRow();
                    dr.ItemArray = new object[] { items[1].ToString(), 1, items[3], items[0], items[2].ToString() };
                    dtMaterials.Rows.Add(dr);
                    DataRowView addedToOrderDg = dtMaterials.DefaultView[dtMaterials.Rows.IndexOf(dr)];
                    materialsDictionary.Add(items[1].ToString(), addedToOrderDg);
                    orderMaterials.Items.Add(addedToOrderDg);
                }

                // Пересчет общей стоимости материалов
                double materialCost = Convert.ToDouble(materialsTotalCostLabel.Content);
                materialCost += Convert.ToDouble(items[3]);
                materialsTotalCostLabel.Content = Math.Round(materialCost, 2);

                double servicesCost = Convert.ToDouble(servicesTotalCostLabel.Content);
                orderTotalCostLabel.Content = Math.Round(servicesCost + materialCost, 2);
                RefreshDiscountLabel();  // Пересчет скидки
            }
        }

        /// <summary>
        /// Кнопка "Удалить услугу" - уменьшение количества или удаление услуги из наряда
        /// </summary>
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if (orderServiceDG.SelectedItem != null)
            {
                var drv = orderServiceDG.SelectedItem as DataRowView;
                var items = drv.Row.ItemArray;

                if (servicesDictionary.ContainsKey(items[0].ToString()))
                {
                    DataRowView values;
                    servicesDictionary.TryGetValue(items[0].ToString(), out values);
                    int newCount = Convert.ToInt32(values[1]);

                    if (--newCount >= 1)  // Уменьшаем количество, если осталось >= 1
                    {
                        drv.Row.SetField<int>(1, newCount);
                        drv.Row.AcceptChanges();
                        servicesDictionary.Remove(items[0].ToString());
                        servicesDictionary.Add(items[0].ToString(), drv);
                    }
                    else  // Удаляем услугу полностью
                    {
                        orderServiceDG.Items.Remove(drv);
                        servicesDictionary.Remove(items[0].ToString());
                    }
                }

                // Пересчет стоимости
                double servicesCost = Convert.ToDouble(servicesTotalCostLabel.Content);
                if (servicesCost != 0)
                {
                    servicesCost -= Convert.ToDouble(items[2]);
                    servicesTotalCostLabel.Content = Math.Round(servicesCost, 2);
                }

                double materialsCost = Convert.ToDouble(materialsTotalCostLabel.Content);
                orderTotalCostLabel.Content = Math.Round(servicesCost + materialsCost, 2);
                RefreshDiscountLabel();  // Пересчет скидки
            }
        }

        /// <summary>
        /// Кнопка "Удалить материал" - уменьшение количества или удаление материала из наряда
        /// </summary>
        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            if (orderMaterials.SelectedItem != null)
            {
                var drv = orderMaterials.SelectedItem as DataRowView;
                var items = drv.Row.ItemArray;

                if (materialsDictionary.ContainsKey(items[0].ToString()))
                {
                    DataRowView values;
                    materialsDictionary.TryGetValue(items[0].ToString(), out values);
                    int newCount = Convert.ToInt32(values.Row.ItemArray[1]);

                    if (--newCount >= 1)  // Уменьшаем количество, если осталось >= 1
                    {
                        drv.Row.SetField<int>(1, newCount);
                        drv.Row.AcceptChanges();
                        materialsDictionary.Remove(items[0].ToString());
                        materialsDictionary.Add(items[0].ToString(), drv);
                    }
                    else  // Удаляем материал полностью
                    {
                        orderMaterials.Items.Remove(drv);
                        materialsDictionary.Remove(items[0].ToString());
                    }
                }

                // Пересчет стоимости
                double materialCost = Convert.ToDouble(materialsTotalCostLabel.Content);
                if (materialCost != 0)
                {
                    materialCost -= Convert.ToDouble(items[2]);
                    materialsTotalCostLabel.Content = Math.Round(materialCost, 2);
                }

                double servicesCost = Convert.ToDouble(servicesTotalCostLabel.Content);
                orderTotalCostLabel.Content = Math.Round(servicesCost + materialCost, 2);
                RefreshDiscountLabel();  // Пересчет скидки
            }
        }

        /// <summary>
        /// Кнопка "Закрыть наряд" - сохранение заказ-наряда в БД и закрытие заявки
        /// </summary>
        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            // Проверка: должны быть указаны выполненные услуги
            if (servicesDictionary.Count < 1)
            {
                MessageBox.Show($"В заказ-наряде должны быть оказаны указаны выполненные услуги", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult res = MessageBox.Show("Вы уверены, что хотите закрыть наряд?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
            if (res != MessageBoxResult.Yes)
                return;

            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();  // Транзакция для согласованного сохранения

                // Основной запрос на вставку заказ-наряда
                string cmdText = $"INSERT INTO `order`(`idorder`, `orderDate`, `totalCost`, `connection_claim_id`) VALUE ({numberOrderLabel.Content}, '{DateTime.Now.Date.ToString("yyyy-MM-dd")}', {orderTotalCostLabel.Content.ToString().Replace(',', '.')}, {claimId});";

                // Добавление услуг в пакет услуг
                if (servicesDictionary.Count > 0)
                {
                    cmdText += "INSERT INTO `services_pack` VALUES ";
                }
                foreach (var el in servicesDictionary)
                {
                    DataRowView drv = el.Value;
                    int serviceId = Convert.ToInt32(drv.Row.ItemArray[3]);
                    cmdText += $"({serviceId}, {numberOrderLabel.Content}, {el.Value[1]}),";
                }

                // Добавление материалов в пакет материалов
                if (materialsDictionary.Count > 0)
                {
                    cmdText = cmdText.Trim(new char[] { ',' });
                    cmdText += ";";
                    cmdText += "INSERT INTO `materials_pack` VALUES ";
                }
                foreach (var el in materialsDictionary)
                {
                    DataRowView drv = el.Value;
                    int materialId = Convert.ToInt32(drv.Row.ItemArray[3]);
                    cmdText += $"({materialId}, {numberOrderLabel.Content}, {el.Value[1]}),";
                }

                try
                {
                    cmdText = cmdText.TrimEnd(new char[] { ',' });
                    cmdText += ";";
                    // Обновление статуса заявки на "Закрыта" и связь с заказ-нарядом
                    cmdText += $"Update `connection_claim` SET `claim_status_id` = (Select `idclaim_status` from `claim_status` where `status` = 'Закрыта'), `order_id` = {numberOrderLabel.Content} where `id_claim` = {claimId}";

                    MySqlCommand cmd = new MySqlCommand($"{cmdText};", conn);
                    cmd.Transaction = transaction;
                    cmd.ExecuteNonQuery();
                    transaction.Commit();

                    MessageBox.Show($"Наряд успешно закрыт", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshDG(true);  // Обновление родительского DataGrid

                    // Предложение распечатать акт
                    MessageBoxResult printRes = MessageBox.Show("Хотите распечатать акт выполненных работ?", "Внимание", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                    if (printRes == MessageBoxResult.Yes)
                        PrintADocument();
                    this.Close();
                }
                catch (Exception exc)
                {
                    transaction.Rollback();  // Откат транзакции при ошибке
                    MessageBox.Show($"Не удалось закрыть наряд\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Обработчик чекбокса скидки - применение 15% скидки к общей сумме
        /// </summary>
        private void discountCheckBox_Click(object sender, RoutedEventArgs e)
        {
            RefreshDiscountLabel();
        }

        /// <summary>
        /// Обновление суммы скидки и итоговой стоимости
        /// Скидка составляет 15% от общей суммы
        /// </summary>
        private void RefreshDiscountLabel()
        {
            double currentOrderCost = Convert.ToDouble(orderTotalCostLabel.Content);
            bool checkedFlag = discountCheckBox.IsChecked.HasValue && discountCheckBox.IsChecked.Value;
            double discount = currentOrderCost * 0.15;
            discountAmountLabel.Content = checkedFlag ? Math.Round(discount, 3) : 0;
            orderTotalCostLabel.Content = checkedFlag ? Math.Round(currentOrderCost - discount, 3) : Math.Round(Convert.ToDouble(servicesTotalCostLabel.Content) + Convert.ToDouble(materialsTotalCostLabel.Content), 3);
        }

        /// <summary>
        /// Печать акта выполненных работ в формате Word
        /// Формирует документ на основе шаблона с подстановкой данных
        /// </summary>
        private void PrintADocument()
        {
            if (servicesDictionary.Count < 1)
            {
                MessageBox.Show("В наряде должны быть выбраны выполненные услуги", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Путь к шаблону акта
            string fileName = Directory.GetCurrentDirectory();
            if (fileName.Contains("bin\\"))
            {
                fileName = string.Join("\\", fileName.Split('\\').TakeWhile(el => el != "bin"));
            }
            fileName += "\\Resources\\Templates\\ActTemplate.doc";

            Word.Application wordApp = new Word.Application();
            wordApp.Visible = false;  // Работаем в фоновом режиме

            try
            {
                Word.Document doc = wordApp.Documents.Add();
                Word.Range range = doc.Content;
                Word.PageSetup pageSetup = doc.PageSetup;

                // Настройка полей страницы
                pageSetup.LeftMargin = wordApp.CentimetersToPoints(0.75f);
                pageSetup.RightMargin = wordApp.CentimetersToPoints(0.75f);
                pageSetup.TopMargin = wordApp.CentimetersToPoints(1.5f);
                pageSetup.BottomMargin = wordApp.CentimetersToPoints(2.68f);

                pageSetup.PaperSize = Word.WdPaperSize.wdPaperA4;
                pageSetup.Orientation = Word.WdOrientation.wdOrientPortrait;

                range.Collapse(Word.WdCollapseDirection.wdCollapseEnd);

                if (range.End > 1)
                    range.MoveEnd(Word.WdUnits.wdCharacter, -1);

                // Вставка содержимого шаблона
                range.InsertFile(
                    FileName: fileName,
                    Link: false,
                    Range: Missing.Value,
                    ConfirmConversions: false
                );

                doc.Fields.Update();  // Обновление полей документа

                // Замена плейсхолдеров в документе
                ReplaceWord("{orderNumber}", numberOrderLabel.Content.ToString(), doc);
                ReplaceWord("{orderDate}", (DateTime.Now).ToString("dd.MM.yyyy"), doc);
                ReplaceWord("{companyName}", Properties.Settings.Default.companyName, doc);
                ReplaceWord("{companyDescription}", Properties.Settings.Default.companyDescription, doc);
                ReplaceWord("{clientName}", clientLabel.Content.ToString(), doc);
                string address = string.Join(", ", mountAddressTextBox.Text.Split(new string[] { ", ", "\t,", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(el => el.Trim()));
                ReplaceWord("{mountAddress}", address, doc);
                ReplaceWord("{totalServicesCost}", Convert.ToDouble(servicesTotalCostLabel.Content).ToString("f2"), doc);

                if (discountCheckBox.IsChecked.HasValue && discountCheckBox.IsChecked.Value)
                    ReplaceWord("{discountAmount}", $"Размер скидки: {discountAmountLabel.Content} руб.", doc);
                else
                    ReplaceWord("{discountAmount}", "", doc);
                ReplaceWord("{totalOrderCost}", orderTotalCostLabel.Content.ToString(), doc);

                // Формирование таблицы услуг
                if (range.Find.Execute("{tableServices}"))
                {
                    range.Text = "";
                    int rowCount = servicesDictionary.Count;
                    Word.Table tbl = doc.Tables.Add(range, rowCount + 1, 4);

                    // Настройка границ и внешнего вида таблицы
                    tbl.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    tbl.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;

                    // Заголовки таблицы
                    for (int i = 1; i < 5; i++)
                    {
                        tbl.Rows[1].Cells[i].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                        tbl.Rows[1].Cells[i].Range.Bold = 3;
                        tbl.Rows[1].Cells[i].Range.Font.Name = "Arial";
                        tbl.Rows[1].Cells[i].Range.Font.Size = 8;
                        tbl.Rows[1].Cells[i].Range.ParagraphFormat.LeftIndent = 0;
                        tbl.Rows[1].Cells[i].Range.ParagraphFormat.RightIndent = 0;
                    }
                    tbl.Rows[1].Cells[1].Range.Text = "Наименование услуги";
                    tbl.Rows[1].Cells[2].Range.Text = "Единица измерения";
                    tbl.Rows[1].Cells[3].Range.Text = "Количество";
                    tbl.Rows[1].Cells[4].Range.Text = "Сумма (руб.)";

                    // Заполнение таблицы услугами
                    int contentCounter = 2;
                    foreach (var el in servicesDictionary)
                    {
                        double serviceCost = Convert.ToDouble(el.Value.Row.ItemArray[2]);
                        double serviceQuantity = Convert.ToInt32(el.Value.Row.ItemArray[1]);
                        tbl.Rows[contentCounter].Cells[1].Range.Text = el.Value.Row.ItemArray[0].ToString();
                        tbl.Rows[contentCounter].Cells[2].Range.Text = el.Value.Row.ItemArray[4].ToString();
                        tbl.Rows[contentCounter].Cells[3].Range.Text = el.Value.Row.ItemArray[1].ToString();
                        tbl.Rows[contentCounter].Cells[4].Range.Text = Math.Round(serviceCost * serviceQuantity, 2).ToString("f2");

                        for (int i = 1; i < 5; i++)
                        {
                            tbl.Rows[contentCounter].Cells[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                            tbl.Rows[contentCounter].Cells[2].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                            tbl.Rows[contentCounter].Cells[3].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                            tbl.Rows[contentCounter].Cells[4].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;
                            tbl.Rows[contentCounter].Cells[i].Range.Font.Name = "Segoe UI";
                            tbl.Rows[contentCounter].Cells[i].Range.Font.Size = 8;
                            tbl.Rows[contentCounter].Cells[i].Range.ParagraphFormat.LeftIndent = 0;
                            tbl.Rows[contentCounter].Cells[i].Range.ParagraphFormat.RightIndent = 0;
                            tbl.Rows[contentCounter].Cells[i].Range.ParagraphFormat.LineSpacingRule = Word.WdLineSpacing.wdLineSpaceSingle;
                            tbl.Rows[contentCounter].Cells[i].Range.ParagraphFormat.SpaceAfter = 2f;
                        }
                        contentCounter++;
                    }

                    // Итоговая строка таблицы услуг
                    var tblOverallRow = tbl.Rows.Add();
                    tblOverallRow.Range.Shading.BackgroundPatternColor = Word.WdColor.wdColorGray10;
                    tblOverallRow.Cells[1].Range.Text = "Итого оказано услуг";
                    tblOverallRow.Cells[1].Range.Bold = 3;
                    tblOverallRow.Cells[1].Range.Font.Name = "Arial";
                    tblOverallRow.Cells[1].Range.Font.Size = 8;
                    tblOverallRow.Cells[1].Range.ParagraphFormat.LeftIndent = 0;
                    tblOverallRow.Cells[1].Range.ParagraphFormat.RightIndent = 0;

                    tblOverallRow.Cells[4].Range.Text = $"{Convert.ToDouble(servicesTotalCostLabel.Content).ToString("f2")}";
                    tblOverallRow.Cells[4].Range.Bold = 3;
                    tblOverallRow.Cells[4].Range.Font.Name = "Arial";
                    tblOverallRow.Cells[4].Range.Font.Size = 8;
                    tblOverallRow.Cells[4].Range.ParagraphFormat.LeftIndent = 0;
                    tblOverallRow.Cells[4].Range.ParagraphFormat.RightIndent = 0;
                }

                // Формирование таблицы материалов (если есть)
                if (range.Find.Execute("{tableMaterials}") && materialsDictionary.Count > 0)
                {
                    range.Text = "";
                    int rowCount = materialsDictionary.Count;
                    Word.Table tbl = doc.Tables.Add(range, rowCount + 1, 4);

                    tbl.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    tbl.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;

                    // Заголовки таблицы материалов
                    for (int i = 1; i < 5; i++)
                    {
                        tbl.Rows[1].Cells[i].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                        tbl.Rows[1].Cells[i].Range.Bold = 3;
                        tbl.Rows[1].Cells[i].Range.Font.Name = "Arial";
                        tbl.Rows[1].Cells[i].Range.Font.Size = 8;
                        tbl.Rows[1].Cells[i].Range.ParagraphFormat.LeftIndent = 0;
                        tbl.Rows[1].Cells[i].Range.ParagraphFormat.RightIndent = 0;
                    }
                    tbl.Rows[1].Cells[1].Range.Text = "Наименование материала";
                    tbl.Rows[1].Cells[2].Range.Text = "Единица измерения";
                    tbl.Rows[1].Cells[3].Range.Text = "Количество";
                    tbl.Rows[1].Cells[4].Range.Text = "Сумма (руб.)";

                    // Заполнение таблицы материалами
                    int contentCounter = 2;
                    foreach (var el in materialsDictionary)
                    {
                        double materialCost = Convert.ToDouble(el.Value.Row.ItemArray[2]);
                        double materialQuantity = Convert.ToInt32(el.Value.Row.ItemArray[1]);
                        tbl.Rows[contentCounter].Cells[1].Range.Text = el.Value.Row.ItemArray[0].ToString();
                        tbl.Rows[contentCounter].Cells[2].Range.Text = el.Value.Row.ItemArray[4].ToString();
                        tbl.Rows[contentCounter].Cells[3].Range.Text = el.Value.Row.ItemArray[1].ToString();
                        tbl.Rows[contentCounter].Cells[4].Range.Text = Math.Round(materialCost * materialQuantity, 2).ToString("f2");

                        for (int i = 1; i < 5; i++)
                        {
                            tbl.Rows[contentCounter].Cells[1].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                            tbl.Rows[contentCounter].Cells[2].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                            tbl.Rows[contentCounter].Cells[3].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                            tbl.Rows[contentCounter].Cells[4].Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphRight;
                            tbl.Rows[contentCounter].Cells[i].Range.Font.Name = "Segoe UI";
                            tbl.Rows[contentCounter].Cells[i].Range.Font.Size = 8;
                            tbl.Rows[contentCounter].Cells[i].Range.ParagraphFormat.LeftIndent = 0;
                            tbl.Rows[contentCounter].Cells[i].Range.ParagraphFormat.RightIndent = 0;
                            tbl.Rows[contentCounter].Cells[i].Range.ParagraphFormat.LineSpacingRule = Word.WdLineSpacing.wdLineSpaceSingle;
                            tbl.Rows[contentCounter].Cells[i].Range.ParagraphFormat.SpaceAfter = 2f;
                        }
                        contentCounter++;
                    }

                    // Итоговая строка таблицы материалов
                    var tblOverallRow = tbl.Rows.Add();
                    tblOverallRow.Range.Shading.BackgroundPatternColor = Word.WdColor.wdColorGray10;
                    tblOverallRow.Cells[1].Range.Text = "Итого материалов";
                    tblOverallRow.Cells[1].Range.Bold = 3;
                    tblOverallRow.Cells[1].Range.Font.Name = "Arial";
                    tblOverallRow.Cells[1].Range.Font.Size = 8;
                    tblOverallRow.Cells[1].Range.ParagraphFormat.LeftIndent = 0;
                    tblOverallRow.Cells[1].Range.ParagraphFormat.RightIndent = 0;

                    tblOverallRow.Cells[4].Range.Text = $"{Convert.ToDouble(materialsTotalCostLabel.Content).ToString("f2")}";
                    tblOverallRow.Cells[4].Range.Bold = 3;
                    tblOverallRow.Cells[4].Range.Font.Name = "Arial";
                    tblOverallRow.Cells[4].Range.Font.Size = 8;
                    tblOverallRow.Cells[4].Range.ParagraphFormat.LeftIndent = 0;
                    tblOverallRow.Cells[4].Range.ParagraphFormat.RightIndent = 0;

                    ReplaceWord("{totalMaterialsCost}", $"Затрачено материалов на выполнение услуг на сумму: {Convert.ToDouble(materialsTotalCostLabel.Content).ToString("f2")} руб.", doc);
                }
                else
                {
                    ReplaceWord("{tableMaterials}", "", doc);
                    ReplaceWord("{totalMaterialsCost}", "", doc);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось подготовить к печати заказ-наряд\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                wordApp.Visible = true;  // Показать Word с готовым документом
            }
        }

        /// <summary>
        /// Замена текста в документе Word
        /// </summary>
        private void ReplaceWord(string src, string dest, Word.Document doc)
        {
            Word.Range range = doc.Content;
            range.Find.Execute(FindText: src, ReplaceWith: dest);
        }
    }
}