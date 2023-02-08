using Autodesk.Revit.DB;
using IBIMTool.RevitExtensions;
using IBIMTool.RevitModels;
using IBIMTool.RevitUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Document = Autodesk.Revit.DB.Document;
using Line = Autodesk.Revit.DB.Line;
using Material = Autodesk.Revit.DB.Material;
using Plane = Autodesk.Revit.DB.Plane;


namespace IBIMTool.CutOpening
{
    internal sealed class CutHoleCollisionManager
    {

        #region Default members

        private const double epsilon = 0.0005;
        private const double footToMm = 304.8;
        private readonly XYZ vertical = XYZ.BasisZ;
        private readonly Options options = new Options()
        {
            ComputeReferences = true,
            IncludeNonVisibleObjects = true,
            DetailLevel = ViewDetailLevel.Medium
        };

        private readonly Transform identity = Transform.Identity;
        private readonly double threshold = Math.Round(Math.Cos(Math.PI / 4), 5);
        private readonly SolidCurveIntersectionOptions intersectOptions = new SolidCurveIntersectionOptions();
        private readonly ElementMulticategoryFilter catFilter = GetElementMulticategoryFilter();
        private static ElementMulticategoryFilter GetElementMulticategoryFilter()
        {
            List<BuiltInCategory> builtInCats = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Conduit,
                BuiltInCategory.OST_CableTray,
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_DuctCurves,
                BuiltInCategory.OST_GenericModel,
                BuiltInCategory.OST_MechanicalEquipment
            };
            return new ElementMulticategoryFilter(builtInCats);
        }

        #endregion


        #region Input members

        public IList<DocumentModel> DocumentModelList { get; set; }
        private IDictionary<int, ElementId> elementTypeIdData { get; set; }

        private readonly double offset = Math.Round(Convert.ToDouble(Properties.Settings.Default.CutOffsetInMm / footToMm), 5);
        private readonly double minSideSize = Math.Round((Properties.Settings.Default.MinSideSizeInMm / footToMm) - epsilon, 5);
        private readonly double minDepthSize = Math.Round((Properties.Settings.Default.MinDepthSizeInMm / footToMm) - epsilon, 5);

        #endregion


        #region Temporary fields

        private Plane plane = null;
        private Outline outline = null;
        private ElementModel model = null;

        private XYZ centroid = null;
        private XYZ hostNormal = null;
        private Solid hostSolid = null;
        private PlanarFace hostface = null;
        private CurveLoop curveLoop = null;

        private List<ElementModel> collisions = null;
        private ISet<XYZ> points = null;

        private double hight = 0;
        private double width = 0;
        private double depth = 0;

        #endregion


        #region Initialize

        public void InitializeElementTypeIdData(Document doc)
        {
            elementTypeIdData = RevitPurginqManager.PurgeAndGetValidConstructionTypeIds(doc);
        }


        public IDictionary<string, Material> GetStructureCoreMaterialData(Document doc)
        {
            return elementTypeIdData.GetStructureCoreMaterialData(doc);
        }

        #endregion


        #region Retrieve collision

        public IEnumerable<ElementModel> GetCollisionModelData(Document doc, Material material)
        {
            Properties.Settings.Default.Upgrade();
            IEnumerable<Element> enclosureStructures = elementTypeIdData?.GetInstancesByTypeIdDataAndMaterial(doc, material);
            using TransactionGroup transGroup = new TransactionGroup(doc, "GetCollision");
            TransactionStatus status = transGroup.Start();
            foreach (Element host in enclosureStructures)
            {
                collisions = GetCollisionByHost(doc, host);
                for (int idx = 0; idx < collisions.Count; idx++)
                {
                    model = collisions[idx];
                    centroid = model.Centroid;
                    outline = model.SectionOutline;
                    Debug.Assert(!outline.IsEmpty, "Empty outline");
                    model.SetSizeDescription();
                    yield return model;
                }
            }
            status = transGroup.Assimilate();
        }


