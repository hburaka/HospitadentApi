using System;
using System.Collections.Generic;
using HospitadentApi.Entity;
using HospitadentApi.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HospitadentApi.WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentController : ControllerBase
    {
        private readonly AppointmentRepository _appointmentRepository;
        private readonly ILogger<AppointmentController> _logger;

        public AppointmentController(AppointmentRepository appointmentRepository, ILogger<AppointmentController> logger)
        {
            _appointmentRepository = appointmentRepository ?? throw new ArgumentNullException(nameof(appointmentRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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

                _logger.LogInformation("GetByCriteria called: doctorId={DoctorId}, from={From}, to={To}, clinicId={ClinicId}, appointmentStatus={AppointmentStatus}, appointmentType={AppointmentType}, treatmentType={TreatmentType}, isConfirmed={IsConfirmed}",
                    doctorId, start, end, clinicId, appointmentStatus, appointmentType, treatmentType, isConfirmed);

                if (from.HasValue && to.HasValue && to.Value < from.Value)
                {
                    _logger.LogWarning("GetByCriteria called with end < start: from={From} to={To}", from, to);
                    return BadRequest("Bitiş tarihi, başlangıç tarihinden önce olamaz.");
                }

                if (end < start) end = start;

                var maxDays = 365;
                if ((end - start).TotalDays > maxDays)
                {
                    _logger.LogWarning("GetByCriteria requested too large date range: {Days} days", (end - start).TotalDays);
                    return BadRequest($"Tarih aralığı en fazla {maxDays} gün olabilir. Lütfen daha kısa bir aralık seçin.");
                }

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
                {
                    _logger.LogInformation("GetByCriteria found no appointments for given criteria");
                    return NotFound("Bu kriterlere uygun herhangi bir randevu bulunamadı.");
                }

                _logger.LogInformation("GetByCriteria returning {Count} appointments", list.Count);
                return Ok(list);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "GetByCriteria bad request");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in GetByCriteria");
                return StatusCode(500, "Randevu sorgulama sırasında bir hata oluştu.");
            }
        }

        [HttpGet("doctor/{doctorId}", Name = "GetDoctorAppointments")]
        public ActionResult<IEnumerable<Appointment>> GetDoctorAppointments(
            int doctorId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to)
        {
            if (doctorId <= 0)
            {
                _logger.LogWarning("GetDoctorAppointments called with invalid doctorId: {DoctorId}", doctorId);
                return BadRequest("doctorId must be greater than zero.");
            }

            try
            {
                var start = from?.Date ?? Tools.GetTurkiyeDate().Date;
                var end = to?.Date ?? start.AddDays(14);
                if (end < start) end = start;

                _logger.LogInformation("GetDoctorAppointments called: doctorId={DoctorId}, from={From}, to={To}", doctorId, start, end);

                var list = _appointmentRepository.GetByCriteria(doctorId, start, end);
                if (list == null || list.Count == 0)
                {
                    _logger.LogInformation("GetDoctorAppointments found no appointments for doctorId={DoctorId}", doctorId);
                    return NotFound("No appointments found for the given doctor and date range.");
                }

                _logger.LogInformation("GetDoctorAppointments returning {Count} appointments for doctorId={DoctorId}", list.Count, doctorId);
                return Ok(list);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "GetDoctorAppointments bad request for doctorId={DoctorId}", doctorId);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in GetDoctorAppointments for doctorId={DoctorId}", doctorId);
                return StatusCode(500, "Doktor randevuları sorgulanırken bir hata oluştu.");
            }
        }
    }
}