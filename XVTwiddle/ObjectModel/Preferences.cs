using System;
using System.Collections.Generic;
using System.Text;
using Librarium.Json;
using ReactiveUI;
using DynamicData;
using System.Windows;
using System.Linq;
using XVTwiddle.Json;
using ReactiveUI.Fody.Helpers;
using System.Threading.Tasks;

namespace XVTwiddle.ObjectModel
{
    /// <summary>
    /// Houses the options defined by the user.
    /// </summary>
    public class Preferences : ReactiveObject, IModelToFile
    {
        /// <summary>
        /// Gets or sets the name of the json file saved to disk.
        /// </summary>
        public string FileName { get; set; } = null!;

        /// <summary>
        /// Gets or sets the path of the json file saved to disk.
        /// </summary>
        public string FilePath { get; set; } = null!;

        /// <summary>
        /// Gets or sets whether to automatically open the most recently opened project when the
        /// program starts.
        /// </summary>
        public bool OpenLastProjectOnStartup { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to use the Metric system or the Imperial system for measurements.
        /// </summary>
        public bool PreferMetric { get; set; }

        /// <summary>
        /// Gets a list of most recently opened projects. The list will keep no more than 10
        /// recently opened projects.
        /// </summary>
        [Reactive]
        public SourceCache<RecentItem, string> RecentlyOpenedProjects { get; set; } = new SourceCache<RecentItem, string>(x => x.Path);

        public Preferences()
        {
            this.WhenAnyValue(x => x.RecentlyOpenedProjects)
                .Subscribe(x =>
                {
                    this.RecentlyOpenedProjects.LimitSizeTo(10);
                });
        }

        /// <summary>
        /// Loads Preferences from disk.
        /// </summary>
        public static async Task Load()
        {
            RecentItem? firstItem = App.Preferences.RecentlyOpenedProjects.Items.FirstOrDefault();
            if (App.Preferences.OpenLastProjectOnStartup && firstItem is { })
            {
                ProjectFile projectFile = JFile.Load<ProjectFile>(firstItem.Path, "Project.json");
                await App.Metadata.ChangeCurrentProject(projectFile.CreateModel());

                await App.Metadata.CurrentProject.Load();
            }
        }

        /// <summary>
        /// Gets a memberwise copy of the <see cref="Preferences"/> object.
        /// </summary>
        public Preferences GetCopy() => (Preferences)this.MemberwiseClone();

        /// <summary>
        /// Saves the <see cref="Preferences"/> to disk.
        /// </summary>
        public void Save()
        {
            PreferencesFile file = new PreferencesFile();
            file.PopulateFile(this);
            file.Save();
        }
    }
}