using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using IBIMTool.RevitExtensions;
using IBIMTool.RevitUtils;
using IBIMTool.Services;
using System;
using System.Linq;
using Document = Autodesk.Revit.DB.Document;
using Line = Autodesk.Revit.DB.Line;


namespace IBIMTool.Commands
{

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    internal sealed class AutoJoinGeometryCommand : IExternalCommand, IExternalCommandAvailability
    {
        public const int MaxWitdhInMm = 50;
        Result IExternalCommand.Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Level level = uidoc.ActiveView.GenLevel;
            Document doc = uidoc.Document;

            if (level is null || !level.IsValidObject)
            {
                message = "HostLevel not Valid";
                return Result.Failed;
            }

            int counter = 0;
            FilteredElementCollector collector;
            XYZ offset = new XYZ(0.005, 0.005, 0.005);
            BuiltInCategory bip = BuiltInCategory.OST_Walls;
            WarningSwallower transactionWarning = new WarningSwallower();
            ElementLevelFilter level1Filter = new ElementLevelFilter(level.Id);
            double nativeMaxWitdh = UnitUtils.ConvertToInternalUnits(MaxWitdhInMm, DisplayUnitType.DUT_MILLIMETERS);
            collector = new FilteredElementCollector(doc, uidoc.ActiveView.Id).OfClass(typeof(Wall)).OfCategory(bip);
            collector = collector.WhereElementIsNotElementType();
            foreach (Wall walltrg in collector.ToElements())
            {
                if (walltrg.FindInserts(true, true, true, true).Any())
                {
                    BoundingBoxXYZ bb = walltrg.get_BoundingBox(null);
                    Outline outline = new Outline(bb.Min -= offset, bb.Max += offset);
                    collector = RevitFilterManager.GetElementsOfCategory(doc, typeof(Wall), bip, true);
                    collector = collector.WherePasses(new BoundingBoxIntersectsFilter(outline));
                    foreach (Wall wallsrs in collector.WherePasses(level1Filter).ToElements())
                    {
                        if (JoinGeometryUtils.AreElementsJoined(doc, walltrg, wallsrs)) { continue; }

                        WallType wallType = doc.GetElement(wallsrs.GetTypeId()) as WallType;

                        if (nativeMaxWitdh < wallType.Width) { continue; }

                        Line lineTrg = (walltrg.Location as LocationCurve).Curve as Line;
                        Line lineSrs = (wallsrs.Location as LocationCurve).Curve as Line;

                        XYZ normal1 = lineTrg.Direction.DumbToPositive();
                        XYZ normal2 = lineSrs.Direction.DumbToPositive();

                        if (normal1.IsAlmostEqualTo(normal2))
                        {
                            using Transaction trx = new Transaction(doc);
                            TransactionStatus status = TransactionStatus.Started;
                            FailureHandlingOptions failOpt = trx.GetFailureHandlingOptions();
                            failOpt = failOpt.SetFailuresPreprocessor(transactionWarning);
                            failOpt = failOpt.SetClearAfterRollback(true);
                            trx.SetFailureHandlingOptions(failOpt);
                            if (status == trx.Start("JoinWall"))
                            {
                                try
                                {
                                    JoinGeometryUtils.JoinGeometry(doc, walltrg, wallsrs);
                                    status = trx.Commit();
                                    counter++;
                                }
                                catch (Exception ex)
                                {
                                    IBIMLogger.Log(ex.Message);
                                    if (!trx.HasEnded())
                                    {
                                        status = trx.RollBack();
                                        trx.Dispose();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            string errors = transactionWarning.GetWarningMessage();
            IBIMLogger.Info($"Successfully Completed!\nJoined walls: {counter} count\n" + errors);
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
            return typeof(AutoJoinGeometryCommand).FullName;
        }

    }
}