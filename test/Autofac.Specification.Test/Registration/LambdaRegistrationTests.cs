﻿using System;
using Xunit;

namespace Autofac.Specification.Test.Registration
{
    public class LambdaRegistrationTests
    {
        private interface IA
        {
        }

        private interface IB
        {
        }

        [Fact]
        public void RegisterLambdaAsImplementedInterfaces()
        {
            var builder = new ContainerBuilder();
            builder.Register(c => new A()).AsImplementedInterfaces();
            var context = builder.Build();

            context.Resolve<IA>();
            context.Resolve<IB>();
        }

        [Fact]
        public void RegisterLambdaAsUnsupportedService()
        {
            var builder = new ContainerBuilder();
            builder.Register(c => "hello").As<IA>();
            Assert.Throws<ArgumentException>(() => builder.Build());
        }

        private class A : IA, IB
        {
        }
    }
}
