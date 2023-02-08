using Autodesk.Revit.UI;
using IBIMTool.Core;
using IBIMTool.Services;
using System;
using System.IO;


namespace IBIMTool.RevitEventHandlers
{
    public class SettingsEventHandler : IExternalEventHandler
    {
        private readonly object syncLock = new object();
        private readonly string localPath = IBIMToolHelper.LocalPath;
        private string activeTitle;
        public void Execute(UIApplication app)
        {
            lock (syncLock)
            {
                try
                {
                    activeTitle = app.ActiveUIDocument.Document.Title;
                    Properties.Settings.Default.ActiveDocumentTitle = activeTitle;
                    Properties.Settings.Default.Save();

                    if (!Directory.Exists(localPath))
                    {
                        Directory.CreateDirectory(localPath);
                    }
                }
                catch (Exception ex)
                {
                    IBIMLogger.Error(ex);
                }
                finally
                {
                    Properties.Settings.Default.Upgrade();
                    string title = Properties.Settings.Default.ActiveDocumentTitle;
                    if (string.IsNullOrWhiteSpace(title) || title != activeTitle)
                    {
                        throw new ArgumentException("ActiveDocumentTitle");
                    }
                }
            }
        }


        public string GetName()
        {
            return typeof(SettingsEventHandler).FullName;
        }
    }
}
