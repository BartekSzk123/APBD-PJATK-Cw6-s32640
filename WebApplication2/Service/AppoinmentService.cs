using Microsoft.Data.SqlClient;
using WebApplication2.DTOs;
using WebApplication2.Exceptions;

namespace WebApplication2.Service;

public class AppoinmentService(IConfiguration configuration) : IAppointmentService
{
    public async Task<IEnumerable<AppointmentListDto>> GetAll(string? status = null,  string? patientLastName = null)
    {
        List<AppointmentListDto> result = [];

        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = """
                                SELECT
                                  a.IdAppointment,
                                  a.AppointmentDate,
                                  a.Status,
                                  a.Reason,
                                  p.FirstName + N' ' + p.LastName AS PatientFullName,
                                  p.Email AS PatientEmail
                              FROM dbo.Appointments a
                              JOIN dbo.Patients p ON p.IdPatient = a.IdPatient
                              WHERE (@Status IS NULL OR a.Status = @Status)
                                AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
                              ORDER BY a.AppointmentDate;
                              """;
        
        command.Parameters.AddWithValue(@"Status", (object?)status ?? DBNull.Value);
        command.Parameters.AddWithValue(@"PatientLastName", (object?)patientLastName ?? DBNull.Value);
        
        await connection.OpenAsync();
        var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = reader.GetString(5),
            });
        }
        return result;
    }

    public async Task<AppointmentDetailsDto> GetById(int id)
    {
        AppointmentDetailsDto result = null;
        
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        command.CommandText = """
                               SELECT a.IdAppointment, 
                                     a.AppointmentDate, 
                                     a.Status, 
                                     a.Reason,
                                     p.FirstName + N' ' + p.LastName AS PatientFullName,
                                     p.Email AS PatientEmail,
                                     doc.FirstName + N' ' + doc.LastName AS DoctorFullName,
                                     s.Name
                              FROM dbo.Appointments a
                              LEFT JOIN dbo.Patients p ON a.IdPatient = p.IdPatient
                              LEFT JOIN dbo.Doctors doc ON a.IdDoctor = doc.IdDoctor
                              LEFT JOIN dbo.Specializations s ON doc.IdSpecialization = s.IdSpecialization
                              WHERE a.IdAppointment = @Id;
                              """;
        command.Parameters.AddWithValue(@"Id", id);

        await connection.OpenAsync();
        var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (result is null)
            {
                result = new AppointmentDetailsDto
                {
                    IdAppointment = reader.GetInt32(0),
                    AppointmentDate = reader.GetDateTime(1),
                    Status = reader.GetString(2),
                    Reason = reader.GetString(3),
                    PatientFullName = reader.GetString(4),
                    PatientEmail = reader.GetString(5),
                    DoctorFullName = reader.GetString(6),
                    Specialization = reader.GetString(7)
                };
            }
        }

        if (result is null)
        {
            throw new AppointmentNotFound($"Appointment with id: {id} not found");
        }
        return result;
    }
}