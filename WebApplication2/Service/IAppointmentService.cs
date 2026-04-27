using WebApplication2.DTOs;

namespace WebApplication2.Service;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetAll(string? status = null, string? patientLastName = null);
    Task<AppointmentDetailsDto> GetById(int id);
    Task<int> CreateAppointment(CreateAppointmentDto createAppointmentDto);
    Task<int> UpdateAppointment(UpdateAppointmentDto updateAppointmentDto, int id);
}