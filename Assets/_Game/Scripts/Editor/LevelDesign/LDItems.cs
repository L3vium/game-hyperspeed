using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Com.IsartDigital.Hyperspeed.EditorTools
{
    // What the level design tools know about each kind of object : its colour in the scene, the fields worth editing and
    // a sensible default for the stage it goes in.
    public static class LDItems
    {
        public enum FieldType { Float, Int, Bool, PaneKind }

        public struct Field
        {
            public string name;
            public string label;
            public FieldType type;
            public FieldInfo info;
        }

        public static readonly ItemKind[] KINDS = (ItemKind[])System.Enum.GetValues(typeof(ItemKind));

        private static readonly Dictionary<ItemKind, Field[]> _fields = new Dictionary<ItemKind, Field[]>();

        public static Color ColorOf(ItemKind pKind)
        {
            switch (pKind)
            {
                case ItemKind.Pad: return new Color(0.2f, 0.95f, 1f);
                case ItemKind.Strip: return new Color(1f, 0.85f, 0.1f);
                case ItemKind.Ring: return new Color(0.4f, 1f, 0.5f);
                case ItemKind.Crate: return new Color(1f, 0.5f, 0.15f);
                case ItemKind.Laser: return new Color(1f, 0.15f, 0.2f);
                case ItemKind.Hammer: return new Color(0.85f, 0.1f, 0.5f);
                case ItemKind.Pane: return new Color(0.6f, 0.85f, 1f);
                case ItemKind.Rope: return new Color(1f, 1f, 0.4f);
                case ItemKind.Mover: return new Color(0.75f, 0.5f, 1f);
                case ItemKind.Bridge: return new Color(0.3f, 0.6f, 1f);
                case ItemKind.Meteor: return new Color(1f, 0.3f, 0.05f);
                case ItemKind.Prop: return new Color(0.7f, 0.65f, 0.6f);
                case ItemKind.Arch: return new Color(1f, 1f, 1f);
                case ItemKind.GravityGate: return new Color(0.3f, 1f, 0.9f);
                case ItemKind.FloatRock: return new Color(0.55f, 0.45f, 0.4f);
            }
            return Color.white;
        }

        // Kinds that span the whole track width : their lateral does not matter.
        public static bool FullWidth(ItemKind pKind) => pKind == ItemKind.Laser || pKind == ItemKind.Hammer || pKind == ItemKind.Arch || pKind == ItemKind.GravityGate;

        // Kinds that run from s to s1.
        public static bool Spans(ItemKind pKind) => pKind == ItemKind.Rope || pKind == ItemKind.Bridge;

        public static Field[] FieldsOf(ItemKind pKind)
        {
            Field[] lFields;
            if (_fields.TryGetValue(pKind, out lFields)) return lFields;
            List<Field> lList = new List<Field>();
            switch (pKind)
            {
                case ItemKind.Pad: Add(lList, "width", "Width"); Add(lList, "length", "Length"); Add(lList, "value", "Boost (x cruise)"); break;
                case ItemKind.Strip: Add(lList, "width", "Half width"); Add(lList, "value", "Launch speed"); break;
                case ItemKind.Ring: Add(lList, "height", "Height"); Add(lList, "size", "Radius"); break;
                case ItemKind.Crate: Add(lList, "size", "Size"); Add(lList, "flag", "Rolls forward", FieldType.Bool); break;
                case ItemKind.Laser: Add(lList, "height", "Height"); Add(lList, "variant", "Beams", FieldType.Int); Add(lList, "value", "Spin (deg/s)"); break;
                case ItemKind.Hammer: Add(lList, "value", "Phase"); break;
                case ItemKind.Pane: Add(lList, "width", "Width"); Add(lList, "height", "Height"); Add(lList, "variant", "Material", FieldType.PaneKind); break;
                case ItemKind.Rope: Add(lList, "s1", "End distance"); Add(lList, "height", "Start height"); Add(lList, "height1", "End height"); break;
                case ItemKind.Mover: Add(lList, "width", "Width"); Add(lList, "length", "Length"); Add(lList, "height", "Height"); Add(lList, "value", "Speed (m/s)"); break;
                case ItemKind.Bridge: Add(lList, "s1", "End distance"); Add(lList, "height", "Height"); Add(lList, "width", "Half width"); break;
                case ItemKind.Meteor: Add(lList, "size", "Radius"); Add(lList, "flag", "Falls from the sky", FieldType.Bool); break;
                case ItemKind.Prop:
                    Add(lList, "width", "Width"); Add(lList, "length", "Length"); Add(lList, "height", "Height"); Add(lList, "value", "Lean");
                    Add(lList, "variant", "Palette (0-2)", FieldType.Int); Add(lList, "jitter", "Yaw"); Add(lList, "rough", "Taper");
                    break;
                case ItemKind.Arch: Add(lList, "flag", "Lit", FieldType.Bool); break;
                case ItemKind.FloatRock: Add(lList, "height", "Height"); Add(lList, "size", "Size"); Add(lList, "variant", "Shape (0-5)", FieldType.Int); Add(lList, "flag", "Absolute altitude", FieldType.Bool); break;
            }
            lFields = lList.ToArray();
            _fields[pKind] = lFields;
            return lFields;
        }

        private static void Add(List<Field> pList, string pName, string pLabel, FieldType pType = FieldType.Float)
        {
            pList.Add(new Field { name = pName, label = pLabel, type = pType, info = typeof(ItemData).GetField(pName) });
        }

        // A new object of this kind at (s, lateral), sized for the stage.
        public static ItemData Create(ItemKind pKind, float pS, float pLateral, LevelLayout pLayout)
        {
            float lHalf = pLayout != null ? pLayout.field.HalfWidth(pS) : 200f;
            float lCruise = pLayout != null && pLayout.profile != null ? pLayout.profile.cruise : 250f;
            ItemData lItem = new ItemData { kind = pKind, s = pS, lateral = pLateral };
            switch (pKind)
            {
                case ItemKind.Pad: lItem.width = lHalf * 0.42f; lItem.length = lCruise * 0.16f; lItem.value = 0.5f; break;
                case ItemKind.Strip: lItem.width = lHalf * 0.2f; lItem.value = 60f; break;
                case ItemKind.Ring: lItem.height = 14f; lItem.size = 24f; break;
                case ItemKind.Crate: lItem.size = 80f; break;
                case ItemKind.Laser: lItem.height = 6f; lItem.variant = 3; break;
                case ItemKind.Hammer: lItem.value = Random.Range(0f, 6.28f); break;
                case ItemKind.Pane: lItem.width = 46f; lItem.height = 36f; break;
                case ItemKind.Rope: lItem.s1 = pS + lCruise * 4f; lItem.height = 7f; lItem.height1 = 3f; break;
                case ItemKind.Mover: lItem.width = lHalf * 0.22f; lItem.length = lItem.width * 2.6f; lItem.height = 8f; lItem.value = lCruise * 0.5f; break;
                case ItemKind.Bridge: lItem.s1 = pS + lCruise * 4f; lItem.height = 20f; lItem.width = lHalf * 0.16f; break;
                case ItemKind.Meteor: lItem.size = 50f; lItem.flag = true; break;
                case ItemKind.Prop: lItem.width = 45f; lItem.length = 60f; lItem.height = 90f; lItem.rough = 0.2f; lItem.jitter = Random.Range(-14f, 14f); break;
                case ItemKind.Arch: lItem.flag = true; break;
                case ItemKind.FloatRock: lItem.height = 60f; lItem.size = 30f; lItem.variant = Random.Range(0, 6); lItem.jitter = Random.Range(0f, 360f); break;
            }
            return lItem;
        }
    }
}
