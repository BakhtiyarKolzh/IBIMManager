using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using IBIMTool.Services;
using System.Collections.Generic;

namespace IBIMTool.RevitUtils
{
    internal class RevitFamilyManager
    {

        private static readonly object SingleLocker = new object();
        private static TransactionStatus status = TransactionStatus.Uninitialized;
        public static Family LoadFamily(ref Document doc, in string familyPath)
        {
            Family family = null;
            lock (SingleLocker)
            {
                IFamilyLoadOptions opt = UIDocument.GetRevitUIFamilyLoadOptions();
                using Transaction trx = new Transaction(doc);
                status = trx.Start("Load family");
                if (status == TransactionStatus.Started)
                {
                    if (doc.LoadFamily(familyPath, opt, out family))
                    {
                        status = trx.Commit();
                    }
                    else
                    {
                        status = trx.RollBack();
                        IBIMLogger.Error($"Not Loaded family");
                    }
                };
            }
            return family;
        }


        public static IList<FamilySymbol> GetFamilySymbols(ref Document doc, in Family family)
        {
            IList<FamilySymbol> result = new List<FamilySymbol>(5);
            if (family != null && family.IsValidObject && family.IsEditable)
            {
                foreach (ElementId symbId in family.GetFamilySymbolIds())
                {
                    Element element = doc.GetElement(symbId);
                    if (element is FamilySymbol symbol)
                    {
                        result.Add(symbol);
                    }
                }
            }
            return result;
        }


        public static FamilySymbol GetFamilySymbol(Document doc, in string uniqueId)
        {
            FamilySymbol familySymbol = null;
            if (!string.IsNullOrEmpty(uniqueId))
            {
                if (doc.GetElement(uniqueId) is FamilySymbol symbol)
                {
                    familySymbol = symbol;
                }
            }
            return familySymbol;
        }


        public static void ActivateFamilySimbol(ref Document doc, in FamilySymbol symbol)
        {
            lock (SingleLocker)
            {
                if (symbol.IsValidObject && !symbol.IsActive)
                {
                    using Transaction trx = new Transaction(doc);
                    status = trx.Start("ActivateFamilySimbol");
                    if (status == TransactionStatus.Started)
                    {
                        symbol.Activate();
                        status = trx.Commit();
                    }
                }
            }
        }
    }


}
