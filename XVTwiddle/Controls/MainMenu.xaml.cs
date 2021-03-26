#nullable disable

using XVTwiddle.ViewModels;
using XVTwiddle.ViewModels.Pages;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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

namespace XVTwiddle.Controls
{
    /// <summary>
    /// Interaction logic for MainMenu.xaml
    /// </summary>
    public partial class MainMenu : ReactiveUserControl<MainMenuViewModel>
    {
        /// <summary>
        /// Creates a new <see cref="MainMenu"/>.
        /// </summary>
        public MainMenu()
        {
            this.ViewModel = new MainMenuViewModel();

            this.InitializeComponent();
            this.WhenActivated(dispose =>
            {
                this.OneWayBind(this.ViewModel,
                    vm => vm.RecentlyOpenedProjects,
                    view => view.RecentlyOpenedProjectsMenuItem.ItemsSource)
                .DisposeWith(dispose);

                this.BindCommand(this.ViewModel,
                    vm => vm.OpenProject,
                    view => view.OpenProjectMenuItem)
                .DisposeWith(dispose);

                this.BindCommand(this.ViewModel,
                    vm => vm.Close,
                    view => view.CloseMenuItem)
                .DisposeWith(dispose);
            });
        }
    }
}