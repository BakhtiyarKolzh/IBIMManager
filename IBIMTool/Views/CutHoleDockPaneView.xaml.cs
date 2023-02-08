using Autodesk.Revit.UI;
using IBIMTool.Core;
using IBIMTool.RevitModels;
using IBIMTool.ViewModels;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;


namespace IBIMTool.Views
{
    /// <summary> Логика взаимодействия для CutHoleDockPaneView.xaml </summary>
    public partial class CutHoleDockPaneView : Page, IDockablePaneProvider
    {
        private bool Disposed { get; set; } = false;
        private readonly ExternalEvent externalEvent;
        private readonly CutHoleDataViewModel viewModel;
        private readonly string docPath = IBIMToolHelper.DocumentPath;
        public CutHoleDockPaneView(CutHoleDataViewModel vm)
        {
            InitializeComponent();
            DataContext = viewModel = vm;
            viewModel.DockPanelView = this;
            externalEvent = CutHoleDataViewModel.RevitExternalEvent;
            viewModel = vm ?? throw new ArgumentNullException(nameof(vm));
        }


        [STAThread]
        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.InitialState = new DockablePaneState
            {
                TabBehind = DockablePanes.BuiltInDockablePanes.PropertiesPalette,
                DockPosition = DockPosition.Tabbed,
            };
            data.FrameworkElement = this;
            data.VisibleByDefault = false;
        }


        [STAThread]
        internal void RaiseExternalEvent()
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                viewModel.Dispose();
                ExternalEventRequest request = externalEvent.Raise();
                if (ExternalEventRequest.Accepted == request)
                {
                    Disposed = false;
                    viewModel.IsStarted = true;
                    SnapsToDevicePixels = true;
                }
            }, DispatcherPriority.Background);
        }


        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox chbox = sender as CheckBox;
            if (chbox.DataContext is ElementModel model)
            {
                if (model != null && model.IsValidModel())
                {
                    viewModel.ViewDataGridCollection.Refresh();
                    viewModel.VerifySelectDataViewCollection();
                    viewModel.ShowElementModelView(model);
                }
            }
        }


        private void LoadFamilyBtn_Click(object sender, RoutedEventArgs e)
        {
            RadioButton btn = sender as RadioButton;
            OpenFileDialog openDialog = new OpenFileDialog()
            {
                Filter = "Family Files (*.rfa)|*.rfa",
                Title = "Open opening symbol",
                InitialDirectory = docPath,
                CheckFileExists = true,
                ValidateNames = true,
                Multiselect = false,
            };

            if (true == openDialog.ShowDialog())
            {
                string path = openDialog.FileName;
                if (!string.IsNullOrEmpty(path))
                {
                    viewModel.LoadFamilyAsync(path);
                    btn.IsEnabled = false;
                }
            }
            else
            {
                btn.IsChecked = false;
            }
        }


        public void Dispose()
        {
            if (!Disposed)
            {
                Disposed = true;
                ActiveTitle.Content = null;
                viewModel?.Dispose();
            }
        }


    }
}