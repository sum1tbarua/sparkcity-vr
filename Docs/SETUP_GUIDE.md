# Spark City VR - Setup Guide (Unity 6.3 LTS + Meta Quest 3S)

This package contains the full game code and data. You add it to a Unity VR project, click one menu item,
and the scene is built for you. Everything in the game (rooms, stations, UI, cluster) is generated from code
when you press Play, so it is easy to change.

```
SparkCityVR_package/
  SparkCityVR/            <- copy this folder into your project's Assets/ folder
    Scripts/Runtime/      game code (C#)
    Scripts/Editor/       "Spark City VR" menu (scene builder, build settings)
    Resources/SparkCityData/   content.json (all game text + answers), tutorial
  SETUP_GUIDE.md          this file
  TEACHER_GUIDE.md        running the Nov 3 in-class simulation + answer key
  MISSION_AUTHORING.md    how to change texts and answers
```

---

## 1. Create the Unity project (10 min)

1. Unity Hub > **New project** > Editor **6000.3.21f1**.
2. Pick the **VR** template (Universal Render Pipeline + XR Interaction Toolkit + OpenXR already set up).
   *If you don't see it, download it in the Hub, or use **Universal 3D** and do step 3.*
3. Only if you did NOT use the VR template - Window > Package Manager > Unity Registry, install:
   - **XR Interaction Toolkit** (3.x) -> then open its **Samples** tab and import **Starter Assets**
   - **OpenXR Plugin**
   - **XR Plugin Management**
   - Accept the prompt to enable the new Input System and restart.
4. Make sure the XRI **Starter Assets** sample is imported (Package Manager > XR Interaction Toolkit > Samples).
   The game needs its `XR Origin (XR Rig)` prefab.
5. Window > TextMeshPro > **Import TMP Essential Resources** (skip if already imported).

## 2. Add Spark City VR (2 min)

1. Unzip the package. Drag the **`SparkCityVR`** folder into the Project window under **Assets**.
2. Wait for scripts to compile. Check the Console: there should be **no red errors**.
   - If you see errors, copy the first one or two and send them to me - they are usually one-line fixes
     caused by a package version difference.
3. Menu bar > **Spark City VR > Build Game Scene**.
   This creates materials, a new scene `Assets/SparkCityVR/Scenes/SparkCity.unity` with the XR rig,
   the EventSystem and the game object, and adds the scene to the build list.

## 3. Test in the Editor without a headset (desktop test mode)

1. Project Settings > XR Plug-in Management > **monitor tab**: OpenXR **unticked** (Android tab stays ticked).
2. XR Plug-in Management > XR Interaction Toolkit: **untick** "Use XR Interaction Simulator in scenes".
3. Press **Play** and click once in the Game view. A **DESKTOP TEST MODE** bar appears at the top-left.

| Input | Does |
|---|---|
| Mouse | aims the right controller's ray |
| Left click | trigger: press buttons, choose objects |
| Right drag, arrow keys, Q/E | look / turn |
| W A S D | walk |
| F | normal / wide (headset-like) view |

Shortcut: select `SparkCityGame` in the Hierarchy and set **Debug Start Mission** to `1` or `2` to skip the welcome space
(set it back to `0` before building for the headset). Desktop mode never runs on the headset.

## 4. Quest 3S build settings (10 min, first time only)

1. Menu > **Spark City VR > Configure Quest Build Settings** - switches to Android and sets IL2CPP, ARM64,
   Linear color and an app id (`com.sparkcityedu.sparkcityvr`).
2. Project Settings > **XR Plug-in Management** > **Android** tab (robot icon): tick **OpenXR**.
3. XR Plug-in Management > **OpenXR** > Android tab:
   - **Interaction Profiles**: add **Oculus Touch Controller Profile** and **Meta Quest Touch Plus Controller Profile**.
   - **OpenXR Feature Groups**: enable **Meta Quest** / **Meta Quest Support**.
   - Render Mode: **Single Pass Instanced**.
4. XR Plug-in Management > **Project Validation** > click **Fix All** until it is clean.
5. Player Settings > Other Settings > **Minimum API Level**: Android 10 (API 29) or higher (32 recommended).

## 5. Put the headset in developer mode (first time only)

