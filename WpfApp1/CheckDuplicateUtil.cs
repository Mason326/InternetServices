using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    class CheckDuplicateUtil
    {
        public static bool HasNoDuplicate(string tableName, string fieldName, string inputValue)
        {
            try
            {
                string[] removedMultipleSpacesArray = inputValue.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string removedMultipleSpaces = string.Join(" ", removedMultipleSpacesArray);
                string trimmedInputValue = removedMultipleSpaces.Trim();
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand($"Select * from `{tableName}` where trim({fieldName}) = '{trimmedInputValue}'", conn);
                    object duplicateId = cmd.ExecuteScalar();
                    if (duplicateId != null)
                        return false;
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static int HasNoDuplicate(string tableName, string fieldName, string inputValue, bool fieldNameIsAnExpression)
        {
            try
            {
                string[] removedMultipleSpacesArray = inputValue.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string removedMultipleSpaces = string.Join(" ", removedMultipleSpacesArray);
                string trimmedInputValue = removedMultipleSpaces.Trim();
                using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
                {
                    conn.Open();
                    string query = $"Select * from `{tableName}` where trim(`{fieldName}`) = '{trimmedInputValue}'";
                    if (fieldNameIsAnExpression)
                        query = query.Replace("`", "");
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    object clientId = cmd.ExecuteScalar();
                    if (clientId == null)
                        return -1;
                    return Convert.ToInt32(clientId);
                }
            }
            catch
            {
                return -1;
            }
        }
    }
}
