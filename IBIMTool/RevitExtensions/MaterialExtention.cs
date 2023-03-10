using Autodesk.Revit.DB;
using IBIMTool.RevitUtils;
using System.Collections.Generic;


namespace IBIMTool.RevitExtensions
{
    public static class MaterialExtention
    {
        public static IDictionary<string, Material> GetStructureCoreMaterialData(this IDictionary<int, ElementId> sourceTypeIds, Document doc)
        {
            CompoundStructure compound = null;
            IDictionary<string, Material> result = new SortedDictionary<string, Material>();
            if (sourceTypeIds != null && sourceTypeIds.Count > 0)
            {
                foreach (KeyValuePair<int, ElementId> item in sourceTypeIds)
                {
                    Element elem = doc.GetElement(item.Value);
                    if (elem is RoofType roofType)
                    {
                        compound = roofType.GetCompoundStructure();
                    }
                    else if (elem is WallType wallType)
                    {
                        compound = wallType.GetCompoundStructure();
                    }
                    else if (elem is FloorType floorType)
                    {
                        compound = floorType.GetCompoundStructure();
                    }
                    Material material = RevitFilterManager.GetCompoundStructureMaterial(doc, elem, compound);
                    if (material != null)
                    {
                        result[material.Name] = material;
                    }
                }
            }
            return result;
        }


        public static IEnumerable<Element> GetInstancesByTypeIdDataAndMaterial(this IDictionary<int, ElementId> sourceTypeIds, Document doc, Material structure)
        {
            string materialName = structure.Name;
            foreach (KeyValuePair<int, ElementId> item in sourceTypeIds)
            {
                Material material = null;
                Element elem = doc.GetElement(item.Value);
                if (elem is RoofType roofType)
                {
                    CompoundStructure compound = roofType.GetCompoundStructure();
                    material = RevitFilterManager.GetCompoundStructureMaterial(doc, elem, compound);
                }
                else if (elem is WallType wallType)
                {
                    CompoundStructure compound = wallType.GetCompoundStructure();
                    material = RevitFilterManager.GetCompoundStructureMaterial(doc, elem, compound);
                }
                else if (elem is FloorType floorType)
                {
                    CompoundStructure compound = floorType.GetCompoundStructure();
                    material = RevitFilterManager.GetCompoundStructureMaterial(doc, elem, compound);
                }
                if (material != null && material.Name == materialName)
                {
                    foreach (Element inst in RevitFilterManager.GetInstancesByElementTypeId(doc, elem.Id))
                    {
                        yield return inst;
                    }
                }
            }
        }

    }
}
