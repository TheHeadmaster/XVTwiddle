#nullable disable

using Librarium.WPF.Windows;
using XVTwiddle.ViewModels;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using XVTwiddle.UI;

namespace XVTwiddle.Windows
{
    /// <summary>
    /// The <see cref="NewProjectWindow"/> allows the user to create a new project from a template.
    /// </summary>
    public partial class NewProjectWindow : BorderlessReactiveWindow<NewProjectWindowViewModel>
    {
        public NewProjectWindow()
        {
            this.InitializeComponent();

            this.ViewModel = new NewProjectWindowViewModel()
            {
                Parent = this,
                ProjectName = "My Project",
                ProjectPath = @"C:\"
            };

            this.ProjectNameTextBox.Watermark = "My Project";
            this.ProjectPathTextBox.Watermark = @"C:\";

            this.WhenActivated(dispose =>
            {
                this.Bind(this.ViewModel,
                    vm => vm.ProjectName,
                    view => view.ProjectNameTextBox.Text)
                .DisposeWith(dispose);

                this.OneWayBind(this.ViewModel,
                    vm => vm.ProjectNameValidationText,
                    view => view.ProjectNameValidation.Text)
                .DisposeWith(dispose);

                this.Bind(this.ViewModel,
                    vm => vm.ProjectPath,
                    view => view.ProjectPathTextBox.Text)
                .DisposeWith(dispose);

                this.OneWayBind(this.ViewModel,
                    vm => vm.ProjectPathValidationText,
                    view => view.ProjectPathValidation.Text)
                .DisposeWith(dispose);

                this.BindCommand(this.ViewModel,
                    vm => vm.CreateProject,
                    view => view.TemplateWizard,
                    nameof(this.TemplateWizard.Finish))
                .DisposeWith(dispose);

                this.BindCommand(this.ViewModel,
                    vm => vm.BrowseFolder,
                    view => view.BrowseButton)
                .DisposeWith(dispose);
            });

            this.Closed += this.NewProjectWindow_Closed;
            App.Metadata.StatusBarColor = StatusBarColor.Processing;
            App.Metadata.StatusBarMessage = "Window Modal";
        }

        private void NewProjectWindow_Closed(object sender, EventArgs args)
        {
            App.Metadata.StatusBarColor = StatusBarColor.Idle;
            App.Metadata.StatusBarMessage = "Ready";
        }
    }
}