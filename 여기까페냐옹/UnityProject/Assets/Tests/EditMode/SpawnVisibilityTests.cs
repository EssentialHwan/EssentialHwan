using NUnit.Framework;
using UnityEngine;
using YeogiCafe.Core;

namespace YeogiCafe.Tests
{
    // [버그수정 회귀 가드] "Play 해도 고양이가 안 움직인다" 대응.
    // 원인: 1x 배속 + 첫 스폰까지 spawnMorning(30s)×IntervalMultiplier(1.6)≈48초 무반응.
    // 대응: (1) 아침 즉시 첫 손님 스폰, (2) 기본 배속 2x. 아래 기본값이 유지되는지 검증.
    public class SpawnVisibilityTests
    {
        [Test]
        public void DayManager_DefaultSpeed_IsNormal()
        {
            var go = new GameObject("day");
            var day = go.AddComponent<DayManager>();
            Assert.AreEqual(1f, day.speed, "기본은 1배속(하루 6분) — 2x/3x는 설정에서");
        }

        [Test]
        public void CustomerSpawner_ImmediateFirstSpawn_DefaultsOn()
        {
            var go = new GameObject("spawner");
            var spawner = go.AddComponent<CustomerSpawner>();
            Assert.IsTrue(spawner.immediateFirstSpawn,
                "아침 시작 즉시 첫 손님을 스폰해 대기 없이 이동이 보여야 함");
        }
    }
}
