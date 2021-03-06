#nullable disable

using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using XVTwiddle.ViewModels;
using ReactiveUI;

namespace XVTwiddle.Controls
{
    /// <summary>
    /// Interaction logic for MainStatusBar.xaml
    /// </summary>
    public partial class MainStatusBar : ReactiveUserControl<MainStatusBarViewModel>
    {
        /// <summary>
        /// Creates a new <see cref="MainStatusBar"/>.
        /// </summary>
        public MainStatusBar()
        {
            this.InitializeComponent();

            this.ViewModel = new MainStatusBarViewModel();

            this.WhenActivated(dispose =>
            {
                this.OneWayBind(this.ViewModel,
                    vm => vm.StatusBarMessage,
                    view => view.StatusBarMessage.Text)
                .DisposeWith(dispose);

                this.OneWayBind(this.ViewModel,
                    vm => vm.Color.Color,
                    view => view.StatusBarBorder.Background)
                .DisposeWith(dispose);

                this.OneWayBind(this.ViewModel,
                    vm => vm.Color.Foreground,
                    view => view.StatusBarMessage.Foreground)
                .DisposeWith(dispose);

                this.OneWayBind(this.ViewModel,
                    vm => vm.ProgressPercentage,
                    view => view.StatusBarProgress.Value)
                .DisposeWith(dispose);

                this.OneWayBind(this.ViewModel,
                    vm => vm.IsIndeterminate,
                    view => view.StatusBarProgress.IsIndeterminate)
                .DisposeWith(dispose);
            });
        }
    }
}