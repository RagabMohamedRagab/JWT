using JWT.Models;
using JWT.Utitlies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

namespace JWT.Services {
    public class AuthService : IAuthService {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Jwt _jwt;
        public AuthService(UserManager<ApplicationUser> userManager, IOptions<Jwt> jwt, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _jwt = jwt.Value;
            _roleManager = roleManager;
        }
        public async Task<AuthModel> RegisterAsync(RegisterModel model)
        {
            if (await _userManager.FindByEmailAsync(model.Email) is not null)
                return new AuthModel { Message = "Email is Already Exist" };
            if (await _userManager.FindByNameAsync(model.UserName) is not null)
                return new AuthModel { Message = "UserName is Already Exist" };
            var user = new ApplicationUser()
            {
                Email = model.Email,
                UserName = model.UserName,
                FirstName = model.FirstName,
                LastName = model.LastName,
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = string.Empty;
                foreach (var error in result.Errors)
                {
                    errors += $"{error.Description}, ";
                }
                return new AuthModel() { Message = errors };
            }
            await _userManager.AddToRoleAsync(user, "User");
            var jwtSecuirtyToken = await CreateJwtToken(user);
            return new AuthModel()
            {
                Email = user.Email,
                Username = user.UserName,
                IsAuthenticated = true,
                //ExpiresIn = jwtSecuirtyToken.ValidTo,
                Roles = new List<string> { "User" },
                Token = new JwtSecurityTokenHandler().WriteToken(jwtSecuirtyToken),

            };
        }
        public async Task<AuthModel> LoginAsync(LoginModel model)
        {
            var Auth = new AuthModel();
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null || !await _userManager.CheckPasswordAsync(user, model.Password))
            {
                Auth.Message = "Email or Password InCorrect";
                return Auth;
            }
            var jwtSecuirtyToken = await CreateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);
            Auth.Email = model.Email;
            Auth.Username = user.UserName;
            Auth.Message = "SucessFully Login";
            Auth.IsAuthenticated = true;
            //Auth.ExpiresIn = jwtSecuirtyToken.ValidTo;
            Auth.Token = new JwtSecurityTokenHandler().WriteToken(jwtSecuirtyToken);
            Auth.Roles = roles.ToList();
            ///////// Method Refresh Token //////
            if(user.RefreshTokens.Any(t=>t.IsActive))
            {
                // ف حاله موجود توكن
                var activationToken = user.RefreshTokens.FirstOrDefault(c => c.IsActive);
                Auth.RefreshToken = activationToken.Token;
                Auth.RefreshTokenExpiretion = activationToken.ExpiresOn;
            }
            else
            {
                // ف حاله موجود مش توكن
                var refreshTokens = GeneratedRefreshToken();
                Auth.RefreshToken = refreshTokens.Token;
                Auth.RefreshTokenExpiretion=refreshTokens.ExpiresOn;
                user.RefreshTokens.Add(refreshTokens);
              await  _userManager.UpdateAsync(user);
            }
            return Auth;

        }
        public async Task<String> AddUserInRoleAsync(AddUserInRoleModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user is null || !await _roleManager.RoleExistsAsync(model.Role))
                return "User ID or Role are Not Valid";
            if (await _userManager.IsInRoleAsync(user, model.Role))
                return "User Aready in Role ";
            var result = await _userManager.AddToRoleAsync(user, model.Role);
            return result.Succeeded ? "" : "Something is not Valid";
        }
        private async Task<JwtSecurityToken> CreateJwtToken(ApplicationUser user)
        {
            var userclaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);
            var roleClaims = new List<Claim>();
            foreach (var role in roles)
            {
                roleClaims.Add(new Claim("roles", role));
            }
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Email,user.Email),
                new Claim("uid",user.Id)
            }
            .Union(userclaims)
            .Union(roleClaims);

            var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
            var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _jwt.IssUse,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.Now.AddSeconds(_jwt.DurationInDays),
                signingCredentials: signingCredentials
                 );

            return jwtSecurityToken;
        }
        public async Task<AuthModel> RefreshTokenInDatabae(string Token)
        {
            var authModel=new AuthModel();
            var user = await _userManager.Users.SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == Token));
            if(user is null)
            {
                authModel.IsAuthenticated = false;
                authModel.Message = "Token Not Found";
                return authModel;
            }
            var refreshToken = user.RefreshTokens.Single(t => t.Token == Token);
            if(!refreshToken.IsActive)
            {
                authModel.IsAuthenticated = false;
                authModel.Message = "InActive Token";
                return authModel;
            }
            refreshToken.RevokedOn = DateTime.UtcNow;
            var newRefreshToken = GeneratedRefreshToken();
            user.RefreshTokens.Add(newRefreshToken);
           await _userManager.UpdateAsync(user);
            var JwtToken =await CreateJwtToken(user);
            authModel.IsAuthenticated = true;
            authModel.Token = new JwtSecurityTokenHandler().WriteToken(JwtToken);
            authModel.Email = user.Email;
            authModel.RefreshTokenExpiretion = DateTime.UtcNow.AddDays(7);
            authModel.Message = "Success";
            authModel.Roles=await _userManager.GetRolesAsync(user);
            authModel.RefreshToken = newRefreshToken.Token;
            authModel.Username = user.UserName;
            return authModel;
        }
      public async  Task<bool> RevokeTokenInDatabae(string Token)
        {
            var user = await _userManager.Users.SingleOrDefaultAsync(u => u.RefreshTokens.Any(t => t.Token == Token));
            if (user is null)
            {
               
                return false;
            }
            var refreshToken = user.RefreshTokens.Single(t => t.Token == Token);
            if (!refreshToken.IsActive)
            {
                return false;
            }
            refreshToken.RevokedOn = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            return true;
        }
        private RefreshToken GeneratedRefreshToken()
        {
            var randomNumber = new byte[32];
            using var generator = new RNGCryptoServiceProvider();
            generator.GetBytes(randomNumber);
            return new RefreshToken()
            {
                Token = Convert.ToBase64String(randomNumber),
                CreatOn = DateTime.UtcNow,
                ExpiresOn = DateTime.UtcNow.AddDays(5)
            };
        }
    }
}
