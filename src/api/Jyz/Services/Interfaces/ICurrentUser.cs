namespace Jyz.Services.Interfaces
{
    public interface ICurrentUser
    {
        bool IsAuthenticated { get; }
        int? Id { get; }         
        string? Username { get; } 
        IReadOnlyList<string> Roles { get; }
        IReadOnlyList<string> Permissions { get; }
    }
}
