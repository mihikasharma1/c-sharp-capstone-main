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
app.Use((context, next) =>
{
    var prefix = context.Request.Headers["X-Forwarded-Prefix"].FirstOrDefault();
    if (!string.IsNullOrEmpty(prefix))
    {
        context.Request.PathBase = prefix;
    }
    return next();
});
if (!app.Environment.IsDevelopment())
{
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider.GetRequiredService<CatalogServiceContext>().Database.Migrate();
}

app.MapGet("/health", async (CatalogServiceContext context) =>
{
    var canConnect = await context.Database.CanConnectAsync();
    var migrationsCount = context.Database.IsRelational()
        ? (await context.Database.GetAppliedMigrationsAsync()).Count()
        : 0;
    return Results.Ok(new { status = canConnect ? "UP" : "DOWN", service = "CatalogService", database = "catalogservicedb", migrations = migrationsCount });
});

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{


    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<CatalogServiceContext>();
    await DataSeeder.SeedAsync(context);
}

app.MapControllers();
app.Run();