using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using AirConditioningClash.Models.Eletrica;

namespace AirConditioningClash.Services.Eletrica
{
    public class AplicacaoConduiteService
    {
        public void AplicarSomenteDiametro(Conduit conduit, InfrastructureCatalogItem infra)
        {
            if (conduit == null)
                return;

            if (infra == null || !infra.DiametroNominalPolegadas.HasValue)
                return;

            Parameter pDiametro = conduit.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM);
            if (pDiametro == null || pDiametro.IsReadOnly)
                return;

            double diameterFeet = ConverterPolegadasParaPes(infra.DiametroNominalPolegadas.Value);
            pDiametro.Set(diameterFeet);
        }

        private double ConverterPolegadasParaPes(double polegadas)
        {
            return polegadas / 12.0;
        }
    }
}