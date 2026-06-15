using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    /// <summary>
    /// Класс-держатель выбранных доп. услуг
    /// </summary>
    class AdditionalServicesHolder
    {
        // Наименования не должны повторяться
        public static Dictionary<string, DataRowView> additionalServices = new Dictionary<string, DataRowView>();
    }
}
