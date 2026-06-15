using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    /// <summary>
    /// Класс держатель данных текущего пользователя
    /// </summary>
    public class AccountHolder
    {
        public static int userId { get; set; }
        public static string FIO { get; set; }
        public static string UserLogin { get; set; }
        public static string UserPassword { get; set; }
        public static string UserRole { get; set; }
    }
}
