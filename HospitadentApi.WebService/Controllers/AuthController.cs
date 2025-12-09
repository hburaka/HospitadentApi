using HospitadentApi.Entity;
using HospitadentApi.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HospitadentApi.WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IWebHostEnvironment _environment;

        public AuthController(
            UserRepository userRepository,
            IConfiguration configuration,
            ILogger<AuthController> logger,
            IWebHostEnvironment environment)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Login attempt for: {Username}", request.Username);

            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("Login attempt with empty username or password");
                return BadRequest(new { message = "Kullanıcı adı ve şifre gereklidir." });
            }

            try
            {
                var user = _userRepository.AuthenticateUser(request.Username, request.Password);

                if (user == null)
                {
                    _logger.LogWarning("Authentication failed for: {Username}", request.Username);
                    return Unauthorized(new { message = "Kullanıcı adı veya şifre hatalı." });
                }

                var token = GenerateJwtToken(user);

                _logger.LogInformation("User logged in successfully: Id={UserId} Name={Name}", user.Id, user.Name);

                return Ok(new LoginResponse
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Name = user.Name,
                        LastName = user.LastName,
                        UserType = user.UserType,
                        ClinicId = user.Clinic?.Id,
                        DepartmentId = user.Department?.Id,
                        RoleId = user.URole?.Id
                    },
                    ExpiresIn = int.Parse(
                        Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES") 
                        ?? _configuration["JwtSettings:ExpirationInMinutes"] 
                        ?? "1440")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for: {Username}. Error: {ErrorMessage}", request.Username, ex.Message);
                
                // Development'ta detaylı hata, Production'da generic mesaj
                if (_environment.IsDevelopment())
                {
                    return StatusCode(500, new { 
                        message = "Giriş işlemi sırasında bir hata oluştu.",
                        error = ex.Message,
                        stackTrace = ex.StackTrace
                    });
                }
                
                return StatusCode(500, new { message = "Giriş işlemi sırasında bir hata oluştu." });
            }
        }

        private string GenerateJwtToken(User user)
        {
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                           ?? _configuration["JwtSettings:SecretKey"] 
                           ?? throw new InvalidOperationException("JWT SecretKey bulunamadı!");
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                        ?? _configuration["JwtSettings:Issuer"] 
                        ?? "HospitadentApi";
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                          ?? _configuration["JwtSettings:Audience"] 
                          ?? "HospitadentApi_Users";
            var expirationMinutes = int.Parse(
                Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES") 
                ?? _configuration["JwtSettings:ExpirationInMinutes"] 
                ?? "1440");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, $"{user.Name} {user.LastName}".Trim()),
                new Claim("UserId", user.Id.ToString()),
                new Claim("UserType", user.UserType.ToString())
            };

            if (user.Clinic != null)
            {
                claims.Add(new Claim("ClinicId", user.Clinic.Id.ToString()));
            }

            if (user.Department != null)
            {
                claims.Add(new Claim("DepartmentId", user.Department.Id.ToString()));
            }

            if (user.URole != null)
            {
                claims.Add(new Claim("RoleId", user.URole.Id.ToString()));
                claims.Add(new Claim(ClaimTypes.Role, user.URole.Name ?? "User"));
            }

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("validate")]
        [Authorize]
        public ActionResult ValidateToken()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var userName = User.Identity?.Name;

            _logger.LogInformation("Token validated for user: {UserId} - {UserName}", userId, userName);

            return Ok(new
            {
                message = "Token geçerli",
                userId = userId,
                userName = userName
            });
        }

    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
        public int ExpiresIn { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int UserType { get; set; }
        public int? ClinicId { get; set; }
        public int? DepartmentId { get; set; }
        public int? RoleId { get; set; }
    }

}

