using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System.Linq;

namespace AirConditioningClash.Filters
{
    public class FiltroObstaculoEletrica : ISelectionFilter
    {
        private Document _docLocal;

        public FiltroObstaculoEletrica(Document doc)
        {
            _docLocal = doc;
        }

        public bool AllowElement(Element elem)
        {
            return elem is RevitLinkInstance;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            if (reference.LinkedElementId == ElementId.InvalidElementId)
                return false;

            RevitLinkInstance linkInstance = _docLocal.GetElement(reference.ElementId) as RevitLinkInstance;
            if (linkInstance == null) return false;

            Document docVinculado = linkInstance.GetLinkDocument();
            if (docVinculado == null) return false;

            Element elementoVinculado = docVinculado.GetElement(reference.LinkedElementId);
            if (elementoVinculado?.Category == null) return false;

            // Categorias elétricas
            BuiltInCategory[] categoriasPermitidas = new BuiltInCategory[]
            {
                BuiltInCategory.OST_Conduit,
                BuiltInCategory.OST_ConduitFitting,
                BuiltInCategory.OST_LightingFixtures,
                BuiltInCategory.OST_GenericModel
            };

            return categoriasPermitidas.Contains(elementoVinculado.Category.BuiltInCategory);
        }
    }
}