        private List<ElementModel> GetCollisionByHost(Document doc, Element hostElem)
        {
            hostSolid = hostElem.GetSolid(identity, options);
            hostNormal = hostElem.GetHostNormal(identity, out hostface);
            List<ElementModel> result = new List<ElementModel>(15);
            for (int idx = 0; idx < DocumentModelList.Count; idx++)
            {
                DocumentModel docmod = DocumentModelList[idx];
                string notation = docmod.SectionNotation;
                foreach (Element elem in hostSolid.GetIntersection(catFilter, docmod, identity))
                {
                    if (IsIntersectionValid(elem, hostSolid, hostNormal, out _, out depth))
                    {
                        points = hostSolid.GetIntersectionPoints(elem, identity, options, ref centroid);

                        plane = hostface.CreatePlaneByCentroid(ref doc, centroid);

                        outline = plane.ProjectPointsOnPlane(points, out curveLoop);

                        outline.GetSectionSize(hostNormal, out width, out hight);

                        if (minSideSize < Math.Min(width, hight))
                        {
                            model = new ElementModel(elem, hostElem)
                            {
                                Depth = depth,
                                Width = width,
                                Hight = hight,
                                Centroid = centroid,
                                SectionPlane = plane,
                                SectionOutline = outline,
                                ProjectSection = notation,
                                Direction = hostNormal.CrossProduct(XYZ.BasisZ)
                            };
                            result.Add(model);
                        }
                    }
                }
            }

            return result;
        }


        private ICollection<ElementId> IsContainsElements(Document doc, Outline outline, ICollection<ElementId> ids)
        {
            return new FilteredElementCollector(doc, ids).WhereElementIsNotElementType()
            .WherePasses(new BoundingBoxIntersectsFilter(outline))
            .ToElementIds();
        }

        #endregion


        #region  Determining size opening

        private bool IsIntersectionValid(Element elem, in Solid solid, in XYZ normal, out XYZ vector, out double depth)
        {
            depth = 0;
            vector = XYZ.Zero;
            Line interLine = null;
            if (elem.Location is LocationCurve curve)
            {
                interLine = curve.Curve as Line;
                if (normal.IsAlmostEqualTo(interLine.Direction, threshold))
                {
                    vector = interLine.Direction.Normalize();
                }
            }
            else if (elem is FamilyInstance instance)
            {
                Transform transform = instance.GetTransform();
                if (normal.IsAlmostEqualTo(transform.BasisX, threshold))
                {
                    vector = transform.BasisX.Normalize();
                    interLine = CreateLine(vector, transform.Origin);
                }

                if (normal.IsAlmostEqualTo(transform.BasisY, threshold))
                {
                    vector = transform.BasisY.Normalize();
                    interLine = CreateLine(vector, transform.Origin);
                }
            }

            if (solid != null && interLine != null)
            {
                SolidCurveIntersection curves = solid.IntersectWithCurve(interLine, intersectOptions);
                if (curves != null && 0 < curves.SegmentCount)
                {
                    interLine = curves.GetCurveSegment(0) as Line;
                    vector = interLine.GetEndPoint(1) - interLine.GetEndPoint(0);
                    depth = Math.Round(Math.Abs(normal.DotProduct(vector)), 5);
                }
            }

            return minDepthSize < depth;
        }


        private Line CreateLine(XYZ direction, XYZ centroid)
        {
            XYZ strPnt = centroid - (direction * 3);
            XYZ endPnt = centroid + (direction * 3);
            return Line.CreateBound(strPnt, endPnt);
        }

        #endregion


        #region Other methods

        //private bool ComputeIntersectionVolume(Solid solidA, Solid solidB)
        //{
        //    Solid interLine = BooleanOperationsUtils.ExecuteBooleanOperation(solidA, solidB, BooleanOperationsType.Intersect);
        //    return interLine.Volume > 0;
        //}



