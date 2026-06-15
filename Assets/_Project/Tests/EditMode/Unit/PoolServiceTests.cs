using NUnit.Framework;
using Pool.Application;
using Pool.Domain;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class PoolServiceTests
    {
        private sealed class TestPoolable : IPoolable
        {
            public int ResetCount;

            public void Reset() => ResetCount++;
        }

        [Test]
        public void CreatePool_GetReturn_ResetsAndReuses()
        {
            var service = new PoolService();
            IObjectPool<TestPoolable> pool = service.CreatePool("test", () => new TestPoolable(), initialSize: 1, maxSize: 4);

            TestPoolable first = pool.Get();
            first.ResetCount = 0;
            pool.Return(first);

            Assert.AreEqual(1, first.ResetCount);
            Assert.AreEqual(1, pool.InactiveCount);

            TestPoolable second = pool.Get();
            Assert.AreSame(first, second);
            Assert.AreEqual(1, pool.ActiveCount);
        }

        [Test]
        public void CreatePool_DuplicateId_Throws()
        {
            var service = new PoolService();
            service.CreatePool("dup", () => new TestPoolable());

            Assert.Throws<System.InvalidOperationException>(() =>
                service.CreatePool("dup", () => new TestPoolable()));
        }
    }
}
