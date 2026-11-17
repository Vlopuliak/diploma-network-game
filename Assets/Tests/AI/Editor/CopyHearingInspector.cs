using TheCopy.AI;
using UnityEditor;
using UnityEngine;

namespace TheCopy.Tests
{
    [CustomEditor(typeof(CopyHearing))]
    internal sealed class CopyHearingInspector : Editor
    {
        private const string AutoLogKey = "TheCopy.P305.AutoLog";
        private const string LogIntervalKey = "TheCopy.P305.LogInterval";
        private double _nextLogTime;

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying)
                return;

            var hearing = (CopyHearing)target;
            EditorGUILayout.LabelField("NGO role", hearing.IsServer ? "Server" : "Client");
            if (hearing.GetComponent<CopyPerception>() != null)
                EditorGUILayout.LabelField("Observation schema", CopyPerceptionSnapshot.SchemaId);
            string live = Describe(hearing.TryGetLiveSound(out var current), current);
            string memory = Describe(hearing.TryGetRememberedSound(out var last), last);
            EditorGUILayout.HelpBox($"Live: {live}\nMemory: {memory}", MessageType.Info);
            var light = hearing.GetComponent<CopyLightPerception>();
            var context = hearing.GetComponent<CopySceneContext>();
            if (light != null)
                EditorGUILayout.HelpBox("Light: " + DescribeLight(light), MessageType.Info);
            if (context != null)
                EditorGUILayout.HelpBox("Context: " + DescribeContext(context), MessageType.Info);

            bool autoLog = EditorGUILayout.Toggle("Auto log P3-05 evidence", SessionState.GetBool(AutoLogKey, true));
            float interval = Mathf.Max(0.1f, EditorGUILayout.FloatField(
                "Log interval (seconds)", SessionState.GetFloat(LogIntervalKey, 1f)));
            SessionState.SetBool(AutoLogKey, autoLog);
            SessionState.SetFloat(LogIntervalKey, interval);
            EditorGUILayout.HelpBox("Automatic logs run on the spawned server NPC while this Inspector is visible. " +
                "Lock this Inspector to keep logging when selecting other objects.", MessageType.Info);

            if (autoLog && hearing.IsServer && hearing.IsSpawned &&
                Event.current.type == EventType.Repaint && EditorApplication.timeSinceStartup >= _nextLogTime)
            {
                _nextLogTime = EditorApplication.timeSinceStartup + interval;
                LogEvidence(hearing, live, memory, "AUTO");
            }

            if (GUILayout.Button("Log P3-05 evidence"))
                LogEvidence(hearing, live, memory, "MANUAL");
        }

        private static void LogEvidence(CopyHearing hearing, string live, string memory, string mode)
        {
            string roster = hearing.IsServer && hearing.NetworkManager != null
                ? string.Join(", ", hearing.NetworkManager.ConnectedClientsIds)
                : "server only";
            string extra = "";
            var light = hearing.GetComponent<CopyLightPerception>();
            var context = hearing.GetComponent<CopySceneContext>();
            if (light != null)
                extra += " light=[" + DescribeLight(light) + "]";
            if (context != null)
                extra += " context=[" + DescribeContext(context) + "]";
            Debug.Log($"[P3-05 {mode}] time={Time.time:F2} server={hearing.IsServer} " +
                $"connectedClientIds=[{roster}] live=[{live}] memory=[{memory}]" + extra, hearing);
        }

        private static string DescribeLight(CopyLightPerception sensor)
        {
            if (!sensor.TryGetStrongestLight(out var light))
                return "none";
            return $"sourceClientId={light.SourceClientId} position={light.Position:F2} " +
                $"strength={light.Strength:F3} approximateAim={light.IsAimApproximate}";
        }

        private static string DescribeContext(CopySceneContext sensor)
        {
            if (!sensor.TryRead(out var state))
                return "unavailable";
            return $"zone={Fact(state.ZoneTriggered)} power={Fact(state.PowerOn)} " +
                $"airlock={Fact(state.AirlockOpen)} keypad={Fact(state.KeypadUnlocked)}";
        }

        private static string Fact(bool? value) => value.HasValue ? value.Value.ToString() : "unknown";

        private static string Describe(bool available, CopySoundObservation sound)
        {
            if (!available)
                return "none";

            string id = sound.SourceClientId?.ToString() ?? "anonymous";
            return $"{sound.Source} sourceClientId={id} position={sound.Position:F2} " +
                $"strength={sound.Strength:F3} freshness={sound.Freshness:F3}";
        }
    }
}