        //private double GetLengthValueBySimilarParameterName(Instance floor, string paramName)
        //{
        //    double value = invalidInt;
        //    int minDistance = int.MaxValue;
        //    char[] delimiters = new[] { ' ', '_', '-' };
        //    foreach (SelectedParameter parameter in floor.GetOrderedParameters())
        //    {
        //        Definition highMark = parameter.Definition;
        //        if (parameter.HasValue && highMark.ParameterType == lenParamType)
        //        {
        //            string name = highMark.Name;
        //            string[] strArray = name.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        //            if (strArray.Contains(paramName, StringComparer.CurrentCultureIgnoreCase))
        //            {
        //                int tmp = parameter.IsShared ? name.Length : name.Length + strArray.Length;
        //                if (minDistance > tmp && UnitFormatUtils.TryParse(revitUnits, UnitType.UT_Length, parameter.AsValueString(), out value))
        //                {
        //                    minDistance = tmp;
        //                }
        //            }
        //        }
        //    }
        //    return value;
        //}


        //private bool GetFamilyInstanceReferencePlane(FamilyInstance fi, out XYZ origin, out XYZ vector)
        //{
        //    bool flag = false;
        //    origin = XYZ.Zero;
        //    vector = XYZ.Zero;

        //    Reference reference = fi.GetReferences(FamilyInstanceReferenceType.CenterFrontBack).FirstOrDefault();
        //    reference = SearchInstance != null ? reference.CreateLinkReference(SearchInstance) : reference;

        //    if (null != reference)
        //    {
        //        Document doc = fi.Document;
        //        using Transaction trx = new(doc);
        //        _ = trx.Start("Create Temporary Sketch SectionPlane");
        //        try
        //        {
        //            SketchPlane sketchPlan = SketchPlane.Create(doc, reference);
        //            if (null != sketchPlan)
        //            {
        //                SectionPlane viewPlan = sketchPlan.GetPlane();
        //                vector = viewPlan.Normal;
        //                origin = viewPlan.Centroid;
        //                flag = true;
        //            }
        //        }
        //        finally
        //        {
        //            _ = trx.RollBack();
        //        }
        //    }
        //    return flag;
        //}


        //private double GetRotationAngleFromTransform(Transform local)
        //{
        //    double x = local.BasisX.X;
        //    double y = local.BasisY.Y;
        //    double z = local.BasisZ.Z;
        //    double trace = x + y + z;
        //    return Math.Acos((trace - 1) / 2.0);
        //}

        //private void УxtractSectionSize(Element floor)
        //{
        //    hight = 0; widht = 0;
        //    int catIdInt = floor.Category.Id.IntegerValue;
        //    if (floor.Document.GetElement(floor.GetTypeId()) is ElementType)
        //    {
        //        BuiltInCategory builtInCategory = (BuiltInCategory)catIdInt;
        //        switch (builtInCategory)
        //        {
        //            case BuiltInCategory.OST_PipeCurves:
        //                {
        //                    diameter = floor.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).AsDouble();
        //                    return;
        //                }
        //            case BuiltInCategory.OST_DuctCurves:
        //                {
        //                    SelectedParameter diameterParam = floor.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);
        //                    if (diameterParam != null && diameterParam.HasValue)
        //                    {
        //                        diameter = diameterParam.AsDouble();
        //                        hight = diameter;
        //                        widht = diameter;
        //                    }
        //                    else
        //                    {
        //                        hight = floor.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble();
        //                        widht = floor.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble();
        //                    }
        //                    return;
        //                }
        //            case BuiltInCategory.OST_Conduit:
        //                {
        //                    diameter = floor.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM).AsDouble();
        //                    return;
        //                }
        //            case BuiltInCategory.OST_CableTray:
        //                {
        //                    hight = floor.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).AsDouble();
        //                    widht = floor.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble();
        //                    return;
        //                }
        //            default:
        //                {
        //                    return;
        //                }
        //        }
        //}

        #endregion


        public void Dispose()
        {
            collisions = null;
            DocumentModelList = null;
            elementTypeIdData = null;
        }

    }
}