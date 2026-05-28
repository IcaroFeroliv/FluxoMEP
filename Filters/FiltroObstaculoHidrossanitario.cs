using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System.Linq;

namespace AirConditioningClash.Filters
{
    public class FiltroObstaculoHidrossanitario : ISelectionFilter
    {
        private Document _docLocal;

        public FiltroObstaculoHidrossanitario(Document doc)
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

            // Categorias hidrossanitárias
            BuiltInCategory[] categoriasPermitidas = new BuiltInCategory[]
            {
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_PipeFitting,
                BuiltInCategory.OST_PipeAccessory,
                BuiltInCategory.OST_GenericModel
            };

            return categoriasPermitidas.Contains(elementoVinculado.Category.BuiltInCategory);
        }
    }
}