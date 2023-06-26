using BilibiliDM_PluginFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DGJv3.API
{
    public interface ITTS
	{
		void Speaking(string text);

        string UniqueId { get; }

    }
}
