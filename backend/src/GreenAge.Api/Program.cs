using System.Text;
using System.Text.Json.Serialization;
using GreenAge.Core;
using GreenAge.Infrastructure;
using GreenAge.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var dataDirectory = Path.Combine(builder.Environment.ContentRootPath, "data");
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is required.");

builder.WebHost.UseUrls("http://localhost:5080");
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header
}));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
    .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true, ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        NameClaimType = "name", RoleClaimType = "role"
    };
});
builder.Services.AddAuthorization();

var useSql = string.Equals(builder.Configuration["Storage:Provider"], "Sql", StringComparison.OrdinalIgnoreCase);
if (useSql)
{
    builder.Services.AddDbContext<GreenAgeDbContext>(options =>
    {
        var connection = builder.Configuration.GetConnectionString("Default") ?? "Data Source=greenage.db";
        var engine = builder.Configuration["Storage:SqlEngine"] ?? "Sqlite";
        if (string.Equals(engine, "SqlServer", StringComparison.OrdinalIgnoreCase)) options.UseSqlServer(connection);
        else options.UseSqlite(connection);
    });
    builder.Services.AddScoped<IUserRepository, EfUserRepository>();
    builder.Services.AddScoped<ICategoryRepository, EfCategoryRepository>();
    builder.Services.AddScoped<IPickupRequestRepository, EfPickupRequestRepository>();
    builder.Services.AddScoped<INotificationRepository, EfNotificationRepository>();
    builder.Services.AddScoped<ISosRepository, EfSosRepository>();
}
else
{
    builder.Services.AddSingleton<JsonUserRepository>(_ => new JsonUserRepository(dataDirectory));
    builder.Services.AddSingleton<IUserRepository>(services => services.GetRequiredService<JsonUserRepository>());
    builder.Services.AddSingleton<JsonCategoryRepository>(_ => new JsonCategoryRepository(dataDirectory));
    builder.Services.AddSingleton<ICategoryRepository>(services => services.GetRequiredService<JsonCategoryRepository>());
    builder.Services.AddSingleton<IPickupRequestRepository>(_ => new JsonPickupRequestRepository(dataDirectory));
    builder.Services.AddSingleton<INotificationRepository>(_ => new JsonNotificationRepository(dataDirectory));
    builder.Services.AddSingleton<ISosRepository>(_ => new JsonSosRepository(dataDirectory));
}
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RequestService>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    if (useSql)
    {
        var db = scope.ServiceProvider.GetRequiredService<GreenAgeDbContext>();
        if (app.Environment.IsDevelopment()) await db.Database.MigrateAsync();
        else await db.Database.EnsureCreatedAsync();
        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(GreenAgeDbContext.SeedCategories);
            await db.SaveChangesAsync();
        }
    }
    else await scope.ServiceProvider.GetRequiredService<JsonCategoryRepository>().InitializeAsync();
    await scope.ServiceProvider.GetRequiredService<AuthService>().SeedDemoUsersAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<ApiExceptionMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
