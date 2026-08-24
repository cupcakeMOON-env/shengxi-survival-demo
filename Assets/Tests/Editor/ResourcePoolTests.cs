using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class ResourcePoolTests
    {
        [Test]
        public void StartsAtZero()
        {
            var pool = new ResourcePool();

            Assert.That(pool.GetAmount(ResourceType.Wood), Is.Zero);
            Assert.That(pool.GetAmount(ResourceType.Stone), Is.Zero);
            Assert.That(pool.GetAmount(ResourceType.Food), Is.Zero);
        }

        [Test]
        public void Add_IncreasesAmount()
        {
            var pool = new ResourcePool();

            pool.Add(ResourceType.Wood, 3);
            pool.Add(ResourceType.Wood, 2);

            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(5));
        }

        [Test]
        public void Add_DoesNothingForNonPositiveAmount()
        {
            var pool = new ResourcePool();

            pool.Add(ResourceType.Stone, 0);
            pool.Add(ResourceType.Stone, -3);

            Assert.That(pool.GetAmount(ResourceType.Stone), Is.Zero);
        }

        [Test]
        public void Add_RespectsCapacity()
        {
            var pool = new ResourcePool { Capacity = 10 };

            pool.Add(ResourceType.Food, 12);

            Assert.That(pool.GetAmount(ResourceType.Food), Is.EqualTo(10));
        }

        [Test]
        public void TrySpend_WhenEnough_ReturnsTrueAndReduces()
        {
            var pool = new ResourcePool();
            pool.Add(ResourceType.Wood, 5);

            var ok = pool.TrySpend(ResourceType.Wood, 3);

            Assert.That(ok, Is.True);
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(2));
        }

        [Test]
        public void TrySpend_WhenShort_ReturnsFalseAndKeepsAmount()
        {
            var pool = new ResourcePool();
            pool.Add(ResourceType.Wood, 2);

            var ok = pool.TrySpend(ResourceType.Wood, 3);

            Assert.That(ok, Is.False);
            Assert.That(pool.GetAmount(ResourceType.Wood), Is.EqualTo(2));
        }
    }
}
