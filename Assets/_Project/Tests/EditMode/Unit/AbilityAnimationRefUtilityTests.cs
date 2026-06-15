using AvantajPrim.Abilities.Data;
using NUnit.Framework;

namespace AvantajPrim.Tests.EditMode.Unit
{
    [TestFixture]
    public sealed class AbilityAnimationRefUtilityTests
    {
        [TestCase("anim_Dash", "Dash")]
        [TestCase("anim_Firewall", "Firewall")]
        [TestCase("anim_Heal", "Heal")]
        [TestCase("anim_DefencedAttack", "DefencedAttack")]
        public void TriggerNameFromAddress_StripsPrefix(string address, string expected)
        {
            Assert.AreEqual(expected, AbilityAnimationRefUtility.TriggerNameFromAddress(address));
        }

        [TestCase("Cast_Dash", "Dash")]
        [TestCase("Cast_Firewall", "Firewall")]
        [TestCase("BattleRunForward", "BattleRunForward")]
        public void TriggerNameFromClipAssetName_StripsCastPrefix(string clipName, string expected)
        {
            Assert.AreEqual(expected, AbilityAnimationRefUtility.TriggerNameFromClipAssetName(clipName));
        }

        [Test]
        public void TriggerNameFromAddress_ReturnsNull_WhenEmpty()
        {
            Assert.IsNull(AbilityAnimationRefUtility.TriggerNameFromAddress(null));
            Assert.IsNull(AbilityAnimationRefUtility.TriggerNameFromAddress(string.Empty));
        }

        [TestCase("bb6186b850404577919c74d9284188be", true)]
        [TestCase("anim_Dash", false)]
        [TestCase("sfx_dash", false)]
        public void IsLikelyAssetGuid_Detects32CharHex(string key, bool expected)
        {
            Assert.AreEqual(expected, AddressableAssetRefUtility.IsLikelyAssetGuid(key));
        }
    }
}
