using UnityEditor;
using UnityEngine;

// Drop this file anywhere under an "Editor" folder in your project,
// e.g. Assets/Editor/IndicatorDiagnostics.cs

public static class IndicatorDiagnostics
{
    // The end-of-clip (settled/held) local Z values, taken directly from the
    // .anim keyframe data for Active 1 / Active 2 / Active 3.
    private static readonly (string path, float endZ)[] IndicatorEndValues = new (string, float)[]
    {
        ("TeaDispenser1/Indicator1", 0.00991f),
        ("TeaDispenser2/Indicator2", 0.00591f),
        ("TeaDispenser3/Indicator3", 0.00591f),
    };

    [MenuItem("Tools/DrinkUp/Diagnose Tea Indicators")]
    public static void Diagnose()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null)
        {
            Debug.LogError("Select the TeaProvider prefab root (the object with the Animator component) in the Hierarchy first, then run this again.");
            return;
        }

        Debug.Log($"=== Diagnosing indicators under '{root.name}' ===");

        foreach (var (path, endZ) in IndicatorEndValues)
        {
            Transform indicator = root.transform.Find(path);
            if (indicator == null)
            {
                Debug.LogWarning($"[{path}] NOT FOUND under '{root.name}'. Check the hierarchy/name matches exactly.");
                continue;
            }

            Vector3 currentLocal = indicator.localPosition;

            // Simulate the position the animation will hold once the clip finishes,
            // without needing to actually play it.
            Vector3 simulatedLocal = new Vector3(currentLocal.x, currentLocal.y, endZ);
            Vector3 simulatedWorld = indicator.parent.TransformPoint(simulatedLocal);
            Vector3 currentWorld = indicator.position;

            // Gather renderers on the dispenser this indicator belongs to, to test
            // whether the simulated resting point falls inside the housing mesh.
            string dispenserName = path.Split('/')[0];
            Transform dispenser = root.transform.Find(dispenserName);
            Renderer[] renderers = dispenser != null
                ? dispenser.GetComponentsInChildren<Renderer>()
                : new Renderer[0];

            bool insideAny = false;
            string report = "";
            float nearestSurfaceDistance = float.MaxValue;

            foreach (var r in renderers)
            {
                if (r.gameObject == indicator.gameObject) continue; // skip the indicator's own renderer if any

                bool inside = r.bounds.Contains(simulatedWorld);
                float dist = r.bounds.SqrDistance(simulatedWorld);
                nearestSurfaceDistance = Mathf.Min(nearestSurfaceDistance, dist);
                report += $"\n    - {r.name}: bounds center={r.bounds.center}, size={r.bounds.size}, containsSimulatedPoint={inside}";
                if (inside) insideAny = true;
            }

            Debug.Log(
                $"[{path}]\n" +
                $"  Current authored local pos: {currentLocal}  (world: {currentWorld})\n" +
                $"  Simulated settled local pos (z={endZ}): {simulatedLocal}  (world: {simulatedWorld})\n" +
                $"  --> Falls inside a dispenser mesh renderer's bounds: {insideAny}" +
                $"{report}"
            );
        }

        Debug.Log("=== Done. 'containsSimulatedPoint = True' on a renderer means that indicator's settled position is inside/behind that mesh (likely invisible). 'False' across all renderers means it's sitting in open space with nothing to render against there instead. ===");
    }
}
