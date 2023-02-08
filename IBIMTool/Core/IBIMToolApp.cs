using Authorization;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using IBIMTool.CutOpening;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Revit.Async;
using System;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;


namespace IBIMTool.Core
{

    public sealed class IBIMToolApp : IExternalApplication
    {
        public static IHost Host { get; private set; }
        private static IBIMToolHelper toolHelper { get; set; }
        private static UIControlledApplication uicontrol { get; set; }
        private static IDockablePaneProvider paneProvider { get; set; }

        private CutHoleRegisterDockPane paneRegister = null;

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
            toolHelper.IsActive = Authentification.ValidateActivation();
            toolHelper = Host.Services.GetRequiredService<IBIMToolHelper>();
            paneProvider = Host.Services.GetRequiredService<IDockablePaneProvider>();
            paneRegister = Host.Services.GetRequiredService<CutHoleRegisterDockPane>();
            if (paneRegister.RegisterDockablePane(uicontrol, toolHelper.CutVoidPaneId, paneProvider))
            {
                if (RenderOptions.ProcessRenderMode.Equals(RenderMode.SoftwareOnly))
                {
                    RenderOptions.ProcessRenderMode = RenderMode.Default;
                }
            }
        }


        public Result OnShutdown(UIControlledApplication uicontrol)
        {
            uicontrol.ControlledApplication.ApplicationInitialized -= OnApplicationInitialized;
            return Result.Succeeded;
        }


    }
}