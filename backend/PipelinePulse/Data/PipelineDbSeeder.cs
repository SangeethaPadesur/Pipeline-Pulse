using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PipelinePulse.Models;

namespace PipelinePulse.Data;

public static class PipelineDbSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static async Task SeedAsync(PipelineDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var seedFilePath = Path.Combine(AppContext.BaseDirectory, "seed", "seed.json");
        await using var seedFile = File.OpenRead(seedFilePath);
        var seedData = await JsonSerializer.DeserializeAsync<SeedData>(seedFile, JsonOptions, cancellationToken)
            ?? throw new InvalidDataException($"Seed data file '{seedFilePath}' is empty or invalid.");

        var existingPipelineIds = await dbContext.Pipelines
            .Select(pipeline => pipeline.Id)
            .ToHashSetAsync(cancellationToken);

        dbContext.Pipelines.AddRange(
            seedData.Pipelines
                .Select(pipeline => new Pipeline
                {
                    Id = pipeline.Id,
                    Name = pipeline.Name,
                    Product = ParseProduct(pipeline.Product)
                })
                .Where(pipeline => !existingPipelineIds.Contains(pipeline.Id)));

        await dbContext.SaveChangesAsync(cancellationToken);

        var existingRunIds = await dbContext.Runs
            .Select(run => run.Id)
            .ToHashSetAsync(cancellationToken);

        dbContext.Runs.AddRange(
            seedData.Runs
                .Select(run => new Run
                {
                    Id = run.Id,
                    PipelineId = run.PipelineId,
                    Status = ParseStatus(run.Status),
                    StartedAt = run.StartedAt.UtcDateTime,
                    FinishedAt = run.FinishedAt?.UtcDateTime,
                    DurationMs = run.DurationMs,
                    TriggeredBy = run.TriggeredBy,
                    CommitSha = run.CommitSha
                })
                .Where(run => !existingRunIds.Contains(run.Id)));

        await dbContext.SaveChangesAsync(cancellationToken);

        var existingNoteIds = await dbContext.Notes
            .Select(note => note.Id)
            .ToHashSetAsync(cancellationToken);

        dbContext.Notes.AddRange(
            seedData.Notes
                .Select(note => new Note
                {
                    Id = note.Id,
                    RunId = note.RunId,
                    Author = note.Author,
                    Body = note.Body
                })
                .Where(note => !existingNoteIds.Contains(note.Id)));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static PipelineProduct ParseProduct(string product) => product switch
    {
        "flasharray" => PipelineProduct.FlashArray,
        "flashblade" => PipelineProduct.FlashBlade,
        "hyperscale" => PipelineProduct.Hyperscale,
        _ => throw new InvalidDataException($"Unsupported pipeline product '{product}' in seed data.")
    };

    private static RunStatus ParseStatus(string status) => status switch
    {
        "queued" => RunStatus.Queued,
        "running" => RunStatus.Running,
        "passed" => RunStatus.Passed,
        "failed" => RunStatus.Failed,
        "flaky" => RunStatus.Flaky,
        _ => throw new InvalidDataException($"Unsupported run status '{status}' in seed data.")
    };

    private sealed class SeedData
    {
        public required List<PipelineSeed> Pipelines { get; init; }
        public required List<RunSeed> Runs { get; init; }
        public required List<NoteSeed> Notes { get; init; }
    }

    private sealed class PipelineSeed
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public required string Product { get; init; }
    }

    private sealed class RunSeed
    {
        public required string Id { get; init; }
        public required string PipelineId { get; init; }
        public required string Status { get; init; }
        public DateTimeOffset StartedAt { get; init; }
        public DateTimeOffset? FinishedAt { get; init; }
        public long? DurationMs { get; init; }
        public required string TriggeredBy { get; init; }
        public required string CommitSha { get; init; }
    }

    private sealed class NoteSeed
    {
        public required string Id { get; init; }
        public required string RunId { get; init; }
        public required string Author { get; init; }
        public required string Body { get; init; }
    }
}
