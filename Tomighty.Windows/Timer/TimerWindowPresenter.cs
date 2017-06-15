//
//  Tomighty - http://www.tomighty.org
//
//  This software is licensed under the Apache License Version 2.0:
//  http://www.apache.org/licenses/LICENSE-2.0.txt
//

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Tomighty.Events;

namespace Tomighty.Windows.Timer
{
    internal class TimerWindowPresenter
    {
        private readonly ICountdownClock countdownClock;
        private readonly IWindowState idleState;
        private readonly IWindowState pomodoroState;
        private readonly IWindowState shortBreakState;
        private readonly IWindowState longBreakState;
        private readonly IWindowState pomodoroCompletedState;
        private readonly IWindowState breakFinishedState;
        private readonly IWindowState pomodoroInterruptedState;
        private readonly IWindowState breakInterruptedState;

        private IWindowState currentState;
        private TimerWindow window;
        private Taskbar _taskbar;
        private bool _isPinned;
        private AutoCompleteModel autoCompleteModel;
        private IPomodoroHistory pomodoroHistory;
        private IPomodoroEngine pomodoroEngine;

        public TimerWindowPresenter(IApp app, IPomodoroEngine pomodoroEngine, ICountdownClock countdownClock, IPomodoroHistory pomodoroHistory, IEventHub eventHub)
        {
            this.countdownClock = countdownClock;
            this.pomodoroHistory = pomodoroHistory;
            this.pomodoroEngine = pomodoroEngine;

            autoCompleteModel = new AutoCompleteModel(app, "focusList.json", eventHub);
            idleState = new IdleState(pomodoroEngine);
            pomodoroState = new PomodoroState(pomodoroEngine, autoCompleteModel);
            shortBreakState = new ShortBreakState(pomodoroEngine);
            longBreakState = new LongBreakState(pomodoroEngine);
            pomodoroCompletedState = new PomodoroCompletedState(pomodoroEngine);
            breakFinishedState = new BreakFinishedState(pomodoroEngine);
            pomodoroInterruptedState = new TimerInterruptedState("Pomodoro Interrupted", pomodoroEngine);
            breakInterruptedState = new TimerInterruptedState("Break Interrupted", pomodoroEngine);

            currentState = idleState;

            eventHub.Subscribe<TimerStarted>(OnTimerStarted);
            eventHub.Subscribe<TimeElapsed>(OnTimeElapsed);
            eventHub.Subscribe<TimerStopped>(OnTimerStopped);
        }

        private TimerWindow CreateTimerWindow()
        {
            var window = new TimerWindow();

            foreach (var control in window.Controls)
                ((Control)control).LostFocus += OnLostFocus;

            window.LostFocus += OnLostFocus;
            window.VisibleChanged += OnWindowVisibleChanged;
            window.PinButton.Click += OnPinButtonClick;
            window.CloseButton.Click += OnCloseButtonClick;
            window.FocusTextBox.PreviewKeyDown += OnFocusTextBoxKeyDown;
            window.SuggestListBox.SelectedIndexChanged += OnSuggestListBoxSelectionIndexChanged;
            window.SuggestListBox.PreviewKeyDown += OnSuggestListBoxSelectionKeyDown;

            new DragAroundController(window, () => IsPinned);

            return window;
        }

        public void Toggle(Point approximateLocation)
        {
            var shouldCreateWindow = window == null;

            if (shouldCreateWindow)
            {
                window = CreateTimerWindow();
            }

            if (window.Visible)
            {
                if (!IsPinned)
                    window.Hide();
            }
            else
            {
                Point location = GetLocationNearTrayIcon(approximateLocation);
                window.Show(location);

                if (shouldCreateWindow)
                {
                    currentState.Apply(window, countdownClock.RemainingTime);
                }
            }
        }

        private void OnTimerStarted(TimerStarted @event)
        {
            EnterState(GetRunningTimerStateFor(@event.IntervalType), @event.RemainingTime);
        }

        private void OnTimerStopped(TimerStopped @event)
        {
            PomodoroStatus status = PomodoroStatus.Done;
            if (@event.IsIntervalCompleted)
            {
                EnterState(GetCompletedIntervalStateFor(@event.IntervalType), @event.RemainingTime);
            }
            else
            {
                EnterState(GetTimerInterruptedStateFor(@event.IntervalType), @event.RemainingTime);
                status = PomodoroStatus.Interrupted;
            }

            var focus = string.Empty;
            if (@event.IntervalType == IntervalType.Pomodoro && pomodoroEngine.IsFocusEnabled)
                focus = window.FocusTextBox.Text;

            pomodoroHistory.Add(DateTime.Now, @event.IntervalType, status, focus);
        }

        private void OnTimeElapsed(TimeElapsed @event)
        {
            if (window != null)
            {
                window.UpdateTimeDisplay(@event.RemainingTime.ToTimeString());
            }
        }

        private void EnterState(IWindowState newState, Duration remainingTime)
        {
            if (newState != currentState)
            {
                currentState = newState;
                currentState.Apply(window, remainingTime);
            }
        }

