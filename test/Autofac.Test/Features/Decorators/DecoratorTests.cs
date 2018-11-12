﻿using System;
using System.Collections.Generic;
using System.Linq;
using Autofac.Core;
using Autofac.Features.Decorators;
using Xunit;

namespace Autofac.Test.Features.Decorators
{
    public class DecoratorTests
    {
        public interface IService
        {
        }

        public interface IDecoratedService : IService
        {
            IDecoratedService Decorated { get; }
        }

        public class ImplementorA : IDecoratedService
        {
            public IDecoratedService Decorated => this;
        }

        public class ImplementorB : IDecoratedService
        {
            public IDecoratedService Decorated => this;
        }

        public class ImplementorWithParameters : IDecoratedService
        {
            public IDecoratedService Decorated => this;

            public string Parameter { get; }

            public ImplementorWithParameters(string parameter)
            {
                Parameter = parameter;
            }
        }

        public abstract class Decorator : IDecoratedService
        {
            protected Decorator(IDecoratedService decorated)
            {
                Decorated = decorated;
            }

            public IDecoratedService Decorated { get; }
        }

        public class DecoratorA : Decorator
        {
            public DecoratorA(IDecoratedService decorated)
                : base(decorated)
            {
            }
        }

        public class DecoratorB : Decorator
        {
            public DecoratorB(IDecoratedService decorated)
                : base(decorated)
            {
            }
        }

        public interface IDecoratorWithParameter
        {
            string Parameter { get; }
        }

        public class DecoratorWithParameter : Decorator, IDecoratorWithParameter
        {
            public DecoratorWithParameter(IDecoratedService decorated, string parameter)
                : base(decorated)
            {
                Parameter = parameter;
            }

            public string Parameter { get; }
        }

        public interface IDecoratorWithContext
        {
            IDecoratorContext Context { get; }
        }

        public class DecoratorWithContextA : Decorator, IDecoratorWithContext
        {
            public DecoratorWithContextA(IDecoratedService decorated, IDecoratorContext context)
                : base(decorated)
            {
                Context = context;
            }

            public IDecoratorContext Context { get; }
        }

        public class DecoratorWithContextB : Decorator, IDecoratorWithContext
        {
            public DecoratorWithContextB(IDecoratedService decorated, IDecoratorContext context)
                : base(decorated)
            {
                Context = context;
            }

            public IDecoratorContext Context { get; }
        }

        public class StartableImplementation : IDecoratedService, IStartable
        {
            public IDecoratedService Decorated => this;

            public bool Started { get; private set; }

            public void Start()
            {
                this.Started = true;
            }
        }

        public class DisposableImplementor : IDecoratedService, IDisposable
        {
            public int DisposeCallCount { get; private set; }

            public IDecoratedService Decorated => this;

            public void Dispose()
            {
                DisposeCallCount++;
            }
        }

        public class DisposableDecorator : Decorator, IDisposable
        {
            public int DisposeCallCount { get; private set; }

            public DisposableDecorator(IDecoratedService decorated)
                : base(decorated)
            {
            }

            public void Dispose()
            {
                DisposeCallCount++;
            }
        }

        [Fact]
        public void RegistrationIncludesTheServiceType()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            var container = builder.Build();

            var registration = container.RegistrationFor<IDecoratedService>();
            Assert.NotNull(registration);

            var decoratedService = new TypedService(typeof(IDecoratedService));
            Assert.Contains(registration.Services.OfType<TypedService>(), s => s == decoratedService);
        }

        [Fact]
        public void RegistrationTargetsTheImplementationType()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            var container = builder.Build();

            var registration = container.RegistrationFor<IDecoratedService>();

