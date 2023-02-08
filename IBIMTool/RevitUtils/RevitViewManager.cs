using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using IBIMTool.RevitModels;
using IBIMTool.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Color = Autodesk.Revit.DB.Color;
using Level = Autodesk.Revit.DB.Level;
using View = Autodesk.Revit.DB.View;


namespace IBIMTool.RevitUtils
{
    internal sealed class RevitViewManager
    {

        #region GetCreate3dView
        public static View3D Create3DView(UIDocument uidoc, string viewName)
        {
            bool flag = false;
            View3D view3d = null;
            Document doc = uidoc.Document;
            ViewFamilyType vft = new FilteredElementCollector(doc)
            .OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
            .FirstOrDefault(q => q.ViewFamily == ViewFamily.ThreeDimensional);
            using (Transaction t = new Transaction(doc, "Create3DView"))
            {
                TransactionStatus status = t.Start();
                if (status == TransactionStatus.Started)
                {
                    try
                    {
                        view3d = View3D.CreateIsometric(uidoc.Document, vft.Id);
                        view3d.Name = viewName;
                        status = t.Commit();
                    }
                    catch (Exception ex)
                    {
                        status = t.RollBack();
                        IBIMLogger.Error($"Error 3Dview {ex.Message} flag => {flag}");
                    }
                    finally
                    {
                        ViewDetailLevel detail = ViewDetailLevel.Fine;
                        DisplayStyle style = DisplayStyle.RealisticWithEdges;
                        ViewDiscipline discipline = ViewDiscipline.Mechanical;
                        SetViewSettings(doc, view3d, discipline, style, detail);
                        vft.Dispose();
                    }
                }
            }
            return view3d;
        }


        public static View3D Get3dView(UIDocument uidoc, string viewName = "Preview3DView")
        {
            Document doc = uidoc.Document;
            foreach (View3D view3d in new FilteredElementCollector(doc).OfClass(typeof(View3D)))
            {
                if (!view3d.IsTemplate && view3d.Name.Equals(viewName))
                {
                    ViewDetailLevel detail = ViewDetailLevel.Fine;
                    DisplayStyle style = DisplayStyle.RealisticWithEdges;
                    ViewDiscipline discipline = ViewDiscipline.Mechanical;
                    SetViewSettings(doc, view3d, discipline, style, detail);
                    return view3d;
                }
            }
            return Create3DView(uidoc, viewName);
        }

        #endregion


        #region GetCreatePlanView

