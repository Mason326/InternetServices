using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;

namespace WpfApp1
{
    /// <summary>
    /// Класс соединения с базой данных
    /// </summary>
    public class Connection
    {
        // Параметры строки хранятся в Properties.Settings.Default
        static string server = Properties.Settings.Default.server;
        static string db = Properties.Settings.Default.database;
        static string user = Properties.Settings.Default.user;
        static string password = Properties.Settings.Default.password;
        public static string ConnectionString = $"server={server};user={user};password={password};database={db};AllowLoadLocalInfile=True;CharSet=utf8mb4";

        // Обновить соединение с базой данных
        public static void Refresh() {
            server = Properties.Settings.Default.server;
            db = Properties.Settings.Default.database;
            user = Properties.Settings.Default.user;
            password = Properties.Settings.Default.password;
            ConnectionString = $"server={server};user={user};password={password};database={db};AllowLoadLocalInfile=True;CharSet=utf8mb4";
        }
    }
}
