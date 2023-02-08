using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IBIMTool.RevitExtensions;
using IBIMTool.RevitModels;
using IBIMTool.RevitUtils;
using IBIMTool.Views;
using Revit.Async;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;


namespace IBIMTool.ViewModels
{
    public sealed class PreviewViewModel : ObservableObject, IDisposable
    {
        private View view { get; set; }
        private Document doc { get; set; }
        private UIDocument uidoc { get; set; }

        private BoundingBoxXYZ sectbox { get; set; }
        private BoundingBoxXYZ planbox { get; set; }

        private readonly PreviewDialogWindow window;

        private static PreviewControl previewControl;

        public ElementModel ElementModel { get; private set; }
        public PreviewViewModel(PreviewDialogWindow frame)
        {
            window = frame;
            window.ViewModel = this;
            ChangeViewCommand = new RelayCommand<bool?>(ChangeViewHandler);
        }


        public bool ShowPreviewModel(UIApplication app, ElementModel model)
        {
            ElementModel = model;
            uidoc = app.ActiveUIDocument;
            doc = app.ActiveUIDocument.Document;
            Rectangle viewRect = app.GetActiveViewRectangle();
            uidoc.Selection.SetElementIds(new List<ElementId> { model.Element.Id });
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Height = Math.Abs(viewRect.Bottom - viewRect.Top);
            window.Width = Math.Abs(viewRect.Right - viewRect.Left);
            window.Left = Math.Abs(viewRect.Left);
            window.Top = Math.Abs(viewRect.Top);
            previewControl = null;

            window.Show();

            return window.IsActive;
        }


        #region ChangeViewCommand

        private async void Show3DViewAsync(ElementModel model)
        {
            await RevitTask.RunAsync(app =>
            {
                if (model.IsValidModel())
                {
                    XYZ origin = model.SectionPlane.Origin;
                    View3D view3d = RevitViewManager.Get3dView(uidoc);
                    RevitViewManager.SetCustomColor(uidoc, view3d, model.Element);
                    RevitViewManager.SetCustomSectionBox(uidoc, origin, view3d, 5);
                    previewControl = new PreviewControl(doc, view3d.Id);
                    previewControl.Loaded += PreviewControl_Loaded;
                    window.ViewControl.Content = previewControl;
                    sectbox = view3d.GetSectionBox();
                    view = view3d;
                }
            });
        }


        private async void ShowPlanViewAsync(ElementModel model)
        {
            await RevitTask.RunAsync(app =>
            {
                if (model.IsValidModel())
                {
                    Level level = model.HostLevel;
                    ViewPlan plan = RevitViewManager.GetPlanByLevel(uidoc, level);
                    planbox = RevitViewManager.SetCustomZoomBox(uidoc, plan, ElementModel);
                    previewControl = new PreviewControl(doc, plan.Id);
                    previewControl.Loaded += PreviewControl_Loaded;
                    window.ViewControl.Content = previewControl;
                    view = plan;
                }
            });
        }


        private void PreviewControl_Loaded(object sender, RoutedEventArgs e)
        {
            previewControl.Loaded -= PreviewControl_Loaded;
            if (view != null && view.IsValidObject)
            {
                if (view is ViewPlan && planbox.Enabled)
                {
                    previewControl.UIView.ZoomAndCenterRectangle(planbox.Min, planbox.Max);
                }
                else if (view is View3D view3D && sectbox.Enabled)
                {
                    previewControl.UIView.ZoomAndCenterRectangle(sectbox.Min, sectbox.Max);
                }
            }
        }


        private void DisposeViewControl()
        {
            window.ViewControl.Content = null;
            window.ViewControl.UpdateLayout();
            previewControl?.Dispose();
        }


        public ICommand ChangeViewCommand { get; internal set; }
        public void ChangeViewHandler(bool? parameter)
        {
            if (window.IsInitialized)
            {
                switch (parameter)
                {
                    case true:
                        DisposeViewControl();
                        Show3DViewAsync(ElementModel);
                        window.ViewSwitch.Content = "3DView";
                        break;
                    case false:
                        DisposeViewControl();
                        ShowPlanViewAsync(ElementModel);
                        window.ViewSwitch.Content = "PlanView";
                        break;
                }
            }
        }

        #endregion


        public void Dispose()
        {
            DisposeViewControl();
            ElementModel?.Dispose();
            sectbox?.Dispose();
            planbox?.Dispose();
            uidoc?.Dispose();
            view?.Dispose();
            window?.Close();
        }

    }
}
