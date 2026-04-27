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

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAppointment(int id, [FromBody] UpdateAppointmentDto updateAppointmentDto)
    {
        try
        {
            var result = await appointmentService.UpdateAppointment(updateAppointmentDto, id);
            return NoContent();
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
        catch(WrongStatus e)
        {
            return BadRequest(e.Message);
        }
        catch(CannotModifyCompletedDate e)
        {
            return Conflict(e.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        try
        {
            var result = await appointmentService.DeleteAppointment(id);
            return NoContent();
        }catch(NotFound e)
        {
            return NotFound(e.Message);
        }catch(AppointmentConflict e)
        {
            return Conflict(e.Message);
        }
    }
    
}