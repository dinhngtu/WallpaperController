using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.SystemServices;

namespace WallpaperController {
    internal static class NativeMethods {
        public static string? GetCurrentPackageFullName() {
            // https://github.com/microsoft/Windows-AppConsult-Tools-DesktopBridgeHelpers/blob/master/DesktopBridge.Helpers/Helpers.cs
            if (Environment.OSVersion.Version < new Version(6, 2, 0, 0)) {
                return null;
            }

            uint length = 0;
            WIN32_ERROR err;
            err = PInvoke.GetCurrentPackageFullName(ref length, null);
            if (err == WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE || err != WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER) {
                return null;
            }
            var buf = new char[length];
            unsafe {
                fixed (char* ptr = buf) {
                    err = PInvoke.GetCurrentPackageFullName(ref length, ptr);
                }
            }
            if (err == WIN32_ERROR.ERROR_SUCCESS) {
                return new string(buf, 0, (int)length - 1);
            } else if (err == WIN32_ERROR.APPMODEL_ERROR_NO_PACKAGE) {
                return null;
            } else {
                // ignore error for now
                return null;
            }
        }

        public static readonly Lazy<string?> PackageContext = new(() => GetCurrentPackageFullName());

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
