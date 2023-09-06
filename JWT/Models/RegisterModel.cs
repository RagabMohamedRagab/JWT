using System.ComponentModel.DataAnnotations;

namespace JWT.Models {
    public class RegisterModel {
        [MaxLength(50)]
        public string FirstName { get; set; }
        [MaxLength(250)]
        public string LastName { get; set; }
        [MaxLength(250)]
        public string UserName { get;set; }
        [MaxLength(250)]
        public string Email { get; set; }
        [MaxLength(250)]
        public string Password { get; set; }
    }
}
