using ErrorOr;
using FluentAssertions;
using NSubstitute;
using ReleasePilot.Application.Commands.ApprovePromotion;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.Common;
using ReleasePilot.Domain.Promotions;
using ReleasePilot.Domain.Promotions.Events;

namespace ReleasePilot.Application.Tests.Commands;

public class ApprovePromotionCommandHandlerTests
{
    private readonly IPromotionRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    private readonly ApprovePromotionCommandHandler _handler;

    public ApprovePromotionCommandHandlerTests()
    {
        _repository = Substitute.For<IPromotionRepository>();
        _eventPublisher = Substitute.For<IEventPublisher>();

        _handler = new ApprovePromotionCommandHandler(_repository, _eventPublisher);
    }

    [Fact]
    public async Task Should_ApprovePromotion_When_CommandIsValid()
    {
        // Arrange
        var promotion = PromotionFactory.Requested();
        _repository.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);
        _repository.ExistsInProgressAsync(PromotionFactory.DefaultAppId,
                                          PromotionFactory.DefaultEnvironment,
                                          Arg.Any<CancellationToken>()).Returns(false);

        var command = new ApprovePromotionCommand
        {
            PromotionId =promotion.Id,
            ApprovedBy ="bob",
            ApproverRole = Promotion.ApproverRole
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        promotion.State.Should().Be(PromotionState.Approved);
        await _repository.Received(1).UpdateAsync(promotion, Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(Arg.Is<IDomainEvent>(e => e is PromotionApproved),
                                                       Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_PromotionDoesNotExist()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                   .Returns((Promotion?)null);

        var command = new ApprovePromotionCommand
        {
            PromotionId =Guid.NewGuid(),
            ApprovedBy ="bob",
            ApproverRole = Promotion.ApproverRole
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Should_ReturnConflictError_When_EnvironmentIsLocked()
    {
        // Arrange
        var promotion = PromotionFactory.Requested();
        _repository.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);
        _repository.ExistsInProgressAsync(PromotionFactory.DefaultAppId,
                                          PromotionFactory.DefaultEnvironment,
                                          Arg.Any<CancellationToken>()).Returns(true);

        var command = new ApprovePromotionCommand
        {
            PromotionId =promotion.Id,
            ApprovedBy ="bob",
            ApproverRole = Promotion.ApproverRole
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        result.FirstError.Code.Should().Be("Promotion.SlotOccupied");
    }

    [Fact]
    public async Task Should_ReturnUnauthorizedError_When_ApproverRoleIsInvalid()
    {
        // Arrange
        var promotion = PromotionFactory.Requested();
        _repository.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);
        _repository.ExistsInProgressAsync(PromotionFactory.DefaultAppId,
                                          PromotionFactory.DefaultEnvironment,
                                          Arg.Any<CancellationToken>()).Returns(false);

        var command = new ApprovePromotionCommand
        {
            PromotionId =promotion.Id,
            ApprovedBy ="charlie",
            ApproverRole = "Developer"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Unauthorized);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
    }
}
