using System;
using System.Collections.Generic;
using Core.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace ObjectPooling.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="GenericObjectPool{T}"/> behavior.
    /// </summary>
    public sealed class GenericObjectPoolTests
    {
        private readonly List<GameObject> m_CreatedObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < m_CreatedObjects.Count; index++)
            {
                if (m_CreatedObjects[index] != null)
                {
                    UnityEngine.Object.DestroyImmediate(m_CreatedObjects[index]);
                }
            }

            m_CreatedObjects.Clear();
        }

        [Test]
        public void Get_WhenPoolIsEmpty_CreatesAndActivatesInstance()
        {
            int createdCount = 0;
            GenericObjectPool<TestPoolComponent> pool = CreatePool(
                createInstance: () => CreateComponent("PoolUnit_1", ref createdCount),
                initialCapacity: 0);

            TestPoolComponent instance = pool.Get();

            Assert.That(instance, Is.Not.Null);
            Assert.That(createdCount, Is.EqualTo(1));
            Assert.That(pool.ActiveCount, Is.EqualTo(1));
            Assert.That(pool.InactiveCount, Is.EqualTo(0));
            Assert.That(instance.TakenFromPoolCount, Is.EqualTo(1));
            Assert.That(instance.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Get_AfterRelease_ReusesSameInstance()
        {
            int createdCount = 0;
            GenericObjectPool<TestPoolComponent> pool = CreatePool(
                createInstance: () => CreateComponent("PoolUnit_2", ref createdCount),
                initialCapacity: 0);

            TestPoolComponent first = pool.Get();
            pool.Release(first);
            TestPoolComponent second = pool.Get();

            Assert.That(createdCount, Is.EqualTo(1));
            Assert.That(second, Is.SameAs(first));
            Assert.That(first.ReturnedToPoolCount, Is.EqualTo(1));
            Assert.That(second.TakenFromPoolCount, Is.EqualTo(2));
            Assert.That(pool.ActiveCount, Is.EqualTo(1));
            Assert.That(pool.InactiveCount, Is.EqualTo(0));
        }

        [Test]
        public void Release_WhenCalledTwice_DoesNotDuplicateInactiveEntries()
        {
            int createdCount = 0;
            GenericObjectPool<TestPoolComponent> pool = CreatePool(
                createInstance: () => CreateComponent("PoolUnit_3", ref createdCount),
                initialCapacity: 0);

            TestPoolComponent instance = pool.Get();
            pool.Release(instance);
            pool.Release(instance);

            Assert.That(createdCount, Is.EqualTo(1));
            Assert.That(pool.ActiveCount, Is.EqualTo(0));
            Assert.That(pool.InactiveCount, Is.EqualTo(1));
        }

        [Test]
        public void Clear_RemovesAllActiveAndInactiveInstances()
        {
            int createdCount = 0;
            int destroyedCount = 0;
            GenericObjectPool<TestPoolComponent> pool = new GenericObjectPool<TestPoolComponent>(
                createInstance: () => CreateComponent($"PoolUnit_4_{createdCount}", ref createdCount),
                onGet: component => component.gameObject.SetActive(true),
                onRelease: component => component.gameObject.SetActive(false),
                onDestroy: component =>
                {
                    destroyedCount += 1;
                    UnityEngine.Object.DestroyImmediate(component.gameObject);
                },
                initialCapacity: 1);

            TestPoolComponent first = pool.Get();
            TestPoolComponent second = pool.Get();
            pool.Release(second);

            Assert.That(pool.ActiveCount, Is.EqualTo(1));
            Assert.That(pool.InactiveCount, Is.EqualTo(1));

            pool.Clear();

            Assert.That(first == null, Is.True);
            Assert.That(second == null, Is.True);
            Assert.That(pool.ActiveCount, Is.EqualTo(0));
            Assert.That(pool.InactiveCount, Is.EqualTo(0));
            Assert.That(destroyedCount, Is.EqualTo(createdCount));
        }

        private GenericObjectPool<TestPoolComponent> CreatePool(Func<TestPoolComponent> createInstance, int initialCapacity)
        {
            return new GenericObjectPool<TestPoolComponent>(
                createInstance: createInstance,
                onGet: component => component.gameObject.SetActive(true),
                onRelease: component => component.gameObject.SetActive(false),
                onDestroy: component => UnityEngine.Object.DestroyImmediate(component.gameObject),
                initialCapacity: initialCapacity);
        }

        private TestPoolComponent CreateComponent(string name, ref int createdCount)
        {
            GameObject componentObject = new GameObject(name);
            m_CreatedObjects.Add(componentObject);
            createdCount += 1;
            return componentObject.AddComponent<TestPoolComponent>();
        }

        /// <summary>
        /// Minimal poolable component used by tests.
        /// </summary>
        private sealed class TestPoolComponent : MonoBehaviour, IPoolable
        {
            public int TakenFromPoolCount { get; private set; }

            public int ReturnedToPoolCount { get; private set; }

            public void OnTakenFromPool()
            {
                TakenFromPoolCount += 1;
            }

            public void OnReturnedToPool()
            {
                ReturnedToPoolCount += 1;
            }
        }
    }
}
