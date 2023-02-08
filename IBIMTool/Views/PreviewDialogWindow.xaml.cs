using IBIMTool.RevitEventHandlers;
using IBIMTool.RevitUtils;
using IBIMTool.ViewModels;
using System;
using System.Windows;
using System.Windows.Threading;


namespace IBIMTool.Views
{
    /// <summary>
    /// Логика взаимодействия для PreviewDialogWindow.xaml
    /// </summary>
    /// 
    public partial class PreviewDialogWindow : Window
    {
        public PreviewViewModel ViewModel { get; set; }

        private readonly CreateOpeningHandler eventHandler;
        public PreviewDialogWindow(CreateOpeningHandler handler)
        {
            eventHandler = handler;
            Loaded += PreviewWindow_Loaded;
            InitializeComponent();
            this.SetOwnerWindow();
        }


        [STAThread]
        private void PreviewWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = ViewModel ?? throw new ArgumentNullException(nameof(ViewModel));
            Dispatcher.Invoke(new Action(() =>
            {
                Loaded -= PreviewWindow_Loaded;
                ViewModel.ChangeViewHandler(false);
                eventHandler.Raise(this);
                Activate();
            }));
        }


        private void CutOpeningCmd_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                ViewModel?.Dispose();
            }));
        }


        private void CloseCmd_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                ViewModel?.Dispose();
                eventHandler.Raise(this);
            }));
        }

    }
}
