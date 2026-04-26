using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebApplication2.DTOs;
using WebApplication2.Exceptions;
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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            return Ok(await appointmentService.GetById(id));
        }
        catch (NotFound e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto createAppointmentDto)
    {
        try
        {
            var newId = await appointmentService.CreateAppointment(createAppointmentDto);
            return CreatedAtAction(nameof(GetById), new { id = newId }, createAppointmentDto);
        }
        catch (NotFound e)
        {
            return NotFound(e.Message);
        }
        catch (AppointmentConflict e)
        {
            return Conflict(e.Message);
        }
        catch (NotActive e)
        {
            return BadRequest(e.Message);
        }
        catch (WrongDate e)
        {
            return BadRequest(e.Message);
        }
    }
}