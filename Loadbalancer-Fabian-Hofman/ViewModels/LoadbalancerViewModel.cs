using Loadbalancer.Loadbalancer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Loadbalancer.ViewModels
{
    class LoadbalancerViewModel : ViewModelBase
    {
        private Loadbalancer.Loadbalancer _loadbalancer;

        #region Commands
        private readonly CommandDelegate _startStopLoadbalancerCommand;
        private readonly CommandDelegate _clearLog;
        private readonly CommandDelegate _reloadAlgos;
        private readonly CommandDelegate _addServer;
        private readonly CommandDelegate _removeServer;
        public ICommand StartStopLoadbalancerCommand => _startStopLoadbalancerCommand;
        public ICommand ClearLogCommand => _clearLog;
        public ICommand ReloadAlgos => _reloadAlgos;
        public ICommand AddServer => _addServer;
        public ICommand RemoveServer => _removeServer;
        #endregion

        public LoadbalancerViewModel()
        {
            _loadbalancer = new Loadbalancer.Loadbalancer();
            _startStopLoadbalancerCommand = new CommandDelegate(OnStartStopLoadbalancer, CanStartStopLoadbalancer);
            _clearLog = new CommandDelegate(OnClearLog, CanClearLog);
            _reloadAlgos = new CommandDelegate(OnReloadAlgos, CanReloadAlgos);
            _addServer = new CommandDelegate(OnAddServer, CanAddServer);
            _removeServer = new CommandDelegate(OnRemoveServer, CanRemoveServer);
        }

        #region Bindings
        public bool IsRunning
        {
            get => _loadbalancer.IsRunning;
            set => SetProperty(_loadbalancer.IsRunning, value, () => _loadbalancer.IsRunning = value);
        }

        public int LoadbalancerPort
        {
            get => _loadbalancer.Port;
            set => SetProperty(_loadbalancer.Port, value, () => _loadbalancer.Port = value);
        }

        public int LoadbalancerHealthCheckInterval
        {
            get => _loadbalancer.HealthCheckInterval;
            set => SetProperty(_loadbalancer.HealthCheckInterval, value, () => _loadbalancer.HealthCheckInterval = value);
        }

        public int LoadbalancerBufferSize
        {
            get => _loadbalancer.BufferSize;
            set => SetProperty(_loadbalancer.BufferSize, value, () => _loadbalancer.BufferSize = value);
        }

        public ObservableCollection<ListBoxItem> Log
        {
            get => _loadbalancer.Log;
        }

        public ObservableCollection<ListBoxItem> Algorithms
        {
            get => _loadbalancer.Algorithms;
        }

        public string SelectedAlgorithmString
        {
            get => _loadbalancer.SelectedAlgorithmString;
            set => SetProperty(_loadbalancer.SelectedAlgorithmString, value, () => _loadbalancer.SelectedAlgorithmString = value);
        }

        public ObservableCollection<ListBoxItem> ServerList
        {
            get => _loadbalancer.ServerList;
        }

        public string AddServerIP
        {
            get => _loadbalancer.AddServerIP;
            set => SetProperty(_loadbalancer.AddServerIP, value, () => _loadbalancer.AddServerIP = value);
        }
        public int AddServerPort
        {
            get => _loadbalancer.AddServerPort;
            set => SetProperty(_loadbalancer.AddServerPort, value, () => _loadbalancer.AddServerPort = value);
        }

        public ListBoxItem SelectedItem
        {
            get { return _loadbalancer.SelectedServer; }
        }
        #endregion

        #region Delegate and predicates
        private async void OnStartStopLoadbalancer(object commandParameter)
        {
            if (!_loadbalancer.IsRunning)
            {
                await Task.Run(() => _loadbalancer.Start());
                NotifyPropertyChanged("IsRunning");
            }
            else
            {
                await Task.Run(() => _loadbalancer.Stop());
                NotifyPropertyChanged("IsRunning");
            }
        }
        private bool CanStartStopLoadbalancer(object commandParameter) => true;
        public void OnClearLog(object commandParameter) => _loadbalancer.ClearLog();
        private bool CanClearLog(object commandParameter) => true;
        public void OnReloadAlgos(object commandParameter) => _loadbalancer.InitAlgos();
        private bool CanReloadAlgos(object commandParameter) => true;
        public void OnAddServer(object commandParameter) => _loadbalancer.AddServer();
        private bool CanAddServer(object commandParameter) => true;
        private void OnRemoveServer(object commandParameter) => _loadbalancer.RemoveServer(commandParameter);
        private bool CanRemoveServer(object commandParameter) => true;

        #endregion
    }
}