1. Create (or use) a Meta developer account / organization (developers.meta.com).
2. In the **Meta Horizon** phone app: Devices > your Quest 3S > Headset settings > **Developer Mode** ON.
3. Connect the headset to the PC with a USB-C data cable, put it on and **Allow USB debugging** (tick "Always allow").

## 6. Build and run

1. File > Build Profiles (or Build Settings) > Android > Run Device: your Quest 3S.
2. **Build And Run**. The first build takes several minutes.
3. On the headset the app is under **Library > Unknown Sources** (or *Applications*).

**Installing on many class headsets:** build once (`Build` instead of `Build And Run`) to get an `.apk`,
then install it on each headset with **Meta Quest Developer Hub** (drag the APK onto the device) or
`adb install -r SparkCityVR.apk`. Each headset must be in developer mode.

## 7. Controls (tell students)

| Action | Controller |
|---|---|
| Choose anything (buttons, crates, blocks, towers) | Point the ray, pull the **trigger** |
| Teleport / turn | Thumbsticks (starter rig defaults) |

That is all - there is no grabbing. The game moves the player to each mini-game automatically, so it can be
played **seated** ("Seated mode", on by default, raises the view to standing height).

## 8. Logs

Saving log files is switched off (`Enabled = false` in `Scripts/Runtime/SessionLog.cs`). Set it to `true` to write one JSON line per finished mission to the headset.

## Welcome artwork (your 8K image)

1. Put the image in **Assets/SparkCityVR/Resources/SparkCityArt/** and name it **welcome** (`welcome.png` or `welcome.jpg`).
   Import settings are applied automatically (up to 8192 px, mipmaps, high-quality compression). Keep your original file.
2. Select **SparkCityGame** in the Hierarchy and choose **Welcome Art Mode**:
   - **Flat Screen** - any picture, shown as a giant glowing screen in the welcome space (width: *Flat Screen Width*, default 9 m).
   - **Panorama 360** - ONLY for true equirectangular 360 images (2:1, made for 360 viewing). It becomes the sky around the player.
     A normal wide wallpaper would look stretched in this mode - use Flat Screen for it.
3. For Panorama 360 run **Spark City VR > Build Game Scene** once (it creates the panoramic skybox material used in the Quest build).
4. Check frame rate and image sharpness **on the Quest 3S** with the 8K original before deciding on any lower resolution.

## Fonts (dramatic look)

1. Download two fonts as .ttf - suggested: **Orbitron** (headings) and **Exo 2** (body text), free from fonts.google.com (Open Font License).
2. Rename them **Heading.ttf** and **Body.ttf** and put them in **Assets/SparkCityVR/Resources/SparkCityFonts/**.
3. Run **Spark City VR > Create Font Assets**. It creates "Heading SDF" and "Body SDF" TextMeshPro assets in the same folder.
   (If you skip this step the game still converts the .ttf files at start-up, but creating the assets in setup is the reliable way.)
4. Press Play: titles and buttons use Heading, all other text uses Body. Without these files the default font is used.

## Sound volume

All sounds are soft and quiet by default. To change the overall volume, edit `Volume` in `Scripts/Runtime/Feedback.cs` (0 = silent, 1 = full).

## 9. Performance notes (Quest 3S)

- Everything is low-poly primitives with emissive materials, **no realtime shadows**, one directional light.
- Use the URP asset the VR template assigns for Android; keep MSAA 4x, HDR off.
- If frame rate drops, lower the URP render scale to 0.9 or lower the packet spawn rate in `Mission2Games.cs` (RunStream).

## 10. Troubleshooting

| Problem | Fix |
|---|---|
| "XR rig not found" when building the scene | Import XRI **Starter Assets** sample |
| Pink objects | Re-run **Build Game Scene** (creates the URP materials); check the URP asset is assigned in Graphics + Quality settings |
| Text shows squares | Use only plain characters in JSON text, or import TMP Essentials |
| Can't press buttons | The scene needs the EventSystem with **XR UI Input Module** (Build Game Scene adds it); the controller ray must have UI interaction enabled (on by default in the starter rig) |
| Black screen on headset | OpenXR not ticked on the **Android** tab, or Meta Quest feature group disabled |
