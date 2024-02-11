namespace UserService.Dtos
{
    public class ApplicationUserCreateDto
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public ApplicationUserCreateDto(string email, string username, string password)
        {
            Email = email;
            Username = username;
            Password = password;
        }
    }
}