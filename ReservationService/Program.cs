using Microsoft.EntityFrameworkCore;
using ReservationService.Configuration;
using ReservationService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Reservation Service API", Version = "v1" });
});

builder.Services.AddDbContext<ReservationServiceContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (connectionString == "InMemory")
        options.UseInMemoryDatabase("ReservationServiceDb");
    else
        options.UseNpgsql(connectionString);
});

builder.Services.Configure<ServiceUrlsOptions>(builder.Configuration.GetSection("ServiceUrls"));

var serviceUrls = builder.Configuration.GetSection("ServiceUrls").Get<ServiceUrlsOptions>()
                  ?? throw new InvalidOperationException("ServiceUrls configuration is missing.");

builder.Services.AddHttpClient("UserService", client =>
{
    client.BaseAddress = new Uri(serviceUrls.UserService);
});

builder.Services.AddHttpClient("CatalogService", client =>
{
    client.BaseAddress = new Uri(serviceUrls.CatalogService);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();