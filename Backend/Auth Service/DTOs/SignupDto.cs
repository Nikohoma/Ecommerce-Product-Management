using System.ComponentModel.DataAnnotations;

namespace ECommerceProductManagement.DTOs
{
    public class SignupDto
    {
        [Required,StringLength(25,MinimumLength = 3)]
        public string Name { get; set; }
        [Required,EmailAddress]
        public string Email { get; set; }
        [Required,MinLength(6)]
        public string Password { get; set; }
        [Required,RegularExpression("Admin|ProductManager|Customer|ContentExecutive")]
        public string Role { get; set; } 
    }
}
