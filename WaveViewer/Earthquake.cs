using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaveViewer
{
    public static class Earthquake
    {
        public static int ConvertPgaToMMI(double pga)
        {
            // NOTE: 기상청 MMI scale

            if (pga <= 0)
                return 0;
            if (pga < 0.07)
                return 1;
            if (pga < 0.23)
                return 2;
            if (pga < 0.76)
                return 3;
            if (pga < 2.56)
                return 4;
            if (pga < 6.86)
                return 5;
            if (pga < 14.73)
                return 6;
            if (pga < 31.66)
                return 7;
            if (pga < 68.01)
                return 8;
            if (pga < 146.14)
                return 9;
            if (pga < 314)
                return 10;
            return 11;
        }

        public static int ConvertPgvToMMI(double pgv)
        {
            // NOTE: 기상청 MMI scale

            if (pgv <= 0)
                return 0;
            if (pgv < 0.03)
                return 1;
            if (pgv < 0.07)
                return 2;
            if (pgv < 0.19)
                return 3;
            if (pgv < 0.54)
                return 4;
            if (pgv < 1.46)
                return 5;
            if (pgv < 3.7)
                return 6;
            if (pgv < 9.39)
                return 7;
            if (pgv < 23.85)
                return 8;
            if (pgv < 60.61)
                return 9;
            if (pgv < 154)
                return 10;
            return 11;
        }

        public static string MMIToString(int mmi)
        {
            string[] arr =
            {
                "I-", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII", "XII+"
            };

            if (mmi < 0)
                return arr.First();
            if (mmi >= arr.Length)
                return arr.Last();

            return arr[mmi];
        }
    }
}
