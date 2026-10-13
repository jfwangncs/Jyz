using System.ComponentModel;
using System.Reflection;
using Jyz.Resources;

namespace Jyz.Extensions
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());

            var key = $"{value.GetType().Name}.{value}";
            var localized = EnumResources.GetString(key);
            if (!string.IsNullOrEmpty(localized))
                return localized;

            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }
    }
}
