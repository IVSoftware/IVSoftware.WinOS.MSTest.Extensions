using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IVSoftware.WinOS.MSTest.Extensions.STA.WinForms
{
    public partial class CollectionViewRunner : Form, INotifyPropertyChanged
    {
        public CollectionViewRunner()
        {
            InitializeComponent();
        }

        public string InfoText
        {
            get => infoOverlay?.InfoText! ?? string.Empty;
            set => infoOverlay.InfoText = value;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
    class CollectionViewBindingContext : INotifyPropertyChanged
    {
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
