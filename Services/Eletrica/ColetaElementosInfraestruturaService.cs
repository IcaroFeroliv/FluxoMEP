using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;

namespace AirConditioningClash.Services.Eletrica
{
    public class ColetaElementosInfraestruturaService
    {
        public List<Element> ObterConduites(Document doc, ElementId phaseId = null)
        {
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(Conduit))
                .WhereElementIsNotElementType();

            if (phaseId != null)
                collector = collector.WherePasses(
                    new ElementPhaseStatusFilter(phaseId, ElementOnPhaseStatus.New));

            return collector.Cast<Element>().ToList();
        }

        public List<Element> ObterBandejasCabos(Document doc, ElementId phaseId = null)
        {
            var collector = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_CableTray)
                .WhereElementIsNotElementType();

            if (phaseId != null)
                collector = collector.WherePasses(
                    new ElementPhaseStatusFilter(phaseId, ElementOnPhaseStatus.New));

            return collector.Cast<Element>().ToList();
        }

        public List<Element> ObterTodosOsElementos(Document doc, ElementId phaseId = null)
        {
            var elementos = new List<Element>();
            elementos.AddRange(ObterConduites(doc, phaseId));
            elementos.AddRange(ObterBandejasCabos(doc, phaseId));
            return elementos;
        }

        public double ObterComprimentoMetros(Element elemento)
        {
            try
            {
                Parameter comprimentoParam = elemento.LookupParameter("Comprimento");
                if (comprimentoParam == null)
                    comprimentoParam = elemento.LookupParameter("Length");

                if (comprimentoParam == null)
                    comprimentoParam = elemento.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH);

                if (comprimentoParam == null)
                    return 0;

                if (comprimentoParam.StorageType == StorageType.Double)
                {
                    double comprimentoInterno = comprimentoParam.AsDouble();
                    return UnitUtils.ConvertFromInternalUnits(comprimentoInterno, UnitTypeId.Meters);
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }
    }
}