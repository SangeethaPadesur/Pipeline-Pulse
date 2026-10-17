using PipelinePulse.Data;
using PipelinePulse.Models;

namespace PipelinePulse.Repositories;

public class HealthRepository(PipelineDbContext dbContext) : IHealthRepository
{
    public HealthStatus GetHealth()
    {
        var isDatabaseHealthy = dbContext.Database.CanConnect();

        return new HealthStatus
        {
            Status = isDatabaseHealthy ? "ok" : "error",
            Db = isDatabaseHealthy ? "ok" : "error"
        };
    }
}
