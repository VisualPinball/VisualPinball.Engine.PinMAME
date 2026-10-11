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
		// Bonus when a candidate's name equals the target's label, e.g. the "Gun Home" item of a mech
		// whose items all sit at the same position. Larger than MinimumMargin, so it settles such ties.
		private const double LabelMatchBonus = 0.02;

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
					var cost = usableAnchors == 0
							? distance
							: AbsoluteWeight * distance + RelationWeight * relationError;
					if (LabelsMatch(targets[targetIndex].Label, candidates[candidateIndex].Label)) {
						cost -= LabelMatchBonus;
					}
					costs[targetIndex, candidateIndex] = cost;
				}
				for (var dummyIndex = candidates.Count; dummyIndex < columnCount; dummyIndex++) {
					costs[targetIndex, dummyIndex] = UnassignedCost;
				}
			}

			var assignment = AssignStable(costs, candidates.Count);
			var owners = Enumerable.Repeat(-1, candidates.Count).ToArray();
			for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++) {
				if (assignment[targetIndex] >= 0 && assignment[targetIndex] < candidates.Count) owners[assignment[targetIndex]] = targetIndex;
			}
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
					decisions[targetIndex] = PinMameSpatialMatchDecision.Rejected(targets[targetIndex], "The closest playfield item is too far from the curated position.", distance, score, candidates[candidateIndex]);
					continue;
				}

				// Ambiguous when another item fits this target about as well, and it's either free or its
				// owner would fit this target's item about as well too, so the two could swap. A neighbour
				// that clearly fits its own item better (e.g. the next insert of a row) doesn't count.
				// A target pushed off its own best item by another one isn't trusted either.
				var ambiguous = UnassignedCost - score < MinimumMargin;
				for (var otherCandidateIndex = 0; !ambiguous && otherCandidateIndex < candidates.Count; otherCandidateIndex++) {
					if (otherCandidateIndex == candidateIndex || costs[targetIndex, otherCandidateIndex] >= ImpossibleCost) continue;
					var otherCost = costs[targetIndex, otherCandidateIndex];
					if (otherCost < score) {
						ambiguous = true;
						break;
					}
					if (otherCost - score >= MinimumMargin) continue;
					var owner = owners[otherCandidateIndex];
					ambiguous = owner < 0 || costs[owner, candidateIndex] - costs[owner, otherCandidateIndex] < MinimumMargin;
				}
				if (ambiguous) {
					decisions[targetIndex] = PinMameSpatialMatchDecision.Rejected(targets[targetIndex], "Two playfield items are equally plausible.", distance, score, candidates[candidateIndex]);
					continue;
				}

				if (relationAnchors.Count > 0 && relationError > MaximumRelationError) {
					decisions[targetIndex] = PinMameSpatialMatchDecision.Rejected(targets[targetIndex], "The item is nearby, but its position relative to trusted matches is inconsistent.", distance, score, candidates[candidateIndex]);
					continue;
				}

				// Contested when another target would rather have this item and fits it about as well or
				// better. One that merely wants it (e.g. because it got nothing) but fits it clearly worse
				// doesn't make this match doubtful.
				var contested = false;
				for (var otherTargetIndex = 0; otherTargetIndex < targets.Count; otherTargetIndex++) {
					if (otherTargetIndex == targetIndex) continue;
					var otherAssignedColumn = assignment[otherTargetIndex];
					var otherAssignedCost = otherAssignedColumn >= 0 ? costs[otherTargetIndex, otherAssignedColumn] : UnassignedCost;
					var otherCost = costs[otherTargetIndex, candidateIndex];
					if (otherCost + MinimumMargin < otherAssignedCost && otherCost < score + MinimumMargin) {
						contested = true;
						break;
					}
				}
				if (contested) {
					decisions[targetIndex] = PinMameSpatialMatchDecision.Rejected(targets[targetIndex], "Another machine item is a better fit for this playfield item.", distance, score, candidates[candidateIndex]);
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

		private static bool LabelsMatch(string targetLabel, string candidateLabel)
		{
			if (string.IsNullOrWhiteSpace(targetLabel) || string.IsNullOrWhiteSpace(candidateLabel)) return false;
			return string.Equals(NormalizeLabel(targetLabel), NormalizeLabel(candidateLabel), StringComparison.Ordinal);
		}

		private static string NormalizeLabel(string label) => new string(label.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

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

		/// <summary>
		/// Pairs targets and items in order of increasing cost, so each item goes to the target that
		/// fits it best. Unlike a minimum-cost assignment, a target without a matching item on the
		/// table (it isn't modelled) doesn't take a neighbour's item and push that one away.
		/// </summary>
		private static int[] AssignStable(double[,] costs, int candidateCount)
		{
			var targetCount = costs.GetLength(0);
			var pairs = new List<(double Cost, int Target, int Candidate)>();
			for (var targetIndex = 0; targetIndex < targetCount; targetIndex++) {
				for (var candidateIndex = 0; candidateIndex < candidateCount; candidateIndex++) {
					if (costs[targetIndex, candidateIndex] < UnassignedCost) {
						pairs.Add((costs[targetIndex, candidateIndex], targetIndex, candidateIndex));
					}
				}
			}
			pairs.Sort((a, b) => a.Cost != b.Cost ? a.Cost.CompareTo(b.Cost)
				: a.Target != b.Target ? a.Target.CompareTo(b.Target) : a.Candidate.CompareTo(b.Candidate));

			var assignment = Enumerable.Repeat(-1, targetCount).ToArray();
			var taken = new bool[candidateCount];
			foreach (var (_, targetIndex, candidateIndex) in pairs) {
				if (assignment[targetIndex] >= 0 || taken[candidateIndex]) continue;
				assignment[targetIndex] = candidateIndex;
				taken[candidateIndex] = true;
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
		internal string Label { get; }

		internal PinMameSpatialMatchTarget(string key, float x, float y, string label = null)
		{
			Key = key;
			X = x;
			Y = y;
			Label = label;
		}
	}

	internal sealed class PinMameSpatialMatchCandidate
	{
		internal string Key { get; }
		internal float X { get; }
		internal float Y { get; }
		internal string Label { get; }

		internal PinMameSpatialMatchCandidate(string key, float x, float y, string label = null)
		{
			Key = key;
			X = x;
			Y = y;
			Label = label;
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
		/// <summary>
		/// The item the target got in the best overall assignment, also when that wasn't trusted.
		/// </summary>
		internal PinMameSpatialMatchCandidate Assigned { get; }

		private PinMameSpatialMatchDecision(PinMameSpatialMatchTarget target, PinMameSpatialMatchCandidate candidate,
			string reason, double distance, double score, PinMameSpatialMatchCandidate assigned)
		{
			Target = target;
			Candidate = candidate;
			Reason = reason;
			Distance = distance;
			Score = score;
			Assigned = assigned;
		}

		internal static PinMameSpatialMatchDecision Accepted(PinMameSpatialMatchTarget target,
			PinMameSpatialMatchCandidate candidate, double distance, double score)
			=> new(target, candidate, null, distance, score, candidate);

		internal static PinMameSpatialMatchDecision Rejected(PinMameSpatialMatchTarget target, string reason,
			double distance = double.NaN, double score = double.NaN, PinMameSpatialMatchCandidate assigned = null)
			=> new(target, null, reason, distance, score, assigned);
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
