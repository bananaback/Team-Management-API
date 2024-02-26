using System.Text;
using AuthenticationService.Data;
using AuthenticationService.Models;
using AuthenticationService.Profiles;
using AuthenticationService.Repositories.UserRepositories;
using AuthenticationService.Services.AsyncDataServices;
using AuthenticationService.Services.Authenticators;
using AuthenticationService.Services.CacheServices;
using AuthenticationService.Services.EventProcessingServices;
using AuthenticationService.Services.PasswordHashers;
using AuthenticationService.Services.TokenGenerators;
using AuthenticationService.Services.TokenValidators;
using AuthenticationService.Services.UserServices;
using Azure.Core;
using Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

AuthenticationConfiguration authenticationConfiguration = new AuthenticationConfiguration();
builder.Configuration.GetSection("Authentication").Bind(authenticationConfiguration);

// Add services to the container.
var services = builder.Services;
services.AddAutoMapper(typeof(ApplicationUserProfile));
services.AddSingleton(authenticationConfiguration);
services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
services.AddSingleton<AccessTokenGenerator>();
services.AddSingleton<RefreshTokenGenerator>();
services.AddSingleton<TokenGenerator>();
services.AddSingleton<IConnectionMultiplexer>(provider =>
{
    var connectionString = builder.Configuration.GetConnectionString("redis");
    Console.WriteLine("Redis connection string: " + connectionString);
    return ConnectionMultiplexer.Connect(connectionString!);
});
services.AddScoped<Authenticator>();
services.AddScoped<RefreshTokenValidator>();
services.AddScoped<RedisTokenCache>();
services.AddScoped<IUserService, UserService>();
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<Authenticator>();

if (builder.Environment.IsDevelopment())
{
    /*
    Console.WriteLine("--> Using InMem memory");
    
        services.AddDbContext<AuthenticationDbContext>(options =>
        {
            options.UseInMemoryDatabase("AuthInMem");
        });
        */
    Console.WriteLine("--> Using SqlServer Db: " + builder.Configuration.GetConnectionString("AuthConn"));
    services.AddDbContext<AuthenticationDbContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("AuthConn"));
    });

}
else if (builder.Environment.IsProduction())
{
    var connectionString = builder.Configuration.GetConnectionString("AuthConn");
    Console.WriteLine("--> Using SqlServer Db: " + connectionString);
    services.AddDbContext<AuthenticationDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
}




services.AddSingleton<IEventProcessor, EventProcessor>();
services.AddHostedService<MessageBusSubcriber>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters()
    {
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authenticationConfiguration.AccessTokenSecret)),
        ValidIssuer = authenticationConfiguration.Issuer,
        ValidAudiences = authenticationConfiguration.Audiences,
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ClockSkew = TimeSpan.Zero
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

PrepDb.PrepPopulation(app, builder.Environment.IsProduction());

app.Run();
