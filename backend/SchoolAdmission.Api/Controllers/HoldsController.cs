using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
public class HoldsController : ControllerBase
{
    private readonly HoldService _holds;

    public HoldsController(HoldService holds) => _holds = holds;

    // API 4: POST /classes/{classId}/holds
    [HttpPost("/classes/{classId}/holds")]
    public async Task<IActionResult> Apply(
        string classId, [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        var sid = StudentIdValidator.Require(studentId);
        var created = await _holds.CreateAsync(classId, sid);
        return Created($"/holds/{created.HoldId}", created);
    }

    // API 5: GET /holds/{holdId}
    [HttpGet("/holds/{holdId}")]
    public async Task<IActionResult> Get(string holdId)
        => Ok(await _holds.GetAsync(holdId));

    // API 6: POST /holds/{holdId}/confirm
    [HttpPost("/holds/{holdId}/confirm")]
    public async Task<IActionResult> Confirm(
        string holdId, [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        var sid = StudentIdValidator.Require(studentId);
        return Ok(await _holds.ConfirmAsync(holdId, sid));
    }

    // API 7: DELETE /holds/{holdId}
    [HttpDelete("/holds/{holdId}")]
    public async Task<IActionResult> Cancel(
        string holdId, [FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        var sid = StudentIdValidator.Require(studentId);
        await _holds.CancelAsync(holdId, sid);
        return NoContent();
    }

    // API 8: GET /me/holds
    [HttpGet("/me/holds")]
    public async Task<IActionResult> Mine([FromHeader(Name = "X-Student-Id")] string? studentId)
    {
        var sid = StudentIdValidator.Require(studentId);
        return Ok(await _holds.GetMineAsync(sid));
    }
}