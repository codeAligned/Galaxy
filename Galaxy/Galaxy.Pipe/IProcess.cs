using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipe
{
    public interface IProcess
    {
        void Start();
        bool IsRunning { get; set; }
    }
}
