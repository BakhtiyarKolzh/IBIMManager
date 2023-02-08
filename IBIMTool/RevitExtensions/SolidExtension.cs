using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using System;
using System.Collections.Generic;
using Options = Autodesk.Revit.DB.Options;


namespace IBIMTool.RevitExtensions
{
    internal static class SolidExtension
    {
        public static Solid GetSolid(this Element element, in Transform global, in Options options, double tolerance = 0.05)
        {
            Solid result = null;
            GeometryElement geomElem = element.get_Geometry(options);
            foreach (GeometryObject obj in geomElem.GetTransformed(global))
            {
                if (obj is Solid solid && solid != null)
                {
                    double volume = solid.Volume;
                    if (volume > tolerance)
                    {
                        tolerance = volume;
                        result = solid;
                    }
                }
            }
            return result;
        }


        public static Solid GetIntersectionSolid(this Solid source, in Element elem, in Transform global, in Options options, double tolerance = 0.05)
        {
            Solid result = null;
            GeometryElement geomElement = elem.get_Geometry(options);
            BooleanOperationsType union = BooleanOperationsType.Union;
            BooleanOperationsType intersect = BooleanOperationsType.Intersect;
            foreach (GeometryObject obj in geomElement.GetTransformed(global))
            {
                if (obj is Solid solid && solid != null && solid.Faces.Size > 0)
                {
                    try
                    {
                        solid = BooleanOperationsUtils.ExecuteBooleanOperation(source, solid, intersect);
                        if (result != null && solid != null && solid.Volume > 0)
                        {
                            solid = BooleanOperationsUtils.ExecuteBooleanOperation(result, solid, union);
                        }
                    }
                    finally
                    {
                        double volume = solid.Volume;
                        if (volume > tolerance)
                        {
                            tolerance = volume;
                            result = solid;
                        }
                    }
                }
            }
            return result;
        }


        public static ISet<XYZ> GetIntersectionPoints(this Solid source, in Element elem, in Transform global, in Options options, ref XYZ centroid)
        {
            ISet<XYZ> vertices = new HashSet<XYZ>(100);
            GeometryElement geomElement = elem.get_Geometry(options);
            BooleanOperationsType intersect = BooleanOperationsType.Intersect;
            foreach (GeometryObject obj in geomElement.GetTransformed(global))
            {
                if (obj is Solid solid && solid.Faces.Size > 0)
                {
                    try
                    {
                        solid = BooleanOperationsUtils.ExecuteBooleanOperation(source, solid, intersect);
                    }
                    finally
                    {
                        if (solid != null && solid.Volume > 0)
                        {
                            centroid = solid.ComputeCentroid();
                            foreach (Face f in solid.Faces)
                            {
                                Mesh mesh = f.Triangulate();
                                int n = mesh.NumTriangles;
                                for (int i = 0; i < n; ++i)
                                {
                                    MeshTriangle triangle = mesh.get_Triangle(i);
                                    vertices.Add(triangle.get_Vertex(0));
                                    vertices.Add(triangle.get_Vertex(1));
                                    vertices.Add(triangle.get_Vertex(2));
                                }
                            }
                        }
                    }
                }
            }
            return vertices;
        }


        public static Solid CreateExtrusionGeometry(this IList<CurveLoop> curveloops, in XYZ normal, in double height)
        {
            if (curveloops.Count == 0)
            {
                throw new Exception("CurveLoops is empty");
            }
            double distance = Math.Abs(Math.Round(height + 0.25, 5));
            IList<CurveLoop> profile = new List<CurveLoop>(4);
            foreach (CurveLoop loop in curveloops)
            {
                CurveLoop newloop = CurveLoop.CreateViaCopy(loop);
                Transform trs = Transform.CreateTranslation(normal * distance / 2);
                newloop.Transform(trs.Inverse);
                profile.Add(newloop);
            }
            profile = ExporterIFCUtils.ValidateCurveLoops(profile, normal);
            return GeometryCreationUtilities.CreateExtrusionGeometry(profile, normal, distance);
        }


        public static Solid ScaledSolidByOffset(this Solid solid, double offset)
        {
            XYZ centroid = solid.ComputeCentroid();
            XYZ pnt = new XYZ(offset, offset, offset);
            BoundingBoxXYZ bbox = solid.GetBoundingBox();
            XYZ minPnt = bbox.Min; XYZ maxPnt = bbox.Max;
            double minDiagonal = minPnt.DistanceTo(maxPnt);
            double maxDiagonal = (minPnt - pnt).DistanceTo(maxPnt + pnt);
            Transform trans = Transform.CreateTranslation(XYZ.Zero).ScaleBasisAndOrigin(maxDiagonal / minDiagonal);
            solid = SolidUtils.CreateTransformed(solid, trans.Multiply(Transform.CreateTranslation(centroid).Inverse));
            return SolidUtils.CreateTransformed(solid, Transform.CreateTranslation(centroid));
        }


        public static Solid CreateRectangularPrism(XYZ center, double d1, double d2, double d3)
        {
            List<Curve> profile = new List<Curve>();
            XYZ profile00 = new XYZ(-d1 / 2, -d2 / 2, -d3 / 2);
            XYZ profile01 = new XYZ(-d1 / 2, d2 / 2, -d3 / 2);
            XYZ profile11 = new XYZ(d1 / 2, d2 / 2, -d3 / 2);
            XYZ profile10 = new XYZ(d1 / 2, -d2 / 2, -d3 / 2);

            profile.Add(Line.CreateBound(profile00, profile01));
            profile.Add(Line.CreateBound(profile01, profile11));
            profile.Add(Line.CreateBound(profile11, profile10));
            profile.Add(Line.CreateBound(profile10, profile00));

            CurveLoop curveLoop = CurveLoop.Create(profile);

            SolidOptions options = new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId);

            return GeometryCreationUtilities.CreateExtrusionGeometry(new CurveLoop[] { curveLoop }, XYZ.BasisZ, d3, options);
        }


        public static Solid Sphere(this XYZ origin, double radius = 0.75)
        {
            Frame frame = new Frame(origin, XYZ.BasisX, XYZ.BasisY, XYZ.BasisZ);

            XYZ XyzOnArc = origin + (radius * XYZ.BasisX);
            XYZ start = origin - (radius * XYZ.BasisZ);
            XYZ end = origin + (radius * XYZ.BasisZ);

            Arc arc = Arc.Create(start, end, XyzOnArc);

            Line line = Line.CreateBound(arc.GetEndPoint(1), arc.GetEndPoint(0));

            CurveLoop halfCircle = new CurveLoop();
            halfCircle.Append(arc);
            halfCircle.Append(line);

            List<CurveLoop> loops = new List<CurveLoop>(1) { halfCircle };

            return GeometryCreationUtilities.CreateRevolvedGeometry(frame, loops, 0, 2 * Math.PI);
        }
    }
}
