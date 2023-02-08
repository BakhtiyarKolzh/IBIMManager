using Autodesk.Revit.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Revit.Async;
using IBIMTool.Commands;
using IBIMTool.CutOpening;
using IBIMTool.RevitUtils;
using IBIMTool.ViewModels;
using IBIMTool.Views;
using IBIMTool.RevitEventHandlers;

namespace IBIMTool.Core
{
    public sealed class ContainerConfig
    {
        public static IHost ConfigureServices()
        {
            IHost host = new HostBuilder()
            .ConfigureServices((hostContext, services) =>
            {
                // Singleton
                services.AddSingleton<RevitTask>();
                services.AddSingleton<IBIMToolHelper>();
                services.AddSingleton<IDockablePaneProvider, CutHoleDockPaneView>();

                // Transient
                services.AddTransient<SettingsEventHandler>();

                // CutOpenningManager
                services.AddTransient<CreateOpeningHandler>();
                services.AddTransient<CutHoleDataViewModel>();
                services.AddTransient<CutHoleRegisterDockPane>();
                services.AddTransient<CutHoleShowPanelCommand>();
                services.AddTransient<CutHoleCollisionManager>();
                services.AddTransient<RevitPurginqManager>();
                services.AddTransient<PreviewDialogWindow>();
                services.AddTransient<PreviewViewModel>();

                // RoomFinishing
                services.AddTransient<FinishingViewModel>();
                services.AddTransient<RoomFinishingWindow>();

                // AreaRebarMarkFix
                services.AddTransient<AreaRebarMarkViewModel>();
                services.AddTransient<AreaRebarMarkFixWindow>();

            }).Build();

            return host;
        }
    }
}
