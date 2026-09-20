using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using UserService.Configuration;
using UserService.Data;
using UserService.Repositories;
using UserService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// lets controllers return the API contract's error shape instead of the default 400
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "User Service API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<UserServiceContext>(options =>
        options.UseInMemoryDatabase("UserServiceDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("UserDb");
    builder.Services.AddDbContext<UserServiceContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration is missing.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

builder.Services.AddHttpClient<IReservationServiceClient, ReservationServiceClient>(client =>
{
    var url = builder.Configuration["ServiceUrls:ReservationService"]
        ?? throw new InvalidOperationException("ServiceUrls:ReservationService configuration is missing.");
    client.BaseAddress = new Uri(url);
});

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider.GetRequiredService<UserServiceContext>().Database.Migrate();
}
app.MapGet("/health", async (UserServiceContext context) =>
{
    var canConnect = await context.Database.CanConnectAsync();
    var migrationsCount = context.Database.IsRelational()
        ? (await context.Database.GetAppliedMigrationsAsync()).Count()
        : 0;
    return Results.Ok(new { status = canConnect ? "UP" : "DOWN", service = "UserService", database = "userservicedb", migrations = migrationsCount });
});

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<UserServiceContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DataSeeder.SeedAsync(context, hasher);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();