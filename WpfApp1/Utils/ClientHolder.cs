using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    /// <summary>
    /// Класс-держатель данных клиента
    /// </summary>
    class ClientHolder
    {
        // Данные клиента
        public static object[] data { get; set; } = null;
    }
}
