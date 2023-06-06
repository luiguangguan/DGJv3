using Scriban;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows.Interop;
using System.Windows.Markup.Localizer;
using static System.Net.Mime.MediaTypeNames;

namespace DGJv3
{
    class Writer : INotifyPropertyChanged
    {
        private static string locker = Guid.NewGuid().ToString();
        private static bool isLock2=false;

        private static bool isLock = false;

        private ObservableCollection<SongItem> Songs;

        private ObservableCollection<SongInfo> Playlist;

        private Player Player;

        private DanmuHandler DanmuHandler;

        private Timer timetimer;
        private Timer timer;
        private Timer queuemsg_timer;

        private Template template = null;

        private Dictionary<string, TemplateRender> templates = new  Dictionary<string, TemplateRender>();

        private string CurrentLyric = "";

        private string UpcomingLyric = "";

        public List<Notification> keepingQueue = new List<Notification>() ;

        private EventSafeQueue<object>  _msgQueue;
        public EventSafeQueue<object> MsgQueue { get => _msgQueue; set => SetField(ref _msgQueue, value,nameof(MsgQueue)); }

        public int  QueueMsgMaxStayTime { get => _queueMsgMaxStayTime; set => SetField(ref _queueMsgMaxStayTime, value,nameof(QueueMsgMaxStayTime)); }
        private int _queueMsgMaxStayTime;
        public bool EnableQueueMsg { get => _enableQueueMsg; set => SetField(ref _enableQueueMsg, value, nameof(EnableQueueMsg)); }
        private bool _enableQueueMsg ;

        public bool EnableKeepLastQueueMsg { get => _enableKeepLastQueueMsg; set => SetField(ref _enableKeepLastQueueMsg, value, nameof(EnableKeepLastQueueMsg)); }
        private bool _enableKeepLastQueueMsg;
        public int KeepQueueMsgCount { get => _keepQueueMsgCount; set => SetField(ref _keepQueueMsgCount, value, nameof(KeepQueueMsgCount)); }
        private int _keepQueueMsgCount;
        public int MsgContainerMaxSize { get => _msgContainerMaxSize; set => SetField(ref _msgContainerMaxSize, value, nameof(MsgContainerMaxSize)); }
        private int _msgContainerMaxSize;
        public int MsgLineLength { get => _msgLineLength; set => SetField(ref _msgLineLength, value, nameof(MsgLineLength)); }
        private int _msgLineLength;
        public string MsgQueueTestText { get => _msgQueueTestText; set => SetField(ref _msgQueueTestText, value, nameof(MsgQueueTestText)); }
        private string _msgQueueTestText;

        public string ScribanTemplate { get => scribanTemplate; set => SetField(ref scribanTemplate, value); }


        public bool IsVisibleForKeepQueueMsgCountCtrl
        {
            get { return EnableKeepLastQueueMsg && EnableQueueMsg; }
        }

        private ObservableCollection<OutputInfoTemplate> infoTemplates = null;
        private ObservableCollection<OutputInfoTemplate> InfoTemplates
        {
            get
            {
                return infoTemplates;
            }
            set
            {
                SetField(ref infoTemplates, value);
                infoTemplates.CollectionChanged += InfoTemplates_CollectionChanged;
            }
        }

