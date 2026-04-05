using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace ECommerceProductManagement.DTOs
{
    public class LoginDto
    {
        [Required,EmailAddress]
        public string? Email { get; set; }
        [Required,NotNull]
        public string Password { get; set; }
    }
}
