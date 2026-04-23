using WebApplication2.DTOs;

namespace WebApplication2.Service;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetAll(string? status = null, string? patientLastName = null);
}