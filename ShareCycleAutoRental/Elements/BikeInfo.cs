using AngleSharp.Html.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShareCycleAutoRental
{
    public class BikeInfo
    {
        public string BikeName { get; set; }
        public IHtmlFormElement Element { get; set; }
    }
}
