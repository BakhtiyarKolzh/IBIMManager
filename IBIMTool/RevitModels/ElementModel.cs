using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using System;


namespace IBIMTool.RevitModels
{
    public sealed class ElementModel : ObservableObject, IDisposable
    {
        public readonly Level HostLevel;
        public readonly Element Element;

        public string LevelName { get; private set; }
        public string SymbolName { get; private set; }
        public string FamilyName { get; private set; }

        public string HostMark { get; private set; }
        public string HostUniqueId { get; private set; }
        public string HostCategory { get; private set; }

        public ElementModel(Element instanse, Element host)
        {
            Element etype = instanse.Document.GetElement(instanse.GetTypeId());
            if (host.Document.GetElement(host.LevelId) is Level level && etype != null && etype is ElementType elementType)
            {
                HostLevel = level;
                Element = instanse;
                LevelName = level.Name;
                HostUniqueId = host.UniqueId;
                SymbolName = elementType.Name;
                HostCategory = host.Category.Name;
                FamilyName = elementType.FamilyName;
                HostMark = host.get_Parameter(BuiltInParameter.ALL_MODEL_MARK).AsString();
            }
        }


        public XYZ Centroid { get; internal set; }
        public XYZ Direction { get; internal set; }
        public Plane SectionPlane { get; internal set; }
        public Outline SectionOutline { get; internal set; }
        public Outline SectionOnPlaneOutline { get; internal set; }

        public string Description { get; internal set; }
        public string ProjectSection { get; internal set; }
        public int MidSize { get; internal set; }
        public double Hight { get; internal set; }
        public double Width { get; internal set; }
        public double Depth { get; internal set; }


        private bool selected = false;
        public bool IsSelected
        {
            get => selected;
            set => SetProperty(ref selected, value);
        }


        private string mark;
        public string Mark
        {
            get => mark;
            set => SetProperty(ref mark, value);
        }


        public bool IsValidModel()
        {
            return HostLevel != null && Element.IsValidObject;
        }


        public void SetSizeDescription()
        {
            int h = Convert.ToInt16(Hight * 304.8);
            int w = Convert.ToInt16(Width * 304.8);
            MidSize = Convert.ToInt16((h + w) / 2);
            Description = $"{w}x{h}(h)";
        }


        public override string ToString()
        {
            return $"{SymbolName} - {FamilyName}";
        }


        public void Dispose()
        {
            Element?.Dispose();
            HostLevel?.Dispose();
            SectionPlane?.Dispose();
            SectionOutline?.Dispose();
        }
    }
}
