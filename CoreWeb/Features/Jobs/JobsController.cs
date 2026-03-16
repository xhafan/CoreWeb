using CoreDdd.Queries;
using CoreUtils;
using CoreUtils.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MrWatchdog.Core.Features.Jobs.Queries;
using MrWatchdog.Core.Infrastructure.Repositories;
using MrWatchdog.Core.Messages;
using MrWatchdog.Web.Infrastructure.Authorizations;
using Newtonsoft.Json;
using Rebus.Bus;
using Rebus.Messages;

namespace MrWatchdog.Web.Features.Jobs;

[ApiController]
[Route("api/[controller]")]
public class JobsController(
    IQueryExecutor queryExecutor,
    IJobRepository jobRepository
) : ControllerBase
{
    [HttpGet("{jobGuid}")]
    public async Task<IActionResult> GetJob(Guid jobGuid)
    {
        var jobDtos = (
            await queryExecutor.ExecuteAsync<GetJobQuery, JobDto>(new GetJobQuery(jobGuid))
        ).ToList();

        return jobDtos.IsEmpty()
            ? NotFound()
            : Ok(jobDtos.Single());
    }
    
    [HttpGet("[action]")]
    public async Task<IActionResult> GetRelatedDomainEventJob(Guid commandJobGuid, string type)
    {
        var jobDtos = (
            await queryExecutor.ExecuteAsync<GetRelatedDomainEventJobQuery, JobDto>(new GetRelatedDomainEventJobQuery(commandJobGuid, type))
        ).ToList();

        return jobDtos.IsEmpty()
            ? NotFound()
            : Ok(jobDtos.Single());
    }

    [Authorize(Policy = Policies.SuperAdmin)]
    [HttpPost("[action]")]
    public async Task HandleFailedJobAgain(
        Guid jobGuid, 
        [FromServices]IBus rebusBus, 
        bool ignoreCompletedJob = false
    )
    {
        var job = await jobRepository.LoadByGuidAsync(jobGuid);
        Guard.Hope(ignoreCompletedJob || job.CompletedOn == null, $"Job {jobGuid} has been already completed.");
        
        var messageType = _GetMessageTypes(job.Type);
        Guard.Hope(messageType != null, $"Could not find type {job.Type}.");

        var message = JsonConvert.DeserializeObject(job.InputData, type: messageType, settings: null);
        Guard.Hope(message != null, "Could not deserialize the message.");

        await rebusBus.Send(message, new Dictionary<string, string> {{Headers.MessageId, jobGuid.ToString()}});
    }

    private Type _GetMessageTypes(string messageTypeName)
    {
        var messageType = typeof(BaseMessage).Assembly.GetTypes()
            .SingleOrDefault(x => typeof(BaseMessage).IsAssignableFrom(x)
                                  && !x.IsAbstract
                                  && x.IsPublic
                                  && x.Name == messageTypeName);

        Guard.Hope(messageType != null, $"There is not a message of type {messageTypeName}");
        return messageType;
    }
}