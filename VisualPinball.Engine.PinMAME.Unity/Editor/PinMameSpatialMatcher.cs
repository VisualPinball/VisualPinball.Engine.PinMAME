// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using System.Linq;

namespace VisualPinball.Engine.PinMAME.Editor
{
	internal static class PinMameSpatialMatcher
	{
		private const double MaximumDistance = 0.15;
		private const double UnassignedCost = 0.18;
		private const double MinimumMargin = 0.012;
		private const double MaximumRelationError = 0.12;
		private const double SeedMaximumDistance = 0.09;
		private const double SeedMinimumMargin = 0.02;
		private const double AbsoluteWeight = 0.65;
		private const double RelationWeight = 0.35;
		private const double ImpossibleCost = 1000.0;

		internal static PinMameSpatialMatchResult Match(IReadOnlyList<PinMameSpatialMatchTarget> targets,
			IReadOnlyList<PinMameSpatialMatchCandidate> candidates, IReadOnlyList<PinMameSpatialMatchAnchor> anchors = null)
		{
			if (targets == null) throw new ArgumentNullException(nameof(targets));
			if (candidates == null) throw new ArgumentNullException(nameof(candidates));
			if (targets.Count == 0) return new PinMameSpatialMatchResult(Array.Empty<PinMameSpatialMatchDecision>());

			var relationAnchors = new List<PinMameSpatialMatchAnchor>(anchors ?? Array.Empty<PinMameSpatialMatchAnchor>());
			relationAnchors.AddRange(FindStableSeeds(targets, candidates));
			var columnCount = candidates.Count + targets.Count;
			var costs = new double[targets.Count, columnCount];
			var distances = new double[targets.Count, candidates.Count];
			var relationErrors = new double[targets.Count, candidates.Count];
			var relationScratch = new double[relationAnchors.Count];

			for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++) {
				for (var candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++) {
					var distance = Distance(targets[targetIndex].X, targets[targetIndex].Y,
						candidates[candidateIndex].X, candidates[candidateIndex].Y);
					distances[targetIndex, candidateIndex] = distance;
					if (distance > MaximumDistance) {
						relationErrors[targetIndex, candidateIndex] = 0.0;
						costs[targetIndex, candidateIndex] = ImpossibleCost;
						continue;
					}
					var relationError = RelationError(targets[targetIndex], candidates[candidateIndex], relationAnchors,
						relationScratch, out var usableAnchors);
					relationErrors[targetIndex, candidateIndex] = relationError;
					costs[targetIndex, candidateIndex] = usableAnchors == 0
							? distance
							: AbsoluteWeight * distance + RelationWeight * relationError;
				}
				for (var dummyIndex = candidates.Count; dummyIndex < columnCount; dummyIndex++) {
					costs[targetIndex, dummyIndex] = UnassignedCost;
				}
			}

