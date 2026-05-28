using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using AirConditioningClash.Models;
using AirConditioningClash.Utils;
using Autodesk.Revit.DB;
using System.Text.RegularExpressions;

namespace AirConditioningClash.ViewModels
{
    public class ExportPDFViewModel : ViewModelBase
    {
        private Document _doc;
        private string _outputPath;
        private string _namingRuleText;
        private string _fileNamePreview;
        private string _summaryText;

        // Lista observável: se adicionar/remover itens, a UI atualiza sozinha
        public ObservableCollection<SheetModel> Sheets { get; set; }

        public string OutputPath
        {
            get => _outputPath;
            set { _outputPath = value; OnPropertyChanged(); UpdateSummary(); }
        }

        public string NamingRuleText
        {
            get => _namingRuleText;
            set { _namingRuleText = value; OnPropertyChanged(); UpdatePreview(); }
        }

        public string FileNamePreview
        {
            get => _fileNamePreview;
            set { _fileNamePreview = value; OnPropertyChanged(); }
        }

        public string SummaryText
        {
            get => _summaryText;
            set { _summaryText = value; OnPropertyChanged(); }
        }

        // Comandos para os botões
        public ICommand BrowseCommand { get; }
        public ICommand OpenNamingConfigCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ExportCommand { get; }

        // Ação para fechar a janela a partir do ViewModel
        public Action CloseAction { get; set; }

        private bool _selectAllSheets;
        public bool SelectAllSheets
        {
            get => _selectAllSheets;
            set
            {
                _selectAllSheets = value;
                OnPropertyChanged();

                // Marca ou desmarca todas as folhas da lista
                if (Sheets != null)
                {
                    foreach (var sheet in Sheets)
                    {
                        sheet.IsSelected = _selectAllSheets;
                    }
                }

                UpdatePreview();
                UpdateSummary();
            }
        }

        public ExportPDFViewModel(Document doc)
        {
            _doc = doc;
            Sheets = new ObservableCollection<SheetModel>();

            // Valores padrão iniciais
            NamingRuleText = "<Número da Folha> - <Nome da Folha>";
            OutputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            LoadSheets();

            // Inicializa os comandos
            BrowseCommand = new RelayCommand(ExecuteBrowse);
            OpenNamingConfigCommand = new RelayCommand(ExecuteOpenNamingConfig);
            CancelCommand = new RelayCommand(ExecuteCancel);
            ExportCommand = new RelayCommand(ExecuteExport, CanExecuteExport);
        }

        private void LoadSheets()
        {
            // API do Revit: Puxa todas as folhas que não são placeholders
            var collector = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .Where(s => !s.IsPlaceholder)
                .OrderBy(s => s.SheetNumber);

            foreach (var sheet in collector)
            {
                var model = new SheetModel
                {
                    RevitSheet = sheet,
                    Number = sheet.SheetNumber,
                    Name = sheet.Name,
                    IsSelected = false // Começa tudo desmarcado
                };

                // Assina o evento para atualizar o resumo se o usuário marcar/desmarcar
                model.PropertyChanged += SheetModel_PropertyChanged;
                Sheets.Add(model);
            }

            UpdatePreview();
            UpdateSummary();
        }

