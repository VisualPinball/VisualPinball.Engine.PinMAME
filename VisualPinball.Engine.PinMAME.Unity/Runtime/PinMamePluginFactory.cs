// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// SPDX-License-Identifier: MIT
// Licensed under MIT. See https://opensource.org/license/mit.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VisualPinball.Plugins;
using VisualPinball.Unity;

namespace VisualPinball.Engine.PinMAME
{
	/// <summary>Configures the selected engine's audio; emulation remains owned by the table.</summary>
	internal sealed partial class PinMamePluginFactory : IPlayerPluginFactory, IPluginSystemDefaults, IPluginSettingsProvider, IPluginLaunchCheck
	{
		private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
		private const string ResetRomMemory = "reset-rom-memory";
		private const string CompatibleRomField = "CompatibleRom";
		private const string CompatibleRomsProvider = "compatible-roms";
		private const string CloseTableMessage = "Exit the running table before resetting ROM memory.";

		public PluginDescriptor Descriptor { get; } = new PluginDescriptor("org.visualpinball.pinmame", "PinMAME", "ROM audio and saved memory for tables using the PinMAME engine.") { EnabledByDefault = true, CanDisable = false, Category = "Gamelogic", Order = 5 };
		public PluginSettingsSchema Schema { get; } = new PluginSettingsSchema(1, new PluginSettingsGroup("ROM audio", "Controls PinMAME playback without changing ROM settings.",
			new PluginField("Volume", "Volume", SettingKind.Integer) { Default = 100, Minimum = 0, Maximum = 100, Change = ConfigurationChange.Live,
				Presentation = PluginFieldPresentation.Slider, Unit = "%", GlobalSetting = new PluginGlobalSetting(PluginGlobalSections.VolumeMixer) { Order = 20 } }),
			new PluginSettingsGroup("AltSound", "Plays an AltSound pack instead of the ROM's own sound: recorded music, callouts and effects that the ROM's sound commands trigger.",
				new PluginField("AltSound", "Use AltSound", SettingKind.Boolean) { Default = false, Scope = ConfigurationScope.GlobalAndGame,
					Description = "Plays the pack named after the table's ROM when there is one." },
				new PluginField("AltSoundFolder", "AltSound folder", SettingKind.Folder) { Default = "", FullWidth = true, VisibleWhen = "AltSound",
					Description = "The folder with a subfolder per ROM, such as VPinMAME's altsound folder. Empty looks next to VPinMAME's ROMs and in VPE's PinMAME folder." },
				new PluginField("AltSoundVolume", "AltSound volume", SettingKind.Integer) { Default = 100, Minimum = 0, Maximum = 200, Change = ConfigurationChange.Live,
					Presentation = PluginFieldPresentation.Slider, Unit = "%", Scope = ConfigurationScope.GlobalAndGame, VisibleWhen = "AltSound",
					Description = "The pack's level, at its own level at 100%. The ROM volume above doesn't change it." },
				new PluginField("AltSoundGameVolume", "Follow the game's volume", SettingKind.Boolean) { Default = false, Change = ConfigurationChange.Live,
					Scope = ConfigurationScope.GlobalAndGame, VisibleWhen = "AltSound",
					Description = "Lets the volume set in the game's own menu scale the pack too, as in Visual PinMAME. Packs are often much quieter then." },
				new PluginField("AltSoundRomVolume", "ROM sound under AltSound", SettingKind.Integer) { Default = 0, Minimum = 0, Maximum = 100, Change = ConfigurationChange.Live,
					Presentation = PluginFieldPresentation.Slider, Unit = "%", Scope = ConfigurationScope.GlobalAndGame, VisibleWhen = "AltSound",
					Description = "How much of the ROM's own sound still plays, for packs that don't replace all of it. Off, like Visual PinMAME." }),
			new PluginSettingsGroup("ROM", "",
				new PluginField(CompatibleRomField, "Compatible ROM", SettingKind.Choice) { Scope = ConfigurationScope.GameOnly, Provider = CompatibleRomsProvider, Default = "",
					Description = "The ROM to play when the one this table was made for isn't in your ROM folder. The table lists the ROMs that run the same machine." }),
			new PluginSettingsGroup("ROM memory", "Tables using the same ROM share its saved memory.",
				new PluginField("ResetRomMemory", "Back up and reset ROM memory", SettingKind.Action) { Scope = ConfigurationScope.GameOnly, Provider = ResetRomMemory,
					Description = "Takes effect immediately. The ROM starts fresh next time, clearing saved adjustments, audits and any scores stored in its memory. Exit the table in every VPE window first." }));
		public IPluginSettingsProvider SettingsProvider => this;

