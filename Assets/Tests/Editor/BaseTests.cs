using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class BaseTests
    {
        [Test]
        public void StartsAtFullHp()
        {
            var baseDefense = new Base(new GridPos(5, 5), 20);

            Assert.That(baseDefense.CurrentHp, Is.EqualTo(20));
            Assert.That(baseDefense.IsDestroyed, Is.False);
        }

        [Test]
        public void TakeDamage_ReducesHp()
        {
            var baseDefense = new Base(new GridPos(5, 5), 20);

            baseDefense.TakeDamage(3);

            Assert.That(baseDefense.CurrentHp, Is.EqualTo(17));
        }

        [Test]
        public void TakeDamage_ToZero_DestroysBase()
        {
            var baseDefense = new Base(new GridPos(5, 5), 2);

            baseDefense.TakeDamage(2);

            Assert.That(baseDefense.IsDestroyed, Is.True);
            Assert.That(baseDefense.CurrentHp, Is.Zero);
        }

        [Test]
        public void TakeDamage_Negative_DoesNothing()
        {
            var baseDefense = new Base(new GridPos(5, 5), 20);

            baseDefense.TakeDamage(-5);

            Assert.That(baseDefense.CurrentHp, Is.EqualTo(20));
        }
    }
}
