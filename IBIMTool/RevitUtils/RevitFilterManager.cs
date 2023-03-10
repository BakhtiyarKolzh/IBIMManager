using Autodesk.Revit.DB;
using IBIMTool.RevitModels;
using System;
using System.Collections.Generic;
using System.Linq;
using Document = Autodesk.Revit.DB.Document;
using Level = Autodesk.Revit.DB.Level;

namespace IBIMTool.RevitUtils
{
    internal sealed class RevitFilterManager
    {
        private const double epsilon = 1.0e-3;

        #region Document Collector

        public static FilteredElementCollector GetRevitLinkInstanceCollector(Document doc)
        {
            return GetElementsOfCategory(doc, typeof(RevitLinkInstance), BuiltInCategory.OST_RvtLinks, true);
        }


        public static IList<DocumentModel> GetDocumentCollection(Document doc)
        {
            IList<DocumentModel> result = new List<DocumentModel> { new DocumentModel(doc) };
            foreach (RevitLinkInstance link in GetRevitLinkInstanceCollector(doc))
            {
                Document linkDoc = link.GetLinkDocument();
                if (linkDoc != null && linkDoc.IsValidObject)
                {
                    result.Add(new DocumentModel(linkDoc, link));
                }
            }
            return result;
        }

        #endregion


        #region Standert Filtered Element Collector

        public static FilteredElementCollector GetElementsOfCategory(Document doc, Type type, BuiltInCategory bic, bool? isInstances = null)
        {
            return isInstances.HasValue
                ? (bool)isInstances == true
                    ? new FilteredElementCollector(doc).OfClass(type).OfCategory(bic).WhereElementIsNotElementType()
                    : new FilteredElementCollector(doc).OfClass(type).OfCategory(bic).WhereElementIsElementType()
                : new FilteredElementCollector(doc).OfClass(type).OfCategory(bic);
        }


        public static FilteredElementCollector ParamFilterRuleSymbolName(Document doc, BuiltInCategory BultCat, string familyTypeName)
        {
            return new FilteredElementCollector(doc).OfCategory(BultCat).OfClass(typeof(FamilyInstance))
            .WherePasses(
                        new ElementParameterFilter(
                        new FilterStringRule(
                        new ParameterValueProvider(
                        new ElementId(BuiltInParameter.ELEM_FAMILY_PARAM)),
                        new FilterStringEquals(), familyTypeName, true)));
        }


        public static FilteredElementCollector GetInstancesByElementTypeId(Document doc, ElementId typeId)
        {
            FilterRule rule = ParameterFilterRuleFactory.CreateEqualsRule(new ElementId(BuiltInParameter.ELEM_TYPE_PARAM), typeId);
            FilteredElementCollector collector = new FilteredElementCollector(doc).WherePasses(new ElementParameterFilter(rule));
            return collector.WhereElementIsNotElementType();
        }

        #endregion


        #region Advance Filtered Element

        public static FamilySymbol FindFamilySymbol(Document doc, string familyName, string symbolName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(Family));
            foreach (Family f in collector)
            {
                if (f.Name.Equals(familyName))
                {
                    ISet<ElementId> ids = f.GetFamilySymbolIds();
                    foreach (ElementId id in ids)
                    {
                        FamilySymbol symbol = doc.GetElement(id) as FamilySymbol;
                        if (symbol.Name == symbolName)
                        {
                            return symbol;
                        }
                    }
                }
            }
            return null;
        }


        public static ElementType GetElementTypeByName(Document doc, string name)
        {
            return new FilteredElementCollector(doc).OfClass(typeof(ElementType)).First(q => q.Name.Equals(name)) as ElementType;
        }


