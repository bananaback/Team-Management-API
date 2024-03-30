using Microsoft.EntityFrameworkCore;
using UserService.Models;

namespace UserService.Data
{
    public class PrepDb
    {
        public static void PrepPopulation(IApplicationBuilder app, bool isProd)
        {
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                SeedData(serviceScope.ServiceProvider.GetService<UserDbContext>()!, isProd);
            }
        }

        private static void SeedData(UserDbContext dbContext, bool isProd)
        {
            if (isProd)
            {
                Console.WriteLine("--> Attempting to apply migrations...");
                try
                {
                    dbContext.Database.Migrate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"--> Could not run migration: {ex.Message}");
                }
            }
            if (!dbContext.Users.Any())
            {
                Console.WriteLine("--> Seeding data.");
                dbContext.Users.AddRange(
                    new ApplicationUser(new Guid(), "votrongtin882003@gmail.com", "bananaback", "bananaback")
                );
                dbContext.SaveChanges();
            }
            else
            {
                Console.WriteLine("--> We already have data.");
            }
        }
    }
}