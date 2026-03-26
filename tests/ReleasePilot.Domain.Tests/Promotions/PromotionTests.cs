using ErrorOr;
using FluentAssertions;
using ReleasePilot.Domain.Promotions;
using ReleasePilot.Domain.Promotions.Events;
using ReleasePilot.Domain.Promotions.ValueObjects;
using ApplicationId = ReleasePilot.Domain.Promotions.ValueObjects.ApplicationId;

namespace ReleasePilot.Domain.Tests.Promotions;

public class PromotionTests
{
    private static Promotion CreateRequested()
    {
        var appId = ApplicationId.NewId();
        var version = ApplicationVersion.Create("1.0.0").Value;
        var env = EnvironmentName.Create("staging").Value;
        var sourceEnv = EnvironmentName.Create("dev").Value;
        return Promotion.Request(appId, version, env, sourceEnv, "alice").Value;
    }

    private static Promotion CreateApproved()
    {
        var p = CreateRequested();
        p.Approve("bob", Promotion.ApproverRole);
        return p;
    }

    private static Promotion CreateInProgress()
    {
        var p = CreateApproved();
        p.StartDeployment("system");
        return p;
    }

    public class HappyPath
    {
        [Fact]
        public void Should_CreatePromotionInRequestedState_When_Requested()
        {
            // Arrange
            var appId = ApplicationId.NewId();
            var version = ApplicationVersion.Create("2.3.1").Value;
            var env = EnvironmentName.Create("production").Value;
            var source = EnvironmentName.Create("staging").Value;

            // Act
            var result = Promotion.Request(appId, version, env, source, "alice");

            // Assert
            result.IsError.Should().BeFalse();
            result.Value.State.Should().Be(PromotionState.Requested);
            result.Value.RequestedBy.Should().Be("alice");
        }

        [Fact]
        public void Should_RaisePromotionRequestedEvent_When_Requested()
        {
            // Arrange
            var appId = ApplicationId.NewId();
            var version = ApplicationVersion.Create("1.0.0").Value;
            var env = EnvironmentName.Create("dev").Value;

            // Act
            var result = Promotion.Request(appId, version, env, null, "alice");

            // Assert
            var evt = result.Value.DomainEvents.Should().ContainSingle().Subject;
            evt.Should().BeOfType<PromotionRequested>();
            ((PromotionRequested)evt).PromotionId.Should().Be(result.Value.Id);
        }

        [Fact]
        public void Should_TransitionToApproved_When_ApproveCalledWithValidRole()
        {
            // Arrange
            var promotion = CreateRequested();

            // Act
            var result = promotion.Approve("bob", Promotion.ApproverRole);

            // Assert
            result.IsError.Should().BeFalse();
            promotion.State.Should().Be(PromotionState.Approved);
            promotion.ApprovedBy.Should().Be("bob");
            promotion.DomainEvents.Should().Contain(e => e is PromotionApproved);
        }

        [Fact]
        public void Should_TransitionToInProgress_When_DeploymentStarted()
        {
            // Arrange
            var promotion = CreateApproved();

            // Act
            var result = promotion.StartDeployment("alice");

            // Assert
            result.IsError.Should().BeFalse();
            promotion.State.Should().Be(PromotionState.InProgress);
            promotion.StartedAt.Should().NotBeNull();
            promotion.DomainEvents.Should().Contain(e => e is DeploymentStarted);
        }

        [Fact]
        public void Should_TransitionToCompleted_When_Completed()
        {
            // Arrange
            var promotion = CreateInProgress();

            // Act
            var result = promotion.Complete("alice");

            // Assert
            result.IsError.Should().BeFalse();
            promotion.State.Should().Be(PromotionState.Completed);
            promotion.CompletedAt.Should().NotBeNull();
            promotion.DomainEvents.Should().Contain(e => e is PromotionCompleted);
        }

        [Fact]
        public void Should_TransitionToRolledBack_When_RolledBack()
        {
            // Arrange
            var promotion = CreateInProgress();

            // Act
            var result = promotion.Rollback("Deployment failed health checks.", "alice");

            // Assert
            result.IsError.Should().BeFalse();
            promotion.State.Should().Be(PromotionState.RolledBack);
            promotion.RollbackReason.Should().Be("Deployment failed health checks.");
            promotion.DomainEvents.Should().Contain(e => e is PromotionRolledBack);
        }

        [Fact]
        public void Should_TransitionToCancelled_When_Cancelled()
        {
            // Arrange
            var promotion = CreateRequested();

            // Act
            var result = promotion.Cancel("No longer needed.", "alice");

            // Assert
            result.IsError.Should().BeFalse();
            promotion.State.Should().Be(PromotionState.Cancelled);
            promotion.CancellationReason.Should().Be("No longer needed.");
            promotion.DomainEvents.Should().Contain(e => e is PromotionCancelled);
        }
    }

    public class InvalidTransitions
    {
        [Fact]
        public void Should_ReturnConflictError_When_ApproveCalledOnAlreadyApprovedPromotion()
        {
            // Arrange
            var promotion = CreateApproved();

            // Act
            var result = promotion.Approve("carol", Promotion.ApproverRole);

            // Assert
            result.IsError.Should().BeTrue();
            result.FirstError.Type.Should().Be(ErrorType.Conflict);
        }

