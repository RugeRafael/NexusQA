using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QACopilot.API.Helpers;
using QACopilot.Application.DTOs.TestCases;
using QACopilot.Application.Interfaces.Services;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/testcases")]
[Authorize]
public class TestCasesController : ControllerBase
{
    private readonly ITestCaseService _testCaseService;
    private readonly ILogger<TestCasesController> _logger;

    public TestCasesController(
        ITestCaseService testCaseService,
        ILogger<TestCasesController> logger)
    {
        _testCaseService = testCaseService;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateTestCaseDto request)
    {
        var userId = Guid.Parse(User.FindFirst("uid")!.Value);
        var result = await _testCaseService.GenerateAsync(request, userId);
        return Ok(ApiResponse<TestCaseResponseDto>.Ok(
            result, "Test cases generated successfully."));
    }
}