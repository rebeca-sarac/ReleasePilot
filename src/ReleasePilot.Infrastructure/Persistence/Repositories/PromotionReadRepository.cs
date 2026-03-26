using Microsoft.EntityFrameworkCore;
using ReleasePilot.Application.Common;
using ReleasePilot.Application.Queries.GetEnvironmentStatus;
using ReleasePilot.Application.Queries.GetPromotionById;
using ReleasePilot.Application.Queries.ListPromotionsByApplication;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.Promotions.ValueObjects;

namespace ReleasePilot.Infrastructure.Persistence.Repositories;

public class PromotionReadRepository : IPromotionReadRepository
{
    private readonly AppDbContext _context;

    public PromotionReadRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PromotionDetailResponse?> GetDetailByIdAsync(Guid promotionId, CancellationToken cancellationToken = default)
    {
        var promotion = await _context.Promotions.AsNoTracking()
                                                 .FirstOrDefaultAsync(p => p.Id == promotionId, cancellationToken);

        if (promotion is null)
        {
            return null;
        }

        var history = await _context.PromotionStateHistories.AsNoTracking()
                                                            .Where(h => h.PromotionId == promotionId)
                                                            .OrderBy(h => h.OccurredOn)
                                                            .ToListAsync(cancellationToken);

        return new PromotionDetailResponse
        {
            Id                 = promotion.Id,
            ApplicationId      = promotion.ApplicationId.Value,
            Version            = promotion.Version.Value,
            TargetEnvironment  = promotion.TargetEnvironment.Value,
            State              = promotion.State,
            RequestedBy        = promotion.RequestedBy,
            RequestedAt        = promotion.RequestedAt,
            ApprovedBy         = promotion.ApprovedBy,
            ApprovedAt         = promotion.ApprovedAt,
            StartedAt          = promotion.StartedAt,
            CompletedAt        = promotion.CompletedAt,
            RollbackReason     = promotion.RollbackReason,
            CancellationReason = promotion.CancellationReason,
            IssueReferences    = promotion.IssueReferences,
            StateHistory       = history.Select(h => new PromotionStateHistoryEntry
                                        {
                                            State      = h.State,
                                            OccurredOn = h.OccurredOn,
                                            Actor      = h.Actor
                                        })
                                        .ToList()
        };
    }

    public async Task<IReadOnlyList<EnvironmentStatusResponse>> GetEnvironmentStatusAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var promotions = await _context.Promotions.AsNoTracking()
                                                  .Where(p => p.ApplicationId.Value == applicationId)
                                                  .OrderByDescending(p => p.RequestedAt)
                                                  .ToListAsync(cancellationToken);

        return EnvironmentName.KnownEnvironments.Select(env =>
                                                {
                                                    var latest = promotions.FirstOrDefault(p => p.TargetEnvironment.Value == env);

                                                    return new EnvironmentStatusResponse
                                                    {
                                                        Environment       = env,
                                                        ActiveState       = latest?.State,
                                                        ActivePromotionId = latest?.Id,
                                                        ActiveVersion     = latest?.Version.Value,
                                                        LastActivityAt    = latest?.CompletedAt
                                                                         ?? latest?.StartedAt
                                                                         ?? latest?.ApprovedAt
                                                                         ?? latest?.RequestedAt
                                                    };
                                                })
                                                .ToList();
    }

    public async Task<PagedResponse<PromotionSummaryResponse>> ListByApplicationAsync(Guid applicationId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Promotions.AsNoTracking()
                                       .Where(p => p.ApplicationId.Value == applicationId)
                                       .OrderByDescending(p => p.RequestedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query.Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync(cancellationToken);

        var dtos = items.Select(p => new PromotionSummaryResponse
                                    {
                                        Id = p.Id,
                                        ApplicationId = p.ApplicationId.Value,
                                        Version = p.Version.Value,
                                        TargetEnvironment = p.TargetEnvironment.Value,
                                        State = p.State,
                                        RequestedBy = p.RequestedBy,
                                        RequestedAt = p.RequestedAt
                                    })
                                    .ToList();

        return new PagedResponse<PromotionSummaryResponse>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
