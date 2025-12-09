using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    class RecordsCounter
    {
        public static int CountRecords(string tableName)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"Select Count(*) from `{tableName}`", conn);
                    object countRecords = cmd.ExecuteScalar();
                    if (countRecords != null)
                        return Convert.ToInt32(countRecords);
                    else
                        return 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        public static int CountRecords(string tableName, string condition)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"Select Count(*) from `{tableName}` {condition}", conn);
                    object countRecords = cmd.ExecuteScalar();
                    if (countRecords != null)
                        return Convert.ToInt32(countRecords);
                    else
                        return 0;
                }
            }
            catch
            {
                return 0;
            }
        }
    }
}
