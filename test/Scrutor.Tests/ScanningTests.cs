﻿using Microsoft.Extensions.DependencyInjection;
using Scrutor.Tests;
using Xunit;

namespace Scrutor.Tests
{
    public class ScanningTests
    {
        [Fact]
        public void CanFilterTypesToScan()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                .AddClasses(classes => classes.AssignableTo<ITransientService>())
                    .AsImplementedInterfaces()
                    .WithTransientLifetime());

            var services = collection.GetDescriptors<ITransientService>();

            Assert.Equal(services, collection);

            Assert.All(services, service =>
            {
                Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                Assert.Equal(typeof(ITransientService), service.ServiceType);
            });
        }

        [Fact]
        public void CanRegisterAsSpecificType()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                .AddClasses(classes => classes.AssignableTo<ITransientService>())
                    .As<ITransientService>());

            var services = collection.GetDescriptors<ITransientService>();

            Assert.Equal(services, collection);

            Assert.All(services, service =>
            {
                Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
                Assert.Equal(typeof(ITransientService), service.ServiceType);
            });
        }

        [Fact]
        public void CanSpecifyLifetime()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan.FromAssemblyOf<IScopedService>()
                .AddClasses(classes => classes.AssignableTo<IScopedService>())
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());

            var services = collection.GetDescriptors<IScopedService>();

            Assert.Equal(services, collection);

            Assert.All(services, service =>
            {
                Assert.Equal(ServiceLifetime.Scoped, service.Lifetime);
                Assert.Equal(typeof(IScopedService), service.ServiceType);
            });
        }

        [Fact]
        public void CanRegisterGenericTypes()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan.FromAssemblyOf<IScopedService>()
                .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
                    .AsImplementedInterfaces()
                    .WithScopedLifetime());

            var service = collection.GetDescriptor<IQueryHandler<string, int>>();

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Scoped, service.Lifetime);
            Assert.Equal(typeof(QueryHandler), service.ImplementationType);
        }

        [Fact]
        public void CanScanUsingAttributes()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan.FromAssemblyOf<ITransientService>().AddFromAttributes());

            var service = collection.GetDescriptor<ITransientService>();

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
            Assert.Equal(typeof(TransientService1), service.ImplementationType);
        }

        [Fact]
        public void CanHandleMultipleAttributes()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan.FromAssemblyOf<ITransientServiceToCombine>().AddFromAttributes());

            var transientService = collection.GetDescriptor<ITransientServiceToCombine>();

            Assert.NotNull(transientService);
            Assert.Equal(ServiceLifetime.Transient, transientService.Lifetime);
            Assert.Equal(typeof(CombinedService), transientService.ImplementationType);

            var scopedService = collection.GetDescriptor<IScopedServiceToCombine>();

            Assert.NotNull(scopedService);
            Assert.Equal(ServiceLifetime.Scoped, scopedService.Lifetime);
            Assert.Equal(typeof(CombinedService), scopedService.ImplementationType);

            var singletonService = collection.GetDescriptor<ISingletonServiceToCombine>();

            Assert.NotNull(singletonService);
            Assert.Equal(ServiceLifetime.Singleton, singletonService.Lifetime);
            Assert.Equal(typeof(CombinedService), singletonService.ImplementationType);
        }

        [Fact]
        public void AutoRegisterAsMatchingInterface()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                .AddClasses()
                    .AsMatchingInterface()
                    .WithTransientLifetime());

            Assert.Equal(2, collection.Count);

            var services = collection.GetDescriptors<ITransientService>();

            Assert.NotNull(services);
            Assert.All(services, s =>
            {
                Assert.Equal(ServiceLifetime.Transient, s.Lifetime);
                Assert.Equal(typeof(ITransientService), s.ServiceType);
            });
        }

        [Fact]
        public void AutoRegisterAsMatchingInterfaceSameNamespaceOnly()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan.FromAssemblyOf<ITransientService>()
                .AddClasses()
                    .AsMatchingInterface((t, x) => x.InNamespaces(t.Namespace))
                    .WithTransientLifetime());

            Assert.Equal(1, collection.Count);

            var service = collection.GetDescriptor<ITransientService>();

            Assert.NotNull(service);
            Assert.Equal(ServiceLifetime.Transient, service.Lifetime);
            Assert.Equal(typeof(TransientService), service.ImplementationType);
        }

        [Fact]
        public void ShouldNotRegisterGenericTypesWithMissingParameters()
        {
            var collection = new ServiceCollection();

            collection.Scan(scan => scan
                .FromAssemblyOf<ITransientService>()
                    .AddClasses(x => x.AssignableTo(typeof(BaseQueryHandler<>)))
                    .AsImplementedInterfaces());

            // This would throw if generic type definitions were registered.
            collection.BuildServiceProvider();
        }
    }

    public interface ITransientService { }

    [ServiceDescriptor(typeof(ITransientService))]
    public class TransientService1 : ITransientService { }

    public class TransientService2 : ITransientService { }
    public class TransientService : ITransientService { }

    public interface IScopedService { }

    public class ScopedService1 : IScopedService { }

    public class ScopedService2 : IScopedService { }

    public interface IQueryHandler<TQuery, TResult> { }

    public class QueryHandler : IQueryHandler<string, int> { }

    public class BaseQueryHandler<T> : IQueryHandler<T, int> { }

    public interface ITransientServiceToCombine { }
    public interface IScopedServiceToCombine { }
    public interface ISingletonServiceToCombine { }

    [ServiceDescriptor(typeof(ITransientServiceToCombine))]
    [ServiceDescriptor(typeof(IScopedServiceToCombine), ServiceLifetime.Scoped)]
    [ServiceDescriptor(typeof(ISingletonServiceToCombine), ServiceLifetime.Singleton)]
    public class CombinedService : ITransientServiceToCombine, IScopedServiceToCombine, ISingletonServiceToCombine { }
}

namespace UnwantedNamespace
{
    public class TransientService : ITransientService
    {
    }
}
