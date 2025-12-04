using System;
using System.Collections.Generic;
using HospitadentApi.Entity;
using HospitadentApi.Repository;
using Microsoft.AspNetCore.Mvc;

namespace HospitadentApi.WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly AppointmentRepository _appointmentRepository;

        public AppointmentController(AppointmentRepository appointmentRepository)
        {
            _appointmentRepository = appointmentRepository ?? throw new ArgumentNullException(nameof(appointmentRepository));
        }

        /// <summary>
        /// Belirli bir doktor için, verilen tarih aralığında randevuları getirir.
        /// Örnek: GET api/appointment/doctor/472?from=2025-12-01&to=2025-12-14
        /// from/to gönderilmezse, bugün + 14 gün varsayılır.
        /// </summary>
        [HttpGet("doctor/{doctorId}", Name = "GetDoctorAppointments")]
        public ActionResult<IEnumerable<Appointment>> GetDoctorAppointments(
            int doctorId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            if (doctorId <= 0)
                return BadRequest("doctorId must be greater than zero.");

            try
            {
                var start = from?.Date ?? Tools.GetTurkiyeDate().Date;
                var end = to?.Date ?? start.AddDays(14);
                if (end < start)
                    end = start;

                var list = _appointmentRepository.GetByDoctorAndDateRange(doctorId, start, end);
                if (list == null || list.Count == 0)
                    return NotFound("No appointments found for the given doctor and date range.");

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}


