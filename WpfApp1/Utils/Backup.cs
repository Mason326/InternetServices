using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MySql.Data.MySqlClient;

namespace WpfApp1.Utils
{
    /// <summary>
    /// Класс для резервного копирования (бэкапов)
    /// </summary>
    public class Backup
    {
        public static string MakeABackup()
        {
            // Дампы сохраняются в Resources\Backups
            string pathToProject = string.Join("\\", Directory.GetCurrentDirectory().Split('\\').TakeWhile(item => item != "bin"));
            // Если директории не существует, то создаем ее
            if (!Directory.Exists($@"{pathToProject}\Resources\Backups\"))
                Directory.CreateDirectory($@"{pathToProject}\Resources\Backups\");
            string fileName = $@"{pathToProject}\Resources\Backups\backup_{DateTime.Now.Ticks}.sql";
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    // Использование библиотеки-расширения MySqlBackup
                    using (MySqlBackup backup = new MySqlBackup(cmd))
                    {
                        conn.Open();
                        backup.ExportToFile(fileName);
                    }
                }
            }
            // Путь до файла-дампа возвращаем
            return fileName;
        }
    }
}
