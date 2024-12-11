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
        // Constants for application name and Windows startup registry path
        private const string AppName = "DailyScheduleApp";
        private const string StartupRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        // Observable collections to bind schedule items and tips to the UI
        public ObservableCollection<ScheduleItem> Schedule { get; set; } = new ObservableCollection<ScheduleItem>();
        public ObservableCollection<string> Tips { get; set; } = new ObservableCollection<string>();

        public MainWindow()
        {
            InitializeComponent();

            // Load the auto-start checkbox state based on the registry
            LoadAutoStartSetting();

            // Event handlers for window activation and deactivation
            this.Activated += MainWindow_Activated;
            this.Deactivated += MainWindow_Deactivated;

            // Timer to update the current date and time in real-time
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += UpdateDateTime;
            timer.Start();

            // Load today's schedule from the corresponding JSON file
            LoadTodaySchedule();
        }

        private void MainWindow_Activated(object? sender, EventArgs? e)
        {
            // Change background color when the window gains focus
            this.Background = (SolidColorBrush)Application.Current.Resources["WindowActiveBackground"];
        }

        private void MainWindow_Deactivated(object? sender, EventArgs? e)
        {
            // Change background color when the window loses focus
            this.Background = (SolidColorBrush)Application.Current.Resources["WindowInactiveBackground"];
        }

        private void LoadAutoStartSetting()
        {
            // Check the registry to see if the app is set to run at startup
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, false))
            {
                if (key != null)
                {
                    string? value = key.GetValue(AppName) as string;
                    AutoStartCheckBox.IsChecked = !string.IsNullOrEmpty(value); // Update UI checkbox
                }
            }
        }

        private void AutoStartCheckBox_Checked(object? sender, RoutedEventArgs? e)
        {
            // Enable auto-start when the checkbox is checked
            SetAutoStart(true);
        }

        private void AutoStartCheckBox_Unchecked(object? sender, RoutedEventArgs? e)
        {
            // Disable auto-start when the checkbox is unchecked
            SetAutoStart(false);
        }

        private void SetAutoStart(bool enable)
        {
            // Add or remove the app's registry entry for Windows startup
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, true))
            {
                if (enable)
                {
                    // Add the executable path to the registry
                    string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    key.SetValue(AppName, $"\"{executablePath}\"");
                }
                else
                {
                    // Remove the registry entry if it exists
                    key.DeleteValue(AppName, false);
                }
            }
        }

        private void UpdateDateTime(object? sender, EventArgs? e)
        {
            // Update the UI label with the current date and time every second
            DateTimeLabel.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy HH:mm:ss");
        }

        private void LoadTodaySchedule()
        {
            // Determine the current day of the week and corresponding schedule file name
            string dayOfWeek = DateTime.Now.DayOfWeek.ToString();
            string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{dayOfWeek}.json");

            if (File.Exists(fileName))
            {
                // Read the JSON file and deserialize it into daily data
                var json = File.ReadAllText(fileName);
                var dailyData = JsonConvert.DeserializeObject<DailyData>(json);

                if (dailyData != null)
                {
                    // Populate the schedule and tips collections for binding to the UI
                    Schedule = new ObservableCollection<ScheduleItem>(dailyData.Schedule ?? new List<ScheduleItem>());
                    Tips = new ObservableCollection<string>(dailyData.Tips ?? new List<string>());

                    // Bind the collections to the UI controls
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
                // Notify the user if the file for the current day is missing
                MessageBox.Show($"Schedule file for {dayOfWeek} not found.");
            }
        }

        private void SaveSchedule_Click(object? sender, RoutedEventArgs? e)
        {
            // Create a DailyData object from the current schedule and tips
            var dailyData = new DailyData
            {
                Schedule = new List<ScheduleItem>(Schedule),
                Tips = new List<string>(Tips)
            };

            // Determine the file name based on the current day of the week
            string dayOfWeek = DateTime.Now.DayOfWeek.ToString();
            string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{dayOfWeek}.json");

            // Serialize the DailyData object to JSON and save it to the file
            var json = JsonConvert.SerializeObject(dailyData, Formatting.Indented);
            File.WriteAllText(fileName, json);

            // Notify the user that the schedule was saved successfully
            MessageBox.Show("Schedule saved successfully!");
        }
    }

    // Represents an individual schedule item
    public class ScheduleItem
    {
        public string Time { get; set; } = string.Empty; // Time of the activity
        public string Activity { get; set; } = string.Empty; // Description of the activity
        public bool Done { get; set; } = false; // Status of completion
    }

    // Represents the daily schedule data with schedule items and tips
    public class DailyData
    {
        public List<ScheduleItem> Schedule { get; set; } = new List<ScheduleItem>(); // List of scheduled activities
        public List<string> Tips { get; set; } = new List<string>(); // List of tips for the day
    }
}
