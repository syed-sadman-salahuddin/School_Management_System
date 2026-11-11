using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Services;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Route("classes")]
public class ClassesController : ControllerBase
{
    private readonly ClassService _classes;

    public ClassesController(ClassService classes) => _classes = classes;

    // API 1: POST /classes
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateClassRequest request)
    {
        var created = await _classes.CreateAsync(request);
        return Created($"/classes/{created.Id}", created);
    }

    // API 2: GET /classes
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _classes.GetAllAsync());

    // API 3: GET /classes/{classId}
    [HttpGet("{classId}")]
    public async Task<IActionResult> GetById(string classId)
        => Ok(await _classes.GetByIdAsync(classId));
}