		public Task<PluginChoiceCatalog> DiscoverAsync(string provider, PluginChoiceRequest request, CancellationToken cancellation)
		{
			if (provider != CompatibleRomsProvider) {
				return Task.FromResult(new PluginChoiceCatalog(provider, Array.Empty<PluginChoice>()));
			}
			var package = request.Package?.Path;
			return Task.Run(() => CompatibleRomCatalog(package, cancellation), cancellation);
		}

		/// <summary>
		/// The compatible ROMs the table lists that are in the ROM folder, best match first.
		/// </summary>
		private static PluginChoiceCatalog CompatibleRomCatalog(string package, CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();
			// incomplete: a saved choice stays when its zip is gone or another table is selected
			if (string.IsNullOrWhiteSpace(package)) {
				return new PluginChoiceCatalog("", Array.Empty<PluginChoice>(), isComplete: false) { EmptyMessage = "Select a table to see its compatible ROMs." };
			}
			var roms = PinMameRomAvailability.Read(package, cancellation);
			if (roms == null || roms.Compatible.Count == 0) {
				return new PluginChoiceCatalog("", Array.Empty<PluginChoice>(), isComplete: false) {
					EmptyMessage = "This table lists no compatible ROMs. Export it again with a current VPE to add them.",
				};
			}
			var folders = PinMameRomAvailability.RomFolders;
			var choices = roms.Compatible
				.Where(rom => PinMameRomAvailability.IsAvailable(rom.Id, folders))
				.Select(rom => new PluginChoice(rom.Id, ChoiceLabel(rom), Explain(rom)))
				.ToArray();
			var listed = string.Join(", ", roms.Compatible.Select(rom => rom.Id));
			var folderList = folders.Replace(";", "\n");
			return new PluginChoiceCatalog(string.Join("|", choices.Select(choice => choice.Id)), choices, isComplete: false) {
				AvailableMessage = PinMameRomAvailability.IsAvailable(roms.Rom, folders)
					? $"This table was made for {roms.Rom}, which is in your ROM folder, so it plays with {roms.Rom}. The ROM chosen here plays when {roms.Rom} is missing."
					: $"This table was made for the ROM {roms.Rom}, which isn't in your ROM folder. Play it with one of these compatible ROMs you have, or put {roms.Rom}.zip into:\n{folderList}",
				EmptyMessage = $"None of the ROMs compatible with this table are in your ROM folder:\n{folderList}\n\nCompatible ROMs: {listed}",
			};
		}

		private static string ChoiceLabel(PinMameCompatibleRom rom)
		{
			var label = string.IsNullOrWhiteSpace(rom.Description) ? rom.Id : $"{rom.Description} · {rom.Id}";
			return rom.Compatibility == PinMameRomCompatibility.Unverified ? label + " (unverified)" : label;
		}

		private static string Explain(PinMameCompatibleRom rom) => rom.Compatibility switch {
			PinMameRomCompatibility.Identical => "Another revision of the same game, for the same machine.",
			PinMameRomCompatibility.Compatible => "Runs on the same machine, but may play differently, like a community fix or alternative firmware.",
			_ => "A ROM of the same machine whose compatibility hasn't been confirmed. Switches, lamps or coils may not all work.",
		};

