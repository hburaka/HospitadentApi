using HospitadentApi.Entity;
using HospitadentApi.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HospitadentApi.WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly UserRepository _userRepository;
        private readonly ILogger<UserController> _logger;
        private const int MaxPageSize = 500;
        private const int DefaultPageSize = 100;
        private const int MaxSearchReturn = 1000;

        public UserController(UserRepository userRepository, ILogger<UserController> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("{id}", Name = "GetUser")]
        public ActionResult<User> Get(int id)
        {
            _logger.LogDebug("Get called: Id={Id}", id);

            if (id <= 0)
            {
                _logger.LogWarning("Get called with invalid id: {Id}", id);
                return BadRequest("Id must be provided and greater than zero.");
            }

            try
            {
                _logger.LogInformation("Loading user {Id}", id);
                var user = _userRepository.Load(id);
                if (user is null)
                {
                    _logger.LogInformation("User not found: Id={Id}", id);
                    return NotFound();
                }

                _logger.LogInformation("User loaded: Id={Id} Name={Name}", user.Id, user.Name);
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user {Id}", id);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("search", Name = "SearchUsers")]
        public ActionResult<IEnumerable<User>> Search(
            [FromQuery] int? id,
            [FromQuery] string? name,
            [FromQuery] bool? isActive,
            [FromQuery] bool? isDeleted)
        {
            _logger.LogDebug("Search called: id={Id}, name={Name}, isActive={IsActive}, isDeleted={IsDeleted}", id, name, isActive, isDeleted);

            try
            {
                if (!string.IsNullOrEmpty(name) && name.Length > 200)
                {
                    _logger.LogWarning("Search name parameter too long, truncating");
                    name = name.Substring(0, 200);
                }

                var users = _userRepository.GetByCriteria(id.GetValueOrDefault(), name, isActive, isDeleted) ?? new List<User>();

                if (users.Count == 0)
                {
                    _logger.LogInformation("Search returned no users for criteria id={Id} name={Name}", id, name);
                    return NotFound("No users found matching the criteria.");
                }

                if (users.Count > MaxSearchReturn)
                {
                    _logger.LogWarning("Search result truncated: {Count} -> {Max}", users.Count, MaxSearchReturn);
                    users = users.Take(MaxSearchReturn).ToList();
                }

                _logger.LogInformation("Search returning {Count} users for criteria id={Id} name={Name}", users.Count, id, name);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with params id={Id} name={Name}", id, name);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

