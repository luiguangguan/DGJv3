using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGJv3.InternalModule
{
    public delegate void SpeechCompleted(object sender, SpeechCompletedEventArgs e);

    public class SpeechCompletedEventArgs
    {
        public Stream VoiceStream { get; }

        public SpeechCompletedEventArgs(Stream voiceStream)
        {
            VoiceStream = voiceStream;
        }
    }
}
