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

        [HttpGet(Name = "GetClinic")]
        public IEnumerable<Clinic> Get()
        {
            var clinic = _clinicRepository.Load(1);
            if (clinic is not null) return new List<Clinic> { clinic };
            return Enumerable.Empty<Clinic>();
        }
    }
}
