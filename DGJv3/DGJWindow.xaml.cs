﻿using DGJv3.InternalModule;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Caching;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace DGJv3
{
    /// <summary>
    /// DGJWindow.xaml 的交互逻辑
    /// </summary>
    internal partial class DGJWindow : Window
    {
        public DGJMain PluginMain { get; set; }

        public ObservableCollection<SongItem> Songs { get; set; }

        public ObservableCollection<SongInfo> Playlist { get; set; }

        public ObservableCollection<BlackListItem> Blacklist { get; set; }

        public Player Player { get; set; }

        public Downloader Downloader { get; set; }

        public Writer Writer { get; set; }

        public SearchModules SearchModules { get; set; }

        public DanmuHandler DanmuHandler { get; set; }

        public UIFunction UIFunction { get;set; }

        public UniversalCommand RemoveSongCommmand { get; set; }

        public UniversalCommand RemoveAndBlacklistSongCommand { get; set; }

        public UniversalCommand RemovePlaylistInfoCommmand { get; set; }

        public UniversalCommand PlaySongInPlaylistCommmand { get; set; }

        public UniversalCommand ClearPlaylistCommand { get; set; }

        public UniversalCommand RemoveBlacklistInfoCommmand { get; set; }

        public UniversalCommand ClearBlacklistCommand { get; set; }
        //public UniversalCommand RefreshConfigCommand { get; set; }

        public UniversalCommand NavigatePlayingSongInPlaylistCommand { get; set; }

        public bool IsLogRedirectDanmaku { get; set; }

        public int LogDanmakuLengthLimit { get; set; }
        public bool FormatConfig { get; set; }

        private ObservableCollection<SongItem> SkipSong;

        //在空闲歌单中搜索
        public UniversalCommand SearchInPlayListCommand { get; private set; }

        private string SearchInPlayListKeyWord = "";

        public void Log(string text)
        {
            PluginMain.Log(text);

            if (IsLogRedirectDanmaku)
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (!PluginMain.RoomId.HasValue) { return; }

                        string finalText = text.Substring(0, Math.Min(text.Length, LogDanmakuLengthLimit));
                        string result = LoginCenterAPIWarpper.Send(PluginMain.RoomId.Value, finalText);
                        if (result == null)
                        {
                            PluginMain.Log("发送弹幕时网络错误");
                        }
                        else
                        {
                            var j = JObject.Parse(result);
                            if (j["msg"].ToString() != string.Empty)
                            {
                                PluginMain.Log("发送弹幕时服务器返回：" + j["msg"].ToString());
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.GetType().FullName.Equals("LoginCenter.API.PluginNotAuthorizedException"))
                        {
                            IsLogRedirectDanmaku = false;
                        }
                        else
                        {
                            PluginMain.Log("弹幕发送错误 " + ex.ToString());
                        }
                    }
                });
            }
        }


        private void SetPlayingInList(int index)
        {
            for (int i = 0; i < Playlist.Count; i++)
            {
                if (i == index)
                {
                    Playlist[i].IsPlaying = true;
                }
                else
                {
                    if (Playlist[i].IsPlaying != false)
                        Playlist[i].IsPlaying = false;
                }
            }
        }

        public DGJWindow(DGJMain dGJMain)
        {
            void addResource(string uri)
            {
                Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary()
                {
                    Source = new Uri(uri)
                });
            }
            addResource("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml");
            addResource("pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Blue.xaml");
            addResource("pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.DeepOrange.xaml");
            addResource("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.ProgressBar.xaml");

            DataContext = this;
            PluginMain = dGJMain;
            Songs = new ObservableCollection<SongItem>();
            Playlist = new ObservableCollection<SongInfo>();
            Blacklist = new ObservableCollection<BlackListItem>();
            SkipSong = new ObservableCollection<SongItem>();

            Player = new Player(Songs, Playlist, SkipSong);
            Downloader = new Downloader(Songs, SkipSong);
            SearchModules = new SearchModules();
            UIFunction = new UIFunction(Songs, Playlist, Blacklist, SkipSong, SearchModules);
            DanmuHandler = new DanmuHandler(Songs, Player, Downloader, SearchModules, Blacklist, UIFunction);
            Writer = new Writer(Songs, Playlist, Player, DanmuHandler);

            UIFunction.LogEvent += (sender, e) => { Log("" + e.Message + (e.Exception == null ? string.Empty : e.Exception.Message)); };
            Player.LogEvent += (sender, e) => { Log("播放:" + e.Message + (e.Exception == null ? string.Empty : e.Exception.Message)); };
            Player.SongsListChanged += (sender, e) => {
                var songs = sender as ObservableCollection<SongItem>;
                if (songs == null)
                {
                    Log("songs是null");
                }
                int index = -1;
                if (Playlist?.Count > 0 && songs?.Count > 0 && songs?[0].UserName == Utilities.SparePlaylistUser)
                {
                    for (int i = 0; i < Playlist.Count; i++)
                    {
                        if (songs[0].SongId == Playlist[i].Id)
                        {
                            index = i;
                        }
                        //else
                        //{
                        //    if (Playlist[i].IsPlaying != false)
                        //        Playlist[i].IsPlaying = false;
                        //}
                    }
                }
                NavigateSongInPlaylist(index);
                SetPlayingInList(index);//没有正在播放的曲目


            };
            Downloader.LogEvent += (sender, e) => { Log("下载:" + e.Message + (e.Exception == null ? string.Empty : e.Exception.Message)); };
            Writer.LogEvent += (sender, e) => { Log("文本:" + e.Message + (e.Exception == null ? string.Empty : e.Exception.Message)); };
            SearchModules.LogEvent += (sender, e) => { Log("搜索:" + e.Message + (e.Exception == null ? string.Empty : e.Exception.Message)); };
            DanmuHandler.LogEvent += (sender, e) => { Log("" + e.Message + (e.Exception == null ? string.Empty : e.Exception.Message)); };
            IsVisibleChanged += DGJWindow_IsVisibleChanged;
            
            RemoveSongCommmand = new UniversalCommand((songobj) =>
            {
                if (songobj != null && songobj is SongItem songItem)
                {
                    songItem.Remove(Songs, Downloader, Player);
                }
            });

            RemoveAndBlacklistSongCommand = new UniversalCommand((songobj) =>
            {
                if (songobj != null && songobj is SongItem songItem)
                {
                    songItem.Remove(Songs, Downloader, Player);
                    Blacklist.Add(new BlackListItem(BlackListType.Id, songItem.SongId));
                }
            });


            PlaySongInPlaylistCommmand= new UniversalCommand((songobj) =>
            {
                if (songobj != null && songobj is SongInfo songInfo)
                {
                    Songs.Clear();
                    Player.Next();
                    //SetPlayingInList(-1);
                    songInfo.IsPlaying = true;

                }
            });

            RemovePlaylistInfoCommmand = new UniversalCommand((songobj) =>
            {
                if (songobj != null && songobj is SongInfo songInfo)
                {
                    Playlist.Remove(songInfo);
                }
            });

            ClearPlaylistCommand = new UniversalCommand((e) =>
            {
                Playlist.Clear();
            });

            RemoveBlacklistInfoCommmand = new UniversalCommand((blackobj) =>
            {
                if (blackobj != null && blackobj is BlackListItem blackListItem)
                {
                    Blacklist.Remove(blackListItem);
                }
            });

            ClearBlacklistCommand = new UniversalCommand((x) =>
            {
                Blacklist.Clear();
            });

            
            SearchInPlayListCommand = new UniversalCommand((x) =>
            {
                
                if(string.IsNullOrEmpty(SearchBox.Text)==false)
                {
                    SearchSongInPlaylist(SearchBox.Text);
                }
            });

            //定位到正在播放的歌曲
            NavigatePlayingSongInPlaylistCommand = new UniversalCommand((x) =>
            {
                NavigatePlayingSongInPlaylist();
            });
            //RefreshConfigCommand = new UniversalCommand((x) =>
            //{
            //    ApplyConfig(Config.Load());
            //});

            InitializeComponent();

            ApplyConfig(Config.Load());

            PluginMain.ReceivedDanmaku += (sender, e) => { DanmuHandler.ProcessDanmu(e.Danmaku); };
            PluginMain.Connected += (sender, e) => { LwlApiBaseModule.RoomId = e.roomid; };
            PluginMain.Disconnected += (sender, e) => { LwlApiBaseModule.RoomId = 0; };

            //Task.Run(() => {
            //    //延迟100毫秒重新应用配置，解决外部音乐模块自动选择问题（暂时不知道在哪里加载了外部音乐模块）
            //    Thread.Sleep(100);
            //    this.Dispatcher.Invoke(() => {
            //        //var a = SearchModules.Modules;
            //        ApplyConfig(Config.Load());
            //    });
            //});

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if(assembly.GetName().Name=="NAudio")
                {
                    Log("当前" + assembly.GetName().Name + "版本是" + assembly.GetName().Version.ToString() + ",文件位置：" + assembly.Location);
                }
            }
        }

        private void SearchSongInPlaylist(string keyword)
        {
            if (keyword != SearchInPlayListKeyWord)
            {
                SearchInPlayListKeyWord = keyword;
            }
            int cycle = 0;
            int index = 0;
            index = Songlist_listView.SelectedIndex > -1 ? Songlist_listView.SelectedIndex : 0;
            while (cycle < 2)
            {

                for (int i = index; i < Playlist.Count; i++)
                {
                    if ((Playlist[i].Name.IndexOf(SearchInPlayListKeyWord) > -1 || Playlist[i].SingersText.IndexOf(SearchInPlayListKeyWord) > -1) && Songlist_listView.SelectedIndex != i)
                    {
                        NavigateSongInPlaylist(i);
                        return;
                    }
                }
                index = 0;
                cycle++;
            }
        }

        private void DGJWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //throw new NotImplementedException();
            if (e.NewValue != e.OldValue && (bool)e.NewValue == true)
            {
                NavigatePlayingSongInPlaylist();
            }
        }

        private void NavigatePlayingSongInPlaylist()
        {
            int index = Playlist.IndexOf(Playlist.FirstOrDefault(p => p.IsPlaying == true));
            NavigateSongInPlaylist(index);
        }

        private void NavigateSongInPlaylist(int index)
        {
            if (index < 0 || index > Songlist_listView?.Items.Count - 1)
                return;
            Songlist_listView.ScrollIntoView(Songlist_listView.Items[index]);
            Songlist_listView.SelectedItem = Songlist_listView.Items[index];
            Songlist_listView.UpdateLayout();
        }

        public void SetMusicModule()
        {
            ApplyConfig(Config.Load());
        }


        /// <summary>
        /// 应用设置
        /// </summary>
        /// <param name="config"></param>
        private void ApplyConfig(Config config)
        {
            Player.PlayerType = config.PlayerType;
            Player.DirectSoundDevice = config.DirectSoundDevice;
            Player.WaveoutEventDevice = config.WaveoutEventDevice;
            Player.Volume = config.Volume;
            Player.IsUserPrior = config.IsUserPrior;
            Player.IsPlaylistEnabled = config.IsPlaylistEnabled;
            SearchModules.PrimaryModule = SearchModules.Modules.FirstOrDefault(x => x.UniqueId == config.PrimaryModuleId) ?? SearchModules.NullModule;
            SearchModules.SecondaryModule = SearchModules.Modules.FirstOrDefault(x => x.UniqueId == config.SecondaryModuleId) ?? SearchModules.NullModule;
            DanmuHandler.MaxTotalSongNum = config.MaxTotalSongNum;
            DanmuHandler.MaxPersonSongNum = config.MaxPersonSongNum;
            Writer.ScribanTemplate = config.ScribanTemplate;
            IsLogRedirectDanmaku = config.IsLogRedirectDanmaku;
            LogDanmakuLengthLimit = config.LogDanmakuLengthLimit;
            FormatConfig = config.FormatConfig;

            Player.CurrentPlayMode= config.CurrentPlayMode;
            Player.LastSongId = config.LastSongId;
            Writer.InfoTemplates = new ObservableCollection<KeyValuePair<string, OutputInfo>>();

            if (config.InfoTemplates == null)
                config.InfoTemplates = new Dictionary<string, OutputInfo>();
            foreach (var key in config.InfoTemplates.Keys)
            {
                Writer.InfoTemplates.Add(new KeyValuePair<string, OutputInfo>(key, config.InfoTemplates[key]));
            }
           
            
            LogRedirectToggleButton.IsEnabled = LoginCenterAPIWarpper.CheckLoginCenter();
            if (LogRedirectToggleButton.IsEnabled && IsLogRedirectDanmaku)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(2000); // 其实不应该这么写的，不太合理
                    IsLogRedirectDanmaku = await LoginCenterAPIWarpper.DoAuth(PluginMain);
                });
            }
            else
            {
                IsLogRedirectDanmaku = false;
            }

            Playlist.Clear();
            foreach (var item in config.Playlist)
            {
                item.Module = SearchModules.Modules.FirstOrDefault(x => x.UniqueId == item.ModuleId);
                if (item.Module != null)
                {
                    Playlist.Add(item);
                }
            }

            Blacklist.Clear();
            foreach (var item in config.Blacklist)
            {
                Blacklist.Add(item);
            }
        }

        /// <summary>
        /// 收集设置
        /// </summary>
        /// <returns></returns>
        private Config GatherConfig() => new Config()
        {
            PlayerType = Player.PlayerType,
            DirectSoundDevice = Player.DirectSoundDevice,
            WaveoutEventDevice = Player.WaveoutEventDevice,
            IsUserPrior = Player.IsUserPrior,
            Volume = Player.Volume,
            IsPlaylistEnabled = Player.IsPlaylistEnabled,
            PrimaryModuleId = SearchModules.PrimaryModule.UniqueId,
            SecondaryModuleId = SearchModules.SecondaryModule.UniqueId,
            MaxPersonSongNum = DanmuHandler.MaxPersonSongNum,
            MaxTotalSongNum = DanmuHandler.MaxTotalSongNum,
            ScribanTemplate = Writer.ScribanTemplate,
            Playlist = Playlist.ToArray(),
            Blacklist = Blacklist.ToArray(),
            IsLogRedirectDanmaku = IsLogRedirectDanmaku,
            LogDanmakuLengthLimit = LogDanmakuLengthLimit,
            FormatConfig = FormatConfig,
            CurrentPlayMode=Player.CurrentPlayMode,
            LastSongId = Player.LastSongId,
            InfoTemplates = Writer.InfoTemplates.ToDictionary(p => p.Key, p => p.Value),
        };

        /// <summary>
        /// 弹幕姬退出事件
        /// </summary>
        internal void DeInit()
        {
            Config.Write(GatherConfig());

            Downloader.CancelDownload();
            Player.Next();
            try
            {
                Directory.Delete(Utilities.SongsCacheDirectoryPath, true);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// 主界面右侧
        /// 添加歌曲的
        /// dialog 的
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void DialogAddSongs(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter.Equals(true) && !string.IsNullOrWhiteSpace(AddSongsTextBox.Text))
            {
                var keyword = AddSongsTextBox.Text;
                SongInfo songInfo = null;

                if (SearchModules.PrimaryModule != SearchModules.NullModule)
                {
                    songInfo = SearchModules.PrimaryModule.SafeSearch(keyword);
                }

                if (songInfo == null)
                {
                    if (SearchModules.SecondaryModule != SearchModules.NullModule)
                    {
                        songInfo = SearchModules.SecondaryModule.SafeSearch(keyword);
                    }
                }

                if (songInfo == null)
                {
                    return;
                }

                Songs.Add(new SongItem(songInfo, "主播")); // TODO: 点歌人名字
            }
            AddSongsTextBox.Text = string.Empty;
        }
        private void AddOutputInfo_Click(object sender, RoutedEventArgs eventArgs)
        {
            if (!string.IsNullOrWhiteSpace(AddOutputInfoTextBox.Text))
            {
                if (Writer.InfoTemplates == null)
                {
                    Writer.InfoTemplates = new ObservableCollection<KeyValuePair<string, OutputInfo>>();
                }
                if (Writer.InfoTemplates.Any(p => p.Key == AddOutputInfoTextBox.Text) == false)
                {
                    Writer.InfoTemplates.Add(new KeyValuePair<string, OutputInfo>(AddOutputInfoTextBox.Text, new OutputInfo() { IsEnable = false, Content = string.Empty }));
                    AddOutputInfoTextBox.Text = string.Empty;
                }
            }
        }

        private void DialogRemoveOutputInfo(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter.Equals(false) == false)
            {
                var key = eventArgs.Parameter as string;
                if (Writer.InfoTemplates.Any(p => p.Key == key))
                {
                    Writer.CurrentOutputInfo = null;
                    Writer.InfoTemplates.Remove(Writer.InfoTemplates.FirstOrDefault(p=>p.Key==key));
                    //Writer.InfoTemplates[key].Remove(key, Writer.InfoTemplates);
                }
            }
        }
        /// <summary>
        /// 主界面右侧
        /// 添加空闲歌曲按钮的
        /// dialog 的
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void DialogAddSongsToPlaylist(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter.Equals(true) && !string.IsNullOrWhiteSpace(AddSongPlaylistTextBox.Text))
            {
                var keyword = AddSongPlaylistTextBox.Text;
                if (UIFunction.AddSongsToPlaylist(keyword) == false)
                {
                    return;
                }
            }
            AddSongPlaylistTextBox.Text = string.Empty;
        }

        /// <summary>
        /// 主界面右侧
        /// 添加空闲歌单按钮的
        /// dialog 的
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void DialogAddPlaylist(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter.Equals(true) && !string.IsNullOrWhiteSpace(AddPlaylistTextBox.Text))
            {
                var keyword = AddPlaylistTextBox.Text;
                if (UIFunction.AddPlaylist(keyword) == false)
                {
                    return;
                }
            }
            AddPlaylistTextBox.Text = string.Empty;
        }

        /// <summary>
        /// 黑名单 popupbox 里的
        /// 添加黑名单按钮的
        /// dialog 的
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        private void DialogAddBlacklist(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter.Equals(true)
                && !string.IsNullOrWhiteSpace(AddBlacklistTextBox.Text)
                && AddBlacklistComboBox.SelectedValue != null
                && AddBlacklistComboBox.SelectedValue is BlackListType)
            {
                var keyword = AddBlacklistTextBox.Text;
                var type = (BlackListType)AddBlacklistComboBox.SelectedValue;

                Blacklist.Add(new BlackListItem(type, keyword));
            }
            AddBlacklistTextBox.Text = string.Empty;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private async void LogRedirectToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!await LoginCenterAPIWarpper.DoAuth(PluginMain))
                {
                    LogRedirectToggleButton.IsChecked = false;
                }
            }
            catch (Exception)
            {
                LogRedirectToggleButton.IsChecked = false;
            }
        }

        private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key==System.Windows.Input.Key.Enter)
            {
                var tb = sender as TextBox;
                if (tb != null && string.IsNullOrEmpty(tb.Text) == false)
                    SearchSongInPlaylist(tb.Text);
            }
        }

        private void list_OutputInfo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //CurrentOutputInfo= (sender as ListView)?.SelectedValue
                var lv = sender as ListView;

            if (lv != null)
            {
                try
                {
                    if (lv?.SelectedValue != null)
                        Writer.CurrentOutputInfo = ((KeyValuePair<string, OutputInfo>)(sender as ListView).SelectedValue).Value;
                    else
                        Writer.CurrentOutputInfo = null;
                }
                catch (Exception ex)
                {
                    if (lv.SelectedIndex == -1 && lv.Items.Count == 0)
                    {
                        lv.SelectedIndex = 0;
                    }
                    Log("list_OutputInfo_SelectionChanged事件转换类型失败");
                }
            }
        }

        private void list_OutputInfo_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var lv = sender as ListView;
                if (lv != null)
                {
                    //var visualHit = VisualTreeHelper.HitTest(lv, e.GetPosition(lv));
                    //if (visualHit == null)
                    //{
                    //    // 单击了空白区域，取消选中所有项
                    //    lv.SelectedItems.Clear();
                    //}
                    lv.SelectedIndex = -1;
                }

            }
        }
    }
}
