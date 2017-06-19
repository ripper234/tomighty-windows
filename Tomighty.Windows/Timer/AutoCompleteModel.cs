using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tomighty.Events;

namespace Tomighty.Windows.Timer
{
    public class AutoCompleteModel
    {
        private string filePath = null;
        private Dictionary<string, AutoCompletePoco> keywords = new Dictionary<string, AutoCompletePoco>();

        public AutoCompleteModel(IApp app, string fileName, IEventHub eventHub)
        {
            filePath = Path.Combine(app.GetOrCreateApplicationDirectory(), fileName);
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                foreach (var item in JsonConvert.DeserializeObject<List<AutoCompletePoco>>(json))
                    keywords[item.Focus] = item;
            }

            eventHub.Subscribe<AppExit>(OnAppExit);
        }

        public AutoCompleteStringCollection GetSource()
        {
            var source = new AutoCompleteStringCollection();
            source.AddRange(keywords.OrderByDescending(kvp => kvp.Value.Executed).Select(kvp => kvp.Value.Focus).ToArray());
            return source;
        }

        /// <summary>
        /// Adds only focus words longer than 3 chars
        /// </summary>
        /// <param name="focus">word to add</param>
        public void AddFocusWord(string focus)
        {
            if (!string.IsNullOrEmpty(focus) && focus.Length > 3)
            {
                var now = DateTime.Now;
                if (keywords.ContainsKey(focus))
                    keywords[focus].Executed = now;
                else
                    keywords[focus] = new AutoCompletePoco() { Focus = focus, Executed = now };
            }
        }

        private void OnAppExit(AppExit @event)
        {
            var obj = keywords.Select(kvp => kvp.Value).ToList();
            var json = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }

    public class AutoCompletePoco
    {
        public string Focus { get; set; }
        public DateTime Executed { get; set; }
    }
}
