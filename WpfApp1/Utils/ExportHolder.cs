using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Utils
{
    public class ExportHolder
    {
        public enum ExportOptions { ExportPdf, ExportWord, CancelExport };

        public static ExportOptions exportOptions = ExportOptions.CancelExport;
    }
}
