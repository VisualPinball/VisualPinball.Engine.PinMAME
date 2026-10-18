// Visual Pinball Engine
// Copyright (C) 2026 freezy and VPE Team
//
// SPDX-License-Identifier: MIT
// Licensed under MIT. See https://opensource.org/license/mit.

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using VisualPinball.Plugins;
using VisualPinball.Unity;

namespace VisualPinball.Engine.PinMAME
{
	/// <summary>Configures the selected engine's audio; emulation remains owned by the table.</summary>
	internal sealed class PinMamePluginFactory : IPlayerPluginFactory
	{
		public PluginDescriptor Descriptor { get; } = new PluginDescriptor("org.visualpinball.pinmame", "PinMAME", "ROM audio for tables using the PinMAME engine.") { EnabledByDefault = true, CanDisable = false, Category = "Gamelogic", Order = 5 };
		public PluginSettingsSchema Schema { get; } = new PluginSettingsSchema(1, new PluginSettingsGroup("ROM audio", "Controls PinMAME playback without changing ROM settings.",
			new PluginField("Volume", "Volume", SettingKind.Integer) { Default = 100, Minimum = 0, Maximum = 100, Change = ConfigurationChange.Live,
				Presentation = PluginFieldPresentation.Slider, Unit = "%", GlobalSetting = new PluginGlobalSetting(PluginGlobalSections.VolumeMixer) { Order = 20 } }));
		public IPluginSettingsProvider SettingsProvider => null;
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
				if (_engine != null) { _original = _engine.OutputGain; Apply(game.Settings); }
				return Task.CompletedTask;
			}
			public PluginActionResult Apply(PluginSettingsSnapshot settings)
			{
				_application.Scheduler.VerifyOwner();
				if (_engine != null) _engine.OutputGain = Mathf.Clamp(settings.Integer("Volume", 100), 0, 100) / 100f;
				return new PluginActionResult(true, "");
			}
			// nothing to say for a table without PinMAME
			public void DescribeStatus(PluginStatusWriter writer)
			{
				if (_engine == null) return;
				writer.Add("ROM", string.IsNullOrEmpty(_engine.romId) ? "not set" : _engine.romId);
				writer.Add("Volume", Mathf.RoundToInt(_engine.OutputGain * 100f) + "%");
			}
			public Task<PluginActionResult> ApplyAsync(PluginSettingsSnapshot settings, CancellationToken cancellation) { cancellation.ThrowIfCancellationRequested(); return Task.FromResult(Apply(settings)); }
			public override Task StopGameAsync(CancellationToken cancellation) { EmergencyStop(); return Task.CompletedTask; }
			public void EmergencyStop() { if (_engine != null) _engine.OutputGain = _original; _engine = null; }
			public override void Dispose() => EmergencyStop();
		}
	}
}
