using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace DGJv3
{
    public class SearchModuleForUI : INotifyPropertyChanged
    {
        private SearchModule _SearchModule;
        public SearchModule SearchModule { get => _SearchModule; set => SetField(ref _SearchModule, value); }

        private bool _Selected;
        public bool Selected { get => _Selected; set => SetField(ref _Selected, value); }

        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