		public bool AppliesTo(GameIdentity game) => game.Provider == "pinmame";

		/// <summary>
		/// Stops the launch when the table's ROM isn't there but a compatible one is, so the player
		/// asks which one to play. Without a compatible ROM it lets the launch go on, and the
		/// missing ROM is reported as before.
		/// </summary>
		public Task<IReadOnlyList<PluginValidationIssue>> CheckLaunchAsync(PluginLaunchContext context, CancellationToken cancellation)
		{
			var package = context.Package?.Path;
			var chosen = context.Settings.String(CompatibleRomField);
			var mobile = Application.isMobilePlatform;
			return Task.Run<IReadOnlyList<PluginValidationIssue>>(() => {
				cancellation.ThrowIfCancellationRequested();
				if (mobile || string.IsNullOrWhiteSpace(package)) return Array.Empty<PluginValidationIssue>();
				var roms = PinMameRomAvailability.Read(package, cancellation);
				if (roms == null || roms.Compatible.Count == 0) return Array.Empty<PluginValidationIssue>();
				var folders = PinMameRomAvailability.RomFolders;
				if (PinMameRomAvailability.IsAvailable(roms.Rom, folders)) return Array.Empty<PluginValidationIssue>();
				if (roms.Lists(chosen) && PinMameRomAvailability.IsAvailable(chosen, folders)) return Array.Empty<PluginValidationIssue>();
				if (!roms.Compatible.Any(rom => PinMameRomAvailability.IsAvailable(rom.Id, folders))) return Array.Empty<PluginValidationIssue>();
				return new[] { new PluginValidationIssue(CompatibleRomField, $"This table was made for the ROM {roms.Rom}, which isn't in your ROM folder. Choose a compatible ROM to play it with.") };
			}, cancellation);
		}

		/// <summary>
		/// The ROM to play in place of the table's own, or null to play the table's own.
		/// </summary>
		private static string SubstituteRom(PinMameGamelogicEngine engine, string chosen)
		{
			if (engine == null || string.IsNullOrWhiteSpace(chosen) || string.IsNullOrWhiteSpace(engine.romId)) return null;
			return SubstituteRom(engine.romId, chosen, rom => engine.CompatibleRoms.Any(compatible => string.Equals(compatible.Id, rom, StringComparison.OrdinalIgnoreCase)));
		}

		private static string SubstituteRom(string tableRom, string chosen, Func<string, bool> lists)
		{
			if (string.IsNullOrWhiteSpace(tableRom) || string.IsNullOrWhiteSpace(chosen) || !lists(chosen)) return null;
			var folders = PinMameRomAvailability.RomFolders;
			return !PinMameRomAvailability.IsAvailable(tableRom, folders) && PinMameRomAvailability.IsAvailable(chosen, folders) ? chosen : null;
		}

		/// <summary>
		/// The ROM the selected table plays: the compatible one chosen for it when its own is missing, else its own.
		/// </summary>
		private static string PlayedRom(string tableRom, string chosen, string package, CancellationToken cancellation)
		{
			if (string.IsNullOrWhiteSpace(chosen) || string.IsNullOrWhiteSpace(package)) return tableRom;
			var roms = PinMameRomAvailability.Read(package, cancellation);
			if (roms == null || !string.Equals(roms.Rom, tableRom, StringComparison.OrdinalIgnoreCase)) return tableRom;
			return SubstituteRom(tableRom, chosen, roms.Lists) ?? tableRom;
		}

		public IReadOnlyList<PluginValidationIssue> Validate(PluginChoiceRequest request) => Array.Empty<PluginValidationIssue>();

