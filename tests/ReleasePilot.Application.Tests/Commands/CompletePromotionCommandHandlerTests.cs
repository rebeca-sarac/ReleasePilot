using ErrorOr;
using FluentAssertions;
using NSubstitute;
using ReleasePilot.Application.Commands.CompletePromotion;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.Common;
using ReleasePilot.Domain.Promotions;
using ReleasePilot.Domain.Promotions.Events;

namespace ReleasePilot.Application.Tests.Commands;

public class CompletePromotionCommandHandlerTests
{
    private readonly IPromotionRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly INotificationPort _notificationPort;

    private readonly CompletePromotionCommandHandler _handler;

    public CompletePromotionCommandHandlerTests()
    {
        _repository = Substitute.For<IPromotionRepository>();
        _eventPublisher = Substitute.For<IEventPublisher>();
        _notificationPort = Substitute.For<INotificationPort>();

        _handler = new CompletePromotionCommandHandler(_repository, _eventPublisher, _notificationPort);
    }

    [Fact]
    public async Task Should_TransitionToCompleted_When_PromotionIsInProgress()
    {
        // Arrange
        var promotion = PromotionFactory.InProgress();
        _repository.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);

        var command = new CompletePromotionCommand { PromotionId = promotion.Id, CompletedBy = "alice" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        promotion.State.Should().Be(PromotionState.Completed);
        await _repository.Received(1).UpdateAsync(promotion, Arg.Any<CancellationToken>());
        await _eventPublisher.Received(1).PublishAsync(Arg.Is<IDomainEvent>(e => e is PromotionCompleted),
                                                       Arg.Any<CancellationToken>());
        await _notificationPort.Received(1).SendPromotionNotificationAsync(promotion.Id,
                                                                           promotion.ApplicationId.Value,
                                                                           PromotionState.Completed,
                                                                           Arg.Any<IEnumerable<string>>(),
                                                                           Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_PromotionDoesNotExist()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Promotion?)null);
        
        var command = new CompletePromotionCommand { PromotionId = Guid.NewGuid(), CompletedBy = "alice" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
        await _notificationPort.DidNotReceive().SendPromotionNotificationAsync(Arg.Any<Guid>(),
                                                                               Arg.Any<Guid>(),
                                                                               Arg.Any<PromotionState>(),
                                                                               Arg.Any<IEnumerable<string>>(),
                                                                               Arg.Any<CancellationToken>());
    }
}
