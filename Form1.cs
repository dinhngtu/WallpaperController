using Microsoft.Win32;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Windows.Forms;
using System.IO;
using Windows.Win32;
using Windows.Win32.System.Recovery;
using WallpaperController.Properties;
using System.Diagnostics;

namespace WallpaperController {
    public partial class Form1 : Form {

        private WallpaperSettings? currentConfig = null;
        WallpaperSettings? CurrentConfig {
            get => currentConfig;
            set {
                currentConfig = value;
                presetContextMenuStrip.Items.Clear();
                if (currentConfig?.Presets != null) {
                    foreach (var preset in currentConfig.Presets) {
                        presetContextMenuStrip.Items.Add(preset.PresetName);
                    }
                }
            }
        }

        public Form1() {
            InitializeComponent();

            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
        }

        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e) {
            PInvoke.RegisterApplicationRestart((string?)null, REGISTER_APPLICATION_RESTART_FLAGS.RESTART_NO_REBOOT);
            Close();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Close();
        }

        private async void Form1_Load(object sender, EventArgs e) {
            Settings.Default.Upgrade();
            if (string.IsNullOrEmpty(Settings.Default.ConfigPath)) {
                return;
            }
            await ParseConfig();
        }

        private async Task ParseConfig() {
            configFileNameToolStripMenuItem.Text = Settings.Default.ConfigPath;
            try {
                using var configStream = File.OpenRead(Settings.Default.ConfigPath!);
                var parsedSettings = await JsonSerializer.DeserializeAsync<WallpaperSettings>(configStream);
                if (parsedSettings == null) {
                    return;
                }
                CurrentConfig = parsedSettings;
            } catch {
                MessageBox.Show(
                    this,
                    $"Cannot parse config file: {Settings.Default.ConfigPath}",
                    "Wallpaper Controller",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Hand);
            }
        }

        private async void presetContextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            var presetName = e?.ClickedItem?.Text;
            if (presetName is null) {
                return;
            }
            var preset = CurrentConfig?.Presets.FirstOrDefault(x => x.PresetName == presetName);
            if (preset == null) {
                return;
            }
            await Task.Run(() => WallpaperSetter.ApplyPreset(preset));
        }

        private async void nextToolStripMenuItem_Click(object sender, EventArgs e) {
            await Task.Run(WallpaperSetter.NextWallpaper);
        }

        private async void previousToolStripMenuItem_Click(object sender, EventArgs e) {
            await Task.Run(WallpaperSetter.PreviousWallpaper);
        }

        private async void chooseFileToolStripMenuItem_Click(object sender, EventArgs e) {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK) {
                Settings.Default.ConfigPath = openFileDialog1.FileName;
                Settings.Default.Save();
                await ParseConfig();
            }
        }

        private void editFileToolStripMenuItem_Click(object sender, EventArgs e) {
            var psi = new ProcessStartInfo(Settings.Default.ConfigPath);
            psi.UseShellExecute = true;
            using var proc = Process.Start(psi);
        }
    }
}
