using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using AirConditioningClash.Updaters;

namespace AirConditioningClash.Commands.Hidrossanitario
{
    [Transaction(TransactionMode.Manual)]
    public class ComandoRadarHID : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // AQUI É A MUDANÇA: Instancia a janela HID
            var janela = new Views.Climatizacao.RadarConfigViewHID();
            janela.ShowDialog();

            MonitorConflitosHID.Ligado = janela.LigarRadar;

            if (MonitorConflitosHID.Ligado)
            {
                TaskDialog.Show("Radar HID", "Radar Hidrossanitário ATUALIZADO e Ativado! 🚿");
            }
            else
            {
                TaskDialog.Show("Radar HID", "Radar Hidrossanitário Desligado.");
            }

            return Result.Succeeded;
        }
    }
}