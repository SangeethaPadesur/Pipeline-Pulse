using PipelinePulse.Models;
using PipelinePulse.Repositories;

namespace PipelinePulse.Services;

public class HealthService : IHealthService
{
    private readonly IHealthRepository _healthRepository;

    public HealthService(IHealthRepository healthRepository)
    {
        _healthRepository = healthRepository;
    }

    public HealthStatus GetHealth()
    {
        return _healthRepository.GetHealth();
    }
}