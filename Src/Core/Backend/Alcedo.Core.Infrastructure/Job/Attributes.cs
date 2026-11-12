namespace Alcedo.Core.Infrastructure.Job;

[AttributeUsage(AttributeTargets.Class)]
public class QuartzJobAttribute : Attribute
{
    public string? CronExpression { get; set; }
    public int? IntervalInSeconds { get; set; } = 0;
    public string? JobName { get; set; }

    public string? JobGroup { get; set; }

    public QuartzJobAttribute(string cronExpression, string jobName, string? jobGroup = null)
    {
        CronExpression = cronExpression;

        JobName = jobName;

        JobGroup = jobGroup;
    }

    public QuartzJobAttribute(int intervalInSeconds, string jobName, string? jobGroup = null)
    {
        IntervalInSeconds = intervalInSeconds;

        JobName = jobName;

        JobGroup = jobGroup;
    }
}