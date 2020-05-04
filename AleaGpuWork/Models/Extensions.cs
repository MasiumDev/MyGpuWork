using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AleaGpuWork.Models
{
    public static class Extensions
    {
        public static MMTPNumber ToMmtpNumber(this string price)
        {
            var qmt = price.Replace(".", "");

            return new MMTPNumber
            {
                IFt = int.TryParse(qmt, out int num) && num > 0 ? (price.Length - price.IndexOf(".") - 1).ToString() : " ",
                QMt = qmt
            };
        }

        public static int ToHash(this string str)
        {
            var md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(str));
            return BitConverter.ToInt32(hashed, 0);
        }

        public static int[,] To2DArray<T>(this IEnumerable<T> sequence)
        {
            var props = typeof(T).GetProperties();

            var items = sequence.ToArray();

            var array = new int[ items.Length, props.Length];

            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];

                for (int j = 0; j < props.Length; j++)
                {
                    array[i, j] = int.Parse(props[j].GetValue(item).ToString());
                }
            }

            return array;
        }
    }
}