#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Librarium.WPF.Windows;
using XVTwiddle.UI;
using XVTwiddle.ViewModels;
using XVTwiddle;
using ReactiveUI;

namespace XVTwiddle.Windows
{
    /// <summary>
    /// The main window for the program.
    /// </summary>
    public partial class MainWindow : BorderlessReactiveWindow<MainWindowViewModel>
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.ViewModel = new MainWindowViewModel();

            this.WhenActivated(dispose =>
            {
                this.OneWayBind(this.ViewModel,
                    vm => vm.Title,
                    view => view.Title)
                .DisposeWith(dispose);

                this.BindHotkey(this.ViewModel,
                    vm => vm.Close,
                    new KeyGesture(Key.F4, ModifierKeys.Alt));

                this.BindHotkey(this.ViewModel,
                    vm => vm.OpenProject,
                    new KeyGesture(Key.O, ModifierKeys.Control | ModifierKeys.Shift));
            });
        }

        private async void WindowClosing(object sender, System.ComponentModel.CancelEventArgs args)
        {
            bool? saveResult = await App.Metadata.PromptToSave();
            if (saveResult is null)
            {
                args.Cancel = true;
                return;
            }
            else if (saveResult is true)
            {
                App.Metadata.CurrentProject!.Save();
            }
            Application.Current.Shutdown();
        }
    }
}