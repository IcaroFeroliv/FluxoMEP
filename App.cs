using AirConditioningClash.Commands;
using AirConditioningClash.Updaters;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace AirConditioningClash
{
    // Esta classe roda automaticamente quando o Revit abre
    public class App : IExternalApplication
    {
        public static MonitorConflitos _monitor;

        private void SetIcon(RibbonButton button, string iconPath)
        {
            if (!File.Exists(iconPath))
                return;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            button.LargeImage = bitmap; // Ícone grande (32x32)
            button.Image = bitmap;      // Ícone pequeno (16x16)
        }



        public Result OnStartup(UIControlledApplication application)
        {
            // 1. Criar a Aba (Tab)
            string nomeAba = "FluxoMEP";

            try
            {
                application.CreateRibbonTab(nomeAba);
            }
            catch (Exception)
            {
                // A aba pode já existir (caso tenha outros plugins da empresa), então ignoramos o erro
            }

            // 2. Criar o Painel dentro da Aba
    // --------------------------------------------------
    // Painel 1: Climatização
    // --------------------------------------------------            

            //RibbonPanel painel = application.CreateRibbonPanel(nomeAba, "Compatibilização");
            RibbonPanel painel = application.CreateRibbonPanel(nomeAba, "Climatização");

            // 3. Preparar os dados do Botão
            // Pega o caminho onde esta DLL está salva no computador
            string path = Assembly.GetExecutingAssembly().Location;

            // Cria os dados do botão:
            // "btnCorrigir" = ID interno (único)
            // "Corrigir\nConflito" = O texto que aparece (o \n quebra a linha)
            // path = Onde está o código
            // "AirConditioningClash.ComandoTeste" = O CAMINHO COMPLETO da classe que vai rodar (Namespace + NomeDaClasse)

            string dllFolder = Path.GetDirectoryName(path);

            // --------------------------------------------------
            // BOTÃO 1: RADAR ON/OFF
            // --------------------------------------------------

            PushButtonData botaoRadarData = new PushButtonData(
                "btnRadar",
                "Radar",
                path,
                "AirConditioningClash.Commands.Climatizacao.ComandoRadar"); // Nome da nova classe

            botaoRadarData.ToolTip = "Liga ou Desliga o monitoramento automático de conflitos.";
            PushButton botaoRadar = painel.AddItem(botaoRadarData) as PushButton;
            SetIcon(botaoRadar, Path.Combine(dllFolder, "Resources", "radar.ico"));

            try
            {
                // Cria o monitor
                _monitor = new MonitorConflitos(application.ActiveAddInId);

                // Registra no Revit
                UpdaterRegistry.RegisterUpdater(_monitor);

                // Define o GATILHO: Queremos vigiar TUBOS (PipeCurves)
                ElementCategoryFilter filtroTubos = new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves);

                // Vigiar ADIÇÃO de tubos
                UpdaterRegistry.AddTrigger(_monitor.GetUpdaterId(), filtroTubos, Element.GetChangeTypeElementAddition());

                // Vigiar ALTERAÇÃO DE GEOMETRIA (mexeu, esticou)
                UpdaterRegistry.AddTrigger(_monitor.GetUpdaterId(), filtroTubos, Element.GetChangeTypeGeometry());
            }
            catch (Exception ex)
            {
                // Se der erro, avisa, mas não trava o Revit
                TaskDialog.Show("Erro no Radar", "Falha ao ligar monitoramento: " + ex.Message);
            }


            // --------------------------------------------------
            // BOTÃO 2: Corrigir Conflito
            // --------------------------------------------------

            PushButtonData botaoData = new PushButtonData(
                "btnCorrigir",
                "Corrigir\nConflito",
                path,
                "AirConditioningClash.Commands.Climatizacao.ComandoCorrigirConflito");

            // 4. Adicionar uma dica (Tooltip) que aparece ao passar o mouse
            botaoData.ToolTip = "Seleciona elemento e tubo para realizar desvio automático.";
            PushButton botao = painel.AddItem(botaoData) as PushButton;
            SetIcon(botao, Path.Combine(dllFolder, "Resources", "detour.ico"));


            // --------------------------------------------------
            // BOTÃO 3: Detalhamento
            // --------------------------------------------------

            PulldownButtonData pulldownButtond = new PulldownButtonData(
                            "btndetalhes",
                            "Detalhamento"
                        );
            PulldownButton pulldDownButtond = painel.AddItem(pulldownButtond) as PulldownButton;

            string mainIconPath = Path.Combine(dllFolder, "Resources", "detalhes.ico");
            SetIcon(pulldDownButtond, mainIconPath);

            PushButtonData botaoDetalhamentoData = new PushButtonData(
                "btnDetalhamento",
                "Tags\nPlanta Baixa",
                path,
                "AirConditioningClash.Commands.Climatizacao.ComandoDetalhamento");
            botaoDetalhamentoData.ToolTip = "Insere tags automaticamente nos tubos e equipamentos mecânicos na planta.";

            PushButtonData botaoTag3DData = new PushButtonData(
                "btnTag3D",
                "Tags\nCortes e Vista3D",
                path,
                "AirConditioningClash.Commands.Climatizacao.ComandoTag3D");
            botaoTag3DData.ToolTip = "Insere tags automaticamente nos elementos visíveis na vista 3D ou corte ativa.";

            PushButton botaoDetalhamento = pulldDownButtond.AddPushButton(botaoDetalhamentoData);
            PushButton botaoTag3D = pulldDownButtond.AddPushButton(botaoTag3DData);

            SetIcon(botaoDetalhamento, Path.Combine(dllFolder, "Resources", "planta-baixa.ico"));
            SetIcon(botaoTag3D, Path.Combine(dllFolder, "Resources", "3d.ico"));


            // --------------------------------------------------
            // Painel 2: Hidrossánitario
            // --------------------------------------------------
            RibbonPanel painelHidrossanitario = application.CreateRibbonPanel(nomeAba, "Hidrossanitário");

            // --------------------------------------------------
            // BOTÃO 1: Radar
            // --------------------------------------------------
            PushButtonData botaoHidrossanitarioRadarData = new PushButtonData(
                "btnHidrossanitarioRadar",
                "Radar",
                path,
                "AirConditioningClash.Commands.Hidrossanitario.ComandoRadarHID");
            botaoHidrossanitarioRadarData.ToolTip = "Liga ou Desliga o monitoramento automático de conflitos.";
            PushButton botaoHidrossanitarioRadar = painelHidrossanitario.AddItem(botaoHidrossanitarioRadarData) as PushButton;
            SetIcon(botaoHidrossanitarioRadar, Path.Combine(dllFolder, "Resources", "radarHID.ico"));

            MonitorConflitosHID monitorHID = new MonitorConflitosHID(application.ActiveAddInId);
            UpdaterRegistry.RegisterUpdater(monitorHID);
            ElementClassFilter filtroTubo = new ElementClassFilter(typeof(Pipe));
            UpdaterRegistry.AddTrigger(monitorHID.GetUpdaterId(), filtroTubo, Element.GetChangeTypeElementAddition());
            UpdaterRegistry.AddTrigger(monitorHID.GetUpdaterId(), filtroTubo, Element.GetChangeTypeGeometry());

            // --------------------------------------------------
            // BOTÃO 2: Detalhamento
            // --------------------------------------------------
            
            PulldownButtonData pulldownButtonDataHID = new PulldownButtonData(
                            "btnDetalhesHID",
                            "Detalhamento"
                        );
            PulldownButton pulldDownButtonHID = painelHidrossanitario.AddItem(pulldownButtonDataHID) as PulldownButton;
            string MainIconPathHID = Path.Combine(dllFolder, "Resources", "tagHDS.ico");
            SetIcon(pulldDownButtonHID, MainIconPathHID);

            PushButtonData botaoHidrossanitarioData = new PushButtonData(
                "btnHidrossanitario",
                "Tags\nPlanta Baixa",
                path,
                "AirConditioningClash.Commands.Hidrossanitario.ComandoDetalhamentoHidrossanitario");
            botaoHidrossanitarioData.ToolTip = "Insere tags automaticamente nos tubos.";
            
            PushButtonData botaoTag3DHIDData = new PushButtonData(
                "btnTag3DHID",
                "Tags\nCortes e Vista3D",
                path,
                "AirConditioningClash.Commands.Hidrossanitario.ComandoTag3DHID");
            botaoTag3DHIDData.ToolTip = "Insere tags automaticamente nos elementos visíveis na vista 3D ou corte";

            PushButton botaoDetalhamentoHID = pulldDownButtonHID.AddPushButton(botaoHidrossanitarioData);
            PushButton botaoTag3DHID = pulldDownButtonHID.AddPushButton(botaoTag3DHIDData);
            
            SetIcon(botaoDetalhamentoHID, Path.Combine(dllFolder, "Resources", "plantaHID.ico"));
            SetIcon(botaoTag3DHID, Path.Combine(dllFolder, "Resources", "3dHID.ico"));

            // --------------------------------------------------
            // Painel 3: Elétrica
            // --------------------------------------------------
            RibbonPanel painelEletrica = application.CreateRibbonPanel(nomeAba, "Elétrica");


            // --------------------------------------------------
            // BOTÃO 1: Dimensionamento de Eletrodutos
            // --------------------------------------------------

            PushButtonData botaoEletricaData = new PushButtonData(
                "btnEletrica",
                "Dimensionar\nEletrodutos",
                path,
                "AirConditioningClash.Commands.Eletrica.ComandoDimensionamentoEletrica");

            botaoEletricaData.ToolTip = "Dimensiona eletrodutos com base na ocupação dos cabos.";

            PushButton botaoEletrica = painelEletrica.AddItem(botaoEletricaData) as PushButton;
            SetIcon(botaoEletrica, Path.Combine(dllFolder, "Resources", "conduite.ico"));


            // --------------------------------------------------
            // BOTÃO 2: Exportar Lista de Cabos
            // --------------------------------------------------
            PushButtonData botaoExportarCabosData = new PushButtonData(
                "btnExportarCabos",
                "Exportar\nLista de Cabos",
                path,
                "AirConditioningClash.Commands.Eletrica.ComandoExtracacaoCabosExcel");

            botaoExportarCabosData.ToolTip = "Exporta uma lista de cabos para Excel com base nos fios listados no conduites.";

            PushButton botaoExportar = painelEletrica.AddItem(botaoExportarCabosData) as PushButton;
            SetIcon(botaoExportar, Path.Combine(dllFolder, "Resources", "cabo.ico"));

            // --------------------------------------------------
            // Painel 4: Exportação
            // --------------------------------------------------

            RibbonPanel painelEXT = application.CreateRibbonPanel(nomeAba, "Exportação");

            PulldownButtonData pulldownButtonDataEXT = new PulldownButtonData(
                            "btnExportacao",
                            "Exportação"
                        );
            PulldownButton pulldownButton = painelEXT.AddItem(pulldownButtonDataEXT) as PulldownButton;
            string mainIconExportacao = Path.Combine(dllFolder, "Resources", "exportacao.ico");
            SetIcon(pulldownButton, mainIconExportacao);

            PushButtonData botaoExportarPDFData = new PushButtonData(
                "btnExportarPDF",
                "Exportar\nPDF",
                path,
                "AirConditioningClash.Commands.Exportar.ComandoExportarPDF");
            botaoExportarPDFData.ToolTip = "Exporta a Folhas para PDF.";

            PushButtonData botaoExportarDWGData = new PushButtonData(
                "btnExportarDWG",
                "Exportar\nDWG",
                path,
                "AirConditioningClash.Commands.Exportar.ComandoExportarDWG");
            botaoExportarDWGData.ToolTip = "Exporta a Folhas para DWG.";

            PushButton botaoExportarPDF = pulldownButton.AddPushButton(botaoExportarPDFData);
            PushButton botaoExportarDWG = pulldownButton.AddPushButton(botaoExportarDWGData);
            SetIcon(botaoExportarPDF, Path.Combine(dllFolder, "Resources", "pdf.ico"));
            SetIcon(botaoExportarDWG, Path.Combine(dllFolder, "Resources", "dwg.ico"));

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            if (_monitor != null)
            {
                UpdaterRegistry.UnregisterUpdater(_monitor.GetUpdaterId());
            }

            return Result.Succeeded;
        }
    }
}
