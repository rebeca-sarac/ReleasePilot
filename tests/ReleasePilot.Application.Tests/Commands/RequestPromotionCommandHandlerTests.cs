using ErrorOr;
using FluentAssertions;
using NSubstitute;
using ReleasePilot.Application.Commands.RequestPromotion;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.Common;
using ReleasePilot.Domain.Promotions;
using ReleasePilot.Domain.Promotions.Events;
using ReleasePilot.Domain.Promotions.ValueObjects;
using ApplicationId = ReleasePilot.Domain.Promotions.ValueObjects.ApplicationId;

namespace ReleasePilot.Application.Tests.Commands;

public class RequestPromotionCommandHandlerTests
{
    private readonly IPromotionRepository _repository;
    private readonly IEventPublisher _eventPublisher;

    private readonly RequestPromotionCommandHandler _handler;

    public RequestPromotionCommandHandlerTests()
    {
        _repository = Substitute.For<IPromotionRepository>();
        _eventPublisher = Substitute.For<IEventPublisher>();

        _handler = new RequestPromotionCommandHandler(_repository, _eventPublisher);
    }

    [Fact]
    public async Task Should_ReturnNewGuid_When_CommandIsValid()
    {
        // Arrange
        SetupDevCompleted();

        var command = new RequestPromotionCommand
        {
            ApplicationId = Guid.NewGuid(),
            Version = "2.0.0",
            SourceEnvironment = "dev",
            TargetEnvironment = "staging",
            RequestedBy = "alice"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Should_CallAddAsync_When_CommandIsValid()
    {
        // Arrange
        SetupDevCompleted();
        var command = new RequestPromotionCommand
        {
            ApplicationId =Guid.NewGuid(),
            Version ="2.0.0",
            SourceEnvironment = "dev",
            TargetEnvironment = "staging",
            RequestedBy ="alice"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).AddAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_PublishPromotionRequestedEvent_When_CommandIsValid()
    {
        // Arrange
        SetupDevCompleted();
        var command = new RequestPromotionCommand
        {
            ApplicationId =Guid.NewGuid(),
            Version ="2.0.0",
            SourceEnvironment = "dev",
            TargetEnvironment = "staging",
            RequestedBy ="alice"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        await _eventPublisher.Received(1).PublishAsync(Arg.Is<IDomainEvent>(e => e is PromotionRequested),
                                                       Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnError_When_EnvironmentNameIsInvalid()
    {
        // Arrange
        var command = new RequestPromotionCommand
        {
            ApplicationId =Guid.NewGuid(),
            Version = "1.0.0",
            TargetEnvironment = "unknown-env",
            RequestedBy = "alice"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        await _repository.DidNotReceive().AddAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Succeed_When_PredecessorEnvironmentIsCompleted()
    {
        // Arrange
        SetupDevCompleted();
        var command = new RequestPromotionCommand
        {
            ApplicationId =Guid.NewGuid(),
            Version = "1.0.0",
            SourceEnvironment = "dev",
            TargetEnvironment = "staging",
            RequestedBy = "alice"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        await _repository.Received(1).AddAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnEnvironmentNotReadyError_When_PredecessorHasNoCompletedPromotion()
    {
        // Arrange
        var command = new RequestPromotionCommand
        {
            ApplicationId =Guid.NewGuid(),
            Version = "1.0.0",
            SourceEnvironment = "dev",
            TargetEnvironment = "staging",
            RequestedBy = "alice"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Promotion.EnvironmentNotReady");
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Promotion>(), Arg.Any<CancellationToken>());
    }

    private void SetupDevCompleted() => _repository.HasCompletedPromotionAsync(Arg.Any<ApplicationId>(),
                                                                               Arg.Is<EnvironmentName>(e => e.Value == "dev"),
                                                                               Arg.Any<CancellationToken>()).Returns(true);
}
