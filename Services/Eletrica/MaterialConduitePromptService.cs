using Autodesk.Revit.UI;
using AirConditioningClash.Models.Eletrica;

namespace AirConditioningClash.Services.Eletrica
{
    public class MaterialConduitePromptService
    {
        public bool TryPerguntarMaterial(out EnumMaterialConduite material)
        {
            material = EnumMaterialConduite.FerroGalvanizado;

            TaskDialog td = new TaskDialog("Material do Conduíte");
            td.MainInstruction = "Selecione o material do conduite";
            td.MainContent = "Escolha o material que será adotado para o dimensionamento.";
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "ELETRODUTO DE FERRO GALVANIZADO");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "ELETRODUTO DE PEAD");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "ELETRODUTO DE PVC RÍGIDO");
            td.CommonButtons = TaskDialogCommonButtons.Cancel;
            td.DefaultButton = TaskDialogResult.CommandLink1;

            TaskDialogResult result = td.Show();

            if (result == TaskDialogResult.CommandLink1)
            {
                material = EnumMaterialConduite.FerroGalvanizado;
                return true;
            }

            if (result == TaskDialogResult.CommandLink2)
            {
                material = EnumMaterialConduite.Pead;
                return true;
            }

            if (result == TaskDialogResult.CommandLink3)
            {
                material = EnumMaterialConduite.PvcRigido;
                return true;
            }

            return false;
        }

        public string ObterDescricao(EnumMaterialConduite material)
        {
            switch (material)
            {
                case EnumMaterialConduite.FerroGalvanizado:
                    return "ELETRODUTO DE FERRO GALVANIZADO";
                case EnumMaterialConduite.Pead:
                    return "ELETRODUTO DE PEAD";
                case EnumMaterialConduite.PvcRigido:
                    return "ELETRODUTO DE PVC RÍGIDO";
                default:
                    return string.Empty;
            }
        }
    }
}