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
    public class DoctorBranchCodeController : ControllerBase
    {
        private readonly DoctorBranchCodeRepository _repository;

        public DoctorBranchCodeController(DoctorBranchCodeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        // GET api/doctorbranchcode/{id}
        [HttpGet("{id}", Name = "GetDoctorBranchCode")]
        public ActionResult<DoctorBranchCode> Get(int id)
        {
            // validation: ensure id is supplied and valid
            if (id <= 0)
                throw new ArgumentException("Id must be provided and greater than zero.", nameof(id));

            try
            {
                var item = _repository.Load(id);
                if (item is not null)
                    return Ok(item);

                return NotFound($"DoctorBranchCode with id {id} not found.");
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading DoctorBranchCode with id {id}", ex);
            }
        }

        // GET api/doctorbranchcode/all
        [HttpGet("all", Name = "GetDoctorBranchCodes")]
        public ActionResult<IEnumerable<DoctorBranchCode>> GetAll()
        {
            try
            {
                if (_repository == null)
                    throw new InvalidOperationException("DoctorBranchCodeRepository is not initialized.");

                var items = _repository.LoadAll() ?? new List<DoctorBranchCode>();

                if (!items.Any())
                    return NotFound("No doctor branch codes found.");

                return Ok(items);
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading doctor branch codes", ex);
            }
        }
    }
}