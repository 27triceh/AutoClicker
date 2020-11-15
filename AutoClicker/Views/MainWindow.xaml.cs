﻿using System;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Interop;
using AutoClicker.Enums;
using AutoClicker.Utils;
using CanExecuteRoutedEventArgs = System.Windows.Input.CanExecuteRoutedEventArgs;
using ExecutedRoutedEventArgs = System.Windows.Input.ExecutedRoutedEventArgs;
using MouseCursor = System.Windows.Forms.Cursor;
using Point = System.Drawing.Point;

namespace AutoClicker.Views
{
    public partial class MainWindow : Window
    {
        #region Dependency Properties

        public int Hours
        {
            get => (int)GetValue(HoursProperty);
            set => SetValue(HoursProperty, value);
        }

        public static readonly DependencyProperty HoursProperty =
            DependencyProperty.Register(nameof(Hours), typeof(int), typeof(MainWindow),
                new PropertyMetadata(default(int)));

        public int Minutes
        {
            get => (int)GetValue(MinutesProperty);
            set => SetValue(MinutesProperty, value);
        }

        public static readonly DependencyProperty MinutesProperty =
            DependencyProperty.Register(nameof(Minutes), typeof(int), typeof(MainWindow),
                new PropertyMetadata(default(int)));

        public int Seconds
        {
            get => (int)GetValue(SecondsProperty);
            set => SetValue(SecondsProperty, value);
        }

        public static readonly DependencyProperty SecondsProperty =
            DependencyProperty.Register(nameof(Seconds), typeof(int), typeof(MainWindow),
                new PropertyMetadata(default(int)));

        public int Milliseconds
        {
            get => (int)GetValue(MillisecondsProperty);
            set => SetValue(MillisecondsProperty, value);
        }

        public static readonly DependencyProperty MillisecondsProperty =
            DependencyProperty.Register(nameof(Milliseconds), typeof(int), typeof(MainWindow),
                new PropertyMetadata(default(int)));

        public MouseButton SelectedMouseButton
        {
            get => (MouseButton)GetValue(SelectedMouseButtonProperty);
            set => SetValue(SelectedMouseButtonProperty, value);
        }

        public static readonly DependencyProperty SelectedMouseButtonProperty =
            DependencyProperty.Register(nameof(SelectedMouseButton), typeof(MouseButton), typeof(MainWindow),
                new PropertyMetadata(default(MouseButton)));

        public MouseAction SelectedMouseAction
        {
            get => (MouseAction)GetValue(SelectedMouseActionProperty);
            set => SetValue(SelectedMouseActionProperty, value);
        }

        public static readonly DependencyProperty SelectedMouseActionProperty =
            DependencyProperty.Register(nameof(SelectedMouseAction), typeof(MouseAction), typeof(MainWindow),
                new PropertyMetadata(default(MouseAction)));
        public RepeatMode SelectedRepeatMode
        {
            get => (RepeatMode)GetValue(SelectedRepeatModeProperty);
            set => SetValue(SelectedRepeatModeProperty, value);
        }

        public static readonly DependencyProperty SelectedRepeatModeProperty =
            DependencyProperty.Register(nameof(SelectedRepeatMode), typeof(RepeatMode), typeof(MainWindow),
                new PropertyMetadata(default(RepeatMode)));

        public LocationMode SelectedLocationMode
        {
            get => (LocationMode)GetValue(SelectedLocationModeProperty);
            set => SetValue(SelectedLocationModeProperty, value);
        }

        public static readonly DependencyProperty SelectedLocationModeProperty =
            DependencyProperty.Register(nameof(SelectedLocationMode), typeof(LocationMode), typeof(MainWindow),
                new PropertyMetadata(default(LocationMode)));

        public int PickedXValue
        {
            get => (int)GetValue(PickedXValueProperty);
            set => SetValue(PickedXValueProperty, value);
        }

        public static readonly DependencyProperty PickedXValueProperty =
            DependencyProperty.Register(nameof(PickedXValue), typeof(int), typeof(MainWindow),
                new PropertyMetadata(default(int)));

        public int PickedYValue
        {
            get => (int)GetValue(PickedYValueProperty);
            set => SetValue(PickedYValueProperty, value);
        }

