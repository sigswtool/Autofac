﻿using System;
using System.Linq;

namespace Autofac.Specification.Test.Features.CircularDependency
{
    public class DependsByCtor
    {
        public DependsByCtor(DependsByProp o)
        {
            this.Dep = o;
        }

        public DependsByProp Dep { get; private set; }
    }
}
