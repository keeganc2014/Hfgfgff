using System.Collections.Generic;
using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

namespace Gtag
{
    /// <summary>
    /// Shows every player in the room once (keyed by ActorNumber), with their name in their colour.
    /// A name or colour change only updates that player's row - it never adds a second entry.
    /// </summary>
    public class GtagLeaderboard : MonoBehaviourPunCallbacks
    {
        public TextMeshPro board;
        public int maxRows = 16;
        public string header = "<b>PLAYERS</b>\n";

        readonly Dictionary<int, Player> players = new Dictionary<int, Player>();
        bool dirty = true;

        public override void OnEnable() { base.OnEnable(); Rebuild(); }
        public override void OnJoinedRoom() => Rebuild();
        public override void OnLeftRoom() { players.Clear(); dirty = true; }
        public override void OnPlayerEnteredRoom(Player p) => dirty = true;
        public override void OnPlayerLeftRoom(Player p) => dirty = true;
        public override void OnPlayerPropertiesUpdate(Player p, Hashtable changed) => dirty = true;

        void Update()
        {
            if (!dirty) return;
            dirty = false;
            Rebuild();
        }

        void Rebuild()
        {
            if (board == null) return;
            players.Clear();
            if (PhotonNetwork.InRoom)
                foreach (var p in PhotonNetwork.PlayerList) players[p.ActorNumber] = p; // one row per actor

            var list = new List<Player>(players.Values);
            list.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));

            var sb = new StringBuilder(header);
            int shown = 0;
            foreach (var p in list)
            {
                if (shown++ >= maxRows) break;
                string n = string.IsNullOrWhiteSpace(p.NickName) ? "PLAYER" + p.ActorNumber : p.NickName;
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(GetColour(p))).Append('>')
                  .Append(Sanitize(n)).Append("</color>");
                if (p.IsLocal) sb.Append(" (YOU)");
                sb.Append('\n');
            }
            if (shown == 0) sb.Append("NOT CONNECTED");
            board.text = sb.ToString();
        }

        public static Color GetColour(Player p)
        {
            if (p.CustomProperties.TryGetValue("Colour", out object o) && o is string s && !string.IsNullOrEmpty(s))
            {
                try { return JsonUtility.FromJson<Color>(s); } catch { }
            }
            return Color.white;
        }

        static string Sanitize(string s) => s.Replace("<", "").Replace(">", "");
    }
}
