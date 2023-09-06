using JWT.Models;
using JWT.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JWT.Controllers {
    [Route("api/[controller]/[action]")]
    [ApiController]
    [Authorize ]
    public class AuthController : ControllerBase {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Register([FromBody]RegisterModel model)
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState);
            var result =await  _authService.RegisterAsync(model);
            if (!result.IsAuthenticated)
                return BadRequest(result.Message);

            return Ok(result);
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _authService.LoginAsync(model);
            if (!result.IsAuthenticated)
                return BadRequest(result.Message);
            if (!String.IsNullOrEmpty(result.RefreshToken))
                SetRefreshToken(result.RefreshToken, result.RefreshTokenExpiretion);
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> AddToRole(AddUserInRoleModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result =await  _authService.AddUserInRoleAsync(model);
            if (!String.IsNullOrEmpty(result))
                return BadRequest(result);
            return Ok("Success");
        }
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetRefreshToken()
        {
            var refreshToken = Request.Cookies["RefreshToken"];
            var result=await _authService.RefreshTokenInDatabae(refreshToken);
            if (!result.IsAuthenticated)
                return BadRequest(result);
            SetRefreshToken(result.RefreshToken,result.RefreshTokenExpiretion);
            return Ok(result);
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> RevokedToken([FromBody] RevokedTokenDTO model)
        {
            var token = model.Token ?? Request.Cookies["RefreshToken"];
            if (String.IsNullOrEmpty(token))
                return BadRequest("Invalid Token");
            var result =await _authService.RevokeTokenInDatabae(token);
            if (!result)
                return BadRequest("Falied...");
            return Ok("Succes Revoked Token");
        }
        private void SetRefreshToken(string Refreshtoken,DateTime expiers)
        {
            var cookies = new CookieOptions()
            {
                HttpOnly = true,
                Expires = expiers.ToLocalTime()
            };
            Response.Cookies.Append("RefreshToken", Refreshtoken, cookies);
        }
    }
}
