using System.ComponentModel;

namespace Jyz.Constants
{
    public enum ErrorCode
    {
        //unexpected  error 
        [Description("System error.")]
        SystemError = 1000,
        //client error
        [Description("Validation error.")]
        ValidationError = 4000,

        [Description("The token is unauthorized.")]
        UnauthorizedError,

        [Description("The resource is forbidden.")]
        ForbiddenError,

        [Description("The operation is failed.")]
        OperationFailed,

        #region Auth
        [Description("Invalid credentials.")]
        InvalidCredentials = 4100,

        [Description("User not found.")]
        UserNotFound,

        [Description("Account is disabled.")]
        AccountDisabled,

        [Description("Account is locked, please try again later.")]
        AccountLocked,

        [Description("Password not set, please login with phone number.")]
        PasswordNotSet,

        [Description("Phone number not bound, please login with username and password.")]
        PhoneNotBound,

        [Description("Invalid SMS verification code.")]
        InvalidSmsCode,

        [Description("SMS verification code has expired.")]
        SmsCodeExpired,

        [Description("WeChat authorization failed.")]
        WeChatAuthFailed,

        [Description("WeChat account not bound, please bind your WeChat first.")]
        WeChatNotBound,

        [Description("Account is not activated, please activate your account first.")]
        AccountNotActivated,

        [Description("Unsupported login method.")]
        UnsupportedLoginMethod,
        #endregion

        #region User
        [Description("Duplicate username.")]
        DuplicateUser = 4150,

        [Description("Old password is incorrect.")]
        InvalidOldPassword,
        #endregion

        #region Role
        [Description("Role not found.")]
        RoleNotFound = 4200,
        [Description("Duplicate role name.")]
        DuplicateRole,
        #endregion

        #region Permission
        [Description("Permission not found.")]
        PermissionNotFound = 4250,
        [Description("Duplicate permission code.")]
        DuplicatePermission,
        #endregion

        //internal service error
        [Description("The product is out of stock")]
        OutOfSotck = 4300,

        #region DictType
        [Description("Dict type not found.")]
        DictTypeNotFound = 4350,

        [Description("Duplicate dict type code.")]
        DuplicateDictTypeCode,
        #endregion

        #region DictItem
        [Description("Dict item not found.")]
        DictItemNotFound = 4360,

        [Description("Duplicate dict item code.")]
        DuplicateDictItemCode,
        #endregion
    }
}
