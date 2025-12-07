using HospitadentApi.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using HospitadentApi.Repository;

namespace HospitadentApi.WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorBranchCodeController : ControllerBase
    {
        private readonly DoctorBranchCodeRepository _repository;
        private readonly ILogger<DoctorBranchCodeController> _logger;

        public DoctorBranchCodeController(DoctorBranchCodeRepository repository, ILogger<DoctorBranchCodeController> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET api/doctorbranchcode/{id}
        [HttpGet("{id}", Name = "GetDoctorBranchCode")]
        public ActionResult<DoctorBranchCode> Get(int id)
        {
            _logger.LogDebug("Get called: Id={Id}", id);

            if (id <= 0)
            {
                _logger.LogWarning("Get called with invalid id: {Id}", id);
                return BadRequest("Id must be provided and greater than zero.");
            }

            try
            {
                var item = _repository.Load(id);
                if (item is null)
                {
                    _logger.LogInformation("DoctorBranchCode not found: Id={Id}", id);
                    return NotFound();
                }

                _logger.LogInformation("DoctorBranchCode loaded: Id={Id} Name={Name}", item.Id, item.Name);
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading DoctorBranchCode with id {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        // GET api/doctorbranchcode/all
        [HttpGet("all", Name = "GetDoctorBranchCodes")]
        public ActionResult<IEnumerable<DoctorBranchCode>> GetAll()
        {
            _logger.LogDebug("GetAll called");

            try
            {
                var items = _repository.LoadAll() ?? new List<DoctorBranchCode>();
                if (!items.Any())
                {
                    _logger.LogInformation("GetAll returned no doctor branch codes");
                    return NotFound("No doctor branch codes found.");
                }

                _logger.LogInformation("GetAll returning {Count} doctor branch codes", items.Count);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading doctor branch codes");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}