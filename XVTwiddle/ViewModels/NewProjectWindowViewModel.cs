using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DynamicData;
using DynamicData.Binding;
using FluentValidation.Results;
using Microsoft.WindowsAPICodePack.Dialogs;
using XVTwiddle.ObjectModel;
using XVTwiddle.ViewModels.Validation;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace XVTwiddle.ViewModels
{
    public class NewProjectWindowViewModel : ReactiveObject
    {
        private ValidationResult ValidationResult { [ObservableAsProperty] get; } = null!;

        /// <summary>
        /// Opens a folder browse dialog.
        /// </summary>
        public ReactiveCommand<Unit, Unit> BrowseFolder { get; }

        /// <summary>
        /// Creates a new project.
        /// </summary>
        public ReactiveCommand<Unit, Unit> CreateProject { get; }

        /// <summary>
        /// Gets whether all finalize selections are valid.
        /// </summary>
        public bool IsValid { [ObservableAsProperty] get; }

        /// <summary>
        /// The parent window.
        /// </summary>
        public Window? Parent { get; set; }

        [Reactive]
        public string ProjectName { get; set; } = "";

        /// <summary>
        /// The validation text for the project name input.
        /// </summary>
        public string ProjectNameValidationText { [ObservableAsProperty] get; } = null!;

        /// <summary>
        /// The path to the new project.
        /// </summary>
        [Reactive]
        public string ProjectPath { get; set; } = "";

        public string ProjectPathValidationText { [ObservableAsProperty] get; } = null!;

        /// <summary>
        /// Gets the validation context.
        /// </summary>
        public NewProjectWindowValidation ValidationContext { get; } = new NewProjectWindowValidation();

        public NewProjectWindowViewModel()
        {
            this.CreateProject = ReactiveCommand.CreateFromTask(
                this.CreateProjectAsync,
                this.WhenAnyValue(x => x.IsValid));

            this.BrowseFolder = ReactiveCommand.CreateFromTask(
                this.BrowseAsync);

            this.WhenAnyValue(x => x.ProjectName, x => x.ProjectPath, (x, y) => this.ValidationContext.Validate(this))
                .ToPropertyEx(this, x => x.ValidationResult);

            this.WhenAnyValue(x => x.ValidationResult)
                .Select(x =>
                {
                    ValidationFailure? projectNameInvalid = this.ValidationResult.Errors.FirstOrDefault(y => y.PropertyName == nameof(this.ProjectName));
                    return projectNameInvalid is null ? "" : projectNameInvalid.ErrorMessage;
                })
                .ToPropertyEx(this, x => x.ProjectNameValidationText, "");

            this.WhenAnyValue(x => x.ValidationResult)
                .Select(x =>
                {
                    ValidationFailure? projectPathInvalid = this.ValidationResult.Errors.FirstOrDefault(x => x.PropertyName == nameof(this.ProjectPath));
                    return projectPathInvalid is null ? "" : projectPathInvalid.ErrorMessage;
                })
                .ToPropertyEx(this, x => x.ProjectPathValidationText, "");

            this.WhenAnyValue(x => x.ValidationResult)
                .Select(x => x.IsValid)
                .ToPropertyEx(this, x => x.IsValid, initialValue: false);
        }

        public Task BrowseAsync()
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog
            {
                Title = "Choose the folder you would like to save the project in...",
                IsFolderPicker = true,
                InitialDirectory = @"C:\",

                AddToMostRecentlyUsedList = false,
                AllowNonFileSystemItems = false,
                DefaultDirectory = @"C:\",
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true
            };

            if (dlg.ShowDialog(this.Parent) == CommonFileDialogResult.Ok)
            {
                this.ProjectPath = dlg.FileName;
            }
            return Task.CompletedTask;
        }

        public async Task CreateProjectAsync()
        {
            //List<PackTemplate> packTemplates = this.PackTemplates.Where(x => x.IsSelected).Select(x => x.Pack).ToList();
            //
            //Project project = TemplateManager.CreateFromTemplates(
            //    this.SelectedCoreTemplate!, this.SelectedUITemplate!, packTemplates, this.ProjectPath, this.ProjectName);
            //await project.Load();
            //project.Save();
            //await App.Metadata.ChangeCurrentProject(project);
        }
    }
}