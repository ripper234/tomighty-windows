using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tomighty
{
    public interface IApp
    {
        string GetOrCreateApplicationDirectory();
    }
}