        public static readonly DependencyProperty PickedYValueProperty =
            DependencyProperty.Register(nameof(PickedYValue), typeof(int), typeof(MainWindow),
                new PropertyMetadata(default(int)));

        public int SelectedTimesToRepeat
        {
            get => (int)GetValue(SelectedTimesToRepeatProperty);
            set => SetValue(SelectedTimesToRepeatProperty, value);
        }

        public static readonly DependencyProperty SelectedTimesToRepeatProperty =
            DependencyProperty.Register(nameof(SelectedTimesToRepeat), typeof(int), typeof(MainWindow),
                new PropertyMetadata(default(int)));

        #endregion Dependency Properties

        #region Fields

        private int timesRepeated = 0;
        private readonly Timer clickTimer;
        private AboutWindow aboutWindow = null;
        private SettingsWindow settingsWindow = null;

        private IntPtr _windowHandle;
        private HwndSource _source;

        #endregion Fields

        #region Lifetime

        public MainWindow()
        {
            clickTimer = new Timer();
            clickTimer.Elapsed += OnClickTimerElapsed;

            DataContext = this;
            ResetTitle();
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);
            _source.AddHook(StartStopHooks);

            AppSettings.HotKeyChangedEvent += AppSettings_HotKeyChanged;

            RegisterHotKey(_windowHandle, Constants.START_HOTKEY_ID, Constants.MOD_NONE, AppSettings.StartHotkey.VirtualCode);
            RegisterHotKey(_windowHandle, Constants.STOP_HOTKEY_ID, Constants.MOD_NONE, AppSettings.StopHotkey.VirtualCode);
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(StartStopHooks);
            UnregisterHotKey(_windowHandle, Constants.START_HOTKEY_ID);
            UnregisterHotKey(_windowHandle, Constants.STOP_HOTKEY_ID);
            AppSettings.HotKeyChangedEvent -= AppSettings_HotKeyChanged;

            base.OnClosed(e);
        }

        #endregion Lifetime

        #region Commands

        #region Start Command

