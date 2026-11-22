using GorillaLocomotion;
using UnityEngine;

namespace Gtag
{
    /// <summary>Moves the local player to the "stump" spawn point at start and when asked (e.g. after a tag/respawn).</summary>
    public class GtagStumpSpawn : MonoBehaviour
    {
        public Transform stump;               // assign, or an object named "stump" is looked up
        public Vector3 offset = new Vector3(0, 1.2f, 0);

        void Start()
        {
            if (stump == null) { var go = GameObject.Find("stump"); if (go != null) stump = go.transform; }
            Respawn();
        }

        public void Respawn()
        {
            var p = Player.Instance;
            if (p == null || stump == null) return;
            var rb = p.GetComponent<Rigidbody>();
            if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            p.transform.position = stump.position + offset;
            p.transform.rotation = Quaternion.Euler(0, stump.eulerAngles.y, 0);
        }
    }
}
