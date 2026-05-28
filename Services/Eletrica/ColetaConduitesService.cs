using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using AirConditioningClash.Models.Eletrica;

namespace AirConditioningClash.Services.Eletrica
{
    public class ColetaConduitesService
    {
        public List<Conduit> ObterConduites(UIDocument uiDoc, EnumEscopoProcessamentoEletrica escopo)
        {
            if (escopo == EnumEscopoProcessamentoEletrica.TodosDoProjeto)
                return ObterTodosDoProjeto(uiDoc.Document);

            return ObterSelecionados(uiDoc);
        }

        private List<Conduit> ObterSelecionados(UIDocument uiDoc)
        {
            var conduites = new List<Conduit>();

            ICollection<ElementId> ids = uiDoc.Selection.GetElementIds();
            if (ids != null && ids.Count > 0)
            {
                conduites = ids
                    .Select(id => uiDoc.Document.GetElement(id))
                    .OfType<Conduit>()
                    .ToList();

                if (conduites.Any())
                    return conduites;
            }

            IList<Reference> picks = uiDoc.Selection.PickObjects(
                ObjectType.Element,
                new FiltroSelecaoConduite(),
                "Selecione um ou mais conduites");

            return picks
                .Select(x => uiDoc.Document.GetElement(x))
                .OfType<Conduit>()
                .ToList();
        }

        private List<Conduit> ObterTodosDoProjeto(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Conduit))
                .WhereElementIsNotElementType()
                .Cast<Conduit>()
                .ToList();
        }
    }

    public class FiltroSelecaoConduite : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Conduit;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}