			var assignment = Assign(costs);
			var decisions = new PinMameSpatialMatchDecision[targets.Count];
			for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++) {
				var candidateIndex = assignment[targetIndex];
				if (candidateIndex < 0 || candidateIndex >= candidates.Count) {
					decisions[targetIndex] = PinMameSpatialMatchDecision.Rejected(targets[targetIndex], "No unassigned playfield item is close enough.");
					continue;
				}

				var score = costs[targetIndex, candidateIndex];
				var distance = distances[targetIndex, candidateIndex];
				var relationError = relationErrors[targetIndex, candidateIndex];
				if (distance > MaximumDistance || score >= UnassignedCost) {
					decisions[targetIndex] = PinMameSpatialMatchDecision.Rejected(targets[targetIndex], "The closest playfield item is too far from the curated position.", distance, score);
					continue;
				}

				var alternative = UnassignedCost;
				for (var otherCandidateIndex = 0; otherCandidateIndex < candidates.Count; otherCandidateIndex++) {
					if (otherCandidateIndex == candidateIndex) continue;
					alternative = System.Math.Min(alternative, costs[targetIndex, otherCandidateIndex]);
				}
				if (alternative - score < MinimumMargin) {
					decisions[targetIndex] = PinMameSpatialMatchDecision.Rejected(targets[targetIndex], "Two playfield items are equally plausible.", distance, score);
					continue;
				}

				if (relationAnchors.Count > 0 && relationError > MaximumRelationError) {
					decisions[targetIndex] = PinMameSpatialMatchDecision.Rejected(targets[targetIndex], "The item is nearby, but its position relative to trusted matches is inconsistent.", distance, score);
					continue;
				}

				var contested = false;
				for (var otherTargetIndex = 0; otherTargetIndex < targets.Count; otherTargetIndex++) {
					if (otherTargetIndex == targetIndex) continue;
					var otherAssignedColumn = assignment[otherTargetIndex];
					var otherAssignedCost = otherAssignedColumn >= 0 ? costs[otherTargetIndex, otherAssignedColumn] : UnassignedCost;
					if (costs[otherTargetIndex, candidateIndex] + MinimumMargin < otherAssignedCost) {
						contested = true;
						break;
					}
				}
				if (contested) {
					decisions[targetIndex] = PinMameSpatialMatchDecision.Rejected(targets[targetIndex], "Another machine item is a better fit for this playfield item.", distance, score);
					continue;
				}

				decisions[targetIndex] = PinMameSpatialMatchDecision.Accepted(targets[targetIndex], candidates[candidateIndex], distance, score);
			}
			return new PinMameSpatialMatchResult(decisions);
		}

		private static IEnumerable<PinMameSpatialMatchAnchor> FindStableSeeds(IReadOnlyList<PinMameSpatialMatchTarget> targets,
			IReadOnlyList<PinMameSpatialMatchCandidate> candidates)
		{
			if (targets.Count < 2 || candidates.Count < 2) yield break;
			for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++) {
				var nearestCandidate = -1;
				var best = double.MaxValue;
				var second = double.MaxValue;
				for (var candidateIndex = 0; candidateIndex < candidates.Count; candidateIndex++) {
					var distance = Distance(targets[targetIndex].X, targets[targetIndex].Y, candidates[candidateIndex].X, candidates[candidateIndex].Y);
					if (distance < best) {
						second = best;
						best = distance;
						nearestCandidate = candidateIndex;
					} else if (distance < second) {
						second = distance;
					}
				}
				if (nearestCandidate < 0 || best > SeedMaximumDistance || second - best < SeedMinimumMargin) continue;

				var nearestTarget = -1;
				var candidateBest = double.MaxValue;
				var candidateSecond = double.MaxValue;
				for (var otherTargetIndex = 0; otherTargetIndex < targets.Count; otherTargetIndex++) {
					var distance = Distance(targets[otherTargetIndex].X, targets[otherTargetIndex].Y,
						candidates[nearestCandidate].X, candidates[nearestCandidate].Y);
					if (distance < candidateBest) {
						candidateSecond = candidateBest;
						candidateBest = distance;
						nearestTarget = otherTargetIndex;
					} else if (distance < candidateSecond) {
						candidateSecond = distance;
					}
				}
				if (nearestTarget != targetIndex || candidateSecond - candidateBest < SeedMinimumMargin) continue;
				yield return new PinMameSpatialMatchAnchor(targets[targetIndex].X, targets[targetIndex].Y,
					candidates[nearestCandidate].X, candidates[nearestCandidate].Y, targets[targetIndex].Key);
			}
		}

		private static double RelationError(PinMameSpatialMatchTarget target, PinMameSpatialMatchCandidate candidate,
			IReadOnlyList<PinMameSpatialMatchAnchor> anchors, double[] scratch, out int usableAnchors)
		{
			usableAnchors = 0;
			if (anchors.Count == 0) return 0.0;
			var count = 0;
			for (var index = 0; index < anchors.Count; index++) {
				var anchor = anchors[index];
				if (string.Equals(anchor.TargetKey, target.Key, StringComparison.Ordinal)) continue;
				scratch[count++] = Distance(candidate.X - anchor.CandidateX, candidate.Y - anchor.CandidateY,
					target.X - anchor.TargetX, target.Y - anchor.TargetY);
			}
			usableAnchors = count;
			if (count == 0) return 0.0;
			Array.Sort(scratch, 0, count);
			return scratch[count / 2];
		}

		private static int[] Assign(double[,] costs)
		{
			var rowCount = costs.GetLength(0);
			var columnCount = costs.GetLength(1);
			var rowPotential = new double[rowCount + 1];
			var columnPotential = new double[columnCount + 1];
			var matching = new int[columnCount + 1];
			var path = new int[columnCount + 1];
			var minimum = new double[columnCount + 1];
			var used = new bool[columnCount + 1];
			for (var row = 1; row <= rowCount; row++) {
				matching[0] = row;
				var column0 = 0;
				Array.Fill(minimum, double.PositiveInfinity);
				Array.Clear(used, 0, used.Length);
				do {
					used[column0] = true;
					var currentRow = matching[column0];
					var delta = double.PositiveInfinity;
					var column1 = 0;
					for (var column = 1; column <= columnCount; column++) {
						if (used[column]) continue;
						var current = costs[currentRow - 1, column - 1] - rowPotential[currentRow] - columnPotential[column];
						if (current < minimum[column]) {
							minimum[column] = current;
							path[column] = column0;
						}
						if (minimum[column] < delta) {
							delta = minimum[column];
							column1 = column;
						}
					}
					for (var column = 0; column <= columnCount; column++) {
						if (used[column]) {
							rowPotential[matching[column]] += delta;
							columnPotential[column] -= delta;
						} else {
							minimum[column] -= delta;
						}
					}
					column0 = column1;
				} while (matching[column0] != 0);
				do {
					var column1 = path[column0];
					matching[column0] = matching[column1];
					column0 = column1;
				} while (column0 != 0);
			}

			var assignment = Enumerable.Repeat(-1, rowCount).ToArray();
			for (var column = 1; column <= columnCount; column++) {
				if (matching[column] > 0) assignment[matching[column] - 1] = column - 1;
			}
			return assignment;
		}

		private static double Distance(double x1, double y1, double x2, double y2)
		{
			var x = x1 - x2;
			var y = y1 - y2;
			return System.Math.Sqrt(x * x + y * y);
		}
	}

	internal sealed class PinMameSpatialMatchTarget
	{
		internal string Key { get; }
		internal float X { get; }
		internal float Y { get; }

		internal PinMameSpatialMatchTarget(string key, float x, float y)
		{
			Key = key;
			X = x;
			Y = y;
		}
	}

	internal sealed class PinMameSpatialMatchCandidate
	{
		internal string Key { get; }
		internal float X { get; }
		internal float Y { get; }

		internal PinMameSpatialMatchCandidate(string key, float x, float y)
		{
			Key = key;
			X = x;
			Y = y;
		}
	}

	internal sealed class PinMameSpatialMatchAnchor
	{
		internal float TargetX { get; }
		internal float TargetY { get; }
		internal float CandidateX { get; }
		internal float CandidateY { get; }
		internal string TargetKey { get; }

		internal PinMameSpatialMatchAnchor(float targetX, float targetY, float candidateX, float candidateY,
			string targetKey = null)
		{
			TargetX = targetX;
			TargetY = targetY;
			CandidateX = candidateX;
			CandidateY = candidateY;
			TargetKey = targetKey;
		}
	}

	internal sealed class PinMameSpatialMatchDecision
	{
		internal PinMameSpatialMatchTarget Target { get; }
		internal PinMameSpatialMatchCandidate Candidate { get; }
		internal bool IsAccepted => Candidate != null;
		internal string Reason { get; }
		internal double Distance { get; }
		internal double Score { get; }

		private PinMameSpatialMatchDecision(PinMameSpatialMatchTarget target, PinMameSpatialMatchCandidate candidate,
			string reason, double distance, double score)
		{
			Target = target;
			Candidate = candidate;
			Reason = reason;
			Distance = distance;
			Score = score;
		}

		internal static PinMameSpatialMatchDecision Accepted(PinMameSpatialMatchTarget target,
			PinMameSpatialMatchCandidate candidate, double distance, double score)
			=> new(target, candidate, null, distance, score);

		internal static PinMameSpatialMatchDecision Rejected(PinMameSpatialMatchTarget target, string reason,
			double distance = double.NaN, double score = double.NaN)
			=> new(target, null, reason, distance, score);
	}

	internal sealed class PinMameSpatialMatchResult
	{
		internal PinMameSpatialMatchDecision[] Decisions { get; }

		internal PinMameSpatialMatchResult(PinMameSpatialMatchDecision[] decisions)
		{
			Decisions = decisions;
		}
	}
}
