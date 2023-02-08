using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using IBIMTool.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace IBIMTool.Services
{
    public sealed class IBIMLogger
    {

        private static readonly string caption = IBIMToolHelper.ApplicationName;
        private static readonly string documentPath = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
        private static readonly string logFilePath = Path.Combine(documentPath, "BIMLog", "Revit.log");

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string GetCurrentMethod()
        {
            StackTrace st = new StackTrace();
            StackFrame sf = st.GetFrame(1);
            return sf.GetMethod().Name;
        }


        public static void ThreadProcessLog(string name)
        {
            Thread th = Thread.CurrentThread;
            Debug.WriteLine($"Task Thread ID: {th.ManagedThreadId}, Thread Name: {th.Name}, Process Name: {name}");
        }


        public static void Log(string text)
        {
            Debug.WriteLine(text);
        }


        public static void Error(Exception ex)
        {
            StringBuilder builder = new StringBuilder();
            builder = builder.AppendLine(ex.Source);
            builder = builder.AppendLine(ex.Message);
            builder = builder.AppendLine(ex.StackTrace);
            Error(builder.ToString());
            builder.Clear();
        }


        public static void Error(string text)
        {
            Debug.WriteLine(text);
            TaskDialog dlg = new TaskDialog(caption)
            {
                MainContent = text,
                MainInstruction = "Error: " + GetCurrentMethod(),
                MainIcon = TaskDialogIcon.TaskDialogIconInformation
            };
            dlg.Show();
        }


        public static void Warning(string text)
        {
            Debug.WriteLine(text);
            TaskDialog dlg = new TaskDialog(caption)
            {
                MainContent = text,
                MainInstruction = "Warning: ",
                MainIcon = TaskDialogIcon.TaskDialogIconInformation
            };
            dlg.Show();
        }


        public static void Info(string text)
        {
            Debug.WriteLine(text);
            TaskDialog dlg = new TaskDialog(caption) 
            { 
                MainContent = text,
                MainInstruction = "Information: ", 
                MainIcon = TaskDialogIcon.TaskDialogIconInformation 
            };
            dlg.Show();
        }


        public static string ElementDescription(Element elem)
        {
            if (elem.IsValidObject && elem is FamilyInstance)
            {
                FamilyInstance finst = elem as FamilyInstance;

                string typeName = elem.GetType().Name;
                string famName = null == finst ? string.Empty : $"{finst.Symbol.Family.Name}";
                string catName = null == elem.Category ? string.Empty : $"{elem.Category.Name}";
                string symbName = null == finst || elem.Name.Equals(finst.Symbol.Name) ? string.Empty : $"{finst.Symbol.Name}";

                return $"{famName}-{symbName}<{elem.Id.IntegerValue} {elem.Name}>({typeName}-{catName})";
            }
            return "<null>";
        }

    }
}