using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum enumValue)
    {
        if (enumValue == null) return "Desconocido";

        var member = enumValue.GetType()
            .GetMember(enumValue.ToString())
            .FirstOrDefault();

        var displayAttribute = member?
            .GetCustomAttribute<DisplayAttribute>();

        return displayAttribute?.GetName() ?? enumValue.ToString();
    }
}