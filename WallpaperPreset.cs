using System.Text.Json.Serialization;

namespace WallpaperController {
    internal enum DesktopWallpaperPosition {
        Center = 0,
        Tile = 1,
        Stretch = 2,
        Fit = 3,
        Fill = 4,
        Span = 5,
    }

    [JsonDerivedType(typeof(WallpaperPresetBackgroundColor), "BackgroundColor")]
    [JsonDerivedType(typeof(WallpaperPresetFile), "File")]
    [JsonDerivedType(typeof(WallpaperPresetPerMonitorFileList), "PerMonitorFileList")]
    //[JsonDerivedType(typeof(WallpaperPresetSlideshowFileList), "SlideshowFileList")]
    [JsonDerivedType(typeof(WallpaperPresetSlideshowDirectory), "SlideshowDirectory")]
    internal class WallpaperPresetBase {
        [JsonRequired]
        public string PresetName { get; set; }
        public string? BackgroundColor { get; set; }
        public DesktopWallpaperPosition? Position { get; set; }
        public string? LockScreenImage { get; set; }
    }

    internal class WallpaperPresetBackgroundColor : WallpaperPresetBase {
    }

    internal class WallpaperPresetFile : WallpaperPresetBase {
        [JsonRequired]
        public string FilePath { get; set; }
    }

    internal class WallpaperPresetPerMonitorFileList : WallpaperPresetBase {
        [JsonRequired]
        public string[] FilePaths { get; set; }
    }

    internal class WallpaperPresetSlideshowOptions {
        public bool ShuffleImages { get; set; }
        public uint SlideshowTick { get; set; }
    }

    internal class WallpaperPresetSlideshow : WallpaperPresetBase {
        public WallpaperPresetSlideshowOptions? SlideshowOptions { get; set; }
    }

    /*
    internal class WallpaperPresetSlideshowFileList : WallpaperPresetSlideshow {
        [JsonRequired]
        public string[] SlideshowFilePaths { get; set; }
    }
    */

    internal class WallpaperPresetSlideshowDirectory : WallpaperPresetSlideshow {
        [JsonRequired]
        public string SlideshowDirectoryPath { get; set; }
    }
}
