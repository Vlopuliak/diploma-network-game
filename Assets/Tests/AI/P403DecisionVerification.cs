#if UNITY_EDITOR
using System;
using TheCopy.AI;
using TheCopy.Shared;
using Unity.Netcode;
using UnityEngine;

namespace TheCopy.Tests
{
    internal static class P403DecisionVerification
    {
        internal static bool Requested => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-p403DecisionInput");

        internal static void Verify(NetworkManager host, CopyHearing hearing)
        {
            VerifyEncoder();
            var input = hearing.GetComponent<CopyDecisionInput>();
            var perception = hearing.GetComponent<CopyPerception>();
            var light = hearing.GetComponent<CopyLightPerception>();
            var player = host.LocalClient.PlayerObject;
            var values = new float[CopyDecisionEncoder.ObservationCount];
            light.enabled = false;
            hearing.enabled = false;
            hearing.enabled = true;
            Expect(input.TryCapture(values, out var quiet) && OnlyContinue(quiet) && values[0] == 0f && values[12] == 0f && values[24] == 0f,
                "real-scene quiet input permits only Continue");
            Expect(values[32] == 1f && values[33] == 1f && values[34] == 1f && values[35] == 1f &&
                values[36] == 1f && values[37] == 1f && values[38] == 1f && values[39] == 1f,
                "real scene context maps to the four documented known/value pairs");

            hearing.transform.position = player.transform.position + Vector3.left * 3f - Vector3.up * 0.6f;
            Expect(CopyPlayerNoise.TryPublish(host, host.LocalClientId, NoiseSource.PlayerFootstep, 0.65f), "host sound accepted for input integration");
            Vector3 heardPosition = player.transform.position;
            Expect(input.TryCapture(values, out var heard) && heard.TryGetTarget(CopyDecisionAction.InvestigateSound, out var target) &&
                target.Position == heardPosition && target.SourceClientId == host.LocalClientId && values[20] == 1f,
                "actual sound produces a remembered-position target and footstep one-hot");
            var beforeMovement = (float[])values.Clone();
            player.transform.position += Vector3.right * 1000f;
            Expect(input.TryCapture(values, out var hiddenMovement) && Equal(values, beforeMovement) &&
                hiddenMovement.TryGetTarget(CopyDecisionAction.InvestigateSound, out var oldTarget) && oldTarget.Position == heardPosition,
                "unheard player movement does not leak into observations or sound target");

            light.enabled = true;
            var beam = player.GetComponentInChildren<Light>();
            hearing.transform.position = beam.transform.position + beam.transform.forward * 2f - Vector3.up * 0.6f;
            Physics.SyncTransforms();
            Expect(input.TryCapture(values, out var illuminated) && illuminated.IsAllowed(CopyDecisionAction.InvestigateLight) &&
                illuminated.TryGetTarget(CopyDecisionAction.InvestigateLight, out var lightTarget) &&
                lightTarget.Position == beam.transform.position && values[31] == 1f,
                "actually perceived flashlight produces a target with source relationship");

            input.enabled = false;
            Expect(!input.TryCapture(values, out _) && Zero(values), "disabled input clears caller buffer and refuses capture");
            input.enabled = true;
            perception.enabled = false;
            values[0] = 1f;
            Expect(!input.TryCapture(values, out _) && Zero(values), "missing capture clears previous numeric observations");
            perception.enabled = true;
            Expect(input.TryCapture(values, out _), "input recovers after perception is re-enabled");
            Debug.Log("[P4-03] COMPLETE: draft numeric observations and sensory candidates");
        }

