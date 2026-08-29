using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Jewelry.EditingSystem;

internal interface ICollectionAdapter
{
    void Add(object? item);
    void Remove(object? item);
    bool TryMove(int oldIndex, int newIndex);
}

internal static class CollectionAdapter
{
    public static ICollectionAdapter Create(object collection)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

        return Adapters.GetValue(collection, CreateCore);
    }

    public static bool TryMove(object collection, int oldIndex, int newIndex)
    {
        return Create(collection).TryMove(oldIndex, newIndex);
    }

    private static ICollectionAdapter CreateCore(object collection)
    {
        var factory = Factories.GetOrAdd(collection.GetType(), CreateFactory);
        return factory.Create(collection);
    }

#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050",
        Justification = "The dynamic generic adapter path is guarded by RuntimeFeature.IsDynamicCodeSupported and is unreachable under NativeAOT.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2026",
        Justification = "The JIT-only factory constructor is preserved explicitly and the NativeAOT path does not use runtime generic construction.")]
#endif
    private static ICollectionAdapterFactory CreateFactory(Type collectionType)
    {
#if NET8_0_OR_GREATER
        if (RuntimeFeature.IsDynamicCodeSupported)
            return CreateDynamicFactory(collectionType);

        return CreateAotFactory(collectionType);
#else
        return CreateDynamicFactory(collectionType);
#endif
    }

#if NET8_0_OR_GREATER
    [RequiresDynamicCode("Creates a closed generic collection adapter for the runtime item type.")]
    [RequiresUnreferencedCode("Creates a closed generic collection adapter for the runtime item type.")]
    [DynamicDependency(
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor,
        typeof(GenericCollectionAdapterFactory<>))]
#endif
    private static ICollectionAdapterFactory CreateDynamicFactory(Type collectionType)
    {
        foreach (var interfaceType in collectionType.GetInterfaces())
        {
            if (interfaceType.IsGenericType is false ||
                interfaceType.GetGenericTypeDefinition() != typeof(ICollection<>))
                continue;

            var itemType = interfaceType.GetGenericArguments()[0];
            var factoryType = typeof(GenericCollectionAdapterFactory<>).MakeGenericType(itemType);
            return (ICollectionAdapterFactory)(Activator.CreateInstance(factoryType) ??
                throw new InvalidOperationException("Failed to create a collection adapter factory."));
        }

        throw new NotSupportedException(
            $"Collection type '{collectionType}' must implement IList or ICollection<T>.");
    }

#if NET8_0_OR_GREATER
    [DynamicDependency("Add", typeof(ICollection<>))]
    [DynamicDependency("Remove", typeof(ICollection<>))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2070",
        Justification = "Only interface metadata is inspected. ICollection<T> members used through reflection are preserved explicitly.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075",
        Justification = "The reflected type is verified to be ICollection<T>, whose Add/Remove members are preserved explicitly.")]
    private static ICollectionAdapterFactory CreateAotFactory(Type collectionType)
    {
        Type? collectionInterface = null;

        foreach (var interfaceType in collectionType.GetInterfaces())
        {
            if (interfaceType.IsGenericType is false ||
                interfaceType.GetGenericTypeDefinition() != typeof(ICollection<>))
                continue;

            collectionInterface = interfaceType;
            break;
        }

        if (collectionInterface is null)
            throw new NotSupportedException(
                $"Collection type '{collectionType}' must implement IList or ICollection<T>.");

        var addMethod = collectionInterface.GetMethod(nameof(ICollection<object>.Add)) ??
            throw new InvalidOperationException("ICollection<T>.Add method was not found.");
        var removeMethod = collectionInterface.GetMethod(nameof(ICollection<object>.Remove)) ??
            throw new InvalidOperationException("ICollection<T>.Remove method was not found.");
        var moveMethod = FindObservableCollectionMoveMethod(collectionType);

        return new ReflectionCollectionAdapterFactory(addMethod, removeMethod, moveMethod);
    }

    [DynamicDependency("Move", typeof(ObservableCollection<>))]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2070",
        Justification = "The reflected type is verified to be ObservableCollection<T>, whose Move member is preserved explicitly.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075",
        Justification = "Base types are inspected only to locate ObservableCollection<T>; Move is preserved explicitly.")]
    private static MethodInfo? FindObservableCollectionMoveMethod(Type collectionType)
    {
        for (var type = collectionType; type is { }; type = type.BaseType)
        {
            if (type.IsGenericType is false ||
                type.GetGenericTypeDefinition() != typeof(ObservableCollection<>))
                continue;

            return type.GetMethod(
                nameof(ObservableCollection<object>.Move),
                [typeof(int), typeof(int)]);
        }

        return null;
    }
#endif

    private interface ICollectionAdapterFactory
    {
        ICollectionAdapter Create(object collection);
    }

    private sealed class GenericCollectionAdapterFactory<T> : ICollectionAdapterFactory
    {
        public ICollectionAdapter Create(object collection)
        {
            return new GenericCollectionAdapter<T>(collection);
        }
    }

    private sealed class GenericCollectionAdapter<T>(object collection) : ICollectionAdapter
    {
        public void Add(object? item)
        {
            _collection.Add((T)item!);
        }

        public void Remove(object? item)
        {
            if (_collection.Remove((T)item!) is false)
                throw new InvalidOperationException("The item to remove was not found in the collection.");
        }

        public bool TryMove(int oldIndex, int newIndex)
        {
            if (_collection is not ObservableCollection<T> observableCollection)
                return false;

            observableCollection.Move(oldIndex, newIndex);
            return true;
        }

        private readonly ICollection<T> _collection = (ICollection<T>)collection;
    }

#if NET8_0_OR_GREATER
    private sealed class ReflectionCollectionAdapterFactory(
        MethodInfo addMethod,
        MethodInfo removeMethod,
        MethodInfo? moveMethod) : ICollectionAdapterFactory
    {
        public ICollectionAdapter Create(object collection)
        {
            return new ReflectionCollectionAdapter(collection, _addInvoker, _removeInvoker, _moveInvoker);
        }

        private readonly MethodInvoker _addInvoker = MethodInvoker.Create(addMethod);
        private readonly MethodInvoker _removeInvoker = MethodInvoker.Create(removeMethod);
        private readonly MethodInvoker? _moveInvoker = moveMethod is null ? null : MethodInvoker.Create(moveMethod);
    }

    private sealed class ReflectionCollectionAdapter(
        object collection,
        MethodInvoker addInvoker,
        MethodInvoker removeInvoker,
        MethodInvoker? moveInvoker) : ICollectionAdapter
    {
        public void Add(object? item)
        {
            _ = addInvoker.Invoke(collection, item);
        }

        public void Remove(object? item)
        {
            if (removeInvoker.Invoke(collection, item) is not true)
                throw new InvalidOperationException("The item to remove was not found in the collection.");
        }

        public bool TryMove(int oldIndex, int newIndex)
        {
            if (moveInvoker is null)
                return false;

            _ = moveInvoker.Invoke(collection, oldIndex, newIndex);
            return true;
        }
    }
#endif

    private static readonly ConditionalWeakTable<object, ICollectionAdapter> Adapters = new();
    private static readonly ConcurrentDictionary<Type, ICollectionAdapterFactory> Factories = new();
}
