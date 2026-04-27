using System.ComponentModel.DataAnnotations;

namespace WebApplication2.DTOs;

public class UpdateAppointmentDto
{
    [Required]
    public int IdPatient { get; set; } 
    [Required]
    public int IdDoctor { get; set; } 
    [Required]
    public DateTime Date { get; set; }
    [Required]
    public string Status { get; set; } = string.Empty;
    [Required,MaxLength(250)]
    public string Reason { get; set; } = string.Empty;
    [Required]
    public string InternalNotes { get; set; } = string.Empty;
}