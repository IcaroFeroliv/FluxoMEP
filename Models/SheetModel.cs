using AirConditioningClash.Utils;
using Autodesk.Revit.DB;

namespace AirConditioningClash.Models
{
    // Herdamos de ViewModelBase para que o CheckBox "IsSelected" atualize a UI em tempo real
    public class SheetModel : ViewModelBase
    {
        private bool _isSelected;

        // Propriedade ligada à coluna de Checkbox no XAML
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string Number { get; set; }
        public string Name { get; set; }

        // Guardamos o elemento real do Revit para podermos exportá-lo no final!
        public ViewSheet RevitSheet { get; set; }
    }
}