        private IWindowState GetRunningTimerStateFor(IntervalType intervalType)
        {
            if (intervalType == IntervalType.Pomodoro) return pomodoroState;
            if (intervalType == IntervalType.ShortBreak) return shortBreakState;
            if (intervalType == IntervalType.LongBreak) return longBreakState;
            throw new ArgumentException($"Unknown interval type: {intervalType}");
        }

        private IWindowState GetCompletedIntervalStateFor(IntervalType intervalType)
        {
            if (intervalType == IntervalType.Pomodoro) return pomodoroCompletedState;
            if (intervalType == IntervalType.ShortBreak || intervalType == IntervalType.LongBreak) return breakFinishedState;
            throw new ArgumentException($"Unknown interval type: {intervalType}");
        }

        private IWindowState GetTimerInterruptedStateFor(IntervalType intervalType)
        {
            if (intervalType == IntervalType.Pomodoro) return pomodoroInterruptedState;
            if (intervalType == IntervalType.ShortBreak || intervalType == IntervalType.LongBreak) return breakInterruptedState;
            throw new ArgumentException($"Unknown interval type: {intervalType}");
        }

        private Point GetLocationNearTrayIcon(Point approximateLocation)
        {
            var screen = Screen.FromPoint(approximateLocation);
            var x = approximateLocation.X - window.Width / 2;
            var y = screen.Bounds.Height - window.Height - Taskbar.Size.Height - 2;
            return new Point(x, y);
        }

        private void OnPinButtonClick(object sender, System.EventArgs e)
        {
            IsPinned = !IsPinned;
        }

        private void OnCloseButtonClick(object sender, System.EventArgs e)
        {
            window.Hide();
            IsPinned = false;
        }

        private void OnWindowVisibleChanged(object sender, System.EventArgs e)
        {
            if (window.Visible)
            {
                window.Focus();
            }
        }

        private void OnLostFocus(object sender, System.EventArgs e)
        {
            if (!IsPinned && !window.ContainsFocus)
                window.Hide();
        }

        private void OnFocusTextBoxKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
            {
                autoCompleteModel.AddFocusWord(window.FocusTextBox.Text);
                window.UnfocusFocusTextBox();
            }

            if (e.KeyCode == Keys.Space && e.Control && string.IsNullOrEmpty(window.FocusTextBox.Text))
            {
                var source = autoCompleteModel.GetSource();
                var count = 0;
                var list = new List<string>();
                foreach (var item in source)
                {
                    list.Add(item.ToString());
                    ++count;
                    if (count >= 10)
                        break;
                }

                window.SuggestListBox.Items.Clear();
                window.SuggestListBox.Items.AddRange(list.ToArray());
                window.SuggestListBox.Visible = true;
                window.SuggestListBox.Focus();
            }
            else 
                window.SuggestListBox.Visible = false;
        }

        private void OnSuggestListBoxSelectionIndexChanged(object sender, EventArgs e)
        {
            window.FocusTextBox.Text = window.SuggestListBox.SelectedItem.ToString();
        }

        private void OnSuggestListBoxSelectionKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
            {
                window.SuggestListBox.Visible = false;
                window.FocusTextBox.Focus();
            }
        }

        private Taskbar Taskbar => _taskbar == null ? _taskbar = new Taskbar() : _taskbar;

        private bool IsPinned
        {
            get { return _isPinned; }
            set
            {
                if (value != _isPinned)
                {
                    _isPinned = value;

                    if (window != null)
                    {
                        window.UpdatePinButtonState(_isPinned);
                    }
                }
            }
        }

        private interface IWindowState
        {
            void Apply(TimerWindow window, Duration remainingTime);
        }

        private class IdleState : IWindowState
        {
            private readonly IPomodoroEngine pomodoroEngine;

            public IdleState(IPomodoroEngine pomodoroEngine)
            {
                this.pomodoroEngine = pomodoroEngine;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle("Idle");
                window.UpdateColorScheme(TimerWindow.DarkGrayColorScheme);
                window.UpdateTimeDisplay(Duration.Zero.ToTimeString());
                window.SetTimerAction("Start Pomodoro", StartTimer);
                window.SetIsFocusVisible(false);
            }

            private void StartTimer()
            {
                pomodoroEngine.StartTimer(IntervalType.Pomodoro);
            }
        }

        private class PomodoroState : IWindowState
        {
            private readonly IPomodoroEngine pomodoroEngine;
            private readonly AutoCompleteModel autoCompleteModel;

            public PomodoroState(IPomodoroEngine pomodoroEngine, AutoCompleteModel autoCompleteModel)
            {
                this.pomodoroEngine = pomodoroEngine;
                this.autoCompleteModel = autoCompleteModel;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle("Pomodoro");
                window.UpdateColorScheme(TimerWindow.RedColorScheme);
                window.UpdateTimeDisplay(remainingTime.ToTimeString());
                window.SetTimerAction("Interrupt", pomodoroEngine.StopTimer);
                if (pomodoroEngine.IsFocusEnabled)
                {
                    window.FocusTextBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    window.FocusTextBox.AutoCompleteCustomSource = autoCompleteModel.GetSource();
                    window.FocusTextBox.AutoCompleteSource = AutoCompleteSource.CustomSource;
                    window.SetIsFocusVisible(true);
                    window.FocusTextBox.Focus();
                }
            }
        }

        private class ShortBreakState : IWindowState
        {
            private readonly IPomodoroEngine pomodoroEngine;

            public ShortBreakState(IPomodoroEngine pomodoroEngine)
            {
                this.pomodoroEngine = pomodoroEngine;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle("Short Break");
                window.UpdateColorScheme(TimerWindow.GreenColorScheme);
                window.UpdateTimeDisplay(remainingTime.ToTimeString());
                window.SetTimerAction("Interrupt", pomodoroEngine.StopTimer);
                window.SetIsFocusVisible(false);
            }
        }

        private class LongBreakState : IWindowState
        {
            private readonly IPomodoroEngine pomodoroEngine;

            public LongBreakState(IPomodoroEngine pomodoroEngine)
            {
                this.pomodoroEngine = pomodoroEngine;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle("Long Break");
                window.UpdateColorScheme(TimerWindow.BlueColorScheme);
                window.UpdateTimeDisplay(remainingTime.ToTimeString());
                window.SetTimerAction("Interrupt", pomodoroEngine.StopTimer);
                window.SetIsFocusVisible(false);
            }
        }

        private class PomodoroCompletedState : IWindowState
        {
            private readonly IPomodoroEngine pomodoroEngine;

            public PomodoroCompletedState(IPomodoroEngine pomodoroEngine)
            {
                this.pomodoroEngine = pomodoroEngine;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle("Pomodoro Completed");
                window.UpdateColorScheme(TimerWindow.DarkGrayColorScheme);
                window.UpdateTimeDisplay(Duration.Zero.ToTimeString());
                window.SetTimerAction($"Start {pomodoroEngine.SuggestedBreakType.GetName()}", StartTimer);
                window.SetIsFocusVisible(false);
            }

            private void StartTimer()
            {
                pomodoroEngine.StartTimer(pomodoroEngine.SuggestedBreakType);
            }
        }

        private class BreakFinishedState : IWindowState
        {
            private readonly IPomodoroEngine pomodoroEngine;

            public BreakFinishedState(IPomodoroEngine pomodoroEngine)
            {
                this.pomodoroEngine = pomodoroEngine;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle("Break Finished");
                window.UpdateColorScheme(TimerWindow.DarkGrayColorScheme);
                window.UpdateTimeDisplay(Duration.Zero.ToTimeString());
                window.SetTimerAction("Start Pomodoro", StartTimer);
                window.SetIsFocusVisible(false);
            }

            private void StartTimer()
            {
                pomodoroEngine.StartTimer(IntervalType.Pomodoro);
            }
        }

        private class TimerInterruptedState : IWindowState
        {
            private readonly string title;
            private readonly IPomodoroEngine pomodoroEngine;

            public TimerInterruptedState(string title, IPomodoroEngine pomodoroEngine)
            {
                this.title = title;
                this.pomodoroEngine = pomodoroEngine;
            }

            public void Apply(TimerWindow window, Duration remainingTime)
            {
                if (window == null)
                    return;

                window.UpdateTitle(title);
                window.UpdateColorScheme(TimerWindow.DarkGrayColorScheme);
                window.UpdateTimeDisplay(remainingTime.ToTimeString());
                window.SetTimerAction("Start Pomodoro", StartTimer);
                window.SetIsFocusVisible(false);
            }

            private void StartTimer()
            {
                pomodoroEngine.StartTimer(IntervalType.Pomodoro);
            }
        }

        private class DragAroundController
        {
            private readonly Form form;
            private readonly Func<bool> canStartDraggingAround;
            private bool dragAround;
            private int offsetX, offsetY;

            public DragAroundController(Form form, Func<bool> canStartDraggingAround)
            {
                this.form = form;
                this.canStartDraggingAround = canStartDraggingAround;

                form.MouseDown += OnMouseDown;
                form.MouseUp += OnMouseUp;
                form.MouseMove += OnMouseMove;
            }

            private void OnMouseDown(object sender, MouseEventArgs e)
            {
                if (canStartDraggingAround())
                {
                    dragAround = true;
                    offsetX = e.Location.X;
                    offsetY = e.Location.Y;
                }
            }

            private void OnMouseUp(object sender, MouseEventArgs e)
            {
                dragAround = false;
            }

            private void OnMouseMove(object sender, MouseEventArgs e)
            {
                if (dragAround)
                {
                    var relativeX = e.Location.X;
                    var relativeY = e.Location.Y;

                    var deltaX = relativeX - offsetX;
                    var deltaY = relativeY - offsetY;

                    form.Location = new Point(
                        form.Location.X + deltaX,
                        form.Location.Y + deltaY
                    );
                }
            }
        }
    }
}
