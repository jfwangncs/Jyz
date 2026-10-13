using System.Globalization;
using System.Resources;

namespace Jyz.Resources
{
    /// <summary>
    /// Resolves localized enum descriptions from the embedded <c>EnumMessages</c> resources.
    /// Keys follow the "{EnumTypeName}.{MemberName}" format, e.g. "ResponseCode.Success".
    /// To add a new language, add a satellite resource file (e.g. EnumMessages.fr.resx)
    /// with the same keys and register the culture in AddCustomLocalization().
    /// </summary>
    public static class EnumResources
    {
        private static readonly ResourceManager ResourceManager =
            new($"{typeof(EnumResources).Namespace}.EnumMessages", typeof(EnumResources).Assembly);

        public static string? GetString(string key, CultureInfo? culture = null)
            => ResourceManager.GetString(key, culture ?? CultureInfo.CurrentUICulture);
    }
}
