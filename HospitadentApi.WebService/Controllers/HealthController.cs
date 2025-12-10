using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using HospitadentApi.Repository;
using System;
using System.Data;

namespace HospitadentApi.WebService.Controllers
{
    [Route("/")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public HealthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Get()
        {
            try
            {
                var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
                          ?? _configuration.GetConnectionString("DefaultConnection") 
                          ?? _configuration["ConnectionStrings:DefaultConnection"];

                if (string.IsNullOrWhiteSpace(conn))
                {
                    return Ok(new 
                    { 
                        status = "OK", 
                        message = "Hospitadent API çalışıyor", 
                        database = "Bağlantı stringi bulunamadı",
                        timestamp = DateTime.UtcNow 
                    });
                }

                // Veritabanı bağlantısını test et
                using var db = new DBHelper(conn);
                var result = db.ExecuteScalarSql("SELECT 1");
                
                return Ok(new 
                { 
                    status = "OK", 
                    message = "Hospitadent API çalışıyor", 
                    database = "Veritabanı bağlantısı başarılı",
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                return Ok(new 
                { 
                    status = "OK", 
                    message = "Hospitadent API çalışıyor", 
                    database = $"Veritabanı bağlantı hatası: {ex.Message}",
                    timestamp = DateTime.UtcNow 
                });
            }
        }
    }
}

