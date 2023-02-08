using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace IBIMTool.RevitUtils
{
    internal sealed class WarningSwallower : IFailuresPreprocessor
    {
        private IList<FailureResolutionType> resolutionList;
        private readonly StringBuilder warningText = new StringBuilder();
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failAccessor)
        {
            foreach (FailureMessageAccessor failure in failAccessor.GetFailureMessages())
            {
                if (failure.GetSeverity() == FailureSeverity.None) { continue; }
                resolutionList = failAccessor.GetAttemptedResolutionTypes(failure);
                if (resolutionList.Count > 1)
                {
                    warningText.AppendLine("Cannot resolve failures");
                    return FailureProcessingResult.ProceedWithRollBack;
                }
                else
                {
                    FailureResolutionType resolution = resolutionList.FirstOrDefault();
                    warningText.AppendLine($"Fail: {failure.GetDescriptionText()} {resolution}");
                    if (resolution == FailureResolutionType.Invalid)
                    {
                        return FailureProcessingResult.ProceedWithRollBack;
                    }
                    else if (resolution == FailureResolutionType.DeleteElements)
                    {
                        ICollection<ElementId> ids = failure.GetFailingElementIds();
                        failAccessor.DeleteElements(ids.ToList());
                    }
                    else if (resolution == FailureResolutionType.Others)
                    {
                        failAccessor.DeleteWarning(failure);
                    }
                    else { failAccessor.ResolveFailure(failure); }

                    return FailureProcessingResult.ProceedWithCommit;
                }
            }
            resolutionList?.Clear();
            return FailureProcessingResult.Continue;
        }


        public string GetWarningMessage()
        {
            return "Post Processing Failures: " + warningText.ToString();
        }
    }
}
