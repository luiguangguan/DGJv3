using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DGJv3
{
    internal class Player : INotifyPropertyChanged
    {
        private Random random = new Random();

        private ObservableCollection<SongItem> Songs;

        private ObservableCollection<SongInfo> Playlist;

        private Dispatcher dispatcher;

        private ObservableCollection<SongItem> SkipSong;

        private DispatcherTimer newSongTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromSeconds(1),
            IsEnabled = true,
        };

        private DispatcherTimer updateTimeTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(100),
            IsEnabled = true,
        };

        public UniversalCommand PlayPauseCommand { get; private set; }
        public UniversalCommand NextCommand { get; private set; }

        public UniversalCommand ChangePlayModeCommand { get; private set; }

        public string LastSongId { get; set; }

        /// <summary>
        /// 播放器类型
        /// </summary>
        public PlayerType PlayerType { get => _playerType; set => SetField(ref _playerType, value); }
        private PlayerType _playerType;

        /// <summary>
        /// DirectSound 设备
        /// </summary>
        public Guid DirectSoundDevice { get => _directSoundDevice; set => SetField(ref _directSoundDevice, value); }
        private Guid _directSoundDevice;

        /// <summary>
        /// WaveoutEvent 设备
        /// </summary>
        public int WaveoutEventDevice { get => _waveoutEventDevice; set => SetField(ref _waveoutEventDevice, value); }
        private int _waveoutEventDevice;

        /// <summary>
        /// 用户点歌优先
        /// </summary>
        public bool IsUserPrior { get => _isUserPrior; set => SetField(ref _isUserPrior, value); }
        private bool _isUserPrior = false;

        /// <summary>
        /// 当前播放时间
        /// </summary>
        public TimeSpan CurrentTime
        {
            get => mp3FileReader == null ? TimeSpan.Zero : mp3FileReader.CurrentTime;
            set
            {
                if (mp3FileReader != null)
                {
                    mp3FileReader.CurrentTime = value;
                }
            }
        }

        public string CurrentTimeString
        {
            get
            {
                var currentMinutes = Math.Floor(CurrentTime.TotalMinutes);
                //分钟
                var min = currentMinutes > 99 ? currentMinutes.ToString() : currentMinutes.ToString("00");
                return min + ":" + CurrentTime.Seconds.ToString("00");
            }
        }

        /// <summary>
        /// 当前播放时间秒数
        /// </summary>
        public double CurrentTimeDouble
        {
            get => CurrentTime.TotalSeconds;
            set => CurrentTime = TimeSpan.FromSeconds(value);
        }

        /// <summary>
        /// 歌曲全长
        /// </summary>
        public TimeSpan TotalTime { get => mp3FileReader == null ? TimeSpan.Zero : mp3FileReader.TotalTime; }

        public string TotalTimeString
        {
            get
            {
                var currentMinutes = Math.Floor(TotalTime.TotalMinutes);
                var min = currentMinutes > 99 ? currentMinutes.ToString() : currentMinutes.ToString("00");
                return min + ":" + TotalTime.Seconds.ToString("00");
            }
        }

        /// <summary>
        /// 当前是否正在播放歌曲
        /// </summary>
        public bool IsPlaying
        {
            get => Status == PlayerStatus.Playing;
            set
            {
                if (value)
                {
                    Play();
                }
                else
                {
                    Pause();
                }
            }
        }

        private PlayMode playMode = PlayMode.LooptListPlay;
        /// <summary>
        /// 播放模式
        /// </summary>
        public PlayMode CurrentPlayMode
        {
            get { return playMode; }
            set
            {
                //playMode = value;
                SetField(ref playMode, value,nameof(CurrentPlayMode));
                //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentPlayMode)));
            }
        }
        private PlayMode currentPlayMode=PlayMode.LooptListPlay;

        /// <summary>
        /// 当前歌曲播放状态
        /// </summary>
        public PlayerStatus Status
        {
            get
            {
                if (wavePlayer != null)
                {
                    switch (wavePlayer.PlaybackState)
                    {
                        case PlaybackState.Stopped:
                            return PlayerStatus.Stopped;
                        case PlaybackState.Playing:
                            return PlayerStatus.Playing;
                        case PlaybackState.Paused:
                            return PlayerStatus.Paused;
                        default:
                            return PlayerStatus.Stopped;
                    }
                }
                else
                {
                    return PlayerStatus.Stopped;
                }
            }
        }

        /// <summary>
        /// 播放器音量
        /// </summary>
        public float Volume
        {
            get => _volume;
            set
            {
                if (sampleChannel != null)
                {
                    sampleChannel.Volume = value;
                }
                SetField(ref _volume, value, nameof(Volume));
            }
        }
        private float _volume = 1f;

        public float Volume2 { get; set; }
        public bool IsMute { get => _muted; set => SetField(ref _muted, value, nameof(IsMute)); }
        private bool _muted = false;

        /// <summary>
        /// 当前歌词
        /// </summary>
        public string CurrentLyric { get => currentLyric; set => SetField(ref currentLyric, value); }
        private string currentLyric;

        /// <summary>
        /// 下一句歌词       
        /// </summary>
        public string UpcomingLyric { get => upcomingLyric; set => SetField(ref upcomingLyric, value); }

        /// <summary>
        /// 是否使用空闲歌单
        /// </summary>
        public bool IsPlaylistEnabled { get => _isPlaylistEnabled; set => SetField(ref _isPlaylistEnabled, value); }
        private bool _isPlaylistEnabled;

        private string upcomingLyric;

        private IWavePlayer wavePlayer = null;

        private Mp3FileReader mp3FileReader = null;

        private SampleChannel sampleChannel = null;

        private SongItem currentSong = null;

        private int currentLyricIndex = -1;

        public Player(ObservableCollection<SongItem> songs, ObservableCollection<SongInfo> playlist, ObservableCollection<SongItem> skipSongs)
        {
            Songs = songs;
            Playlist = playlist;
            SkipSong = skipSongs;
            dispatcher = Dispatcher.CurrentDispatcher;
            newSongTimer.Tick += NewSongTimer_Tick;
            updateTimeTimer.Tick += UpdateTimeTimer_Tick;
            PropertyChanged += This_PropertyChanged;
            PlayPauseCommand = new UniversalCommand((obj) => { IsPlaying ^= true; });
            NextCommand = new UniversalCommand((obj) => { Next(); });

            ChangePlayModeCommand = new UniversalCommand((obj) =>
            {
                TogglePlayMode(true);
            });
            Songs.CollectionChanged += (sender, e) =>
            {
                SongsListChanged.Invoke(sender, new PropertyChangedEventArgs(nameof(sender)));//调用事件
            };
        }

        /// <summary>
        /// 播放模式切换
        /// <paramref name="tr_event">触发属性改变事件</paramref>
        /// </summary>
        public void TogglePlayMode(bool tr_event = false)
        {
            PlayMode pm = 0;
            if (Convert.ToInt32(CurrentPlayMode) >= 2)
            {
                pm = 0;
            }
            else
            {
                pm = CurrentPlayMode + 1;
            }
            SetPlayMode(pm);
        }

        public void SetPlayMode(PlayMode playMode)
        {
            CurrentPlayMode = playMode;
        }

        private void This_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Status))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPlaying)));
            }
            else if (e.PropertyName == nameof(CurrentTime))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTimeDouble)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTimeString)));
            }
            else if (e.PropertyName == nameof(TotalTime))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalTimeString)));
            }
            else if (e.PropertyName == nameof(Volume))
            {
                //音量变动事件
                Player play = sender as Player;
                float vol = play.Volume;

                //记录非点击静音时的音量
                if (play.IsMute == false)
                    play.Volume2 = vol;

                if (play.Volume > 0)
                {
                    play.IsMute = false;
                }
                else
                {
                    play.IsMute = true;
                }
            }
            else if (e.PropertyName == nameof(IsMute))
            {
                //静音变动事件
                Player play = sender as Player;
                if (play.IsMute)
                    play.Volume = 0;
                else if (play.Volume == 0)
                    play.Volume = play.Volume2;
            }
        }

        /// <summary>
        /// 定时器 100ms 调用一次
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateTimeTimer_Tick(object sender, EventArgs e)
        {
            if (mp3FileReader != null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));

                if (currentSong != null)
                {
                    var index = currentSong.Lyric.GetLyric(CurrentTimeDouble, out string current, out string upcoming);
                    if (index != currentLyricIndex)
                    {
                        currentLyricIndex = index;
                        SetLyric(current, upcoming);
                    }
                }
            }
        }

        /// <summary>
        /// 定时器 1s 调用一次
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewSongTimer_Tick(object sender, EventArgs e)
        {
            if (Songs.Count > 0 && Songs[0].Status == SongStatus.WaitingPlay)
            {
                LoadSong(Songs[0]);
            }
            else if (Songs.Count > 1
                     && currentSong != null
                     && currentSong.UserName == Utilities.SparePlaylistUser
                     && Songs.FirstOrDefault(
                         s => s.UserName != Utilities.SparePlaylistUser)?.Status == SongStatus.WaitingPlay
                     && IsUserPrior)
            {
                Next();
                var pendingRemove = Songs.Where(s => s.UserName == Utilities.SparePlaylistUser).ToList();
                foreach (var songItem in pendingRemove)
                {
                    Songs.Remove(songItem);
                }

                //将已经下载好等待播放的歌曲放回集合中
                pendingRemove = pendingRemove.Where(p => p.Status == SongStatus.WaitingPlay).ToList();
                Log("待加回集合的歌曲：" + pendingRemove.Count, null);
                foreach (var songItem in pendingRemove)
                {
                    Log("加回集合的歌曲：" + songItem.SongName, null);
                    Songs.Add(songItem);
                }
            }

            if (Songs.Count < 2 && IsPlaylistEnabled && Playlist.Count > 0)
            {
                int index = -1;
                int time = 0;
                do
                {
                    //默认第一首
                    index = 0;
                    var currentSongId = currentSong?.SongId;
                    if (string.IsNullOrEmpty(currentSongId) && !string.IsNullOrEmpty(LastSongId))
                    {
                        currentSongId = LastSongId;
                    }
                    else if (string.IsNullOrEmpty(currentSongId) && Songs?.Count > 0)
                    {
                        currentSongId = Songs[0].SongId;
                    }

                    if (Songs?.Count <= 0 && Playlist.Any(p => p.IsPlaying == true))
                    {
                        index = Playlist.IndexOf(Playlist.FirstOrDefault(p => p.IsPlaying == true));
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(currentSongId))
                        {
                            //如果当前有播放的歌曲，则根据ID推算下一首歌曲
                            for (int i = 0; i < Playlist.Count; i++)
                            {


                                if (Playlist[i].Id == currentSongId)
                                {
                                    if (Songs.Count == 0 && !string.IsNullOrEmpty(LastSongId))
                                    {
                                        //当前曲目为空且最后一次
                                        index = i;
                                        break;
                                    }
                                    //第一次播放
                                    if (CurrentPlayMode == PlayMode.LoopOnetPlay)
                                    {
                                        //单曲循环
                                        index = i;
                                    }
                                    else if (CurrentPlayMode == PlayMode.LooptListPlay)
                                    {
                                        //列表循环
                                        if (i < Playlist.Count - 1)
                                        {
                                            index = i + 1;
                                        }
                                        else
                                        {
                                            index = 0;
                                        }
                                    }
                                    else if (CurrentPlayMode == PlayMode.ShufflePlay)
                                    {
                                        //随机播放
                                        int cy = 100;//重复执行次数
                                        do
                                        {
                                            index = random.Next(0, Playlist.Count);
                                            cy--;
                                        } while (index == i && Playlist.Count > 1 && cy > 1);
                                    }
                                    //else if(CurrentPlayMode== PlayMode.ListPlay)
                                    //{
                                    //    //列表顺序播放
                                    //    if (i < Playlist.Count - 1)
                                    //    {
                                    //        index = i + 1;
                                    //    }
                                    //    else
                                    //    {
                                    //        index = -1;

                                    //    }
                                    //}

                                    break;
                                }
                            }
                        }
                    }
                    time++;
                } while (index > -1 && Songs.Any(ele => Playlist[index].Id == ele.SongId) && time < 3);

                //跳过不可用的歌曲
                if (SkipSong?.Count > 0)
                {
                    int cIndex = index;
                    do
                    {
                        var song = new SongItem(Playlist[index], Utilities.SparePlaylistUser);
                        if (SkipSong.Any(p => p.SongId == song.SongId && p.SongName == song.SongName && p.ModuleName == song.ModuleName && p.Note == song.Note && p.UserName == song.UserName) == false)
                            break;
                        if (index < Playlist.Count - 1)
                        {
                            index++;
                        }
                        else
                        {
                            index = 0;
                        }
                        SkipSong.Remove(SkipSong.FirstOrDefault(p => p.SongId == song.SongId && p.SongName == song.SongName && p.ModuleName == song.ModuleName && p.Note == song.Note && p.UserName == song.UserName));
                    } while (cIndex != index);
                }

                if (index > -1)
                {
                    SongInfo info = Playlist[index];
                    if (info.Lyric == null)
                    {
                        info.Lyric = info.Module.SafeGetLyricById(info.Id);
                    }
                    Songs.Add(new SongItem(info, Utilities.SparePlaylistUser));
                }
            }
        }

        /// <summary>
        /// 加载歌曲并开始播放
        /// </summary>
        /// <param name="songItem"></param>
        private void LoadSong(SongItem songItem)
        {
            currentSong = songItem;

            currentSong.Status = SongStatus.Playing;

            wavePlayer = CreateIWavePlayer();
            mp3FileReader = new Mp3FileReader(currentSong.FilePath);
            sampleChannel = new SampleChannel(mp3FileReader)
            {
                Volume = Volume
            };

            wavePlayer.PlaybackStopped += (sender, e) => UnloadSong();

            wavePlayer.Init(sampleChannel);
            wavePlayer.Play();

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalTime)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));

            if (songItem.UserName == Utilities.SparePlaylistUser)
            {

                LastSongId = songItem.SongId;
            }
        }

        /// <summary>
        /// 卸载歌曲并善后
        /// </summary>
        private void UnloadSong()
        {
            try
            {
                wavePlayer?.Dispose();
            }
            catch (Exception) { }

            try
            {
                mp3FileReader?.Dispose();
            }
            catch (Exception) { }

            wavePlayer = null;
            sampleChannel = null;
            mp3FileReader = null;

            try
            {
                File.Delete(currentSong.FilePath);
            }
            catch (Exception ex)
            {
                Log("卸载歌曲删除文件出错", ex);
            }

            dispatcher.Invoke(() => Songs.Remove(currentSong));

            currentSong = null;

            SetLyric(string.Empty, string.Empty);

            // TODO: PlayerBroadcasterLoop 功能

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalTime)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentTime)));
        }

        private void SetLyric(string current, string upcoming)
        {
            CurrentLyric = current;
            UpcomingLyric = upcoming;
            Task.Run(() => LyricEvent?.Invoke(this, new LyricChangedEventArgs()
            {
                CurrentLyric = current,
                UpcomingLyric = upcoming
            }));
        }

        /// <summary>
        /// 根据当前设置初始化 IWavePlayer
        /// </summary>
        /// <returns></returns>
        private IWavePlayer CreateIWavePlayer()
        {
            switch (PlayerType)
            {
                case PlayerType.WaveOutEvent:
                    return new WaveOutEvent() { DeviceNumber = WaveoutEventDevice };
                case PlayerType.DirectSound:
                    return new DirectSoundOut(DirectSoundDevice);
                default:
                    return null;
            }
        }

        /// <summary>
        /// 对外接口 继续
        /// <para>
        /// 注：此接口可在任意线程同步调用
        /// </para>
        /// </summary>
        public void Play()
        {
            if (wavePlayer != null)
            {
                wavePlayer.Play();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }

        }

        /// <summary>
        /// 对外接口 暂停
        /// <para>
        /// 注：此接口可在任意线程同步调用
        /// </para>
        /// </summary>
        public void Pause()
        {
            if (wavePlayer != null)
            {
                wavePlayer.Pause();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }

        }

        /// <summary>
        /// 对外接口 下一首
        /// <para>
        /// 注：此接口可在任意线程同步调用
        /// </para>
        /// </summary>
        public void Next()
        {
            if (wavePlayer != null)
            {
                wavePlayer.Stop();
            }
        }

        public event PropertyChangedEventHandler SongsListChanged;

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public event LyricChangedEvent LyricEvent;

        public event LogEvent LogEvent;
        private void Log(string message, Exception exception) => LogEvent?.Invoke(this, new LogEventArgs() { Message = message, Exception = exception });
    }
}
