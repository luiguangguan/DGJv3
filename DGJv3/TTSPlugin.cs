using DGJv3.API;
using DGJv3.InternalModule;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DGJv3
{
    internal class TTSPlugin
    {
        public ObservableCollection<TTS> TTSlist;

        public WindowsTTS WindowsTTS;

        private ITTS _currentTTS;
        public ITTS CurrentTTS { get => _currentTTS; set => SetField(ref _currentTTS, value, nameof(CurrentTTS)); }

        private TTSPluginType _ttsType;
        public TTSPluginType TtsType { get => _ttsType; set => SetField(ref _ttsType, value, nameof(TtsType)); }

        private bool _ttsEnbale;
        public bool TTSPluginEnbale { get => _ttsEnbale; set => SetField(ref _ttsEnbale, value, nameof(TTSPluginEnbale)); }

        public TTSPlugin(WindowsTTS windowsTTS, ObservableCollection<TTS> ttsList)
        {
            TTSlist = ttsList;
            WindowsTTS = windowsTTS;
            TTSlist.CollectionChanged += TTSlist_CollectionChanged;
        }

        /// <summary>
        /// 合成语音
        /// </summary>
        /// <param name="text"></param>
        public void Speaking(string text)
        {
            try
            {
                if (TTSPluginEnbale)
                {
                    if (TtsType == TTSPluginType.InternalTTS)
                    {
                        WindowsTTS?.Speaking(text);
                    }
                    else
                    {
                        CurrentTTS?.Speaking(text);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("语音合成出错", ex);
            }
        }

        private void TTSlist_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (CurrentTTS == null)
            {
                CurrentTTS = TTSlist?.FirstOrDefault(x => x.UniqueId == Config.Load()?.TtsPluginId);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public event LogEvent LogEvent;
        private void Log(string message, Exception exception = null) => LogEvent?.Invoke(this, new LogEventArgs() { Message = message, Exception = exception });
    }
}