        private static void VerifyEncoder()
        {
            var values = new float[40];
            Expect(CopyDecisionEncoder.SchemaId == "the-copy.decision-input/draft-1" &&
                CopyDecisionEncoder.ObservationCount == 40 && CopyDecisionCandidates.ActionCount == 7 &&
                (int)CopyDecisionAction.InvestigateSound == 1 && (int)CopyDecisionAction.ObstructMechanism == 6,
                "draft schema size and legacy action IDs are fixed");
            Expect(CopyDecisionEncoder.TryEncode(default, Vector3.zero, Quaternion.identity, values, out var quiet) &&
                Zero(values) && OnlyContinue(quiet) && !quiet.IsAllowed((CopyDecisionAction)99),
                "empty input is zero and cannot expose unsupported targets");
            var memory = new CopySoundObservation(Vector3.right * 18f, NoiseSource.PlayerFootstep, 0.6f, 0.5f, ulong.MaxValue);
            var light = new CopyLightObservation(ulong.MaxValue, Vector3.forward * 18f, 0.4f, false);
            var context = new CopySceneSnapshot(true, false, null, true);
            var snapshot = new CopyPerceptionSnapshot(10, null, memory, light, context);
            Expect(CopyDecisionEncoder.TryEncode(snapshot, Vector3.zero, Quaternion.identity, values, out var candidates), "valid snapshot encodes");
            Expect(values[12] == 1f && values[13] == 0.6f && values[14] == 0.5f && values[15] == 1f &&
                values[16] == 0f && values[17] == 0f && values[18] == 1f && values[19] == 0f &&
                values[20] == 1f && values[21] == 0f && values[22] == 0f && values[23] == 1f,
                "memory block order, distance, and noise one-hot match contract");
            Expect(values[24] == 1f && values[25] == 0.4f && values[26] == 0f && values[27] == 0f &&
                values[28] == 1f && values[29] == 1f && values[30] == 0f && values[31] == 1f,
                "light block order and same-source flag match contract");
            Expect(values[32] == 1f && values[33] == 1f && values[34] == 1f && values[35] == 0f &&
                values[36] == 0f && values[37] == 0f && values[38] == 1f && values[39] == 1f,
                "known true, known false, and unknown facts have distinct encodings");
            Expect(candidates.CapturedAt == 10 && candidates.TryGetTarget(CopyDecisionAction.InvestigateSound, out var soundTarget) &&
                soundTarget.Position == memory.Position && soundTarget.SourceClientId == ulong.MaxValue &&
                candidates.TryGetTarget(CopyDecisionAction.InvestigateLight, out var lightTarget) && lightTarget.Position == light.Position,
                "world-space targets preserve full-width IDs as metadata");
            Expect(!candidates.IsAllowed(CopyDecisionAction.ObservePlayerA) && !candidates.IsAllowed(CopyDecisionAction.ObservePlayerB) &&
                !candidates.IsAllowed(CopyDecisionAction.AssistMechanism) && !candidates.IsAllowed(CopyDecisionAction.ObstructMechanism) &&
                candidates.IsAllowed(CopyDecisionAction.Continue) && !candidates.TryGetTarget(CopyDecisionAction.Continue, out _),
                "unimplemented player and mechanism actions remain masked");
            var reference = (float[])values.Clone();
            var translated = new CopyPerceptionSnapshot(1000000, null,
                new CopySoundObservation(memory.Position + Vector3.one * 10f, memory.Source, memory.Strength, memory.Freshness, 0),
                new CopyLightObservation(0, light.Position + Vector3.one * 10f, light.Strength, false), context);
            Expect(CopyDecisionEncoder.TryEncode(translated, Vector3.one * 10f, Quaternion.identity, values, out _) && Equal(values, reference),
                "translation, absolute time, and client-ID renumbering do not change numeric input");
            Quaternion heading = Quaternion.Euler(0, 90, 0);
            var rotated = new CopyPerceptionSnapshot(10, null,
                new CopySoundObservation(heading * memory.Position, memory.Source, memory.Strength, memory.Freshness, ulong.MaxValue),
                new CopyLightObservation(ulong.MaxValue, heading * light.Position, light.Strength, false), context);
            Expect(CopyDecisionEncoder.TryEncode(rotated, Vector3.zero, heading, values, out _) && Equal(values, reference),
                "observer-local coordinates preserve rotational invariance");

            var live = new CopySoundObservation(Vector3.forward, NoiseSource.PlayerVoice, 0.8f, 1f, 0);
            Expect(CopyDecisionEncoder.TryEncode(new CopyPerceptionSnapshot(1, live, null, null, null), Vector3.zero, Quaternion.identity, values, out var liveOnly) &&
                values[0] == 1f && values[1] == 0.8f && values[2] == 1f && values[7] == 1f && values[11] == 1f &&
                values[12] == 0f && liveOnly.IsAllowed(CopyDecisionAction.InvestigateSound), "live-only sound has a target and host ID is known");
            var anonymous = new CopySoundObservation(Vector3.forward, NoiseSource.Environmental, 0.2f, 1f, null);
            Expect(CopyDecisionEncoder.TryEncode(new CopyPerceptionSnapshot(1, null, anonymous,
                new CopyLightObservation(0, Vector3.one, 0.5f, true), null), Vector3.zero, Quaternion.identity, values, out var approximate) &&
                values[23] == 0f && values[24] == 1f && values[30] == 1f && values[31] == 0f &&
                approximate.IsAllowed(CopyDecisionAction.InvestigateSound) && !approximate.IsAllowed(CopyDecisionAction.InvestigateLight),
                "anonymous sound remains usable and approximate light cannot create an exact target");
            var far = new CopySoundObservation(Vector3.right * 100000f, NoiseSource.PhysicalImpact, 0.1f, 0.1f, null);
            Expect(CopyDecisionEncoder.TryEncode(new CopyPerceptionSnapshot(1, null, far, null, null), Vector3.zero, Quaternion.identity, values, out _) &&
                values[15] == 1f && values[18] == 1f, "far remembered positions saturate at normalized limits");
            Expect(CopyDecisionEncoder.TryEncode(default, Vector3.zero, Quaternion.identity, values, out var cleared) && Zero(values) && OnlyContinue(cleared),
                "reused buffer and candidates lose expired stimuli");

            Expect(Reject(new CopyPerceptionSnapshot(double.NaN, null, null, null, null)), "invalid timestamp rejected with cleared output");
            Expect(Reject(new CopyPerceptionSnapshot(1, null, new CopySoundObservation(Vector3.one, NoiseSource.PlayerVoice, float.NaN, 1, 0), null, null)),
                "non-finite sound strength rejected");
            Expect(Reject(new CopyPerceptionSnapshot(1, null, new CopySoundObservation(Vector3.one, NoiseSource.PlayerVoice, 1, float.PositiveInfinity, 0), null, null)),
                "invalid freshness rejected");
            Expect(Reject(new CopyPerceptionSnapshot(1, null, new CopySoundObservation(Vector3.one, (NoiseSource)255, 1, 1, 0), null, null)),
                "unknown sound category rejected");
            Expect(Reject(new CopyPerceptionSnapshot(1, null, null, new CopyLightObservation(0, new Vector3(float.NaN, 0, 0), 1, false), null)),
                "invalid light position rejected");
            values[0] = 1;
            Expect(!CopyDecisionEncoder.TryEncode(default, Vector3.zero, new Quaternion(0, 0, 0, 0), values, out _) && Zero(values),
                "invalid observer rotation rejected");
            bool wrongLength = false;
            try { CopyDecisionEncoder.TryEncode(default, Vector3.zero, Quaternion.identity, new float[27], out _); }
            catch (ArgumentException) { wrongLength = true; }
            Expect(wrongLength, "legacy 27-value buffer cannot silently consume the new layout");
        }

        private static bool Reject(CopyPerceptionSnapshot snapshot)
        {
            var values = new float[40];
            Array.Fill(values, 1f);
            return !CopyDecisionEncoder.TryEncode(snapshot, Vector3.zero, Quaternion.identity, values, out _) && Zero(values);
        }

        private static bool OnlyContinue(CopyDecisionCandidates candidates)
        {
            for (int action = 1; action < CopyDecisionCandidates.ActionCount; action++)
                if (candidates.IsAllowed((CopyDecisionAction)action))
                    return false;
            return candidates.IsAllowed(CopyDecisionAction.Continue);
        }

        private static bool Zero(float[] values)
        {
            foreach (float value in values)
                if (value != 0f)
                    return false;
            return true;
        }

        private static bool Equal(float[] actual, float[] expected)
        {
            for (int index = 0; index < actual.Length; index++)
                if (!float.IsFinite(actual[index]) || Mathf.Abs(actual[index] - expected[index]) > 0.00001f)
                    return false;
            return true;
        }

        private static void Expect(bool condition, string label)
        {
            if (!condition)
                throw new InvalidOperationException(label);
            Debug.Log("[P4-03] PASS " + label);
        }
    }
}
#endif
