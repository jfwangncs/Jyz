namespace Jyz.Constants
{
    public static class RedisKey
    {
        public const string ApplicationName = "Jyz";
        public const string UserPermission = ApplicationName + ":USER_PERMISSION_{0}";
        public const string UserBlacklist = ApplicationName + ":USER_BLACKLIST_{0}";
    }
}
