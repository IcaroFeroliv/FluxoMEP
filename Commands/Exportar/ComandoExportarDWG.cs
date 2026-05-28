using AirConditioningClash.ViewModels;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace AirConditioningClash.Commands.Exportar
{
    [Transaction(TransactionMode.Manual)]
    public class ComandoExportarDWG : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            var vm = new ExportDWGViewModel(doc);
            var win = new Views.Climatizacao.ExportDWG { DataContext = vm };
            vm.CloseAction = () => win.Close();
            win.ShowDialog();
            return Result.Succeeded;
        }
    }
}