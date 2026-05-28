using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using AirConditioningClash.Utils;

namespace AirConditioningClash.ViewModels
{
    public class ConfigNomenclaturaViewModel : ViewModelBase
    {
        // Coleções para as duas listas da interface
        public ObservableCollection<string> AvailableParameters { get; set; }
        public ObservableCollection<string> ChosenParameters { get; set; }

        // Propriedades para saber qual item o usuário clicou nas listas
        private string _selectedAvailableParameter;
        public string SelectedAvailableParameter
        {
            get => _selectedAvailableParameter;
            set { _selectedAvailableParameter = value; OnPropertyChanged(); }
        }

        private string _selectedChosenParameter;
        public string SelectedChosenParameter
        {
            get => _selectedChosenParameter;
            set { _selectedChosenParameter = value; OnPropertyChanged(); }
        }

        private string _separatorText;
        public string SeparatorText
        {
            get => _separatorText;
            set { _separatorText = value; OnPropertyChanged(); }
        }

        // Comandos dos botões
        public ICommand AddParameterCommand { get; }
        public ICommand RemoveParameterCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        // Ações de retorno
        public Action<string> OnSave { get; set; } // Envia a regra pronta de volta para a janela principal
        public Action CloseAction { get; set; }

        // Atualize o construtor para receber a lista:
        public ConfigNomenclaturaViewModel(List<string> parametrosDisponiveis)
        {
            // Agora inicializamos a lista com os dados reais do projeto!
            AvailableParameters = new ObservableCollection<string>(parametrosDisponiveis);

            ChosenParameters = new ObservableCollection<string>();
            SeparatorText = "-";

            AddParameterCommand = new RelayCommand(AddParam, CanAddParam);
            RemoveParameterCommand = new RelayCommand(RemoveParam, CanRemoveParam);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        // Lógica de Mover Parâmetros
        private bool CanAddParam(object obj) => !string.IsNullOrEmpty(SelectedAvailableParameter);
        private void AddParam(object obj)
        {
            ChosenParameters.Add(SelectedAvailableParameter);
            AvailableParameters.Remove(SelectedAvailableParameter);
        }

        private bool CanRemoveParam(object obj) => !string.IsNullOrEmpty(SelectedChosenParameter);
        private void RemoveParam(object obj)
        {
            AvailableParameters.Add(SelectedChosenParameter);
            ChosenParameters.Remove(SelectedChosenParameter);
        }

        // Lógica de Salvar
        private void Save(object obj)
        {
            if (!ChosenParameters.Any())
            {
                System.Windows.MessageBox.Show("Selecione pelo menos um parâmetro para compor o nome.", "ARQ Flow", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Pega os itens escolhidos, coloca os sinais de <> e junta tudo com o separador
            // Exemplo de resultado: "<Número da Folha> - <Nome da Folha>"
            string rule = string.Join($" {SeparatorText} ", ChosenParameters.Select(p => $"<{p}>"));

            // Devolve a regra e fecha
            OnSave?.Invoke(rule);
            CloseAction?.Invoke();
        }

        private void Cancel(object obj)
        {
            CloseAction?.Invoke();
        }
    }
}