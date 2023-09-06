using System.Text.Json.Serialization;

namespace JWT.Utitlies {
    public class AuthModel {
        public string Message { get; set; }
        public bool IsAuthenticated { get; set; } 
        public string Username { get; set; }
        public string Email { get; set; }
        public IList<string> Roles { get; set;}
        public string Token { get; set; }
        [JsonIgnore]
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiretion { get; set; }
    }
}
