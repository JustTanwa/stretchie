using stretchie;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace stretchie
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _masterTimer;
        private TimeSpan _timeRemaining;
        private TimeSpan _workInterval = TimeSpan.FromMinutes(45);

        // Routine State Variables
        private List<StretchStep> _routine;
        private int _currentStepIndex = 0;
        private int _holdTimeRemaining = 0;
        private bool _isDoingRoutine = false;
        private bool _isPaused = false;
        // Path definitions targeting local AppData profiles
        private readonly string _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "stretchie", "settings.json");

        public MainWindow()
        {
            InitializeComponent();
            //InitializeRoutineList();
            LoadApplicationConfiguration();
            SetupMasterTimer();
        }

        private void SetupMasterTimer()
        {
            _timeRemaining = _workInterval;
            _masterTimer = new DispatcherTimer();
            _masterTimer.Interval = TimeSpan.FromSeconds(1);
            _masterTimer.Tick += Timer_Tick;
            _masterTimer.Start();
            UpdateTimerDisplay();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_isPaused)
            {
                return; // Skip timer updates if paused
            }
            if (!_isDoingRoutine)
            {
                // ---- COUNTDOWN MODE ----
                if (_timeRemaining > TimeSpan.Zero)
                {
                    _timeRemaining = _timeRemaining.Subtract(TimeSpan.FromSeconds(1));
                    UpdateTimerDisplay();
                }
                else
                {
                    StartStretchRoutine();
                }
            }
            else
            {
                // ---- ROUTINE MODE (For Timed Holds) ----
                if (_routine[_currentStepIndex].Type == StepType.Hold && _holdTimeRemaining > 0)
                {
                    _holdTimeRemaining--;
                    TxtRoutineTimer.Text = $"{_holdTimeRemaining}s";

                    if (_holdTimeRemaining == 0)
                    {
                        //PlayCustomChime();
                        TxtRoutineTimer.Text = "Done!";
                        BtnNextStep.IsEnabled = true;
                    }
                }
            }
        }

        private void UpdateTimerDisplay()
        {
            TxtTimer.Text = _timeRemaining.ToString(@"mm\:ss");
        }

        private void StartStretchRoutine()
        {
            _isDoingRoutine = true;
            _currentStepIndex = 0;

            // Force window into user view
            this.WindowState = WindowState.Normal;
            this.Topmost = true;
            this.Activate();
            PlayCustomChime();

            // Swap view visibility
            ViewCountdown.Visibility = Visibility.Collapsed;
            GridRoutine.Visibility = Visibility.Visible;

            PresentCurrentStep();
        }
        private void PlayCustomChime()
        {
            try
            {
                // UriKind.Relative handles resources inside the running binary package
                var soundUri = new Uri("pack://application:,,,/assets/playful_sound.wav");
                var streamInfo = Application.GetResourceStream(soundUri);

                if (streamInfo != null)
                {
                    using (Stream soundStream = streamInfo.Stream)
                    {
                        SoundPlayer player = new SoundPlayer(soundStream);
                        player.Play();
                    }
                }
            }
            catch (Exception)
            {
                // Fallback pattern if the file path breaks or is unreadable
                System.Media.SystemSounds.Exclamation.Play();
            }
        }


        private void PresentCurrentStep()
        {
            StretchStep currentStep = _routine[_currentStepIndex];
            TxtRoutineProgress.Text = $"Stretch {_currentStepIndex + 1} of {_routine.Count}";
            TxtStretchName.Text = currentStep.Name;

            if (currentStep.Type == StepType.Repetition)
            {
                TxtStretchInstruction.Text = $"Perform {currentStep.TargetCount} repetitions.";
                TxtRoutineTimer.Visibility = Visibility.Collapsed;
                BtnNextStep.Content = "Done / Next";
                BtnNextStep.IsEnabled = true;
            }
            else if (currentStep.Type == StepType.Hold)
            {
                _holdTimeRemaining = currentStep.DurationSeconds;
                TxtStretchInstruction.Text = $"Hold stretch position ({currentStep.TargetCount} set{(currentStep.TargetCount > 1 ? "s" : "")})";
                TxtRoutineTimer.Text = $"{_holdTimeRemaining}s";
                TxtRoutineTimer.Visibility = Visibility.Visible;

                // Disable button during the hold loop so user is forced to stretch
                BtnNextStep.Content = "Hold...";
                BtnNextStep.IsEnabled = false;
            }
        }

        private void BtnNextStep_Click(object sender, RoutedEventArgs e)
        {
            _currentStepIndex++;

            if (_currentStepIndex < _routine.Count)
            {
                PresentCurrentStep();
            }
            else
            {
                // Routine Complete -> Reset everything back to work mode
                _isDoingRoutine = false;
                GridRoutine.Visibility = Visibility.Collapsed;
                ViewCountdown.Visibility = Visibility.Visible;

                this.Topmost = false;
                _timeRemaining = _workInterval;
                UpdateTimerDisplay();
            }
        }

        private void BtnSkip_Click(object sender, RoutedEventArgs e)
        {
            _timeRemaining = _workInterval;
            UpdateTimerDisplay();
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            _isPaused = !_isPaused; // Toggle the state

            if (_isPaused)
            {
                // UI Changes for Paused State
                BtnPause.Content = "Resume Timer";
                BtnPause.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60")); // Green
                TxtStatus.Text = "Timer Paused (Away from Desk)";
                TxtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E67E22")); // Orange
                TxtTimer.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BDC3C7")); // Gray out time
            }
            else
            {
                // UI Changes for Resumed State
                BtnPause.Content = "Pause Timer";
                BtnPause.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D35400")); // Orange/Red
                TxtStatus.Text = "Time until next stretch:";
                TxtStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F8C8D")); // Default Gray
                TxtTimer.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2980B9")); // Default Blue
            }
        }

        // ================= SIDE MENU NAVIGATION CONTROLS =================
        private void BtnNavTimer_Click(object sender, RoutedEventArgs e)
        {
            ViewSettings.Visibility = Visibility.Collapsed;
            ViewCountdown.Visibility = Visibility.Visible;

            // Highlights active panel
            BtnNavTimer.Background = new SolidColorBrush(Color.FromRgb(52, 152, 219));
            BtnNavSettings.Background = Brushes.Transparent;
        }

        private void BtnNavSettings_Click(object sender, RoutedEventArgs e)
        {
            ViewCountdown.Visibility = Visibility.Collapsed;
            ViewSettings.Visibility = Visibility.Visible;

            BtnNavSettings.Background = new SolidColorBrush(Color.FromRgb(52, 152, 219));
            BtnNavTimer.Background = Brushes.Transparent;
        }

        // ================= DATA SUBMISSION ACTIONS =================
        private void BtnSaveTimer_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtInputMinutes.Text, out int minutes) && minutes > 0)
            {
                _workInterval = TimeSpan.FromMinutes(minutes);
                _timeRemaining = _workInterval; // update running clock instantly
                UpdateTimerDisplay();
                SaveApplicationConfiguration();
                MessageBox.Show($"Timer interval successfully updated to {minutes} minutes!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please insert a valid positive number for minutes.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnAddExercise_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtNewExerciseName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Exercise title field cannot be blank.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StepType type = CmbNewExerciseType.SelectedIndex == 0 ? StepType.Repetition : StepType.Hold;
            int.TryParse(TxtNewExerciseTarget.Text, out int target);
            int.TryParse(TxtNewExerciseDuration.Text, out int duration);

            // Create step item (Default blank fallback icon string assigned)
            StretchStep newStep = new StretchStep(name, type, target, duration);

            _routine.Add(newStep);
            SaveApplicationConfiguration();

            // Clear Input Fields
            TxtNewExerciseName.Clear();
            MessageBox.Show($"\"{name}\" successfully injected into your hourly fitness routines!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDeleteExercise_Click(object sender, RoutedEventArgs e)
        {
            // Cast selected item back into our structure object
            if (LstExercises.SelectedItem is StretchStep selectedExercise)
            {
                var result = MessageBox.Show($"Are you sure you want to remove \"{selectedExercise.Name}\"?",
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _routine.Remove(selectedExercise);
                    SaveApplicationConfiguration(); // Automatically updates UI and rewrites json file safely
                }
            }
            else
            {
                MessageBox.Show("Please select an exercise from the list first to delete.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void SaveApplicationConfiguration()
        {
            var configToSave = new AppSettings
            {
                TimerMinutes = (int)_workInterval.TotalMinutes,
                Exercises = _routine
            };

            // Removed the rogue ReadAllTextAsync call that caused the file lock
            string jsonString = JsonSerializer.Serialize(configToSave, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, jsonString);

            // Refresh the UI ListBox display whenever data updates
            UpdateExerciseListBox();
        }

        private void UpdateExerciseListBox()
        {
            LstExercises.ItemsSource = null;
            LstExercises.ItemsSource = _routine;
        }
        // ================= CONFIGURATION MANAGER (THE "DATABASE" REPLACEMENT) =================
        private void LoadApplicationConfiguration()
        {
            try
            {
                string directory = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

                if (File.Exists(_filePath))
                {
                    string jsonString = File.ReadAllText(_filePath);
                    AppSettings savedSettings = JsonSerializer.Deserialize<AppSettings>(jsonString);

                    _workInterval = TimeSpan.FromMinutes(savedSettings.TimerMinutes);
                    _routine = savedSettings.Exercises ?? new List<StretchStep>();
                    TxtInputMinutes.Text = savedSettings.TimerMinutes.ToString();

                    UpdateExerciseListBox();
                }
                else
                {
                    LoadDefaults();
                }
            }
            catch
            {
                LoadDefaults();
            }
        }

        private void LoadDefaults()
        {
            _workInterval = TimeSpan.FromMinutes(60);
            _routine = new List<StretchStep>
            {
                new StretchStep("Chin Tucks", StepType.Repetition, 5, 0, "/Assets/chin_tuck.png"),
                new StretchStep("Ear to Shoulder (Left)", StepType.Hold, 1, 10, "/Assets/ear_to_shoulder.png"),
                new StretchStep("Ear to Shoulder (Right)", StepType.Hold, 1, 10, "/Assets/ear_to_shoulder.png")
            };
            SaveApplicationConfiguration();
        }
    }
}
