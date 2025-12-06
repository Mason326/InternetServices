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

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Window19.xaml
    /// </summary>
    public partial class Order : Window
    {
        int claimId;
        DataTable dtServices = new DataTable();
        Dictionary<string, DataRowView> servicesDictionary = new Dictionary<string, DataRowView>();
        DataTable dtMaterials = new DataTable();
        Dictionary<string, DataRowView> materialsDictionary = new Dictionary<string, DataRowView>();
        public Order(int claimIdentifier)
        {
            InitializeComponent();
            claimId = claimIdentifier;
            dtServices.Columns.Add("service_name", typeof(string));
            dtServices.Columns.Add("count", typeof(int));
            dtServices.Columns.Add("cost", typeof(double));
            dtServices.Columns.Add("service_id", typeof(int));
            dtServices.Columns.Add("units", typeof(string));
            dtMaterials.Columns.Add("material_name", typeof(string));
            dtMaterials.Columns.Add("count", typeof(int));
            dtMaterials.Columns.Add("cost", typeof(double));
            dtMaterials.Columns.Add("material_id", typeof(int));
            dtMaterials.Columns.Add("units", typeof(string));
            materialsTotalCostLabel.Content = 0;
            servicesTotalCostLabel.Content = 0;
            orderTotalCostLabel.Content = 0;
            discountAmountLabel.Content = 0;
        }



        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            numberOrderLabel.Content = GetOrderNumber();
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

            FillServicesDG("");
            FillMaterialsDG("");
        }
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
                return 1;
            }
        }

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

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (servicesDG.SelectedItem != null)
            {
                var drv = servicesDG.SelectedItem as DataRowView;
                var items = drv.Row.ItemArray;
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
                else
                {
                    DataRow dr = dtServices.NewRow();
                    dr.ItemArray = new object[] { items[1].ToString(), 1, items[3], items[0], items[2].ToString() };
                    dtServices.Rows.Add(dr);
                    DataRowView addedToOrderDg = dtServices.DefaultView[dtServices.Rows.IndexOf(dr)];
                    servicesDictionary.Add(items[1].ToString(), addedToOrderDg);
                    orderServiceDG.Items.Add(addedToOrderDg);
                }

                double servicesCost = Convert.ToDouble(servicesTotalCostLabel.Content);
                servicesCost += Convert.ToDouble(items[3]);
                servicesTotalCostLabel.Content = servicesCost;

                double materialsCost = Convert.ToDouble(materialsTotalCostLabel.Content);

                orderTotalCostLabel.Content = servicesCost + materialsCost;
                RefreshDiscountLabel();
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (materialsDG.SelectedItem != null)
            {
                var drv = materialsDG.SelectedItem as DataRowView;
                var items = drv.Row.ItemArray;
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
                else
                {
                    DataRow dr = dtMaterials.NewRow();
                    dr.ItemArray = new object[] { items[1].ToString(), 1, items[3], items[0], items[2].ToString() };
                    dtMaterials.Rows.Add(dr);
                    DataRowView addedToOrderDg = dtMaterials.DefaultView[dtMaterials.Rows.IndexOf(dr)];
                    materialsDictionary.Add(items[1].ToString(), addedToOrderDg);
                    orderMaterials.Items.Add(addedToOrderDg);
                }
                double materialCost = Convert.ToDouble(materialsTotalCostLabel.Content);
                materialCost += Convert.ToDouble(items[3]);
                materialsTotalCostLabel.Content = materialCost;

                double servicesCost = Convert.ToDouble(servicesTotalCostLabel.Content);

                orderTotalCostLabel.Content = servicesCost + materialCost;
                RefreshDiscountLabel();
            }
        }

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
                    if (--newCount >= 1)
                    {
                        drv.Row.SetField<int>(1, newCount);
                        drv.Row.AcceptChanges();
                        servicesDictionary.Remove(items[0].ToString());
                        servicesDictionary.Add(items[0].ToString(), drv);
                    }
                    else
                    {
                        orderServiceDG.Items.Remove(drv);
                        servicesDictionary.Remove(items[0].ToString());
                    }
                }
                double servicesCost = Convert.ToDouble(servicesTotalCostLabel.Content);

                if (servicesCost != 0)
                {
                    servicesCost -= Convert.ToDouble(items[2]);
                    servicesTotalCostLabel.Content = servicesCost;
                }
                double materialsCost = Convert.ToDouble(materialsTotalCostLabel.Content);

                orderTotalCostLabel.Content = servicesCost + materialsCost;
                RefreshDiscountLabel();
            }
        }

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
                    if (--newCount >= 1)
                    {
                        drv.Row.SetField<int>(1, newCount);
                        drv.Row.AcceptChanges();
                        materialsDictionary.Remove(items[0].ToString());
                        materialsDictionary.Add(items[0].ToString(), drv);
                    }
                    else
                    {
                        orderMaterials.Items.Remove(drv);
                        materialsDictionary.Remove(items[0].ToString());
                    }
                }
                double materialCost = Convert.ToDouble(materialsTotalCostLabel.Content);
                if (materialCost != 0)
                {
                    materialCost -= Convert.ToDouble(items[2]);
                    materialsTotalCostLabel.Content = materialCost;
                }

                double servicesCost = Convert.ToDouble(servicesTotalCostLabel.Content);

                orderTotalCostLabel.Content = servicesCost + materialCost;
                RefreshDiscountLabel();
            }
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();
                if (servicesDictionary.Count < 1 && materialsDictionary.Count < 1)
                {
                    MessageBox.Show($"В заказ-наряде должны быть оказаны указаны выполненные услуги и затраченные материалы", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                string cmdText = $"INSERT INTO `order`(`idorder`, `orderDate`, `totalCost`, `connection_claim_id`) VALUE ({numberOrderLabel.Content}, '{DateTime.Now.Date.ToString("yyyy-MM-dd")}', {orderTotalCostLabel.Content}, {claimId});";
                
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
                    cmdText += $"Update `connection_claim` SET `claim_status_id` = (Select `idclaim_status` from `claim_status` where `status` = 'Закрыта'), `order_id` = {numberOrderLabel.Content} where `id_claim` = {claimId}";
                    MySqlCommand cmd = new MySqlCommand($"{cmdText};", conn);
                    cmd.Transaction = transaction;
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    MessageBox.Show($"Наряд успешно закрыт", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception exc)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Не удалось закрыть наряд\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void discountCheckBox_Click(object sender, RoutedEventArgs e)
        {
            RefreshDiscountLabel();
        }

        private void RefreshDiscountLabel()
        {
            double currentOrderCost = Convert.ToDouble(orderTotalCostLabel.Content);
            bool checkedFlag = discountCheckBox.IsChecked.HasValue && discountCheckBox.IsChecked.Value;
            double discount = currentOrderCost * 0.15;
            discountAmountLabel.Content = checkedFlag ? Math.Round(discount, 3) : 0;
            orderTotalCostLabel.Content = checkedFlag ? Math.Round(currentOrderCost - discount, 3) : Math.Round(Convert.ToDouble(servicesTotalCostLabel.Content) + Convert.ToDouble(materialsTotalCostLabel.Content), 3);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (servicesDictionary.Count < 1)
            {
                MessageBox.Show("В наряде должны быть выбраны выполненные услуги", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string fileName = Directory.GetCurrentDirectory();
            if (fileName.Contains("bin\\"))
            {
                fileName = string.Join("\\", fileName.Split('\\').TakeWhile(el => el != "bin"));
            }
            fileName += "\\Resources\\Templates\\ActTemplate.doc";
            Word.Application wordApp = new Word.Application();
            wordApp.Visible = false;

            try
            {
                Word.Document doc = wordApp.Documents.Open(fileName, ReadOnly: false);
                Word.Range range = doc.Content;

                ReplaceWord("{orderNumber}", numberOrderLabel.Content.ToString(), doc);
                ReplaceWord("{orderDate}", (DateTime.Parse(executionDateLabel.Content.ToString())).ToString("dd.MM.yyyy"), doc);
                ReplaceWord("{companyName}", Properties.Settings.Default.companyName, doc);
                ReplaceWord("{companyDescription}", Properties.Settings.Default.companyDescription, doc);
                ReplaceWord("{clientName}", clientLabel.Content.ToString(), doc);
                string address = string.Join(", ", mountAddressTextBox.Text.Split(new string[] { ", ", "\t,", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(el => el.Trim()));
                ReplaceWord("{mountAddress}", address, doc);
                ReplaceWord("{totalServicesCost}", Convert.ToDouble(servicesTotalCostLabel.Content).ToString("f2"), doc);
                ReplaceWord("{companyName}", Properties.Settings.Default.companyName, doc);
                if (discountCheckBox.IsChecked.HasValue && discountCheckBox.IsChecked.Value)
                    ReplaceWord("{discountAmount}", $"Размер скидки: {discountAmountLabel.Content} руб.", doc);
                else
                    ReplaceWord("{discountAmount}", "", doc);
                ReplaceWord("{totalOrderCost}", orderTotalCostLabel.Content.ToString(), doc);

                if (range.Find.Execute("{tableServices}"))
                {
                    range.Text = "";
                    int rowCount = servicesDictionary.Count;
                    Word.Table tbl = doc.Tables.Add(range, rowCount + 1, 4);

                    tbl.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    tbl.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
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

                if (range.Find.Execute("{tableMaterials}") && materialsDictionary.Count > 0)
                {
                    range.Text = "";
                    int rowCount = materialsDictionary.Count;
                    Word.Table tbl = doc.Tables.Add(range, rowCount + 1, 4);

                    tbl.Borders.InsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
                    tbl.Borders.OutsideLineStyle = Word.WdLineStyle.wdLineStyleSingle;
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
            finally
            {
                // в конце делаем документ видимым
                wordApp.Visible = true;
            }

        }
        private void ReplaceWord(string src, string dest, Word.Document doc)
        {
            Word.Range range = doc.Content;
            range.Find.Execute(FindText: src, ReplaceWith: dest);
        }
    }
}