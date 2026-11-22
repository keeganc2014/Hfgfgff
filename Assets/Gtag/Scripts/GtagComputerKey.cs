using UnityEngine;

namespace Gtag
{
    public enum GtagKeyType { Letter, Delete, Confirm, Colour }

    /// <summary>Put on a trigger collider. Touched by a hand (a collider whose name contains "Hand" or "Sphere", like the template's RSphere/LSphere).</summary>
    [RequireComponent(typeof(Collider))]
    public class GtagComputerKey : MonoBehaviour
    {
        public GtagKeyType type = GtagKeyType.Letter;
        public string letter = "A";
        public Color colour = Color.white;

        void Reset() { GetComponent<Collider>().isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            if (!IsHand(other)) return;
            var pc = GtagComputer.Instance;
            if (pc == null) return;
            switch (type)
            {
                case GtagKeyType.Letter: pc.PressKey(letter); break;
                case GtagKeyType.Delete: pc.Delete(); break;
                case GtagKeyType.Confirm: pc.Confirm(); break;
                case GtagKeyType.Colour: pc.SelectColour(colour); break;
            }
        }

        static bool IsHand(Collider c)
        {
            string n = c.name;
            return n.Contains("Hand") || n.Contains("Sphere");
        }
    }
}
