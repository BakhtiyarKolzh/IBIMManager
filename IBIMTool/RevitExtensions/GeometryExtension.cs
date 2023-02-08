using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using IBIMTool.RevitModels;
using IBIMTool.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Document = Autodesk.Revit.DB.Document;
using Line = Autodesk.Revit.DB.Line;
using Plane = Autodesk.Revit.DB.Plane;


namespace IBIMTool.RevitExtensions
{
    internal static class GeometryExtension
    {
        public static Outline GetOutLine(this BoundingBoxXYZ bbox)
        {
            Transform transform = bbox.Transform;
            return new Outline(transform.OfPoint(bbox.Min), transform.OfPoint(bbox.Max));
        }


        public static XYZ GetHostNormal(this Element elem, in Transform identity, out PlanarFace resultface, double tollerance = 0)
        {
            resultface = null;
            XYZ resultNormal = XYZ.BasisZ;
            if (elem is Wall wall)
            {
                resultNormal = identity.OfVector(wall.Orientation).Normalize();
                Reference reference = HostObjectUtils.GetSideFaces(wall, ShellLayerType.Exterior).First();
                resultface = elem.GetGeometryObjectFromReference(reference) as PlanarFace;
            }
            else if (elem is HostObject hostObject)
            {
                foreach (Reference refFace in HostObjectUtils.GetTopFaces(hostObject))
                {
                    GeometryObject geo = elem.GetGeometryObjectFromReference(refFace);
                    if (geo is PlanarFace planar && planar.Area > tollerance)
                    {
                        resultNormal = identity.OfVector(planar.FaceNormal).Normalize();
                        tollerance = planar.Area;
                        resultface = planar;
                        //{
                        //    BoundingBoxUV box = planar.GetBoundingBox();
                        //    resultNormal = planar.ComputeNormal((box.Max + box.Min) * 0.5);
                        //    resultNormal = identity.OfVector(resultNormal).Normalize();
                        //}
                    }
                }
            }
            return resultNormal;
        }


        public static BoundingBoxUV GetSectionBound(this Solid solid, Document doc, in XYZ normal, in XYZ centroid, out IList<CurveLoop> loops)
        {
            loops = null;
            BoundingBoxUV result = null;
            using (Transaction trx = new Transaction(doc, "GetSectionBound"))
            {
                TransactionStatus status = trx.Start();
                try
                {
                    Plane plane = Plane.CreateByNormalAndOrigin(normal, centroid);
                    Face face = ExtrusionAnalyzer.Create(solid, plane, normal).GetExtrusionBase();
                    loops = ExporterIFCUtils.ValidateCurveLoops(face.GetEdgesAsCurveLoops(), normal);
                    result = face.GetBoundingBox();
                    status = trx.Commit();
                }
                catch (Exception)
                {
                    if (!trx.HasEnded())
                    {
                        status = trx.RollBack();
                    }
                }
            }
            return result;
        }


        public static IList<Element> GetIntersection(this Solid solid, in ElementMulticategoryFilter catFilter, in DocumentModel docmod, in Transform identity)
        {
            Document doc = docmod.Document;
            FilteredElementCollector collector;
            Transform transform = docmod.Transform;
            if (!transform.AlmostEqual(identity))
            {
                IBIMLogger.Log("Link is " + doc.Title);
                solid = SolidUtils.CreateTransformed(solid, transform.Inverse);
            }
            BoundingBoxXYZ bbox = solid.GetBoundingBox();
            ElementQuickFilter bboxFilter = new BoundingBoxIntersectsFilter(bbox.GetOutLine());
            LogicalAndFilter intersectFilter = new LogicalAndFilter(bboxFilter, new ElementIntersectsSolidFilter(solid));
            collector = new FilteredElementCollector(doc).WherePasses(catFilter).WhereElementIsNotElementType();
            collector = collector.WherePasses(intersectFilter);
            return collector.ToElements();
        }


        public static Plane CreatePlaneByCentroid(this PlanarFace planar, ref Document doc, in XYZ centroid)
        {
            Plane plane = null;
            XYZ normal = planar.FaceNormal;
            using (Transaction trx = new Transaction(doc, "CreatePlaneByCentroid"))
            {
                TransactionStatus status = trx.Start();
                try
                {
                    IntersectionResult result = planar.Project(centroid);
                    plane = Plane.CreateByNormalAndOrigin(normal, result.XYZPoint);
                    status = trx.Commit();
                }
                catch (Exception ex)
                {
                    if (!trx.HasEnded())
                    {
                        status = trx.RollBack();
                        IBIMLogger.Error(ex.Message);
                    }
                }
            }
            return plane;
        }


