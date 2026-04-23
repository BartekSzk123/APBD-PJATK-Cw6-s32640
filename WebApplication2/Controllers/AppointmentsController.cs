using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebApplication2.DTOs;
using WebApplication2.Service;

namespace WebApplication2.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(IAppointmentService appointmentService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(string? status = null, string? patientLastName = null)
    {
        return Ok(await appointmentService.GetAll(status, patientLastName));
    }
}