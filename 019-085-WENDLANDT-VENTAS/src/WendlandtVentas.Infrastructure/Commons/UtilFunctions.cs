using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace WendlandtVentas.Infrastructure.Commons
{
    public static class UtilFunctions
    {
        private static string GetDisplayName(this Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName);
            return (property ?? throw new InvalidOperationException()).GetCustomAttribute(typeof(DisplayAttribute)) is
                DisplayAttribute displayAttribute
                    ? displayAttribute.Name
                    : propertyName;
        }

        public static bool PropertiesAreEqual<T>(T self, T to, params string[] ignore) where T : class
        {
            if (self == null || to == null) return self == to;

            var type = typeof(T);
            var ignoreList = new List<string>(ignore);
            foreach (var propertyInfo in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (ignoreList.Contains(propertyInfo.Name)) continue;
                try
                {
                    var selfValue = type.GetProperty(propertyInfo.Name)?.GetValue(self, null);
                    var toValue = type.GetProperty(propertyInfo.Name)?.GetValue(to, null);

                    if (selfValue != toValue && (selfValue == null || !selfValue.Equals(toValue)))
                        return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }

        public static double DistanceTo(double lat1, double lon1, double lat2, double lon2)
        {
            double rlat1 = Math.PI * lat1 / 180;
            double rlat2 = Math.PI * lat2 / 180;
            double theta = lon1 - lon2;
            double rtheta = Math.PI * theta / 180;
            double dist =
                Math.Sin(rlat1) * Math.Sin(rlat2) + Math.Cos(rlat1) *
                Math.Cos(rlat2) * Math.Cos(rtheta);
            dist = Math.Acos(dist);
            dist = dist * 180 / Math.PI;
            dist = dist * 60 * 1.1515;
            return Math.Round(dist * 1.609344, 2) + .01;
        }

        public static DateTime ConvertToPacificTime(DateTime utcDate)
        {
            var est = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcDate, est);
        }

        //acortador de titulos
        public static string TrimTitle(string title, int limit)
        {
            if (!string.IsNullOrEmpty(title) && title.Length > limit)
            {
                return title.Substring(0, limit) + "...";
            }

            return title;
        }

        //da formato a un numero de telefono
        public static string FormatPhone(string telefono)
        {
            if (!String.IsNullOrEmpty(telefono))
            {
                return telefono.Insert(0, "(").Insert(4, ") ").Insert(9, "-");
            }
            else
            {
                return telefono;
            }
        }

        //quita el codigo html de un texto
        public static string RemoveHtmlTags(string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }
    }
}