        public static ElementType GetFamilySymbolByName(Document doc, string name)
        {
            return new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).First(q => q.Name.Equals(name)) as FamilySymbol;
        }


        public static List<View3D> Get3DViews(Document document, bool template = false, string name = null)
        {
            List<View3D> elements = new List<View3D>();
            int invalidId = ElementId.InvalidElementId.IntegerValue;
            FilteredElementCollector collector = new FilteredElementCollector(document).OfClass(typeof(View3D));
            foreach (View3D view in collector)
            {
                if (invalidId == view.Id.IntegerValue)
                {
                    continue;
                }
                if (template == view.IsTemplate)
                {
                    continue;
                }
                if (null != name)
                {
                    if (view.Name.Contains(name))
                    {
                        continue;
                    }
                }
                elements.Add(view);
            }
            collector.Dispose();
            return elements;
        }


        public static Element GetFirst3dView(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));
            return collector.Cast<View3D>().First(v3 => !v3.IsTemplate);
        }

        #endregion


        #region Parameter Filter Factory

        /// <summary>Contains functions that create appropriate FilterRule objects based on the allParameters given </summary>
        /// <parameter name="ruleSwitch"> 
        /// 0 = CreateEqualsRule;
        /// 1 = CreateGreaterOrEqualRule;
        /// 2 = CreateGreaterOrEqualRule;
        /// 3 = CreateNotEqualsRule;
        /// </parameter>
        /// <returns> FilteredElementCollector </returns>
        public static FilteredElementCollector ParamFilterFactory(FilteredElementCollector collector, ElementId paramId, int value, int ruleSwitch = 0)
        {
            FilterRule filterRule;
            switch (ruleSwitch)
            {
                case 0:
                    filterRule = ParameterFilterRuleFactory.CreateEqualsRule(paramId, value);
                    break;
                case 1:
                    filterRule = ParameterFilterRuleFactory.CreateGreaterOrEqualRule(paramId, value);
                    break;
                case 2:
                    filterRule = ParameterFilterRuleFactory.CreateGreaterOrEqualRule(paramId, value);
                    break;
                case 3:
                    filterRule = ParameterFilterRuleFactory.CreateNotEqualsRule(paramId, value);
                    break;
                default:
                    return collector;
            }
            return collector.WherePasses(new ElementParameterFilter(filterRule));
        }


        public static FilteredElementCollector ParamFilterFactory(FilteredElementCollector collector, ElementId paramId, string value, int ruleSwitch = 0)
        {
            FilterRule filterRule;
            switch (ruleSwitch)
            {
                case 0:
                    filterRule = ParameterFilterRuleFactory.CreateContainsRule(paramId, value, false);
                    break;
                case 1:
                    filterRule = ParameterFilterRuleFactory.CreateBeginsWithRule(paramId, value, false);
                    break;
                case 2:
                    filterRule = ParameterFilterRuleFactory.CreateEndsWithRule(paramId, value, false);
                    break;
                case 3:
                    filterRule = ParameterFilterRuleFactory.CreateNotEqualsRule(paramId, value, false);
                    break;
                default:
                    return collector;
            }
            return collector.WherePasses(new ElementParameterFilter(filterRule));
        }


        public static FilteredElementCollector ParamFilterFactory(FilteredElementCollector collector, ElementId paramId, double value, int ruleSwitch = 0)
        {
            FilterRule filterRule;
            switch (ruleSwitch)
            {
                case 0:
                    filterRule = ParameterFilterRuleFactory.CreateEqualsRule(paramId, value, epsilon);
                    break;
                case 1:
                    filterRule = ParameterFilterRuleFactory.CreateGreaterOrEqualRule(paramId, value, epsilon);
                    break;
                case 2:
                    filterRule = ParameterFilterRuleFactory.CreateGreaterOrEqualRule(paramId, value, epsilon);
                    break;
                case 3:
                    filterRule = ParameterFilterRuleFactory.CreateNotEqualsRule(paramId, value, epsilon);
                    break;
                default:
                    return collector;
            }
            return collector.WherePasses(new ElementParameterFilter(filterRule));
        }

        #endregion


        #region FamilySymbol View Filter
        public static void CreateFilterByFamilySymbol(FamilySymbol symbol)
        {
            Document doc = symbol.Document;
            string symbolName = symbol.Name;
            string familyName = symbol.FamilyName;
            TransactionManager.CreateTransaction(doc, "Createfilter", () =>
            {
                IList<FilterRule> filterRules = new List<FilterRule>();
                ElementId symbolParamId = new ElementId(BuiltInParameter.SYMBOL_NAME_PARAM);
                ElementId familyParamId = new ElementId(BuiltInParameter.ALL_MODEL_FAMILY_NAME);
                ICollection<ElementId> categories = new List<ElementId>() { symbol.Category.Id };
                filterRules.Add(ParameterFilterRuleFactory.CreateEqualsRule(familyParamId, familyName, false));
                filterRules.Add(ParameterFilterRuleFactory.CreateEqualsRule(symbolParamId, symbolName, false));
                ElementFilter elementFilter = CreateElementFilterFromFilterRules(filterRules);
                ParameterFilterElement paramFilter = ParameterFilterElement.Create(doc, symbol.Name, categories, elementFilter);
            });
        }


        public static ElementFilter CreateElementFilterFromFilterRules(IList<FilterRule> filterRules)
        {
            IList<ElementFilter> elemFilters = new List<ElementFilter>();
            foreach (FilterRule filterRule in filterRules)
            {
                elemFilters.Add(new ElementParameterFilter(filterRule));
            }
            return new LogicalAndFilter(elemFilters);
        }

        #endregion


        #region Material filter

        public static Material GetCompoundStructureMaterial(Document doc, Element element, CompoundStructure compound)
        {
            Material material = null;
            if (compound != null)
            {
                double tolerance = 0.05;
                MaterialFunctionAssignment function = MaterialFunctionAssignment.Structure;
                foreach (CompoundStructureLayer layer in compound.GetLayers())
                {
                    if (function == layer.Function && tolerance < layer.Width)
                    {
                        material = doc.GetElement(layer.MaterialId) as Material;
                        material ??= element.Category.Material;
                        tolerance = Math.Round(layer.Width, 3);
                    }
                }
            }
            return material;
        }

        #endregion


        #region Category filter

        public static IEnumerable<Category> GetCategories(Document doc, bool model = true)
        {
            foreach (ElementId catId in ParameterFilterUtilities.GetAllFilterableCategories())
            {
                Category cat = Category.GetCategory(doc, catId);
                if (cat != null && cat.AllowsBoundParameters)
                {
                    if (cat.CategoryType == CategoryType.Model && model)
                    {
                        yield return cat;
                    }
                }
            }
        }


        public static IDictionary<string, Category> GetEngineerCategories(Document doc)
        {
            IDictionary<string, Category> result = new SortedDictionary<string, Category>();
            IList<BuiltInCategory> builtInCats = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_Conduit,
                BuiltInCategory.OST_CableTray,
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_DuctCurves,
                BuiltInCategory.OST_GenericModel,
                BuiltInCategory.OST_MechanicalEquipment
            };
            foreach (BuiltInCategory catId in builtInCats)
            {
                Category cat = Category.GetCategory(doc, catId);
                if (cat != null)
                {
                    result[cat.Name] = cat;
                }
            }
            return result;
        }


        #endregion


        #region Level filter

        public static IEnumerable<Level> GetValidLevels(Document doc)
        {
            FilteredElementCollector collector = null;
            List<ElementId> validIds = new List<ElementId>();
            validIds.AddRange(GetElementsOfCategory(doc, typeof(Wall), BuiltInCategory.OST_Walls).ToElementIds());
            validIds.AddRange(GetElementsOfCategory(doc, typeof(Floor), BuiltInCategory.OST_Floors).ToElementIds());
            foreach (Level level in new FilteredElementCollector(doc).OfClass(typeof(Level)))
            {
                using (ElementLevelFilter levelFilter = new ElementLevelFilter(level.Id))
                {
                    collector = new FilteredElementCollector(doc, validIds);
                    if (collector.WherePasses(levelFilter).Any())
                    {
                        yield return level;
                    }
                }
            }
        }

        #endregion


    }
}
