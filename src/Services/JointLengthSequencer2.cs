namespace JointLengthSequencing.Services;

/// <summary>
/// A service that implements the sequencing of two datasets of joints based on their lengths.
/// </summary>
/// <remarks>
/// Optimized
/// </remarks>
public class JointLengthSequencer2 : IJointLengthSequencer
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
		if (baseData == null || targetData == null || baseData.Count == 0 || targetData.Count == 0)
			return Task.FromResult(new List<JointMatchResult>());

		var baseJoints = ProcessDataset(baseData, baseLengthCol);
		var targetJoints = ProcessDataset(targetData, targetLengthCol);

		var basePivots = SelectPivots(baseJoints, pivotPercentile, pivotRequired, tolerance);
		var targetPivots = SelectPivots(targetJoints, pivotPercentile, pivotRequired, tolerance);

		if (basePivots == null || targetPivots == null || basePivots.Count < pivotRequired || targetPivots.Count < pivotRequired)
			return Task.FromResult(new List<JointMatchResult>());

		var alignedPivotPairs = AlignPivots(basePivots, targetPivots, tolerance);
		if (alignedPivotPairs.Count == 0)
			return Task.FromResult(new List<JointMatchResult>());

		var allMatches = new List<JointMatchResult>(alignedPivotPairs.Count * 2);

		foreach (var pair in alignedPivotPairs)
		{
			allMatches.Add(new JointMatchResult
			{
				BaseIndex = pair.Item1.OriginalIndex,
				TargetIndex = pair.Item2.OriginalIndex
			});
		}

		var alignedPairsArray = alignedPivotPairs.ToArray();
		for (int i = 0; i < alignedPairsArray.Length - 1; i++)
		{
			var currentPair = alignedPairsArray[i];
			var nextPair = alignedPairsArray[i + 1];

			var baseSegment = GetSegmentBetween(baseJoints, currentPair.Item1.Length, nextPair.Item1.Length);
			var targetSegment = GetSegmentBetween(targetJoints, currentPair.Item2.Length, nextPair.Item2.Length);

			if (baseSegment.Count == 0 || targetSegment.Count == 0)
				continue;

			var segmentMatches = AlignSegments(baseSegment, targetSegment, tolerance);
			allMatches.AddRange(segmentMatches);
		}

		return Task.FromResult(allMatches);
	}

	/// <summary>
	/// Processes the dataset by extracting the length values from the specified column and creates a sorted list of <see cref="Joint"/> objects.
	/// </summary>
	/// <param name="data">The dataset represented as a list of dictionaries, where each dictionary corresponds to a joint and contains its attributes.</param>
	/// <param name="lengthColumn">The name of the column containing the length values for each joint.</param>
	/// <returns>A sorted list of <see cref="Joint"/> objects based on their lengths.</returns>
	private static List<Joint> ProcessDataset(List<Dictionary<string, object>> data, string lengthColumn)
	{
		var joints = new Joint[data.Count];
		for (int i = 0; i < data.Count; i++)
		{
			joints[i] = new Joint
			{
				OriginalIndex = i,
				Length = Convert.ToDouble(data[i][lengthColumn].ToString())
			};
		}
		Array.Sort(joints, (a, b) => a.Length.CompareTo(b.Length));
		return [.. joints];
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
		if (totalJoints == 0) return null;

		int candidateCount = Math.Max((int)(percentile * totalJoints), required);
		var pivots = new List<Joint>(candidateCount);
		int currentIndex = 0;

		while (currentIndex < sortedJoints.Count)
		{
			var currentJoint = sortedJoints[currentIndex];
			pivots.Add(currentJoint);
			double nextLength = currentJoint.Length + tolerance;
			currentIndex = FindFirstIndexGreaterOrEqual(sortedJoints, currentIndex + 1, nextLength);
		}

		return pivots.Count >= candidateCount ? pivots : null;
	}

	/// <summary>
	/// Finds the first index in the list of joints where the joint length is greater than or equal to the specified target length.
	/// </summary>
	/// <param name="joints">The list of joints to search through.</param>
	/// <param name="startIndex">The starting index for the search.</param>
	/// <param name="targetLength">The target length to compare against.</param>
	/// <returns>The index of the first joint with a length greater than or equal to the target length, or the count of joints if no such joint is found.</returns>
	private static int FindFirstIndexGreaterOrEqual(List<Joint> joints, int startIndex, double targetLength)
	{
		int low = startIndex;
		int high = joints.Count - 1;
		int result = joints.Count;

		while (low <= high)
		{
			int mid = (low + high) / 2;
			if (joints[mid].Length >= targetLength)
			{
				result = mid;
				high = mid - 1;
			}
			else
			{
				low = mid + 1;
			}
		}
		return result;
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
		var dp = new Cell[m + 1][];
		for (int i = 0; i <= m; i++)
			dp[i] = new Cell[n + 1];

		// DP table initialization and filling optimized with jagged arrays
		for (int i = 1; i <= m; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				double diff = Math.Abs(basePivots[i - 1].Length - targetPivots[j - 1].Length);
				Cell currentBest = new Cell(0, 0.0, 0);

				if (diff <= tolerance)
				{
					Cell diagonal = dp[i - 1][j - 1];
					int newScore = diagonal.Score + 1;
					double newQuality = diagonal.Quality + (tolerance - diff);
					if (newScore > currentBest.Score || (newScore == currentBest.Score && newQuality > currentBest.Quality))
					{
						currentBest = new Cell(newScore, newQuality, 1);
					}
				}

				Cell up = dp[i - 1][j];
				Cell left = dp[i][j - 1];

				if (up.Score > currentBest.Score || (up.Score == currentBest.Score && up.Quality > currentBest.Quality))
					currentBest = new Cell(up.Score, up.Quality, 2);

				if (left.Score > currentBest.Score || (left.Score == currentBest.Score && left.Quality > currentBest.Quality))
					currentBest = new Cell(left.Score, left.Quality, 3);

				dp[i][j] = currentBest;
			}
		}

		// Backtrace logic remains similar
		var alignedPairs = new List<Tuple<Joint, Joint>>();
		int x = m, y = n;
		while (x > 0 && y > 0)
		{
			switch (dp[x][y].Direction)
			{
				case 1:
					alignedPairs.Add(Tuple.Create(basePivots[x - 1], targetPivots[y - 1]));
					x--;
					y--;
					break;

				case 2:
					x--;
					break;

				case 3:
					y--;
					break;
			}
		}
		alignedPairs.Reverse();
		return alignedPairs;
	}

	/// <summary>
	/// Retrieves a sublist of joints from a sorted list where each joint's length is between the specified start and end lengths (exclusive).
	/// </summary>
	/// <param name="joints">The sorted list of joints to search within.</param>
	/// <param name="startLength">The lower bound length for the joints to be included.</param>
	/// <param name="endLength">The upper bound length for the joints to be included.</param>
	/// <returns>A list of joints whose lengths are within the specified range, or an empty list if no such joints exist.</returns>
	private static List<Joint> GetSegmentBetween(List<Joint> joints, double startLength, double endLength)
	{
		int startIdx = FindFirstIndexGreaterThan(joints, startLength);
		int endIdx = FindLastIndexLessThan(joints, endLength);
		if (startIdx >= joints.Count || endIdx < 0 || startIdx > endIdx)
			return [];

		return joints.GetRange(startIdx, endIdx - startIdx + 1);
	}

	/// <summary>
	/// Finds the first index in the sorted list of joints whose length is greater than the target value.
	/// </summary>
	/// <param name="joints">The sorted list of joints.</param>
	/// <param name="target">The target value.</param>
	/// <returns>The first index greater than the target value, or the count of joints if no such index is found.</returns>
	private static int FindFirstIndexGreaterThan(List<Joint> joints, double target)
	{
		int low = 0;
		int high = joints.Count - 1;
		int result = joints.Count;
		while (low <= high)
		{
			int mid = (low + high) / 2;
			if (joints[mid].Length > target)
			{
				result = mid;
				high = mid - 1;
			}
			else
			{
				low = mid + 1;
			}
		}
		return result;
	}

	/// <summary>
	/// Finds the last index in the sorted list of joints whose length is less than the target value.
	/// </summary>
	/// <param name="joints">The sorted list of joints.</param>
	/// <param name="target">The target value.</param>
	/// <returns>The last index less than the target value, or -1 if no such index is found.</returns>
	private static int FindLastIndexLessThan(List<Joint> joints, double target)
	{
		int low = 0;
		int high = joints.Count - 1;
		int result = -1;
		while (low <= high)
		{
			int mid = (low + high) / 2;
			if (joints[mid].Length < target)
			{
				result = mid;
				low = mid + 1;
			}
			else
			{
				high = mid - 1;
			}
		}
		return result;
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
		if (m == 0 || n == 0)
			return [];

		// Similar DP optimizations as in AlignPivots
		var dp = new Cell[m + 1][];
		for (int i = 0; i <= m; i++)
			dp[i] = new Cell[n + 1];

		// DP filling logic with early exit conditions
		for (int i = 1; i <= m; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				double diff = Math.Abs(baseSegment[i - 1].Length - targetSegment[j - 1].Length);
				Cell currentBest = new(0, 0.0, 0);

				if (diff <= tolerance)
				{
					Cell diagonal = dp[i - 1][j - 1];
					int newScore = diagonal.Score + 1;
					double newQuality = diagonal.Quality + (tolerance - diff);
					if (newScore > currentBest.Score || (newScore == currentBest.Score && newQuality > currentBest.Quality))
					{
						currentBest = new Cell(newScore, newQuality, 1);
					}
				}

				Cell up = dp[i - 1][j];
				Cell left = dp[i][j - 1];

				if (up.Score > currentBest.Score || (up.Score == currentBest.Score && up.Quality > currentBest.Quality))
					currentBest = up;

				if (left.Score > currentBest.Score || (left.Score == currentBest.Score && left.Quality > currentBest.Quality))
					currentBest = left;

				dp[i][j] = currentBest;
			}
		}

		// Backtrace logic
		var matches = new List<JointMatchResult>();
		int x = m, y = n;
		while (x > 0 && y > 0)
		{
			switch (dp[x][y].Direction)
			{
				case 1:
					matches.Add(new JointMatchResult
					{
						BaseIndex = baseSegment[x - 1].OriginalIndex,
						TargetIndex = targetSegment[y - 1].OriginalIndex
					});
					x--;
					y--;
					break;

				case 2:
					x--;
					break;

				case 3:
					y--;
					break;
			}
		}
		matches.Reverse();
		return matches;
	}
}