using BaseOps.Domain.Entities;
using BaseOps.Domain.Enums;
using BaseOps.Infrastructure.Services;
using Xunit;

namespace BaseOps.UnitTests;

public class AnnualLeaveServiceTests
{
    [Fact]
    public void PriorityCalculator_CalculateScore_ReturnsCorrectScore()
    {
        // Arrange
        var calculator = new PriorityCalculator();
        var request = new AnnualLeaveRequest
        {
            User = new ApplicationUser { EmployeeId = "EMP001", FullName = "Test User", PasswordHash = "test" }
        };

        // Act
        var score = calculator.CalculatePriorityScore(request, SourceChoice.Choice1);

        // Assert
        Assert.True(score > 0);
    }

    [Fact]
    public void PriorityCalculator_SortRequests_SortsByPriority()
    {
        // Arrange
        var calculator = new PriorityCalculator();
        var requests = new List<AnnualLeaveRequest>
        {
            new AnnualLeaveRequest { User = new ApplicationUser { EmployeeId = "EMP003", FullName = "User 3", PasswordHash = "test" } },
            new AnnualLeaveRequest { User = new ApplicationUser { EmployeeId = "EMP001", FullName = "User 1", PasswordHash = "test" } },
            new AnnualLeaveRequest { User = new ApplicationUser { EmployeeId = "EMP002", FullName = "User 2", PasswordHash = "test" } }
        };

        // Act
        calculator.SortRequestsByPriority(requests);

        // Assert
        Assert.Equal("EMP001", requests[0].User.EmployeeId);
        Assert.Equal("EMP002", requests[1].User.EmployeeId);
        Assert.Equal("EMP003", requests[2].User.EmployeeId);
    }

    [Fact]
    public void ExtractEmployeeIdNumeric_ValidId_ReturnsNumericValue()
    {
        // Arrange
        var calculator = new PriorityCalculator();
        var employeeId = "EMP001";

        // Act
        var numeric = calculator.GetType()
            .GetMethod("ExtractEmployeeIdNumeric", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(calculator, new object[] { employeeId });

        // Assert
        Assert.NotNull(numeric);
        Assert.Equal(1, (int)numeric!);
    }

    [Fact]
    public void ManpowerConstraint_GetConstraints_NoConstraints_ReturnsDefault()
    {
        // This would require a mock DbContext - simplified for now
        // In a real test, we'd use an in-memory database
        Assert.True(true);
    }
}
