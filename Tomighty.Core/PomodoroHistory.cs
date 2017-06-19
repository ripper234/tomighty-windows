using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tomighty
{
    public class PomodoroHistory : IPomodoroHistory
    {
        private string filePath;
        public PomodoroHistory(IApp app)
        {
            filePath = Path.Combine(app.GetOrCreateApplicationDirectory(), "PomodoroHistory.csv");
            Init();
        }

        public void Add(DateTime dateTime, IntervalType intervalType, PomodoroStatus status, string focus = "")
        {

            Init();
            using (var textStrem = File.AppendText(filePath))
                textStrem.WriteLine($"{dateTime.ToFileTimeUtc()}, {dateTime.ToShortDateString()}, {dateTime.ToUniversalTime().ToShortTimeString()}, {intervalType.ToString()}, {status.ToString()}, {focus}");
        }

        public void Export(string filePath)
        {
            try
            {
                Init();
                File.Copy(this.filePath, filePath, true);
            }
            catch { }
        }

        private void Init()
        {
            if (!File.Exists(filePath))
                using (var textStream = File.CreateText(filePath))
                    textStream.WriteLine("Time Stamp, Date, Time, Type, Status, Focus");
        }
    }
}
