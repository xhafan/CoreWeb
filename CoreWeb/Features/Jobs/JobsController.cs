using CoreBackend.Features.Jobs.Queries;
using CoreBackend.Infrastructure.Repositories;
using CoreDdd.Queries;
using CoreUtils;
using CoreUtils.Extensions;
using CoreWeb.Infrastructure.Authorizations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Rebus.Bus;
using Rebus.Messages;

namespace CoreWeb.Features.Jobs;

[ApiController]
[Route("api/[controller]")]
public class JobsController(
    IQueryExecutor queryExecutor,
    IJobRepository jobRepository,
    IMessageTypeGetter messageTypeGetter
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

    [Authorize(Policy = CoreWebPolicies.SuperAdmin)]
    [HttpPost("[action]")]
    public async Task HandleFailedJobAgain(
        Guid jobGuid, 
        [FromServices]IBus rebusBus, 
        bool ignoreCompletedJob = false
    )
    {
        var job = await jobRepository.LoadByGuidAsync(jobGuid);
        Guard.Hope(ignoreCompletedJob || job.CompletedOn == null, $"Job {jobGuid} has been already completed.");
        
        var messageType = messageTypeGetter.GetMessageType(job.Type);
        Guard.Hope(messageType != null, $"Could not find type {job.Type}.");

        var message = JsonConvert.DeserializeObject(job.InputData, type: messageType, settings: null);
        Guard.Hope(message != null, "Could not deserialize the message.");

        await rebusBus.Send(message, new Dictionary<string, string> {{Headers.MessageId, jobGuid.ToString()}});
    }
}