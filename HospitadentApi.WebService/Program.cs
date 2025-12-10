using HospitadentApi.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.DataProtection;
using System.Text;
using DotNetEnv;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
               ?? cfg.GetConnectionString("DefaultConnection") 
               ?? cfg["ConnectionStrings:DefaultConnection"];
    
    if (string.IsNullOrWhiteSpace(conn))
    {
        throw new InvalidOperationException("Connection string bulunamadı! CONNECTION_STRING environment variable'ı kontrol edin.");
    }
    
    var logger = sp.GetRequiredService<ILogger<ClinicRepository>>();
    // Connection string'i logla (güvenlik için şifreyi gizle)
    var safeConn = conn.Contains("Pwd=") 
        ? conn.Substring(0, conn.IndexOf("Pwd=") + 4) + "***" 
        : conn;
    logger.LogInformation("ClinicRepository için connection string kullanılıyor: {ConnectionString}", safeConn);
    
    return new ClinicRepository(conn, logger);
});

builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
               ?? cfg.GetConnectionString("DefaultConnection") 
               ?? cfg["ConnectionStrings:DefaultConnection"];
    var logger = sp.GetRequiredService<ILogger<DoctorBranchCodeRepository>>();
    return new DoctorBranchCodeRepository(conn ?? throw new InvalidOperationException("Connection string bulunamadı!"), logger);
});
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
               ?? cfg.GetConnectionString("DefaultConnection") 
               ?? cfg["ConnectionStrings:DefaultConnection"];
    var clinicRepo = sp.GetRequiredService<ClinicRepository>();
    var logger = sp.GetRequiredService<ILogger<UserRepository>>();
    return new UserRepository(conn ?? throw new InvalidOperationException("Connection string bulunamadı!"), clinicRepo, logger);
});
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
               ?? cfg.GetConnectionString("DefaultConnection") 
               ?? cfg["ConnectionStrings:DefaultConnection"];
    var logger = sp.GetRequiredService<ILogger<UserWorkingScheduleRepository>>();
    return new UserWorkingScheduleRepository(conn ?? throw new InvalidOperationException("Connection string bulunamadı!"), logger);
});
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
               ?? cfg.GetConnectionString("DefaultConnection") 
               ?? cfg["ConnectionStrings:DefaultConnection"];
    var logger = sp.GetRequiredService<ILogger<AppointmentRepository>>();
    return new AppointmentRepository(conn ?? throw new InvalidOperationException("Connection string bulunamadı!"), logger);
});
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
               ?? cfg.GetConnectionString("DefaultConnection") 
               ?? cfg["ConnectionStrings:DefaultConnection"];
    var logger = sp.GetRequiredService<ILogger<PatientRepository>>();
    return new PatientRepository(conn ?? throw new InvalidOperationException("Connection string bulunamadı!"), logger);
});
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
               ?? cfg.GetConnectionString("DefaultConnection") 
               ?? cfg["ConnectionStrings:DefaultConnection"];
    var logger = sp.GetRequiredService<ILogger<DepartmentRepository>>();
    return new DepartmentRepository(conn ?? throw new InvalidOperationException("Connection string bulunamadı!"), logger);
});
builder.Services.AddScoped(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
               ?? cfg.GetConnectionString("DefaultConnection") 
               ?? cfg["ConnectionStrings:DefaultConnection"];
    var logger = sp.GetRequiredService<ILogger<URoleRepository>>();
    return new URoleRepository(conn ?? throw new InvalidOperationException("Connection string bulunamadı!"), logger);
});

builder.Services.AddScoped<IRepository<HospitadentApi.Entity.DoctorBranchCode>>(sp => sp.GetRequiredService<DoctorBranchCodeRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.Clinic>>(sp => sp.GetRequiredService<ClinicRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.User>>(sp => sp.GetRequiredService<UserRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.UserWorkingSchedule>>(sp => sp.GetRequiredService<UserWorkingScheduleRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.Appointment>>(sp => sp.GetRequiredService<AppointmentRepository>());
builder.Services.AddScoped<IRepository<HospitadentApi.Entity.Patient>>(sp => sp.GetRequiredService<PatientRepository>());

var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                ?? builder.Configuration["JwtSettings:SecretKey"] 
                ?? throw new InvalidOperationException("JWT SecretKey bulunamadı!");
var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
             ?? builder.Configuration["JwtSettings:Issuer"] 
             ?? "HospitadentApi";
var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
               ?? builder.Configuration["JwtSettings:Audience"] 
               ?? "HospitadentApi_Users";
var expirationMinutes = int.Parse(
    Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES") 
    ?? builder.Configuration["JwtSettings:ExpirationInMinutes"] 
    ?? "1440");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// DataProtection: Production'da ephemeral file system kullan (container içinde kalıcı storage'a gerek yok)
if (builder.Environment.IsProduction())
{
    var tempKeysPath = Path.Combine(Path.GetTempPath(), "HospitadentApi-DataProtection-Keys");
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(tempKeysPath)); // Keys'i geçici dosya sisteminde sakla (container restart'ta kaybolur ama sorun değil)
}

builder.Services.AddControllers();

// Register IHttpClientFactory so controllers/services can request IHttpClientFactory or HttpClient via DI
builder.Services.AddHttpClient();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Hospitadent API",
        Version = "v1",
        Description = "Hospitadent API - JWT Authentication ile korumalı"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Örnek: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseStaticFiles();

// Security Headers ve Robots Tag Middleware
app.Use(async (context, next) =>
{
    // X-Robots-Tag: Arama motorlarının indekslemesini engelle
    context.Response.Headers.Append("X-Robots-Tag", "noindex, nofollow");
    
    // Security Headers
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    
    // HSTS (HTTPS zorunluluğu) - Sadece HTTPS üzerinden çalışıyorsa
    if (context.Request.IsHttps)
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }
    
    // Referrer-Policy
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hospitadent API v1");
        c.InjectJavascript("/swagger-login.js");
    });
    // HTTPS redirection sadece Development'ta (Production'da reverse proxy HTTPS sağlıyor)
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();