namespace VisualPinball.Unity
{
	public class PinMameRom
	{
		public readonly string Id;
		public readonly string Version;
		public readonly string Description;
		public readonly PinMameRomLanguage Language;

		public PinMameRom(string id, string version = null, string description = null, PinMameRomLanguage language = PinMameRomLanguage.English)
		{
			Id = id;
			Version = version;
			Description = description;
			Language = language;
		}
	}

	public enum PinMameRomLanguage {
		English, French, German
	}
}
