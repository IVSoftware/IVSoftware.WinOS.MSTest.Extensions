using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            get => _infoText;
            set
            {
                if (!Equals(_infoText, value))
                {
                    _infoText = value;
                    OnPropertyChanged();
                }
            }
        }
        string _infoText = string.Empty;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public event PropertyChangedEventHandler? PropertyChanged;

    }
}
