using ErrorOr;
using FluentAssertions;
using NSubstitute;
using ReleasePilot.Application.Commands.CancelPromotion;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.Common;
using ReleasePilot.Domain.Promotions;
using ReleasePilot.Domain.Promotions.Events;

namespace ReleasePilot.Application.Tests.Commands;

public class CancelPromotionCommandHandlerTests
{
    private readonly IPromotionRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly INotificationPort _notificationPort;

    private readonly CancelPromotionCommandHandler _handler;

    public CancelPromotionCommandHandlerTests()
    {
        _repository = Substitute.For<IPromotionRepository>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        _notificationPort = Substitute.For<INotificationPort>();

        _handler = new CancelPromotionCommandHandler(_repository, _eventPublisher, _notificationPort);
    }

    [Fact]
    public async Task Should_TransitionToCancelled_When_PromotionIsRequested()
    {
        // Arrange
        var promotion = PromotionFactory.Requested();
        _repository.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);

        var command = new CancelPromotionCommand { PromotionId = promotion.Id, Reason = "No longer needed.", CancelledBy = "alice" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        promotion.State.Should().Be(PromotionState.Cancelled);
        promotion.CancellationReason.Should().Be("No longer needed.");
        await _repository.Received(1).UpdateAsync(promotion, Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(Arg.Is<IDomainEvent>(e => e is PromotionCancelled),
                                                       Arg.Any<CancellationToken>());
        await _notificationPort.Received(1).SendPromotionNotificationAsync(promotion.Id,
                                                                           promotion.ApplicationId.Value,
                                                                           PromotionState.Cancelled,
                                                                           Arg.Any<IEnumerable<string>>(),
                                                                           Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnConflictError_When_PromotionIsApproved()
    {
        // Arrange
        var promotion = PromotionFactory.Approved();
        _repository.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);

        var command = new CancelPromotionCommand { PromotionId = promotion.Id, Reason = "Changed mind.", CancelledBy = "alice" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
        await _notificationPort.DidNotReceive().SendPromotionNotificationAsync(Arg.Any<Guid>(),
                                                                               Arg.Any<Guid>(),
                                                                               Arg.Any<PromotionState>(),
                                                                               Arg.Any<IEnumerable<string>>(),
                                                                               Arg.Any<CancellationToken>());
    }
}