        private OutputInfo currentOutputInfo;
        public OutputInfo CurrentOutputInfo
        {
            get => currentOutputInfo; set
            {
                SetField(ref currentOutputInfo, value);
                if (CurrentOutputInfo != null)
                    CurrentOutputInfo.PropertyChanged += (object sender, PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentOutputInfo)));
            }
        }

        public string Result { get => result; set => SetField(ref result, value); }

        private string scribanTemplate;
        private string result;

        internal Writer(ObservableCollection<SongItem> songs, ObservableCollection<SongInfo> playlist, Player player, DanmuHandler danmuHandler, ObservableCollection<OutputInfoTemplate> infoTemplates, EventSafeQueue<object> msgQueue)
        {
            Songs = songs;
            Playlist = playlist;
            Player = player;
            InfoTemplates = infoTemplates;
            DanmuHandler = danmuHandler;
            MsgQueue = msgQueue;

            PropertyChanged += Writer_PropertyChanged;

            Player.LyricEvent += Player_LyricEvent;

            timer = new Timer(1000)
            {
                AutoReset = true
            };
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            queuemsg_timer = new Timer(1000)
            {
                AutoReset = true
            };
            queuemsg_timer.Elapsed += Queuemsg_timer_Elapsed; ;
            queuemsg_timer.Start();

            timetimer = new Timer(1000)
            {
                AutoReset = true
            };
            timetimer.Elapsed += Timetimer_Elapsed; ;
            timetimer.Start();
        }

        private void Timetimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //只更歌曲播放时间
            string repx = "\\{\\{\\s*当前播放时间\\s*\\}\\}";
            var kv = templates.Where(
                p => (
                p.Value.Template.Page.Body.CanOutput
                && (Regex.IsMatch(p.Value.OriginPattern, repx))
                )
                );
            var kv2 = kv.ToDictionary(p => p.Key, p => p.Value);
            OutputInfoToFile(kv2);
        }

        private void Queuemsg_timer_Elapsed(object sender, ElapsedEventArgs e)
        {

            lock (locker)
            {
                if (isLock2)
                    return;
                isLock2 = true;
                try
                {
                    if (EnableQueueMsg)
                    {
                        if (MsgQueue.TryDequeue(out object msg))
                        {
                            if (keepingQueue.Count >= MsgContainerMaxSize)
                            {
                                keepingQueue.RemoveRange(0, keepingQueue.Count - (MsgContainerMaxSize - 1));
                            }
                            
                            keepingQueue.Add(new Notification() { CreateTime = DateTime.Now, Message = msg.ToString() });
                        }

                        if (keepingQueue.Count > 0)
                        {
                            var discardeds = keepingQueue.Where(p => (DateTime.Now - p.CreateTime).TotalSeconds > Convert.ToDouble(QueueMsgMaxStayTime)).ToList();//找出过期的消息
                            discardeds = discardeds.OrderBy(p => p.CreateTime).ToList();
                            foreach (var item in discardeds)
                            {
                                if (EnableKeepLastQueueMsg == false || (EnableKeepLastQueueMsg && keepingQueue.Count > KeepQueueMsgCount) || keepingQueue.Count > MsgContainerMaxSize)
                                {
                                    keepingQueue.Remove(item);
                                }
                            }
                        }
                    }
                    else
                    {
                        keepingQueue.Clear();
                    }
                }
                catch (Exception ex)
                {
                    Log("执行消息队列输出时出错", ex);
                }
                isLock2=false;
            }

        }

        private void Player_LyricEvent(object sender, LyricChangedEventArgs e)
        {
            try
            {
                CurrentLyric = e.CurrentLyric;
                UpcomingLyric = e.UpcomingLyric;
                try
                {
                    //只更新歌词部分的模板
                    string repx = "\\{\\{\\s*当前歌词\\s*\\}\\}";
                    string repx2 = "\\{\\{\\s*下句歌词\\s*\\}\\}";
                    var kv = templates.Where(
                        p =>(
                        p.Value.Template.Page.Body.CanOutput
                        && (Regex.IsMatch(p.Value.OriginPattern, repx) || Regex.IsMatch(p.Value.OriginPattern, repx2))
                        )
                        );
                    var kv2 = kv.ToDictionary(p => p.Key, p => p.Value);
                    OutputInfoToFile(kv2);
                }
                catch (Exception ex) { }
            }
            catch (Exception) { }
        }

        private void Writer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CurrentOutputInfo))
            {
                var localtemplate = Template.Parse(CurrentOutputInfo?.Content);
                if (localtemplate.HasErrors)
                {
                    Result = "模板有语法错误" + Environment.NewLine + string.Join(Environment.NewLine, localtemplate.Messages);
                    try
                    {
                        File.WriteAllText(Utilities.ScribanOutputFilePath, Result);
                    }
                    catch (Exception) { }
                }
                else
                {
                    template = localtemplate;
                }
            }
            if (e.PropertyName == nameof(InfoTemplates))
            {
                if (InfoTemplates?.Count > 0)
                {
                    templates = new Dictionary<string, TemplateRender>();
                    foreach (var item in InfoTemplates)
                    { 
                        if(string.IsNullOrEmpty(item.Value?.Content)||item.Value?.IsEnable==false)
                        {
                            continue;
                        }
                        var localtemplate = Template.Parse(item.Value.Content);
                        templates.Add(item.Key, new TemplateRender { Template= localtemplate, OriginPattern = item.Value?.Content });
                    }
                }
            }
            if(e.PropertyName==nameof(EnableKeepLastQueueMsg))
            {
                PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(nameof(IsVisibleForKeepQueueMsgCountCtrl)));
            }
            if (e.PropertyName == nameof(EnableQueueMsg))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisibleForKeepQueueMsgCountCtrl)));
            }
            if(e.PropertyName==nameof(MsgContainerMaxSize)|| e.PropertyName == nameof(KeepQueueMsgCount))
            {
                //if(KeepQueueMsgCount>MsgContainerMaxSize)
                //    KeepQueueMsgCount = MsgContainerMaxSize;
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //只更歌曲播放时间
            string repx = "\\{\\{\\s*当前播放时间\\s*\\}\\}";
            var kv = templates.Where(
                p => (
                p.Value.Template.Page.Body.CanOutput
                && (!Regex.IsMatch(p.Value.OriginPattern, repx))
                )
                );
            var kv2 = kv.ToDictionary(p => p.Key, p => p.Value);
            OutputInfoToFile(kv2);
        }

        private void OutputInfoToFile(Dictionary<string,TemplateRender> ts)
        {
            var localsongs = Songs.Select(x => new
            {
                歌名 = x.SongName,
                歌手 = x.SingersText,
                歌曲id = x.SongId,
                点歌人 = x.UserName,
                状态 = x.Status.ToStatusString(),
                搜索模块 = x.ModuleName,
            });
            var localplaylist = Playlist.Select(x => new
            {
                播放状态=x.IsPlaying?"▶":"",
                歌名 = x.Name,
                歌手 = x.SingersText,
                歌曲id = x.Id,
                搜索模块 = x.Module.ModuleName,
            });


            var 消息队列 = keepingQueue.Select(x => new
            {
                信息 = Regex.Replace(x.Message, @"(.{1," + (MsgLineLength <= 0 ? x.Message.Length : MsgLineLength) + @"})", "$1" + Environment.NewLine).TrimEnd(),
                时间 = x.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            });

            var playingSong = Songs?.Where(o => o.Status == SongStatus.Playing).FirstOrDefault();
            var waitPlaySong = Songs?.Where(o => o.Status == SongStatus.WaitingPlay).FirstOrDefault();

            var 播放列表 = localsongs;
            var 空闲歌单 = localplaylist;
            int 歌曲数量 = Songs.Count;
            string 当前播放时间 = Player.CurrentTimeString;
            string 当前总时间 = Player.TotalTimeString;
            string 播放模式 = Player.CurrentPlayMode.ToString();
            string 播放模式名称 = Player.CurrentPlayMode.ToZhName();
            string 当前音量 = Convert.ToInt32(Player.Volume * 100).ToString();
            var 总共最大点歌数量 = DanmuHandler.MaxTotalSongNum;
            var 单人最大点歌数量 = DanmuHandler.MaxPersonSongNum;

            string 当前播放 = playingSong == null ? Utilities.SpareNoSongNotice : playingSong.SongName;
            string 当前歌手 = playingSong == null ? "" : playingSong.Singers == null ? "" : string.Join("/", playingSong.Singers);
            string 当前点歌用户 = playingSong == null ? "" : playingSong.UserName;
            string 当前模块 = playingSong == null ? "" : playingSong.Module?.ModuleName;

            string 下一首播放 = waitPlaySong == null ? Utilities.SpareNoSongNotice : waitPlaySong.SongName;
            string 下一首歌手 = waitPlaySong == null ? "" : waitPlaySong.Singers == null ? "" : string.Join("/", waitPlaySong.Singers);
            string 下一首点歌用户 = waitPlaySong == null ? "" : waitPlaySong.UserName;
            string 下一首模块 = waitPlaySong == null ? "" : waitPlaySong.Module?.ModuleName;


            var realContent = new
            {
                播放列表,
                空闲歌单,
                歌曲数量,
                当前播放时间,
                当前总时间,
                播放模式,
                播放模式名称,
                当前音量,
                总共最大点歌数量,
                单人最大点歌数量,

                当前播放,
                当前歌手,
                当前点歌用户,
                当前模块,

                下一首播放,
                下一首歌手,
                下一首点歌用户,
                下一首模块,
                当前歌词 = CurrentLyric,
                下句歌词 = UpcomingLyric,
                消息队列

            };

            var localresult = template?.Render(realContent) ?? string.Empty;

            if (ts?.Count > 0)
            {
                var n=DateTime.Now;
                foreach (var item in ts)
                {
                    var info = "";
                    if (item.Value.Template.HasErrors)
                    {
                        info = "模板有语法错误" + Environment.NewLine + string.Join(Environment.NewLine, item.Value.Template.Messages);
                    }
                    else
                    {
                        info = item.Value?.Template.Render(realContent) ?? string.Empty;
                    }
                    try
                    {
                        if (info == item.Value.Text || DateTime.Now.Second % 10 == 0)
                        {
                            //如果文本没有变动则不写盘，节约IO操作
                            continue;
                        }
                        item.Value.Text = info;
                        File.WriteAllText(Path.Combine(Utilities.DataDirectoryPath, item.Key), item.Value.Text);
                    }
                    catch (Exception ex)
                    {
                        Log("输出即时播放器信息到文件时出错", ex);
                    }
                }
            }

            Result = localresult;
        }

        private void InfoTemplates_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e?.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add || e?.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
            {
                if (e.NewItems?.Count > 0)
                {
                    foreach (OutputInfoTemplate item in e.NewItems)
                    {
                        item.Value.PropertyChanged += (object obj_sender, PropertyChangedEventArgs ee) =>
                        {
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InfoTemplates)));
                        };
                        item.PropertyChanged += (object obj_sender, PropertyChangedEventArgs ee) =>
                        {
                            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InfoTemplates)));
                        };
                        item.PropertyChanging += TemplateNameEdite_PropertyChanging;
                    }
                }
            }
            else if (e?.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (OutputInfoTemplate item in e.OldItems)
                {
                    string tfp = Path.Combine(Utilities.DataDirectoryPath, item.Key);
                    try
                    {
                        if (File.Exists(tfp))
                            File.Delete(tfp);
                    }
                    catch (Exception ex)
                    {
                        Log("删除输出模板文件出错:" + tfp, ex);
                    }
                }
            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(InfoTemplates)));
        }

        private void TemplateNameEdite_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            var o = sender as OutputInfoTemplate;
            if (e.PropertyName == nameof(o.Key))
            {
                string tfp = Path.Combine(Utilities.DataDirectoryPath, o.Key);
                try
                {
                    if (File.Exists(tfp))
                        File.Delete(tfp);
                }
                catch (Exception ex)
                {
                    Log("删除输出模板文件出错:" + tfp, ex);

                }
            }
        }

        public event LogEvent LogEvent;
        private void Log(string message, Exception exception = null) => LogEvent?.Invoke(this, new LogEventArgs() { Message = message, Exception = exception });

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
