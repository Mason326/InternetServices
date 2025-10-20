using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    class CredentialsGenerator
    {
        public static char[] MixChars(char[] targetChars)
        {
            Random random = new Random();
            for (int i = 0; i < targetChars.Length; i++)
            {
                int fIndex = random.Next(0, targetChars.Length - 1);
                int sIndex = random.Next(0, targetChars.Length - 1);
                (targetChars[fIndex], targetChars[sIndex]) = (targetChars[sIndex], targetChars[fIndex]);
            }
            return targetChars;
        }

        public static string GenerateCredential(char[] mixedChars, bool isPassword)
        {
            Random random = new Random();
            int lettersCount = 12;
            if (isPassword)
                lettersCount = 16;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lettersCount; i++)
            {
                sb.Append(mixedChars[random.Next(0, mixedChars.Length - 1)]);
            }
            return sb.ToString();
        }
    }
}
