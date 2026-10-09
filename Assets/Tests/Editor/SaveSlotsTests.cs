using System.Collections.Generic;
using NUnit.Framework;
using ShengXi.Simulation;

namespace ShengXi.Tests.Editor
{
    /// <summary>存档位定义：固定三个、Id 唯一、下标与显示名一一对应。</summary>
    public class SaveSlotsTests
    {
        [Test]
        public void HasThreeDistinctSlots()
        {
            Assert.That(SaveSlots.Count, Is.EqualTo(3));
            Assert.That(SaveSlots.All.Count, Is.EqualTo(SaveSlots.Count));

            var unique = new HashSet<string>(SaveSlots.All);
            Assert.That(unique.Count, Is.EqualTo(SaveSlots.Count), "三个槽位的 Id 不能重复");

            foreach (var id in SaveSlots.All)
            {
                Assert.That(SaveSlots.IsValid(id), Is.True, $"{id} 应当是合法槽位");
            }
        }

        [Test]
        public void IdAt_MapsIndexToSlot()
        {
            for (var i = 0; i < SaveSlots.Count; i++)
            {
                var id = SaveSlots.IdAt(i);
                Assert.That(id, Is.Not.Null);
                Assert.That(SaveSlots.IndexOf(id), Is.EqualTo(i), "Id 与下标必须能互相换算");
            }
        }

        [Test]
        public void DisplayName_IsOneBased()
        {
            Assert.That(SaveSlots.DisplayName(SaveSlots.IdAt(0)), Is.EqualTo("存档 1"));
            Assert.That(SaveSlots.DisplayName(SaveSlots.IdAt(1)), Is.EqualTo("存档 2"));
            Assert.That(SaveSlots.DisplayName(SaveSlots.IdAt(2)), Is.EqualTo("存档 3"));
        }

        [Test]
        public void OutOfRangeInput_IsRejected()
        {
            Assert.That(SaveSlots.IdAt(-1), Is.Null);
            Assert.That(SaveSlots.IdAt(SaveSlots.Count), Is.Null);
            Assert.That(SaveSlots.IndexOf("not-a-slot.json"), Is.EqualTo(-1));
            Assert.That(SaveSlots.IndexOf(null), Is.EqualTo(-1));
            Assert.That(SaveSlots.IsValid("not-a-slot.json"), Is.False);
            Assert.That(SaveSlots.DisplayName("not-a-slot.json"), Is.EqualTo("未知存档位"));
        }
    }
}
