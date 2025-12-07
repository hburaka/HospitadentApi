using System;
using System.Collections.Generic;
using System.Linq;
using HospitadentApi.Entity;
using HospitadentApi.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HospitadentApi.WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorAvailabilityController : ControllerBase
    {
        private readonly AppointmentRepository _appointmentRepository;
        private readonly UserWorkingScheduleRepository _workingScheduleRepository;
        private readonly ILogger<DoctorAvailabilityController> _logger;

        // Limits / defaults to protect the API from expensive requests
        private const int DefaultSlotMinutes = 30;
        private const int MinSlotMinutes = 5;
        private const int MaxSlotMinutes = 480; // 8 hours
        private const int MaxDaysRange = 60;
        private const int MaxReturnedSlots = 5000;

        public DoctorAvailabilityController(
            AppointmentRepository appointmentRepository,
            UserWorkingScheduleRepository workingScheduleRepository,
            ILogger<DoctorAvailabilityController> logger)
        {
            _appointmentRepository = appointmentRepository ?? throw new ArgumentNullException(nameof(appointmentRepository));
            _workingScheduleRepository = workingScheduleRepository ?? throw new ArgumentNullException(nameof(workingScheduleRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("doctor/{doctorId}/slots", Name = "GetDoctorAvailableSlots")]
        public ActionResult<IEnumerable<DoctorAvailableSlotDto>> GetDoctorAvailableSlots(
            int doctorId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? clinicId,
            [FromQuery] int slotMinutes = DefaultSlotMinutes)
        {
            _logger.LogDebug("GetDoctorAvailableSlots called: doctorId={DoctorId}, from={From}, to={To}, clinicId={ClinicId}, slotMinutes={SlotMinutes}",
                doctorId, from, to, clinicId, slotMinutes);

            if (doctorId <= 0)
            {
                _logger.LogWarning("Invalid doctorId: {DoctorId}", doctorId);
                return BadRequest("doctorId must be greater than zero.");
            }

            if (slotMinutes < MinSlotMinutes || slotMinutes > MaxSlotMinutes)
            {
                _logger.LogWarning("Invalid slotMinutes: {SlotMinutes}", slotMinutes);
                return BadRequest($"slotMinutes must be between {MinSlotMinutes} and {MaxSlotMinutes}.");
            }

            try
            {
                var start = from?.Date ?? Tools.GetTurkiyeDate().Date;
                var end = to?.Date ?? start.AddDays(14);

                _logger.LogInformation("Computing availability for doctor {DoctorId} between {Start} and {End}", doctorId, start, end);

                if (from.HasValue && to.HasValue && to.Value < from.Value)
                {
                    _logger.LogWarning("GetDoctorAvailableSlots called with end < start: from={From} to={To}", from, to);
                    return BadRequest("Bitiş tarihi, başlangıç tarihinden önce olamaz.");
                }

                if (end < start)
                    end = start;

                if ((end - start).TotalDays > MaxDaysRange)
                {
                    _logger.LogWarning("Requested date range too large: {Days} days", (end - start).TotalDays);
                    return BadRequest($"Tarih aralığı en fazla {MaxDaysRange} gün olabilir. Lütfen daha kısa bir aralık seçin.");
                }

                IList<UserWorkingSchedule> schedules;
                try
                {
                    schedules = _workingScheduleRepository.GetByCriteria(
                        userId: doctorId,

                sb, row, from: start,
                        to: end,
                        clinicId: clinicId);
                    _logger.LogInformation("Loaded {Count} working schedule rows", schedules?.Count ?? 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading working schedules for doctor {DoctorId}", doctorId);
                    if (IsMySqlDateError(ex))
                    {
                        return StatusCode(500,
                            "Doktor çalışma saatleri okunurken bir veri hatası oluştu. Lütfen sistem yöneticisine başvurun. (MySQL tarih dönüşüm hatası)");
                    }
                    throw;
                }

                if (schedules == null || schedules.Count == 0)
                {
                    _logger.LogInformation("No schedules found for doctor {DoctorId} in range", doctorId);
                    return NotFound("Bu doktor için belirtilen tarih aralığında tanımlı çalışma saati bulunamadı.");
                }

                // Partition schedules into explicit-date ones and recurring-by-day ones
                var explicitByDate = new Dictionary<DateTime, List<UserWorkingSchedule>>();
                var recurringByDay = new Dictionary<DayOfWeek, List<UserWorkingSchedule>>();

                foreach (var s in schedules)
                {
                    if (s.CustomDate.HasValue && s.CustomDate.Value != DateTime.MinValue && s.CustomDate.Value.Year >= 1900)
                    {
                        var dt = s.CustomDate.Value.Date;
                        if (!explicitByDate.TryGetValue(dt, out var list)) { list = new List<UserWorkingSchedule>(); explicitByDate[dt] = list; }
                        list.Add(s);
                    }
                    else if (!string.IsNullOrEmpty(s.Day))
                    {
                        if (Enum.TryParse<DayOfWeek>(s.Day, true, out var dow))
                        {
                            if (!recurringByDay.TryGetValue(dow, out var list)) { list = new List<UserWorkingSchedule>(); recurringByDay[dow] = list; }
                            list.Add(s);
                        }
                        else
                        {
                            _logger.LogDebug("Skipping schedule with unrecognized Day value: {Day}", s.Day);
                        }
                    }
                }

                // Load appointments in range once
                IList<Appointment> appointments;
                try
                {
                    appointments = _appointmentRepository.GetByCriteria(
                        doctorId: doctorId,
                        from: start,
                        to: end,
                        clinicId: clinicId,
                        appointmentStatus: null,
                        appointmentType: null,
                        treatmentType: null,
                        isConfirmed: null);
                    _logger.LogInformation("Loaded {Count} appointments for doctor {DoctorId}", appointments?.Count ?? 0, doctorId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading appointments for doctor {DoctorId}", doctorId);
                    if (IsMySqlDateError(ex))
                    {
                        return StatusCode(500,
                            "Randevu bilgileri okunurken bir veri hatası oluştu. Lütfen sistem yöneticisine başvurun. (MySQL tarih dönüşüm hatası)");
                    }
                    throw;
                }

                // Group appointments by date for fast lookup
                var appointmentsByDate = (appointments ?? new List<Appointment>())
                    .GroupBy(a => a.StartDate.Date)
                    .ToDictionary(g => g.Key, g => g.OrderBy(a => a.StartDate).ToList());

                var slotLength = TimeSpan.FromMinutes(slotMinutes);
                var result = new List<DoctorAvailableSlotDto>(256);

                // Iterate each date in range once, combine explicit and recurring for that date
                for (var cur = start.Date; cur <= end.Date; cur = cur.AddDays(1))
                {
                    if (!explicitByDate.TryGetValue(cur, out var listForDate))
                        listForDate = new List<UserWorkingSchedule>();

                    if (recurringByDay.TryGetValue(cur.DayOfWeek, out var recList))
                        listForDate.AddRange(recList);

                    if (listForDate.Count == 0)
                        continue;

                    // For this date, get appointments (sorted)
                    appointmentsByDate.TryGetValue(cur, out var dayAppointments);
                    dayAppointments ??= new List<Appointment>();

                    // Build slots for each working schedule on this date
                    foreach (var schedule in listForDate)
                    {
                        // Validate schedule times
                        var startTime = schedule.StartTime;
                        var endTime = schedule.EndTime;

                        if (startTime == TimeSpan.Zero && endTime == TimeSpan.Zero)
                            continue;

                        var workStart = cur.Add(startTime);
                        var workEnd = cur.Add(endTime);

                        if (workEnd <= workStart)
                            continue;

                        for (var slotStart = workStart; slotStart + slotLength <= workEnd; slotStart = slotStart.Add(slotLength))
                        {
                            var slotEnd = slotStart.Add(slotLength);

                            // simple conflict check; dayAppointments is ordered which helps if large
                            var hasConflict = dayAppointments.Any(a => a.StartDate < slotEnd && a.EndDate > slotStart);
                            if (hasConflict)
                                continue;

                            result.Add(new DoctorAvailableSlotDto
                            {
                                DoctorId = doctorId,
                                ClinicId = schedule.ClinicId,
                                Date = cur,
                                Start = slotStart,
                                End = slotEnd
                            });

                            if (result.Count >= MaxReturnedSlots)
                            {
                                _logger.LogWarning("Result truncated at {MaxReturnedSlots} slots for doctor {DoctorId}", MaxReturnedSlots, doctorId);
                                Response.Headers["X-Result-Truncated"] = "true";
                                // Return early with truncated result to protect the server
                                return Ok(result.OrderBy(x => x.Date).ThenBy(x => x.Start).ToList());
                            }
                        }
                    }
                }

                _logger.LogInformation("Available slots found: {Count}", result.Count);

                if (result.Count == 0)
                {
                    _logger.LogInformation("No available slots for doctor {DoctorId}", doctorId);
                    return NotFound("Belirtilen tarih aralığında uygun randevu slotu bulunamadı.");
                }

                var ordered = result.OrderBy(x => x.Date).ThenBy(x => x.Start).ToList();
                return Ok(ordered);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Bad request in GetDoctorAvailableSlots");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error while computing availability for doctor {DoctorId}", doctorId);
                var errorDetail = ex.Message;
                if (ex.InnerException != null)
                    errorDetail += $" | İç Hata: {ex.InnerException.Message}";
                return StatusCode(500, $"Doktor uygunluk slotları hesaplanırken bir hata oluştu. Detay: {errorDetail}");
            }
        }

        private static bool IsMySqlDateError(Exception ex)
        {
            if (ex == null) return false;
            var msg = ex.Message ?? string.Empty;
            if (msg.Contains("Unable to convert MySQL date/time", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("MySQL date/time", StringComparison.OrdinalIgnoreCase))
                return true;
            if (ex.InnerException != null)
                return IsMySqlDateError(ex.InnerException);
            return false;
        }
    }

    public class DoctorAvailableSlotDto
    {
        public int DoctorId { get; set; }
        public int ClinicId { get; set; }
        public DateTime Date { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}


