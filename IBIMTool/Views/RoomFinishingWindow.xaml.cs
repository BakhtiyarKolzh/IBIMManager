using Autodesk.Revit.UI;
using IBIMTool.RevitUtils;
using IBIMTool.ViewModels;
using System;
using System.Windows;
using System.Windows.Threading;


namespace IBIMTool.Views
{
    /// <summary>
    /// Логика взаимодействия для RoomFinishingWindow.xaml
    /// </summary>
    public partial class RoomFinishingWindow : Window
    {
        private readonly ExternalEvent externalEvent;
        private readonly FinishingViewModel viewModel;
        public RoomFinishingWindow(FinishingViewModel vm)
        {
            InitializeComponent();
            this.SetOwnerWindow();
            DataContext = viewModel = vm;
            externalEvent = FinishingViewModel.RevitExternalEvent;
            viewModel = vm ?? throw new ArgumentNullException(nameof(viewModel));
            Loaded += OnWindow_Loaded;
        }


        private void OnWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.CurrentDispatcher.Invoke(() =>
            {
                ExternalEventRequest request = externalEvent.Raise();
                if (ExternalEventRequest.Accepted == request)
                {
                    viewModel.GetValidRooms();
                }
            }, DispatcherPriority.Background);
        }

    }
}