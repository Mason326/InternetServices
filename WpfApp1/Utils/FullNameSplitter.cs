using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    /// <summary>
    /// Класс для отображения иницалов ФИО и скрытия
    /// </summary>
    class FullNameSplitter
    {
        /// <summary>
        /// Метод для создания из полного ФИО - фамилии и инициалов
        /// </summary>
        /// <param name="fullName">Вводимое ФИО</param>
        /// <returns>Строка вида: Фамилия И.О.</returns>
        public static string MakeShortName(string fullName)
        {
            try
            {
                // Разбиваем ФИО
                string[] fullNameByParts = fullName.Split(' ');
                // Получаем фамилию
                string surname = fullNameByParts[0];
                // Получаем инициалы
                char nameInitial = string.IsNullOrEmpty(fullNameByParts[1]) ? ' ' : fullNameByParts[1][0];
                char patronymicInitial = string.IsNullOrEmpty(fullNameByParts[2]) ? ' ' : fullNameByParts[2][0];
                // Возвращаем строку вида: Фамилия И.О.
                return $"{surname} {nameInitial}.{patronymicInitial}.";
            }
            catch
            {
                // Если не удалось преобразовать, возвращаем как есть
                return fullName;
            }
        }

        /// <summary>
        /// Метод скрытия клиентского ФИО
        /// </summary>
        /// <param name="fullName">Вводимое ФИО</param>
        /// <returns>Строка вида: Имя Отчество Ф.</returns>
        public static string HideClientName(string fullName)
        {
            try
            {
                string[] clientFio = fullName.Split(' ');
                // Преобразовываем строку
                string hiddenName = $"{clientFio[1]} {clientFio[2]} {clientFio[0][0]}.";
                // Возвращаем
                return hiddenName;
            }
            catch
            {
                // Если не удалось преобразовать, возвращаем как есть
                return fullName;
            }
        }
    }
}
