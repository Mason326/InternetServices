using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for Window20.xaml
    /// </summary>
    public partial class Contract : Window
    {
        object[] fieldVals;
        public Contract(object[] fieldValues)
        {
            InitializeComponent();
            fieldVals = fieldValues;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            contractNumberLabel.Content = GetContractNumber();
            contractDateLabel.Content = DateTime.Now.ToString("dd.MM.yyyy");
            claimNumberLabel.Content = fieldVals[0];
            var clientDescription = GetClientFullName(Convert.ToInt32(fieldVals[0]));
            claimСreationDateLabel.Content = fieldVals[1];
            claimExecutionDateLabel.Content = fieldVals[2];
            claimAddressTextBox.Text = fieldVals[3].ToString();
            tariffLabel.Content = fieldVals[4];
            claimClientLabel.Content = fieldVals[5];
            claimManagerLabel.Content = fieldVals[6];
            claimClientLabel.Content = clientDescription.full_name;
            abonentLoginTextBox.Text = clientDescription.login;
            abonentPasswordTextBox.Text = clientDescription.password;
            claimStatusLabel.Content = fieldVals[7];
            contractStatusComboBox.ItemsSource = new string[] { "Заключен" };
            contractStatusComboBox.SelectedItem = "Заключен";
        }

        private (string full_name, string login, string password) GetClientFullName(int claimId)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"SELECT `client`.full_name, `client`.subscriber_login, `client`.subscriber_password FROM connection_claim inner join `client` on `client`.idclient = connection_claim.client_id where id_claim = {claimId};", conn);
                    object[] clientDescr = new object[3];
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            dr.GetValues(clientDescr);
                        }
                    }
                    return (clientDescr[0].ToString(), clientDescr[1].ToString(), clientDescr[2].ToString());
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось получить данные клиента\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return ("", "", "");
                }
            }
        }

        private int GetContractNumber()
        {
            // TODO check status
            if (CheckDuplicateUtil.HasNoDuplicate("contract", "connection_claim_id", $"{fieldVals[0]}"))
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    try
                    {
                        conn.Open();
                        MySqlCommand cmd = new MySqlCommand($"SELECT max(idcontract) FROM contract;", conn);
                        int contractId = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
                        return contractId;
                    }
                    catch
                    {
                        return 1;
                    }
                }
            }
            else
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    try
                    {
                        conn.Open();
                        MySqlCommand cmd = new MySqlCommand($"SELECT idcontract FROM contract where connection_claim_id = {fieldVals[0]};", conn);
                        int contractId = Convert.ToInt32(cmd.ExecuteScalar());
                        return contractId;
                    }
                    catch
                    {
                        return 1;
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (CheckDuplicateUtil.HasNoDuplicate("contract", "connection_claim_id", $"{claimNumberLabel.Content}"))
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    try
                    {
                        conn.Open();
                        MySqlCommand cmd = new MySqlCommand($"Insert into `contract`(idcontract, contract_date, connection_claim_id, contract_status_id) Value ({contractNumberLabel.Content}, '{DateTime.Parse(contractDateLabel.Content.ToString()).ToString("yyyy-MM-dd")}', {claimNumberLabel.Content}, (SELECT idcontract_status FROM contract_status where `status` = 'Заключен'));", conn);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show($"Договор успешно оформлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show($"Не удалось оформить договор\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show($"Не удалось оформить договор. Обнаружен дубликат", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            string fileName = Directory.GetCurrentDirectory();
            if (fileName.Contains("bin\\"))
            {
                fileName = string.Join("\\", fileName.Split('\\').TakeWhile(el => el != "bin"));
            }
            fileName += "\\Resources\\Templates\\ContractTemplate.docx";
            Word.Application wordApp = new Word.Application();
            wordApp.Visible = false;

            Word.Document wordDocument = wordApp.Documents.Open(fileName, ReadOnly: true);

            ReplaceWord("{contractNumber}", contractNumberLabel.Content.ToString(), wordDocument);
            ReplaceWord("{contractDate}", contractDateLabel.Content.ToString(), wordDocument);
            ReplaceWord("{companyName}", Properties.Settings.Default.companyName, wordDocument);
            ReplaceWord("{companyDirector}", Properties.Settings.Default.companyDirector, wordDocument);
            ReplaceWord("{abonentFullName}", claimClientLabel.Content.ToString(), wordDocument);
            //ReplaceWord("{orderStatus}", "Сформирован", wordDocument);
            //ReplaceWord("{deliveryDate}", completionDate.Content.ToString(), wordDocument);
            //ReplaceWord("{orderSum}", totalOrderCost.ToString(), wordDocument);

            wordApp.Visible = true;
        }

        void ReplaceWord(string src, string dest, Word.Document doc)
        {
            Word.Range range = doc.Content; // всё содержимое 

            //range.Find.ClearFormatting();
            range.Find.Execute(FindText: src, ReplaceWith: dest);
        }
    }
}
