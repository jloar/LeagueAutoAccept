using System;
using System.Threading;
using System.Threading.Tasks;

namespace Leauge_Auto_Accept
{
    class Program
    {
        private static readonly NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();

        [STAThread]
        private static void Main()
        {
            //force culture to be invariant for now.
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            // Attempt to load existing settings
            Settings.loadSettings();

            Updater.initialize();

            // These run for as long as the window is open
            Task.Run(() => runGuarded(MainLogic.acceptQueue));
            Task.Run(() => runGuarded(LCU.CheckIfLeagueClientIsOpenTask));

            var app = new App();
            app.InitializeComponent();
            app.DispatcherUnhandledException += (sender, e) =>
            {
                Log.Error(e.Exception, "Unhandled exception in the window.");
                e.Handled = true;
            };
            app.Run(new MainWindow());
        }

        // A task's exception is only seen if something waits on it, and nothing does,
        // so without this a crashed loop would stop silently while the app appears to keep running
        private static void runGuarded(Action loop)
        {
            while (true)
            {
                try
                {
                    loop();
                    return;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Background task {0} crashed, restarting it.", loop.Method.Name);
                    Thread.Sleep(2000);
                }
            }
        }
    }
}
