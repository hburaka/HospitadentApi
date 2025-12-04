using System;
using System.Collections.Generic;
using System.Linq;
using HospitadentApi.Entity;
using HospitadentApi.Repository;
using Microsoft.AspNetCore.Mvc;

namespace HospitadentApi.WebService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DoctorAvailabilityController : ControllerBase
    {
        private readonly AppointmentRepository _appointmentRepository;
        private readonly UserWorkingScheduleRepository _workingScheduleRepository;

        public DoctorAvailabilityController(
            AppointmentRepository appointmentRepository,
            UserWorkingScheduleRepository workingScheduleRepository)
        {
            _appointmentRepository = appointmentRepository ?? throw new ArgumentNullException(nameof(appointmentRepository));
            _workingScheduleRepository = workingScheduleRepository ?? throw new ArgumentNullException(nameof(workingScheduleRepository));
        }
        [HttpGet("doctor/{doctorId}/slots", Name = "GetDoctorAvailableSlots")]
        public ActionResult<IEnumerable<DoctorAvailableSlotDto>> GetDoctorAvailableSlots(
            int doctorId,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int? clinicId,
            [FromQuery] int slotMinutes = 30)
        {
            if (doctorId <= 0)
                return BadRequest("doctorId must be greater than zero.");

            if (slotMinutes <= 0 || slotMinutes > 480)
                return BadRequest("slotMinutes 0'dan büyük ve 480'den (8 saat) küçük/eşit olmalıdır.");

            try
            {
                var start = from?.Date ?? Tools.GetTurkiyeDate().Date;
                var end = to?.Date ?? start.AddDays(14);

                if (from.HasValue && to.HasValue && to.Value < from.Value)
                    return BadRequest("Bitiş tarihi, başlangıç tarihinden önce olamaz.");

                if (end < start)
                    end = start;

                // Güvenlik ve performans için, tarih aralığını örnek olarak en fazla 60 gün ile sınırlandırıyoruz.
                var maxDays = 60;
                if ((end - start).TotalDays > maxDays)
                    return BadRequest($"Tarih aralığı en fazla {maxDays} gün olabilir. Lütfen daha kısa bir aralık seçin.");

                // 1) İlgili tarih aralığında hekimin çalışma saatlerini çek
                IList<UserWorkingSchedule> schedules;
                try
                {
                    schedules = _workingScheduleRepository.GetByCriteria(
                        userId: doctorId,
                        from: start,
                        to: end,
                        clinicId: clinicId);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Unable to convert MySQL date/time") || 
                        ex.Message.Contains("MySQL date/time") ||
                        (ex.InnerException != null && ex.InnerException.Message.Contains("Unable to convert MySQL date/time")))
                    {
                        return StatusCode(500, 
                            "Doktor çalışma saatleri okunurken bir veri hatası oluştu. " +
                            "Lütfen sistem yöneticisine başvurun. (MySQL tarih dönüşüm hatası)");
                    }
                    throw;
                }

                if (schedules == null || schedules.Count == 0)
                    return NotFound("Bu doktor için belirtilen tarih aralığında tanımlı çalışma saati bulunamadı.");

                var expandedSchedules = new List<UserWorkingSchedule>();
                var dateRange = Enumerable.Range(0, (end - start).Days + 1)
                                          .Select(d => start.AddDays(d))
                                          .ToList();

                foreach (var schedule in schedules)
                {
                    if (schedule.CustomDate.HasValue && 
                        schedule.CustomDate.Value != DateTime.MinValue && 
                        schedule.CustomDate.Value.Year >= 1900)
                    {
                        expandedSchedules.Add(schedule);
                    }
                    else if (!string.IsNullOrEmpty(schedule.Day))
                    {
                        foreach (var date in dateRange)
                        {
                            if (string.Equals(date.DayOfWeek.ToString(), schedule.Day, StringComparison.OrdinalIgnoreCase))
                            {
                                var expandedSchedule = new UserWorkingSchedule
                                {
                                    Id = schedule.Id,
                                    CompanyId = schedule.CompanyId,
                                    ClinicId = schedule.ClinicId,
                                    UserId = schedule.UserId,
                                    CustomDate = date, // Tarih aralığındaki güne göre set et
                                    Day = schedule.Day,
                                    StartTime = schedule.StartTime,
                                    EndTime = schedule.EndTime,
                                    SavedBy = schedule.SavedBy,
                                    SavedOn = schedule.SavedOn,
                                    UpdatedBy = schedule.UpdatedBy,
                                    UpdatedOn = schedule.UpdatedOn,
                                    IsDeleted = schedule.IsDeleted,
                                    DeletedBy = schedule.DeletedBy,
                                    DeletedOn = schedule.DeletedOn
                                };
                                expandedSchedules.Add(expandedSchedule);
                            }
                        }
                    }
                }

                schedules = expandedSchedules;

                if (schedules == null || schedules.Count == 0)
                    return NotFound("Bu doktor için belirtilen tarih aralığında tanımlı çalışma saati bulunamadı.");

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
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Unable to convert MySQL date/time") || 
                        ex.Message.Contains("MySQL date/time") ||
                        (ex.InnerException != null && ex.InnerException.Message.Contains("Unable to convert MySQL date/time")))
                    {
                        return StatusCode(500, 
                            "Randevu bilgileri okunurken bir veri hatası oluştu. " +
                            "Lütfen sistem yöneticisine başvurun. (MySQL tarih dönüşüm hatası)");
                    }
                    throw;
                }

                // randevuları güne göre gruplayalım (performans için)
                var appointmentsByDate = (appointments ?? new List<Appointment>())
                    .GroupBy(a => a.StartDate.Date)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var slotLength = TimeSpan.FromMinutes(slotMinutes);
                var result = new List<DoctorAvailableSlotDto>();

                // 3) Her çalışma kaydı için, çalışma saatlerini slotlara böl ve randevu çakışmalarını filtrele
                foreach (var schedule in schedules)
                {
                    if (!schedule.CustomDate.HasValue || schedule.CustomDate.Value == DateTime.MinValue)
                        continue;

                    var day = schedule.CustomDate.Value.Date;
                    if (day == DateTime.MinValue || day.Year < 1900)
                        continue;

                    if (schedule.StartTime == TimeSpan.Zero && schedule.EndTime == TimeSpan.Zero)
                        continue;

                    var workStart = day.Add(schedule.StartTime);
                    var workEnd = day.Add(schedule.EndTime);

                    if (workEnd <= workStart)
                        continue;

                    // ilgili gündeki randevuları al
                    appointmentsByDate.TryGetValue(day, out var dayAppointments);
                    dayAppointments ??= new List<Appointment>();

                    for (var slotStart = workStart; slotStart + slotLength <= workEnd; slotStart = slotStart.Add(slotLength))
                    {
                        var slotEnd = slotStart.Add(slotLength);

                        // Bu slot herhangi bir randevu ile çakışıyor mu?
                        var hasConflict = dayAppointments.Any(a =>
                            a.StartDate < slotEnd && a.EndDate > slotStart);

                        if (!hasConflict)
                        {
                            result.Add(new DoctorAvailableSlotDto
                            {
                                DoctorId = doctorId,
                                ClinicId = schedule.ClinicId,
                                Date = day,
                                Start = slotStart,
                                End = slotEnd
                            });
                        }
                    }
                }

                if (result.Count == 0)
                    return NotFound("Belirtilen tarih aralığında uygun randevu slotu bulunamadı.");

                // Tarihe ve saate göre sırala
                result = result
                    .OrderBy(x => x.Date)
                    .ThenBy(x => x.Start)
                    .ToList();

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // NOT: Debug amaçlı olarak hata detayını dışarı veriyoruz.
                // Canlıya alırken sadece genel bir mesaj dönecek şekilde daraltabilirsiniz.
                var errorDetail = ex.Message;
                if (ex.InnerException != null)
                    errorDetail += $" | İç Hata: {ex.InnerException.Message}";
                return StatusCode(500, $"Doktor uygunluk slotları hesaplanırken bir hata oluştu. Detay: {errorDetail}");
            }
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


