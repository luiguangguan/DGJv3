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

        public abstract void Speaking(string text);

        public event PropertyChangedEventHandler PropertyChanged;
        public event LogEvent LogEvent;

        protected void Log(string message, Exception exception = null) => LogEvent?.Invoke(this, new LogEventArgs() { Message = message, Exception = exception });


        //        private void WriteError(Exception exception, string description)
        //        {
        //            try
        //            {
        //                string path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        //                using (StreamWriter outfile = new StreamWriter(path + @"\TTS引擎" + ModuleName + "错误报告.txt"))
        //                {
        //                    //outfile.WriteLine("请将错误报告发给 " + ModuleAuthor + " 谢谢，联系方式：" + ModuleContact);
        //                    outfile.WriteLine(description);
        //                    outfile.WriteLine(ModuleName + " 本地时间：" + DateTime.Now.ToString());
        //                    outfile.Write(exception.ToString());
        //                    new Thread(() =>
        //                    {
        //                        System.Windows.MessageBox.Show("TTS引擎“" + ModuleName + @"”遇到了未处理的错误
        //日志已经保存在桌面,请发给引擎作者 " + ModuleAuthor + ", 联系方式：" + ModuleContact);
        //                    }).Start();
        //                }
        //            }
        //            catch (Exception)
        //            { }
        //        }

    }
}
