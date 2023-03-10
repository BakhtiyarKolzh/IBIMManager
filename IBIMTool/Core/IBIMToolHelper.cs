using Autodesk.Revit.UI;
using IBIMTool.Properties;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace IBIMTool.Core
{
    public sealed class IBIMToolHelper
    {
        public static Icon baseIcon = Resources.baseIcon;

        public static string RibbonPanelName = "Automation";
        public static string ApplicationName = "IBIMTool";

        public static string AutoJoinButtonName = "AutoJoin";
        public static string FinishingButtonName = "Finishing";
        public static string CutOpenningButtonName = "CutOpening";
        public static string AreaRebarMarkButtonName = "AreaRebarMark";

        public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();
        public static readonly string AssemblyLocation = Path.GetFullPath(Assembly.Location);
        public static readonly string AssemblyDirectory = Path.GetDirectoryName(AssemblyLocation);
        public static readonly string AssemblyName = Path.GetFileNameWithoutExtension(AssemblyLocation);
        public static readonly string DocumentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        public static readonly string AppDirPath = Path.Combine(AppDataPath, @"Autodesk\Revit\Addins\2019\IBIMTool");
        public static readonly string LocalPath = Path.Combine(DocumentPath, ApplicationName);


        public DockablePaneId CutVoidPaneId { get; } = new DockablePaneId(new Guid("{C586E687-A52C-42EE-AC75-CD81EE1E7A9A}"));
        public bool IsActive { get; set; } = false;

        #region IconConvertToImageSource
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject([In] IntPtr hObject);
        internal static ImageSource GetImageSource()
        {
            Bitmap bmp = baseIcon.ToBitmap();
            IntPtr handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    handle,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally { _ = DeleteObject(handle); }
        }
        #endregion
    }


}
