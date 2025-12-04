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

                // Kullanıcı hem başlangıç hem bitiş tarihi gönderdiyse, bitişin başlangıçtan önce olmaması gerekir.
                if (from.HasValue && to.HasValue && to.Value < from.Value)
                    return BadRequest("Bitiş tarihi, başlangıç tarihinden önce olamaz.");

                if (end < start)
                    end = start;

                // Güvenlik ve performans için, istenen tarih aralığını örnek olarak en fazla 1 yıl ile sınırlandırıyoruz.
                var maxDays = 365;
                if ((end - start).TotalDays > maxDays)
                    return BadRequest($"Tarih aralığı en fazla {maxDays} gün olabilir. Lütfen daha kısa bir aralık seçin.");

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
                    return NotFound("Bu kriterlere uygun herhangi bir randevu bulunamadı.");

                return Ok(list);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                // Log exception here (consider using ILogger)
                return StatusCode(500, "Randevu sorgulama sırasında bir hata oluştu.");
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
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                // Log exception here (consider using ILogger)
                return StatusCode(500, "Doktor randevuları sorgulanırken bir hata oluştu.");
            }
        }
    }
}


    