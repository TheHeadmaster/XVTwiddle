using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DynamicData;
using Librarium.Json;
using XVTwiddle.ObjectModel;

namespace XVTwiddle.Json
{
    public class PreferencesFile : JFile, IFileToModel<Preferences>
    {
        public bool OpenLastProjectOnStartup { get; set; }

        public List<RecentItem> RecentlyOpenedProjects { get; set; } = new List<RecentItem>();

        public Preferences CreateModel()
        {
            Preferences preferences = new Preferences
            {
                OpenLastProjectOnStartup = this.OpenLastProjectOnStartup,
                RecentlyOpenedProjects = new SourceCache<RecentItem, string>(x => x.Path),
                FilePath = this.FilePath,
                FileName = this.FileName,
            };
            preferences.RecentlyOpenedProjects.AddOrUpdate(this.RecentlyOpenedProjects);
            return preferences;
        }

        public void PopulateFile(Preferences model)
        {
            this.OpenLastProjectOnStartup = model.OpenLastProjectOnStartup;
            this.RecentlyOpenedProjects = model.RecentlyOpenedProjects.Items.ToList();
            this.FilePath = model.FilePath;
            this.FileName = model.FileName;
        }
    }
}