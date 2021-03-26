using System;
using System.Collections.Generic;
using System.Text;
using XVTwiddle.Controls;
using XVTwiddle.UI;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace XVTwiddle.ViewModels
{
    /// <summary>
    /// The ViewModel for the <see cref="MainStatusBar"/>.
    /// </summary>
    public class MainStatusBarViewModel : ReactiveObject
    {
        public StatusBarColor Color { [ObservableAsProperty]get; } = null!;

        public bool IsIndeterminate { [ObservableAsProperty]get; }

        /// <summary>
        /// Gets the app meta.
        /// </summary>
        public Meta Metadata => App.Metadata;

        public double ProgressPercentage { [ObservableAsProperty]get; }

        public string StatusBarMessage { [ObservableAsProperty]get; } = "";

        public MainStatusBarViewModel()
        {
            this.Metadata.WhenAnyValue(x => x.StatusBarColor)
                .ToPropertyEx(this, x => x.Color);

            this.Metadata.WhenAnyValue(x => x.StatusBarMessage)
                .ToPropertyEx(this, x => x.StatusBarMessage);

            this.Metadata.WhenAnyValue(x => x.ProgressIsIndeterminate)
                .ToPropertyEx(this, x => x.IsIndeterminate);

            this.Metadata.WhenAnyValue(x => x.ProgressPercentage)
                .ToPropertyEx(this, x => x.ProgressPercentage);
        }
    }
}