        public static Outline ProjectPointsOnPlane(this Plane plane, in ISet<XYZ> points, out CurveLoop curveLoop)
        {
            curveLoop = null;

            XYZ Xvector = plane.XVec;
            XYZ Yvector = plane.YVec;
            XYZ origin = plane.Origin;

            double minU = 0, maxU = 0;
            double minV = 0, maxV = 0;

            foreach (XYZ pnt in points)
            {
                plane.Project(pnt, out UV uvp, out double dist);
                double upnt = uvp.U; double vpnt = uvp.V;
                minU = Math.Min(minU, upnt);
                maxU = Math.Max(maxU, upnt);
                minV = Math.Min(minV, vpnt);
                maxV = Math.Max(maxV, vpnt);
            }

            List<Curve> profile = new List<Curve>();

            XYZ minPt1 = origin + (minU * Xvector) + (minV * Yvector);
            XYZ midPn2 = origin + (minU * Xvector) + (maxV * Yvector);
            XYZ maxPt3 = origin + (maxU * Xvector) + (maxV * Yvector);
            XYZ midPt4 = origin + (maxU * Xvector) + (minV * Yvector);

            profile.Add(Line.CreateBound(minPt1, midPn2));
            profile.Add(Line.CreateBound(midPn2, maxPt3));
            profile.Add(Line.CreateBound(maxPt3, midPt4));
            profile.Add(Line.CreateBound(midPt4, minPt1));

            curveLoop = CurveLoop.Create(profile);

            return new Outline(minPt1, maxPt3);
        }


        public static void GetSectionSize(this Outline outline, XYZ direction, out double width, out double hight)
        {
            XYZ vertical = XYZ.BasisZ;
            XYZ minPt = outline.MinimumPoint;
            XYZ maxPt = outline.MaximumPoint;

            if (direction.IsAlmostEqualTo(vertical, 0.25))
            {
                width = Math.Round(XYZ.BasisX.GetDistanceAlone(minPt, maxPt), 5);
                hight = Math.Round(XYZ.BasisY.GetDistanceAlone(minPt, maxPt), 5);
            }
            else
            {
                direction = direction.CrossProduct(vertical);
                hight = Math.Round(vertical.GetDistanceAlone(minPt, maxPt), 5);
                width = Math.Round(direction.GetDistanceAlone(minPt, maxPt), 5);
            }
        }


        public static IList<CurveLoop> GetSectionProfile(ElementModel model, in double offset)
        {

            XYZ normal = model.SectionPlane.Normal;
            XYZ vectorX = model.SectionPlane.XVec;
            XYZ vectorY = model.SectionPlane.YVec;

            XYZ origin = model.Centroid;

            double positiveWidth = model.Width * 0.5;
            double negativeWidth = -model.Width * 0.5;
            double positiveHight = model.Hight * 0.5;
            double negativeHight = -model.Hight * 0.5;

            XYZ pt0 = origin + (negativeWidth * vectorX) + (negativeHight * vectorY);
            XYZ pt1 = origin + (positiveWidth * vectorX) + (negativeHight * vectorY);
            XYZ pt2 = origin + (positiveWidth * vectorX) + (positiveHight * vectorY);
            XYZ pt3 = origin + (negativeWidth * vectorX) + (positiveHight * vectorY);

            IList<Curve> edges = new List<Curve>(4)
            {
                Line.CreateBound(pt0, pt1),
                Line.CreateBound(pt1, pt2),
                Line.CreateBound(pt2, pt3),
                Line.CreateBound(pt3, pt0),
            };

            CurveLoop loop = CurveLoop.Create(edges);

            if (!loop.HasPlane())
            {
                IBIMLogger.Error("Loop not have plane");
            }

            if (!loop.IsCounterclockwise(normal)) { loop.Flip(); }
            loop = CurveLoop.CreateViaOffset(loop, offset, normal);
            IList<CurveLoop> curveloops = new List<CurveLoop>() { loop };

            return ExporterIFCUtils.ValidateCurveLoops(curveloops, normal);
        }





        public static void CreateDirectShape(this Solid solid, Document doc, BuiltInCategory builtIn = BuiltInCategory.OST_GenericModel)
        {
            if (0 < solid.Faces.Size && 0 < solid.Volume)
            {
                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(builtIn));
                ds.ApplicationDataId = doc.ProjectInformation.UniqueId;
                ds.SetShape(new GeometryObject[] { solid });
                ds.Name = "DirectShapeBySolid";
            }
        }
    }
}