using System;
using AirConditioningClash.ViewModels;
using AirConditioningClash.Views;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace AirConditioningClash.Commands.Exportar
{
    // O TransactionMode.Manual é obrigatório porque nós mesmos estamos
    // abrindo e fechando a transação lá no nosso ExportService.
    [Transaction(TransactionMode.Manual)]
    public class ComandoExportarPDF : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // 1. Pega o documento atual do Revit
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // 2. Cria o "Cérebro" (ViewModel) passando o documento atual para ele
                var viewModel = new ExportPDFViewModel(doc);

                // 3. Cria a Janela (View)
                var window = new Views.Climatizacao.ExportPDF
                {
                    DataContext = viewModel // Conecta o Cérebro à Janela
                };

                // 4. Ensina o ViewModel como fechar esta janela específica
                viewModel.CloseAction = () => window.Close();

                // 5. Abre a janela de forma Modal (o Revit fica "pausado" esperando o usuário terminar)
                // Isso é essencial para podermos rodar a Transação de exportação com segurança!
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}