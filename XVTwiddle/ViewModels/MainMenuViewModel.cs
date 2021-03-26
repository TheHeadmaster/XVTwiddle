using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AvalonDock.Layout;
using DynamicData;
using DynamicData.Binding;
using XVTwiddle.Controls;
using XVTwiddle.UI;
using XVTwiddle.ViewModels.Pages;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;

namespace XVTwiddle.ViewModels
{
    /// <summary>
    /// The ViewModel for the <see cref="MainMenu"/> control.
    /// </summary>
    public class MainMenuViewModel : ReactiveObject
    {
        private readonly ReadOnlyObservableCollection<RecentItem> recentlyOpenedProjects;

        /// <summary>
        /// Closes the program.
        /// </summary>
        public ReactiveCommand<Unit, Unit> Close { get; } = ReactiveCommand.CreateFromTask(x => App.Metadata.Close());

        /// <summary>
        /// Opens a project.
        /// </summary>
        public ReactiveCommand<string, Unit> OpenProject { get; }

        /// <summary>
        /// Gets a list of recently opened projects.
        /// </summary>
        public ReadOnlyObservableCollection<RecentItem> RecentlyOpenedProjects => this.recentlyOpenedProjects;

        /// <summary>
        /// Creates a new <see cref="MainMenuViewModel"/>.
        /// </summary>
        public MainMenuViewModel()
        {
            App.Preferences.RecentlyOpenedProjects
                .Connect()
                .Sort(SortExpressionComparer<RecentItem>.Descending(x => x.Timestamp))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out this.recentlyOpenedProjects)
                .Subscribe();

            IObservable<bool> canOpenPage = App.Metadata.WhenAnyValue(x => x.IsRunning, x => x.CurrentProject, (x, y) => x)
                .Select(isRunning => !isRunning && App.Metadata.CurrentProject is { });

            this.OpenProject = ReactiveCommand.CreateFromTask<string>(x => App.Metadata.OpenProject(x));
            this.Close = ReactiveCommand.CreateFromTask(x => App.Metadata.Close());
        }
    }
}