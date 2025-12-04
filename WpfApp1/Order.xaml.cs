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
using MySql.Data.MySqlClient;

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
        DataTable dtAddServices = new DataTable();
        Dictionary<string, DataRowView> addServicesDictionary = new Dictionary<string, DataRowView>();
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
            dtAddServices.Columns.Add("additional_service_name", typeof(string));
            dtAddServices.Columns.Add("cost", typeof(double));
            dtAddServices.Columns.Add("additional_service_id", typeof(int));
            dtMaterials.Columns.Add("material_name", typeof(string));
            dtMaterials.Columns.Add("count", typeof(int));
            dtMaterials.Columns.Add("cost", typeof(double));
            dtMaterials.Columns.Add("material_id", typeof(int));
            materialsTotalCostLabel.Content = 0;
            servicesTotalCostLabel.Content = 0;
            orderTotalCostLabel.Content = 0;
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
                    dr.ItemArray = new object[] { items[1].ToString(), ++newCount, items[3].ToString(), items[0] };
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
                    dr.ItemArray = new object[] { items[1].ToString(), 1, items[3], items[0] };
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
                    dr.ItemArray = new object[] { items[1].ToString(), ++newCount, items[3], items[0] };
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
                    dr.ItemArray = new object[] { items[1].ToString(), 1, items[3], items[0] };
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
            }
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();
                string cmdText = $"INSERT INTO `order`(`idorder`, `orderDate`, `totalCost`, `connection_claim_id`) VALUE ({numberOrderLabel.Content}, {DateTime.Now.Date}, total_Cost_delai);";
                cmdText += "INSERT INTO `services_pack` VALUES ";
                foreach (var el in servicesDictionary)
                {
                    DataRowView drv = el.Value;
                    int serviceId = Convert.ToInt32(drv.Row.ItemArray[0]);
                    cmdText += $"({serviceId}, {numberOrderLabel.Content}, {el.Value[1]}),";
                }
                try
                {
                    cmdText = cmdText.TrimEnd(new char[] { ',' });
                    MySqlCommand cmd = new MySqlCommand($"{cmdText};", conn);
                    cmd.Transaction = transaction;
                    cmd.ExecuteNonQuery();
                    transaction.Commit();
                    MessageBox.Show("Робит");
                }
                catch (Exception exc)
                {
                    transaction.Rollback();
                    MessageBox.Show($"Не удалось загрузить материалы\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

}