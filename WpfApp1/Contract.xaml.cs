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
using System.Diagnostics;
using WpfApp1.Utils;
using Word = Microsoft.Office.Interop.Word;
using Microsoft.Win32;

namespace WpfApp1
{
    /// <summary>
    /// Форма "Договор" - создание и экспорт договора на основе заявки на подключение
    /// Позволяет оформить договор в системе, экспортировать его в Word или PDF
    /// Использует шаблон договора из ресурсов приложения
    /// </summary>
    public partial class Contract : Window
    {
        // Массив значений полей заявки
        object[] fieldVals;
        // Массив данных клиента
        object[] clientVerbose;

        /// <summary>
        /// Конструктор формы - инициализация компонентов, сохранение данных заявки
        /// </summary>
        /// <param name="fieldValues">Данные заявки, на основе которой создается договор</param>
        public Contract(object[] fieldValues)
        {
            InitializeComponent();
            fieldVals = fieldValues;
        }

        /// <summary>
        /// Событие загрузки формы - заполнение всех полей договора данными из заявки и БД
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

            // Заполнение основных полей договора
            contractNumberLabel.Content = GetContractNumber();          // Номер договора (следующий по порядку)
            contractDateLabel.Content = DateTime.Now.ToString("dd.MM.yyyy");  // Текущая дата
            claimNumberLabel.Content = fieldVals[0];                    // Номер заявки

            // Получение данных клиента
            var clientDescription = GetClientData(Convert.ToInt32(fieldVals[0]));
            clientVerbose = clientDescription;

            // Данные заявки
            claimСreationDateLabel.Content = DateTime.Parse(fieldVals[1].ToString()).ToString("dd.MM.yyyy");
            claimExecutionDateLabel.Content = DateTime.Parse(fieldVals[2].ToString()).ToString("dd.MM.yyyy");
            claimAddressTextBox.Text = fieldVals[3].ToString();
            tariffLabel.Content = fieldVals[4];
            claimClientLabel.Content = fieldVals[5];
            claimManagerLabel.Content = fieldVals[6];

            // Данные клиента для договора
            claimClientLabel.Content = clientDescription[1];            // ФИО клиента
            abonentLoginTextBox.Text = clientDescription[6].ToString(); // Логин абонента
            abonentPasswordTextBox.Text = clientDescription[7].ToString(); // Пароль абонента
            claimStatusLabel.Content = fieldVals[7];                    // Статус заявки
        }

