using ErrorOr;
using FluentAssertions;
using NSubstitute;
using ReleasePilot.Application.Commands.RollbackPromotion;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.Common;
using ReleasePilot.Domain.Promotions;
using ReleasePilot.Domain.Promotions.Events;

namespace ReleasePilot.Application.Tests.Commands;

public class RollbackPromotionCommandHandlerTests
{
    private readonly IPromotionRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly INotificationPort _notificationPort;

    private readonly RollbackPromotionCommandHandler _handler;

    public RollbackPromotionCommandHandlerTests()
    {
        _repository = Substitute.For<IPromotionRepository>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        _notificationPort = Substitute.For<INotificationPort>();

        _handler = new RollbackPromotionCommandHandler(_repository, _eventPublisher, _notificationPort);
    }

    [Fact]
    public async Task Should_TransitionToRolledBack_When_ReasonIsProvided()
    {
        // Arrange
        var promotion = PromotionFactory.InProgress();
        _repository.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);

        var command = new RollbackPromotionCommand { PromotionId = promotion.Id, Reason = "Health check failed.", RolledBackBy = "alice" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        promotion.State.Should().Be(PromotionState.RolledBack);
        promotion.RollbackReason.Should().Be("Health check failed.");
        await _repository.Received(1).UpdateAsync(promotion, Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(Arg.Is<IDomainEvent>(e => e is PromotionRolledBack),
                                                       Arg.Any<CancellationToken>());
        await _notificationPort.Received(1).SendPromotionNotificationAsync(promotion.Id,
                                                                           promotion.ApplicationId.Value,
                                                                           PromotionState.RolledBack,
                                                                           Arg.Any<IEnumerable<string>>(),
                                                                           Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnValidationError_When_ReasonIsEmpty()
    {
        // Arrange
        var promotion = PromotionFactory.InProgress();
        _repository.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);

        var command = new RollbackPromotionCommand { PromotionId = promotion.Id, Reason = "", RolledBackBy = "alice" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
        await _notificationPort.DidNotReceive().SendPromotionNotificationAsync(Arg.Any<Guid>(),
                                                                               Arg.Any<Guid>(),
                                                                               Arg.Any<PromotionState>(),
                                                                               Arg.Any<IEnumerable<string>>(),
                                                                               Arg.Any<CancellationToken>());
    }
}
