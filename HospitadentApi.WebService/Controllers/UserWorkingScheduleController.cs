// HospitadentApi.WebService\Controllers\UserWorkingScheduleController.cs
using HospitadentApi.Entity;
using HospitadentApi.Repository;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace HospitadentApi.WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserWorkingScheduleController : ControllerBase
    {
        private readonly UserWorkingScheduleRepository _repo;

        public UserWorkingScheduleController(UserWorkingScheduleRepository repo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        // GET api/userworkingschedule/{id}
        [HttpGet("{id}", Name = "GetWorkingSchedule")]
        public ActionResult<UserWorkingSchedule> Get(int id)
        {
            if (id <= 0) return BadRequest("Id must be provided and greater than zero.");
            try
            {
                var item = _repo.Load(id);
                if (item == null) return NotFound($"UserWorkingSchedule with id {id} not found.");
                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/userworkingschedule/all
        [HttpGet("all", Name = "GetAllWorkingSchedules")]
        public ActionResult<IEnumerable<UserWorkingSchedule>> GetAll()
        {
            try
            {
                var list = _repo.LoadAll();
                if (list == null || list.Count == 0) return NotFound("No schedules found.");
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET api/userworkingschedule/user/{userId}
        [HttpGet("user/{userId}", Name = "GetSchedulesByUser")]
        public ActionResult<IEnumerable<UserWorkingSchedule>> GetByUser(int userId)
        {
            if (userId <= 0) return BadRequest("UserId must be provided and greater than zero.");
            try
            {
                var list = _repo.GetByUser(userId);
                if (list == null || list.Count == 0) return NotFound($"No schedules found for user {userId}.");
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST api/userworkingschedule/criteria
        [HttpPost("criteria", Name = "GetSchedulesByCriteria")]
        public ActionResult<IEnumerable<UserWorkingSchedule>> GetByCriteria([FromBody] ScheduleCriteria criteria)
        {
            if (criteria == null) return BadRequest("Criteria must be provided.");
            try
            {
                var list = _repo.GetByCriteria(criteria.UserId, criteria.FromDate, criteria.ToDate, criteria.ClinicId);
                if (list == null || list.Count == 0) return NotFound("No schedules found matching the criteria.");
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // Note: PUT/DELETE can be added when repository update/remove methods are implemented.
    }

    public class ScheduleCriteria
    {
        public int UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? ClinicId { get; set; }
    }
}