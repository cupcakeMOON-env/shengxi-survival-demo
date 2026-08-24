using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class ActionPointSystemTests
    {
        [Test]
        public void StartsAtMax()
        {
            var ap = new ActionPointSystem(10);

            Assert.That(ap.Current, Is.EqualTo(10));
            Assert.That(ap.MaxPerDay, Is.EqualTo(10));
        }

        [Test]
        public void Spend_ReducesCurrent()
        {
            var ap = new ActionPointSystem(10);

            var ok = ap.Spend(3);

            Assert.That(ok, Is.True);
            Assert.That(ap.Current, Is.EqualTo(7));
        }

        [Test]
        public void Spend_MoreThanCurrent_Fails()
        {
            var ap = new ActionPointSystem(5);

            var ok = ap.Spend(6);

            Assert.That(ok, Is.False);
            Assert.That(ap.Current, Is.EqualTo(5));
        }

        [Test]
        public void ResetForNewDay_RestoresMax()
        {
            var ap = new ActionPointSystem(10);
            ap.Spend(4);

            ap.ResetForNewDay();

            Assert.That(ap.Current, Is.EqualTo(10));
        }
    }
}
