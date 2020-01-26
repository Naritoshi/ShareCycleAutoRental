using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShareCycleAutoRental
{
    public class Result<T>
    {
        public bool HasError { get; set; } = false;

        public string Message { get; set; }

        public T ResultObject { get; set; }
    }
}
