using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Revit.Async;
using System;
using System.Windows.Threading;


namespace IBIMTool.Core
{

    public sealed class IBIMToolApp : IExternalApplication
    {
        public static IHost Host { get; private set; }
        private static IBIMToolHelper toolHelper { get; set; }
        private static UIControlledApplication uicontrol { get; set; }
        private static IDockablePaneProvider paneProvider { get; set; }


        public Result OnStartup(UIControlledApplication controlledApp)
        {
            uicontrol = controlledApp;
            SetupUIPanel.Initialize(controlledApp);
            Host = ContainerConfig.ConfigureServices();
            Dispatcher.CurrentDispatcher.Thread.Name = "RevitGeneralThread";
            controlledApp.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;
            RevitTask.Initialize(controlledApp);
            return Result.Succeeded;
        }


        [STAThread]
        private void OnApplicationInitialized(object sender, ApplicationInitializedEventArgs e)
        {
            toolHelper = Host.Services.GetRequiredService<IBIMToolHelper>();
        }


        public Result OnShutdown(UIControlledApplication uicontrol)
        {
            uicontrol.ControlledApplication.ApplicationInitialized -= OnApplicationInitialized;
            return Result.Succeeded;
        }


    }
}