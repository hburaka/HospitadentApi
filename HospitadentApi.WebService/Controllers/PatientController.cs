using System;
using System.Collections.Generic;
using HospitadentApi.Entity;
using HospitadentApi.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HospitadentApi.WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PatientController : ControllerBase
    {
        private readonly PatientRepository _patientRepository;
        private readonly ILogger<PatientController> _logger;

        public PatientController(PatientRepository patientRepository, ILogger<PatientController> logger)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("{id}", Name = "GetPatient")]
        public ActionResult<Patient> Get(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Get called with invalid id: {Id}", id);
                return BadRequest("Id must be provided and greater than zero.");
            }

            try
            {
                _logger.LogInformation("Loading patient {Id}", id);
                var item = _patientRepository.Load(id);
                if (item == null)
                {
                    _logger.LogInformation("Patient {Id} not found", id);
                    return NotFound();
                }
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading patient {Id}", id);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("search", Name = "SearchPatients")]
        public ActionResult<IEnumerable<Patient>> Search(
            [FromQuery] int? id = null,
            [FromQuery] string? fullName = null,
            [FromQuery] string? mobile = null,
            [FromQuery] string? tcNo = null,
            [FromQuery] int? clinicId = null,
            [FromQuery] int limit = 25)
        {
            if (id == null && string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(mobile) && string.IsNullOrWhiteSpace(tcNo) && clinicId == null)
            {
                _logger.LogWarning("Search called without criteria");
                return BadRequest("At least one search criterion must be provided.");
            }

            try
            {
                _logger.LogInformation("Searching patients id={Id} fullName={FullName} mobile={Mobile} tcNo={TcNo} clinicId={ClinicId} limit={Limit}",
                    id, fullName, mobile, tcNo, clinicId, limit);

                var list = _patientRepository.GetByCriteria(
                    id: id,
                    fullName: fullName,
                    mobile: mobile,
                    tcNo: tcNo,
                    clinicId: clinicId,
                    limit: limit);

                if (list == null || list.Count == 0)
                {
                    _logger.LogInformation("No patients found for criteria");
                    return NotFound("No patients found for the given criteria.");
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patients with params id={Id} fullName={FullName} clinicId={ClinicId}", id, fullName, clinicId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}