            Assert.NotNull(registration);
            Assert.Equal(typeof(ImplementorA), registration.Target.Activator.LimitType);
        }

        [Fact(Skip ="Cannot currently determine requested resolve service type")]
        public void DecoratedRegistrationCanIncludeImplementationType()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>().AsSelf();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            var container = builder.Build();

            Assert.IsType<ImplementorA>(container.Resolve<ImplementorA>());
        }

        [Fact]
        public void DecoratedRegistrationCanIncludeOtherServices()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>().As<IService>();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            var container = builder.Build();

            var serviceRegistration = container.RegistrationFor<IService>();
            var decoratedServiceRegistration = container.RegistrationFor<IDecoratedService>();

            Assert.NotNull(serviceRegistration);
            Assert.NotNull(decoratedServiceRegistration);
            Assert.Same(serviceRegistration, decoratedServiceRegistration);

            Assert.IsType<DecoratorA>(container.Resolve<IService>());
            Assert.IsType<DecoratorA>(container.Resolve<IDecoratedService>());
        }

        [Fact]
        public void ResolvesDecoratedServiceWhenNoDecoratorsRegistered()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            var container = builder.Build();

            var instance = container.Resolve<IDecoratedService>();

            Assert.IsType<ImplementorA>(instance);
        }

        [Fact]
        public void ResolvesDecoratedServiceWhenSingleDecoratorRegistered()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            var container = builder.Build();

            var instance = container.Resolve<IDecoratedService>();

            Assert.IsType<DecoratorA>(instance);
            Assert.IsType<ImplementorA>(instance.Decorated);
        }

        [Fact]
        public void DecoratorRegisteredAsLambdaCanBeResolved()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterDecorator<IDecoratedService>((c, p, i) => new DecoratorA(i));
            var container = builder.Build();

            var instance = container.Resolve<IDecoratedService>();

            Assert.IsType<DecoratorA>(instance);
            Assert.IsType<ImplementorA>(instance.Decorated);
        }

        [Fact]
        public void DecoratorRegisteredAsLambdaCanAcceptAdditionalParameters()
        {
            const string parameterName = "parameter";
            const string parameterValue = "ABC";

            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterDecorator<IDecoratedService>((c, p, i) =>
            {
                var stringParameter = (string)p
                    .OfType<NamedParameter>()
                    .FirstOrDefault(np => np.Name == parameterName)?.Value;

                return new DecoratorWithParameter(i, stringParameter);
            });
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            var container = builder.Build();

            var parameter = new NamedParameter(parameterName, parameterValue);
            var instance = container.Resolve<IDecoratedService>(parameter);

            Assert.Equal(parameterValue, ((DecoratorWithParameter)instance.Decorated).Parameter);
        }

        [Fact]
        public void ResolvesDecoratedServiceWhenMultipleDecoratorRegistered()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            builder.RegisterDecorator<DecoratorB, IDecoratedService>();
            var container = builder.Build();

            var instance = container.Resolve<IDecoratedService>();

            Assert.IsType<DecoratorB>(instance);
            Assert.IsType<DecoratorA>(instance.Decorated);
            Assert.IsType<ImplementorA>(instance.Decorated.Decorated);
        }

        [Fact]
        public void CanResolveMultipleDecoratedServices()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterType<ImplementorB>().As<IDecoratedService>();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            var container = builder.Build();

            var services = container.Resolve<IEnumerable<IDecoratedService>>();

            Assert.Collection(
                services,
                s =>
                {
                    Assert.IsType<DecoratorA>(s);
                    Assert.IsType<ImplementorA>(s.Decorated);
                },
                s =>
                {
                    Assert.IsType<DecoratorA>(s);
                    Assert.IsType<ImplementorB>(s.Decorated);
                });
        }

        [Fact]
        public void CanResolveDecoratorWithFunc()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            var container = builder.Build();

            var factory = container.Resolve<Func<IDecoratedService>>();

            Assert.IsType<DecoratorA>(factory());
        }

        [Fact]
        public void CanResolveDecoratorWithLazy()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            var container = builder.Build();

            var lazy = container.Resolve<Lazy<IDecoratedService>>();

            Assert.IsType<DecoratorA>(lazy.Value);
        }

        [Fact]
        public void ResolvesDecoratedServiceWhenRegisteredWithoutGenericConstraint()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterDecorator(typeof(DecoratorA), typeof(IDecoratedService));
            builder.RegisterDecorator(typeof(DecoratorB), typeof(IDecoratedService));
            var container = builder.Build();

            var instance = container.Resolve<IDecoratedService>();

            Assert.IsType<DecoratorB>(instance);
            Assert.IsType<DecoratorA>(instance.Decorated);
            Assert.IsType<ImplementorA>(instance.Decorated.Decorated);
        }

        [Fact]
        public void DecoratorRegistrationsGetAppliedInOrderAdded()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            builder.RegisterDecorator<DecoratorB, IDecoratedService>();
            var container = builder.Build();

            var instance = container.Resolve<IDecoratedService>();

            Assert.IsType<DecoratorB>(instance);
            Assert.IsType<DecoratorA>(instance.Decorated);
            Assert.IsType<ImplementorA>(instance.Decorated.Decorated);

            builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterDecorator<DecoratorB, IDecoratedService>();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            container = builder.Build();

            instance = container.Resolve<IDecoratedService>();

            Assert.IsType<DecoratorA>(instance);
            Assert.IsType<DecoratorB>(instance.Decorated);
            Assert.IsType<ImplementorA>(instance.Decorated.Decorated);
        }

        [Fact]
        public void CanApplyDecoratorConditionallyAtRuntime()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>(context => context.AppliedDecorators.Any());
            builder.RegisterDecorator<DecoratorB, IDecoratedService>();
            var container = builder.Build();

            var instance = container.Resolve<IDecoratedService>();

            Assert.IsType<DecoratorB>(instance);
            Assert.IsType<ImplementorA>(instance.Decorated);
        }

        [Fact]
        public void CanInjectDecoratorContextAsSnapshot()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            builder.RegisterDecorator<DecoratorB, IDecoratedService>();
            builder.RegisterDecorator<DecoratorWithContextA, IDecoratedService>();
            builder.RegisterDecorator<DecoratorWithContextB, IDecoratedService>();
            var container = builder.Build();

            var instance = container.Resolve<IDecoratedService>();

            var contextB = ((IDecoratorWithContext)instance).Context;
            Assert.Equal(typeof(IDecoratedService), contextB.ServiceType);
            Assert.Equal(typeof(ImplementorA), contextB.ImplementationType);
            Assert.IsType<DecoratorWithContextA>(contextB.CurrentInstance);
            Assert.Collection(
                contextB.AppliedDecorators,
                item => Assert.IsType<DecoratorA>(item),
                item => Assert.IsType<DecoratorB>(item),
                item => Assert.IsType<DecoratorWithContextA>(item));
            Assert.Collection(
                contextB.AppliedDecoratorTypes,
                item => Assert.Equal(typeof(DecoratorA), item),
                item => Assert.Equal(typeof(DecoratorB), item),
                item => Assert.Equal(typeof(DecoratorWithContextA), item));

            var contextA = ((IDecoratorWithContext)instance.Decorated).Context;
            Assert.Equal(typeof(IDecoratedService), contextA.ServiceType);
            Assert.Equal(typeof(ImplementorA), contextA.ImplementationType);
            Assert.IsType<DecoratorB>(contextA.CurrentInstance);
            Assert.Collection(
                contextA.AppliedDecorators,
                item => Assert.IsType<DecoratorA>(item),
                item => Assert.IsType<DecoratorB>(item));
            Assert.Collection(
                contextA.AppliedDecoratorTypes,
                item => Assert.Equal(typeof(DecoratorA), item),
                item => Assert.Equal(typeof(DecoratorB), item));
        }

        [Fact]
        public void DecoratorInheritsDecoratedLifetimeWhenSingleInstance()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>().SingleInstance();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();

            var container = builder.Build();

            var instance = container.Resolve<IDecoratedService>();
            Assert.Same(instance, container.Resolve<IDecoratedService>());

            using (var scope = container.BeginLifetimeScope())
            {
                Assert.Same(instance, scope.Resolve<IDecoratedService>());
            }
        }

        [Fact]
        public void DecoratorInheritsDecoratedLifetimeWhenInstancePerDependency()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>().InstancePerDependency();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();

            var container = builder.Build();

            var first = container.Resolve<IDecoratedService>();
            var second = container.Resolve<IDecoratedService>();
            Assert.NotSame(first, second);

            using (var scope = container.BeginLifetimeScope())
            {
                var third = scope.Resolve<IDecoratedService>();
                Assert.NotSame(first, third);
                Assert.NotSame(second, third);
            }
        }

        [Fact]
        public void DecoratorInheritsDecoratedLifetimeWhenInstancePerLifetimeScope()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>().InstancePerLifetimeScope();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();

            var container = builder.Build();

            var first = container.Resolve<IDecoratedService>();
            var second = container.Resolve<IDecoratedService>();
            Assert.Same(first, second);

            using (var scope = container.BeginLifetimeScope())
            {
                var third = scope.Resolve<IDecoratedService>();
                Assert.NotSame(first, third);

                var forth = scope.Resolve<IDecoratedService>();
                Assert.Same(third, forth);
            }
        }

        [Fact]
        public void DecoratorInheritsDecoratedLifetimeWhenInstancePerMatchingLifetimeScope()
        {
            const string tag = "foo";

            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>()
                .As<IDecoratedService>()
                .InstancePerMatchingLifetimeScope(tag);
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();

            var container = builder.Build();

            using (var scope = container.BeginLifetimeScope(tag))
            {
                var first = scope.Resolve<IDecoratedService>();
                var second = scope.Resolve<IDecoratedService>();
                Assert.Same(first, second);

                using (var scope2 = scope.BeginLifetimeScope())
                {
                    var third = scope2.Resolve<IDecoratedService>();
                    Assert.Same(second, third);
                }
            }
        }

        [Fact]
        public void ParametersArePassedThroughDecoratorChain()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            builder.RegisterDecorator<DecoratorWithParameter, IDecoratedService>();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            var container = builder.Build();

            var parameter = new NamedParameter("parameter", "ABC");
            var instance = container.Resolve<IDecoratedService>(parameter);

            Assert.Equal("ABC", ((DecoratorWithParameter)instance.Decorated).Parameter);
        }

        [Fact]
        public void ParametersCanBeConfiguredOnDecoratedService()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorWithParameters>().As<IDecoratedService>().WithParameter("parameter", "ABC");
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            builder.RegisterDecorator<DecoratorB, IDecoratedService>();
            var container = builder.Build();

            var instance = container.Resolve<IDecoratedService>();

            Assert.Equal("ABC", ((ImplementorWithParameters)instance.Decorated.Decorated).Parameter);
        }

        [Fact]
        public void DecoratorCanBeAppliedToServiceRegisteredInChildLifetimeScope()
        {
            var builder = new ContainerBuilder();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            var container = builder.Build();

            var scope = container.BeginLifetimeScope(b => b.RegisterType<ImplementorA>().As<IDecoratedService>());
            var instance = scope.Resolve<IDecoratedService>();

            Assert.IsType<DecoratorA>(instance);
        }

        [Fact]
        public void DecoratorCanBeRegisteredInChildLifetimeScope()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ImplementorA>().As<IDecoratedService>();
            var container = builder.Build();

            var scope = container.BeginLifetimeScope(b => b.RegisterDecorator<DecoratorA, IDecoratedService>());

            var scopedInstance = scope.Resolve<IDecoratedService>();
            Assert.IsType<DecoratorA>(scopedInstance);

            var rootInstance = container.Resolve<IDecoratedService>();
            Assert.IsType<ImplementorA>(rootInstance);
        }

        [Fact]
        public void StartableTypesCanBeDecorated()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<StartableImplementation>()
                .As<IDecoratedService>()
                .As<IStartable>()
                .SingleInstance();
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            var container = builder.Build();

            var decorated = Assert.IsType<DecoratorA>(container.Resolve<IDecoratedService>());
            var instance = Assert.IsType<StartableImplementation>(decorated.Decorated);
            Assert.True(instance.Started);
        }

        [Fact]
        public void DecoratorsApplyToKeyedServices()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<ImplementorA>().Keyed<IDecoratedService>("service");
            builder.RegisterDecorator<DecoratorA, IDecoratedService>();
            var container = builder.Build();

            var instance = container.ResolveKeyed<IDecoratedService>("service");

            Assert.IsType<DecoratorA>(instance);
            Assert.IsType<ImplementorA>(instance.Decorated);
        }

        [Fact]
        public void DecoratorAndDecoratedBothDisposedWhenInstancePerDependency()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<DisposableImplementor>().
                As<IDecoratedService>()
                .InstancePerDependency();
            builder.RegisterDecorator<DisposableDecorator, IDecoratedService>();
            var container = builder.Build();

            DisposableDecorator decorator;
            DisposableImplementor decorated;

            using (var scope = container.BeginLifetimeScope())
            {
                var instance = scope.Resolve<IDecoratedService>();
                decorator = (DisposableDecorator)instance;
                decorated = (DisposableImplementor)instance.Decorated;
            }

            Assert.Equal(1, decorator.DisposeCallCount);
            Assert.Equal(1, decorated.DisposeCallCount);
        }

        [Fact]
        public void DecoratorAndDecoratedBothDisposedWhenInstancePerLifetimeScope()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<DisposableImplementor>()
                .As<IDecoratedService>()
                .InstancePerLifetimeScope();
            builder.RegisterDecorator<DisposableDecorator, IDecoratedService>();
            var container = builder.Build();

            DisposableDecorator decorator;
            DisposableImplementor decorated;

            using (var scope = container.BeginLifetimeScope())
            {
                var instance = scope.Resolve<IDecoratedService>();
                decorator = (DisposableDecorator)instance;
                decorated = (DisposableImplementor)instance.Decorated;
            }

            Assert.Equal(1, decorator.DisposeCallCount);
            Assert.Equal(1, decorated.DisposeCallCount);
        }

        [Fact]
        public void DecoratorAndDecoratedBothDisposedWhenInstancePerMatchingLifetimeScope()
        {
            const string tag = "foo";

            var builder = new ContainerBuilder();

            builder.RegisterType<DisposableImplementor>()
                .As<IDecoratedService>()
                .InstancePerMatchingLifetimeScope(tag);
            builder.RegisterDecorator<DisposableDecorator, IDecoratedService>();
            var container = builder.Build();

            DisposableDecorator decorator;
            DisposableImplementor decorated;

            using (var scope = container.BeginLifetimeScope(tag))
            {
                var instance = scope.Resolve<IDecoratedService>();
                decorator = (DisposableDecorator)instance;
                decorated = (DisposableImplementor)instance.Decorated;

                DisposableDecorator decorator2;
                DisposableImplementor decorated2;

                using (var scope2 = scope.BeginLifetimeScope())
                {
                    var instance2 = scope2.Resolve<IDecoratedService>();
                    decorator2 = (DisposableDecorator)instance2;
                    decorated2 = (DisposableImplementor)instance2.Decorated;
                }

                Assert.Equal(0, decorator2.DisposeCallCount);
                Assert.Equal(0, decorated2.DisposeCallCount);
            }

            Assert.Equal(1, decorator.DisposeCallCount);
            Assert.Equal(1, decorated.DisposeCallCount);
        }

        [Fact]
        public void DecoratorAndDecoratedBothDisposedWhenSingleInstance()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<DisposableImplementor>()
                .As<IDecoratedService>()
                .SingleInstance();
            builder.RegisterDecorator<DisposableDecorator, IDecoratedService>();
            var container = builder.Build();

            var instance = container.Resolve<IDecoratedService>();
            container.Dispose();

            var decorator = (DisposableDecorator)instance;
            var decorated = (DisposableImplementor)instance.Decorated;

            Assert.Equal(1, decorator.DisposeCallCount);
            Assert.Equal(1, decorated.DisposeCallCount);
        }
    }
}
