using System.ComponentModel.DataAnnotations;

namespace WebApplication2.DTOs;

public class CreateAppointmentDto
{
    [Required]
    public int IdPatient { get; set; } 
    [Required]
    public int IdDoctor { get; set; } 
    [Required]
    public DateTime Date { get; set; }
    [Required,MaxLength(250)]
    public string Reason { get; set; } = string.Empty;
}