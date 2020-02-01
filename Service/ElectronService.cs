using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;

namespace ElectronUpdateTest.Service
{
    public class ElectronService : IDisposable
    {
        public static string VERSION { get; } = "0.0.6";
        public string UpdateInfo { get; set; } = VERSION;
        public bool Resized { get; set; } = false;
        Task<UpdateCheckResult> resultTask;

        public ElectronService()
        {
            Electron.AutoUpdater.OnUpdateAvailable += AutoUpdater_OnUpdateAvailable;
            Electron.AutoUpdater.OnUpdateNotAvailable += AutoUpdater_OnUpdateNotAvailable;
            Electron.AutoUpdater.OnCheckingForUpdate += AutoUpdater_OnCheckingForUpdate;
            Electron.AutoUpdater.OnError += AutoUpdater_OnError;
        }

        public void Dispose()
        {
            Electron.AutoUpdater.OnUpdateAvailable -= AutoUpdater_OnUpdateAvailable;
            Electron.AutoUpdater.OnUpdateNotAvailable -= AutoUpdater_OnUpdateNotAvailable;
            Electron.AutoUpdater.OnCheckingForUpdate -= AutoUpdater_OnCheckingForUpdate;
            Electron.AutoUpdater.OnError -= AutoUpdater_OnError;
        }

        public async Task Resize()
        {
            BrowserWindow browserWindow = null;
            int failsafe = 16;
            await Task.Run(() =>
            {
                do
                {
                    Thread.Sleep(250);
                    browserWindow = Electron.WindowManager.BrowserWindows.FirstOrDefault();
                    if (browserWindow != null)
                    {
                        try
                        {
                            browserWindow.SetPosition(0, 0);
                            browserWindow.SetSize(1920, 1024);
                            browserWindow.SetMenuBarVisibility(false);
                        }
                        catch
                        {
                        }
                    }
                    failsafe--;
                } while (browserWindow == null && failsafe > 0);
                if (failsafe > 0)
                    Resized = true;
            });

        }

        public async Task<bool> CheckForUpdate()
        {
            if (Resized == false) return false;
            bool success = false;

            Console.WriteLine("Checking for update ...");
            UpdateCheckResult result = new UpdateCheckResult();
            try
            {
                Electron.Notification.Show(new NotificationOptions("Hello", await Electron.App.GetVersionAsync()));
                Electron.AutoUpdater.AutoDownload = false;
                resultTask =  Electron.AutoUpdater.CheckForUpdatesAsync();
                //resultTask = Electron.AutoUpdater.CheckForUpdatesAsync();
                //resultTask = Electron.AutoUpdater.CheckForUpdatesAndNotifyAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            Console.WriteLine("Update Check running?!");
            return success;
        }

        public async Task<bool> CheckForUpdate2()
        {
            if (Resized == false) return false;
            bool success = false;

            Console.WriteLine("Checking for update ...");
            UpdateCheckResult result = new UpdateCheckResult();
            try
            {
                Electron.Notification.Show(new NotificationOptions("Hello", await Electron.App.GetVersionAsync()));
                Electron.AutoUpdater.AutoDownload = false;
                Electron.AutoUpdater.OnUpdateAvailable += AutoUpdater_OnUpdateAvailable;
                Electron.AutoUpdater.OnUpdateNotAvailable += AutoUpdater_OnUpdateNotAvailable;
                Electron.AutoUpdater.OnCheckingForUpdate += AutoUpdater_OnCheckingForUpdate;
                Electron.AutoUpdater.OnError += AutoUpdater_OnError;

                result = await Electron.AutoUpdater.CheckForUpdatesAndNotifyAsync();
                //resultTask = Electron.AutoUpdater.CheckForUpdatesAsync();
                //resultTask = Electron.AutoUpdater.CheckForUpdatesAndNotifyAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            Console.WriteLine("Update Check running?!");

            Console.WriteLine(result.UpdateInfo.Version);
            return success;
        }

        private void AutoUpdater_OnError(string obj)
        {
            Console.WriteLine("Update error: " + obj);
        }

        private void AutoUpdater_OnCheckingForUpdate()
        {
            Console.WriteLine("Checking for new update.");
        }

        private void AutoUpdater_OnUpdateNotAvailable(UpdateInfo obj)
        {
            Console.WriteLine("No new version available: " + obj.Version);
        }

        private void AutoUpdater_OnUpdateAvailable(UpdateInfo obj)
        {
            Console.WriteLine("Version available: " + obj.Version);
            var result = resultTask.Result;
            UpdateInfo = String.Format("{0} ({1}): {2}", result.UpdateInfo.Version, result.UpdateInfo.ReleaseDate, result.UpdateInfo.ReleaseNotes.FirstOrDefault());
        }

        public async Task QuitAndInstall()
        {
            await Electron.AutoUpdater.DownloadUpdateAsync();

            Electron.AutoUpdater.OnDownloadProgress += AutoUpdater_OnDownloadProgress;

            Electron.AutoUpdater.OnUpdateDownloaded += AutoUpdater_OnUpdateDownloaded;
        }

        private void AutoUpdater_OnDownloadProgress(ProgressInfo obj)
        {
            UpdateInfo = obj.Percent + " " + obj.BytesPerSecond + "/s" + " " + obj.Transferred + " " + obj.Progress + " " + obj.Total;
            
            Console.WriteLine(UpdateInfo);
        }

        private void AutoUpdater_OnUpdateDownloaded(UpdateInfo obj)
        {
            try
            {
                Electron.AutoUpdater.QuitAndInstall(false, true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Electron.AutoUpdater.OnUpdateDownloaded -= AutoUpdater_OnUpdateDownloaded;
            }
        }
    }
}
