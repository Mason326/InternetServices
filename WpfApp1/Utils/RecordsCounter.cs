using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    /// <summary>
    /// Класс подсчета записей
    /// </summary>
    class RecordsCounter
    {
        /// <summary>
        /// Метод посчета записей в таблице
        /// </summary>
        /// <param name="tableName">Название таблицы</param>
        /// <returns>Количество записей</returns>
        public static int CountRecords(string tableName)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    // Получаем количество записей
                    MySqlCommand cmd = new MySqlCommand($"Select Count(*) from `{tableName}`", conn);
                    object countRecords = cmd.ExecuteScalar();
                    // Проверяем удалось ли получить количество записей
                    if (countRecords != null)
                    {
                        // Возвращаем количество записей
                        return Convert.ToInt32(countRecords);
                    }
                    else
                    { 
                        // Если нет, значит записей 0
                        return 0;
                    }    
                }
            }
            catch
            {
                // Если ошибка, значит записей 0
                return 0;
            }
        }

        /// <summary>
        /// Метод подсчета записей с доп условием
        /// </summary>
        /// <param name="tableName">Название таблицы</param>
        /// <param name="condition">Доп условие</param>
        /// <returns>Количество записей</returns>
        public static int CountRecords(string tableName, string condition)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    // Получаем количество записей с учетом доп условий
                    MySqlCommand cmd = new MySqlCommand($"Select Count(*) from `{tableName}` {condition}", conn);
                    object countRecords = cmd.ExecuteScalar();
                    // Проверяем удалось ли получить количество записей
                    if (countRecords != null)
                    {
                        // Возвращаем количество записей
                        return Convert.ToInt32(countRecords);
                    }
                    else
                    {
                        // Если нет, значит записей 0
                        return 0;
                    }
                }
            }
            catch
            {
                // Если ошибка, значит записей 0
                return 0;
            }
        }
    }
}
