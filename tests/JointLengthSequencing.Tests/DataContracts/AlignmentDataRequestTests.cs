using FluentAssertions;
using JointLengthSequencing.DataContracts;

namespace JointLengthSequencing.Tests.DataContracts;

/// <summary>
/// Unit tests for AlignmentDataRequest validation.
/// </summary>
public class AlignmentDataRequestTests
{
    [Fact]
    public void Validate_WithValidData_ReturnsTrue()
    {
        // Arrange
        var request = new AlignmentDataRequest
        {
            BaseData = new List<Dictionary<string, object>>
            {
                new() { { "length", 2.0 } }
            },
            TargetData = new List<Dictionary<string, object>>
            {
                new() { { "length", 2.0 } }
            },
            PivotPercentile = 0.1,
            Tolerance = 1.5,
            PivotRequired = 10
        };

        List<string>? errors = null;

        // Act
        var result = request.Validate(ref errors);

        // Assert
        result.Should().BeTrue();
        errors.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Validate_WithNullBaseData_ReturnsFalse()
    {
        // Arrange
        var request = new AlignmentDataRequest
        {
            BaseData = null!,
            TargetData = new List<Dictionary<string, object>>
            {
                new() { { "length", 2.0 } }
            }
        };

        List<string>? errors = null;

        // Act
        var result = request.Validate(ref errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("BaseData"));
    }

    [Fact]
    public void Validate_WithEmptyTargetData_ReturnsFalse()
    {
        // Arrange
        var request = new AlignmentDataRequest
        {
            BaseData = new List<Dictionary<string, object>>
            {
                new() { { "length", 2.0 } }
            },
            TargetData = new List<Dictionary<string, object>>()
        };

        List<string>? errors = null;

        // Act
        var result = request.Validate(ref errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("TargetData"));
    }

    [Fact]
    public void Validate_WithInvalidPivotPercentile_ReturnsFalse()
    {
        // Arrange
        var request = new AlignmentDataRequest
        {
            BaseData = new List<Dictionary<string, object>>
            {
                new() { { "length", 2.0 } }
            },
            TargetData = new List<Dictionary<string, object>>
            {
                new() { { "length", 2.0 } }
            },
            PivotPercentile = 1.5  // Invalid: > 1
        };

        List<string>? errors = null;

        // Act
        var result = request.Validate(ref errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("PivotPercentile"));
    }

    [Fact]
    public void Validate_WithNegativeTolerance_ReturnsFalse()
    {
        // Arrange
        var request = new AlignmentDataRequest
        {
            BaseData = new List<Dictionary<string, object>>
            {
                new() { { "length", 2.0 } }
            },
            TargetData = new List<Dictionary<string, object>>
            {
                new() { { "length", 2.0 } }
            },
            Tolerance = -1.0  // Invalid
        };

        List<string>? errors = null;

        // Act
        var result = request.Validate(ref errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("Tolerance"));
    }

    [Fact]
    public void Validate_WithZeroPivotRequired_ReturnsFalse()
    {
        // Arrange
        var request = new AlignmentDataRequest
        {
            BaseData = new List<Dictionary<string, object>>
            {
                new() { { "length", 2.0 } }
            },
            TargetData = new List<Dictionary<string, object>>
            {
                new() { { "length", 2.0 } }
            },
            PivotRequired = 0  // Invalid
        };

        List<string>? errors = null;

        // Act
        var result = request.Validate(ref errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("PivotRequired"));
    }

    [Fact]
    public void Validate_WithEmptyColumnName_ReturnsFalse()
    {
        // Arrange
        var request = new AlignmentDataRequest
        {
            BaseData = new List<Dictionary<string, object>>
            {
                new() { { "length", 2.0 } }
            },
            TargetData = new List<Dictionary<string, object>>
            {
                new() { { "length", 2.0 } }
            },
            BaseLengthCol = ""  // Invalid
        };

        List<string>? errors = null;

        // Act
        var result = request.Validate(ref errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("BaseLengthCol"));
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var request = new AlignmentDataRequest
        {
            BaseData = null!,
            TargetData = new List<Dictionary<string, object>>(),
            PivotPercentile = 2.0,
            Tolerance = -1.0,
            PivotRequired = 0
        };

        List<string>? errors = null;

        // Act
        var result = request.Validate(ref errors);

        // Assert
        result.Should().BeFalse();
        errors.Should().HaveCountGreaterThan(3);
    }
}
