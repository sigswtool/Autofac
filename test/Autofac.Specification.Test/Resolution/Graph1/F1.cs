using System;
using System.Collections.Generic;
using System.Linq;

namespace Autofac.Specification.Test.Resolution.Graph1
{
    // In the below scenario, B1 depends on A1, CD depends on A1 and B1,
    // and E depends on IC1 and B1.
    public class F1
    {
        public F1(IList<A1> aList)
        {
            this.AList = aList;
        }

        public IList<A1> AList { get; private set; }
    }
}
