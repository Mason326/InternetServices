using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    class FullNameSplitter
    {
        public static string MakeShortName(string fullName)
        {
            string[] fullNameByParts = fullName.Split(' ');
            string surname = fullNameByParts[0];
            char nameInitial = string.IsNullOrEmpty(fullNameByParts[1]) ? ' ' : fullNameByParts[1][0];
            char patronymicInitial = string.IsNullOrEmpty(fullNameByParts[2]) ? ' ' : fullNameByParts[2][0];
            return $"{surname} {nameInitial}.{patronymicInitial}.";
        }

        public static string HideClientName(string fullName)
        {
            string[] clientFio = fullName.Split(' ');
            string hiddenName = $"{clientFio[1]} {clientFio[2]} {clientFio[0][0]}.";
            return hiddenName;
        }
    }
}
