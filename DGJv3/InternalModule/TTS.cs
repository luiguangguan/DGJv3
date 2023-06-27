using DGJv3.API;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DGJv3.InternalModule
{
    public abstract class TTS : ITTS, INotifyPropertyChanged
    {
        public string ModuleName { get; private set; } = "TTS模块";
        public string ModuleAuthor { get; private set; } = "模块作者";
        public string ModuleContact { get; private set; } = "联系方式";

        public TTS(string Name, string Author, string Contact)
        {
            ModuleName = Name;
            ModuleAuthor = Author;
            ModuleContact = Contact;
        }

        public string UniqueId
        {
            get
            {
                if (uniqueId == null)
                {
                    using (MD5 md5 = MD5.Create())
                    {
                        uniqueId = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes($"{GetType().FullName}{ModuleName}{ModuleAuthor}"))).Replace("-", "");
                    }
                }

                return uniqueId;
            }
        }

        private string uniqueId = null;

        public abstract Task Speaking(string text);

        public abstract event PropertyChangedEventHandler PropertyChanged;
        public virtual event LogEvent LogEvent;
        public abstract event SpeechCompleted SpeechCompletedToPlay;

        protected void Log(string message, Exception exception = null) => LogEvent?.Invoke(this, new LogEventArgs() { Message = message, Exception = exception });

    }
}
