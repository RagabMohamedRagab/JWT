using Microsoft.EntityFrameworkCore;

namespace JWT.Models {
    [Owned]
    public class RefreshToken {
        public string Token { get; set; }
        public DateTime ExpiresOn { get; set; }
        public bool IsExpired =>DateTime.UtcNow > ExpiresOn;   // في حاله التوكن خلص
        public DateTime CreatOn { get; set; }   // هو بيتعمل امتا
        public DateTime? RevokedOn { get;set; }  // هو اتمسح امتا
        public bool IsActive => RevokedOn == null && !IsExpired;      // هل هو شغال اما لا
    }
}
