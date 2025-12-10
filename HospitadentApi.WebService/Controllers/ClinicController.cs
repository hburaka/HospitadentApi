using HospitadentApi.Entity;
using HospitadentApi.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace HospitadentApi.WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClinicController : ControllerBase
    {
        private readonly ClinicRepository _clinicRepository;
        private readonly ILogger<ClinicController> _logger;

        public ClinicController(ClinicRepository clinicRepository, ILogger<ClinicController> logger)
        {
            _clinicRepository = clinicRepository ?? throw new ArgumentNullException(nameof(clinicRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("{id}", Name = "GetClinic")]
        public ActionResult<Clinic> Get(int id)
        {
            _logger.LogDebug("Get called: Id={Id}", id);

            if (id <= 0)
            {
                _logger.LogWarning("Get called with invalid id: {Id}", id);
                return BadRequest("Id must be provided and greater than zero.");
            }

            try
            {
                _logger.LogInformation("Loading clinic {Id}", id);
                var clinic = _clinicRepository.Load(id);
                if (clinic is null)
                {
                    _logger.LogInformation("Clinic not found: Id={Id}", id);
                    return NotFound($"Clinic with id {id} not found.");
                }

                _logger.LogInformation("Clinic loaded: Id={Id} Name={Name}", clinic.Id, clinic.Name);
                return Ok(clinic);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading clinic with id {Id}", id);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("all", Name = "GetClinics")]
        public ActionResult<IEnumerable<Clinic>> GetAll()
        {
            _logger.LogDebug("GetAll called");

            try
            {
                var clinics = _clinicRepository.LoadAll() ?? new List<Clinic>();

                if (!clinics.Any())
                {
                    _logger.LogInformation("GetAll returned no clinics");
                    return NotFound("No clinics found.");
                }

                _logger.LogInformation("GetAll returning {Count} clinics", clinics.Count);
                return Ok(clinics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading clinics");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("test", Name = "GetTest")]
        public ActionResult<Clinic> Test()
        {
            _logger.LogDebug("GetTest called");

            try
            {
                var clinics = new Clinic { Name = "test" };
                return Ok(clinics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading clinics");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
