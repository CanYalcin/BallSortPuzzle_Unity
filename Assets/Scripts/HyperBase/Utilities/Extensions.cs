using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HyperBase.Utilities
{
    /// <summary>General-purpose extension methods used across the project.</summary>
    public static class Extensions
    {
        public static void DestroyChildren(this Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Object.Destroy(t.GetChild(i).gameObject);
        }

        public static void ResetLocal(this Transform t)
        {
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale    = Vector3.one;
        }

        public static T GetOrAdd<T>(this GameObject go) where T : Component
            => go.TryGetComponent<T>(out var c) ? c : go.AddComponent<T>();

        public static T Random<T>(this IList<T> list)
            => list[UnityEngine.Random.Range(0, list.Count)];

        public static void Shuffle<T>(this IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public static float Remap(this float v, float inMin, float inMax, float outMin, float outMax)
            => outMin + (v - inMin) / (inMax - inMin) * (outMax - outMin);

        public static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);

        public static void StretchToParent(this RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static string ToShortNumber(this int n)
        {
            if (n >= 1_000_000) return $"{n / 1_000_000f:F1}M";
            if (n >= 1_000)     return $"{n / 1_000f:F1}K";
            return n.ToString();
        }

        public static void SetInteractable(this CanvasGroup cg, bool v)
        {
            cg.interactable   = v;
            cg.blocksRaycasts = v;
        }
    }
}