        public static ViewPlan GetPlanByLevel(UIDocument uidoc, Level level, string prefix = "Preview")
        {
            if (level == null)
            {
                return null;
            }
            Document doc = uidoc.Document;
            string viewName = prefix + level.Name.Trim();
            foreach (ViewPlan plan in new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)))
            {
                if (!plan.IsTemplate && level.Id.Equals(plan.GenLevel.Id))
                {
                    return plan;
                }
            }
            return CreatePlanView(doc, level, viewName);
        }


        public static ViewPlan CreatePlanView(Document doc, Level level, string name)
        {
            ViewPlan floorPlan = null;
            IBIMLogger.Log("ViewPlan name: " + name);
            TransactionManager.CreateTransaction(doc, "CreateFloorPlan", () =>
            {
                ViewFamilyType vft = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
                .FirstOrDefault(x => ViewFamily.FloorPlan == x.ViewFamily);
                floorPlan = ViewPlan.Create(doc, vft.Id, level.Id);
                floorPlan.DisplayStyle = DisplayStyle.ShadingWithEdges;
                floorPlan.Discipline = ViewDiscipline.Coordination;
                floorPlan.DetailLevel = ViewDetailLevel.Fine;
                floorPlan.Name = name;
            });

            return floorPlan;
        }

        #endregion


        #region SetViewSettings

        public static void SetViewSettings(Document doc, View view, ViewDiscipline discipline, DisplayStyle style, ViewDetailLevel detail)
        {
            TransactionManager.CreateTransaction(doc, "SetViewSettings", () =>
            {
                view.ViewTemplateId = ElementId.InvalidElementId;
                view.Discipline = discipline;
                view.DisplayStyle = style;
                view.DetailLevel = detail;
                if (view is View3D view3D)
                {
                    view3D.IsSectionBoxActive = false;
                }
            });
        }

        #endregion


        #region ShowModelInPlanView
        public static void ShowModelInPlanView(UIDocument uidoc, in ElementModel model, ViewDiscipline discipline)
        {
            Document doc = uidoc.Document;
            Level level = model.HostLevel;
            ViewPlan viewPlan = GetPlanByLevel(uidoc, level: level);
            if (viewPlan != null && model.Element.IsValidObject)
            {
                try
                {
                    double cutPlane = 1200 / 304.8;
                    double offsetPlane = 300 / 304.8;

                    PlanViewRange viewRange = viewPlan.GetViewRange();

                    Element topLevel = doc.GetElement(viewRange.GetLevelId(PlanViewPlane.TopClipPlane));
                    Element botLevel = doc.GetElement(viewRange.GetLevelId(PlanViewPlane.BottomClipPlane));

                    if (topLevel is Level && botLevel is Level && topLevel.Id != botLevel.Id)
                    {
                        using (Transaction trx = new Transaction(doc, "SetViewRange"))
                        {
                            viewRange.SetOffset(PlanViewPlane.CutPlane, cutPlane);
                            viewRange.SetOffset(PlanViewPlane.TopClipPlane, offsetPlane);
                            viewRange.SetOffset(PlanViewPlane.BottomClipPlane, -offsetPlane);
                            viewRange.SetOffset(PlanViewPlane.ViewDepthPlane, -offsetPlane);
                            TransactionStatus status = trx.Start();
                            viewPlan.SetViewRange(viewRange);
                            status = trx.Commit();
                        }
                    }
                }
                finally
                {
                    ActivateView(uidoc, viewPlan, discipline);
                    SetCustomZoomBox(uidoc, viewPlan, model);
                    uidoc.RefreshActiveView();
                }
            }
        }


        public static void ShowElements(UIDocument uidoc, IList<ElementId> elems)
        {
            if (elems.Any())
            {
                uidoc.Selection.Dispose();
                uidoc.ShowElements(elems);
            }
        }

        #endregion


        #region ActivateView
        public static void ActivateView(UIDocument uidoc, in View view, ViewDiscipline discipline)
        {
            ElementId activeId = uidoc.ActiveGraphicalView.Id;
            if (view != null && activeId != view.Id)
            {
                uidoc.Selection.Dispose();
                uidoc.RequestViewChange(view);
                ViewDetailLevel detail = ViewDetailLevel.Fine;
                DisplayStyle style = DisplayStyle.ShadingWithEdges;
                SetViewSettings(uidoc.Document, view, discipline, style, detail);
                foreach (UIView uv in uidoc.GetOpenUIViews())
                {
                    if (activeId != uv.ViewId)
                    {
                        try
                        {
                            uv.Close();
                            uv.Dispose();
                        }
                        catch (Exception ex)
                        {
                            IBIMLogger.Error(ex.Message);
                        }
                    }
                }
            }
        }

        #endregion


        #region SetCustomSectionBox
        public static void SetCustomSectionBox(UIDocument uidoc, XYZ centroid, View3D view3d, double offset)
        {
            if (view3d != null && view3d.IsValidObject)
            {
                TransactionManager.CreateTransaction(uidoc.Document, "SetSectionBox", () =>
                {
                    BoundingBoxXYZ bbox = CreateBoundingBox(centroid, offset);
                    ZoomElementInView(uidoc, view3d, bbox);
                    view3d.CropBoxVisible = false;
                    view3d.CropBoxActive = false;
                    view3d.SetSectionBox(bbox);
                });
            }
        }
        #endregion


        #region SetCustomZoomBox
        public static BoundingBoxXYZ SetCustomZoomBox(UIDocument uidoc, in View view, in ElementModel model)
        {
            BoundingBoxXYZ bbox = CreateBoundingBox(view, model.Element, model.SectionPlane.Origin);
            ZoomElementInView(uidoc, view, bbox);
            return bbox;
        }
        #endregion


        #region ZoomElementInView
        private static void ZoomElementInView(UIDocument uidoc, View view, BoundingBoxXYZ box)
        {
            UIView uiview = uidoc.GetOpenUIViews().Cast<UIView>().FirstOrDefault(v => v.ViewId.Equals(view.Id));
            if (box != null && box.Enabled)
            {
                uiview?.ZoomAndCenterRectangle(box.Min, box.Max);
            }
        }
        #endregion


        #region CreateBoundingBox
        private static BoundingBoxXYZ CreateBoundingBox(XYZ centroid, double offset = 7)
        {
            BoundingBoxXYZ bbox = new BoundingBoxXYZ();
            XYZ vector = new XYZ(offset, offset, offset);
            bbox.Min = centroid - vector;
            bbox.Max = centroid + vector;
            bbox.Enabled = true;
            return bbox;
        }


        private static BoundingBoxXYZ CreateBoundingBox(View view, Element element, XYZ centroid, double offset = 7)
        {
            BoundingBoxXYZ bbox = element.get_BoundingBox(view);
            if (bbox != null && bbox.Enabled)
            {
                bbox.Min = new XYZ(centroid.X - offset, centroid.Y - offset, bbox.Min.Z);
                bbox.Max = new XYZ(centroid.X + offset, centroid.Y + offset, bbox.Max.Z);
            }
            else
            {
                bbox = CreateBoundingBox(centroid, offset);
                bbox.Min = new XYZ(bbox.Min.X, bbox.Min.Y, view.Origin.Z);
                bbox.Max = new XYZ(bbox.Max.X, bbox.Max.Y, view.Origin.Z);
            }
            return bbox;
        }

        #endregion


        #region SetCategoryTransparency
        public static void SetCategoryTransparency(Document doc, View3D view, Category category, int transparency = 15, bool halftone = false)
        {
            ElementId catId = category.Id;
            OverrideGraphicSettings graphics;
            if (view.IsCategoryOverridable(catId))
            {
                graphics = new OverrideGraphicSettings();
                graphics = graphics.SetHalftone(halftone);
                graphics = graphics.SetSurfaceTransparency(transparency);
                TransactionManager.CreateTransaction(doc, "Override", () =>
                {
                    view.SetCategoryOverrides(catId, graphics);
                });
            }
        }

        #endregion


        #region SetCustomColor
        public static ElementId GetSolidFillPatternId(Document doc)
        {
            ElementId solidFillPatternId = null;
            foreach (FillPatternElement fp in new FilteredElementCollector(doc).WherePasses(new ElementClassFilter(typeof(FillPatternElement))))
            {
                FillPattern pattern = fp.GetFillPattern();
                if (pattern != null && pattern.IsSolidFill)
                {
                    solidFillPatternId = fp.Id;
                    break;
                }
            }
            return solidFillPatternId;
        }


        public static void SetCustomColor(UIDocument uidoc, View3D view, Element elem)
        {
            Document doc = uidoc.Document;
            Color color = new Color(255, 0, 0);
            OverrideGraphicSettings graphics = new OverrideGraphicSettings();
            ElementId patternId = GetSolidFillPatternId(doc);
            if (!view.AreGraphicsOverridesAllowed())
            {
                IBIMLogger.Error($"Graphic overrides are not alowed for '{view.Name}' view3d");
            }
            else
            {
                graphics = graphics.SetSurfaceForegroundPatternVisible(true);
                graphics = graphics.SetSurfaceBackgroundPatternVisible(true);
                graphics = graphics.SetSurfaceForegroundPatternColor(color);
                graphics = graphics.SetSurfaceBackgroundPatternColor(color);
                graphics = graphics.SetSurfaceForegroundPatternId(patternId);
                graphics = graphics.SetSurfaceBackgroundPatternId(patternId);

                TransactionManager.CreateTransaction(doc, "OverrideColor", () =>
                {
                    view.SetElementOverrides(elem.Id, graphics);
                });
            }
        }

        #endregion


        #region ViewFilter

        public static void CreateViewFilter(Document doc, View view, Element elem, ElementFilter filter)
        {
            string filterName = "Filter" + elem.Name;
            OverrideGraphicSettings ogSettings = new OverrideGraphicSettings();
            IList<ElementId> categories = CheckFilterableCategoryByElement(elem);
            ParameterFilterElement prmFilter = ParameterFilterElement.Create(doc, filterName, categories, filter);
            ogSettings = ogSettings.SetProjectionLineColor(new Color(255, 0, 0));
            view.SetFilterOverrides(prmFilter.Id, ogSettings);
        }


        public static void ShowFilterableParameters(Document doc, Element elem)
        {
            IList<ElementId> categories = new List<ElementId>() { elem.Category.Id };
            StringBuilder builder = new StringBuilder("FilterableParametrsByElement");
            foreach (ElementId prmId in ParameterFilterUtilities.GetFilterableParametersInCommon(doc, categories))
            {
                builder.AppendLine(LabelUtils.GetLabelFor((BuiltInParameter)prmId.IntegerValue));
            }
            IBIMLogger.Info(builder.ToString());
            builder.Clear();
        }


        //ParameterValueProvider provider = new ParameterValueProvider(new ElementId((int)BuiltInParameter.ID_PARAM));
        //FilterElementIdRule rule = new FilterElementIdRule(provider, new FilterNumericEquals(), view.Id);


        private static IList<ElementId> CheckFilterableCategoryByElement(Element elem)
        {
            ICollection<ElementId> catIds = ParameterFilterUtilities.GetAllFilterableCategories();
            IList<ElementId> categories = new List<ElementId>();
            foreach (ElementId catId in catIds)
            {
                if (elem.Category.Id == catId)
                {
                    categories.Add(catId);
                    break;
                }
            }
            return categories;
        }


        #endregion

    }
}