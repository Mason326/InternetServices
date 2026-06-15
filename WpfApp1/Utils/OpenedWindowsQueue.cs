using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp1.Utils
{
    /// <summary>
    /// Класс, хранящий очередь открытых окон для последовательного их закрытия
    /// </summary>
    public class OpenedWindowsQueue
    {
        // Очередь открытых окон
        public static Queue<Window> windows;
    }
}
