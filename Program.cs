using System.Threading;
using System.Windows.Forms;
using System;
using WallpaperController;

namespace WallpaperController {
    internal class MyApplicationContext : ApplicationContext {
        readonly Form1 form1;

        public MyApplicationContext() {
            form1 = new Form1();
            form1.FormClosed += Form1_FormClosed;
            form1.Show();
            form1.Hide();
        }

        private void Form1_FormClosed(object? sender, FormClosedEventArgs e) {
            ExitThread();
        }
    }

    internal static class Program {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            using var m = new Mutex(true, "{1B3E628C-C811-48E2-8F98-55F7FACBC5FC}", out var created);
            if (!created) {
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MyApplicationContext());
        }
    }
}
