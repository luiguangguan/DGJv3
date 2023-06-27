using BilibiliDM_PluginFramework;
using DGJv3.InternalModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGJv3.API
{
    public interface ITTS
	{
		Task Speaking(string text);

        string UniqueId { get; }

        event SpeechCompleted SpeechCompletedToPlay;

    }
}
