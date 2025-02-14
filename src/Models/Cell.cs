namespace JointLengthSequencing.Models;

/// <summary>
/// Represents a cell with a score, quality, and direction.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Cell"/> struct.
/// </remarks>
/// <param name="score">The score of the cell.</param>
/// <param name="quality">The quality of the cell.</param>
/// <param name="direction">The direction of the cell.</param>
public readonly struct Cell(int score, double quality, int direction)
{
    /// <summary>
    /// Gets the score of the cell.
    /// </summary>
    public int Score { get; } = score;

    /// <summary>
    /// Gets the quality of the cell.
    /// </summary>
    public double Quality { get; } = quality;

    /// <summary>
    /// Gets the direction of the cell.
    /// </summary>
    public int Direction { get; } = direction; // 1=diagonal, 2=up, 3=left
}