		public async Task<PluginActionResult> RunActionAsync(string action, PluginChoiceRequest request, CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();
			if (action != ResetRomMemory || Application.isMobilePlatform) {
				return new PluginActionResult(false, "This action is unavailable.");
			}
			if (!(request.Game is GameIdentity game) || game.Provider != "pinmame") {
				return new PluginActionResult(false, "Select a table that uses PinMAME.");
			}
			// the memory of the ROM the table plays, which is the compatible one chosen when the table's own is missing
			var chosen = request.Settings.String(CompatibleRomField);
			var package = request.Package?.Path;
			var rom = await Task.Run(() => PlayedRom(game.Key, chosen, package, cancellation), cancellation);
			if (!PinMameGamelogicEngine.TryResetNvram(rom, out var backup)) {
				return new PluginActionResult(false, CloseTableMessage);
			}
			var message = $"{rom} will start with fresh ROM memory.";
			return new PluginActionResult(true, backup == null ? message : $"{message} Backup saved to {backup}");
		}

		public IPlayerPlugin Create(PluginApplicationContext context) => new SettingsAdapter(context);
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Register() => PlayerPluginRegistry.Register(new PinMamePluginFactory());
		private sealed class SettingsAdapter : PlayerPlugin, IPluginSynchronousReconfiguration, IPluginReconfiguration, IPluginEmergencyStop, IPluginStatusSource
		{
			private readonly PluginApplicationContext _application;
			private PinMameGamelogicEngine _engine;
			private float _original;
			internal SettingsAdapter(PluginApplicationContext application) => _application = application;
			public override Task PrepareGameAsync(PluginGameContext game, CancellationToken cancellation)
			{
				cancellation.ThrowIfCancellationRequested();
				var scene = game.Capabilities.Require<IUnityGameScene>(UnityPluginCapability.GameScene);
				_engine = scene.Table.GetComponentInChildren<PinMameGamelogicEngine>(true);
				if (_engine != null) {
					if (!Application.isMobilePlatform) {
						var substitute = SubstituteRom(_engine, game.Settings.String(CompatibleRomField));
						_engine.UseCompatibleRom(substitute);
						if (substitute != null) Logger.Info("[PinMAME] ROM {Rom} isn't in the ROM folder; playing {Substitute}, the compatible ROM chosen for it.", _engine.romId, substitute);
					}
					_original = _engine.OutputGain;
					_engine.ConfigureAltSound(game.Settings.Boolean("AltSound"), game.Settings.String("AltSoundFolder"));
					Apply(game.Settings);
				}
				return Task.CompletedTask;
			}
			public PluginActionResult Apply(PluginSettingsSnapshot settings)
			{
				_application.Scheduler.VerifyOwner();
				if (_engine != null) {
					_engine.OutputGain = Mathf.Clamp(settings.Integer("Volume", 100), 0, 100) / 100f;
					_engine.SetAltSoundLevels(Mathf.Clamp(settings.Integer("AltSoundVolume", 100), 0, 200) / 100f, Mathf.Clamp(settings.Integer("AltSoundRomVolume", 0), 0, 100) / 100f,
						settings.Boolean("AltSoundGameVolume"));
				}
				return new PluginActionResult(true, "");
			}
			// nothing to say for a table without PinMAME
			public void DescribeStatus(PluginStatusWriter writer)
			{
				if (_engine == null) return;
				writer.Add("ROM", string.IsNullOrEmpty(_engine.romId) ? "not set" : _engine.romId);
				if (_engine.RomSubstitute is { } substitute) writer.Add("Playing", substitute + ", a compatible ROM");
				writer.Add("Volume", Mathf.RoundToInt(_engine.OutputGain * 100f) + "%");
				writer.Add("AltSound", _engine.AltSoundStatus);
			}
			public Task<PluginActionResult> ApplyAsync(PluginSettingsSnapshot settings, CancellationToken cancellation) { cancellation.ThrowIfCancellationRequested(); return Task.FromResult(Apply(settings)); }
			public override Task StopGameAsync(CancellationToken cancellation) { EmergencyStop(); return Task.CompletedTask; }
			public void EmergencyStop() { if (_engine != null) _engine.OutputGain = _original; _engine = null; }
			public override void Dispose() => EmergencyStop();
		}
	}
}
