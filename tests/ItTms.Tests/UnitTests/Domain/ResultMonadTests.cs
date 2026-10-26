using FluentAssertions;
using ItTms.Domain.Common;

namespace ItTms.Tests.UnitTests.Domain;

public class ResultMonadTests
{
    [Fact]
    public void Success_CreatesSuccessResultWithoutError()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_CreatesFailureResultWithSpecifiedError()
    {
        // Arrange
        var error = Error.Validation("INVALID_INPUT", "Dữ liệu không hợp lệ.");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void TypedResult_AccessingValueOnFailure_ThrowsInvalidOperationException()
    {
        // Arrange
        var result = Result<string>.Failure(new Error("FAILED", "Lỗi"));

        // Act
        Action act = () => _ = result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access Value on a failed result.");
    }

    [Fact]
    public void TypedResult_ImplicitConversion_WorksCorrectly()
    {
        // Act
        Result<int> successResult = 42;
        Result<int> failureResult = new Error("FAIL", "Thất bại");

        // Assert
        successResult.IsSuccess.Should().BeTrue();
        successResult.Value.Should().Be(42);

        failureResult.IsFailure.Should().BeTrue();
        failureResult.Error.Code.Should().Be("FAIL");
    }
}
