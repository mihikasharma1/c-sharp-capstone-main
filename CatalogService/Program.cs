using CatalogService.Data;
using CatalogService.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Catalog Service API", Version = "v1" });
});

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<CatalogServiceContext>(options =>
        options.UseInMemoryDatabase("CatalogServiceDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("CatalogDb");
    builder.Services.AddDbContext<CatalogServiceContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services.AddScoped<IBookRepository, BookRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider.GetRequiredService<CatalogServiceContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CatalogServiceContext>();
    await DataSeeder.SeedAsync(context);
}

app.MapControllers();
app.Run();