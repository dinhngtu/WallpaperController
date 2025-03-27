using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.Win32;
using Windows.Win32.System.SystemServices;

namespace WallpaperController {
    internal static class SystemUtils {
        public static Package? GetCurrentPackage() {
            try {
                return Package.Current;
            } catch {
                return null;
            }
        }

        public static bool IsPackaged = GetCurrentPackage() != null;

        public static void RevealPath(string path) {
            try {
                unsafe {
                    var hr = PInvoke.SHParseDisplayName(
                        path,
                        null,
                        out var pidl,
                        (uint)(SFGAO_FLAGS.SFGAO_FOLDER | SFGAO_FLAGS.SFGAO_FILESYSTEM),
                        null);
                    hr.ThrowOnFailure();

                    try {
                        hr = PInvoke.SHOpenFolderAndSelectItems(pidl, 0, null, 0);
                        hr.ThrowOnFailure();
                    } finally {
                        Marshal.FreeCoTaskMem((nint)pidl);
                    }
                }
            } catch {
            }
        }
    }
}
