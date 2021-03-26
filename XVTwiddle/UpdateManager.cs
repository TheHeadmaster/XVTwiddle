using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Librarium.Core;
using Octokit;

namespace XVTwiddle
{
    public static class UpdateManager
    {
        /// <summary>
        /// The title of the application.
        /// </summary>
        private const string applicationTitle = "XVTwiddle";

        /// <summary>
        /// The name of the Github repository to pull updates from.
        /// </summary>
        private const string repositoryName = "XVTwiddle";

        /// <summary>
        /// The owner of the Github repository to pull updates from.
        /// </summary>
        private const string repositoryOwner = "TheHeadmaster";

        /// <summary>
        /// The name of the update file to download.
        /// </summary>
        private const string updateFilename = "XVTwiddle.Installer.msi";

        /// <summary>
        /// The Github client that connects to the repository.
        /// </summary>
        private static GitHubClient Client { get; set; } = null!;

        /// <summary>
        /// The url that points to the most recent release version in the Github repository.
        /// </summary>
        private static string UpdateUrl { get; } = $"https://github.com/{repositoryOwner}/{repositoryName}/releases/download";

        /// <summary>
        /// Prompts the user for permission to update the software.
        /// </summary>
        /// <param name="updateName">
        /// </param>
        /// <returns>
        /// </returns>
        private static bool GetPermissionToUpdate(string updateName)
        {
            MessageBoxResult messageBoxResult = Xceed.Wpf.Toolkit.MessageBox.Show("A new version of XVTWiddle (" + updateName + ") is available. Do you wish to install it?", "XVTwiddle Update", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes && App.Metadata.CurrentProject is { })
            {
                messageBoxResult = Xceed.Wpf.Toolkit.MessageBox.Show("XVTwiddle will need to close during the updating procedure. Press \"Yes\" to save your work and continue, or \"No\" to cancel.", "XVTwiddle Update", MessageBoxButton.YesNo);
            }

            return messageBoxResult == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Determines if the current version of the program needs an update.
        /// </summary>
        /// <param name="version">
        /// The version to check against.
        /// </param>
        private static bool NeedsUpdate(string[] version) =>
            AppMeta.CurrentVersion.Major < Convert.ToInt32(version[0])
            || (AppMeta.CurrentVersion.Major == Convert.ToInt32(version[0])
                && AppMeta.CurrentVersion.Minor < Convert.ToInt32(version[1]))
            || (AppMeta.CurrentVersion.Major == Convert.ToInt32(version[0])
                && AppMeta.CurrentVersion.Minor == Convert.ToInt32(version[1])
                && AppMeta.CurrentVersion.Build < Convert.ToInt32(version[2]));

        /// <summary>
        /// Checks to see if there are any available updates for the current running version of
        /// XVTwiddle.
        /// </summary>
        /// <param name="silentMode">
        /// </param>
        public static void CheckForUpdates(bool silentMode)
        {
            try
            {
                IReadOnlyList<Release>? result = Client.Repository.Release.GetAll(repositoryOwner, repositoryName)?.Result;
                if (result is null || result.Count <= 0)
                {
                    if (!silentMode)
                    {
                        Xceed.Wpf.Toolkit.MessageBox.Show("XVTwiddle is already at the latest version.", "XVTwiddle Up to Date", MessageBoxButton.OK);
                    }
                    return;
                }
                Release release = Client.Repository.Release.GetAll("TheHeadmaster", "XVTwiddle").Result[0];
                string[] version = release.TagName.Split('.', '-');
                version[0] = version[0].Replace("v", string.Empty);
                if (NeedsUpdate(version))
                {
                    if (!GetPermissionToUpdate(release.TagName)) { return; }

                    string address = $"{UpdateUrl}/{release.TagName}/{updateFilename}";
                    string fileName = Path.Combine(Path.GetTempPath(), updateFilename);
                    using (WebClient webClient = new WebClient())
                    {
                        webClient.DownloadFile(address, fileName);
                    }

                    new Process()
                    {
                        StartInfo = new ProcessStartInfo()
                        {
                            Arguments = "/i " + fileName,
                            FileName = "msiexec"
                        }
                    }.Start();
                    App.Current.Shutdown();
                }
                else if (!silentMode)
                {
                    Xceed.Wpf.Toolkit.MessageBox.Show("XVTwiddle is already at the latest version.", "XVTwiddle Up to Date", MessageBoxButton.OK);
                }
            }
            catch (Exception)
            {
                if (!silentMode)
                {
                    Xceed.Wpf.Toolkit.MessageBox.Show($"Something went wrong. Check your internet connection. If the problem persists, please open an issue at https://github/{repositoryOwner}/{repositoryName}/issues.", "XVTwiddle", MessageBoxButton.OK);
                }
            }
        }

        /// <summary>
        /// Initializes the update manager.
        /// </summary>
        public static void Initialize()
        {
            UpdateManager.Client = new GitHubClient(new ProductHeaderValue(applicationTitle, AppMeta.CurrentVersion.ToString()));
            UpdateManager.CheckForUpdates(true);
        }
    }
}