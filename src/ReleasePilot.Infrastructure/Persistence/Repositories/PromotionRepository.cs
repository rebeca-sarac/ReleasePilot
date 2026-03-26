using Microsoft.EntityFrameworkCore;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.Promotions;
using ReleasePilot.Domain.Promotions.ValueObjects;
using ApplicationId = ReleasePilot.Domain.Promotions.ValueObjects.ApplicationId;

namespace ReleasePilot.Infrastructure.Persistence.Repositories;

public class PromotionRepository : IPromotionRepository
{
    private readonly AppDbContext _context;

    public PromotionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Promotion?> GetByIdAsync(Guid promotionId, CancellationToken cancellationToken = default)
    {
        return await _context.Promotions.FirstOrDefaultAsync(p => p.Id == promotionId, cancellationToken);
    }

    public async Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        await _context.Promotions.AddAsync(promotion, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsInProgressAsync(ApplicationId applicationId, EnvironmentName targetEnvironment, CancellationToken cancellationToken = default)
    {
        return await _context.Promotions.AnyAsync(p => p.ApplicationId.Value == applicationId.Value
                                                    && p.TargetEnvironment.Value == targetEnvironment.Value
                                                    && p.State == PromotionState.InProgress, cancellationToken);
    }

    public async Task<bool> HasCompletedPromotionAsync(ApplicationId applicationId, EnvironmentName environment, CancellationToken cancellationToken = default)
    {
        return await _context.Promotions.AnyAsync(p => p.ApplicationId.Value == applicationId.Value
                                                    && p.TargetEnvironment.Value == environment.Value
                                                    && p.State == PromotionState.Completed, cancellationToken);
    }
}
