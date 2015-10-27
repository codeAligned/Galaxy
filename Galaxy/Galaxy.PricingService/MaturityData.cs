using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Galaxy.PricingService
{
    public class MaturityData
    {
        public double ImpliedDiv { get; set; }
        public double ImpliedRate { get; set; }
        public double TimeToExpi { get; set; }
        public double BaseOffset { get; set; }
    }
}
