using FluentAssertions;
using JointLengthSequencing.Services;

namespace JointLengthSequencing.Tests.Services;

/// <summary>
/// Unit tests for JointLengthSequencer service.
/// </summary>
public class JointLengthSequencerTests
{
    private readonly JointLengthSequencer _sequencer;

    public JointLengthSequencerTests()
    {
        _sequencer = new JointLengthSequencer();
    }

    [Fact]
    public async Task CalculateMatches_WithValidData_ReturnsMatches()
    {
        // Arrange
        var baseData = new List<Dictionary<string, object>>
        {
            new() { { "length", 2.0 } },
            new() { { "length", 4.5 } },
            new() { { "length", 6.0 } },
            new() { { "length", 8.5 } },
            new() { { "length", 10.0 } }
        };

        var targetData = new List<Dictionary<string, object>>
        {
            new() { { "length", 2.1 } },
            new() { { "length", 4.6 } },
            new() { { "length", 6.1 } },
            new() { { "length", 8.6 } },
            new() { { "length", 10.1 } }
        };

        // Act
        var result = await _sequencer.CalculateMatches(
            baseData,
            targetData,
            pivotPercentile: 0.5,
            tolerance: 0.5,
            pivotRequired: 2);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CalculateMatches_WithEmptyBaseData_ReturnsEmptyList()
    {
        // Arrange
        var baseData = new List<Dictionary<string, object>>();
        var targetData = new List<Dictionary<string, object>>
        {
            new() { { "length", 2.0 } }
        };

        // Act
        var result = await _sequencer.CalculateMatches(baseData, targetData);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateMatches_WithEmptyTargetData_ReturnsEmptyList()
    {
        // Arrange
        var baseData = new List<Dictionary<string, object>>
        {
            new() { { "length", 2.0 } }
        };
        var targetData = new List<Dictionary<string, object>>();

        // Act
        var result = await _sequencer.CalculateMatches(baseData, targetData);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CalculateMatches_WithInvalidColumnName_ThrowsArgumentException()
    {
        // Arrange
        var baseData = new List<Dictionary<string, object>>
        {
            new() { { "length", 2.0 } }
        };
        var targetData = new List<Dictionary<string, object>>
        {
            new() { { "length", 2.0 } }
        };

        // Act & Assert
        var act = async () => await _sequencer.CalculateMatches(
            baseData,
            targetData,
            baseLengthCol: "nonexistent");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*nonexistent*");
    }

    [Fact]
    public async Task CalculateMatches_WithInvalidLengthValue_ThrowsArgumentException()
    {
        // Arrange
        var baseData = new List<Dictionary<string, object>>
        {
            new() { { "length", "invalid" } }
        };
        var targetData = new List<Dictionary<string, object>>
        {
            new() { { "length", 2.0 } }
        };

        // Act & Assert
        var act = async () => await _sequencer.CalculateMatches(baseData, targetData);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid length value*");
    }

    [Fact]
    public async Task CalculateMatches_WithCustomTolerance_RespectsToleranceValue()
    {
        // Arrange
        var baseData = new List<Dictionary<string, object>>
        {
            new() { { "length", 2.0 } },
            new() { { "length", 5.0 } },
            new() { { "length", 10.0 } }
        };

        var targetData = new List<Dictionary<string, object>>
        {
            new() { { "length", 2.5 } },  // Within 1.0 tolerance
            new() { { "length", 5.8 } },  // Within 1.0 tolerance
            new() { { "length", 11.0 } }  // Within 1.0 tolerance
        };

        // Act
        var result = await _sequencer.CalculateMatches(
            baseData,
            targetData,
            tolerance: 1.0,
            pivotRequired: 1);

        // Assert
        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CalculateMatches_WithDifferentColumnNames_ProcessesCorrectly()
    {
        // Arrange
        var baseData = new List<Dictionary<string, object>>
        {
            new() { { "baseLength", 2.0 } },
            new() { { "baseLength", 4.0 } }
        };

        var targetData = new List<Dictionary<string, object>>
        {
            new() { { "targetLength", 2.1 } },
            new() { { "targetLength", 4.1 } }
        };

        // Act
        var result = await _sequencer.CalculateMatches(
            baseData,
            targetData,
            baseLengthCol: "baseLength",
            targetLengthCol: "targetLength",
            pivotRequired: 1);

        // Assert
        result.Should().NotBeNull();
    }
}
