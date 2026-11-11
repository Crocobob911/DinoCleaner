using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DinoCleaner.Core.Tests
{
    /// <summary>
    /// 저장/돈 기능 자동 테스트. 실제 세이브는 건드리지 않고 매번 빈 임시 폴더에서 실행됩니다.
    /// 실행: Window > General > Test Runner > EditMode > Run All
    /// </summary>
    public class SaveSystemTests
    {
        private string tempDir;

        [SetUp]
        public void SetUp()
        {
            tempDir = Path.Combine(Path.GetTempPath(), "DinoCleanerSaveTest_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            SaveSystem.DirectoryOverride = tempDir;
            SaveSystem.Load(); // 빈 폴더 → 새 게임 상태
            SettingsSystem.Load(); // 빈 폴더 → 기본 설정
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.DirectoryOverride = null;
            SaveSystem.Load(); // 실제 세이브 상태로 되돌림
            SettingsSystem.Load();
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }

        [Test]
        public void 세이브가_없으면_시작자금으로_시작()
        {
            Assert.AreEqual(SaveData.NewGameMoney, new Wallet().Money);
            Assert.IsFalse(File.Exists(SaveSystem.FilePath));
        }

        [Test]
        public void Income하면_파일에_저장되고_다시_불러와도_유지()
        {
            new Wallet().Income(250);

            Assert.IsTrue(File.Exists(SaveSystem.FilePath));
            SaveSystem.Load(); // 게임을 껐다 켠 것과 같음
            Assert.AreEqual(SaveData.NewGameMoney + 250, new Wallet().Money);
        }

        [Test]
        public void TryConsume_돈이_충분하면_차감되고_저장()
        {
            var wallet = new Wallet();
            wallet.Income(500);
            Assert.IsTrue(wallet.TryConsume(300));

            SaveSystem.Load();
            Assert.AreEqual(SaveData.NewGameMoney + 200, new Wallet().Money);
        }

        [Test]
        public void TryConsume_돈이_부족하면_실패하고_그대로()
        {
            var wallet = new Wallet();
            Assert.IsFalse(wallet.TryConsume(SaveData.NewGameMoney + 1));
            Assert.AreEqual(SaveData.NewGameMoney, wallet.Money);
            Assert.IsFalse(File.Exists(SaveSystem.FilePath), "실패했는데 저장이 일어나면 안 됨");
        }

        [Test]
        public void 돈이_바뀔때만_Changed_호출()
        {
            var wallet = new Wallet();
            int count = 0;
            wallet.Changed += () => count++;

            wallet.Income(10);                                // +1
            wallet.TryConsume(5);                             // +1
            wallet.TryConsume(int.MaxValue);                  // 실패 → 호출 안 됨
            wallet.Income(0);                                 // 변화 없음 → 호출 안 됨

            Assert.AreEqual(2, count);
        }

        [Test]
        public void Income에_음수가_오면_무시()
        {
            LogAssert.Expect(LogType.Warning, new Regex("음수"));
            var wallet = new Wallet();
            wallet.Income(-100);
            Assert.AreEqual(SaveData.NewGameMoney, wallet.Money);
        }

        [Test]
        public void 돈이_int_최대값을_넘지_않음()
        {
            var wallet = new Wallet();
            wallet.Income(int.MaxValue);
            wallet.Income(1); // 시작 자금이 0이어도 넘침 처리를 확인하도록 한 번 더
            Assert.AreEqual(int.MaxValue, wallet.Money);
        }

        [Test]
        public void 예전_세이브에_없는_필드는_기본값으로_채움()
        {
            // 저장 항목이 추가되기 전의 세이브 파일을 흉내냄
            File.WriteAllText(SaveSystem.FilePath, "{\"version\":1}");
            SaveSystem.Load();
            Assert.AreEqual(SaveData.NewGameMoney, new Wallet().Money);
        }

        [Test]
        public void 깨진_세이브는_따로_옮기고_새_게임으로_시작()
        {
            File.WriteAllText(SaveSystem.FilePath, "{ 이건 JSON이 아님");
            LogAssert.Expect(LogType.Error, new Regex("세이브 파일을 읽지 못해"));

            SaveSystem.Load();

            Assert.AreEqual(SaveData.NewGameMoney, new Wallet().Money);
            Assert.IsFalse(File.Exists(SaveSystem.FilePath));
            Assert.AreEqual(1, Directory.GetFiles(tempDir, "save_broken_*.json").Length);
        }

        [Test]
        public void 튜토리얼_완료여부는_새_게임에서_false()
        {
            Assert.IsFalse(SaveSystem.Data.tutorialCompleted);
        }

        [Test]
        public void 튜토리얼_완료를_저장하면_다시_불러와도_유지()
        {
            SaveSystem.Data.tutorialCompleted = true;
            SaveSystem.Save();

            SaveSystem.Load();
            Assert.IsTrue(SaveSystem.Data.tutorialCompleted);
        }

        [Test]
        public void 튜토리얼_필드가_없는_예전_세이브는_미완료로_불러옴()
        {
            File.WriteAllText(SaveSystem.FilePath, "{\"version\":1,\"money\":300}");
            SaveSystem.Load();

            Assert.IsFalse(SaveSystem.Data.tutorialCompleted);
            Assert.AreEqual(300, new Wallet().Money);
        }

        [Test]
        public void 업그레이드_단계가_없으면_기본값()
        {
            Assert.AreEqual(1, SaveSystem.Data.GetUpgradeLevel("pressure", 1));
        }

        [Test]
        public void 업그레이드_단계를_저장하면_다시_불러와도_유지()
        {
            SaveSystem.Data.SetUpgradeLevel("pressure", 2);
            SaveSystem.Data.SetUpgradeLevel("fairyDino", 1);
            SaveSystem.Data.SetUpgradeLevel("pressure", 3); // 같은 id는 덮어씀
            SaveSystem.Save();

            SaveSystem.Load();
            Assert.AreEqual(3, SaveSystem.Data.GetUpgradeLevel("pressure", 1));
            Assert.AreEqual(1, SaveSystem.Data.GetUpgradeLevel("fairyDino", 0));
            Assert.AreEqual(2, SaveSystem.Data.upgrades.Count);
        }

        [Test]
        public void 업그레이드와_돈_차감이_한_번의_저장에_함께_기록()
        {
            var wallet = new Wallet();
            wallet.Income(500);

            SaveSystem.Data.SetUpgradeLevel("pressure", 2);
            wallet.TryConsume(100); // 이 저장에 업그레이드 단계도 포함되어야 함

            SaveSystem.Load();
            Assert.AreEqual(2, SaveSystem.Data.GetUpgradeLevel("pressure", 1));
            Assert.AreEqual(SaveData.NewGameMoney + 400, new Wallet().Money);
        }

        [Test]
        public void 업그레이드_필드가_없는_예전_세이브도_불러옴()
        {
            File.WriteAllText(SaveSystem.FilePath, "{\"version\":1,\"money\":700}");
            SaveSystem.Load();

            Assert.AreEqual(700, new Wallet().Money);
            Assert.IsNotNull(SaveSystem.Data.upgrades);
            Assert.AreEqual(0, SaveSystem.Data.upgrades.Count);
        }

        // ===== 설정 (settings.json) =====

        [Test]
        public void 설정파일이_없으면_기본값()
        {
            SettingsData s = SettingsSystem.Data;
            Assert.AreEqual(50, s.bgmVolume);
            Assert.AreEqual(65, s.sfxVolume);
            Assert.AreEqual(40, s.mouseSensitivity);
            Assert.IsFalse(File.Exists(SettingsSystem.FilePath));
        }

        [Test]
        public void 설정은_MarkDirty_후_SaveIfDirty때만_저장()
        {
            SettingsSystem.SaveIfDirty(); // 바뀐 게 없으면 파일을 만들지 않음
            Assert.IsFalse(File.Exists(SettingsSystem.FilePath));

            SettingsSystem.Data.bgmVolume = 10;
            SettingsSystem.MarkDirty();
            SettingsSystem.SaveIfDirty();

            Assert.IsTrue(File.Exists(SettingsSystem.FilePath));
            Assert.IsFalse(SettingsSystem.IsDirty);
            SettingsSystem.Load();
            Assert.AreEqual(10, SettingsSystem.Data.bgmVolume);
        }

        [Test]
        public void 설정과_게임진행은_다른_파일()
        {
            SettingsSystem.Data.sfxVolume = 5;
            SettingsSystem.Save();
            new Wallet().Income(100);

            Assert.AreNotEqual(SaveSystem.FilePath, SettingsSystem.FilePath);

            SaveSystem.DeleteSave(); // 새 게임
            SettingsSystem.Load();
            Assert.AreEqual(5, SettingsSystem.Data.sfxVolume, "새 게임을 해도 설정은 유지되어야 함");
            Assert.AreEqual(SaveData.NewGameMoney, new Wallet().Money);
        }

        [Test]
        public void 설정파일의_범위밖_값은_0에서_100으로_보정()
        {
            File.WriteAllText(SettingsSystem.FilePath, "{\"bgmVolume\":150,\"sfxVolume\":-5}");
            SettingsSystem.Load();

            Assert.AreEqual(100, SettingsSystem.Data.bgmVolume);
            Assert.AreEqual(0, SettingsSystem.Data.sfxVolume);
            Assert.AreEqual(40, SettingsSystem.Data.mouseSensitivity, "파일에 없는 항목은 기본값");
        }

        [Test]
        public void 깨진_설정파일은_기본값으로()
        {
            File.WriteAllText(SettingsSystem.FilePath, "망가진 설정");
            LogAssert.Expect(LogType.Error, new Regex("설정 파일을 읽지 못해"));

            SettingsSystem.Load();

            Assert.AreEqual(50, SettingsSystem.Data.bgmVolume);
            Assert.AreEqual(1, Directory.GetFiles(tempDir, "settings_broken_*.json").Length);
        }

        [Test]
        public void 덮어쓰기_저장후_임시파일이_남지_않음()
        {
            var wallet = new Wallet();
            wallet.Income(1);
            wallet.Income(1); // 두 번째는 기존 파일을 교체하는 경로

            Assert.IsFalse(File.Exists(SaveSystem.FilePath + ".tmp"));
            SaveSystem.Load();
            Assert.AreEqual(SaveData.NewGameMoney + 2, new Wallet().Money);
        }
    }
}
