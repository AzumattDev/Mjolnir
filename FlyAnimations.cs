using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace Mjolnir
{
    public partial class Mjolnir
    {
        private static Dictionary<string, string> debugFly = new Dictionary<string, string>();
        public static Dictionary<string, AnimationClip> ExternalAnimations = new Dictionary<string, AnimationClip>();
        private static bool FirstInit;
        private static RuntimeAnimatorController CustomDebugFly;
        private static RuntimeAnimatorController OrigDebugFly;
        void AnimationAwake()
        {
            AssetBundle asset = GetAssetBundleFromResources("azumattanimations");
            debugFly.Add("Walking", "DebugFlyForward");
            debugFly.Add("Standard Run", "DebugFlySuperman");
            debugFly.Add("Idle", "DebugFly");
            debugFly.Add("jump", "DebugFly");
            debugFly.Add("Jog Forward", "DebugFlySuperman");
            debugFly.Add("Jog Strafe Left", "DebugFly");
            debugFly.Add("Jog backward", "DebugFly");
            debugFly.Add("Jog Strafe Left mirrored", "DebugFly");
            debugFly.Add("Sword And Shield Run Right", "DebugFlySuperman");
            debugFly.Add("Cheer", "DebugFlyLeft");
            debugFly.Add("Waving", "DebugFlyRight");
            debugFly.Add("No no no", "DebugFlyBack");

            ExternalAnimations.Add("DebugFly", asset.LoadAsset<AnimationClip>("DebugFlyMode.anim"));
            ExternalAnimations.Add("DebugFlyForward", asset.LoadAsset<AnimationClip>("DebugFlyForward.anim"));
            ExternalAnimations.Add("DebugFlySuperman", asset.LoadAsset<AnimationClip>("DebugFlySuperMan.anim"));
            ExternalAnimations.Add("DebugFlyLeft", asset.LoadAsset<AnimationClip>("DebugFlyLeft.anim"));
            ExternalAnimations.Add("DebugFlyRight", asset.LoadAsset<AnimationClip>("DebugFlyRight.anim"));
            ExternalAnimations.Add("DebugFlyBack", asset.LoadAsset<AnimationClip>("DebugFlyBack.anim"));
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Start))]
        [HarmonyPriority(Priority.Last)]
        static class TESTPATCHPLAYERANIMS
        {

            static void Postfix(Player __instance)
            {

                if (!FirstInit)
                {
                    FirstInit = true;

                    OrigDebugFly = MakeAOC(new Dictionary<string, string>(), __instance.m_animator.runtimeAnimatorController);
                    CustomDebugFly = MakeAOC(debugFly, __instance.m_animator.runtimeAnimatorController);
                }



            }
        }


        public static RuntimeAnimatorController MakeAOC(Dictionary<string, string> replacement, RuntimeAnimatorController ORIGINAL)
        {
            AnimatorOverrideController aoc = new AnimatorOverrideController(ORIGINAL);
            var anims = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (var animation in aoc.animationClips)
            {
                string name = animation.name;
                if (replacement.ContainsKey(name))
                {
                    AnimationClip newClip = MonoBehaviour.Instantiate<AnimationClip>(ExternalAnimations[replacement[name]]);
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

        /*static void FlyCheckMethod()
        {
            Player p = Player.m_localPlayer;
            if (p.m_debugFly)
            {

                p.m_animator.runtimeAnimatorController = CustomDebugFly;
            }
            else
            {

                p.m_animator.runtimeAnimatorController = OrigDebugFly;
            }
        }*/


        [HarmonyPatch(typeof(Character), nameof(Character.UpdateDebugFly), typeof(float))]
        static class DebugFlyCustomAnimationController
        {
            static void Postfix(Character __instance)
            {
                __instance.m_zanim.SetBool(Character.onGround, true);
                __instance.m_zanim.SetFloat(Character.forward_speed, 0f);
                if (ZInput.GetButton("Forward") && !ZInput.GetButton("Run"))
                {
                    __instance.m_zanim.SetFloat(Character.forward_speed, 1f);
                }
                else
                if (Input.GetKey(KeyCode.W) && ZInput.GetButton("Run"))
                {
                    __instance.m_zanim.SetFloat(Character.forward_speed, 10f);
                }
                else
                if (ZInput.GetButton("Left"))
                {
                    __instance.m_zanim.SetTrigger("emote_cheer");
                }
                else
                if (ZInput.GetButton("Right"))
                {
                    __instance.m_zanim.SetTrigger("emote_wave");
                }
                else
                if (ZInput.GetButton("Backward"))
                {
                    __instance.m_zanim.SetTrigger("emote_nonono");
                }
                else
                {
                    __instance.m_zanim.SetTrigger("emote_stop");
                }
                //Player.m_localPlayer.transform.rotation = Quaternion.LookRotation(GameCamera.instance.transform.forward);
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]

        static class UnEquipMjolnir
        {
            [HarmonyPrefix]
            static void RemoveFlight(ItemDrop.ItemData item)
            {
                if (item == null)
                {
                    return;
                }
                if (item.m_dropPrefab.name != "Mjolnir") return;
                Player.m_localPlayer.m_animator.runtimeAnimatorController = OrigDebugFly;
                Player.m_localPlayer.m_zanim.SetTrigger("emote_stop");
                Player.m_localPlayer.m_debugFly = false;
            }

        }
    }
}