using Microsoft.EntityFrameworkCore;
using PipelinePulse.Models;

namespace PipelinePulse.Data;

public class PipelineDbContext(DbContextOptions<PipelineDbContext> options) : DbContext(options)
{
    public DbSet<Pipeline> Pipelines => Set<Pipeline>();
    public DbSet<Run> Runs => Set<Run>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var pipeline = modelBuilder.Entity<Pipeline>();

        pipeline.HasKey(item => item.Id);
        pipeline.Property(item => item.Id).IsRequired();
        pipeline.Property(item => item.Name).IsRequired();
        pipeline.Property(item => item.Product)
            .HasConversion(product => ToDatabaseValue(product), product => FromPipelineProduct(product))
            .IsRequired();
        pipeline.Property(item => item.CreatedAt)
            .IsRequired();
        pipeline.HasMany(item => item.Runs)
            .WithOne(item => item.Pipeline)
            .HasForeignKey(item => item.PipelineId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        var run = modelBuilder.Entity<Run>();

        run.HasKey(item => item.Id);
        run.Property(item => item.Id).IsRequired();
        run.Property(item => item.PipelineId).IsRequired();
        run.Property(item => item.Status)
            .HasConversion(item => ToDatabaseValue(item), item => FromRunStatus(item))
            .IsRequired();
        run.Property(item => item.StartedAt)
            .IsRequired();
        run.Property(item => item.TriggeredBy)
            .IsRequired();
        run.Property(item => item.CommitSha)
            .IsRequired();
        run.HasMany(item => item.Notes)
            .WithOne(item => item.Run)
            .HasForeignKey(item => item.RunId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        var note = modelBuilder.Entity<Note>();

        note.HasKey(item => item.Id);
        note.Property(item => item.Id).IsRequired();
        note.Property(item => item.RunId).IsRequired();
        note.Property(item => item.Author).IsRequired();
        note.Property(item => item.Body).IsRequired();
        note.Property(item => item.CreatedAt).IsRequired();

        var idempotencyRecord = modelBuilder.Entity<IdempotencyRecord>();

        idempotencyRecord.HasKey(item => item.Key);
        idempotencyRecord.Property(item => item.Key).HasMaxLength(200).IsRequired();
        idempotencyRecord.Property(item => item.RequestHash).HasMaxLength(64).IsRequired();
        idempotencyRecord.Property(item => item.ResponseBody).IsRequired();
        idempotencyRecord.Property(item => item.ResponseLocation).IsRequired();
    }

    private static string ToDatabaseValue(PipelineProduct product) => product switch
    {
        PipelineProduct.FlashArray => "flasharray",
        PipelineProduct.FlashBlade => "flashblade",
        PipelineProduct.Hyperscale => "hyperscale",
        _ => throw new ArgumentOutOfRangeException(nameof(product), product, "Unsupported pipeline product.")
    };

    private static PipelineProduct FromPipelineProduct(string product) => product switch
    {
        "flasharray" => PipelineProduct.FlashArray,
        "flashblade" => PipelineProduct.FlashBlade,
        "hyperscale" => PipelineProduct.Hyperscale,
        _ => throw new InvalidOperationException($"Unsupported pipeline product '{product}' in the database.")
    };

    private static string ToDatabaseValue(RunStatus status) => status switch
    {
        RunStatus.Queued => "queued",
        RunStatus.Running => "running",
        RunStatus.Passed => "passed",
        RunStatus.Failed => "failed",
        RunStatus.Flaky => "flaky",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported run status.")
    };

    private static RunStatus FromRunStatus(string status) => status switch
    {
        "queued" => RunStatus.Queued,
        "running" => RunStatus.Running,
        "passed" => RunStatus.Passed,
        "failed" => RunStatus.Failed,
        "flaky" => RunStatus.Flaky,
        _ => throw new InvalidOperationException($"Unsupported run status '{status}' in the database.")
    };
}
