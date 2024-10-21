using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WallpaperController {
    internal class WallpaperSettings {
        [JsonRequired]
        public IList<WallpaperPresetBase> Presets { get; set; }
    }
}
