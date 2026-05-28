using Autodesk.Revit.UI;
using AirConditioningClash.Models.Eletrica;

namespace AirConditioningClash.Services.Eletrica
{
    public class EscopoProcessamentoPromptService
    {
        public bool TryPerguntarEscopo(out EnumEscopoProcessamentoEletrica escopo)
        {
            escopo = EnumEscopoProcessamentoEletrica.Selecionados;

            TaskDialog td = new TaskDialog("Escopo do processamento");
            td.MainInstruction = "Como deseja processar os conduites?";
            td.MainContent = "Escolha o escopo do dimensionamento.";
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Selecionar um ou mais conduites");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Todos os conduites do projeto");
            td.CommonButtons = TaskDialogCommonButtons.Cancel;
            td.DefaultButton = TaskDialogResult.CommandLink1;

            TaskDialogResult result = td.Show();

            if (result == TaskDialogResult.CommandLink1)
            {
                escopo = EnumEscopoProcessamentoEletrica.Selecionados;
                return true;
            }

            if (result == TaskDialogResult.CommandLink2)
            {
                escopo = EnumEscopoProcessamentoEletrica.TodosDoProjeto;
                return true;
            }

            return false;
        }
    }
}