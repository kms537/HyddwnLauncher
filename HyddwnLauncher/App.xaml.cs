﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using CefSharp;
using HyddwnLauncher.Core;
using HyddwnLauncher.Util;

namespace HyddwnLauncher
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly string LauncherAssembly = Assembly.GetExecutingAssembly().Location;
        private static readonly string Assemblypath = Path.GetDirectoryName(LauncherAssembly);
        public static string[] CmdArgs;

        protected override void OnStartup(StartupEventArgs e)
        {
            var settings = new CefSettings();
            settings.CefCommandLineArgs.Add("disable-gpu-vsync", "1");
            settings.CefCommandLineArgs.Add("disable-gpu", "1");
            Cef.Initialize();

            var packFileClean = false;

            if (!Directory.Exists($@"{Assemblypath}\Logs\Hyddwn Launcher"))
                Directory.CreateDirectory($@"{Assemblypath}\Logs\Hyddwn Launcher");

            var logFileString = $@"{Assemblypath}\Logs\Hyddwn Launcher\Hyddwn Launcher-{DateTime.Now:yyyy-MM-dd_hh-mm}.log";;
            var launcherVersionString = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Log.LogFile = logFileString;

            Log.Info("=== Application Startup ===");

            Log.Info($"Hyddwn Launcher version: {launcherVersionString}");

            Log.Info("Initializing Launcher Context");
            var launcherContext = new LauncherContext(logFileString, launcherVersionString);

#if DEBUG
            launcherContext.LauncherSettingsManager.Reset();
#endif
            Log.Info("Checking for launch arguments...");
            CmdArgs = Environment.GetCommandLineArgs();
            for (var index = 0; index != e.Args.Length; ++index)
            {
                if (e.Args[index].Contains("/?") || e.Args[index].Contains("-?"))
                {
                    Console.WriteLine(HyddwnLauncher.Properties.Resources.usage__);
                    Console.WriteLine(HyddwnLauncher.Properties.Resources.usage___);
                    Environment.Exit(0);
                }

                if (e.Args[index].Contains("/noadmin"))
                {
                    launcherContext.LauncherSettingsManager.LauncherSettings.RequiresAdmin = false;
                    Log.Info("/noadmin was declared. Disabled admin elevation requirement.");
                }

                if (e.Args[index].Contains("/noupdate"))
                {
                    launcherContext.LauncherSettingsManager.LauncherSettings.AutomaticallyCheckForUpdates = false;
                    Log.Info("/noupdate was declared. Disabled automatic update checking.");
                }

                if (e.Args[index].Contains("/clean"))
                {
                    packFileClean = true;
                    Log.Info("[Expiremental] /clean was declared. Pack files generated by custom servers will be deleted once all Mabinogi clients have closed.");
                }    
            }

            if (packFileClean)
            {
                WaitForProcessEndThenCleanPack();
                return;
            }

#if DEBUG
            launcherContext.LauncherSettingsManager.LauncherSettings.RequiresAdmin = false;
            launcherContext.LauncherSettingsManager.LauncherSettings.FirstRun = true;
#endif
            CheckForAdmin(launcherContext.LauncherSettingsManager.LauncherSettings.RequiresAdmin);
            
            ServicePointManager.DefaultConnectionLimit =
                launcherContext.LauncherSettingsManager.LauncherSettings.ConnectionLimit;
            Log.Info($"Applied max download limit of {launcherContext.LauncherSettingsManager.LauncherSettings.ConnectionLimit} based off of user settings.");

            Log.Info("Application preinitialized, beginning startup tasks.");
            var splashScreen = new SplashScreen(launcherContext);
            Current.MainWindow = splashScreen;
            splashScreen.Show();
            base.OnStartup(e);
        }

        private void WaitForProcessEndThenCleanPack()
        {
            var clientCount = 0;

            var mabiClients = Process.GetProcessesByName("client");

            foreach (var mabiClient in mabiClients)
            {
                clientCount++;
                mabiClient.EnableRaisingEvents = true;
                mabiClient.Exited += (sender, args) =>
                {
                    clientCount--;
                    if (clientCount != 0) return;
                    var location = mabiClient.MainModule.FileName;
                    var locationPath = Path.GetDirectoryName(location);
                    if (locationPath == null) return;
                    var packPath = Path.Combine(locationPath, "package");
                    var files = Directory.GetFiles(packPath);
                    foreach (var file in from file in files
                        let fileName = Path.GetFileName(file)
                        where fileName.Contains("hy_")
                        select file)
                        File.Delete(file);
                    Shutdown();
                };
            }
        }

        private static void CheckForAdmin(bool requiresAdmin)
        {
            if (!requiresAdmin || IsAdministrator())
                return;
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory
            };
            if (LauncherAssembly != null)
                startInfo.FileName = LauncherAssembly;
            startInfo.Arguments = string.Join(" ", Environment.GetCommandLineArgs());
            startInfo.Verb = "runas";
            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                Log.Error("Cannot start elevated instance:\r\n{0}", (object) ex);
            }

            Environment.Exit(0);
        }

        public static bool IsAdministrator()
        {
            return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Info("=== Application Shutdown ===");
            base.OnExit(e);
        }
    }
}