        private void SheetModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SheetModel.IsSelected))
            {
                UpdatePreview();
                UpdateSummary();
            }
        }

        private void UpdatePreview()
        {
            // Pega a primeira folha selecionada, ou a primeira da lista se nenhuma estiver marcada
            var sheetModel = Sheets.FirstOrDefault(s => s.IsSelected) ?? Sheets.FirstOrDefault();

            if (sheetModel != null && sheetModel.RevitSheet != null)
            {
                string preview = NamingRuleText;

                // Substitui os fixos (pois já temos propriedades fáceis para eles)
                preview = preview.Replace("<Número da Folha>", sheetModel.Number);
                preview = preview.Replace("<Nome da Folha>", sheetModel.Name);

                // Busca todas as outras tags customizadas (tudo que está entre < e >)
                var tags = Regex.Matches(NamingRuleText, @"<(.*?)>");
                foreach (Match match in tags)
                {
                    string tagCompleta = match.Value; // Ex: "<Desenhado por>"
                    string nomeParametro = match.Groups[1].Value; // Ex: "Desenhado por"

                    // Evita refazer os que já fizemos
                    if (nomeParametro != "Número da Folha" && nomeParametro != "Nome da Folha")
                    {
                        string valor = ObterValorParametroPreview(sheetModel.RevitSheet, nomeParametro);
                        preview = preview.Replace(tagCompleta, valor);
                    }
                }

                FileNamePreview = $"{preview}.pdf";
            }
        }

        // Método auxiliar para extrair o valor do parâmetro da folha para o Preview
        private string ObterValorParametroPreview(ViewSheet folha, string nomeParametro)
        {
            Parameter param = folha.LookupParameter(nomeParametro);
            if (param != null && param.HasValue)
            {
                // AsValueString() pega o valor formatado (ex: datas), AsString() pega texto simples
                return param.AsValueString() ?? param.AsString() ?? "";
            }
            return "";
        }

        private void UpdateSummary()
        {
            int selectedCount = Sheets.Count(s => s.IsSelected);
            SummaryText = $"Você selecionou {selectedCount} folha(s) para exportar.\n" +
                          $"Os arquivos serão salvos em: {OutputPath}\n" +
                          $"Padrão de nome: {NamingRuleText}";
        }

        private void ExecuteBrowse(object obj)
        {
            // Usa o seletor de pastas nativo do WPF / .NET 8
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Selecione a pasta para salvar os PDFs"
            };

            if (dialog.ShowDialog() == true)
            {
                OutputPath = dialog.FolderName;
            }
        }

        private void ExecuteOpenNamingConfig(object obj)
        {
            // 1. Pega os parâmetros da primeira folha do projeto para servir de menu
            var primeiraFolha = Sheets.FirstOrDefault()?.RevitSheet;
            List<string> parametrosDaFolha = new List<string> { "Número da Folha", "Nome da Folha" }; // Garante esses dois no topo

            if (primeiraFolha != null)
            {
                foreach (Parameter param in primeiraFolha.Parameters)
                {
                    string nomeParam = param.Definition.Name;

                    // Adiciona se tiver nome e se já não estiver na lista
                    if (!string.IsNullOrEmpty(nomeParam) && !parametrosDaFolha.Contains(nomeParam))
                    {
                        parametrosDaFolha.Add(nomeParam);
                    }
                }
            }

            // 2. Instancia o ViewModel passando a lista dinâmica!
            var configVM = new ConfigNomenclaturaViewModel(parametrosDaFolha);

            var configWindow = new Views.Climatizacao.ConfigNomenclaturaWindow
            {
                DataContext = configVM
            };

            configVM.CloseAction = () => configWindow.Close();
            configVM.OnSave = (novaRegra) =>
            {
                NamingRuleText = novaRegra;
            };

            configWindow.ShowDialog();
        }

        private void ExecuteCancel(object obj)
        {
            CloseAction?.Invoke();
        }

        private bool CanExecuteExport(object obj)
        {
            // Só habilita o botão de exportar se tiver pelo menos 1 folha selecionada
            return Sheets.Any(s => s.IsSelected);
        }

        private void ExecuteExport(object obj)
        {
            try
            {
                // Pega apenas as folhas que o usuário marcou com o checkbox
                var folhasParaExportar = Sheets.Where(s => s.IsSelected).ToList();

                // Instancia o nosso novo serviço
                var exportService = new Services.ExportService();

                // Chama o método que faz o trabalho pesado
                exportService.ExportarParaPDF(_doc, folhasParaExportar, OutputPath, NamingRuleText);

                System.Windows.MessageBox.Show(
                    $"{folhasParaExportar.Count} folha(s) exportada(s) com sucesso!\n\nSalvas em:\n{OutputPath}",
                    "Fluxo MEP - Sucesso",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                // Fecha a janela após terminar
                CloseAction?.Invoke();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Ocorreu um erro ao exportar: {ex.Message}",
                    "Fluxo MEP - Erro",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}