using System.Collections.Concurrent;

namespace JointLengthSequencing.Services;

/// <summary>
/// A service that implements the sequencing of two datasets of joints based on their lengths using dynamic programming.
/// </summary>
/// <remarks>
/// Improved performance on optimized.
/// </remarks>
public class JointLengthSequencer3 : IJointLengthSequencer
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
		if (baseData?.Count == 0 || targetData?.Count == 0)
			return Task.FromResult(new List<JointMatchResult>());

		var baseJoints = ProcessDataset(baseData!, baseLengthCol);
		var targetJoints = ProcessDataset(targetData!, targetLengthCol);

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
		var segmentResults = new ConcurrentBag<List<JointMatchResult>>();

		Parallel.For(0, alignedPairsArray.Length - 1, i =>
		{
			var currentPair = alignedPairsArray[i];
			var nextPair = alignedPairsArray[i + 1];

			(int baseStart, int baseEnd) = GetSegmentIndices(baseJoints, currentPair.Item1.Length, nextPair.Item1.Length);
			(int targetStart, int targetEnd) = GetSegmentIndices(targetJoints, currentPair.Item2.Length, nextPair.Item2.Length);

			if (baseStart > baseEnd || targetStart > targetEnd)
				return;

			var segmentMatches = AlignSegments(baseJoints, baseStart, baseEnd, targetJoints, targetStart, targetEnd, tolerance);
			segmentResults.Add(segmentMatches);
		});

		foreach (var matches in segmentResults)
			allMatches.AddRange(matches);

		return Task.FromResult(allMatches);
	}

	/// <summary>
	/// Processes the dataset by extracting the relevant column and creating a list of <see cref="Joint"/> objects.
	/// </summary>
	/// <param name="data">The dataset.</param>
	/// <param name="lengthColumn">The column name for the length values.</param>
	/// <returns>A list of <see cref="Joint"/> objects sorted by their lengths.</returns>
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
		if (sortedJoints.Count == 0) return null;

		int candidateCount = Math.Max((int)(percentile * sortedJoints.Count), required);
		var pivots = new List<Joint>(candidateCount);
		int currentIndex = 0;

		var lengthComparer = Comparer<Joint>.Create((a, b) => a.Length.CompareTo(b.Length));
		while (currentIndex < sortedJoints.Count)
		{
			var currentJoint = sortedJoints[currentIndex];
			pivots.Add(currentJoint);
			double nextLength = currentJoint.Length + tolerance;

			// Use built-in BinarySearch to find next index
			int searchResult = sortedJoints.BinarySearch(currentIndex + 1, sortedJoints.Count - currentIndex - 1,
				new Joint { Length = nextLength }, lengthComparer);
			currentIndex = searchResult < 0 ? ~searchResult : searchResult;
		}

		return pivots.Count >= candidateCount ? pivots : null;
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
		int m = basePivots.Count, n = targetPivots.Count;
		var dp = new Cell[m + 1][];
		for (int i = 0; i <= m; i++)
			dp[i] = new Cell[n + 1];

		// Precompute lengths for faster access
		var baseLengths = basePivots.Select(j => j.Length).ToArray();
		var targetLengths = targetPivots.Select(j => j.Length).ToArray();

		for (int i = 1; i <= m; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				double diff = Math.Abs(baseLengths[i - 1] - targetLengths[j - 1]);
				Cell currentBest = new(0, 0.0, 0);

				if (diff <= tolerance)
				{
					Cell diagonal = dp[i - 1][j - 1];
					int newScore = diagonal.Score + 1;
					double newQuality = diagonal.Quality + (tolerance - diff);
					if (newScore > currentBest.Score || (newScore == currentBest.Score && newQuality > currentBest.Quality))
						currentBest = new Cell(newScore, newQuality, 1);
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

		// Backtrace logic remains unchanged
		var alignedPairs = new List<Tuple<Joint, Joint>>();
		int x = m, y = n;
		while (x > 0 && y > 0)
		{
			switch (dp[x][y].Direction)
			{
				case 1:
					alignedPairs.Add(Tuple.Create(basePivots[x - 1], targetPivots[y - 1]));
					x--; y--;
					break;

				case 2: x--; break;
				case 3: y--; break;
			}
		}
		alignedPairs.Reverse();
		return alignedPairs;
	}

	/// <summary>
	/// Finds the start and end indices for a segment of joints whose lengths lie between the given start and end lengths.
	/// </summary>
	/// <param name="joints">The list of joints to search within.</param>
	/// <param name="startLength">The lower bound length for the joints to be included.</param>
	/// <param name="endLength">The upper bound length for the joints to be included.</param>
	/// <returns>A tuple containing the start and end indices of the segment, inclusive.</returns>
	private static (int startIdx, int endIdx) GetSegmentIndices(List<Joint> joints, double startLength, double endLength)
	{
		int startIdx = joints.BinarySearch(new Joint { Length = startLength }, Comparer<Joint>.Create((a, b) => a.Length.CompareTo(b.Length)));
		startIdx = startIdx < 0 ? ~startIdx : startIdx + 1;

		int endIdx = joints.BinarySearch(new Joint { Length = endLength }, Comparer<Joint>.Create((a, b) => a.Length.CompareTo(b.Length)));
		endIdx = endIdx < 0 ? ~endIdx : endIdx;

		return (startIdx, endIdx - 1);
	}

	/// <summary>
	/// Aligns two segments of joints using dynamic programming.
	/// </summary>
	/// <param name="baseJoints">The base segment of joints.</param>
	/// <param name="baseStart">The starting index of the base segment.</param>
	/// <param name="baseEnd">The ending index of the base segment.</param>
	/// <param name="targetJoints">The target segment of joints.</param>
	/// <param name="targetStart">The starting index of the target segment.</param>
	/// <param name="targetEnd">The ending index of the target segment.</param>
	/// <param name="tolerance">The tolerance for aligning joints.</param>
	/// <returns>The list of aligned joints.</returns>
	private static List<JointMatchResult> AlignSegments(
		List<Joint> baseJoints, int baseStart, int baseEnd,
		List<Joint> targetJoints, int targetStart, int targetEnd,
		double tolerance)
	{
		int m = baseEnd - baseStart + 1;
		int n = targetEnd - targetStart + 1;
		if (m == 0 || n == 0) return [];

		var dp = new Cell[m + 1][];
		for (int i = 0; i <= m; i++)
			dp[i] = new Cell[n + 1];

		var baseLengths = baseJoints.Skip(baseStart).Take(m).Select(j => j.Length).ToArray();
		var targetLengths = targetJoints.Skip(targetStart).Take(n).Select(j => j.Length).ToArray();

		for (int i = 1; i <= m; i++)
		{
			for (int j = 1; j <= n; j++)
			{
				double diff = Math.Abs(baseLengths[i - 1] - targetLengths[j - 1]);
				Cell currentBest = new(0, 0.0, 0);

				if (diff <= tolerance)
				{
					Cell diagonal = dp[i - 1][j - 1];
					int newScore = diagonal.Score + 1;
					double newQuality = diagonal.Quality + (tolerance - diff);
					if (newScore > currentBest.Score || (newScore == currentBest.Score && newQuality > currentBest.Quality))
						currentBest = new Cell(newScore, newQuality, 1);
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

		var matches = new List<JointMatchResult>();
		int x = m, y = n;
		while (x > 0 && y > 0)
		{
			switch (dp[x][y].Direction)
			{
				case 1:
					matches.Add(new JointMatchResult
					{
						BaseIndex = baseJoints[baseStart + x - 1].OriginalIndex,
						TargetIndex = targetJoints[targetStart + y - 1].OriginalIndex
					});
					x--; y--;
					break;

				case 2: x--; break;
				case 3: y--; break;
			}
		}
		matches.Reverse();
		return matches;
	}
}