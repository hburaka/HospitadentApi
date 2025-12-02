using HospitadentApi.Entity;
using HospitadentApi.Repository;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace HospitadentApi.WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClinicController : ControllerBase
    {
        private readonly ClinicRepository _clinicRepository;

        public ClinicController(ClinicRepository clinicRepository)
        {
            _clinicRepository = clinicRepository;
        }

        // GET api/clinic/{id}
        [HttpGet("{id}", Name = "GetClinic")]
        public ActionResult<IEnumerable<Clinic>> Get(int id)
        {
            // validation: ensure id is supplied and valid
            if (id <= 0)
                throw new ArgumentException("Id must be provided and greater than zero.", nameof(id));

            try
            {
                var clinic = _clinicRepository.Load(id);
                if (clinic is not null)
                    return new List<Clinic> { clinic };

                return NotFound($"Clinic with id {id} not found.");
            }
            catch (ArgumentException) // preserve validation exceptions
            {
                throw;
            }
            catch (Exception ex)
            {
                // add context and rethrow so middleware/logging can capture details
                throw new Exception($"Error loading clinic with id {id}", ex);
            }
        }

        // GET api/clinic/all
        [HttpGet("all", Name = "GetClinics")]
        public ActionResult<IEnumerable<Clinic>> GetAll()
        {
            // basic controller-level checks and contextual exception handling
            try
            {
                if (_clinicRepository == null)
                    throw new InvalidOperationException("Clinic repository is not initialized.");

                var clinics = _clinicRepository.LoadAll() ?? new List<Clinic>();

                if (!clinics.Any())
                    return NotFound("No clinics found.");

                return Ok(clinics);
            }
            catch (InvalidOperationException) // preserve repository initialization errors
            {
                throw;
            }
            catch (Exception ex)
            {
                // add context and rethrow to preserve stack and let middleware/logging handle it
                throw new Exception("Error loading clinics", ex);
            }
        }
    }
}
