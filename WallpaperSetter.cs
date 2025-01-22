using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System.UserProfile;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;
using Windows.Win32.UI.Shell;

namespace WallpaperController {
    internal class WallpaperSetter {
        readonly IDesktopWallpaper dw;

        public WallpaperSetter() {
            dw = (IDesktopWallpaper)new DesktopWallpaper();
        }

        async Task ApplyPresetOptions(WallpaperPresetBase preset) {
            if (preset.BackgroundColor != null) {
                var bg = ColorTranslator.FromHtml(preset.BackgroundColor);
                dw.SetBackgroundColor(new COLORREF(((uint)bg.B << 24) | ((uint)bg.G << 16) | (uint)bg.R));
            }
            if (preset.Position != null) {
                dw.SetPosition(preset.Position.Value);
            }
            if (preset.LockScreenImage != null && NativeMethods.PackageContext.Value != null) {
                var imageFile = await StorageFile.GetFileFromPathAsync(preset.LockScreenImage);
                await LockScreen.SetImageFileAsync(imageFile);
            }
        }

        async Task ApplyBackgroundColorPreset() {
            // if in slideshow, must first change to an existing single wallpaper (that's different from the current one)
            // should we disable virtualization on this key to avoid stale reads if we happen to write here?
            using var wallpaperKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Wallpapers");
            if (wallpaperKey.GetValue("BackgroundType") is int oldBgType && oldBgType == 2) {
                PWSTR monitorId;
                PWSTR wallpaper;
                unsafe {
                    dw.GetMonitorDevicePathAt(0, &monitorId);
                    try {
                        dw.GetWallpaper(monitorId, &wallpaper);
                        try {
                            dw.SetWallpaper(null, wallpaper);
                        } finally {
                            Marshal.FreeCoTaskMem((nint)(void*)wallpaper);
                        }
                    } finally {
                        Marshal.FreeCoTaskMem((nint)(void*)monitorId);
                    }
                }
                for (int i = 0; i < 5; i++) {
                    await Task.Delay(100);
                    if (wallpaperKey.GetValue("BackgroundType") is int newBgType && newBgType != 2) {
                        break;
                    }
                }
            }
            dw.Enable((BOOL)false);
        }

        void ApplyFilePreset(WallpaperPresetFile presetFile) {
            dw.SetWallpaper(null, presetFile.FilePath);
        }

        string GetMonitorDevicePathAt(uint i) {
            PWSTR monitorId;
            unsafe {
                dw.GetMonitorDevicePathAt(i, &monitorId);
                try {
                    return monitorId.ToString();
                } finally {
                    Marshal.FreeCoTaskMem((nint)(void*)monitorId);
                }
            }
        }

        IEnumerable<string> GetMonitorDevicePaths(bool attachedOnly = true) {
            dw.GetMonitorDevicePathCount(out var monitorCount);
            for (uint i = 0; i < monitorCount; i++) {
                var monitorId = GetMonitorDevicePathAt(i);
                if (attachedOnly) {
                    try {
                        dw.GetMonitorRECT(monitorId, out var rect);
                    } catch (COMException) {
                        continue;
                    }
                }
                yield return monitorId;
            }
        }

        void ApplyPerMonitorFileListPreset(WallpaperPresetPerMonitorFileList presetPMFileList) {
            dw.GetMonitorDevicePathCount(out var monitorCount);
            for (uint i = 0; i < monitorCount; i++) {
                PWSTR monitorId;
                unsafe {
                    dw.GetMonitorDevicePathAt(i, &monitorId);
                    try {
                        fixed (char* path = presetPMFileList.FilePaths[i % presetPMFileList.FilePaths.Length]) {
                            dw.SetWallpaper(monitorId, path);
                        }
                    } finally {
                        Marshal.FreeCoTaskMem((nint)(void*)monitorId);
                    }
                }
            }
        }

        void ApplySlideshowOptions(WallpaperPresetSlideshow presetSlideshow) {
            if (presetSlideshow.SlideshowOptions != null) {
                DESKTOP_SLIDESHOW_OPTIONS options = 0;
                if (presetSlideshow.SlideshowOptions.ShuffleImages) {
                    options |= DESKTOP_SLIDESHOW_OPTIONS.DSO_SHUFFLEIMAGES;
                }
                var slideshowTick = Math.Max(presetSlideshow.SlideshowOptions.SlideshowTick, 60 * 1000);
                dw.SetSlideshowOptions(options, slideshowTick);
            }
        }

        void ApplySlideshowDirectoryPreset(WallpaperPresetSlideshowDirectory presetSlideshowDirectory) {
            unsafe {
                var hr = PInvoke.SHParseDisplayName(
                    presetSlideshowDirectory.SlideshowDirectoryPath,
                    null,
                    out var pidl,
                    (uint)(SFGAO_FLAGS.SFGAO_FOLDER | SFGAO_FLAGS.SFGAO_FILESYSTEM),
                    null);
                hr.ThrowOnFailure();

                try {
                    hr = PInvoke.SHCreateShellItemArrayFromIDLists(
                        1,
                        &pidl,
                        out var sia);
                    hr.ThrowOnFailure();

                    dw.SetSlideshow(sia);
                } finally {
                    Marshal.FreeCoTaskMem((nint)pidl);
                }
            }
        }

        public async Task ApplyPreset(WallpaperPresetBase preset) {
            try {
                await ApplyPresetOptions(preset);

                if (preset is WallpaperPresetBackgroundColor) {
                    await ApplyBackgroundColorPreset();

                } else if (preset is WallpaperPresetFile presetFile) {
                    ApplyFilePreset(presetFile);

                } else if (preset is WallpaperPresetPerMonitorFileList presetPMFileList) {
                    ApplyPerMonitorFileListPreset(presetPMFileList);

                } else if (preset is WallpaperPresetSlideshow presetSlideshow) {
                    ApplySlideshowOptions(presetSlideshow);
                    if (presetSlideshow is WallpaperPresetSlideshowDirectory presetSlideshowDirectory) {
                        ApplySlideshowDirectoryPreset(presetSlideshowDirectory);
                    }
                }
            } catch (COMException) {
            }
        }

        public void NextWallpaper() {
            try {
                dw.AdvanceSlideshow(null, DESKTOP_SLIDESHOW_DIRECTION.DSD_FORWARD);
            } catch (COMException) {
            } catch (NotImplementedException) {
            }
        }

        public void PreviousWallpaper() {
            try {
                dw.AdvanceSlideshow(null, DESKTOP_SLIDESHOW_DIRECTION.DSD_BACKWARD);
            } catch (COMException) {
            } catch (NotImplementedException) {
            }
        }

        public List<string> CurrentWallpapers() {
            var ret = new List<string>();
            try {
                foreach (var monitorId in GetMonitorDevicePaths()) {
                    dw.GetWallpaper(monitorId.ToString(), out var wallpaper);
                    try {
                        ret.Add(wallpaper.ToString());
                    } finally {
                        unsafe {
                            Marshal.FreeCoTaskMem((nint)(void*)wallpaper);
                        }
                    }
                }
            } catch {
                ret.Clear();
            }
            return ret;
        }
    }
}
