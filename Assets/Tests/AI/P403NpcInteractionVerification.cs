#if UNITY_EDITOR
using System;
using System.Collections;
using TheCopy.AI;
using TheCopy.Shared;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace TheCopy.Tests
{
    internal static class P403NpcInteractionVerification
    {
        public static bool Requested => Array.Exists(Environment.GetCommandLineArgs(), a => a == "-p403NpcInteraction");

        public static void Prepare(GameObject probe, NetworkInteractiveObject power)
        {
            var adapter = probe.AddComponent<CopyPowerInteraction>();
            var settings = new SerializedObject(adapter);
            settings.FindProperty("target").objectReferenceValue = power;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        public static IEnumerator Verify(NetworkManager host, CopyHearing probe, NetworkInteractiveObject power,
            NetworkAirlockDoor door, CopySceneContext context)
        {
            var adapter = probe.GetComponent<CopyPowerInteraction>();
            var npc = probe.NetworkObject;
            var player = host.LocalClient.PlayerObject;
            player.transform.position = power.transform.position + Vector3.right * 100f;
            probe.transform.position = power.transform.position + Vector3.left;
            Physics.SyncTransforms();
            yield return new WaitForSeconds(0.6f);

            int changes = 0;
            int noises = 0;
            Action<bool> onChange = _ => changes++;
            var listener = new NoiseListener(sound =>
            {
                if (sound.Source == NoiseSource.Environmental && sound.Position == power.transform.position)
                    noises++;
            });
            power.OnPowerStateChanged += onChange;
            NoiseDispatcher.Register(listener);
            try
            {
                bool desired = !power.IsActive.Value;
                Check(!adapter.TryGetCandidate(desired, out _) && !adapter.TryApply(desired), "scene permissions default to denied");
                SetPermission(adapter, desired, true);
                Check(adapter.TryGetCandidate(desired, out var candidate) && candidate.CanInteractNow &&
                    candidate.NetworkObjectId == power.NetworkObjectId && candidate.Position == power.transform.position &&
                    candidate.RequestedState == desired, "explicit target exposes actionable candidate");
                Check(!power.TrySetActiveFromNpc(null, desired), "null actor rejected");
                Check(!power.TrySetActiveFromNpc(player, desired), "player object rejected as NPC");
                var unspawned = new GameObject("Unspawned NPC").AddComponent<CopyPerception>();
                Check(!power.TrySetActiveFromNpc(unspawned.NetworkObject, desired), "unspawned actor rejected");
                UnityEngine.Object.Destroy(unspawned.gameObject);
                var perception = probe.GetComponent<CopyPerception>();
                perception.enabled = false;
                Check(!power.TrySetActiveFromNpc(npc, desired), "disabled NPC rejected");
                perception.enabled = true;
                probe.transform.position = power.transform.position + Vector3.left * 4.6f;
                Check(adapter.TryGetCandidate(desired, out candidate) && !candidate.CanInteractNow && !adapter.TryApply(desired),
                    "out-of-range candidate cannot execute");
                probe.transform.position = power.transform.position + Vector3.left;
                Check(changes == 0 && noises == 0, "rejected requests leave state and noise untouched");
                Check(adapter.TryApply(desired) && power.IsActive.Value == desired, "NPC uses own distance with player far away");
                Check(changes == 1 && noises == 1, "real change emits exactly one power event and relay noise");
                Check(door.IsDoorOpen.Value == desired && context.TryRead(out var state) && state.PowerOn == desired &&
                    state.AirlockOpen == desired, "NPC change updates linked door and perception context");
                Check(probe.TryGetRememberedSound(out var sound) && sound.Source == NoiseSource.Environmental,
                    "relay noise reaches NPC memory");
                Check(power.TrySetActiveFromNpc(npc, desired) && changes == 1 && noises == 1, "idempotent retry emits nothing");
                Check(!adapter.TryGetCandidate(desired, out _), "already satisfied target is not a candidate");
                Check(!power.TrySetActiveFromNpc(npc, !desired), "NPC reverse change respects cooldown");
                player.transform.position = power.transform.position + Vector3.right;
                power.Interact();
                Check(power.IsActive.Value == desired && changes == 1, "player shares NPC cooldown");
                yield return new WaitForSeconds(0.6f);
                power.Interact();
                Check(power.IsActive.Value == !desired && changes == 2 && noises == 2, "player still interacts after cooldown");
                Check(!power.TrySetActiveFromNpc(npc, desired), "NPC shares player cooldown");
                yield return new WaitForSeconds(0.6f);
                SetPermission(adapter, desired, false);
                Check(!adapter.TryApply(desired), "direction permission can be revoked");
                SetPermission(adapter, desired, true);
                Check(adapter.TryApply(desired), "NPC can interact again after player cooldown");
                adapter.enabled = false;
                Check(!adapter.TryGetCandidate(!desired, out _) && !adapter.TryApply(!desired), "disabled adapter rejects execution");
                adapter.enabled = true;
                var settings = new SerializedObject(adapter);
                settings.FindProperty("target").objectReferenceValue = null;
                settings.ApplyModifiedPropertiesWithoutUndo();
                Check(!adapter.TryGetCandidate(!desired, out _), "missing target clears candidate");
                Debug.Log("[P4-03 INTERACTION] COMPLETE: actual switch NPC integration");
            }
            finally
            {
                power.OnPowerStateChanged -= onChange;
                NoiseDispatcher.Unregister(listener);
            }
        }

        private static void SetPermission(CopyPowerInteraction adapter, bool active, bool allowed)
        {
            var settings = new SerializedObject(adapter);
            settings.FindProperty(active ? "allowPowerOn" : "allowPowerOff").boolValue = allowed;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            Debug.Log("[P4-03 INTERACTION] PASS " + label);
        }

        private sealed class NoiseListener : INoiseListener
        {
            private readonly Action<NoiseEvent> receive;
            public NoiseListener(Action<NoiseEvent> receive) { this.receive = receive; }
            public void OnNoise(NoiseEvent sound) => receive(sound);
        }
    }
}
#endif
