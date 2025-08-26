using LibraryManagement.API.Data;
using LibraryManagement.API.Hubs;
using LibraryManagement.API.Models;
using LibraryManagement.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using LibraryManagement.API.Services;




var builder = WebApplication.CreateBuilder(args);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// --- Service Registrations ---

// 1. DbContext and Identity
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
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
              .AllowCredentials(); // Important for SignalR with auth
    });
});

// 4. Custom Application Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<AIRecommendationService>();
builder.Services.AddHostedService<OverdueNotificationService>();

// 5. SignalR
builder.Services.AddSignalR();

// --- THIS IS THE CRITICAL LINE THAT WAS LIKELY MISSING ---
// It registers all the services needed for your API controllers to work.
builder.Services.AddControllers();

// 6. Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- End of Service Registrations ---

var app = builder.Build();

// Seeding Logic (Roles and PreApprovedUsers)
// ... your existing seeding code ...

// --- Middleware Pipeline ---
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}


app.UseStaticFiles();



app.UseHttpsRedirection();

app.UseRouting(); // UseRouting must come before UseCors and UseAuthentication/Authorization
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();

// This line maps the routes for your API controllers. It needs AddControllers() to work.
app.MapControllers();
app.MapHub<NotificationHub>("/notificationhub");

app.Run();