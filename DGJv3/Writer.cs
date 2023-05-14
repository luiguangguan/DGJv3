using Scriban;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;

namespace DGJv3
{
    class Writer : INotifyPropertyChanged
    {
        private static string lockerString=Guid.NewGuid().ToString();

        private static bool isLock = false;

        private ObservableCollection<SongItem> Songs;

        private ObservableCollection<SongInfo> Playlist;

        private Player Player;

        private DanmuHandler DanmuHandler;

        private Timer timer;

        private Template template = null;

        private Dictionary<string,Template> templates = new  Dictionary<string, Template>();

        private string CurrentLyric = "";
        private string UpcomingLyric = "";


        public string ScribanTemplate { get => scribanTemplate; set => SetField(ref scribanTemplate, value); }

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

        internal Writer(ObservableCollection<SongItem> songs, ObservableCollection<SongInfo> playlist, Player player, DanmuHandler danmuHandler, ObservableCollection<OutputInfoTemplate> infoTemplates)
        {
            Songs = songs;
            Playlist = playlist;
            Player = player;
            InfoTemplates = infoTemplates;
            DanmuHandler = danmuHandler;

            PropertyChanged += Writer_PropertyChanged;

            Player.LyricEvent += Player_LyricEvent;

            timer = new Timer(1000)
            {
                AutoReset = true
            };
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Player_LyricEvent(object sender, LyricChangedEventArgs e)
        {
            try
            {
                CurrentLyric = e.CurrentLyric;
                UpcomingLyric = e.UpcomingLyric;
                File.WriteAllText(Utilities.LyricOutputFilePath, e.CurrentLyric + Environment.NewLine + e.UpcomingLyric);
                if (isLock)
                {
                    return;
                }
                lock (lockerString)
                {
                    isLock = true;
                    OutputInfoToFile();
                    isLock = false;
                }
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
                    templates = new Dictionary<string, Template>();
                    foreach (var item in InfoTemplates)
                    { 
                        if(string.IsNullOrEmpty(item.Value?.Content)||item.Value?.IsEnable==false)
                        {
                            continue;
                        }
                        var localtemplate = Template.Parse(item.Value.Content);
                        templates.Add(item.Key, localtemplate);
                    }
                }
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if(isLock)
            {
                return;
            }
            lock (lockerString)
            {
                isLock = true;
                OutputInfoToFile();
                isLock = false;
            }
        }

        private void OutputInfoToFile()
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
                歌名 = x.Name,
                歌手 = x.SingersText,
                歌曲id = x.Id,
                搜索模块 = x.Module.ModuleName,
            });

            var playingSong = Songs?.Where(o => o.Status == SongStatus.Playing).FirstOrDefault();
            var waitPlaySong = Songs?.Where(o => o.Status == SongStatus.WaitingPlay).FirstOrDefault();

            var 播放列表 = localsongs;
            var 空闲歌单 = localplaylist;
            int 歌曲数量 = Songs.Count;
            string 当前播放时间 = Player.CurrentTimeString;
            string 当前总时间 = Player.TotalTimeString;
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
                下句歌词 = UpcomingLyric

            };

            var localresult = template?.Render(realContent) ?? string.Empty;

            if (templates?.Count > 0)
            {
                foreach (var item in templates)
                {
                    var info = "";
                    if (item.Value.HasErrors)
                    {
                        info = "模板有语法错误" + Environment.NewLine + string.Join(Environment.NewLine, item.Value.Messages);
                    }
                    else
                    {
                        info = item.Value?.Render(realContent) ?? string.Empty;
                    }
                    try
                    {
                        File.WriteAllText(Path.Combine(Utilities.DataDirectoryPath, item.Key), info);
                    }
                    catch (Exception ex)
                    {
                        Log("输出即时播放器信息到文件时出错", ex);
                    }
                }
            }

            Result = localresult;
            //if (localresult != Result)
            //{
            //    Result = localresult;

            //    try
            //    {
            //        if (InfoTemplates?.Count > 0)
            //        {
            //            foreach (var item in InfoTemplates)
            //            {

            //            }
            //        }
            //        //File.WriteAllText(Utilities.ScribanOutputFilePath, Result);

            //        //File.WriteAllText(Utilities.CurrentArtist, 当前歌手);
            //        //File.WriteAllText(Utilities.CurrentBiliUser, 当前点歌用户);
            //        //File.WriteAllText(Utilities.CurrentSong, 当前播放);
            //        //File.WriteAllText(Utilities.CurrentTime, 当前播放时间);
            //        //File.WriteAllText(Utilities.CurrentTotalTime, 当前总时间);
            //    }
            //    catch (Exception) { }
            //}
            //else
            //{
            //    if (template?.HasErrors == true)
            //    {
            //        try
            //        {
            //            Result = "模板有错误" + Environment.NewLine + string.Join(Environment.NewLine, template.Messages);
            //            File.WriteAllText(Utilities.ScribanOutputFilePath, Result);

            //            File.WriteAllText(Utilities.CurrentArtist, "");
            //            File.WriteAllText(Utilities.CurrentBiliUser, "");
            //            File.WriteAllText(Utilities.CurrentSong, "");
            //            File.WriteAllText(Utilities.CurrentTime, "");
            //            File.WriteAllText(Utilities.CurrentTotalTime, "");
            //        }
            //        catch (Exception) { }
            //    }
            //}
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
