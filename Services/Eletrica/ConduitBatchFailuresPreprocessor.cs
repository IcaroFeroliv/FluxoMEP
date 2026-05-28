using System.Linq;
using Autodesk.Revit.DB;

namespace AirConditioningClash.Services.Eletrica
{
    public class ConduitBatchFailuresPreprocessor : IFailuresPreprocessor
    {
        public string UltimaMensagemErro { get; private set; }

        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            var failureMessages = failuresAccessor.GetFailureMessages();

            if (failureMessages == null || failureMessages.Count == 0)
                return FailureProcessingResult.Continue;

            bool possuiErro = false;

            foreach (FailureMessageAccessor failure in failureMessages.ToList())
            {
                FailureSeverity severity = failure.GetSeverity();

                if (severity == FailureSeverity.Warning)
                {
                    failuresAccessor.DeleteWarning(failure);
                    continue;
                }

                if (severity == FailureSeverity.Error || severity == FailureSeverity.DocumentCorruption)
                {
                    possuiErro = true;
                    UltimaMensagemErro = failure.GetDescriptionText();
                }
            }

            if (possuiErro)
            {
                return FailureProcessingResult.ProceedWithRollBack;
            }

            return FailureProcessingResult.Continue;
        }
    }
}