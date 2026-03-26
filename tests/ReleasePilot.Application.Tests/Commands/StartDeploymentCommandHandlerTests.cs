using ErrorOr;
using FluentAssertions;
using NSubstitute;
using ReleasePilot.Application.Commands.StartDeployment;
using ReleasePilot.Application.Ports;
using ReleasePilot.Domain.Promotions;
using ReleasePilot.Domain.Promotions.ValueObjects;
using ApplicationId = ReleasePilot.Domain.Promotions.ValueObjects.ApplicationId;

namespace ReleasePilot.Application.Tests.Commands;

public class StartDeploymentCommandHandlerTests
{
    private readonly IPromotionRepository _repository;
    private readonly IDeploymentPort _deploymentPort;
    private readonly IEventPublisher _eventPublisher;

    private readonly StartDeploymentCommandHandler _handler;

    public StartDeploymentCommandHandlerTests()
    {
        _repository = Substitute.For<IPromotionRepository>();
        _deploymentPort = Substitute.For<IDeploymentPort>();
        _eventPublisher = Substitute.For<IEventPublisher>();

        _handler = new StartDeploymentCommandHandler(_repository, _deploymentPort, _eventPublisher);
    }

    [Fact]
    public async Task Should_TriggerDeploymentAndUpdate_When_PromotionIsApproved()
    {
        // Arrange
        var promotion = PromotionFactory.Approved();
        _repository.GetByIdAsync(promotion.Id, Arg.Any<CancellationToken>()).Returns(promotion);

        var command = new StartDeploymentCommand { PromotionId = promotion.Id, StartedBy = "alice" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        promotion.State.Should().Be(PromotionState.InProgress);
        await _deploymentPort.Received(1).TriggerDeploymentAsync(promotion.Id,
                                                                 Arg.Any<ApplicationId>(),
                                                                 Arg.Any<ApplicationVersion>(),
                                                                 Arg.Any<EnvironmentName>(),
                                                                 Arg.Any<CancellationToken>());
        await _repository.Received(1).UpdateAsync(promotion, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ReturnNotFoundError_When_PromotionDoesNotExist()
    {
        // Arrange
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Promotion?)null);

        var command = new StartDeploymentCommand { PromotionId = Guid.NewGuid(), StartedBy = "alice" };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        await _deploymentPort.DidNotReceive().TriggerDeploymentAsync(Arg.Any<Guid>(),
                                                                     Arg.Any<ApplicationId>(),
                                                                     Arg.Any<ApplicationVersion>(),
                                                                     Arg.Any<EnvironmentName>(),
                                                                     Arg.Any<CancellationToken>());
    }
}
