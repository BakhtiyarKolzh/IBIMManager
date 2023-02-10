using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using IBIMTool.Authorization;
using IBIMTool.Core;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Threading;

namespace IBIMTool.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal class InfoCommand : IExternalCommand, IExternalCommandAvailability
    {
        private static int counter = Properties.Settings.Default.Countdemo;
        private readonly IBIMToolHelper toolHelper = IBIMToolApp.Host.Services.GetRequiredService<IBIMToolHelper>();
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            AuthentificationViewModel auto = new AuthentificationViewModel();

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            toolHelper.IsActive = counter < 0 || auto.StartValidateActivation();
            Properties.Settings.Default.Countdemo = counter += 1;
            Properties.Settings.Default.Save();

            return Result.Succeeded;
        }


        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories)
        {
            return true;
        }


        public static string GetPath()
        {
            return typeof(InfoCommand).FullName;
        }
    }
}
