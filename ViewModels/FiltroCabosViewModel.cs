using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using AirConditioningClash.Utils;
using Autodesk.Revit.DB;

namespace AirConditioningClash.ViewModels
{
    public class FiltroCabosViewModel : ViewModelBase
    {
        private bool _aplicarFiltro;
        private Phase _faseSelecionada;

        public ObservableCollection<Phase> Fases { get; } = new ObservableCollection<Phase>();

        public bool AplicarFiltro
        {
            get => _aplicarFiltro;
            set
            {
                _aplicarFiltro = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NaoAplicarFiltro));
                OnPropertyChanged(nameof(FiltroVisivel));
            }
        }

        public bool NaoAplicarFiltro
        {
            get => !_aplicarFiltro;
            set { if (value) AplicarFiltro = false; }
        }

        public bool FiltroVisivel => _aplicarFiltro;

        public Phase FaseSelecionada
        {
            get => _faseSelecionada;
            set { _faseSelecionada = value; OnPropertyChanged(); }
        }

        public bool ExportarConfirmado { get; private set; }

        public ICommand ExportarCommand { get; }
        public ICommand CancelarCommand { get; }
        public Action CloseAction { get; set; }

        public FiltroCabosViewModel(Document doc)
        {
            CarregarFases(doc);
            ExportarCommand = new RelayCommand(ExecuteExportar, CanExecuteExportar);
            CancelarCommand = new RelayCommand(_ => CloseAction?.Invoke());
        }

        private void CarregarFases(Document doc)
        {
            foreach (Phase fase in doc.Phases)
                Fases.Add(fase);

            if (Fases.Count > 0)
                FaseSelecionada = Fases[0];
        }

        private bool CanExecuteExportar(object _) =>
            !AplicarFiltro || FaseSelecionada != null;

        private void ExecuteExportar(object _)
        {
            ExportarConfirmado = true;
            CloseAction?.Invoke();
        }
    }
}
