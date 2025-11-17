using IVSoftware.WinOS.MSTest.Extensions.STA.OP;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace IVSoftware.WinOS.MSTest.Extensions.STA.WinForms
{
    public partial class CollectionViewRunner
        : Form
        , INotifyPropertyChanged
        , IInfoContentForm
    {
        public CollectionViewRunner()
        {
            InitializeComponent();
            base.DataContext = new CollectionViewDataContext();
            DataContext.PropertyChanged += (sender, e) =>
            {
                Debug.Fail($@"ADVISORY - First Time.");
            };
        }
        new CollectionViewDataContext DataContext => (CollectionViewDataContext)base.DataContext;
        public string InfoText
        {
            get => infoOverlay?.InfoText! ?? string.Empty;
            set => infoOverlay.InfoText = value;
        }
        public bool IsVisbleDSA 
        {
            get => ((IInfoContentForm)infoOverlay).IsVisbleDSA;
            set => ((IInfoContentForm)infoOverlay).IsVisbleDSA = value;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
    class CollectionViewDataContext : INotifyPropertyChanged
    {
        public IList? ItemsSource
        {
            get => _itemsSource;
            set
            {
                if (!Equals(_itemsSource, value))
                {
                    _itemsSource = value;
                    OnPropertyChanged();
                }
            }
        }
        IList? _itemsSource = null;


        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
