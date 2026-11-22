using Photon.Pun;
using Photon.VR.Player;
using UnityEngine;

namespace Gtag
{
    /// <summary>
    /// Goes on the networked player prefab (Resources/PhotonVR/Player). Drives the Gorilla IK rig
    /// from the PhotonVRPlayer head/hand transforms and tints it with the player's colour.
    /// Remote players see the gorilla; the local player's own copy is hidden.
    /// </summary>
    [RequireComponent(typeof(PhotonVRPlayer))]
    public class GtagPlayerModel : MonoBehaviourPun
    {
        public Transform modelRoot;                 // the gorilla model instance
        public Transform headTarget, leftHandTarget, rightHandTarget; // IK targets in the rig
        public Renderer[] tintRenderers;
        public Vector3 bodyOffset = new Vector3(0, -0.45f, 0);

        PhotonVRPlayer vr;
        MaterialPropertyBlock mpb;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int Color_ = Shader.PropertyToID("_Color");
        string lastColour;

        void Awake()
        {
            vr = GetComponent<PhotonVRPlayer>();
            mpb = new MaterialPropertyBlock();
            if (photonView.IsMine && modelRoot != null) modelRoot.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (modelRoot == null || photonView.IsMine) return;
            if (headTarget) { headTarget.SetPositionAndRotation(vr.Head.position, vr.Head.rotation); }
            if (leftHandTarget) leftHandTarget.SetPositionAndRotation(vr.LeftHand.position, vr.LeftHand.rotation);
            if (rightHandTarget) rightHandTarget.SetPositionAndRotation(vr.RightHand.position, vr.RightHand.rotation);

            Vector3 flatFwd = Vector3.ProjectOnPlane(vr.Head.forward, Vector3.up).normalized;
            if (flatFwd.sqrMagnitude > 0.001f)
                modelRoot.SetPositionAndRotation(vr.Head.position + bodyOffset, Quaternion.LookRotation(flatFwd));

            ApplyColour();
        }

        void ApplyColour()
        {
            var owner = photonView.Owner;
            if (owner == null || !owner.CustomProperties.TryGetValue("Colour", out object o)) return;
            string s = o as string;
            if (string.IsNullOrEmpty(s) || s == lastColour) return;
            lastColour = s;
            Color c = JsonUtility.FromJson<Color>(s);
            foreach (var r in tintRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(mpb);
                mpb.SetColor(BaseColor, c);   // URP Lit
                mpb.SetColor(Color_, c);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
