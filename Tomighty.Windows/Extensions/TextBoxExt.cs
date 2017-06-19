using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tomighty.Windows.Extensions
{
    public static class TextBoxExt
    {
        private const uint EM_SETCUEBANNER = 0x1501;

        public static void CueBanner(this TextBox textBox, bool showcuewhenfocus, string cuetext)
        {
            uint BOOL = 0;
            if (showcuewhenfocus == true)
                BOOL = 1; 

            SendMessage(textBox.Handle, EM_SETCUEBANNER, (IntPtr)BOOL, cuetext); ;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);
    }
}
