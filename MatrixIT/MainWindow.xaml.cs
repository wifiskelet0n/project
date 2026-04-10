using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MatrixIT
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MeterMonitorViewModel();
        }
    }

    public class MeterMonitorViewModel : INotifyPropertyChanged
    {
        // Модель данных счётчика
        public class WaterMeter
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Location { get; set; }
            public string RemoteAddress { get; set; } // IP или идентификатор в сети телеметрии
        }

        // Имитация базы данных объектов учёта
        public ObservableCollection<WaterMeter> MeterList { get; } = new ObservableCollection<WaterMeter>
        {
            new WaterMeter { Id = "MTR-001", Name = "Окей Уфа", Location = "ул. Ленина, 15", RemoteAddress = "192.168.10.101" },
            new WaterMeter { Id = "MTR-002", Name = "Благовещенск водоканал", Location = "промзона Восточная", RemoteAddress = "192.168.10.205" },
            new WaterMeter { Id = "MTR-003", Name = "Башня Субай", Location = "северный водозабор", RemoteAddress = "192.168.10.78" }
        };

        private WaterMeter _selectedMeter;
        public WaterMeter SelectedMeter
        {
            get => _selectedMeter;
            set { _selectedMeter = value; OnPropertyChanged(); _ = RefreshDataAsync(); }
        }

        private double _currentReading;
        public double CurrentReading
        {
            get => _currentReading;
            set { _currentReading = value; OnPropertyChanged(); }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private string _statusColor = "Green";
        public string StatusColor
        {
            get => _statusColor;
            set { _statusColor = value; OnPropertyChanged(); }
        }

        private DateTime _lastUpdateTime;
        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set { _lastUpdateTime = value; OnPropertyChanged(); }
        }

        // Команды
        public ICommand RefreshCommand => new RelayCommand(async _ => await RefreshDataAsync());
        public ICommand SendReportCommand => new RelayCommand(_ => SendReport());

        public MeterMonitorViewModel()
        {
            SelectedMeter = MeterList.Count > 0 ? MeterList[0] : null;
        }

        /// <summary>
        /// Основной алгоритм опроса счётчика из труднодоступного места.
        /// Этот алгоритм и будет предметом обратного проектирования.
        /// </summary>
        private async Task RefreshDataAsync()
        {
            if (SelectedMeter == null)
            {
                StatusMessage = "Счётчик не выбран";
                StatusColor = "Red";
                return;
            }

            StatusMessage = "Опрос устройства...";
            StatusColor = "Orange";

            try
            {
                // 1. Установка соединения с удалённым устройством (имитация задержки)
                await Task.Delay(800); // эмуляция сетевой задержки

                // 2. Отправка команды запроса показаний
                // В реальной системе используется протокол Modbus/MQTT/SNMP
                bool connectionOk = await SimulateConnectAndSendCommand(SelectedMeter.RemoteAddress);

                if (!connectionOk)
                {
                    StatusMessage = "Ошибка связи с устройством";
                    StatusColor = "Red";
                    return;
                }

                // 3. Получение и декодирование данных (например, по Modbus)
                double rawValue = await SimulateReadMeterValue(SelectedMeter.Id);

                // 4. Валидация данных (проверка на аномалии: резкий скачок, отрицательное значение)
                if (rawValue < 0 || (CurrentReading > 0 && Math.Abs(rawValue - CurrentReading) > 500))
                {
                    StatusMessage = "Получены подозрительные данные. Требуется проверка.";
                    StatusColor = "DarkOrange";
                    // В реальной системе здесь может быть отправка уведомления диспетчеру
                }
                else
                {
                    StatusMessage = "Данные обновлены успешно";
                    StatusColor = "Green";
                }

                // 5. Обновление интерфейса
                CurrentReading = rawValue;
                LastUpdateTime = DateTime.Now;

                // 6. Логирование события опроса (в файл или БД)
                LogPollingEvent(SelectedMeter.Id, rawValue, StatusMessage);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
                StatusColor = "Red";
                // В реальной системе: запись в журнал ошибок, уведомление администратора
            }
        }

        // Имитация отправки команды и установки соединения
        private async Task<bool> SimulateConnectAndSendCommand(string address)
        {
            await Task.Delay(200);
            // Имитация: 95% успешных соединений
            return new Random().Next(100) < 95;
        }

        // Имитация чтения значения со счётчика
        private async Task<double> SimulateReadMeterValue(string meterId)
        {
            await Task.Delay(300);
            // Генерация реалистичных показаний (например, 12345.67 + небольшое приращение)
            Random rnd = new Random();
            double baseValue = 12345.67 + rnd.NextDouble() * 100;
            return Math.Round(baseValue, 2);
        }

        private void LogPollingEvent(string meterId, double value, string status)
        {
            // В реальной системе: запись в БД или файл
            System.Diagnostics.Debug.WriteLine($"{DateTime.Now:O}: Meter {meterId} = {value:F2}, Status: {status}");
        }

        private void SendReport()
        {
            // Формирование отчёта (упрощённо)
            MessageBox.Show($"Отчёт по {SelectedMeter?.Name}\nПоказания: {CurrentReading} м³\nВремя: {LastUpdateTime}",
                            "ПАК Матрикс: Отчёт сформирован");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // Простейшая реализация RelayCommand
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;
        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object parameter) => _execute(parameter);
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}