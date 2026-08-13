// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// ReSharper disable CheckNamespace

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace VisualPinball.Engine.PinMAME.Editor
{
	internal static class PinMameGameDefinitionCatalogClient
	{
		internal const string BaseUrl = "https://games.visualpinball.org/";
		private static readonly TimeSpan FreshCacheAge = TimeSpan.FromMinutes(10);
		private static readonly Regex SafeSlug = new("^[a-z0-9-]+$", RegexOptions.CultureInvariant);
		private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
		private static readonly ConcurrentDictionary<string, SemaphoreSlim> CacheLocks = new(StringComparer.OrdinalIgnoreCase);
		private static readonly object CacheRootLock = new();
		private static string _cacheRoot;

		internal static async Task<PinMameCatalogResult<PinMameCatalog>> GetCatalogAsync(bool forceRefresh, CancellationToken cancellationToken, bool cacheOnly = false)
		{
			var cachePath = CachePath("index.json");
			var result = await GetJsonAsync("data/index.json", cachePath, forceRefresh, cancellationToken, cacheOnly).ConfigureAwait(false);
			PinMameCatalog catalog;
			try {
				catalog = ParseCatalog(result.Value);
			} catch (Exception exception) when ((exception is JsonException || exception is InvalidDataException) && result.FromCache && !cacheOnly) {
				result = await GetJsonAsync("data/index.json", cachePath, true, cancellationToken, cacheOnly, true).ConfigureAwait(false);
				catalog = ParseCatalog(result.Value);
			}
			if (catalog.Version == 1) {
				var driverResult = await GetJsonAsync("data/drivers.json", CachePath("drivers.json"), forceRefresh, cancellationToken, cacheOnly).ConfigureAwait(false);
				PinMameCatalogDriver[] drivers;
				try {
					drivers = JsonConvert.DeserializeObject<PinMameCatalogDriver[]>(driverResult.Value) ?? Array.Empty<PinMameCatalogDriver>();
				} catch (JsonException) when (driverResult.FromCache && !cacheOnly) {
					driverResult = await GetJsonAsync("data/drivers.json", CachePath("drivers.json"), true, cancellationToken, false, true).ConfigureAwait(false);
					drivers = JsonConvert.DeserializeObject<PinMameCatalogDriver[]>(driverResult.Value) ?? Array.Empty<PinMameCatalogDriver>();
				}
				UpgradeLegacyCatalog(catalog, drivers);
				var warning = CombineWarnings("The machine-definition site is still serving catalog version 1; ROMs were joined from the driver index for compatibility.", CombineWarnings(result.Warning, driverResult.Warning));
				return new PinMameCatalogResult<PinMameCatalog>(catalog, result.FromCache && driverResult.FromCache, warning);
			}
			return new PinMameCatalogResult<PinMameCatalog>(catalog, result.FromCache, result.Warning);
		}

		internal static async Task<PinMameCatalogResult<string>> GetDefinitionAsync(PinMameCatalogMachine machine, bool forceRefresh, CancellationToken cancellationToken, bool cacheOnly = false)
		{
			if (machine == null || string.IsNullOrWhiteSpace(machine.Detail)) {
				throw new InvalidOperationException("The selected machine has no published detail definition.");
			}
			if (!SafeSlug.IsMatch(machine.Slug ?? string.Empty)) {
				throw new InvalidDataException($"The catalog returned an unsafe machine slug: {machine.Slug}.");
			}
			var expectedDetail = $"data/machines/{machine.Slug}.json";
			if (!string.Equals(machine.Detail, expectedDetail, StringComparison.Ordinal)) {
				throw new InvalidDataException($"The catalog returned an unexpected detail path for {machine.Name}: {machine.Detail}.");
			}

			var cachePath = CachePath($"machines-{machine.Slug}.json");
			var result = await GetJsonAsync(expectedDetail, cachePath, forceRefresh, cancellationToken, cacheOnly).ConfigureAwait(false);
			try {
				ValidateDefinitionJson(result.Value, machine.Name);
			} catch (Exception exception) when ((exception is JsonException || exception is InvalidDataException) && result.FromCache && !cacheOnly) {
				result = await GetJsonAsync(expectedDetail, cachePath, true, cancellationToken, false, true).ConfigureAwait(false);
				ValidateDefinitionJson(result.Value, machine.Name);
			}
			return result;
		}

		internal static async Task<string> ResolveMachineIdAsync(string romId, CancellationToken cancellationToken)
		{
			if (string.IsNullOrWhiteSpace(romId)) {
				return null;
			}
			var cachePath = CachePath("drivers.json");
			var result = await GetJsonAsync("data/drivers.json", cachePath, false, cancellationToken).ConfigureAwait(false);
			PinMameCatalogDriver[] drivers;
			try {
				drivers = JsonConvert.DeserializeObject<PinMameCatalogDriver[]>(result.Value) ?? Array.Empty<PinMameCatalogDriver>();
			} catch (JsonException) when (result.FromCache) {
				result = await GetJsonAsync("data/drivers.json", cachePath, true, cancellationToken, false, true).ConfigureAwait(false);
				drivers = JsonConvert.DeserializeObject<PinMameCatalogDriver[]>(result.Value) ?? Array.Empty<PinMameCatalogDriver>();
			}
			return drivers.FirstOrDefault(driver => string.Equals(driver.Id, romId, StringComparison.OrdinalIgnoreCase))?.MachineId;
		}

		private static void ValidateDefinitionJson(string json, string machineName)
		{
			var root = JObject.Parse(json);
			if (!string.Equals(root.Value<string>("format"), "pinmame-machine-definition", StringComparison.Ordinal)) {
				throw new InvalidDataException($"The detail endpoint for {machineName} did not return a PinMAME machine definition.");
			}
		}

		internal static PinMameCatalog ParseCatalog(string json)
		{
			var catalog = JsonConvert.DeserializeObject<PinMameCatalog>(json) ?? throw new InvalidDataException("The PinMAME machine catalog is empty.");
			if (!string.Equals(catalog.Format, "pinmame-machine-reference-index", StringComparison.Ordinal)) {
				throw new InvalidDataException($"The machine catalog has an unsupported format: {catalog.Format ?? "none"}.");
			}
			if (catalog.Version is < 1 or > 2) {
				throw new InvalidDataException($"The machine catalog has an unsupported version: {catalog.Version}.");
			}
			catalog.Machines ??= Array.Empty<PinMameCatalogMachine>();
			return catalog;
		}

		internal static void UpgradeLegacyCatalog(PinMameCatalog catalog, IEnumerable<PinMameCatalogDriver> drivers)
		{
			if (catalog == null) throw new ArgumentNullException(nameof(catalog));
			var romsByMachine = (drivers ?? Enumerable.Empty<PinMameCatalogDriver>())
				.Where(driver => !string.IsNullOrWhiteSpace(driver.Id) && !string.IsNullOrWhiteSpace(driver.MachineId))
				.GroupBy(driver => driver.MachineId, StringComparer.Ordinal)
				.ToDictionary(group => group.Key, group => group.Select(driver => driver.Id).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToArray(), StringComparer.Ordinal);
			foreach (var machine in catalog.Machines ?? Array.Empty<PinMameCatalogMachine>()) {
				if (romsByMachine.TryGetValue(machine.Id ?? string.Empty, out var roms)) machine.Roms = roms;
				else machine.Roms ??= machine.RootDrivers ?? Array.Empty<string>();
				machine.MachineKind ??= (machine.Id ?? string.Empty).StartsWith("diagnostic.", StringComparison.Ordinal)
					? "diagnostic_software"
					: "unknown";
			}
		}

		private static async Task<PinMameCatalogResult<string>> GetJsonAsync(string relativeUrl, string cachePath, bool forceRefresh,
			CancellationToken cancellationToken, bool cacheOnly = false, bool invalidateCache = false)
		{
			var cacheLock = CacheLocks.GetOrAdd(cachePath, _ => new SemaphoreSlim(1, 1));
			await cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
			try {
				if (invalidateCache) {
					DeleteCacheEntry(cachePath);
				}
				if (cacheOnly) {
					if (!File.Exists(cachePath)) throw new FileNotFoundException($"No cached PinMAME definition is available at {cachePath}.", cachePath);
					return new PinMameCatalogResult<string>(File.ReadAllText(cachePath), true, null);
				}
				if (!forceRefresh && IsFresh(cachePath)) {
					try {
						return new PinMameCatalogResult<string>(File.ReadAllText(cachePath), true, null);
					} catch (IOException) {
						// The cache changed between the freshness check and read; fetch a replacement.
					}
				}

				using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(new Uri(BaseUrl), relativeUrl));
				var etagPath = cachePath + ".etag";
				if (File.Exists(cachePath) && File.Exists(etagPath)) {
					var etag = File.ReadAllText(etagPath).Trim();
					if (EntityTagHeaderValue.TryParse(etag, out var entityTag)) {
						request.Headers.IfNoneMatch.Add(entityTag);
					}
				}
				if (forceRefresh) {
					request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
				}

				using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
				if (response.StatusCode == HttpStatusCode.NotModified && File.Exists(cachePath)) {
					File.SetLastWriteTimeUtc(cachePath, DateTime.UtcNow);
					return new PinMameCatalogResult<string>(File.ReadAllText(cachePath), true, null);
				}
				response.EnsureSuccessStatusCode();
				var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				JToken.Parse(json);
				WriteCache(cachePath, json);
				if (response.Headers.ETag != null) {
					File.WriteAllText(etagPath, response.Headers.ETag.ToString());
				} else if (File.Exists(etagPath)) {
					File.Delete(etagPath);
				}
				return new PinMameCatalogResult<string>(json, false, null);
			} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
				throw;
			} catch (OperationCanceledException exception) when (File.Exists(cachePath)) {
				return new PinMameCatalogResult<string>(File.ReadAllText(cachePath), true,
					$"The machine-definition request timed out; using the cached copy. {exception.Message}");
			} catch (OperationCanceledException exception) {
				throw new TimeoutException("The machine-definition request timed out and no cached copy is available.", exception);
			} catch (Exception exception) when (File.Exists(cachePath)) {
				return new PinMameCatalogResult<string>(File.ReadAllText(cachePath), true,
					$"Could not refresh the machine definitions; using the cached copy. {exception.Message}");
			} finally {
				cacheLock.Release();
			}
		}

		private static bool IsFresh(string path) => File.Exists(path) && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) < FreshCacheAge;

		internal static void EnsureCacheRoot()
		{
			if (_cacheRoot != null) return;
			lock (CacheRootLock) {
				if (_cacheRoot != null) return;
				var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
				_cacheRoot = Path.Combine(projectRoot, "Library", "PinMAME", "GameDefinitions");
				Directory.CreateDirectory(_cacheRoot);
			}
		}

		private static string CachePath(string fileName)
		{
			EnsureCacheRoot();
			return Path.Combine(_cacheRoot, fileName);
		}

		private static string CombineWarnings(string first, string second)
		{
			if (string.IsNullOrWhiteSpace(first)) return second;
			if (string.IsNullOrWhiteSpace(second) || string.Equals(first, second, StringComparison.Ordinal)) return first;
			return $"{first}\n{second}";
		}

		private static void WriteCache(string path, string content)
		{
			var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
			try {
				File.WriteAllText(temporaryPath, content);
				if (File.Exists(path)) {
					File.Delete(path);
				}
				File.Move(temporaryPath, path);
			} finally {
				if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
			}
		}

		private static void DeleteCacheEntry(string path)
		{
			if (File.Exists(path)) File.Delete(path);
			var etagPath = path + ".etag";
			if (File.Exists(etagPath)) File.Delete(etagPath);
		}
	}

	internal sealed class PinMameCatalog
	{
		[JsonProperty("format")]
		public string Format { get; set; }

		[JsonProperty("version")]
		public int Version { get; set; }

		[JsonProperty("machines")]
		public PinMameCatalogMachine[] Machines { get; set; }
	}

	internal sealed class PinMameCatalogMachine
	{
		private static readonly HashSet<string> NonPhysicalKinds = new(StringComparer.Ordinal) {
			"physical_conversion", "redemption_game", "diagnostic_software", "system_software",
		};

		[JsonProperty("slug")]
		public string Slug { get; set; }

		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("manufacturer")]
		public string Manufacturer { get; set; }

		[JsonProperty("year")]
		public int? Year { get; set; }

		[JsonProperty("status")]
		public string Status { get; set; }

		[JsonProperty("completionScore")]
		public int CompletionScore { get; set; }

		[JsonProperty("machineKind")]
		public string MachineKind { get; set; }

		[JsonProperty("roms")]
		public string[] Roms { get; set; }

		[JsonProperty("rootDrivers")]
		public string[] RootDrivers { get; set; }

		[JsonProperty("detail")]
		public string Detail { get; set; }

		public bool IsSelectable => !string.IsNullOrWhiteSpace(Detail)
			&& !string.Equals(Status, "stub", StringComparison.Ordinal)
			&& !NonPhysicalKinds.Contains(MachineKind ?? string.Empty);

		public string DisplayName {
			get {
				var identity = string.Join(" · ", new[] { Name, Manufacturer, Year?.ToString(CultureInfo.InvariantCulture) }.Where(value => !string.IsNullOrWhiteSpace(value)));
				return string.Equals(Status, "partial", StringComparison.Ordinal) ? $"{identity} — partial {CompletionScore}%" : identity;
			}
		}
	}

	internal sealed class PinMameCatalogDriver
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("machineId")]
		public string MachineId { get; set; }
	}

	internal readonly struct PinMameCatalogResult<T>
	{
		public T Value { get; }
		public bool FromCache { get; }
		public string Warning { get; }

		public PinMameCatalogResult(T value, bool fromCache, string warning)
		{
			Value = value;
			FromCache = fromCache;
			Warning = warning;
		}
	}
}
