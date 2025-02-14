namespace JointLengthSequencing.Services;

/// <summary>
/// A service that implements the sequencing of two datasets of joints based on their lengths.
/// </summary>
public class JointLengthSequencer : IJointLengthSequencer
{
	/// <summary>
	/// Calculates a list of joint match results by aligning two datasets of joints based on their lengths.
	/// </summary>
	/// <param name="baseData">The base dataset of joints.</param>
	/// <param name="targetData">The target dataset of joints.</param>
	/// <param name="pivotPercentile">The percentile for selecting pivot joints. Defaults to 0.1.</param>
	/// <param name="tolerance">The tolerance for aligning pivot joints. Defaults to 1.5.</param>
	/// <param name="pivotRequired">The minimum number of pivot joints required for alignment. Defaults to 10.</param>
	/// <param name="baseLengthCol">The column name for the length of each joint in the base dataset. Defaults to "length".</param>
	/// <param name="targetLengthCol">The column name for the length of each joint in the target dataset. Defaults to "length".</param>
	/// <returns>A list of joint match results, where each result contains the index of the corresponding joint in the base dataset and the target dataset.</returns>
	public Task<List<JointMatchResult>> CalculateMatches(
		List<Dictionary<string, object>> baseData,
		List<Dictionary<string, object>> targetData,
		double pivotPercentile = 0.1,
		double tolerance = 1.5,
		int pivotRequired = 10,
		string baseLengthCol = "length",
		string targetLengthCol = "length")
	{
		var baseJoints = ProcessDataset(baseData, baseLengthCol);
		var targetJoints = ProcessDataset(targetData, targetLengthCol);

		var basePivots = SelectPivots(baseJoints, pivotPercentile, pivotRequired, tolerance);
		var targetPivots = SelectPivots(targetJoints, pivotPercentile, pivotRequired, tolerance);

		if (basePivots == null || targetPivots == null)
			return Task.FromResult(new List<JointMatchResult>());

		var alignedPivotPairs = AlignPivots(basePivots, targetPivots, tolerance);
		var allMatches = new List<JointMatchResult>();

		// Add pivot matches
		foreach (var pair in alignedPivotPairs)
		{
			allMatches.Add(new JointMatchResult
			{
				BaseIndex = pair.Item1.OriginalIndex,
				TargetIndex = pair.Item2.OriginalIndex
			});
		}

		// Process segments between pivots
		for (int i = 0; i < alignedPivotPairs.Count - 1; i++)
		{
			var currentPair = alignedPivotPairs[i];
			var nextPair = alignedPivotPairs[i + 1];

			var baseSegment = GetSegmentBetween(baseJoints,
				currentPair.Item1.Length,
				nextPair.Item1.Length);

			var targetSegment = GetSegmentBetween(targetJoints,
				currentPair.Item2.Length,
				nextPair.Item2.Length);

			var segmentMatches = AlignSegments(baseSegment, targetSegment, tolerance);
			allMatches.AddRange(segmentMatches);
		}

		return Task.FromResult(allMatches);
	}

	/// <summary>
	/// Processes the dataset by extracting the relevant column and creating a list of <see cref="Joint"/> objects.
	/// </summary>
	/// <param name="data">The dataset.</param>
	/// <param name="lengthColumn">The column name for the length values.</param>
	/// <returns>A list of <see cref="Joint"/> objects.</returns>
	private static List<Joint> ProcessDataset(List<Dictionary<string, object>> data, string lengthColumn)
	{
		return [.. data
			.Select((row, idx) => new Joint
			{
				OriginalIndex = idx,
				Length = Convert.ToDouble(row[lengthColumn].ToString())
			})
			.OrderBy(j => j.Length)];
	}

	/// <summary>
	/// Selects the pivot joints from the sorted joints list by taking the top percentile of joints and then
	/// filtering out joints that are not separated by a certain tolerance.
	/// </summary>
	/// <param name="sortedJoints">The sorted list of joints.</param>
	/// <param name="percentile">The percentile of joints to take as candidates. Defaults to 0.1.</param>
	/// <param name="required">The minimum number of pivot joints required. Defaults to 10.</param>
	/// <param name="tolerance">The tolerance for separating pivot joints. Defaults to 1.5.</param>
	/// <returns>A list of pivot joints, or <c>null</c> if the required number of pivot joints is not met.</returns>
	private static List<Joint>? SelectPivots(List<Joint> sortedJoints, double percentile, int required, double tolerance)
	{
		int totalJoints = sortedJoints.Count;
		int candidateCount = Math.Max((int)(percentile * totalJoints), required);
		var pivots = new List<Joint>();

		// Process joints in sorted order until we find enough pivots
		foreach (var joint in sortedJoints)
		{
			// Check if we should add this joint as a pivot
			if (pivots.Count == 0 || joint.Length >= pivots[^1].Length + tolerance)
			{
				pivots.Add(joint);
			}
		}

		// Final check and ordering
		if (pivots.Count < candidateCount)
			return null;

		return [.. pivots.OrderBy(j => j.Length)];
	}

