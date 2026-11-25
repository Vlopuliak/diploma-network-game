using System;

namespace TheCopy.AI
{
    // Training criterion only. Never selects the gameplay NPC's action.
    public static class CopyAttentionReward
    {
        public const string ProfileId = "the-copy.expert-attention/1";

        public static bool TryEvaluate(float[] observations, bool[] allowed, int action, out float reward)
        {
            reward = 0;
            if (!Valid(observations, allowed)) return false;
            var utility = new float[7];
            bool sensory = allowed[1] || allowed[2] || allowed[3] || allowed[4];
            utility[0] = sensory ? 0 : 1;
            if (allowed[1]) utility[1] = observations[12] == 1 ? observations[13] : observations[1];
            if (allowed[2]) utility[2] = observations[25];
            if (allowed[3]) utility[3] = 0.01f + 0.99f * (1 - observations[44]);
            if (allowed[4]) utility[4] = 0.01f + 0.99f * (1 - observations[49]);
            float max = 0;
            for (int i = 0; i < 7; i++) if (allowed[i]) max = Math.Max(max, utility[i]);
            reward = action < 0 || action >= 7 || !allowed[action] ? -1 : utility[action] / max;
            return true;
        }

        private static bool Valid(float[] v, bool[] allowed)
        {
            if (v == null || v.Length != 50 || allowed == null || allowed.Length != 7) return false;
            for (int i = 0; i < 50; i++)
            {
                bool signed = i >= 3 && i <= 5 || i >= 15 && i <= 17 || i >= 26 && i <= 28 ||
                    i >= 41 && i <= 43 || i >= 46 && i <= 48;
                if (!float.IsFinite(v[i]) || v[i] < (signed ? -1 : 0) || v[i] > 1) return false;
            }
            foreach (int i in new[] { 0, 7, 8, 9, 10, 11, 12, 19, 20, 21, 22, 23, 24, 30, 31,
                32, 33, 34, 35, 36, 37, 38, 39, 40, 45 })
                if (v[i] != 0 && v[i] != 1) return false;
            foreach (var block in new[] { (0, 12), (12, 24), (24, 31), (40, 45), (45, 50) })
                if (v[block.Item1] == 0)
                    for (int i = block.Item1 + 1; i < block.Item2; i++) if (v[i] != 0) return false;
            foreach (int offset in new[] { 0, 12 })
                if (v[offset] == 1 && (v[offset + 1] <= 0 || v[offset + 2] <= 0 ||
                    v[offset + 7] + v[offset + 8] + v[offset + 9] + v[offset + 10] != 1)) return false;
            if (v[24] == 1 && v[25] <= 0) return false;
            foreach (int i in new[] { 32, 34, 36, 38 }) if (v[i] == 0 && v[i + 1] != 0) return false;
            int sound = v[12] == 1 ? 12 : 0;
            if (v[31] == 1 && !(v[sound] == 1 && v[sound + 11] == 1 && v[24] == 1)) return false;
            return allowed[0] && allowed[1] == (v[0] == 1 || v[12] == 1) &&
                allowed[2] == (v[24] == 1 && v[30] == 0) && allowed[3] == (v[40] == 1) &&
                allowed[4] == (v[45] == 1) && !allowed[5] && !allowed[6];
        }
    }
}
