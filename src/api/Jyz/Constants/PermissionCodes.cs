namespace Jyz.Constants
{
    public static class PermissionCodes
    {
        public const string System = "system";

        public const string Role = "role";
        public const string RoleAdd = "role:add";
        public const string RoleGet = "role:get";
        public const string RoleEdit = "role:edit";
        public const string RoleAssign = "role:assign";

        public const string Permission = "permission";
        public const string PermissionGet = "permission:get";
        public const string PermissionAdd = "permission:add";
        public const string PermissionEdit = "permission:edit";
        public const string PermissionDelete = "permission:delete";
        public const string PermissionSync = "permission:sync";

        public const string User = "user";
        public const string UserGet = "user:get";
        public const string UserEdit = "user:edit";

        public const string AuditLog = "audit-log";
        public const string AuditLogGet = "audit-log:get";

        public const string LoginLog = "login-log";
        public const string LoginLogGet = "login-log:get";

        public const string DictType = "dict-type";
        public const string DictTypeGet = "dict-type:get";
        public const string DictTypeAdd = "dict-type:add";
        public const string DictTypeEdit = "dict-type:edit";

        public const string DictItem = "dict-item";
        public const string DictItemGet = "dict-item:get";
        public const string DictItemAdd = "dict-item:add";
        public const string DictItemEdit = "dict-item:edit";

        public const string RedisCache = "redis-cache";
        public const string RedisCacheGet = "redis-cache:get";
        public const string RedisCacheDelete = "redis-cache:delete";
    }
}
