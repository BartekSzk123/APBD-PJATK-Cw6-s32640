using Microsoft.Data.SqlClient;
using WebApplication2.DTOs;
using WebApplication2.Exceptions;

namespace WebApplication2.Service;

public class AppointmentService(IConfiguration configuration) : IAppointmentService
{
    public async Task<IEnumerable<AppointmentListDto>> GetAll(string? status = null, string? patientLastName = null)
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
            throw new NotFound($"Appointment with id: {id} not found");
        }

        return result;
    }

    public async Task<int> CreateAppointment(CreateAppointmentDto createAppointmentDto)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        await connection.OpenAsync();

        command.CommandText = """
                                SELECT IsActive 
                                FROM dbo.Patients 
                                WHERE IdPatient = @IdPatient;
                              """;

        command.Parameters.AddWithValue(@"IdPatient", createAppointmentDto.IdPatient);
        var result = await command.ExecuteScalarAsync();

        if (result is null)
        {
            throw new NotFound($"Patient with id {createAppointmentDto.IdPatient} not found");
        }

        if (!(bool)result)
        {
            throw new NotActive($"Patient with id {createAppointmentDto.IdPatient} is not active");
        }

        command.CommandText = """
                                SELECT IsActive 
                                FROM dbo.Doctors
                                WHERE IdDoctor = @IdDoctor;
                              """;

        command.Parameters.AddWithValue(@"IdDoctor", createAppointmentDto.IdDoctor);
        result = await command.ExecuteScalarAsync();

        if (result is null)
        {
            throw new NotFound($"Doctor with id {createAppointmentDto.IdDoctor} not found");
        }

        if (!(bool)result)
        {
            throw new NotActive($"Doctor with id {createAppointmentDto.IdDoctor} is not active");
        }

        if (createAppointmentDto.Date <= DateTime.Now)
        {
            throw new WrongDate($"Date cannot be  in the past");
        }

        command.CommandText = """
                                SELECT COUNT(1)
                                FROM dbo.Appointments
                                WHERE IdDoctor = @IdDoctor
                                AND AppointmentDate = @AppointmentDate;
                              """;

        command.Parameters.AddWithValue(@"AppointmentDate", createAppointmentDto.Date);

        var count = await command.ExecuteScalarAsync();
        var count2 = Convert.ToInt32(count);

        if (count2 > 0)
        {
            throw new AppointmentConflict("Busy doctor");
        }

        command.CommandText = """
                                INSERT INTO dbo.Appointments (IdPatient, IdDoctor, AppointmentDate,Status,Reason)
                                OUTPUT INSERTED.IdAppointment
                                VALUES (@IdPatient, @IdDoctor, @AppointmentDate, N'Scheduled', @Reason);
                              """;
        
        command.Parameters.AddWithValue(@"Reason", createAppointmentDto.Reason);

        var newId = await command.ExecuteScalarAsync();
        var insertedId =  Convert.ToInt32(newId);
        
        return insertedId;
    }

    public async Task<int> UpdateAppointment(UpdateAppointmentDto updateAppointmentDto, int id)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        await connection.OpenAsync();

        command.CommandText = """
                                SELECT status
                                FROM dbo.Appointments
                                WHERE IdAppointment = @IdAppointment;
                              """;
        command.Parameters.AddWithValue(@"IdAppointment", id);

        var result = await command.ExecuteScalarAsync();
        
        if (result is null)
        {
            throw new NotFound($"Appointment with id: {id} not found");
        }
        
        var status = Convert.ToString(result);

        if (status is "Completed")
        {
            throw new CannotModifyCompletedDate($"Appointment id: {id} is completed! cannot modify date");
        }
        
        
        command.CommandText = """
                                SELECT IsActive 
                                FROM dbo.Patients 
                                WHERE IdPatient = @IdPatient;
                              """;

        command.Parameters.AddWithValue(@"IdPatient", updateAppointmentDto.IdPatient);
        var patient = await command.ExecuteScalarAsync();

        if (patient is null)
        {
            throw new NotFound($"Patient with id {updateAppointmentDto.IdPatient} not found");
        }

        if (!(bool)patient)
        {
            throw new NotActive($"Patient with id {updateAppointmentDto.IdPatient} is not active");
        }
        
        command.CommandText = """
                                SELECT IsActive 
                                FROM dbo.Doctors
                                WHERE IdDoctor = @IdDoctor;
                              """;

        command.Parameters.AddWithValue(@"IdDoctor", updateAppointmentDto.IdDoctor);
        var doctor = await command.ExecuteScalarAsync();

        if (doctor is null)
        {
            throw new NotFound($"Doctor with id {updateAppointmentDto.IdDoctor} not found");
        }

        if (!(bool)doctor)
        {
            throw new NotActive($"Doctor with id {updateAppointmentDto.IdDoctor} is not active");
        }

        if (updateAppointmentDto.Date <= DateTime.Now)
        {
            throw new WrongDate($"Date cannot be  in the past");
        }

        var allowedStatuses = new[] { "Scheduled", "Completed", "Cancelled" };

        if (!allowedStatuses.Contains(updateAppointmentDto.Status))
        {
            throw new WrongStatus($"Status {updateAppointmentDto.Status} is not allowed");
        }
        
        command.CommandText = """
                                SELECT COUNT(1)
                                FROM dbo.Appointments
                                WHERE IdDoctor = @IdDoctor
                                AND AppointmentDate = @AppointmentDate
                                AND IdAppointment <> @IdAppointment;
                              """;

        command.Parameters.AddWithValue(@"AppointmentDate", updateAppointmentDto.Date);

        var count = await command.ExecuteScalarAsync();
        var count2 = Convert.ToInt32(count);

        if (count2 > 0)
        {
            throw new AppointmentConflict("Busy doctor");
        }
        
        command.Parameters.Clear();
        
        command.CommandText = """
                                UPDATE Appointments
                                SET IdPatient = @IdPatient,
                                    IdDoctor = @IdDoctor,
                                    AppointmentDate = @AppointmentDate,
                                    Status = @Status,
                                    Reason = @Reason,
                                    InternalNotes = @InternalNotes
                                WHERE IdAppointment = @Id;
                              """;
        command.Parameters.AddWithValue("@Id", id);
        command.Parameters.AddWithValue("@IdPatient", updateAppointmentDto.IdPatient);
        command.Parameters.AddWithValue("@IdDoctor", updateAppointmentDto.IdDoctor);
        command.Parameters.AddWithValue("@AppointmentDate", updateAppointmentDto.Date);
        command.Parameters.AddWithValue("@Status", updateAppointmentDto.Status);
        command.Parameters.AddWithValue("@Reason", updateAppointmentDto.Reason);
        command.Parameters.AddWithValue("@InternalNotes", updateAppointmentDto.InternalNotes);
        
        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected;
    }

    public async Task<int> DeleteAppointment(int id)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        await connection.OpenAsync();

        command.CommandText = """
                               SELECT status
                               FROM dbo.Appointments
                               Where IdAppointment = @IdAppointment;
                              """;
        command.Parameters.AddWithValue(@"IdAppointment", id);
        var result = await command.ExecuteScalarAsync();

        if (result is null)
        {
            throw new NotFound($"Appointment with id: {id} not found");
        }
        
        var status = Convert.ToString(result);

        if (status is "Completed")
        {
            throw new AppointmentConflict("Cannot delete completed appointment");
        }
        
        command.CommandText = """
                                DELETE FROM Appointments
                                WHERE IdAppointment = @IdAppointment;
                              """;
       
        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected;
    }
}