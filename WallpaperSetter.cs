using System;
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
    internal static class WallpaperSetter {
        static readonly Lazy<string?> PackageContext = new(() => Utils.GetCurrentPackageFullName());

        static async Task ApplyPresetOptions(WallpaperPresetBase preset, IDesktopWallpaper dw) {
            if (preset.BackgroundColor != null) {
                var bg = ColorTranslator.FromHtml(preset.BackgroundColor);
                dw.SetBackgroundColor(new COLORREF(((uint)bg.B << 24) | ((uint)bg.G << 16) | (uint)bg.R));
            }
            if (preset.Position != null) {
                dw.SetPosition(preset.Position.Value);
            }
            if (preset.LockScreenImage != null && PackageContext.Value != null) {
                var imageFile = await StorageFile.GetFileFromPathAsync(preset.LockScreenImage);
                await LockScreen.SetImageFileAsync(imageFile);
            }
        }

        static void ApplyFilePreset(IDesktopWallpaper dw, WallpaperPresetFile presetFile) {
            unsafe {
                fixed (char* path = presetFile.FilePath) {
                    dw.SetWallpaper(null, path);
                }
            }
        }

        static void ApplyPerMonitorFileListPreset(IDesktopWallpaper dw, WallpaperPresetPerMonitorFileList presetPMFileList) {
            dw.GetMonitorDevicePathCount(out var monitorCount);
            for (uint i = 0; i < monitorCount; i++) {
                PWSTR monitorId;
                unsafe {
                    dw.GetMonitorDevicePathAt(i, &monitorId);
                    fixed (char* path = presetPMFileList.FilePaths[i % presetPMFileList.FilePaths.Length]) {
                        dw.SetWallpaper(monitorId, path);
                    }
                }
            }
        }

        static void ApplySlideshowOptions(IDesktopWallpaper dw, WallpaperPresetSlideshow presetSlideshow) {
            if (presetSlideshow.SlideshowOptions != null) {
                DESKTOP_SLIDESHOW_OPTIONS options = 0;
                if (presetSlideshow.SlideshowOptions.ShuffleImages) {
                    options |= DESKTOP_SLIDESHOW_OPTIONS.DSO_SHUFFLEIMAGES;
                }
                var slideshowTick = Math.Max(presetSlideshow.SlideshowOptions.SlideshowTick, 60 * 1000);
                dw.SetSlideshowOptions(options, slideshowTick);
            }
        }

        static void ApplySlideshowDirectoryPreset(IDesktopWallpaper dw, WallpaperPresetSlideshowDirectory presetSlideshowDirectory) {
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

        public static async Task ApplyPreset(WallpaperPresetBase preset) {
            if (new DesktopWallpaper() is not IDesktopWallpaper dw) {
                return;
            }
            try {
                await ApplyPresetOptions(preset, dw);

                if (preset is WallpaperPresetBackgroundColor) {
                    dw.Enable((BOOL)false);

                } else if (preset is WallpaperPresetFile presetFile) {
                    ApplyFilePreset(dw, presetFile);

                } else if (preset is WallpaperPresetPerMonitorFileList presetPMFileList) {
                    ApplyPerMonitorFileListPreset(dw, presetPMFileList);

                } else if (preset is WallpaperPresetSlideshow presetSlideshow) {
                    ApplySlideshowOptions(dw, presetSlideshow);
                    if (presetSlideshow is WallpaperPresetSlideshowDirectory presetSlideshowDirectory) {
                        ApplySlideshowDirectoryPreset(dw, presetSlideshowDirectory);
                    }
                }
            } catch (COMException) {
            }
        }

        public static void NextWallpaper() {
            if (new DesktopWallpaper() is not IDesktopWallpaper dw) {
                return;
            }
            try {
                dw.AdvanceSlideshow(null, DESKTOP_SLIDESHOW_DIRECTION.DSD_FORWARD);
            } catch (COMException) {
            } catch (NotImplementedException) {
            }
        }

        public static void PreviousWallpaper() {
            if (new DesktopWallpaper() is not IDesktopWallpaper dw) {
                return;
            }
            try {
                dw.AdvanceSlideshow(null, DESKTOP_SLIDESHOW_DIRECTION.DSD_BACKWARD);
            } catch (COMException) {
            } catch (NotImplementedException) {
            }
        }
    }
}
