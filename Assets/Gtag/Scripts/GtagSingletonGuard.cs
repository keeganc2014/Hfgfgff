using Photon.VR;
using Photon.VR.Player;
using UnityEngine;

namespace Gtag
{
    /// <summary>
    /// Stops duplicates: PhotonVRManager and PlayerSpawner are DontDestroyOnLoad, so reloading the
    /// scene would otherwise create a second copy (PhotonVRManager then quits the app).
    /// Put this on the same GameObject as PhotonVRManager and PhotonVRPlayerSpawner (run before them).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GtagSingletonGuard : MonoBehaviour
    {
        public string key = "PhotonVRManager";
        static readonly System.Collections.Generic.HashSet<string> alive = new System.Collections.Generic.HashSet<string>();

        void Awake()
        {
            if (!alive.Add(key)) { Destroy(gameObject); return; }
        }

        void OnDestroy() { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => alive.Clear();   // domain reload off in editor
    }
}
