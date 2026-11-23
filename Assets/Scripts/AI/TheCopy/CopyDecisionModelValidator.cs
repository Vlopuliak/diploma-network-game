using Unity.InferenceEngine;

namespace TheCopy.AI
{
    public static class CopyDecisionModelValidator
    {
        // Structural input guard only. Training provenance and feature semantics need the paired contract.
        public static bool TryValidate(Model model, out string reason)
        {
            reason = "Expected exactly obs_0 and action_masks inputs";
            if (model == null || model.inputs.Count != 2) return false;
            bool observations = false, masks = false;
            foreach (var input in model.inputs)
            {
                int width;
                if (input.name == "obs_0" && !observations)
                { observations = true; width = CopyDecisionEncoderV2.ObservationCount; }
                else if (input.name == "action_masks" && !masks)
                { masks = true; width = CopyDecisionCandidatesV2.ActionCount; }
                else return false;
                reason = "Invalid float [batch, " + width + "] input: " + input.name;
                if (input.dataType != DataType.Float || input.shape.isRankDynamic || input.shape.rank != 2 ||
                    input.shape.Get(1) != width || (input.shape.Get(0) != -1 && input.shape.Get(0) != 1))
                    return false;
            }
            reason = "Missing ML-Agents output";
            foreach (string name in new[] { "discrete_actions", "discrete_action_output_shape", "version_number", "memory_size" })
                if (!model.outputs.Exists(output => output.name == name)) return false;
            reason = null;
            return observations && masks;
        }
    }
}
