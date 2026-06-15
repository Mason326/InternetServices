using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Utils
{
    /// <summary>
    /// Класс-держатель параметров экпорта
    /// </summary>
    public class ExportHolder
    {
        /// <summary>
        ///  Параметры экспорта
        /// </summary>
        public enum ExportOptions { ExportPdf, ExportWord, CancelExport };

        public static ExportOptions exportOptions = ExportOptions.CancelExport;
    }
}
