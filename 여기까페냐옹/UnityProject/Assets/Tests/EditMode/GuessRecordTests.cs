using NUnit.Framework;
using UnityEngine;
using YeogiCafe.Data;
using YeogiCafe.Observation;
using YeogiCafe.Save;

namespace YeogiCafe.Tests
{
    // 추리 기록: 짐작부터 도전, 하루 한 번, 오답은 기억, 짐작 단계 정답은 보너스 1.5배
    public class GuessRecordTests
    {
        [Test]
        public void 오답은_하루에_한번만_정답은_기록된다()
        {
            var cfg = ScriptableObject.CreateInstance<BalanceConfig>();
            var save = new SaveGame();
            var obs = new ObservationManager(cfg, save);
            var rec = save.GetOrCreateCat("cat_cheese").observation.Get(PrefAxis.Food);

            Assert.AreEqual(ObservationManager.GuessResult.NotReady, obs.TryGuess("cat_cheese", PrefAxis.Food, "menu_milk", false, 1, out _));

            rec.state = ObsState.Suspected;
            Assert.AreEqual(ObservationManager.GuessResult.Wrong, obs.TryGuess("cat_cheese", PrefAxis.Food, "menu_milk", false, 1, out _));
            Assert.Contains("menu_milk", rec.wrongGuesses);
            Assert.AreEqual(ObservationManager.GuessResult.TriedToday, obs.TryGuess("cat_cheese", PrefAxis.Food, "menu_tuna_sand", true, 1, out _));

            Assert.AreEqual(ObservationManager.GuessResult.Correct, obs.TryGuess("cat_cheese", PrefAxis.Food, "menu_tuna_sand", true, 2, out int bonus));
            Assert.AreEqual(ObsState.Recorded, rec.state);
            Assert.AreEqual("menu_tuna_sand", rec.recordedValue);
            Assert.AreEqual(Mathf.RoundToInt(cfg.discoveryBonusGold * 1.5f), bonus);
        }
    }
}
