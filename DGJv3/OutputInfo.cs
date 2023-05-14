using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace DGJv3
{
    internal class OutputInfo : INotifyPropertyChanged
    {
        private bool isEnable = false;
        [JsonProperty("ible")]
        public bool IsEnable
        {
            get => isEnable;
            set
            {
                isEnable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Content"));
            }
        }

        private string content = null;
        [JsonProperty("conten")]
        public string Content
        {
            get => content;
            set
            {
                content = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Content"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
