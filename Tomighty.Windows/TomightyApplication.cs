//
//  Tomighty - http://www.tomighty.org
//
//  This software is licensed under the Apache License Version 2.0:
//  http://www.apache.org/licenses/LICENSE-2.0.txt
//

using System.ComponentModel;
using System.Windows.Forms;
using Tomighty.Events;
using Tomighty.Windows.About;
using Tomighty.Windows.Notifications;
using Tomighty.Windows.Preferences;
using Tomighty.Windows.Timer;
using Tomighty.Windows.Tray;

namespace Tomighty.Windows
{
    internal class TomightyApplication : ApplicationContext
    {
        public TomightyApplication()
        {
            var eventHub = new SynchronousEventHub();
            var timer = new Tomighty.Timer(eventHub);
            var app = new App();
            var userPreferences = new UserPreferences(app);
            var pomodoroEngine = new PomodoroEngine(timer, userPreferences, eventHub);
            var pomodoroHistory = new PomodoroHistory(app);

            var trayMenu = new TrayMenu() as ITrayMenu;
            var trayIcon = CreateTrayIcon(trayMenu);
            var timerWindowPresenter = new TimerWindowPresenter(app, pomodoroEngine, timer, pomodoroHistory, eventHub);

            new TrayIconController(trayIcon, timerWindowPresenter, eventHub);
            new TrayMenuController(trayMenu, this, pomodoroEngine, eventHub);
            new NotificationsPresenter(pomodoroEngine, eventHub);

            var aboutWindowPresenter = new AboutWindowPresenter();
            var userPreferencesPresenter = new UserPreferencesPresenter(userPreferences, pomodoroHistory);

            trayMenu.OnAboutClick((sender, e) => aboutWindowPresenter.Show());
            trayMenu.OnPreferencesClick((sender, e) => userPreferencesPresenter.Show());

            Application.ApplicationExit += (sender, e) => eventHub.Publish(new AppExit());
            ThreadExit += (sender, e) => trayIcon.Dispose();

            if (userPreferences.IsFocusEnabled)
            {
                pomodoroEngine.StartTimer(IntervalType.Pomodoro);
                timerWindowPresenter.Show();
            }
        }

        private static NotifyIcon CreateTrayIcon(ITrayMenu trayMenu)
        {
            var trayIcon = new NotifyIcon(new Container());

            trayIcon.Text = "Tomighty";
            trayIcon.Icon = Properties.Resources.icon_tomato_white;
            trayIcon.ContextMenuStrip = trayMenu.Component;
            trayIcon.Visible = true;

            return trayIcon;
        }
    }
}
