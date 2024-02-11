using System.ComponentModel.DataAnnotations;

namespace UserService.Models
{
    public class ApplicationUser
    {
        [Key]
        public Guid UserId { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        [MaxLength(50)]
        public string Username { get; set; }
        [Required]
        public string PasswordHash { get; set; }

        public ApplicationUser()
        {
            Email = string.Empty;
            Username = string.Empty;
            PasswordHash = string.Empty;
        }
        public ApplicationUser(Guid userId, string email, string username, string passwordHash)
        {
            UserId = userId;
            Email = email;
            Username = username;
            PasswordHash = passwordHash;
        }
    }
}