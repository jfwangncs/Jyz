using JfYu.Data.Model;
using JfYu.Data.Service;
using Jyz.Entity;
using Jyz.Model;
using Jyz.Model.Request;
using Jyz.Model.User;

namespace Jyz.Services.Interfaces
{
    public interface IUserService : IService<User, AppDbContext>
    {
        Task<User?> LoginAsync(LoginRequest login);

        Task<PagedData<User>> GetPagedAsync(QueryRequest query);

        Task<bool> UpdateAsync(int userId, UpdateUserRequest request, CancellationToken cancellationToken = default);

        Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

        Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    }
}
