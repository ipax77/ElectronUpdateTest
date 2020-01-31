using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;

namespace ElectronUpdateTest.Service
{
    public class ElectronService
    {
        public static string VERSION { get; } = "0.0.5";
        public string UpdateInfo { get; set; } = VERSION;
        public bool Resized { get; set; } = false;

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

            UpdateCheckResult result = new UpdateCheckResult();
            try
            {
                Electron.AutoUpdater.AutoDownload = false;
                result = await Electron.AutoUpdater.CheckForUpdatesAsync();
                
                Console.WriteLine(result.UpdateInfo.Version);
                Console.WriteLine(result.UpdateInfo.ReleaseDate);
            }
            catch
            {
                return false;
            }

            if (VERSION == result.UpdateInfo.Version)
                success = false;
            else
            {
                UpdateInfo = String.Format("{0} ({1}): {2}", result.UpdateInfo.Version, result.UpdateInfo.ReleaseDate, result.UpdateInfo.ReleaseNotes.FirstOrDefault());
                success = true;
            }

            return success;
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
