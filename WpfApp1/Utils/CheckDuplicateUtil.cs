using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    /// <summary>
    /// Класс проверки дубликатов
    /// </summary>
    class CheckDuplicateUtil
    {
        /// <summary>
        /// Проверка дубликата
        /// </summary>
        /// <param name="tableName">В какой таблице проверяем дубликаты</param>
        /// <param name="fieldName">Какое поле будем проверять</param>
        /// <param name="inputValue">Какое значение проверяется на дубликат</param>
        /// <returns>Логическое true / false</returns>
        public static bool HasNoDuplicate(string tableName, string fieldName, string inputValue)
        {
            try
            {
                // Убираем из ввода всё лишнее
                string[] removedMultipleSpacesArray = inputValue.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string removedMultipleSpaces = string.Join(" ", removedMultipleSpacesArray);
                string trimmedInputValue = removedMultipleSpaces.Trim();
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    // Пытаемся найти дубликат наименования
                    MySqlCommand cmd = new MySqlCommand($"Select * from `{tableName}` where trim({fieldName}) = '{trimmedInputValue}'", conn);
                    // Получаем ID существующей записи или null
                    object duplicateId = cmd.ExecuteScalar();
                    if (duplicateId != null)
                    { 
                        // Нашли дубликат
                        return false;
                    }
                    // Если не нашли дубликата
                    return true;
                }
            }
            catch
            {
                // По умолчанию дубликат найден
                return false;
            }
        }

        /// <summary>
        /// Проверка дубликата, если вводимые данные являются не значением, а SQL-выражением
        /// </summary>
        /// <param name="tableName">В какой таблице проверяем дубликаты</param>
        /// <param name="fieldName">Какое поле будем проверять</param>
        /// <param name="inputValue">Какое значение проверяется на дубликат</param>
        /// <param name="fieldNameIsAnExpression">Является ли значение SQL-выражением</param>
        /// <returns>ID дубликата или -1 если дубликата нет</returns>
        public static int HasNoDuplicate(string tableName, string fieldName, string inputValue, bool fieldNameIsAnExpression)
        {
            try
            {
                // Убираем всё лишнее
                string[] removedMultipleSpacesArray = inputValue.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string removedMultipleSpaces = string.Join(" ", removedMultipleSpacesArray);
                string trimmedInputValue = removedMultipleSpaces.Trim();
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    // Пытаемся найти дубликат наименования
                    string query = $"Select * from `{tableName}` where trim(`{fieldName}`) = '{trimmedInputValue}'";
                    // Является ли значение SQL-выражением
                    if (fieldNameIsAnExpression)
                        query = query.Replace("`", "");
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    // ID записи или null
                    object clientId = cmd.ExecuteScalar();
                    if (clientId == null)
                    {
                        // Дубликата нет
                        return -1;
                    }
                    // Дубликат есть (ID предоставлен)
                    return Convert.ToInt32(clientId);
                }
            }
            catch
            {
                // Дубликата нет
                return -1;
            }
        }

        /// <summary>
        /// Проверка дубликата с доп логикой для договоров
        /// </summary>
        /// <param name="tableName">В какой таблице проверяем дубликаты</param>
        /// <param name="fieldName">Какое поле будем проверять</param>
        /// <param name="inputValue">Какое значение проверяется на дубликат</param>
        /// <returns>Логическое true / false</returns>
        public static bool HasNoDuplicateContract(string tableName, string fieldName, string inputValue)
        {
            try
            {
                // Убираем всё лишнее
                string[] removedMultipleSpacesArray = inputValue.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string removedMultipleSpaces = string.Join(" ", removedMultipleSpacesArray);
                string trimmedInputValue = removedMultipleSpaces.Trim();
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    // Пытаемся найти дубликат
                    MySqlCommand cmd = new MySqlCommand($"Select idcontract, contract_date, connection_claim_id, contract_status.`status` from `{tableName}` inner join contract_status on contract_status_id = contract_status.idcontract_status where trim({fieldName}) = '{trimmedInputValue}'", conn);
                    using (MySqlDataReader dr = cmd.ExecuteReader())
                    {
                        object[] currentRecord = new object[dr.FieldCount];
                        // Ищем договоры по запросу
                        while(dr.Read())
                        {
                            dr.GetValues(currentRecord);
                            string status = currentRecord[3].ToString();
                            // Если выполнится значит есть заключенный договор, что является дубликатом
                            if (status == "Заключен")
                                return false;
                        }
                    }
                    // Дубликата нет
                    return true;
                }
            }
            catch
            {
                // Дубликат есть
                return false;
            }
        }
    }
}
