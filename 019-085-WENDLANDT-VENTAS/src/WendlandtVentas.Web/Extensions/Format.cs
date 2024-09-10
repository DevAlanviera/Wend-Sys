using System;
using System.Globalization;

namespace WendlandtVentas.Web.Extensions
{
    public static class Format
    {
        private static readonly string formatCurrency = "C";
        private static readonly string formatCommas = "{0:n0}";
        private static readonly string formaNullableTwoDecimals = "{0:#,##0.##}";
        private static readonly string formaTwoDecimals = "{0:n}";
        private static readonly string formatLong = "dddd, MMMM dd, yyyy";
        private static readonly string formatShort = "dd/MM/yyy";

        public static string FormatCommasNullableTwoDecimals(this double number)
        {
            return string.Format(formaNullableTwoDecimals, number);
        }
        public static string FormatCommasTwoDecimals(this double number)
        {
            return string.Format(formaTwoDecimals, number);
        }
        public static string FormatCurrency(this decimal number)
        {
            return number.ToString(formatCurrency);
        }
        public static string FormatCommasNullableTwoDecimals(this decimal number)
        {
            return string.Format(formaNullableTwoDecimals, number);
        }
        public static string FormatCommasTwoDecimals(this decimal number)
        {
            return string.Format(formaTwoDecimals, number);
        }
        public static string FormatCommas(this int number)
        {
            return string.Format(formatCommas, number);
        }
        public static string FormatDateLongMx(this DateTime date)
        {
            return date.ToString(formatLong, new CultureInfo("es-ES"));
        }
        public static string FormatDateShortMx(this DateTime date)
        {
            return date.ToString(formatShort, new CultureInfo("es-ES"));
        }
    }
}