using Autodesk.Revit.DB;
using System;

namespace IBIMTool.RevitExtensions
{
    internal static class VectorExtension
    {
        public static double GetHorizontAngle(this XYZ normal)
        {
            return Math.Atan(normal.X / normal.Y);
        }


        public static void GetAngleBetween(this XYZ normal, in XYZ vector, out double horizont, out double vertical)
        {
            double sin = (normal.X * vector.Y) - (vector.X * normal.Y);
            double cos = (normal.X * vector.X) + (normal.Y * vector.Y);
            vertical = NormaliseAngle(Math.Asin(normal.Z - vector.Z));
            horizont = NormaliseAngle(Math.Atan2(sin, cos));
        }


        private static double NormaliseAngle(double angle)
        {
            angle = Math.Abs(angle);
            angle = angle > Math.PI ? (Math.PI * 2) - angle : angle;
            angle = angle > Math.PI / 2 ? Math.PI - angle : angle;
            return Math.Abs(angle);
        }


        public static XYZ DumbToPositive(this XYZ vector)
        {
            double valX = Math.Abs(vector.X);
            double valY = Math.Abs(vector.Y);
            double valZ = Math.Abs(vector.Z);
            return new XYZ(valX, valY, valZ);
        }


        public static bool IsParallel(this XYZ normal, in XYZ direction)
        {
            return normal.CrossProduct(direction).IsZeroLength();
        }


        public static double GetDistanceAlone(this XYZ vector, in XYZ p1, in XYZ p2)
        {
            return Math.Abs(vector.DotProduct(p1.Subtract(p2)));
        }


        public static double ConvertToDegrees(this double radians, int digit = 3)
        {
            return Math.Round(180 / Math.PI * radians, digit);
        }
    }
}
