using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DynamicData;
using Librarium.Json;
using XVTwiddle.ObjectModel;
using XVTwiddle.ObjectModel.Attributes;
using XVTwiddle.Json;
using ReactiveUI.Fody.Helpers;
using System.Text.Json.Serialization;
using ReactiveUI;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using XVTwiddle.Diagnostics;

namespace XVTwiddle.ObjectModel
{
    /// <summary>
    /// Represents a XVTwiddle project.
    /// </summary>
    public class Project : SaveableObject, IModelToFile
    {
        /// <summary>
        /// Gets whether all project items are saved.
        /// </summary>
        public bool AreItemsSaved { [ObservableAsProperty]get; }

        /// <summary>
        /// A list of all <see cref="ElementMatchup"/> s in the database.
        /// </summary>
        ///[Reactive]
        ///[Memento]
        ///public SourceCache<ElementMatchup, int> ElementMatchups { get; set; } = new SourceCache<ElementMatchup, int>(x => x.ID);

        /// <summary>
        /// The name of the <see cref="JFile"/> on disk.
        /// </summary>
        public string FileName { get; set; } = "";

        /// <summary>
        /// The path to the <see cref="JFile"/> on disk.
        /// </summary>
        public string FilePath { get; set; } = "";

        /// <summary>
        /// The name of the project.
        /// </summary>
        [Reactive]
        [Memento]
        public string? Name { get; set; } = "";

        /// <summary>
        /// The version of XVTwiddle that the project was made with.
        /// </summary>
        [Reactive]
        [Memento]
        public Version? Version { get; set; }

        /// <summary>
        /// Creates a new <see cref="Project"/>.
        /// </summary>
        public Project()
        {
            ///this.Elements.Connect()
            ///    .WhenValueChanged(x => x.IsSaved)
            ///    .Select(x => this.Elements.Items.All(x => x.IsSaved))
            ///    .ToPropertyEx(this, x => x.AreItemsSaved, true);
        }

        /// <summary>
        /// Closes the project with an option to save before closing.
        /// </summary>
        /// <param name="saveBeforeClosing">
        /// Saves before closing if <see langword="true"/>.
        /// </param>
        public async Task Close(bool saveBeforeClosing)
        {
            if (saveBeforeClosing)
            {
                await SaveAllAsync();
            }
            SaveableObjects.Clear();
            await App.Metadata.ClearCurrentProject();
        }

        /// <summary>
        /// Loads the project and all of its associated data objects.
        /// </summary>
        [Log("Loading Project...", "Project loaded successfully.", "Loading project failed.")]
        public Task Load()
        {
            Directory.CreateDirectory(Path.Combine(this.FilePath, "Images"));
            Log.Information("Ensured project directories were created.");

            App.Preferences.RecentlyOpenedProjects.AddOrUpdate(new RecentItem(this.Name ?? "", this.FilePath, DateTime.Now));
            Log.Information("Project updated in recently opened items list.");

            App.Preferences.Save();
            foreach (SaveableObject loadedObject in SaveableObjects.Items)
            {
                loadedObject.IsSaved = true;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Saves the <see cref="JFile"/> to disk.
        /// </summary>
        public void Save()
        {
            ProjectFile file = new ProjectFile();
            file.PopulateFile(this);
            file.Save();
        }

        /// <summary>
        /// Saves the <see cref="JFile"/> to disk.
        /// </summary>
        public override Task SaveAsync()
        {
            this.Save();
            this.IsSaved = true;
            return Task.CompletedTask;
        }
    }
}