using Microsoft.EntityFrameworkCore;
using Repositories.UserRepositories;
using Services.BackgroundServices;
using Services.OutboxMessageServices;
using Services.UserServices;
using UserService.AsyncDataServices;
using UserService.Data;
using UserService.OutboxMessageServices;
using UserService.PasswordHashers;
using UserService.Profiles;
using UserService.Repositories.OutboxRepositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var services = builder.Services;
//services.AddDbContext<UserDbContext>(options => options.UseSqlServer("Server=localhost,1433;Database=UserDb;Trusted_Connection=True;TrustServerCertificate=True"));
services.AddAutoMapper(typeof(ApplicationUserProfile));

services.AddSingleton<IMessageBusClient, MessageBusClient>();
services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
services.AddScoped<IUserRepository, DatabaseUserRepository>();
services.AddScoped<IOutboxRepository, OutboxRepository>();
services.AddScoped<IUserService, DatabaseUserService>();
services.AddScoped<IOutboxMessageService, OutboxMessageService>();
services.AddHostedService<OutboxProcessorService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("--> Using ImMem Db");
    services.AddDbContext<UserDbContext>(options => options.UseInMemoryDatabase("InMem"));
    //services.AddDbContext<UserDbContext>(options =>
    //{
    //    options.UseSqlServer(builder.Configuration.GetConnectionString("UserConn"));
    //});
}
else if (builder.Environment.IsProduction())
{
    Console.WriteLine("--> Using SqlServer Db" + builder.Configuration.GetConnectionString("UserConn"));
    services.AddDbContext<UserDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("UserConn"));
    });
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

