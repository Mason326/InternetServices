using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MySql.Data.MySqlClient;

namespace WpfApp1.Utils
{
    public class Backup
    {
        public static void MakeABackup(object sender, EventArgs e)
        {
            string pathToProject = string.Join("\\", Directory.GetCurrentDirectory().Split('\\').TakeWhile(item => item != "bin"));
            if (!Directory.Exists($@"{pathToProject}\Resources\Backups\"))
                Directory.CreateDirectory($@"{pathToProject}\Resources\Backups\");
            using (MySqlConnection conn = new MySqlConnection(Connection.ConnectionString))
            {
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    using (MySqlBackup backup = new MySqlBackup(cmd))
                    {
                        conn.Open();
                        backup.ExportToFile($@"{pathToProject}\Resources\Backups\backup_{DateTime.Now.Ticks}.sql");
                    }
                }
            }
        }
    }
}
