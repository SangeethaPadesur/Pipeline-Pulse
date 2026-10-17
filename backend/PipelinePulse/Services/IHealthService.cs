using PipelinePulse.Models;

namespace PipelinePulse.Services;

public interface IHealthService
{
    HealthStatus GetHealth();
}