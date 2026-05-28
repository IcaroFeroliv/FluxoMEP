using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using AirConditioningClash.Updaters;

namespace AirConditioningClash.Commands.Climatizacao
{
    [Transaction(TransactionMode.Manual)]
    public class ComandoRadar : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Abre a janela de configuração
            var janela = new Views.Climatizacao.RadarConfigView();
            janela.ShowDialog();

            // Atualiza o estado do Monitor baseado na decisão da janela
            MonitorConflitos.Ligado = janela.LigarRadar;

            if (MonitorConflitos.Ligado)
            {
                TaskDialog.Show("Radar", "Radar Configurado e ATIVADO! 📡\nAgora estou vigiando as categorias selecionadas.");
            }
            else
            {
                TaskDialog.Show("Radar", "Radar DESLIGADO. 💤");
            }

            return Result.Succeeded;
        }
    }
}