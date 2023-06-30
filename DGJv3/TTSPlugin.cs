using DGJv3.API;
using DGJv3.InternalModule;
using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace DGJv3
{
    internal class TTSPlugin : INotifyPropertyChanged
    {
        public PlayerConfig PlayerConfig { get; }
        private SampleChannel sampleChannel;

        public ObservableCollection<TTS> TTSlist;

        public WindowsTTS WindowsTTS;

        private TTS _currentTTS;
        public TTS CurrentTTS { get => _currentTTS; set => SetField(ref _currentTTS, value, nameof(CurrentTTS)); }

        private TTSPluginType _ttsType;
        public TTSPluginType TtsType { get => _ttsType; set => SetField(ref _ttsType, value, nameof(TtsType)); }

        private bool _ttsEnbale;
        public bool TTSPluginEnbale { get => _ttsEnbale; set => SetField(ref _ttsEnbale, value, nameof(TTSPluginEnbale)); }

        private EventSafeQueue<byte[]> VoiceQueue = new EventSafeQueue<byte[]>();
        private string Locker = Guid.NewGuid().ToString();

        public string TestText { get => _testText; set => SetField(ref _testText, value, nameof(TestText)); }
        private string _testText = "语音测试";

        public TTSPlugin(WindowsTTS windowsTTS, ObservableCollection<TTS> ttsList, PlayerConfig playerConfig)
        {
            TTSlist = ttsList;
            WindowsTTS = windowsTTS;
            windowsTTS.SpeechCompletedToPlay += SpeechCompletedToPlay;
            TTSlist.CollectionChanged += TTSlist_CollectionChanged;
            PlayerConfig = playerConfig;
            VoiceQueue.Enqueued += VoiceQueue_Enqueued;
            playerConfig.PropertyChanged += TTSPlugin_PropertyChanged;

        }

        private void TTSPlugin_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlayerConfig.Volume))
            {
                //音量改变
                if (sampleChannel != null)
                    sampleChannel.Volume = PlayerConfig.Volume;
            }
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
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null && e.NewItems.Count > 0)
                {
                    for (int i = 0; i < e.NewItems.Count; i++)
                    {
                        var tts = (TTS)e.NewItems[i];
                        tts.SpeechCompletedToPlay += SpeechCompletedToPlay;
                    }
                }
            }
        }

        private void SpeechCompletedToPlay(object sender, SpeechCompletedEventArgs e)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                e.VoiceStream.CopyTo(memoryStream);
                byte[] vioceData = memoryStream.ToArray();
                if (vioceData.Length <= 0)
                {
                    Log("语音数据流长度为了0，请检查你的TTS应用的设置");
                    return;
                }
                VoiceQueue.Enqueue(vioceData);
            }
        }

        private void VoiceQueue_Enqueued(object obj, byte[] streamByte)
        {
            byte[] bytes;
            if (VoiceQueue.TryDequeue(out bytes))
            {
                PliayVoice(bytes);
            }
        }

        private Task PliayVoice(byte[] streamByte)
        {
            var task = Task.Run(() =>
            {
                lock (Locker)
                {
                    try
                    {
                        using (MemoryStream stream = new MemoryStream(streamByte))
                        {

                            IWavePlayer wavePlayer;
                            stream.Seek(0, SeekOrigin.Begin);

                            //using (WaveStream waveStream = new RawSourceWaveStream(e.VoiceStream, new WaveFormat(44100,16,2)))
                            using (WaveFileReader waveStream = new WaveFileReader(stream))
                            using (wavePlayer = PlayerConfig.CreateIWavePlayer())
                            {
                                //wavePlayer.PlaybackStopped += (sender, e) => { };
                                sampleChannel = new SampleChannel(waveStream)
                                {
                                    Volume = PlayerConfig.Volume
                                };
                                wavePlayer.Init(sampleChannel);
                                wavePlayer.Play();
                                while (wavePlayer.PlaybackState == PlaybackState.Playing)
                                {
                                    Thread.Sleep(100);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("NAudio播放声音出错", ex);
                    }
                }
            });
            return task;
        }

        private void WavePlayer_PlaybackStopped(object sender, StoppedEventArgs e)
        {
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
