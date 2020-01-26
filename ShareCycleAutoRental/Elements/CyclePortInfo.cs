using AngleSharp.Html.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShareCycleAutoRental
{
    public class CyclePortInfo
    {
        public string PortName { get; set; }
        public string PortNameEnglish { get; set; }
        public string PortQuantity { get; set; }
        public IHtmlFormElement Element { get; set; }
    }
}
