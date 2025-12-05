// HospitadentApi.WebService\Controllers\UserController.cs
using HospitadentApi.Entity;
using HospitadentApi.Repository;
using Microsoft.AspNetCore.Mvc;
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

        public UserController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // GET api/user/{id}
        [HttpGet("{id}", Name = "GetUser")]
        public ActionResult<IEnumerable<User>> Get(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Id must be provided and greater than zero.", nameof(id));

            try
            {
                var user = _userRepository.Load(id);
                if (user is not null)
                    return new List<User> { user };

                return NotFound($"User with id {id} not found.");
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading user with id {id}", ex);
            }
        }

        //// GET api/user/all
        //[HttpGet("all", Name = "GetUsers")]
        //public ActionResult<IEnumerable<User>> GetAll()
        //{
        //    try
        //    {
        //        if (_userRepository == null)
        //            throw new InvalidOperationException("User repository is not initialized.");

        //        var users = _userRepository.LoadAll() ?? new List<User>();

        //        if (!users.Any())
        //            return NotFound("No users found.");

        //        return Ok(users);
        //    }
        //    catch (InvalidOperationException)
        //    {
        //        throw;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Error loading users", ex);
        //    }
        //}

        // GET api/user/search
        [HttpGet("search", Name = "SearchUsers")]
        public ActionResult<IEnumerable<User>> Search(
            [FromQuery] int? id,
            [FromQuery] string? name,
            [FromQuery] bool? isActive,
            [FromQuery] bool? isDeleted)
        {
            try
            {
                var users = _userRepository.GetByCriteria(id.GetValueOrDefault(), name, isActive, isDeleted);
                if (users == null || !users.Any())
                    return NotFound("No users found matching the criteria.");
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}

