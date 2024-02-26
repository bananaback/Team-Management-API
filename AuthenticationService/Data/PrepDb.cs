using AuthenticationService.Data;
using Microsoft.EntityFrameworkCore;

namespace Data;

public class PrepDb
{
    public static void PrepPopulation(IApplicationBuilder app, bool IsProduction)
    {
        using (var serviceScope = app.ApplicationServices.CreateScope())
        {
            SeedData(serviceScope.ServiceProvider.GetRequiredService<AuthenticationDbContext>(), IsProduction);
        }
    }

    private static void SeedData(AuthenticationDbContext authenticationDbContext, bool isProduction)
    {
        if (isProduction)
        {
            Console.WriteLine("--> Attempting to apply migrations...");
            try
            {
                authenticationDbContext.Database.Migrate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not run migration: {ex.Message}");
            }
        }
    }
}