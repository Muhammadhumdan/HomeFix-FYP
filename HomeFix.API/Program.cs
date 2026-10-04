using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using HomeFix.API.Authentication;
using HomeFix.API.Data;
using HomeFix.API.Interfaces;
using HomeFix.API.Middleware;
using HomeFix.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuration ----------
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 characters. Set it in appsettings.Development.json or the Jwt__Key environment variable.");
builder.Services.AddSingleton(jwt);

// ---------- Database ----------
builder.Services.AddDbContext<HomeFixDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Controllers + validation responses ----------
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = ctx =>
    {
        var errors = ctx.ModelState.Where(e => e.Value!.Errors.Count > 0)
            .ToDictionary(e => e.Key, e => e.Value!.Errors.Select(x => string.IsNullOrEmpty(x.ErrorMessage) ? "Invalid value." : x.ErrorMessage).ToArray());
        var first = errors.Values.SelectMany(v => v).FirstOrDefault() ?? "Validation failed.";
        return new BadRequestObjectResult(new { message = first, errors, status = 400 });
    });

// ---------- Swagger with JWT support ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HomeFix API", Version = "v1", Description = "AI-enhanced local services marketplace" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
        In = ParameterLocation.Header, Description = "Paste ONLY the JWT token (without the word 'Bearer')."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// ---------- JWT authentication + role authorization ----------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = jwt.Issuer,
            ValidateAudience = true, ValidAudience = jwt.Audience,
            ValidateLifetime = true, ClockSkew = TimeSpan.Zero,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            RoleClaimType = ClaimTypes.Role, NameClaimType = ClaimTypes.Name
        };
    });
builder.Services.AddAuthorization();

// ---------- CORS (React dev server) ----------
var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(o => o.AddPolicy("Client", p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

// ---------- Dependency injection ----------
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProviderService, ProviderService>();
builder.Services.AddScoped<IServiceListingService, ServiceListingService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddSingleton<RuleBasedQueryParser>();
builder.Services.AddHttpClient<AnthropicQueryParser>(c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddScoped<IAiSearchService, AiSearchService>();

var app = builder.Build();

// ---------- Create / update the database and seed demo data ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HomeFixDbContext>();
    try
    {
        // Uses migrations when they exist, otherwise creates the schema directly.
        if (db.Database.GetMigrations().Any()) await db.Database.MigrateAsync();
        else await db.Database.EnsureCreatedAsync();
        await SeedData.InitializeAsync(db, scope.ServiceProvider.GetRequiredService<IPasswordService>());
    }
    catch (Exception ex)
    {
        app.Logger.LogCritical(ex, "Database setup failed. Check ConnectionStrings:DefaultConnection in appsettings.json and that SQL Server is running.");
        throw;
    }
}

// ---------- HTTP pipeline ----------
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HomeFix API v1"));
app.UseCors("Client");
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();
app.Run();
