using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    /// <summary>
    /// Класс генерации паролей (логинов)
    /// </summary>
    class CredentialsGenerator
    {
        /// <summary>
        /// Метод перемешивает символы в массиве
        /// </summary>
        /// <param name="targetChars">Символы которые нужно перемешать</param>
        /// <returns>Массив перемешанных символов</returns>
        public static char[] MixChars(char[] targetChars)
        {
            // Используем рандом
            Random random = new Random();
            for (int i = 0; i < targetChars.Length; i++)
            {
                int fIndex = random.Next(0, targetChars.Length - 1);
                int sIndex = random.Next(0, targetChars.Length - 1);
                // Таким образом меняем символы местами
                (targetChars[fIndex], targetChars[sIndex]) = (targetChars[sIndex], targetChars[fIndex]);
            }
            return targetChars;
        }

        /// <summary>
        /// Метод генерирует пароли (логины)
        /// </summary>
        /// <param name="mixedChars">Массив символов</param>
        /// <returns></returns>
        public static string GenerateCredential(char[] mixedChars)
        {
            // Используем рандом
            Random random = new Random();
            // Количество символов всегда 8
            int lettersCount = 8;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lettersCount; i++)
            {
                // Создаем новую строку пароля
                sb.Append(mixedChars[random.Next(0, mixedChars.Length - 1)]);
            }
            // Строка с сгенерированным паролем
            return sb.ToString();
        }
    }
}
