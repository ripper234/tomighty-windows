//
//  Tomighty - http://www.tomighty.org
//
//  This software is licensed under the Apache License Version 2.0:
//  http://www.apache.org/licenses/LICENSE-2.0.txt
//

using System;
using System.Windows.Forms;

namespace Tomighty.Windows.Preferences
{
    internal class UserPreferencesPresenter
    {
        private readonly IUserPreferences userPreferences;
        private UserPreferencesForm window;
        private IPomodoroHistory pomodoroHistory;
        public UserPreferencesPresenter(IUserPreferences userPreferences, IPomodoroHistory pomodoroHistory)
        {
            this.userPreferences = userPreferences;
            this.pomodoroHistory = pomodoroHistory;
        }

        public void Show()
        {
            if (window == null)
            {
                window = new UserPreferencesForm(userPreferences);
                window.FormClosed += OnWindowClosed;
                window.ExportButton.Click += OnExportButtonClick;
            }

            if (window.Visible)
            {
                window.Focus();
            }
            else
            {
                window.ShowDialog();
            }
        }

        private void OnExportButtonClick(object sender, EventArgs e)
        {
            var saveDialog = new SaveFileDialog() { FileName = "pomodoro", AddExtension = true, DefaultExt = "csv" };
            var result = saveDialog.ShowDialog();
            if (result == DialogResult.OK)
                pomodoroHistory.Export(saveDialog.FileName);
        }

        private void OnWindowClosed(object sender, FormClosedEventArgs e)
        {
            window.Dispose();
            window = null;
        }
    }
}
