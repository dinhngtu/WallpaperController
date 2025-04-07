using System;
using System.Drawing;
using System.Windows.Forms;
using WallpaperController.Properties;

namespace WallpaperController {
    partial class AboutBox1 : Form {
        public AboutBox1() {
            InitializeComponent();
            var logoSize = Math.Min(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = new Icon(Resources.AppIcon, new Size(logoSize, logoSize)).ToBitmap();
            var version = SystemUtils.GetCurrentPackage()?.Id?.Version;
            if (version != null) {
                label2.Text = $"{version.Value.Major}.{version.Value.Minor}.{version.Value.Build}.{version.Value.Revision}";
            } else {
                label2.Text = "";
            }
        }
    }
}
