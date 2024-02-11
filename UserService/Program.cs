using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repositories.UserRepositories;
using Services;
using Services.UserServices;
using UserService.Data;
using UserService.PasswordHashers;
using UserService.Profiles;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var services = builder.Services;
//services.AddDbContext<UserDbContext>(options => options.UseSqlServer("Server=localhost,1433;Database=UserDb;Trusted_Connection=True;TrustServerCertificate=True"));
services.AddAutoMapper(typeof(ApplicationUserProfile));

services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
services.AddScoped<IUserRepository, DatabaseUserRepository>();
services.AddScoped<IUserService, DatabaseUserService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("--> Using ImMem Db");
    services.AddDbContext<UserDbContext>(options => options.UseInMemoryDatabase("InMem"));
}
else if (builder.Environment.IsProduction())
{
    Console.WriteLine("--> Using SqlServer Db" + builder.Configuration.GetConnectionString("UserConn"));
    services.AddDbContext<UserDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("UserConn")));
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

PrepDb.PrepPopulation(app, builder.Environment.IsProduction());

app.Run();

