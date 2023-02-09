using Autodesk.Revit.UI;
using IBIMTool.Commands;
using System;


namespace IBIMTool.Core
{
    public sealed class SetupUIPanel
    {
        private static readonly string appName = IBIMToolHelper.ApplicationName;
        private static readonly string assemblyName = IBIMToolHelper.AssemblyLocation;
        private static readonly string ribbonPanelName = IBIMToolHelper.RibbonPanelName;

        [STAThread]
        public static void Initialize(UIControlledApplication uicontrol)
        {
            // Create ribbon tab and ribbon panels

            RibbonPanel ribbonPanel;
            try
            {
                uicontrol.CreateRibbonTab(appName);
                ribbonPanel = uicontrol.CreateRibbonPanel(appName, ribbonPanelName);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }


            foreach (RibbonPanel panel in uicontrol.GetRibbonPanels(appName))
            {
                ribbonPanel = panel;
            }


            // Create Cut Opening PushButtonData 
            PushButtonData cutOpenningBtn = new PushButtonData("CutOpenning", IBIMToolHelper.CutOpenningButtonName, assemblyName, CutHoleShowPanelCommand.GetPath())
            {
                ToolTip = "Cut Openning panel",
                LargeImage = IBIMToolHelper.GetImageSource()
            };


            PushButtonData areaRebarMarkFixBtn = new PushButtonData("AreaRebarMark", IBIMToolHelper.AreaRebarMarkButtonName, assemblyName, AreaRebarMarkFixCommand.GetPath())
            {
                ToolTip = "Fix area rebars marks",
                LargeImage = IBIMToolHelper.GetImageSource()
            };


            PushButtonData autoJoinButton = new PushButtonData("AutoJoin", IBIMToolHelper.AutoJoinButtonName, assemblyName, AutoJoinGeometryCommand.GetPath())
            {
                ToolTip = "Fix area rebars marks",
                LargeImage = IBIMToolHelper.GetImageSource()
            };


            PushButtonData finishingButton = new PushButtonData("Finishing", IBIMToolHelper.FinishingButtonName, assemblyName, RoomFinishingCommand.GetPath())
            {
                ToolTip = "Instance finishing command",
                LargeImage = IBIMToolHelper.GetImageSource()
            };


            // Create Cut Opening PushButtonData 
            PushButtonData autoinfoBtn = new PushButtonData("Authorization", "Authorization", assemblyName, InfoCommand.GetPath())
            {
                ToolTip = "Info",
                LargeImage = IBIMToolHelper.GetImageSource()
            };


            //Add buttons to ribbon panel
            if (ribbonPanel != null && ribbonPanel.AddItem(cutOpenningBtn) is PushButton btn01)
            {
                btn01.AvailabilityClassName = CutHoleShowPanelCommand.GetPath();
                ribbonPanel.AddSeparator();
            }

            if (ribbonPanel != null && ribbonPanel.AddItem(areaRebarMarkFixBtn) is PushButton btn02)
            {
                btn02.AvailabilityClassName = AreaRebarMarkFixCommand.GetPath();
                ribbonPanel.AddSeparator();
            }

            if (ribbonPanel != null && ribbonPanel.AddItem(autoJoinButton) is PushButton btn03)
            {
                btn03.AvailabilityClassName = AutoJoinGeometryCommand.GetPath();
                ribbonPanel.AddSeparator();
            }

            if (ribbonPanel != null && ribbonPanel.AddItem(finishingButton) is PushButton btn04)
            {
                btn04.AvailabilityClassName = RoomFinishingCommand.GetPath();
                ribbonPanel.AddSeparator();
            }

            if (ribbonPanel != null && ribbonPanel.AddItem(autoinfoBtn) is PushButton btn05)
            {
                btn05.AvailabilityClassName = InfoCommand.GetPath();
                ribbonPanel.AddSeparator();
            }

        }
    }
}