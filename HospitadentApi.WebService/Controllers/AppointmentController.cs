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
        /// Query appointments by flexible criteria.
        /// Example:
        /// GET api/appointment/search?doctorId=472&from=2025-12-01&to=2025-12-14&clinicId=3&appointmentStatus=1
        /// </summary>
        [HttpGet("search", Name = "GetByCriteria")]
        public ActionResult<IEnumerable<Appointment>> GetByCriteria(
            [FromQuery] int? doctorId = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int? clinicId = null,
            [FromQuery] int? appointmentStatus = null,
            [FromQuery] int? appointmentType = null,
            [FromQuery] int? treatmentType = null,
            [FromQuery] bool? isConfirmed = null)
        {
            try
            {
                var start = from?.Date ?? Tools.GetTurkiyeDate().Date;
                var end = to?.Date ?? start.AddDays(14);
                if (end < start) end = start;

                var list = _appointmentRepository.GetByCriteria(
                    doctorId: doctorId,
                    from: start,
                    to: end,
                    clinicId: clinicId,
                    appointmentStatus: appointmentStatus,
                    appointmentType: appointmentType,
                    treatmentType: treatmentType,
                    isConfirmed: isConfirmed
                );

                if (list == null || list.Count == 0)
                    return NotFound("No appointments found for the given criteria.");

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
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

                var list = _appointmentRepository.GetByCriteria(doctorId, start, end);
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


    