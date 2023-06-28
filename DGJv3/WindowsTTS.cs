using DGJv3.API;
using DGJv3.InternalModule;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace DGJv3
{
    public class WindowsTTS : ITTS, INotifyPropertyChanged
    {
        public string UniqueId => "";

        //internal PlayerConfig PlayerConfig { get; }


        internal WindowsTTS()
        {
            //PlayerConfig = playerConfig;
        }

        private void PlayerConfig_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
           
        }

        public Task Speaking(string text)
        {
            var task = Task.Run(() => {
                try
                {

                    using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
                    {
                        //    // 设置语音的名称
                        synthesizer.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);

                        // 设置输出格式为16kHz 16bit Mono PCM
                        using (MemoryStream outputStream = new MemoryStream())
                        {

                            //synthesizer.SetOutputToAudioStream(outputStream,new SpeechAudioFormatInfo(44100, AudioBitsPerSample.Sixteen, AudioChannel.Stereo));
                            synthesizer.SetOutputToWaveStream(outputStream);
                            // 合成语音
                            synthesizer.Speak(text);
                            synthesizer.SetOutputToNull(); // 关闭输出流


                            // 获取合成的音频字节流
                            using (outputStream)
                            {
                                outputStream.Position = 0;
                                SpeechCompletedToPlay.Invoke(this, new SpeechCompletedEventArgs(outputStream));
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Log("WindowsTTS出错了", ex);
                }
            });

            return task;
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
        public event SpeechCompleted SpeechCompletedToPlay;

        private void Log(string message, Exception exception) => LogEvent?.Invoke(this, new LogEventArgs() { Message = message, Exception = exception });


    }
}
