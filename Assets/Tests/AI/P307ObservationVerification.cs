#if UNITY_EDITOR
using System;
using System.Collections;
using TheCopy.AI;
using TheCopy.Shared;
using Unity.Netcode;
using UnityEngine;

namespace TheCopy.Tests
{
    internal static class P307ObservationVerification
    {
        private const string MissingSensorsName = "P3-07 Missing Sensors";
        private const string UnboundContextName = "P3-07 Unbound Context";

        internal static bool Requested => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-p307Observations");

        internal static void PrepareScene()
        {
            new GameObject(MissingSensorsName).AddComponent<CopyPerception>();
            var unbound = new GameObject(UnboundContextName);
            unbound.AddComponent<CopySceneContext>();
            unbound.AddComponent<CopyPerception>();
        }

        internal static IEnumerator Verify(NetworkManager host, CopyHearing hearing)
        {
            VerifyMemoryBoundaries();
            var reader = hearing.GetComponent<CopyPerception>();
            var light = hearing.GetComponent<CopyLightPerception>();
            var context = hearing.GetComponent<CopySceneContext>();
            var player = host.LocalClient.PlayerObject;
            var beam = player.GetComponentInChildren<Light>();
            player.transform.SetPositionAndRotation(new Vector3(100, 1, 100), Quaternion.identity);
            hearing.transform.position = player.transform.position + Vector3.left * 3f - Vector3.up * 0.6f;
            ClearMemory(hearing);
            Expect(CopyPerceptionSnapshot.SchemaId == "the-copy.perception/1", "schema identity is fixed");
            Expect(reader.TryCapture(out var empty) && !empty.LiveSound.HasValue && !empty.RememberedSound.HasValue,
                "initial snapshot has no fabricated sound");
            Expect(empty.Context.HasValue && empty.Context.Value.ZoneTriggered == true &&
                empty.Context.Value.PowerOn == true && empty.Context.Value.AirlockOpen == true &&
                empty.Context.Value.KeypadUnlocked == true,
                "snapshot includes actual scene facts after scenario interactions");

            hearing.transform.position = player.transform.position + Vector3.left * 18f - Vector3.up * 0.6f;
            Publish(host);
            Expect(reader.TryCapture(out var boundary) && !boundary.RememberedSound.HasValue,
                "sound at the exact 18-metre radius is rejected");
            hearing.transform.position = player.transform.position + Vector3.left * 18.01f - Vector3.up * 0.6f;
            Publish(host);
            Expect(reader.TryCapture(out var outside) && !outside.RememberedSound.HasValue,
                "sound beyond hearing radius is rejected");
            hearing.transform.position = player.transform.position + Vector3.left * 17.99f - Vector3.up * 0.6f;
            Publish(host);
            Expect(reader.TryCapture(out var inside) && inside.LiveSound.HasValue && inside.LiveSound.Value.Strength > 0f,
                "sound just inside hearing radius remains audible");

            ClearMemory(hearing);
            hearing.transform.position = player.transform.position + Vector3.left * 3f - Vector3.up * 0.6f;
            hearing.OnNoise(new NoiseEvent { Position = player.transform.position, Loudness = 0f });
            hearing.OnNoise(new NoiseEvent { Position = player.transform.position, Loudness = -1f });
            hearing.OnNoise(new NoiseEvent { Position = new Vector3(float.NaN, 1, 0), Loudness = 1f });
            Expect(reader.TryCapture(out var invalid) && !invalid.RememberedSound.HasValue,
                "zero, negative, and invalid-position sound do not create observations");

            Publish(host);
            Expect(reader.TryCapture(out var first) && first.LiveSound.Value.SourceClientId == host.LocalClientId &&
                first.RememberedSound.Value.Freshness == 1f && first.CapturedAt == Time.timeAsDouble,
                "snapshot preserves host ID zero, freshness, and game timestamp");
            hearing.OnNoise(new NoiseEvent
            {
                Position = player.transform.position,
                Loudness = 0.5f,
                Source = NoiseSource.Environmental
            });
            Expect(reader.TryCapture(out var replacement) && !replacement.RememberedSound.Value.SourceClientId.HasValue &&
                replacement.RememberedSound.Value.Source == NoiseSource.Environmental &&
                first.RememberedSound.Value.Source == NoiseSource.PlayerVoice &&
                first.RememberedSound.Value.SourceClientId == host.LocalClientId,
                "new anonymous sound replaces memory without mutating the previous snapshot");

            ClearMemory(hearing);
            Publish(host);
            float previousScale = Time.timeScale;
            Time.timeScale = 0f;
            try
            {
                yield return new WaitForSecondsRealtime(0.25f);
                Expect(reader.TryCapture(out var paused) && paused.LiveSound.HasValue &&
                    paused.RememberedSound.Value.Freshness == 1f,
                    "scaled game-time memory freezes when timeScale is zero");
            }
            finally
            {
                Time.timeScale = previousScale;
            }
            yield return new WaitForSeconds(0.2f);
            Expect(reader.TryCapture(out var remembered) && !remembered.LiveSound.HasValue &&
                remembered.RememberedSound.HasValue && remembered.RememberedSound.Value.Freshness < 1f,
                "live window ends before four-second memory in the real scene");
            yield return new WaitForSeconds(4.05f);
            Expect(reader.TryCapture(out var expired) && !expired.LiveSound.HasValue && !expired.RememberedSound.HasValue,
                "real-scene snapshot loses sound after configured memory lifetime");

            hearing.transform.position = beam.transform.position + beam.transform.forward * beam.range - Vector3.up * 0.6f;
            Physics.SyncTransforms();
            Expect(reader.TryCapture(out var lightBoundary) && !lightBoundary.Light.HasValue,
                "light at its exact range is absent");
            hearing.transform.position = beam.transform.position + beam.transform.forward * (beam.range - 0.01f) - Vector3.up * 0.6f;
            Physics.SyncTransforms();
            Expect(reader.TryCapture(out var lightInside) && lightInside.Light.HasValue && lightInside.Light.Value.Strength > 0f,
                "light just inside its range is present");
            hearing.transform.position = beam.transform.position +
                Quaternion.AngleAxis(beam.spotAngle * 0.5f - 1f, Vector3.up) * beam.transform.forward * 3f - Vector3.up * 0.6f;
            Physics.SyncTransforms();
            Expect(reader.TryCapture(out var coneInside) && coneInside.Light.HasValue, "point one degree inside beam cone is lit");
            hearing.transform.position = beam.transform.position +
                Quaternion.AngleAxis(beam.spotAngle * 0.5f + 1f, Vector3.up) * beam.transform.forward * 3f - Vector3.up * 0.6f;
            Physics.SyncTransforms();
            Expect(reader.TryCapture(out var coneOutside) && !coneOutside.Light.HasValue, "point one degree outside beam cone is unlit");
            hearing.transform.position = beam.transform.position - Vector3.up * 0.6f;
            Physics.SyncTransforms();
            Expect(reader.TryCapture(out var atOrigin) && atOrigin.Light.HasValue && atOrigin.Light.Value.Strength == 1f,
                "coincident light origin produces a finite full-strength observation");

            context.enabled = false;
            light.enabled = false;
            hearing.enabled = false;
            Expect(reader.TryCapture(out var unavailable) && !unavailable.Context.HasValue && !unavailable.Light.HasValue &&
                !unavailable.LiveSound.HasValue && !unavailable.RememberedSound.HasValue,
                "disabled sensors produce explicit missing observations");
            context.enabled = true;
            light.enabled = true;
            hearing.enabled = true;
            Expect(reader.TryCapture(out var resumed) && resumed.Context.HasValue && !resumed.RememberedSound.HasValue,
                "re-enabled hearing does not restore cleared sound");
            reader.enabled = false;
            Expect(!reader.TryCapture(out _), "disabled snapshot reader refuses capture");
            reader.enabled = true;

            var bare = GameObject.Find(MissingSensorsName).GetComponent<CopyPerception>();
            Expect(bare.TryCapture(out var missing) && !missing.Context.HasValue && !missing.Light.HasValue &&
                !missing.LiveSound.HasValue && !missing.RememberedSound.HasValue,
                "missing sensor components remain distinguishable from false scene facts");
            bare.NetworkObject.Despawn();
            var unboundContext = GameObject.Find(UnboundContextName).GetComponent<CopySceneContext>();
            var unboundReader = unboundContext.GetComponent<CopyPerception>();
            Expect(unboundContext.IsSpawned && unboundReader.TryCapture(out var unbound) && unbound.Context.HasValue &&
                !unbound.Context.Value.ZoneTriggered.HasValue && !unbound.Context.Value.PowerOn.HasValue &&
                !unbound.Context.Value.AirlockOpen.HasValue && !unbound.Context.Value.KeypadUnlocked.HasValue,
                "available context with unbound scene sources preserves unknown facts");
            unboundContext.NetworkObject.Despawn();

            Publish(host);
            hearing.NetworkObject.Despawn(false);
            Expect(!reader.TryCapture(out _), "despawned reader refuses capture");
            hearing.NetworkObject.Spawn();
            Expect(reader.TryCapture(out var respawned) && !respawned.RememberedSound.HasValue,
                "respawn does not leak previous-lifetime sound into the new snapshot");
            Debug.Log("[P3-07] COMPLETE: perception schema and real-scene boundaries");
        }

