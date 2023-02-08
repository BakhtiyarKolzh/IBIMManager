using Autodesk.Revit.UI;
using IBIMTool.Core;
using IBIMTool.Services;
using System;
using System.Windows;


namespace IBIMTool.CutOpening
{
    internal sealed class CutHoleRegisterDockPane
    {
        private readonly string cutHoleToolName = IBIMToolHelper.CutOpenningButtonName;
        public bool RegisterDockablePane(UIControlledApplication controller, DockablePaneId paneId, IDockablePaneProvider dockPane)
        {
            if (!DockablePane.PaneIsRegistered(paneId))
            {
                DockablePaneProviderData data = new DockablePaneProviderData()
                {
                    FrameworkElement = dockPane as FrameworkElement
                };
                data.InitialState.TabBehind = DockablePanes.BuiltInDockablePanes.PropertiesPalette;
                data.EditorInteraction = new EditorInteraction(EditorInteractionType.KeepAlive);
                data.InitialState.DockPosition = DockPosition.Tabbed;
                data.VisibleByDefault = false;
                try
                {
                    controller.RegisterDockablePane(paneId, cutHoleToolName, dockPane);
                }
                catch (Exception exc)
                {
                    IBIMLogger.Error($"ERROR:\nguid={paneId.Guid}\n{exc.Message}");
                    return false;
                }
            }
            return true;
        }
    }
}
