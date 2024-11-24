using System;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace WallpaperController {
    internal class Utils {
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
    }
}
