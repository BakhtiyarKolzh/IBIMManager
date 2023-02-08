using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;


namespace IBIMTool.RevitExtensions
{
    public static class WindowExtesion
    {
        public static Rectangle GetActiveViewRectangle(this UIApplication uiapp)
        {
            Rectangle viewRect = null;
            if (uiapp.MainWindowHandle != IntPtr.Zero)
            {
                UIDocument uidoc = uiapp.ActiveUIDocument;
                IList<UIView> uiViewsWithActiveView = uidoc.GetOpenUIViews();
                UIView activeUIView = uiViewsWithActiveView.FirstOrDefault();
                viewRect = activeUIView.GetWindowRectangle();
            }
            return viewRect;
        }
    }
}
