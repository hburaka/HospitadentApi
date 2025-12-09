using HospitadentApi.Entity;
using HospitadentApi.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace HospitadentApi.WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserWorkingScheduleController : ControllerBase
    {
        private readonly UserWorkingScheduleRepository _repo;
        private readonly ILogger<UserWorkingScheduleController> _logger;

        public UserWorkingScheduleController(UserWorkingScheduleRepository repo, ILogger<UserWorkingScheduleController> logger)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("{id}", Name = "GetWorkingSchedule")]
        public ActionResult<UserWorkingSchedule> Get(int id)
        {
            _logger.LogDebug("Get called: Id={Id}", id);

            if (id <= 0)
            {
                _logger.LogWarning("Get called with invalid id: {Id}", id);
                return BadRequest("Id must be provided and greater than zero.");
            }

            try
            {
                var item = _repo.Load(id);
                if (item == null)
                {
                    _logger.LogInformation("UserWorkingSchedule not found: Id={Id}", id);
                    return NotFound($"UserWorkingSchedule with id {id} not found.");
                }

                _logger.LogInformation("UserWorkingSchedule loaded: Id={Id}", id);
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading UserWorkingSchedule Id={Id}", id);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("all", Name = "GetAllWorkingSchedules")]
        public ActionResult<IEnumerable<UserWorkingSchedule>> GetAll()
        {
            _logger.LogDebug("GetAll called");

            try
            {
                var list = _repo.LoadAll();
                if (list == null || list.Count == 0)
                {
                    _logger.LogInformation("GetAll returned no schedules");
                    return NotFound("No schedules found.");
                }

                _logger.LogInformation("GetAll returning {Count} schedules", list.Count);
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading all UserWorkingSchedules");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("user/{userId}", Name = "GetSchedulesByUser")]
        public ActionResult<IEnumerable<UserWorkingSchedule>> GetByUser(int userId)
        {
            _logger.LogDebug("GetByUser called: UserId={UserId}", userId);

            if (userId <= 0)
            {
                _logger.LogWarning("GetByUser called with invalid userId: {UserId}", userId);
                return BadRequest("UserId must be provided and greater than zero.");
            }

            try
            {
                var list = _repo.GetByUser(userId);
                if (list == null || list.Count == 0)
                {
                    _logger.LogInformation("No schedules found for userId={UserId}", userId);
                    return NotFound($"No schedules found for user {userId}.");
                }

                _logger.LogInformation("GetByUser returning {Count} schedules for userId={UserId}", list.Count, userId);
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading schedules for userId={UserId}", userId);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("criteria", Name = "GetSchedulesByCriteria")]
        public ActionResult<IEnumerable<UserWorkingSchedule>> GetByCriteria([FromBody] ScheduleCriteria criteria)
        {
            _logger.LogDebug("GetByCriteria called: {@Criteria}", criteria);

            if (criteria == null)
            {
                _logger.LogWarning("GetByCriteria called with null criteria");
                return BadRequest("Criteria must be provided.");
            }

            try
            {
                var list = _repo.GetByCriteria(
                    userId: criteria.UserId,
                    from: criteria.FromDate,
                    to: criteria.ToDate,
                    clinicId: criteria.ClinicId);
                if (list == null || list.Count == 0)
                {
                    _logger.LogInformation("GetByCriteria returned no schedules for criteria {@Criteria}", criteria);
                    return NotFound("No schedules found matching the criteria.");
                }

                _logger.LogInformation("GetByCriteria returning {Count} schedules for criteria {@Criteria}", list.Count, criteria);
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying schedules by criteria {@Criteria}", criteria);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }

    public class ScheduleCriteria
    {
        public int UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? ClinicId { get; set; }
    }
}