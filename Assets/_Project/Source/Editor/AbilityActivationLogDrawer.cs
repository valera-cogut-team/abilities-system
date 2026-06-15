using System.Text;
using AvantajPrim.Abilities.Domain;
using AvantajPrim.Abilities.Execution;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    internal static class AbilityActivationLogDrawer
    {
        public static void Draw(AbilityActivationLog log, ref Vector2 scroll, AbilityId replayAbilityId = default)
        {
            if (log == null)
            {
                EditorGUILayout.HelpBox("Activation log is not available.", MessageType.Info);
                return;
            }

            if (log.Casts.Count == 0 && log.Frames.Count == 0)
            {
                EditorGUILayout.HelpBox("No activation log yet. Cast an ability in Play Mode.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField("Activation Log", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Casts", log.Casts.Count.ToString());
            EditorGUILayout.LabelField("Component Frames", log.Frames.Count.ToString());

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(120f), GUILayout.MaxHeight(220f));
            string text = BuildLogText(log);
            EditorGUILayout.TextArea(text, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Replay Last Cast"))
                AvantajPrimProjectTools.ReplayLastCastFromMenu();

            if (!string.IsNullOrEmpty(replayAbilityId.Value) &&
                GUILayout.Button($"Replay '{replayAbilityId.Value}'"))
            {
                ReplayByAbilityId(replayAbilityId);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static string BuildLogText(AbilityActivationLog log)
        {
            var sb = new StringBuilder(512);

            for (int i = 0; i < log.Casts.Count; i++)
            {
                AbilityActivationCast cast = log.Casts[i];
                sb.Append('[').Append(cast.Time.ToString("0.00")).Append("s] CAST ")
                    .Append(cast.AbilityId.Value)
                    .Append(" caster=").Append(cast.CasterId.Value)
                    .Append(" targets=");

                for (int t = 0; t < cast.TargetIds.Length; t++)
                {
                    if (t > 0)
                        sb.Append(',');
                    sb.Append(cast.TargetIds[t].Value);
                }

                sb.AppendLine();
            }

            for (int i = 0; i < log.Frames.Count; i++)
            {
                AbilityReplayFrame frame = log.Frames[i];
                sb.Append("  [").Append(frame.Time.ToString("0.00")).Append("s] ")
                    .Append(frame.ComponentTypeName)
                    .Append(" → target ").Append(frame.TargetId.Value)
                    .AppendLine();
            }

            return sb.ToString();
        }

        private static void ReplayByAbilityId(AbilityId abilityId)
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[AvantajPrim] Replay requires Play Mode.");
                return;
            }

            AbilityActivationReplayService replay = AbilityEditorPlayAccess.Replay;
            if (replay == null)
            {
                Debug.LogWarning("[AvantajPrim] AbilityActivationReplayService is not available.");
                return;
            }

            replay.ReplayByAbilityIdAsync(abilityId).Forget();
            Debug.Log($"[AvantajPrim] Replaying last cast of '{abilityId.Value}'.");
        }
    }
}
