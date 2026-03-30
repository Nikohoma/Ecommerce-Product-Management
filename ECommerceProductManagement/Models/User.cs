using Auth.Models;

namespace ECommerceProductManagement.Models
{
    public class User : BaseEntity
    {

        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
    }
}
