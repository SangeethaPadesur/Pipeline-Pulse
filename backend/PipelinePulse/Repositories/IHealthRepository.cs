using PipelinePulse.Models;

namespace PipelinePulse.Repositories;

public interface IHealthRepository
{
    HealthStatus GetHealth();
}