	/// <summary>
	/// Aligns two lists of pivot joints by finding the longest contiguous subsequence of matching joints
	/// (i.e., joints with lengths that differ by no more than a given tolerance).
	/// </summary>
	/// <param name="basePivots">The list of pivot joints for the base dataset.</param>
	/// <param name="targetPivots">The list of pivot joints for the target dataset.</param>
	/// <param name="tolerance">The tolerance for determining whether two joints are matching.</param>
	/// <returns>A list of tuples, where each tuple contains a pivot joint from the base dataset and
	/// a pivot joint from the target dataset that form a matching pair.</returns>
	private static List<Tuple<Joint, Joint>> AlignPivots(List<Joint> basePivots, List<Joint> targetPivots, double tolerance)
	{
		int m = basePivots.Count;
		int n = targetPivots.Count;
		var dp = new Cell[m + 1, n + 1];

		// Initialize DP table
		for (int i = 0; i <= m; i++)
		{
			for (int j = 0; j <= n; j++)
			{
				dp[i, j] = new Cell(0, 0.0, 0);
			}
		}

		// Fill DP table with match quality
		for (int i = 1; i <= m; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				var baseJoint = basePivots[i - 1];
				var targetJoint = targetPivots[j - 1];
				double diff = Math.Abs(baseJoint.Length - targetJoint.Length);

				// Calculate potential diagonal improvement
				if (diff <= tolerance)
				{
					double quality = dp[i - 1, j - 1].Quality + (tolerance - diff);
					int score = dp[i - 1, j - 1].Score + 1;

					if (score > dp[i, j].Score || (score == dp[i, j].Score && quality > dp[i, j].Quality))
					{
						dp[i, j] = new Cell(score, quality, 1); // 1 = diagonal (match)
					}
				}

				// Check upward movement
				if (dp[i - 1, j].Score > dp[i, j].Score ||
					(dp[i - 1, j].Score == dp[i, j].Score && dp[i - 1, j].Quality > dp[i, j].Quality))
				{
					dp[i, j] = new Cell(dp[i - 1, j].Score, dp[i - 1, j].Quality, 2); // 2 = up
				}

				// Check leftward movement
				if (dp[i, j - 1].Score > dp[i, j].Score ||
					(dp[i, j - 1].Score == dp[i, j].Score && dp[i, j - 1].Quality > dp[i, j].Quality))
				{
					dp[i, j] = new Cell(dp[i, j - 1].Score, dp[i, j - 1].Quality, 3); // 3 = left
				}
			}
		}

		// Backtrace with quality awareness
		var alignedPairs = new List<Tuple<Joint, Joint>>();
		int x = m, y = n;
		while (x > 0 && y > 0)
		{
			switch (dp[x, y].Direction)
			{
				case 1: // Diagonal (match)
					alignedPairs.Add(Tuple.Create(basePivots[x - 1], targetPivots[y - 1]));
					x--;
					y--;
					break;

				case 2: // Up
					x--;
					break;

				case 3: // Left
					y--;
					break;
			}
		}

		alignedPairs.Reverse();
		return alignedPairs;
	}

	/// <summary>
	/// Returns the segment of joints between the given start and end indices (inclusive).
	/// </summary>
	/// <param name="joints">The list of joints to slice.</param>
	/// <param name="startLength">The starting length of the slice.</param>
	/// <param name="endLength">The ending length of the slice.</param>
	/// <returns>The segment of joints between the given start and end indices.</returns>
	private static List<Joint> GetSegmentBetween(List<Joint> joints, double startLength, double endLength)
	{
		return [.. joints
			.Where(j => j.Length > startLength && j.Length < endLength)
			.OrderBy(j => j.Length)];
	}

	/// <summary>
	/// Aligns two segments of joints using dynamic programming.
	/// </summary>
	/// <param name="baseSegment">The base segment of joints.</param>
	/// <param name="targetSegment">The target segment of joints.</param>
	/// <param name="tolerance">The tolerance for aligning joints.</param>
	/// <returns>The list of aligned joints.</returns>
	private static List<JointMatchResult> AlignSegments(List<Joint> baseSegment, List<Joint> targetSegment, double tolerance)
	{
		int m = baseSegment.Count;
		int n = targetSegment.Count;
		var dp = new Cell[m + 1, n + 1];

		// Initialize DP table
		for (int i = 0; i <= m; i++)
		{
			for (int j = 0; j <= n; j++)
			{
				dp[i, j] = new Cell(0, 0.0, 0);
			}
		}

		// Fill DP table with quality tracking
		for (int i = 1; i <= m; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				var baseJoint = baseSegment[i - 1];
				var targetJoint = targetSegment[j - 1];
				var diff = Math.Abs(baseJoint.Length - targetJoint.Length);

				// Check for potential match
				if (diff <= tolerance)
				{
					var diagonal = dp[i - 1, j - 1];
					var newScore = diagonal.Score + 1;
					var newQuality = diagonal.Quality + (tolerance - diff);

					if (newScore > dp[i, j].Score ||
					   (newScore == dp[i, j].Score && newQuality > dp[i, j].Quality))
					{
						dp[i, j] = new Cell(newScore, newQuality, 1);
						continue;
					}
				}

				// Evaluate non-match paths
				var up = dp[i - 1, j];
				var left = dp[i, j - 1];

				if (up.Score > left.Score ||
				   (up.Score == left.Score && up.Quality > left.Quality))
				{
					dp[i, j] = new Cell(up.Score, up.Quality, 2);
				}
				else
				{
					dp[i, j] = new Cell(left.Score, left.Quality, 3);
				}
			}
		}

		// Backtrace with quality awareness
		var matches = new List<JointMatchResult>();
		int x = m, y = n;
		while (x > 0 && y > 0)
		{
			switch (dp[x, y].Direction)
			{
				case 1: // Match
					matches.Add(new JointMatchResult
					{
						BaseIndex = baseSegment[x - 1].OriginalIndex,
						TargetIndex = targetSegment[y - 1].OriginalIndex
					});
					x--;
					y--;
					break;

				case 2: // Move up (skip base joint)
					x--;
					break;

				case 3: // Move left (skip target joint)
					y--;
					break;
			}
		}

		matches.Reverse();
		return matches;
	}
}