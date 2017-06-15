using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tomighty
{
    public interface IPomodoroHistory
    {
        void Add(DateTime dateTime, IntervalType intervalType, PomodoroStatus status, string focus);

        void Export(string filePath);
    }
}
