using LibraryManagement.API.Data;
using LibraryManagement.API.Hubs;
using LibraryManagement.API.Models;
using LibraryManagement.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// --- Service Registrations ---

// 1. DbContext and Identity
var railwayUrl = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                ?? Environment.GetEnvironmentVariable("MYSQL_URL");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (!string.IsNullOrEmpty(railwayUrl))
    {
        try
        {
            var uri = new Uri(railwayUrl);
            var userInfo = uri.UserInfo.Split(':', 2); // username:password

            var connectionString =
                $"server={uri.Host};port={uri.Port};database={uri.AbsolutePath.TrimStart('/')};user={userInfo[0]};password={userInfo[1]};";

            Console.WriteLine($"[DB] Using MySQL on Railway: {uri.Host}:{uri.Port}, DB={uri.AbsolutePath.TrimStart('/')}");

            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
        }
        catch (Exception ex)
        {
            Console.WriteLine("[DB ERROR] Failed to parse Railway URL: " + ex.Message);
        }
    }
    else
    {
        // Fallback to local SQL Server
        var localConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        Console.WriteLine("[DB] Using local SQL Server");
        options.UseSqlServer(localConnectionString);
    }
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddTransient<IEmailSender, EmailSender>();

// 2. Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidAudience = builder.Configuration["JWT:ValidAudience"],
        ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"])),
        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/notificationhub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// 3. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins("https://localhost:7019")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // For SignalR with auth
    });
});

// 4. Custom Application Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<AIRecommendationService>();
builder.Services.AddHostedService<OverdueNotificationService>();

// 5. SignalR
builder.Services.AddSignalR();

// 6. Controllers
builder.Services.AddControllers();

// 7. Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Build app ---
var app = builder.Build();

// Middleware Pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LibraryManagement.API v1");
    c.RoutePrefix = "swagger"; // keeps Swagger at /swagger
});

app.UseStaticFiles();
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationhub");

app.Run();
