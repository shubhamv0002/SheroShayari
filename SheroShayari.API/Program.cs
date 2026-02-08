using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SheroShayari.API.Data;
using SheroShayari.API.Models;
using SheroShayari.API.Repositories;
using SheroShayari.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Use PascalCase property names in JSON (default .NET convention)
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Add Entity Framework Core with SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? 
        "Data Source=SheroShayari.db;Cache=Shared"));

// Add Identity services
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password requirements
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    
    // Email confirmation settings
    options.SignIn.RequireConfirmedEmail = false; // In production, set to true
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(
    jwtSettings["SecretKey"] ?? 
    "your-super-secret-key-that-is-at-least-32-characters-long-for-security!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "SheroShayariAPI",
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"] ?? "SheroShayariUsers",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    
    // Add event logging for debugging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception != null)
            {
                System.Console.WriteLine($"JWT Authentication Failed: {context.Exception.Message}");
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            System.Console.WriteLine("JWT Token Validated Successfully");
            return Task.CompletedTask;
        }
    };
});

// Add repository and service dependencies
builder.Services.AddScoped<IShayariRepository, ShayariRepository>();
builder.Services.AddScoped<IAiGenerationService, AiGenerationService>();
builder.Services.AddScoped<EmailSender>();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender>(provider => 
    provider.GetRequiredService<EmailSender>());
builder.Services.AddHttpClient<IAiGenerationService, AiGenerationService>();

// Add CORS policy to allow the Blazor frontend to communicate with the API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5057",   // API itself
            "https://localhost:7057",  // API HTTPS
            "http://localhost:5160",   // Blazor frontend HTTP
            "https://localhost:7160",  // Blazor frontend HTTPS
            "http://localhost:5162",   // Blazor dev server HTTP
            "https://localhost:7162",  // Blazor dev server HTTPS
            "http://localhost:5173",   // Alternative ports
            "https://localhost:5173",
            "http://localhost:5174",
            "https://localhost:5174")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add OpenAPI/Swagger for API documentation
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\""
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

var app = builder.Build();

// Create the database and apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";
        c.SwaggerEndpoint("/openapi/v1.json", "SheroShayari API");
    });
    app.UseSwagger();
}

// CORS must be applied before UseAuthentication
app.UseCors("AllowBlazorFrontend");
app.UseHttpsRedirection();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
