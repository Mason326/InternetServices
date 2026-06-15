using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    /// <summary>
    /// Класс-держатель данных мастера
    /// </summary>
    class MasterHolder
    {
        // Данные мастера
        public static object[] data { get; set; } = null;
    }
}
