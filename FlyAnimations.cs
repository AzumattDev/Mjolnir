using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Mjolnir
{
    public partial class Mjolnir
    {
        private static readonly Dictionary<string, string> DebugFly = new Dictionary<string, string>();
        public static Dictionary<string, AnimationClip> ExternalAnimations = new Dictionary<string, AnimationClip>();
        private static bool _firstInit;
        private static RuntimeAnimatorController _customDebugFly;
        private static RuntimeAnimatorController _origDebugFly;

        private static void AnimationAwake()
        {
            AssetBundle asset = GetAssetBundleFromResources("azumattanimations");
            DebugFly.Add("Walking", "DebugFlyForward");
            DebugFly.Add("Standard Run New", "DebugFlySuperman");
            DebugFly.Add("Run Item Right", "DebugFlySuperman");
            DebugFly.Add("Idle", "DebugFly");
            DebugFly.Add("IdleTweaked", "DebugFly");
            DebugFly.Add("JumpTweaked", "DebugFly");
            DebugFly.Add("Jog Forward", "DebugFlySuperman");
            DebugFly.Add("Jog Strafe Left", "DebugFly");
            DebugFly.Add("Jog backward", "DebugFly");
            DebugFly.Add("Jog Strafe Left mirrored", "DebugFly");
            DebugFly.Add("Sword And Shield Run Right", "DebugFlySuperman");
            DebugFly.Add("Cheer", "DebugFlyLeft");
            DebugFly.Add("Wave", "DebugFlyRight");
            DebugFly.Add("No no no", "DebugFlyBack");

            ExternalAnimations.Add("DebugFly", asset.LoadAsset<AnimationClip>("DebugFlyMode.anim"));
            ExternalAnimations.Add("DebugFlyForward", asset.LoadAsset<AnimationClip>("DebugFlyForward.anim"));
            ExternalAnimations.Add("DebugFlySuperman", asset.LoadAsset<AnimationClip>("DebugFlySuperMan.anim"));
            ExternalAnimations.Add("DebugFlyLeft", asset.LoadAsset<AnimationClip>("DebugFlyLeft.anim"));
            ExternalAnimations.Add("DebugFlyRight", asset.LoadAsset<AnimationClip>("DebugFlyRight.anim"));
            ExternalAnimations.Add("DebugFlyBack", asset.LoadAsset<AnimationClip>("DebugFlyBack.anim"));
        }


        private static RuntimeAnimatorController MakeAoc(IReadOnlyDictionary<string, string> replacement,
            RuntimeAnimatorController original)
        {
            AnimatorOverrideController aoc = new AnimatorOverrideController(original);
            List<KeyValuePair<AnimationClip, AnimationClip>> anims =
                new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (AnimationClip animation in aoc.animationClips)
            {
                string name = animation.name;
                if (replacement.ContainsKey(name))
                {
                    AnimationClip newClip = Instantiate(ExternalAnimations[replacement[name]]);
                    anims.Add(new KeyValuePair<AnimationClip, AnimationClip>(animation, newClip));
                }
                else
                {
                    anims.Add(new KeyValuePair<AnimationClip, AnimationClip>(animation, animation));
                }
            }

            aoc.ApplyOverrides(anims);
            return aoc;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Start))]
        [HarmonyPriority(Priority.Last)]
        private static class TESTPATCHPLAYERANIMS
        {
            private static void Postfix(Player __instance)
            {
                if (_firstInit) return;
                _firstInit = true;

                _origDebugFly = MakeAoc(new Dictionary<string, string>(),
                    __instance.m_animator.runtimeAnimatorController);
                _customDebugFly = MakeAoc(DebugFly, __instance.m_animator.runtimeAnimatorController);
            }
        }


        /*[HarmonyPatch(typeof(Character), nameof(Character.UpdateDebugFly), typeof(float))]
        private static class DebugFlyCustomAnimationController
        {
            private static void Postfix(Character __instance)
            {
                __instance.m_zanim.SetBool(Character.onGround, true);
                __instance.m_zanim.SetFloat(Character.forward_speed, 0f);
                Player.m_localPlayer.m_animator.runtimeAnimatorController = CustomDebugFly;
                if (ZInput.GetButton("Forward") && !ZInput.GetButton("Run"))
                    __instance.m_zanim.SetFloat(Character.forward_speed, 1f);
                else if (Input.GetKey(KeyCode.W) && ZInput.GetButton("Run"))
                    __instance.m_zanim.SetFloat(Character.forward_speed, 10f);
                else if (ZInput.GetButton("Left"))
                    __instance.m_zanim.SetTrigger("emote_cheer");
                else if (ZInput.GetButton("Right"))
                    __instance.m_zanim.SetTrigger("emote_wave");
                else if (ZInput.GetButton("Backward"))
                    __instance.m_zanim.SetTrigger("emote_nonono");
                else
                    __instance.m_zanim.SetTrigger("emote_stop");
                //Player.m_localPlayer.transform.rotation = Quaternion.LookRotation(GameCamera.instance.transform.forward);
            }
        }*/

        [HarmonyPatch(typeof(Mjolnir), nameof(UpdateMjolnirFlight), typeof(float))]
        private static class DebugFlyCustomAnimationController2
        {
            private static void Postfix()
            {
                Player.m_localPlayer.m_zanim.SetBool(Character.onGround, true);
                Player.m_localPlayer.m_zanim.SetFloat(Character.forward_speed, 0f);
                Player.m_localPlayer.m_animator.runtimeAnimatorController = _customDebugFly;
                if (ZInput.GetButton("Forward") && !ZInput.GetButton("Run"))
                    Player.m_localPlayer.m_zanim.SetFloat(Character.forward_speed, 1f);
                else if (Input.GetKey(KeyCode.W) && ZInput.GetButton("Run"))
                    Player.m_localPlayer.m_zanim.SetFloat(Character.forward_speed, 10f);
                else if (ZInput.GetButton("Left"))
                    Player.m_localPlayer.m_zanim.SetTrigger("emote_cheer");
                else if (ZInput.GetButton("Right"))
                    Player.m_localPlayer.m_zanim.SetTrigger("emote_wave");
                else if (ZInput.GetButton("Backward"))
                    Player.m_localPlayer.m_zanim.SetTrigger("emote_nonono");
                else
                    Player.m_localPlayer.m_zanim.SetTrigger("emote_stop");
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        private static class UnEquipMjolnir
        {
            [HarmonyPrefix]
            private static void RemoveFlight(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects)
            {
                if (item == null || !Player.m_localPlayer || !__instance.IsPlayer()) return;
                if (item.m_dropPrefab.name != "Mjolnir") return;
                if (Player.m_localPlayer.IsDebugFlying()) return;
                Player.m_localPlayer.m_animator.runtimeAnimatorController = _origDebugFly;
                Player.m_localPlayer.m_zanim.SetTrigger("emote_stop");
                Player.m_localPlayer.m_debugFly = false;
            }
        }
    }
}