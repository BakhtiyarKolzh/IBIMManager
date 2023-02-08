using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using IBIMTool.Core;
using IBIMTool.Services;
using IBIMTool.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Globalization;
using System.Threading;


namespace IBIMTool.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class CutHoleShowPanelCommand : IExternalCommand, IExternalCommandAvailability
    {
        private readonly IBIMToolHelper toolHelper = IBIMToolApp.Host.Services.GetRequiredService<IBIMToolHelper>();
        private readonly IDockablePaneProvider paneProvider = IBIMToolApp.Host.Services.GetRequiredService<IDockablePaneProvider>();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            return Execute(commandData.Application, ref message);
        }


        public Result Execute(UIApplication uiapp, ref string message)
        {
            Result result = Result.Succeeded;
            try
            {
                DockablePane pane = uiapp.GetDockablePane(toolHelper.CutVoidPaneId);
                if (paneProvider is CutHoleDockPaneView view && pane != null)
                {
                    if (pane.IsShown())
                    {
                        pane.Hide();
                        view.Dispose();
                    }
                    else
                    {
                        pane.Show();
                        view.RaiseExternalEvent();
                    }
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                IBIMLogger.Error(message);
                result = Result.Failed;
            }

            return result;
        }


        [STAThread]
        public bool IsCommandAvailable(UIApplication uiapp, CategorySet catSet)
        {
            View view = uiapp.ActiveUIDocument?.ActiveGraphicalView;
            return view != null || !(view is ViewSchedule);
        }


        public static string GetPath()
        {
            return typeof(CutHoleShowPanelCommand).FullName;
        }
    }
}