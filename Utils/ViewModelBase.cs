using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AirConditioningClash.Utils
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Método que avisa o XAML que uma propriedade foi alterada
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}