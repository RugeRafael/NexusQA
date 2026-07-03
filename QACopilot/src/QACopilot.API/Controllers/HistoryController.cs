using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QACopilot.API.Helpers;
using QACopilot.Application.DTOs.TestCases;
using QACopilot.Application.Interfaces.Services;

namespace QACopilot.API.Controllers;

[ApiController]
[Route("api/history")]
[Authorize]
public class HistoryController : ControllerBase
{
    private readonly ITestCaseService _testCaseService;

    public HistoryController(ITestCaseService testCaseService)
    {
        _testCaseService = testCaseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _testCaseService.GetHistoryAsync(page, pageSize);
        return Ok(ApiResponse<PagedResultDto<TestCaseHistoryDto>>.Ok(result));
    }
}