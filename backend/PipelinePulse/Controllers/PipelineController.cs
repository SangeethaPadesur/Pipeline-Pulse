using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PipelinePulse.Dtos;
using PipelinePulse.Models;
using PipelinePulse.Services;

namespace PipelinePulse.Controllers;

[ApiController]
[Route("pipelines")]
[Route("api/v1/pipelines")]
public class PipelineController(IPipelineService pipelineService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<PipelineDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePipeline(
        [FromBody] CreatePipelineRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Name must not be blank.");
            return ValidationProblem(ModelState);
        }

        if (idempotencyKey is not null
            && (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 200))
        {
            ModelState.AddModelError("Idempotency-Key", "Idempotency-Key must contain 1 to 200 non-whitespace characters.");
            return ValidationProblem(ModelState);
        }

        if (int.TryParse(request.ProductType, out _)
            || !Enum.TryParse(request.ProductType, ignoreCase: true, out PipelineProduct product)
            || !Enum.IsDefined(product))
        {
            ModelState.AddModelError(nameof(request.ProductType), "ProductType must be one of: FlashArray, FlashBlade, Hyperscale.");
            return ValidationProblem(ModelState);
        }

        try
        {
            var (response, location) = await pipelineService.CreatePipelineAsync(
                request.Name,
                product,
                idempotencyKey,
                cancellationToken);
            return Created(location, response);
        }
        catch (IdempotencyKeyConflictException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Idempotency key conflict",
                Detail = exception.Message
            });
        }
    }

    [HttpGet]
    public async Task<ActionResult<PipelinePageDto>> GetPipelines(
        [FromQuery] string? name,
        [FromQuery] string? productType,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PipelineProduct? product = null;
        if (!string.IsNullOrWhiteSpace(productType))
        {
            if (int.TryParse(productType, out _)
                || !Enum.TryParse(productType, ignoreCase: true, out PipelineProduct parsedProduct)
                || !Enum.IsDefined(parsedProduct))
            {
                return BadRequest("productType must be one of: FlashArray, FlashBlade, Hyperscale.");
            }

            product = parsedProduct;
        }

        var result = await pipelineService.GetPipelinesAsync(
            name,
            product,
            page,
            pageSize,
            cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id}")]
    [ProducesResponseType<PipelineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PipelineDto>> GetPipeline(
        string id,
        CancellationToken cancellationToken)
    {
        var pipeline = await pipelineService.GetPipelineAsync(id, cancellationToken);
        return pipeline is null ? NotFound() : Ok(pipeline);
    }

    [HttpPatch("{id}")]
    [ProducesResponseType<PipelineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PipelineDto>> UpdatePipeline(
        string id,
        [FromBody] UpdatePipelineRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Name is null && request.ProductType is null)
        {
            ModelState.AddModelError(string.Empty, "At least one of Name or ProductType must be provided.");
            return ValidationProblem(ModelState);
        }

        if (request.Name is not null && string.IsNullOrWhiteSpace(request.Name))
        {
            ModelState.AddModelError(nameof(request.Name), "Name must not be blank.");
            return ValidationProblem(ModelState);
        }

        PipelineProduct? product = null;
        if (request.ProductType is not null)
        {
            if (int.TryParse(request.ProductType, out _)
                || !Enum.TryParse(request.ProductType, ignoreCase: true, out PipelineProduct parsedProduct)
                || !Enum.IsDefined(parsedProduct))
            {
                ModelState.AddModelError(nameof(request.ProductType), "ProductType must be one of: FlashArray, FlashBlade, Hyperscale.");
                return ValidationProblem(ModelState);
            }

            product = parsedProduct;
        }

        var pipeline = await pipelineService.UpdatePipelineAsync(
            id,
            request.Name,
            product,
            cancellationToken);
        return pipeline is null ? NotFound() : Ok(pipeline);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePipeline(
        string id,
        CancellationToken cancellationToken)
    {
        var deleted = await pipelineService.DeletePipelineAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("{id}/runs")]
    [ProducesResponseType<RunPageDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RunPageDto>> GetPipelineRuns(
        string id,
        [FromQuery] string? status,
        [FromQuery, Range(1, 100)] int limit = 20,
        [FromQuery] string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        RunStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (int.TryParse(status, out _)
                || !Enum.TryParse(status, ignoreCase: true, out RunStatus parsed)
                || !Enum.IsDefined(parsed))
            {
                ModelState.AddModelError(nameof(status), "Status must be one of: Queued, Running, Passed, Failed, Flaky.");
                return ValidationProblem(ModelState);
            }

            parsedStatus = parsed;
        }

        DateTime? cursorStartedAt = null;
        string? cursorRunId = null;
        if (!string.IsNullOrEmpty(cursor))
        {
            if (!RunCursorCodec.TryDecode(cursor, out var startedAt, out var runId))
            {
                ModelState.AddModelError(nameof(cursor), "Cursor is invalid.");
                return ValidationProblem(ModelState);
            }

            cursorStartedAt = startedAt;
            cursorRunId = runId;
        }

        var result = await pipelineService.GetPipelineRunsAsync(
            id,
            parsedStatus,
            limit,
            cursorStartedAt,
            cursorRunId,
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id}/runs")]
    [ProducesResponseType<RunDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RunDto>> CreatePipelineRun(
        string id,
        [FromBody] CreateRunRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.TriggeredBy))
        {
            ModelState.AddModelError(nameof(request.TriggeredBy), "TriggeredBy must not be blank.");
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.CommitSha))
        {
            ModelState.AddModelError(nameof(request.CommitSha), "CommitSha must not be blank.");
            return ValidationProblem(ModelState);
        }

        var run = await pipelineService.CreatePipelineRunAsync(
            id,
            request.TriggeredBy,
            request.CommitSha,
            cancellationToken);
        return run is null
            ? NotFound()
            : Created($"/pipelines/{id}/runs/{run.Id}", run);
    }

    [HttpGet("~/runs/{id}")]
    [HttpGet("~/api/v1/runs/{id}")]
    [ProducesResponseType<RunDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RunDetailsDto>> GetRun(
        string id,
        CancellationToken cancellationToken)
    {
        var run = await pipelineService.GetRunWithNotesAsync(id, cancellationToken);
        return run is null ? NotFound() : Ok(run);
    }

    [HttpPatch("~/runs/{id}")]
    [HttpPatch("~/api/v1/runs/{id}")]
    [ProducesResponseType<RunDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RunDto>> UpdateRunStatus(
        string id,
        [FromBody] UpdateRunStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status)
            || int.TryParse(request.Status, out _)
            || !Enum.TryParse(request.Status, ignoreCase: true, out RunStatus status)
            || !Enum.IsDefined(status))
        {
            ModelState.AddModelError(
                nameof(request.Status),
                "Status must be one of: Queued, Running, Passed, Failed, Flaky.");
            return ValidationProblem(ModelState);
        }

        var run = await pipelineService.UpdateRunStatusAsync(id, status, cancellationToken);
        return run is null ? NotFound() : Ok(run);
    }

    [HttpGet("~/runs/{id}/events")]
    [HttpGet("~/api/v1/runs/{id}/events")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StreamRunEvents(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var currentStatus = await pipelineService.GetRunStatusAsync(id, cancellationToken);
            if (currentStatus is null)
            {
                return NotFound();
            }

            Response.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";

            await WriteRunStatusEventAsync(currentStatus, cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                var status = await pipelineService.GetRunStatusAsync(id, cancellationToken);
                if (status is null)
                {
                    break;
                }

                if (status != currentStatus)
                {
                    await WriteRunStatusEventAsync(status, cancellationToken);
                    currentStatus = status;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException) when (cancellationToken.IsCancellationRequested)
        {
        }

        return new EmptyResult();
    }

    [HttpPost("~/runs/{id}/notes")]
    [HttpPost("~/api/v1/runs/{id}/notes")]
    [ProducesResponseType<NoteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteDto>> CreateRunNote(
        string id,
        [FromBody] CreateNoteRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Author))
        {
            ModelState.AddModelError(nameof(request.Author), "Author must not be blank.");
            return ValidationProblem(ModelState);
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            ModelState.AddModelError(nameof(request.Body), "Body must not be blank.");
            return ValidationProblem(ModelState);
        }

        var note = await pipelineService.CreateRunNoteAsync(
            id,
            request.Author,
            request.Body,
            cancellationToken);
        return note is null
            ? NotFound()
            : Created($"/runs/{id}#note-{note.Id}", note);
    }

    [HttpDelete("~/notes/{id}")]
    [HttpDelete("~/api/v1/notes/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteNote(
        string id,
        CancellationToken cancellationToken)
    {
        var deleted = await pipelineService.DeleteNoteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private async Task WriteRunStatusEventAsync(string status, CancellationToken cancellationToken)
    {
        var data = JsonSerializer.Serialize(new { Status = status });
        await Response.WriteAsync($"data: {data}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
