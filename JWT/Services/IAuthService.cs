using JWT.Models;
using JWT.Utitlies;
using System;

namespace JWT.Services {
    public interface IAuthService {
        Task<AuthModel> RegisterAsync(RegisterModel model);
        Task<AuthModel> LoginAsync(LoginModel model);
        Task<String> AddUserInRoleAsync(AddUserInRoleModel model);
        Task<AuthModel> RefreshTokenInDatabae(string Token);
        Task<bool> RevokeTokenInDatabae(string Token);
    }
}