        [Fact]
        public void Should_ReturnConflictError_When_StartDeploymentCalledOnRequestedPromotion()
        {
            // Arrange
            var promotion = CreateRequested();

            // Act
            var result = promotion.StartDeployment("alice");

            // Assert
            result.IsError.Should().BeTrue();
            result.FirstError.Type.Should().Be(ErrorType.Conflict);
        }

        [Fact]
        public void Should_ReturnConflictError_When_CompleteCalledOnApprovedPromotion()
        {
            // Arrange
            var promotion = CreateApproved();

            // Act
            var result = promotion.Complete("alice");

            // Assert
            result.IsError.Should().BeTrue();
            result.FirstError.Type.Should().Be(ErrorType.Conflict);
        }

        [Fact]
        public void Should_ReturnConflictError_When_RollbackCalledOnApprovedPromotion()
        {
            // Arrange
            var promotion = CreateApproved();

            // Act
            var result = promotion.Rollback("some reason", "alice");

            // Assert
            result.IsError.Should().BeTrue();
            result.FirstError.Type.Should().Be(ErrorType.Conflict);
        }

        [Fact]
        public void Should_ReturnConflictError_When_CancelCalledOnApprovedPromotion()
        {
            // Arrange
            var promotion = CreateApproved();

            // Act
            var result = promotion.Cancel("changed mind", "alice");

            // Assert
            result.IsError.Should().BeTrue();
            result.FirstError.Type.Should().Be(ErrorType.Conflict);
        }

        [Fact]
        public void Should_ReturnConflictError_When_CancelCalledOnInProgressPromotion()
        {
            // Arrange
            var promotion = CreateInProgress();

            // Act
            var result = promotion.Cancel("changed mind", "alice");

            // Assert
            result.IsError.Should().BeTrue();
            result.FirstError.Type.Should().Be(ErrorType.Conflict);
        }

        [Fact]
        public void Should_RejectAllTransitions_When_PromotionIsCompleted()
        {
            // Arrange
            var promotion = CreateInProgress();
            promotion.Complete("system");

            // Act & Assert
            promotion.Approve("bob", Promotion.ApproverRole).IsError.Should().BeTrue();
            promotion.StartDeployment("alice").IsError.Should().BeTrue();
            promotion.Complete("alice").IsError.Should().BeTrue();
            promotion.Rollback("reason", "alice").IsError.Should().BeTrue();
            promotion.Cancel("reason", "alice").IsError.Should().BeTrue();
        }

        [Fact]
        public void Should_RejectAllTransitions_When_PromotionIsCancelled()
        {
            // Arrange
            var promotion = CreateRequested();
            promotion.Cancel("cancelled", "alice");

            // Act & Assert
            promotion.Approve("bob", Promotion.ApproverRole).IsError.Should().BeTrue();
            promotion.StartDeployment("alice").IsError.Should().BeTrue();
            promotion.Complete("alice").IsError.Should().BeTrue();
            promotion.Rollback("reason", "alice").IsError.Should().BeTrue();
            promotion.Cancel("another reason", "alice").IsError.Should().BeTrue();
        }
    }

    public class BusinessRules
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_ReturnValidationError_When_RollbackReasonIsEmpty(string reason)
        {
            // Arrange
            var promotion = CreateInProgress();

            // Act
            var result = promotion.Rollback(reason, "alice");

            // Assert
            result.IsError.Should().BeTrue();
            result.FirstError.Type.Should().Be(ErrorType.Validation);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_ReturnValidationError_When_CancelReasonIsEmpty(string reason)
        {
            // Arrange
            var promotion = CreateRequested();

            // Act
            var result = promotion.Cancel(reason, "alice");

            // Assert
            result.IsError.Should().BeTrue();
            result.FirstError.Type.Should().Be(ErrorType.Validation);
        }

        [Fact]
        public void Should_ReturnUnauthorizedError_When_ApproverRoleIsInvalid()
        {
            // Arrange
            var promotion = CreateRequested();

            // Act
            var result = promotion.Approve("alice", "Developer");

            // Assert
            result.IsError.Should().BeTrue();
            result.FirstError.Type.Should().Be(ErrorType.Unauthorized);
            result.FirstError.Code.Should().Be("Promotion.Unauthorized");
        }

        [Fact]
        public void Should_StoreIssueReferences_When_ProvidedOnRequest()
        {
            // Arrange
            var appId = ApplicationId.NewId();
            var version = ApplicationVersion.Create("1.0.0").Value;
            var env = EnvironmentName.Create("dev").Value;
            var issueRefs = new[] { "PROJ-1", "PROJ-2" };

            // Act
            var result = Promotion.Request(appId, version, env, null, "alice", issueRefs);

            // Assert
            result.IsError.Should().BeFalse();
            result.Value.IssueReferences.Should().BeEquivalentTo(issueRefs);
        }

        [Fact]
        public void Should_HaveEmptyIssueReferences_When_NoneProvided()
        {
            // Arrange
            var appId = ApplicationId.NewId();
            var version = ApplicationVersion.Create("1.0.0").Value;
            var env = EnvironmentName.Create("dev").Value;

            // Act
            var result = Promotion.Request(appId, version, env, null, "alice");

            // Assert
            result.IsError.Should().BeFalse();
            result.Value.IssueReferences.Should().BeEmpty();
        }
    }
}
