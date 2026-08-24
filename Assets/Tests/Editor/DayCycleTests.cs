using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    public class DayCycleTests
    {
        [Test]
        public void StartsAtDayOne()
        {
            var cycle = new DayCycle();

            Assert.That(cycle.Day, Is.EqualTo(1));
            Assert.That(cycle.IsDay, Is.True);
        }

        [Test]
        public void StartNight_OnlyFromDay()
        {
            var cycle = new DayCycle();

            Assert.That(cycle.StartNight(), Is.True);
            Assert.That(cycle.IsNight, Is.True);
            Assert.That(cycle.StartNight(), Is.False, "夜晚不能再次进入夜晚");
        }

        [Test]
        public void EndNight_IncrementsDay()
        {
            var cycle = new DayCycle();
            cycle.StartNight();

            Assert.That(cycle.EndNight(), Is.True);
            Assert.That(cycle.IsDay, Is.True);
            Assert.That(cycle.Day, Is.EqualTo(2));
        }

        [Test]
        public void EndNight_WhenNotNight_Fails()
        {
            var cycle = new DayCycle();

            Assert.That(cycle.EndNight(), Is.False);
            Assert.That(cycle.Day, Is.EqualTo(1));
        }
    }
}
