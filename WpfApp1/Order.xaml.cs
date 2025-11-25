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
        public Order(int claimIdentifier)
        {
            InitializeComponent();
            claimId = claimIdentifier;
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

            FillServicesDG();
            FillAdditionalServicesDG();
            FillMaterials();
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

        private void FillServicesDG()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"SELECT * FROM `services`;", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    cmd.ExecuteNonQuery();
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    servicesDG.ItemsSource = dt.AsDataView();
                }

            }
            catch(Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить услуги\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillAdditionalServicesDG()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"SELECT * FROM `additional_services`;", conn);
                    MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                    cmd.ExecuteNonQuery();
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    additionalServicesDG.ItemsSource = dt.AsDataView();
                }

            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось загрузить дополнительные услуги\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillMaterials()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($@"SELECT * FROM `materials`;", conn);
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
    }

}