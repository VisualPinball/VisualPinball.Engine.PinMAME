// Visual Pinball Engine
// Copyright (C) 2021 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

namespace VisualPinball.Unity
{
	public class PinMameRom
	{
		public readonly string Id;
		private readonly string _version;
		private readonly string _description;
		private readonly PinMameRomLanguage _language;

		public PinMameRom(string id, string version = null, string description = null, PinMameRomLanguage language = PinMameRomLanguage.English)
		{
			Id = id;
			_version = version;
			_description = description;
			_language = language;
		}

		public override string ToString()
		{
			var id = string.IsNullOrEmpty(_version) ? Id : _version;
			if (!string.IsNullOrEmpty(_description)) {
				return _language != PinMameRomLanguage.English
					? $"{id} ({_description}) - {_language}"
					: $"{id} ({_description})";
			} else {
				return _language != PinMameRomLanguage.English
					? $"{id} - {_language}"
					: $"{id}";
			}
		}
	}

	public enum PinMameRomLanguage {
		English, French, German
	}
}
