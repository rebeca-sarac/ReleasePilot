using ErrorOr;
using FluentAssertions;
using NSubstitute;
using ReleasePilot.Application.Common;
using ReleasePilot.Application.Ports;
using ReleasePilot.Application.Queries.GetPromotionById;

namespace ReleasePilot.Application.Tests.Queries;

public class GetPromotionByIdQueryHandlerTests
{
    private readonly IPromotionReadRepository _readRepository;
    private readonly IIssueTrackerPort _issueTracker;

    private readonly GetPromotionByIdQueryHandler _handler;

    public GetPromotionByIdQueryHandlerTests()
    {
        _readRepository = Substitute.For<IPromotionReadRepository>();
        _issueTracker = Substitute.For<IIssueTrackerPort>();

        _handler = new GetPromotionByIdQueryHandler(_readRepository, _issueTracker);
    }

    [Fact]
    public async Task Should_ReturnNotFound_When_PromotionDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        _readRepository.GetDetailByIdAsync(id, Arg.Any<CancellationToken>()).Returns((PromotionDetailResponse?)null);

        // Act
        var result = await _handler.Handle(new GetPromotionByIdQuery { PromotionId = id }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Promotion.NotFound");
    }

    [Fact]
    public async Task Should_ReturnDetailWithoutWorkItems_When_NoIssueReferences()
    {
        // Arrange
        var id = Guid.NewGuid();
        var promotion = new PromotionDetailResponse { Id = id, IssueReferences = [] };

        _readRepository.GetDetailByIdAsync(id, Arg.Any<CancellationToken>()).Returns(promotion);

        // Act
        var result = await _handler.Handle(new GetPromotionByIdQuery { PromotionId = id }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.WorkItems.Should().BeEmpty();
        await _issueTracker.DidNotReceive().GetWorkItemsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnWorkItems_When_IssueReferencesExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var promotion = new PromotionDetailResponse
        {
            Id = id,
            IssueReferences = ["PROJ-1", "PROJ-2"]
        };
        var workItems = (IReadOnlyList<WorkItemResponse>)
        [
            new WorkItemResponse { Id = "PROJ-1", Title = "Fix login bug", Status = "Done" },
            new WorkItemResponse { Id = "PROJ-2", Title = "Update docs", Status = "In Progress" }
        ];

        _readRepository.GetDetailByIdAsync(id, Arg.Any<CancellationToken>()).Returns(promotion);
        _issueTracker.GetWorkItemsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>()).Returns(workItems);

        // Act
        var result = await _handler.Handle(new GetPromotionByIdQuery { PromotionId = id }, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.WorkItems.Should().HaveCount(2);
        result.Value.WorkItems[0].Id.Should().Be("PROJ-1");
        result.Value.WorkItems[1].Id.Should().Be("PROJ-2");
    }

    [Fact]
    public async Task Should_CallGetWorkItemsAsync_With_PromotionIssueReferences()
    {
        // Arrange
        var id = Guid.NewGuid();
        var issueRefs = new[] { "PROJ-42" };
        var promotion = new PromotionDetailResponse { Id = id, IssueReferences = issueRefs };

        _readRepository.GetDetailByIdAsync(id, Arg.Any<CancellationToken>()).Returns(promotion);
        _issueTracker.GetWorkItemsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
                     .Returns([]);

        // Act
        await _handler.Handle(new GetPromotionByIdQuery { PromotionId = id }, CancellationToken.None);

        // Assert
        await _issueTracker.Received(1).GetWorkItemsAsync(Arg.Is<IEnumerable<string>>(refs => refs.SequenceEqual(issueRefs)),
                                                          Arg.Any<CancellationToken>());
    }
}
