using Photon.Pun;
using Photon.VR;
using TMPro;
using UnityEngine;

namespace Gtag
{
    /// <summary>
    /// The in-game computer. Name is typed with GtagComputerKey buttons, colour is chosen with the
    /// colour keys. Everything goes through PhotonVRManager so the existing player rig is updated
    /// via RPC - no second rig is ever spawned.
    /// </summary>
    public class GtagComputer : MonoBehaviour
    {
        public static GtagComputer Instance { get; private set; }

        public TextMeshPro screen;
        public int maxNameLength = 12;
        public float keyCooldown = 0.15f;

        string typedName = "";
        Color pendingColour = Color.white;
        float lastKey;
        string status = "";

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            typedName = PlayerPrefs.GetString("Username", "");
            if (typedName.Length > maxNameLength) typedName = typedName.Substring(0, maxNameLength);
            if (PhotonVRManager.Manager != null) pendingColour = PhotonVRManager.Manager.Colour;
            Refresh();
        }

        public void PressKey(string key)
        {
            if (Time.time - lastKey < keyCooldown) return;
            lastKey = Time.time;
            if (typedName.Length < maxNameLength) typedName += key.ToUpperInvariant();
            status = "";
            Refresh();
        }

        public void Delete()
        {
            if (Time.time - lastKey < keyCooldown) return;
            lastKey = Time.time;
            if (typedName.Length > 0) typedName = typedName.Substring(0, typedName.Length - 1);
            Refresh();
        }

        public void SelectColour(Color c)
        {
            if (Time.time - lastKey < keyCooldown) return;
            lastKey = Time.time;
            pendingColour = c;
            if (PhotonVRManager.Manager != null && PhotonNetwork.LocalPlayer != null)
                PhotonVRManager.SetColour(c);   // applies instantly to everyone via RPC
            status = "COLOUR SET";
            Refresh();
        }

        public void Confirm()
        {
            if (Time.time - lastKey < keyCooldown) return;
            lastKey = Time.time;
            string n = typedName.Trim();
            if (n.Length == 0) { status = "ENTER A NAME"; Refresh(); return; }
            if (PhotonVRManager.Manager == null || PhotonNetwork.LocalPlayer == null) { status = "NOT CONNECTED"; Refresh(); return; }
            PhotonVRManager.SetUsername(n);      // only updates NickName + refreshes the existing rig
            status = "NAME SET";
            Refresh();
        }

        void Refresh()
        {
            if (screen == null) return;
            string c = ColorUtility.ToHtmlStringRGB(pendingColour);
            screen.text =
                "<b>GTAG COMPUTER</b>\n" +
                "NAME: <color=#" + c + ">" + typedName + "_</color>\n" +
                (PhotonNetwork.InRoom ? "ROOM: " + PhotonNetwork.CurrentRoom.Name + "  (" + PhotonNetwork.CurrentRoom.PlayerCount + ")" : "NOT IN A ROOM") + "\n" +
                status;
        }

        void Update()
        {
            // keep room line fresh
            if (Time.frameCount % 60 == 0) Refresh();
        }
    }
}
