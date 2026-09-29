using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PCBooster.ViewModels
{
    // ── Base ─────────────────────────────────────────────────────────────
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool Set<T>(ref T field, T value, [CallerMemberName] string name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }

    // ── Relay Command ─────────────────────────────────────────────────────
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute    = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
            : this(_ => execute(), canExecute == null ? null : _ => canExecute()) { }

        public event EventHandler CanExecuteChanged
        {
            add    => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object p) => _canExecute?.Invoke(p) ?? true;
        public void Execute(object p)    => _execute(p);
        public void RaiseCanExecuteChanged() =>
            System.Windows.Application.Current?.Dispatcher.Invoke(
                CommandManager.InvalidateRequerySuggested);
    }

    // ── Dashboard ViewModel ───────────────────────────────────────────────
    public class DashboardViewModel : ViewModelBase
    {
        private int    _score         = 68;
        private float  _cpuPercent    = 0;
        private float  _ramPercent    = 0;
        private float  _diskPercent   = 0;
        private float  _tempValue     = 55;
        private string _statusText    = "System monitoring active";
        private bool   _isBoosting    = false;
        private int    _alertCount    = 0;

        public int    Score         { get => _score;       set => Set(ref _score, value); }
        public float  CpuPercent    { get => _cpuPercent;  set => Set(ref _cpuPercent, value); }
        public float  RamPercent    { get => _ramPercent;  set => Set(ref _ramPercent, value); }
        public float  DiskPercent   { get => _diskPercent; set => Set(ref _diskPercent, value); }
        public float  TempValue     { get => _tempValue;   set => Set(ref _tempValue, value); }
        public string StatusText    { get => _statusText;  set => Set(ref _statusText, value); }
        public bool   IsBoosting    { get => _isBoosting;  set => Set(ref _isBoosting, value); }
        public int    AlertCount    { get => _alertCount;  set => Set(ref _alertCount, value); }

        public string ScoreLabel    => $"{Score}/100";
        public string CpuLabel      => $"{CpuPercent:F0}%";
        public string RamLabel      => $"{RamPercent:F0}%";
        public string DiskLabel     => $"{DiskPercent:F0}%";
        public string TempLabel     => $"{TempValue:F0}°C";
        public string AlertBadge    => AlertCount > 0 ? AlertCount.ToString() : "";

        public void UpdateMetrics(float cpu, float ram, float disk, float temp)
        {
            CpuPercent  = cpu;
            RamPercent  = ram;
            DiskPercent = disk;
            TempValue   = temp;
            // Cascade computed labels
            OnPropertyChanged(nameof(CpuLabel));
            OnPropertyChanged(nameof(RamLabel));
            OnPropertyChanged(nameof(DiskLabel));
            OnPropertyChanged(nameof(TempLabel));
        }

        public void AddScore(int gain)
        {
            Score = Math.Min(100, Score + gain);
            OnPropertyChanged(nameof(ScoreLabel));
        }
    }

    // ── Process Entry ViewModel ───────────────────────────────────────────
    public class ProcessViewModel : ViewModelBase
    {
        private float _cpuPercent;
        public int    Pid         { get; set; }
        public string Name        { get; set; }
        public double RamMB       { get; set; }
        public float  CpuPercent  { get => _cpuPercent; set => Set(ref _cpuPercent, value); }
        public string Status      { get; set; }
        public string StartTimeDisplay { get; set; }
        public string Impact      => CpuPercent >= 20 || RamMB >= 500 ? "High"
                                   : CpuPercent >= 5  || RamMB >= 150 ? "Medium" : "Low";
    }

    // ── Settings ViewModel ────────────────────────────────────────────────
    public class SettingsViewModel : ViewModelBase
    {
        private Services.AppSettings _settings;

        public SettingsViewModel()
        {
            _settings = Services.SettingsService.Load();
        }

        public int  PollSeconds
        {
            get => _settings.MetricsPollSeconds;
            set { _settings.MetricsPollSeconds = value; OnPropertyChanged(); }
        }
        public bool ShowTemperature
        {
            get => _settings.ShowTemperature;
            set { _settings.ShowTemperature = value; OnPropertyChanged(); }
        }
        public bool RecordHistory
        {
            get => _settings.RecordHistory;
            set { _settings.RecordHistory = value; OnPropertyChanged(); }
        }
        public bool NotifyOnHighCpu
        {
            get => _settings.NotifyOnHighCpu;
            set { _settings.NotifyOnHighCpu = value; OnPropertyChanged(); }
        }
        public int HighCpuThreshold
        {
            get => _settings.HighCpuThreshold;
            set { _settings.HighCpuThreshold = value; OnPropertyChanged(); }
        }
        public bool NotifyOnHighRam
        {
            get => _settings.NotifyOnHighRam;
            set { _settings.NotifyOnHighRam = value; OnPropertyChanged(); }
        }
        public int HighRamThreshold
        {
            get => _settings.HighRamThreshold;
            set { _settings.HighRamThreshold = value; OnPropertyChanged(); }
        }
        public bool RunOnStartup
        {
            get => _settings.RunOnWindowsStartup;
            set { _settings.RunOnWindowsStartup = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand => new RelayCommand(() =>
        {
            Services.SettingsService.Save(_settings);
            if (RunOnStartup) Services.SettingsService.SetRunOnStartup(true);
            else              Services.SettingsService.SetRunOnStartup(false);
        });
    }

    // ── Benchmark ViewModel ───────────────────────────────────────────────
    public class BenchmarkViewModel : ViewModelBase
    {
        private string _intScore    = "—";
        private string _floatScore  = "—";
        private string _mtScore     = "—";
        private string _ramBwScore  = "—";
        private string _intRating   = "";
        private string _floatRating = "";
        private string _mtRating    = "";
        private string _ramBwRating = "";
        private double _progress    = 0;
        private string _status      = "Press 'Run All Tests' to start";
        private bool   _isRunning   = false;

        public string IntScore    { get => _intScore;    set => Set(ref _intScore,    value); }
        public string FloatScore  { get => _floatScore;  set => Set(ref _floatScore,  value); }
        public string MtScore     { get => _mtScore;     set => Set(ref _mtScore,     value); }
        public string RamBwScore  { get => _ramBwScore;  set => Set(ref _ramBwScore,  value); }
        public string IntRating   { get => _intRating;   set => Set(ref _intRating,   value); }
        public string FloatRating { get => _floatRating; set => Set(ref _floatRating, value); }
        public string MtRating    { get => _mtRating;    set => Set(ref _mtRating,    value); }
        public string RamBwRating { get => _ramBwRating; set => Set(ref _ramBwRating, value); }
        public double Progress    { get => _progress;    set => Set(ref _progress,    value); }
        public string Status      { get => _status;      set => Set(ref _status,      value); }
        public bool   IsRunning   { get => _isRunning;   set { Set(ref _isRunning, value); OnPropertyChanged(nameof(CanRun)); } }
        public bool   CanRun      => !IsRunning;
    }
}
