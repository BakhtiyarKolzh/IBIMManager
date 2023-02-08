using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using IBIMTool.RevitExtensions;
using IBIMTool.RevitModels;
using IBIMTool.RevitUtils;
using IBIMTool.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;
using Document = Autodesk.Revit.DB.Document;

namespace IBIMTool.RevitEventHandlers
{
    public sealed class CreateOpeningHandler : RevitEventWrapper<PreviewDialogWindow>, IDisposable
    {
        private ElementModel model = null;
        private FamilySymbol symbol = null;
        private FamilyInstance opening = null;

        private readonly double roundMin = Math.Round(5 / 304.8, 7);
        private readonly double roundMax = Math.Round(50 / 304.8, 7);
        private readonly StructuralType stype = StructuralType.NonStructural;
        private readonly string symbolUId = Properties.Settings.Default.OpeningUId;
        private readonly string widthPrmName = Properties.Settings.Default.WidthValParam;
        private readonly string hightPrmName = Properties.Settings.Default.HightValParam;
        private readonly string sectionPrmName = Properties.Settings.Default.ProjectSectionParam;
        private readonly string elevOfRefPrmName = Properties.Settings.Default.ElevateOfBaseParam;
        private readonly string levelElevatPrmName = Properties.Settings.Default.LevelElevationParam;


        private readonly double offset = Convert.ToDouble(Properties.Settings.Default.CutOffsetInMm / 304.8);

        public override void Execute(UIApplication app, PreviewDialogWindow window)
        {
            model = window.ViewModel.ElementModel;
            Document doc = app.ActiveUIDocument.Document;
            if (opening == null && CreateOpening(doc))
            {
                window.LabelMsg.Content = "Successfully completed";
                window.LabelMsg.Foreground = Brushes.DarkBlue;
            }
            else if (opening != null && opening.IsValidObject)
            {
                DeleteOpening(doc, opening.Id);
            }
        }


        private bool CreateOpening(Document doc)
        {
            bool result = false;
            Level level = model.HostLevel;
            Element host = doc.GetElement(model.HostUniqueId);
            MidpointRounding rounding = MidpointRounding.AwayFromZero;
            TransactionManager.CreateTransaction(doc, "Create opening", () =>
            {
                symbol = RevitFamilyManager.GetFamilySymbol(doc, symbolUId);
                Debug.Assert(host != null && host.IsValidObject, "Host invalid object");
                Debug.Assert(level != null && level.IsValidObject, "Level invalid object");
                Debug.Assert(symbol != null && symbol.IsValidObject, "Symbol invalid object");
                opening = doc.Create.NewFamilyInstance(model.Centroid, symbol, host, level, stype);
                opening = opening ?? throw new ArgumentNullException("Opening could not be created!!!");
                Parameter elevatParam = opening.get_Parameter(BuiltInParameter.INSTANCE_ELEVATION_PARAM);
                double width = Math.Round((model.Width + (offset * 2)) / roundMax, rounding) * roundMax;
                double hight = Math.Round((model.Hight + (offset * 2)) / roundMax, rounding) * roundMax;
                List<string> names = new List<string> { widthPrmName, hightPrmName, elevOfRefPrmName };
                if (elevatParam != null && names.All(str => !string.IsNullOrWhiteSpace(str)))
                {
                    string section = model.ProjectSection;
                    double elevation = elevatParam.AsDouble();
                    elevation = Math.Round((elevation - (hight * 0.5)) / roundMin) * roundMin;
                    bool elevatSet = opening.SetParamValueByName(elevOfRefPrmName, elevation);
                    bool heightSet = opening.SetParamValueByName(hightPrmName, elevation);
                    if (heightSet && elevatSet && elevatParam.Set(0))
                    {
                        elevation = level.ProjectElevation;
                        result = opening.SetParamValueByName(levelElevatPrmName, elevation);
                        result = opening.SetParamValueByName(sectionPrmName, section);
                        result = opening.SetParamValueByName(widthPrmName, width);
                        result = opening.SetParamValueByName(hightPrmName, hight);
                    }
                }

            });
            return result;
        }


        private void DeleteOpening(Document doc, ElementId id)
        {
            TransactionManager.CreateTransaction(doc, "DeleteOpening", () =>
            {
                if (id != null && 0 < id.IntegerValue)
                {
                    Debug.Assert(doc.Delete(id).Any());
                }
            });
        }

        public void Dispose()
        {
            model?.Dispose();
            symbol?.Dispose();
            opening?.Dispose();
        }


        //private void CreateSolidBox(Document doc, ElementModel model)
        //{
        //    if (model != null && model.IsValidModel())
        //    {
        //        IList<CurveLoop> profile = GeometryExtension.GetSectionProfile(model, offset);
        //        Solid solid = profile.CreateExtrusionGeometry(normal, model.Depth);
        //        solid.CreateDirectShape(doc);
        //    }
        //}





    }
}
