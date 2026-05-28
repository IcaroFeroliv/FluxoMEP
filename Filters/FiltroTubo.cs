using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace AirConditioningClash.Filters
{
    public class FiltroTubo : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            // Verifica se a categoria é Tubulação (PipeCurves)
            // Você pode adicionar OST_DuctCurves se quiser aceitar Dutos também
            if (elem.Category != null &&
                elem.Category.BuiltInCategory == BuiltInCategory.OST_PipeCurves)
            {
                return true;
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return false; // Não precisamos verificar referências para objetos locais neste caso
        }
    }
}