        private static void VerifyMemoryBoundaries()
        {
            var memory = new CopySoundMemory();
            var sound = new NoiseEvent { Position = Vector3.one, Loudness = 0.8f, Source = NoiseSource.PlayerVoice };
            Expect(!memory.TryRecall(0, 4f, out _), "empty memory has no observation");
            memory.Remember(sound, 0.8f, 0, 0);
            double liveBoundary = 0.18f;
            Expect(memory.TryGetLive(liveBoundary - 0.000001, 0.18f, out _), "live signal exists immediately before 0.18 seconds");
            Expect(!memory.TryGetLive(liveBoundary, 0.18f, out _), "live signal is absent at exactly 0.18 seconds");
            Expect(memory.TryRecall(2, 4f, out var half) && half.Freshness == 0.5f && Mathf.Abs(half.Strength - 0.4f) < 0.000001f,
                "half-life produces half freshness and strength");
            Expect(memory.TryRecall(3.9999, 4f, out _), "memory exists immediately before TTL");
            Expect(!memory.TryRecall(4, 4f, out _), "memory is absent at exactly TTL");
            Expect(!memory.TryRecall(5, 4f, out _), "expired memory stays absent after TTL");
            memory.Clear();
            Expect(!memory.TryGetLive(0, 0.18f, out _) && !memory.TryRecall(0, 4f, out _), "clear removes live and remembered sound");
        }

        private static void ClearMemory(CopyHearing hearing)
        {
            hearing.enabled = false;
            hearing.enabled = true;
        }

        private static void Publish(NetworkManager host)
        {
            if (!CopyPlayerNoise.TryPublish(host, host.LocalClientId, NoiseSource.PlayerVoice, 0.8f))
                throw new InvalidOperationException("Host acoustic input rejected");
        }

        private static void Expect(bool condition, string label)
        {
            if (!condition)
                throw new InvalidOperationException(label);
            Debug.Log("[P3-07] PASS " + label);
        }
    }
}
#endif
