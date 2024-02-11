namespace UserService.Dtos
{
    public class ApplicationUserReadDto
    {
        public Guid UserId { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }

        public ApplicationUserReadDto(string email, string username, string passwordHash)
        {
            Email = email;
            Username = username;
            PasswordHash = passwordHash;
        }
    }
}