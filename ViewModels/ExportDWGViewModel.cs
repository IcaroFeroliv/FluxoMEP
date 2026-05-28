using AirConditioningClash.Models;
using AirConditioningClash.Utils;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using AirConditioningClash.Views.Climatizacao;

namespace AirConditioningClash.ViewModels
{
    public class ExportDWGViewModel : ViewModelBase
    {
        private Document _doc;
        public ObservableCollection<SheetModel> Sheets { get; set; }
        public ObservableCollection<ExportDWGSettings> ExportSetups { get; set; }

        private ExportDWGSettings _selectedSetup;
        public ExportDWGSettings SelectedSetup
        {
            get => _selectedSetup;
            set
            {
                _selectedSetup = value;
                OnPropertyChanged();
                UpdateSummary(); // <-- GATILHO ADICIONADO
            }
        }

        private string _outputPath;
        public string OutputPath { get => _outputPath; set { _outputPath = value; OnPropertyChanged(); } }

        private string _namingRuleText = "<Número da Folha> - <Nome da Folha>";
        public string NamingRuleText
        {
            get => _namingRuleText;
            set
            {
                _namingRuleText = value;
                OnPropertyChanged();
                UpdatePreview();
                UpdateSummary(); // <-- GATILHO ADICIONADO
            }
        }

        private string _fileNamePreview;
        public string FileNamePreview { get => _fileNamePreview; set { _fileNamePreview = value; OnPropertyChanged(); } }

        private bool _selectAllSheets;
        public bool SelectAllSheets
        {
            get => _selectAllSheets;
            set
            {
                _selectAllSheets = value;
                if (Sheets != null)
                {
                    foreach (var s in Sheets) s.IsSelected = value;
                }
                OnPropertyChanged();
                UpdatePreview();
                UpdateSummary(); // <-- GATILHO ADICIONADO
            }
        }


        public ICommand BrowseCommand { get; }
        public ICommand OpenNamingConfigCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand CancelCommand { get; }
        public Action CloseAction { get; set; }
        
        private string _summaryText;
        public string SummaryText { get => _summaryText; set { _summaryText = value; OnPropertyChanged(); } }

        private void UpdateSummary()
        {
            int count = Sheets.Count(s => s.IsSelected);
            string setup = SelectedSetup != null ? SelectedSetup.Name : "Nenhum";
            SummaryText = $"Você selecionou {count} folha(s).\nSetup: {setup}\nPadrão: {NamingRuleText}";
        }

        public ExportDWGViewModel(Document doc)
        {
            _doc = doc;
            Sheets = new ObservableCollection<SheetModel>();
            OutputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // Carrega Setups de DWG
            ExportSetups = new ObservableCollection<ExportDWGSettings>(
                new FilteredElementCollector(doc).OfClass(typeof(ExportDWGSettings)).Cast<ExportDWGSettings>()
            );
            SelectedSetup = ExportSetups.FirstOrDefault();

            LoadSheets();

            BrowseCommand = new RelayCommand(o => {
                var dialog = new Microsoft.Win32.OpenFolderDialog();
                if (dialog.ShowDialog() == true) OutputPath = dialog.FolderName;
            });

            OpenNamingConfigCommand = new RelayCommand(ExecuteOpenNamingConfig);
            CancelCommand = new RelayCommand(o => CloseAction?.Invoke());
            ExportCommand = new RelayCommand(ExecuteExport, o => Sheets.Any(s => s.IsSelected) && SelectedSetup != null);
        }

        private void LoadSheets()
        {
            var collector = new FilteredElementCollector(_doc).OfClass(typeof(ViewSheet)).Cast<ViewSheet>().Where(s => !s.IsPlaceholder);
            foreach (var s in collector)
            {
                var m = new SheetModel { RevitSheet = s, Number = s.SheetNumber, Name = s.Name };

                // Atualiza quando o usuário clica num checkbox individual
                m.PropertyChanged += (se, ee) =>
                {
                    UpdatePreview();
                    UpdateSummary(); // <-- GATILHO ADICIONADO
                };

                Sheets.Add(m);
            }

            // Atualiza logo que a janela abre
            UpdatePreview();
            UpdateSummary(); // <-- GATILHO ADICIONADO
        }

        private void UpdatePreview()
        {
            var sheetModel = Sheets.FirstOrDefault(x => x.IsSelected) ?? Sheets.FirstOrDefault();

            if (sheetModel != null && sheetModel.RevitSheet != null)
            {
                string preview = NamingRuleText;

                // Busca todas as tags customizadas
                var tags = Regex.Matches(NamingRuleText, @"<(.*?)>");
                foreach (Match match in tags)
                {
                    string pNome = match.Groups[1].Value;

                    string valor = "";
                    if (pNome == "Número da Folha") valor = sheetModel.Number;
                    else if (pNome == "Nome da Folha") valor = sheetModel.Name;
                    else
                    {
                        Parameter param = sheetModel.RevitSheet.LookupParameter(pNome);
                        if (param != null && param.HasValue)
                            valor = param.AsValueString() ?? param.AsString() ?? "";
                    }

                    preview = preview.Replace(match.Value, valor);
                }

                FileNamePreview = $"{preview}.dwg";
            }
        }

        private void ExecuteOpenNamingConfig(object obj)
        {
            // 1. Pega a primeira folha da lista para extrair os parâmetros do seu projeto
            var primeiraFolha = Sheets.FirstOrDefault()?.RevitSheet;
            List<string> parametrosDaFolha = new List<string> { "Número da Folha", "Nome da Folha" }; // Garante os dois principais no topo

            if (primeiraFolha != null)
            {
                foreach (Parameter param in primeiraFolha.Parameters)
                {
                    string nomeParam = param.Definition.Name;

                    // Verifica se o parâmetro tem nome e se já não foi adicionado
                    if (!string.IsNullOrEmpty(nomeParam) && !parametrosDaFolha.Contains(nomeParam))
                    {
                        parametrosDaFolha.Add(nomeParam);
                    }
                }
            }

            // 2. Instancia a janela enviando a lista completa de parâmetros do Revit
            var configVM = new ConfigNomenclaturaViewModel(parametrosDaFolha);
            var win = new Views.Climatizacao.ConfigNomenclaturaWindow { DataContext = configVM };

            configVM.CloseAction = () => win.Close();
            configVM.OnSave = (rule) => NamingRuleText = rule;

            win.ShowDialog();
        }

        private void ExecuteExport(object obj)
        {
            try
            {
                // Separa as folhas que o usuário marcou
                var folhasParaExportar = Sheets.Where(x => x.IsSelected).ToList();

                // Executa o serviço de exportação
                var service = new Services.ExportDWGService();
                service.ExportarParaDWG(_doc, folhasParaExportar, OutputPath, NamingRuleText, SelectedSetup);

                // Mensagem de sucesso moderna e detalhada
                System.Windows.MessageBox.Show(
                    $"{folhasParaExportar.Count} arquivo(s) DWG exportado(s) com sucesso!\n\nSalvos na pasta:\n{OutputPath}",
                    "Fluxo MEP - Sucesso",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                CloseAction?.Invoke();
            }
            catch (Exception ex)
            {
                // Mensagem de erro amigável caso algo dê errado
                System.Windows.MessageBox.Show(
                    $"Ocorreu um erro durante a exportação:\n{ex.Message}",
                    "Fluxo MEP - Erro",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}