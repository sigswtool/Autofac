﻿using System;
using Autofac.Specification.Test.Util;
using Xunit;

namespace Autofac.Specification.Test.Lifetime
{
    public class InstancePerLifetimeScopeTests
    {
        [Fact]
        public void TypeAsInstancePerScope()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<A>().InstancePerLifetimeScope();

            var container = builder.Build();

            var lifetime = container.BeginLifetimeScope();

            var ctxA = lifetime.Resolve<A>();
            var ctxA2 = lifetime.Resolve<A>();

            Assert.Same(ctxA, ctxA2);

            var targetA = container.Resolve<A>();
            var targetA2 = container.Resolve<A>();

            Assert.Same(targetA, targetA2);
            Assert.NotSame(ctxA, targetA);
        }

        [Fact]
        public void TypeAsInstancePerScopeDisposedWithScope()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<A>().InstancePerLifetimeScope();

            var container = builder.Build();

            var lifetime = container.BeginLifetimeScope();

            var ctxA = lifetime.Resolve<A>();
            var targetA = container.Resolve<A>();

            Assert.False(targetA.IsDisposed);
            Assert.False(ctxA.IsDisposed);

            lifetime.Dispose();

            Assert.False(targetA.IsDisposed);
            Assert.True(ctxA.IsDisposed);

            container.Dispose();

            Assert.True(targetA.IsDisposed);
            Assert.True(ctxA.IsDisposed);
        }

        private class A : DisposeTracker
        {
        }
    }
}
