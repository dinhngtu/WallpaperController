using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System;
using System.Windows.Forms;
using System.IO;
using WallpaperController.Properties;
using System.Diagnostics;

namespace WallpaperController {
    partial class Form1 : Form {

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
            notifyIcon1.Icon = Resources.AppIcon;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Close();
        }

        private async void Form1_Load(object sender, EventArgs e) {
            Settings.Default.Upgrade();
            await ParseConfig();
        }

        private async Task ParseConfig() {
            configFileNameToolStripMenuItem.Text = Settings.Default.ConfigPath;
            if (string.IsNullOrEmpty(Settings.Default.ConfigPath)) {
                return;
            }
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

        private void presetContextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            var presetName = e?.ClickedItem?.Text;
            if (presetName is null) {
                return;
            }
            var preset = CurrentConfig?.Presets.FirstOrDefault(x => x.PresetName == presetName);
            if (preset == null) {
                return;
            }
            Task.Run(() => new WallpaperSetter().ApplyPreset(preset));
        }

        private void nextToolStripMenuItem_Click(object sender, EventArgs e) {
            Task.Run(new WallpaperSetter().NextWallpaper);
        }

        private void previousToolStripMenuItem_Click(object sender, EventArgs e) {
            Task.Run(new WallpaperSetter().PreviousWallpaper);
        }

        private async void chooseFileToolStripMenuItem_Click(object sender, EventArgs e) {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK) {
                Settings.Default.ConfigPath = openFileDialog1.FileName;
                Settings.Default.Save();
                await ParseConfig();
            }
        }

        private void editFileToolStripMenuItem_Click(object sender, EventArgs e) {
            try {
                var psi = new ProcessStartInfo(Settings.Default.ConfigPath) {
                    UseShellExecute = true
                };
                using var proc = Process.Start(psi);
            } catch {
            }
        }

        private async void reloadPresetsToolStripMenuItem_Click(object sender, EventArgs e) {
            await ParseConfig();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {
            new AboutBox1().ShowDialog(this);
        }

        private void currentContextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            if (!string.IsNullOrEmpty(e.ClickedItem.Text)) {
                Task.Run(() => SystemUtils.RevealPath(e.ClickedItem.Text));
            }
        }

        private void currentToolStripMenuItem_DropDownOpening(object sender, EventArgs e) {
            currentContextMenuStrip.Items.Clear();
            foreach (var wallpaper in new WallpaperSetter().CurrentWallpapers()) {
                currentContextMenuStrip.Items.Add(wallpaper);
            }
        }
    }
}
