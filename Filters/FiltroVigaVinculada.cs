using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System.Linq; // Necessário para usar o método .Contains()

namespace AirConditioningClash.Filters
{
    public class FiltroVigaVinculada : ISelectionFilter
    {
        private Document _docLocal;

        public FiltroVigaVinculada(Document doc)
        {
            _docLocal = doc;
        }

        public bool AllowElement(Element elem)
        {
            // Continua permitindo apenas instâncias de vínculo (RVT Links)
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

            // --- A MUDANÇA ESTÁ AQUI ---

            // Lista do que você aceita como obstáculo
            BuiltInCategory[] categoriasPermitidas = new BuiltInCategory[]
            {
                BuiltInCategory.OST_StructuralFraming,    // Vigas
                BuiltInCategory.OST_StructuralColumns,    // Pilares Estruturais
                BuiltInCategory.OST_Columns,              // Colunas Arquitetônicas
                BuiltInCategory.OST_Floors,               // Pisos / Lajes
                BuiltInCategory.OST_Walls,                // Paredes
                BuiltInCategory.OST_StructuralFoundation, // Fundações
                BuiltInCategory.OST_GenericModel          // Para itens IFC genéricos
            };

            // Verifica se a categoria do elemento está na nossa lista
            if (categoriasPermitidas.Contains(elementoVinculado.Category.BuiltInCategory))
            {
                return true;
            }

            return false;
        }
    }
}