        private void StartCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            timesRepeated = 0;
            clickTimer.Interval = CalculateInterval();
            clickTimer.Start();
            Title += Constants.MAIN_WINDOW_TITLE_RUNNING;
        }

        private void StartCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CanStart();
        }

        #endregion Start Command

        #region Stop Command

        private void StopCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            clickTimer.Stop();
            ResetTitle();
        }

        private void StopCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = clickTimer.Enabled;
        }

        #endregion Stop Command

        #region HotkeySettings Command

        private void HotkeySettingsCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            if (settingsWindow == null)
            {
                settingsWindow = new SettingsWindow();
                settingsWindow.Closed += (o, args) => settingsWindow = null;
            }

            settingsWindow.Show();
        }

        #endregion HotkeySettings Command

        #region Exit Command

        private void ExitCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion Exit Command

        #region About Command

        private void AboutCommand_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            if (aboutWindow == null)
            {
                aboutWindow = new AboutWindow();
                aboutWindow.Closed += (o, args) => aboutWindow = null;
            }

            aboutWindow.Show();
        }

        #endregion About Command

        #endregion Commands

        #region External Methods

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        static extern bool SetCursorPosition(int x, int y);

        [DllImport("user32.dll", EntryPoint = "mouse_event")]
        static extern void ExecuteMouseEvent(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        [DllImport("user32.dll")]
        static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        #endregion External Methods

        #region Helper Methods

        private int CalculateInterval()
        {
            return Milliseconds + (Seconds * 1000) + (Minutes * 60 * 1000) + (Hours * 60 * 60 * 1000);
        }

        private bool IsIntervalValid()
        {
            return CalculateInterval() > 0;
        }

        private bool CanStart()
        {
            return !clickTimer.Enabled && IsRepeatModeValid() && IsIntervalValid();
        }

        private int GetTimesToRepeat()
        {
            return SelectedRepeatMode == RepeatMode.Count ? SelectedTimesToRepeat : -1;
        }

        private Point GetSelectedPosition()
        {
            return SelectedLocationMode == LocationMode.CurrentLocation ? MouseCursor.Position : new Point(PickedXValue, PickedYValue);
        }

        private int GetSelectedXPosition()
        {
            return GetSelectedPosition().X;
        }

        private int GetSelectedYPosition()
        {
            return GetSelectedPosition().Y;
        }

        private int GetNumberOfMouseActions()
        {
            return SelectedMouseAction == MouseAction.Single ? 1 : 2;
        }

        private bool IsRepeatModeValid()
        {
            return SelectedRepeatMode == RepeatMode.Infinite || (SelectedRepeatMode == RepeatMode.Count && SelectedTimesToRepeat > 0);
        }

        private void ResetTitle()
        {
            Title = Constants.MAIN_WINDOW_TITLE_DEFAULT;
        }

        #endregion Helper Methods

        #region Event Handlers

        private void OnClickTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                InitMouseClick();
                timesRepeated++;

                if (timesRepeated == GetTimesToRepeat())
                {
                    clickTimer.Stop();
                    ResetTitle();
                }
            });
        }

        private void InitMouseClick()
        {
            Dispatcher.Invoke(() =>
            {
                switch (SelectedMouseButton)
                {
                    case MouseButton.Left:
                        PerformMouseClick(Constants.MOUSEEVENTF_LEFTDOWN, Constants.MOUSEEVENTF_LEFTUP, GetSelectedXPosition(), GetSelectedYPosition());
                        break;
                    case MouseButton.Right:
                        PerformMouseClick(Constants.MOUSEEVENTF_RIGHTDOWN, Constants.MOUSEEVENTF_RIGHTUP, GetSelectedXPosition(), GetSelectedYPosition());
                        break;
                    case MouseButton.Middle:
                        PerformMouseClick(Constants.MOUSEEVENTF_MIDDLEDOWN, Constants.MOUSEEVENTF_MIDDLEUP, GetSelectedXPosition(), GetSelectedYPosition());
                        break;
                }
            });
        }

        private void PerformMouseClick(int mouseDownAction, int mouseUpAction, int xPos, int yPos)
        {
            for (int i = 0; i < GetNumberOfMouseActions(); ++i)
            {
                SetCursorPosition(xPos, yPos);
                ExecuteMouseEvent(mouseDownAction | mouseUpAction, xPos, yPos, 0, 0);
            }
        }

        private IntPtr StartStopHooks(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            int hotkeyId = wParam.ToInt32();
            if (msg == Constants.WM_HOTKEY && hotkeyId == Constants.START_HOTKEY_ID || hotkeyId == Constants.STOP_HOTKEY_ID)
            {
                int virtualKey = ((int)lParam >> 16) & 0xFFFF;
                if (virtualKey == AppSettings.StartHotkey.VirtualCode && CanStart())
                {
                    StartCommand_Execute(null, null);
                }
                if (virtualKey == AppSettings.StopHotkey.VirtualCode && clickTimer.Enabled)
                {
                    StopCommand_Execute(null, null);
                }
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void AppSettings_HotKeyChanged(object sender, HotkeyChangedEventArgs e)
        {
            switch (e.Hotkey.Operation)
            {
                case Operation.Start:
                    if (UnregisterHotKey(_windowHandle, Constants.START_HOTKEY_ID))
                    {
                        RegisterHotKey(_windowHandle, Constants.START_HOTKEY_ID, Constants.MOD_NONE, e.Hotkey.VirtualCode);
                        startButton.Content = $"{Constants.MAIN_WINDOW_START_BUTTON_CONTENT} ({e.Hotkey.Key})";
                        break;
                    }
                    throw new InvalidOperationException($"No hotkey registered on {Constants.START_HOTKEY_ID}");
                case Operation.Stop:
                    if (UnregisterHotKey(_windowHandle, Constants.STOP_HOTKEY_ID))
                    {
                        RegisterHotKey(_windowHandle, Constants.STOP_HOTKEY_ID, Constants.MOD_NONE, e.Hotkey.VirtualCode);
                        stopButton.Content = $"{Constants.MAIN_WINDOW_START_BUTTON_CONTENT} ({e.Hotkey.Key})";
                        break;
                    }
                    throw new InvalidOperationException($"No hotkey registered on {Constants.STOP_HOTKEY_ID}");
                default:
                    throw new NotSupportedException("Operation not supported!");
            }
        }

        #endregion Event Handlers
    }
}