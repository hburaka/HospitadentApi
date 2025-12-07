// HospitadentApi.WebService\Controllers\UserController.cs
using HospitadentApi.Entity;
using HospitadentApi.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HospitadentApi.WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

        // GET api/user/{id}  -- single resource, return User (not IEnumerable<User>)
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

        //// GET api/user/all?page=1&pageSize=100  -- safe paged GetAll
        //[HttpGet("all", Name = "GetUsers")]
        //public ActionResult<IEnumerable<User>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = DefaultPageSize)
        //{
        //    _logger.LogDebug("GetAll called: page={Page} pageSize={PageSize}", page, pageSize);

        //    if (page <= 0) page = 1;
        //    if (pageSize <= 0) pageSize = DefaultPageSize;
        //    pageSize = Math.Min(pageSize, MaxPageSize);

        //    try
        //    {
        //        var users = _userRepository.LoadAll() ?? new List<User>();
        //        if (!users.Any())
        //        {
        //            _logger.LogInformation("GetAll returned no users");
        //            return NotFound("No users found.");
        //        }

        //        var paged = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        //        _logger.LogInformation("GetAll returning {Count} users (page={Page})", paged.Count, page);
        //        return Ok(paged);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error loading users");
        //        return StatusCode(500, $"Internal server error: {ex.Message}");
        //    }
        //}

        // GET api/user/search?id=...&name=...  -- safe GET for simple filters, apply caps

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

