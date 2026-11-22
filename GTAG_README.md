# Gtag Game (merged project)

Base: GCS Fan Game Template v3 + GCS Shop System 1.0.2 + GCS Wardrobe 1.2.1 (all in Assets/Glitched Cat Studios).
Our code: Assets/Gtag (computer, leaderboard, stump spawn, gorilla model, duplicate guards).

## Before your first build (required, ~5 minutes)
1. Export your Gorilla_IK_Rig.blend (not included, to keep the zip small) to FBX in Blender (File > Export > FBX, Apply Transform, include Armature + Mesh)
   and save it as Assets/Gtag/Models/Gorilla_IK_Rig.fbx. Keep the .blend outside the project:
   Unity Cloud build machines do not have Blender, so a .blend inside Assets will fail to import there.
2. Open the project in Unity 6 (project is on 6000.2.12f1; Hub can upgrade it to a newer Unity 6).
3. Open Assets/Scenes/Main.unity, run menu Gtag > Setup Scene, save the scene (Ctrl+S), commit.
4. Set your Photon AppId / VoiceAppId on PhotonVRManager and your PlayFab Title ID (PlayFabSharedSettings).

## What is enforced
- Color space: Linear. Android/Quest: Vulkan only (no OpenGL ES). Windows: Direct3D11 (no OpenGLCore). IL2CPP + ARM64.
  (Gtag > Enforce Linear + Vulkan re-applies this on every editor load.)
- PHOTONVR_004 define added so the Wardrobe compiles against the template's PhotonVR 0.0.4.

## No duplicates
- Leaderboard: one row per Photon ActorNumber; name/colour changes update that row.
- Computer: name/colour go through PhotonVRManager.SetUsername/SetColour -> RPC refresh of the existing rig (no respawn).
- GtagSingletonGuard on PhotonVRManager / PlayerSpawner destroys any second copy.
- Setup Scene rebuilds the stump/computer/leaderboard/model in place instead of adding copies.
