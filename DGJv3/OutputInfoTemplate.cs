using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGJv3
{
    internal class OutputInfoTemplate : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private bool editing = false;
        [JsonIgnore]
        public bool Editing
        {
            get
            {
                return editing;
            }
            set
            {
                editing = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Editing)));
            }
        }

        private string key = null;
        /// <summary>
        /// 这里只模板的文件名称
        /// </summary>
        [JsonProperty("filename")]
        public string Key
        {
            get
            {
                return key;
            }
            set
            {
                if(string.IsNullOrEmpty(value))
                {
                    //不输入的话则不保存
                    return;
                }
                //这里调用事件传过去的this是值未修改的（修改之前的值）
                PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(nameof(Key)));

                key = value.RemoveIllegalCharacterNTFS();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Key)));
            }
        }
        private OutputInfo val = null;
        [JsonProperty("val")]
        public OutputInfo Value
        {
            get
            {
                return val;
            }
            set
            {
                val = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;
    }
}
