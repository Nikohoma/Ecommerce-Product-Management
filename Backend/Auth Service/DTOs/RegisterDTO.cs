using ECommerceProductManagement.DTOs;

namespace Auth.DTOs
{
    public class RegisterDTO:SignupDto
    {
        public string Otp { get; set; }
    }
}
