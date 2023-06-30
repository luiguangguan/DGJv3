﻿using DGJv3.InternalModule;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace DGJv3
{
    class SearchModules : INotifyPropertyChanged
    {
        public SearchModule NullModule { get; private set; }
        public ObservableCollection<SearchModule> Modules { get; set; }
        public SearchModule PrimaryModule { get => primaryModule; set => SetField(ref primaryModule, value); }
        public SearchModule SecondaryModule { get => secondaryModule; set => SetField(ref secondaryModule, value); }

        private SearchModule primaryModule;
        private SearchModule secondaryModule;


        internal SearchModules()
        {
            Modules = new ObservableCollection<SearchModule>();
            Modules.CollectionChanged += Modules_CollectionChanged;

            NullModule = new NullSearchModule();
            Modules.Add(NullModule);

            Modules.Add(new LwlApiNetease());
            Modules.Add(new LwlApiTencent());
            Modules.Add(new LwlApiKugou());
            Modules.Add(new LwlApiBaidu());
            Modules.Add(new LwlApiXiami());


            // TODO: 加载外置拓展

            void logaction(string log)
            {
                Log(log);
            }

            foreach (var m in Modules)
            {
                m._log = logaction;
            }

            PrimaryModule = Modules[1];
            SecondaryModule = Modules[2];
        }

        private void Modules_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Config config = Config.Load();
            if (config != null)
            {

                PrimaryModule = Modules.FirstOrDefault(x => x.UniqueId == config.PrimaryModuleId) ?? PrimaryModule;
                SecondaryModule = Modules.FirstOrDefault(x => x.UniqueId == config.SecondaryModuleId) ?? SecondaryModule;

                //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PrimaryModule)));
                //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SecondaryModule)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        public event LogEvent LogEvent;
        private void Log(string message, Exception exception = null) => LogEvent?.Invoke(this, new LogEventArgs() { Message = message, Exception = exception });
    }
}
