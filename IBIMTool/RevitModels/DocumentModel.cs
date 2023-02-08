using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;


namespace IBIMTool.RevitModels
{
    public sealed class DocumentModel : ObservableObject, IDisposable
    {
        public string Title { get; private set; }
        public bool IsActive { get; private set; }
        public string FilePath { get; private set; }

        public readonly Document Document;
        public readonly Transform Transform;
        public readonly RevitLinkInstance LinkInstance;
        public DocumentModel(Document document, RevitLinkInstance linkInstance = null)
        {
            Document = document;
            LinkInstance = linkInstance;
            FilePath = document.PathName;
            IsActive = !document.IsLinked;
            Title = Path.GetFileNameWithoutExtension(FilePath).Trim();
            Transform = document.IsLinked ? linkInstance.GetTotalTransform() : Transform.Identity;
        }


        private bool enabled = true;
        public bool IsEnabled
        {
            get => enabled;
            set => SetProperty(ref enabled, value);
        }


        private string nota = "MEP";
        public string SectionNotation
        {
            get => nota;
            set => SetProperty(ref nota, value);
        }


        public void Dispose()
        {
            Title = null;
            FilePath = null;
            Document.Dispose();
            Transform.Dispose();
            LinkInstance.Dispose();
        }

        public override string ToString()
        {
            return Title;
        }

    }
}