        /// <summary>
        /// Получение данных клиента по ID заявки
        /// </summary>
        /// <param name="claimId">ID заявки</param>
        /// <returns>Массив с данными клиента</returns>
        private object[] GetClientData(int claimId)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    // JOIN для получения всех данных клиента, связанных с заявкой
                    MySqlCommand cmd = new MySqlCommand($"SELECT `idclient`, `full_name`, `email`, `phone_number`, `place_of_residence`, `birthdate`, `subscriber_login`, `subscriber_password`, `passport_series`, `passport_number`, `issued_by`, `issue_date`, `department_code`, `client_status_id` FROM connection_claim inner join `client` on `client`.idclient = connection_claim.client_id where id_claim = {claimId};", conn);
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        object[] clientDescr = new object[dr.FieldCount];
                        while (dr.Read())
                        {
                            dr.GetValues(clientDescr);
                        }
                        return clientDescr;
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось получить данные клиента\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new object[0];
                }
            }
        }

        /// <summary>
        /// Получение номера договора (максимальный ID в таблице + 1)
        /// Проверяет, существует ли уже договор для данной заявки
        /// </summary>
        /// <returns>Номер нового договора</returns>
        private int GetContractNumber()
        {
            // Проверка: существует ли договор для этой заявки
            if (CheckDuplicateUtil.HasNoDuplicateContract("contract", "connection_claim_id", $"{fieldVals[0]}"))
            {
                // Договора нет - создаем новый
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    try
                    {
                        conn.Open();
                        // Получение максимального ID договора
                        MySqlCommand cmd = new MySqlCommand($"SELECT max(idcontract) FROM contract;", conn);
                        int contractId = Convert.ToInt32(cmd.ExecuteScalar()) + 1;
                        contractStatusComboBox.ItemsSource = new string[] { "Не заключен" };
                        contractStatusComboBox.SelectedItem = "Не заключен";
                        formAContractButton.IsEnabled = true;  // Кнопка оформления активна
                        return contractId;
                    }
                    catch
                    {
                        // Если таблица пуста, начинаем с 1
                        contractStatusComboBox.ItemsSource = new string[] { "Не заключен" };
                        contractStatusComboBox.SelectedItem = "Не заключен";
                        formAContractButton.IsEnabled = true;
                        return 1;
                    }
                }
            }
            else
            {
                // Договор уже существует - только просмотр
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    try
                    {
                        conn.Open();
                        MySqlCommand cmd = new MySqlCommand($"SELECT idcontract FROM contract where connection_claim_id = {fieldVals[0]};", conn);
                        int contractId = Convert.ToInt32(cmd.ExecuteScalar());
                        contractStatusComboBox.ItemsSource = new string[] { "Заключен" };
                        contractStatusComboBox.SelectedItem = "Заключен";
                        formAContractButton.IsEnabled = false;  // Кнопка оформления неактивна
                        return contractId;
                    }
                    catch
                    {
                        return 1;
                    }
                }
            }
        }

        /// <summary>
        /// Кнопка "На главную" - закрытие формы
        /// </summary>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Кнопка "Оформить договор" - сохранение договора в базе данных
        /// </summary>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                try
                {
                    conn.Open();
                    // Вставка нового договора в БД
                    MySqlCommand cmd = new MySqlCommand($"Insert into `contract`(idcontract, contract_date, connection_claim_id, contract_status_id) Value ({contractNumberLabel.Content}, '{DateTime.Parse(contractDateLabel.Content.ToString()).ToString("yyyy-MM-dd")}', {claimNumberLabel.Content}, (SELECT idcontract_status FROM contract_status where `status` = 'Заключен'));", conn);
                    cmd.ExecuteNonQuery();
                    MessageBox.Show($"Договор успешно оформлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                catch (Exception exc)
                {
                    MessageBox.Show($"Не удалось оформить договор\nОшибка: {exc.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Кнопка "Экспорт договора" - запуск процесса экспорта
        /// </summary>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            try
            {
                ExportContract();
            }
            catch (Exception exc)
            {
                MessageBox.Show($"Не удалось подготовить договор для экпорта.\nОшибка: {exc.Message}", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Экспорт договора в Word или PDF с использованием шаблона
        /// Подставляет данные из заявки, клиента и настроек компании в шаблон
        /// </summary>
        private void ExportContract()
        {
            // Диалог выбора формата экспорта (Word или PDF)
            var win = new ExportAs();
            win.ShowDialog();

            if (ExportHolder.exportOptions == ExportHolder.ExportOptions.CancelExport)
                return;

            // Путь к шаблону договора
            string fileName = Directory.GetCurrentDirectory();
            if (fileName.Contains("bin\\"))
            {
                fileName = string.Join("\\", fileName.Split('\\').TakeWhile(el => el != "bin"));
            }
            fileName += "\\Resources\\Templates\\ContractTemplate.doc";

            Word.Application wordApp = new Word.Application();
            wordApp.Visible = false;  // Работаем в фоновом режиме

            // Открытие шаблона
            Word.Document wordDocument = wordApp.Documents.Open(fileName, ReadOnly: true);

            // Замена всех плейсхолдеров в документе на реальные данные
            ReplaceWord("{contractNumber}", contractNumberLabel.Content.ToString(), wordDocument);
            ReplaceWord("{contractDate}", contractDateLabel.Content.ToString(), wordDocument);
            ReplaceWord("{companyName}", Properties.Settings.Default.companyName, wordDocument);
            ReplaceWord("{companyDirector}", Properties.Settings.Default.companyDirector, wordDocument);
            ReplaceWord("{abonentFullName}", claimClientLabel.Content.ToString(), wordDocument);
            ReplaceWord("{abonentLogin}", abonentLoginTextBox.Text, wordDocument);
            ReplaceWord("{abonentPassword}", abonentPasswordTextBox.Text, wordDocument);
            ReplaceWord("{abonentEmail}", clientVerbose[2].ToString(), wordDocument);

            // Очистка адреса от лишних символов
            string address = string.Join(", ", claimAddressTextBox.Text.Split(new string[] { ", ", "\t,", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(el => el.Trim()));
            ReplaceWord("{connectionAddress}", address, wordDocument);
            ReplaceWord("{tariffName}", tariffLabel.Content.ToString(), wordDocument);
            ReplaceWord("{companyDescription}", Properties.Settings.Default.companyDescription, wordDocument);

            // Сокращение ФИО директора (Фамилия И.О.)
            string[] director = Properties.Settings.Default.companyDirector.Split();
            ReplaceWord("{companyDirector}", $"{director[0]} {director[1][0]}. {director[2][0]}.", wordDocument);

            // Паспортные данные и адреса клиента
            ReplaceWord("{birthDate}", DateTime.Parse(clientVerbose[5].ToString()).ToString("dd.MM.yyyy"), wordDocument);
            ReplaceWord("{passportSeries}", clientVerbose[8].ToString(), wordDocument);
            ReplaceWord("{passportNumber}", clientVerbose[9].ToString(), wordDocument);
            ReplaceWord("{issueDate}", DateTime.Parse(clientVerbose[11].ToString()).ToString("dd.MM.yyyy"), wordDocument);
            ReplaceWord("{issuedBy}", clientVerbose[10].ToString(), wordDocument);

            address = string.Join(", ", clientVerbose[4].ToString().Split(new string[] { ", ", "\t,", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(el => el.Trim()));
            ReplaceWord("{residenceAddress}", address, wordDocument);
            ReplaceWord("{phoneNumber}", clientVerbose[3].ToString(), wordDocument);

            address = string.Join(", ", fieldVals[3].ToString().Split(new string[] { ", ", "\t,", "\t" }, StringSplitOptions.RemoveEmptyEntries).Select(el => el.Trim()));
            ReplaceWord("{mountAddress}", address, wordDocument);

            // Экспорт в PDF
            if (ExportHolder.exportOptions == ExportHolder.ExportOptions.ExportPdf)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Title = "Сохранение...";
                saveFileDialog.Filter = "PDF-файл (*.pdf)|*.pdf";
                saveFileDialog.ShowDialog();

                if (saveFileDialog.FileName != string.Empty)
                {
                    string savePath = saveFileDialog.FileName;
                    wordDocument.SaveAs2(savePath, Word.WdSaveFormat.wdFormatPDF);
                    Process.Start(savePath);  // Открытие сохраненного файла
                }
                wordDocument?.Close(false);
                wordApp.Quit();
            }

            // Экспорт в Word (открыть для редактирования)
            if (ExportHolder.exportOptions == ExportHolder.ExportOptions.ExportWord)
            {
                wordApp.Visible = true;  // Показать Word с готовым документом
            }

            ExportHolder.exportOptions = ExportHolder.ExportOptions.CancelExport;  // Сброс выбора
        }

        /// <summary>
        /// Замена текста в документе Word
        /// </summary>
        /// <param name="src">Искомый текст (плейсхолдер)</param>
        /// <param name="dest">Текст для замены</param>
        /// <param name="doc">Документ Word</param>
        private void ReplaceWord(string src, string dest, Word.Document doc)
        {
            Word.Range range = doc.Content;
            range.Find.Execute(FindText: src, ReplaceWith: dest);
        }

        /// <summary>
        /// При изменении статуса договора - включение/выключение кнопки экспорта
        /// Экспорт доступен только для заключенных договоров
        /// </summary>
        private void contractStatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (contractStatusComboBox.SelectedItem)
            {
                case "Заключен":
                    exportAContractButton.IsEnabled = true;
                    break;
                case "Не заключен":
                    exportAContractButton.IsEnabled = false;
                    break;
            }
        }
    }
}