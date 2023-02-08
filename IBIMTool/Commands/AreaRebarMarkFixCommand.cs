using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using IBIMTool.Views;
using Microsoft.Extensions.DependencyInjection;
using IBIMTool.Core;
using System;
using System.Globalization;
using System.Threading;


namespace IBIMTool.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed partial class AreaRebarMarkFixCommand : IExternalCommand, IExternalCommandAvailability
    {
        private readonly AreaRebarMarkFixWindow window = IBIMToolApp.Host.Services.GetRequiredService<AreaRebarMarkFixWindow>();
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            try
            {
                window.Show();
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }


        [STAThread]
        public bool IsCommandAvailable(UIApplication uiapp, CategorySet selectedCategories)
        {
            View view = uiapp.ActiveUIDocument?.ActiveGraphicalView;
            return view is ViewPlan;
        }


        public static string GetPath()
        {
            return typeof(AreaRebarMarkFixCommand).FullName;
        }


    }
}