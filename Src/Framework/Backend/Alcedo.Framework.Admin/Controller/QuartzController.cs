using Alcedo.Core.Presentation;
using Alcedo.Framework.Admin.Application;
using Alcedo.Framework.Shared.Controller;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Alcedo.Framework.Admin.Controller;

[ApiVersion("1.0")]
[Area("admin")]
[Resource("Quartz", "Manage Quartz Jobs")]
public sealed class QuartzController : AlcedoFmApiAreaControllerBase
{
    private readonly IQuartzAppService _quartzAppService;

    public QuartzController(IQuartzAppService quartzAppService)
        => _quartzAppService = quartzAppService;

    [HttpGet("jobs")]
    [Operation("List", "List all jobs")]
    public async Task<ActionResult<List<JobDto>>> GetJobs()
        => Ok(await _quartzAppService.GetJobsAsync());

    [HttpPost("jobs/{group}/{name}/trigger")]
    [Operation("Trigger", "Trigger job")]
    public async Task<ActionResult> TriggerJob(string group, string name)
    {
        await _quartzAppService.TriggerJobAsync(group, name);
        return Ok();
    }

    [HttpPost("jobs/{group}/{name}/pause")]
    [Operation("Pause", "Pause job")]
    public async Task<ActionResult> PauseJob(string group, string name)
    {
        await _quartzAppService.PauseJobAsync(group, name);
        return Ok();
    }

    [HttpPost("jobs/{group}/{name}/resume")]
    [Operation("Resume", "Resume job")]
    public async Task<ActionResult> ResumeJob(string group, string name)
    {
        await _quartzAppService.ResumeJobAsync(group, name);
        return Ok();
    }

    [HttpPost("jobs/{group}/{name}/triggers/{triggerGroup}/{triggerName}/pause")]
    [Operation("PauseTrigger", "Pause trigger")]
    public async Task<ActionResult> PauseTrigger(string group, string name, string triggerGroup,
        string triggerName)
    {
        await _quartzAppService.PauseTriggerAsync(group, name, triggerGroup, triggerName);
        return NoContent();
    }

    [HttpPost("jobs/{group}/{name}/triggers/{triggerGroup}/{triggerName}/resume")]
    [Operation("ResumeTrigger", "Resume trigger")]
    public async Task<ActionResult> ResumeTrigger(string group, string name, string triggerGroup,
        string triggerName)
    {
        await _quartzAppService.ResumeTriggerAsync(group, name, triggerGroup, triggerName);
        return NoContent();
    }

    [HttpPost("scheduler/pause")]
    [Operation("PauseAll", "Pause all jobs")]
    public async Task<ActionResult> PauseAll()
    {
        await _quartzAppService.PauseAllAsync();
        return Ok();
    }

    [HttpPost("scheduler/resume")]
    [Operation("ResumeAll", "Resume all jobs")]
    public async Task<ActionResult> ResumeAll()
    {
        await _quartzAppService.ResumeAllAsync();
        return Ok();
    }

    [HttpGet("scheduler/status")]
    [Operation("GetStatus", "Get scheduler status")]
    public async Task<ActionResult<SchedulerStatusDto>> GetStatus()
        => Ok(await _quartzAppService.GetStatusAsync());

    [HttpPut("jobs/{group}/{name}/triggers/{triggerGroup}/{triggerName}/cron")]
    [Operation("UpdateCron", "Update cron expression")]
    public async Task<ActionResult> UpdateCron(
        string group, string name,
        string triggerGroup, string triggerName,
        [FromBody] UpdateCronRequest request)
    {
        await _quartzAppService.UpdateCronAsync(group, name, triggerGroup, triggerName, request.CronExpression);
        return Ok();
    }

    [HttpDelete("jobs/{group}/{name}")]
    [Operation("Delete", "Delete job")]
    public async Task<ActionResult> DeleteJob(string group, string name)
    {
        await _quartzAppService.DeleteJobAsync(group, name);
        return Ok();
    }

    [HttpDelete("jobs/{group}/{name}/triggers/{triggerGroup}/{triggerName}")]
    [Operation("DeleteTrigger", "Delete trigger")]
    public async Task<ActionResult> DeleteTrigger(string group, string name, string triggerGroup,
        string triggerName)
    {
        await _quartzAppService.DeleteTriggerAsync(group, name, triggerGroup, triggerName);
        return Ok();
    }
}
