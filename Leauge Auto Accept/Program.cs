using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Reflection;

namespace Leauge_Auto_Accept
{
    class Program
    {
        private static readonly NLog.ILogger Log = NLog.LogManager.GetCurrentClassLogger();

        private static void Main()
        {
            //force culture to be invariant for now.
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            // Start application using conhost (Windows Terminal do not support resize)
            // Ref: https://github.com/microsoft/terminal/issues/5094
            var parentProc = ParentProcessUtilities.GetParentProcess();
            var isDebug = Debugger.IsAttached;
            if (!isDebug && parentProc.ProcessName != "conhost")
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "conhost",
                    WorkingDirectory = Directory.GetCurrentDirectory(),
                    Arguments = Process.GetCurrentProcess().MainModule.FileName,
                });
                return;
            }

            // Show initializing message
            UI.initializingWindow();

            // Initlize console size related stuff
            SizeHandler.initialize();

            // Set console title
            Console.Title = "League Auto Accept";

            // Set output to UTF8
            Console.OutputEncoding = Encoding.UTF8;

            Strings.LoadLanguages();

            // Attempt to load existing settings
            Settings.loadSettings();

            Updater.initialize();

            // Start a bunch of task
            var taskKeys = new Task(() => runGuarded(Navigation.ReadKeys));
            taskKeys.Start();
            var taskQueue = new Task(() => runGuarded(MainLogic.acceptQueue));
            taskQueue.Start();
            var taskLeagueAlive = new Task(() => runGuarded(LCU.CheckIfLeagueClientIsOpenTask));
            taskLeagueAlive.Start();
            var taskResizeHandler = new Task(() => runGuarded(SizeHandler.SizeReader));
            taskResizeHandler.Start();

            // Indefinitely await them
            var tasks = new[] { taskKeys, taskQueue, taskLeagueAlive, taskResizeHandler };
            Task.WaitAll(tasks);

            Console.ReadKey();
        }

        // An exception in a task is only observed once every task has ended, and these never end,
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
