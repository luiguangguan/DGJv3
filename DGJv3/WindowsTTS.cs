using DGJv3.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace DGJv3
{
    public class WindowsTTS : ITTS
    {
        public string UniqueId => "";

        public void Speaking(string text)
        {
            try
            {
                using (SpeechSynthesizer synth = new SpeechSynthesizer())
                {
                    // 设置语音的名称
                    synth.SelectVoiceByHints(VoiceGender.Female, VoiceAge.Adult);
                    //synth.SelectVoice("Microsoft Zira Desktop"); // 根据语音名称选择
                    // 播放文本
                    synth.Speak(text);
                }
                //LogEvent += WindowsTTS_LogEvent;
            }
            catch (Exception ex)
            {
                Log("WindowsTTS出错了", ex);
            }
        }

        public event LogEvent LogEvent;
        private void Log(string message, Exception exception) => LogEvent?.Invoke(this, new LogEventArgs() { Message = message, Exception = exception });


    }
}
