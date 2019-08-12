using System;
using System.Linq;
using Autofac.Specification.Test.Util;

namespace Autofac.Specification.Test.Resolution.Graph1
{
    // In the below scenario, B1 depends on A1, CD depends on A1 and B1,
    // and E depends on IC1 and B1.
    public class B1 : DisposeTracker
    {
        public B1(A1 a)
        {
            this.A = a;
        }

        public A1 A { get; private set; }
    }
}