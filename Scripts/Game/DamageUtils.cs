using System;
using System.Reflection;
using UnityEngine;

namespace Assets.Scripts
{
    public static class DamageUtils
    {
        private static readonly string[] CandidateNames =
            { "Damage", "damage", "Dano", "dano", "daño", "Daño", "damageAmount", "damage_value" };

        public static int GetDamageFrom(GameObject go)
        {
            if (go == null) return 0;

            // 1) Preferir IDamageSource (claro y explícito)
            if (go.TryGetComponent<IDamageSource>(out var ds))
                return Mathf.Max(0, ds.DamageAmount);

            var dsParent = go.GetComponentInParent<IDamageSource>();
            if (dsParent != null)
                return Mathf.Max(0, dsParent.DamageAmount);

            // 2) Reflexión en componentes (propiedades/fields con nombres habituales)
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                var t = comp.GetType();

                // Evitar interpretar componentes de salud/vida como fuentes de daño
                if (IsHealthLikeComponent(t)) continue;

                foreach (var name in CandidateNames)
                {
                    var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (pi != null && (pi.PropertyType == typeof(int) || pi.PropertyType == typeof(short)))
                    {
                        try { return Convert.ToInt32(pi.GetValue(comp)); } catch { }
                    }

                    var fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (fi != null && (fi.FieldType == typeof(int) || fi.FieldType == typeof(short)))
                    {
                        try { return Convert.ToInt32(fi.GetValue(comp)); } catch { }
                    }
                }
            }

            // 3) Buscar en padres (fallback)
            var parent = go.transform.parent;
            while (parent != null)
            {
                var compsParent = parent.GetComponents<Component>();
                foreach (var comp in compsParent)
                {
                    if (comp == null) continue;
                    var t = comp.GetType();

                    if (IsHealthLikeComponent(t)) continue;

                    foreach (var name in CandidateNames)
                    {
                        var pi = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (pi != null && (pi.PropertyType == typeof(int) || pi.PropertyType == typeof(short)))
                        {
                            try { return Convert.ToInt32(pi.GetValue(comp)); } catch { }
                        }
                        var fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (fi != null && (fi.FieldType == typeof(int) || fi.FieldType == typeof(short)))
                        {
                            try { return Convert.ToInt32(fi.GetValue(comp)); } catch { }
                        }
                    }
                }
                parent = parent.parent;
            }

            return 0;
        }

        // Heurística simple para detectar componentes que representan salud/vida y NO deben
        // considerarse como fuentes de daño.
        private static bool IsHealthLikeComponent(Type t)
        {
            if (t == null) return false;
            var name = t.Name.ToLowerInvariant();
            if (name.Contains("vida") || name.Contains("health") || name.Contains("hp")) return true;
            // Propiedades o métodos típicos de un componente de vida
            if (t.GetProperty("VidaActual") != null || t.GetProperty("VidaMaxima") != null) return true;
            if (t.GetMethod("AplicarDano") != null || t.GetMethod("Curar") != null) return true;
            return false;
        }
    }
}