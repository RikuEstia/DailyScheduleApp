using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace DailyScheduleApp
{
    public partial class MainWindow : Window
    {
        private const string AppName = "DailyScheduleApp";
        private const string StartupRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public ObservableCollection<ScheduleItem> Schedule { get; set; } = new ObservableCollection<ScheduleItem>();
        public ObservableCollection<string> Tips { get; set; } = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();
            LoadAutoStartSetting();

            this.Activated += MainWindow_Activated;
            this.Deactivated += MainWindow_Deactivated;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += UpdateDateTime;
            timer.Start();

            LoadTodaySchedule();
        }

        private void MainWindow_Activated(object? sender, EventArgs? e)
        {
            // Set to regular purple when window is focused
            this.Background = (SolidColorBrush)Application.Current.Resources["WindowActiveBackground"];
        }

        private void MainWindow_Deactivated(object? sender, EventArgs? e)
        {
            // Set to darker purple when window is out of focus
            this.Background = (SolidColorBrush)Application.Current.Resources["WindowInactiveBackground"];
        }

        private void LoadAutoStartSetting()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, false))
            {
                if (key != null)
                {
                    string? value = key.GetValue(AppName) as string;
                    AutoStartCheckBox.IsChecked = !string.IsNullOrEmpty(value);
                }
            }
        }

        private void AutoStartCheckBox_Checked(object? sender, RoutedEventArgs? e)
        {
            SetAutoStart(true);
        }

        private void AutoStartCheckBox_Unchecked(object? sender, RoutedEventArgs? e)
        {
            SetAutoStart(false);
        }

        private void SetAutoStart(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, true))
            {
                if (enable)
                {
                    string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    key.SetValue(AppName, $"\"{executablePath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }

        private void UpdateDateTime(object? sender, EventArgs? e)
        {
            DateTimeLabel.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy HH:mm:ss");
        }

        private void LoadTodaySchedule()
        {
            string dayOfWeek = DateTime.Now.DayOfWeek.ToString();
            string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{dayOfWeek}.json");

            if (File.Exists(fileName))
            {
                var json = File.ReadAllText(fileName);
                var dailyData = JsonConvert.DeserializeObject<DailyData>(json);

                if (dailyData != null)
                {
                    Schedule = new ObservableCollection<ScheduleItem>(dailyData.Schedule ?? new List<ScheduleItem>());
                    Tips = new ObservableCollection<string>(dailyData.Tips ?? new List<string>());

                    ScheduleDataGrid.ItemsSource = Schedule;
                    TipsListBox.ItemsSource = Tips;
                }
                else
                {
                    MessageBox.Show("Error loading schedule data.");
                }
            }
            else
            {
                MessageBox.Show($"Schedule file for {dayOfWeek} not found.");
            }
        }

        private void SaveSchedule_Click(object? sender, RoutedEventArgs? e)
        {
            var dailyData = new DailyData
            {
                Schedule = new List<ScheduleItem>(Schedule),
                Tips = new List<string>(Tips)
            };

            string dayOfWeek = DateTime.Now.DayOfWeek.ToString();
            string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{dayOfWeek}.json");

            var json = JsonConvert.SerializeObject(dailyData, Formatting.Indented);
            File.WriteAllText(fileName, json);
            MessageBox.Show("Schedule saved successfully!");
        }
    }

    public class ScheduleItem
    {
        public string Time { get; set; } = string.Empty;
        public string Activity { get; set; } = string.Empty;
        public bool Done { get; set; } = false;
    }

    public class DailyData
    {
        public List<ScheduleItem> Schedule { get; set; } = new List<ScheduleItem>();
        public List<string> Tips { get; set; } = new List<string>();
    }
}
