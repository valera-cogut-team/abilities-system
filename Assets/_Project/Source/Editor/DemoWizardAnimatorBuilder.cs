using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace EditorTools
{
    internal static class DemoWizardAnimatorBuilder
    {
        private const string SourceController =
            "Assets/ThridPartyAssets/WizardPolyArt/Animations/WizardAnimControl.controller";

        private const string DestController = "Assets/_Project/Prefabs/Demo/DemoWizardAbilityAnimator.controller";

        private const string AnimRoot = "Assets/ThridPartyAssets/WizardPolyArt/Animations";

        public static void CreateIfMissing()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(DestController) != null)
                return;

            if (!AssetDatabase.CopyAsset(SourceController, DestController))
            {
                Debug.LogWarning("[AvantajPrim] Could not copy WizardAnimControl for demo animator.");
                return;
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(DestController);
            if (controller == null)
                return;

            AddParameterIfMissing(controller, "IsWalking", AnimatorControllerParameterType.Bool);
            foreach (string trigger in new[] { "Dash", "Firewall", "Heal", "DefencedAttack", "GetHit" })
                AddParameterIfMissing(controller, trigger, AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = FindState(stateMachine, "Idle");

            AnimatorState dash = GetOrCreateState(stateMachine, "Dash", LoadClip("BattleRunForward.fbx", "BattleRunForward"));
            AnimatorState firewall = GetOrCreateState(stateMachine, "Firewall", LoadClip("Attack02Start.fbx", "Attack02Start"));
            AnimatorState heal = FindState(stateMachine, "PotionDrink") ?? GetOrCreateState(stateMachine, "Heal",
                LoadClip("PotionDrink.fbx", "PotionDrink"));
            AnimatorState defAttack = FindState(stateMachine, "DefendHit") ??
                                      GetOrCreateState(stateMachine, "DefencedAttack",
                                          LoadClip("DefendHit.fbx", "DefendHit"));
            AnimatorState dead = GetOrCreateState(stateMachine, "Dead", LoadClip("Die.fbx", "Die"));

            AddBoolTransition(idle, FindState(stateMachine, "WalkForward"), "IsWalking", true);
            AddBoolTransition(FindState(stateMachine, "WalkForward"), idle, "IsWalking", false);

            if (idle != null)
            {
                AddTriggerTransition(stateMachine, "Dash", dash, idle);
                AddTriggerTransition(stateMachine, "Firewall", firewall, idle);
                AddTriggerTransition(stateMachine, "Heal", heal, idle);
                AddTriggerTransition(stateMachine, "DefencedAttack", defAttack, idle);
                AddTriggerTransition(stateMachine, "GetHit", FindState(stateMachine, "GetHit"), idle);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        private static void AddParameterIfMissing(AnimatorController controller, string name,
            AnimatorControllerParameterType type)
        {
            if (controller.parameters.Any(p => p.name == name))
                return;

            controller.AddParameter(name, type);
        }

        private static AnimatorState FindState(AnimatorStateMachine machine, string name)
        {
            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state.name == name)
                    return child.state;
            }

            return null;
        }

        private static AnimatorState GetOrCreateState(AnimatorStateMachine machine, string name, Motion motion)
        {
            AnimatorState existing = FindState(machine, name);
            if (existing != null)
            {
                if (motion != null)
                    existing.motion = motion;
                return existing;
            }

            if (motion == null)
                return null;

            AnimatorState child = machine.AddState(name);
            child.motion = motion;
            return child;
        }

        private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
        {
            if (from == null || to == null)
                return;

            foreach (AnimatorStateTransition t in from.transitions)
            {
                if (t.destinationState == to)
                    return;
            }

            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.1f;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
        }

        private static void AddTriggerTransition(AnimatorStateMachine machine, string trigger, AnimatorState destination,
            AnimatorState returnState)
        {
            if (destination == null || returnState == null)
                return;

            foreach (AnimatorStateTransition t in machine.anyStateTransitions)
            {
                if (t.destinationState == destination)
                    return;
            }

            AnimatorStateTransition toAbility = machine.AddAnyStateTransition(destination);
            toAbility.hasExitTime = false;
            toAbility.duration = 0.05f;
            toAbility.AddCondition(AnimatorConditionMode.If, 0f, trigger);

            foreach (AnimatorStateTransition t in destination.transitions)
            {
                if (t.destinationState == returnState)
                    return;
            }

            AnimatorStateTransition back = destination.AddTransition(returnState);
            back.hasExitTime = true;
            back.exitTime = 0.9f;
            back.duration = 0.15f;
        }

        private static AnimationClip LoadClip(string fbxFile, string clipName)
        {
            string path = $"{AnimRoot}/{fbxFile}";
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is AnimationClip clip && clip.name == clipName)
                    return clip;
            }

            return null;
        }
    }
}
