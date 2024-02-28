# Cryptic Cabinet
![Cryptic Cabinet Banner](./Documentation/Images/logo/CoverArt.png "Cryptic Cabinet")

Cryptic Cabinet is a short Mixed Reality experience designed for Meta Quest 2, Quest Pro and Quest 3 headsets. 

It demonstrates the possibilities of MR using the Meta SDK packages for Unity, and implements multiplayer scenarios using Photon Fusion in combination with Meta's shared spatial anchors APIs. This project adapts to your physical room (big or small) to create a unique experience for everyone.

This codebase is available both as a reference and as a template for MR projects.

The majority of Cryptic Cabinet is licensed under [MIT LICENSE](./LICENSE), however files from [Text Mesh Pro](http://www.unity3d.com/legal/licenses/Unity_Companion_License), and [Photon SDK](./Assets/Photon/LICENSE), are licensed under their respective licensing terms.

See the [CONTRIBUTING](./CONTRIBUTING.md) file for how to help out.

This project was built using the [Unity engine](https://unity.com/) with [Photon Fusion](https://doc.photonengine.com/fusion/current/getting-started/fusion-intro).

Test the game on [AppLab - Cryptic Cabinet](https://www.meta.com/experiences/6858450927578454/).

## How to run the project in Unity

1. [Configure the project](./Documentation/ProjectConfiguration.md) with Meta Quest and Photon
2. Make sure you're using  *Unity 2022.3.16f1* or newer.
3. Load the scene Assets/CrypticCabinet/Scenes/MainScene.unity
4. To test in Editor you will need to use Quest Link:
    <details>
      <summary><b>Quest Link</b></summary>

    - In the Oculus desktop app navigate to the Beta Settings Settings -> Beta and enable the following settings:
      - Developer runtime features 
      - Pass-through over Oculus Link
      - Share point cloud over Oculus Link
    - Enable Quest Link:
      - Put on your headset and navigate to "Quick Settings"; select "Quest Link" (or "Quest Air Link" if using Air Link).
      - Select your desktop from the list and then select, "Launch". This will launch the Quest Link app, allowing you to control your desktop from your headset.
    - With the headset on, select "Desktop" from the control panel in front of you. You should be able to see your desktop in VR!
    - Navigate to Unity and press "Play" - the application should launch on your headset automatically.
    </details>
5. To test in Editor as a guest the simulator can be used
   <details>
      <summary><b>Enabling the Simulator</b></summary>
   
      - Select Meta -> Simulator -> Enable Simulator
      - Press Play
      - The simulator should open a new window ([Simulator Docs](https://developer.oculus.com/documentation/unity/xrsim-intro/))
   </details>

# Project Structure
The project is organically structured to distinguish the main components of the MR experience's logic. A breakdown of the core features is defined on the [Main Scene](./Assets/CrypticCabinet/Scenes/MainScene.Unity) under the "**CrypticCabinetLogic**" GameObject.

The **CrypticCabinetLogic** contains the following core objects:

- **ColocationManager**, which is responsible for colocating multiple players within the same room, and to keep a single player aligned to the real room throughout the gameplay. For more information, check the documentation inside the **ColocationManager** script.

- **ConnectionManager**, which handles the Photon Fusion connection workflows for single and multiplayer sessions. The PhotonConnector logic showcases how a shared multiplayer session is handled via Photon Fusion, how the creation of shared rooms and lobbies work, and how the connection states can be handled accordingly.

- **PassthroughManager**, which is responsible for the Color LUT effects applied to the passthrough of the supported Quest headsets. During the gameplay, this feature is showcased when interacting with the UV machine puzzle and the light beam hitting the glass globe in the Orrery puzzle. Additional functions are implemented inside the **PassthroughConfigurator** and **PassthroughChanger** as an example of customized effects using Meta's Color LUT APIs from the SDK.

- **SceneManagement**, which holds the logic to use the Scene Understanding API from the SDK to configure the virtual objects placements around the real room of the player, allowing their tweaking when desired.

- **GameManager**, which controls the game phases flow of the gameplay.

- **Player**, which holds the logic for all the interactions a player can perform across the whole experience. This showcases how the Meta Interaction SDK can be used to simplify usability for XR scenarios.

# Mechanics and Features

An explanation of some of the mechanics and features of the app can be found in the [Feature Overview](./Documentation/FeatureOverview.md) page.
<p>
    <img src="./Documentation/Images/UVbulb_screw.gif" width="30%">
    <img src="./Documentation/Images/Safe.gif" width="30%">
    <img src="./Documentation/Images/Key.gif" width="30%">
</p>

# Gameplay Phases

The [**GameManager**](./Assets/CrypticCabinet/Scripts/GameManagement/GameManager.cs) script controls the game phases of the gameplay. Each game phase is responsible for a specific task of the game:
- **ObjectSpawningGamePhase** handles the scene setup via the scene understanding APIs to tweak the placements for the virtual objects around the room before starting the gameplay;
- **WaitForGuestPhase** waits for Guest players that want to join a multiplayer game before the gameplay starts;
- **Act1TimelinePhase** starts the intro animation of the gameplay;
- **PuzzleLoadingGamePhase** spawns the interactive objects around the room after the intro animation has finished playing;
- **Act3TimelinePhase** starts the outro animation once the gameplay completes and all puzzles are resolved.


# UI System

The main UI for the application is defined through the **UIModalWindow** prefab, which is controlled by the UISystem script.

This script conveniently defines a singleton object that any class in the game can interact with to trigger UI messages and callbacks.


# Getting the code

First, ensure you have Git LFS installed by running this command:

```sh
git lfs install
```

Then, clone this repo using the "Code" button above, or this command:

```sh
git clone https://github.com/oculus-samples/Unity-CrypticCabinet.git
```


Within the repo there's detailed Doxygen docs for the code in [Documentation/docs/html/index.html](./Documentation/docs/html/index.html).
This can be opened in a web browser once the code has been cloned.

<img src="./Documentation/Images/Doxygen.png" >

# Dependencies
This project makes use of the following plugins and software:

- [Unity 2022.3.16f1](https://unity.com/download) or newer
- [Meta XR Utilities](https://npm.developer.oculus.com/-/web/detail/com.meta.xr.sdk.utilities)
- [Meta XR Platform SDK](https://npm.developer.oculus.com/-/web/detail/com.meta.xr.sdk.platform)
- [Meta XR Interaction SDK](https://npm.developer.oculus.com/-/web/detail/com.meta.xr.sdk.interaction)
- [Meta XR Simulator](https://npm.developer.oculus.com/-/web/detail/com.meta.xr.simulator) (Used for test multi-user)
- [Photon Fusion](https://www.photonengine.com/fusion)
- [UniTask](https://github.com/Cysharp/UniTask)

The following is required to test this project within Unity:
- [The Oculus App](https://www.meta.com/gb/quest/setup/)
