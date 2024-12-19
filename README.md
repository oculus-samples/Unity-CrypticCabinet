![Cryptic Cabinet Banner](./Documentation/Images/logo/CoverArt.png "Cryptic Cabinet")
# Cryptic Cabinet
Cryptic Cabinet is an escape room game for Meta Quest headsets that demonstrates how to create mixed reality multiplayer experiences that dynamically understand and adapt to nearly any room size. In this tutorial, you’ll see firsthand how the app adapts to the physical environment and presents possibilities to create delightful mixed reality experiences through the use of capabilities like Scene, Passthrough, Shared Spatial Anchors, Colocation, Interaction SDK, and Passthrough Styling. 

This codebase is available as both a reference and template for mixed reality projects. You can also test the game on the [Meta Horizon Store](https://www.meta.com/experiences/6858450927578454/).

# Before you get started
Get familiar with the packages and tools that enable Cryptic Cabinet to support rich multiplayer experiences and dynamically adapt to users’ physical environment. 

- [Meta XR Core SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-core-sdk-269169): This package includes core features for mixed reality development such as Passthrough, Anchors, and Scene to help you create engaging and immersive experiences. 
- [Meta XR Platform SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-platform-sdk-262366): This package enables you to create social immersive experiences that support matchmaking, in-app purchases, downloadable content (DLC), cloud storage, and more. 
- [Meta XR Interaction SDK](https://assetstore.unity.com/packages/tools/integration/meta-xr-interaction-sdk-265014): This package contains the components unique to Interaction SDK that are used for controller and hand interactions and body pose detection.
- [Meta XR Simulator](https://assetstore.unity.com/packages/tools/integration/meta-xr-simulator-266732): This lightweight XR runtime enables you to simulate Meta Quest headsets and features on the API level so you can iterate and test your experiences without a physical device. 
- [Photon Fusion](https://www.photonengine.com/fusion#): Seamlessly support multiplayer modes by implementing this networking solution to handle and route networking traffic in shared user experiences. 
- [UniTask](https://github.com/Cysharp/UniTask?tab=readme-ov-file): This package provides an efficient allocation-free async/await integration for Unity.

# Learn
Explore the Meta Horizon OS capabilities powering rich, shared mixed reality experiences in Cryptic Cabinet. 

- [Scene](https://developers.meta.com/horizon/documentation/unity/unity-scene-overview/): Leverage Mixed Reality Utility Kit on top of the Scene API to quickly index and query an up-to-date representation of the physical world that you can use to support mixed reality. Scene enables Cryptic Cabinet to support dynamic interactions between users, virtual objects, and their physical space. 
- [Passthrough](https://developers.meta.com/horizon/documentation/unity/unity-passthrough/): See your physical space in full, rich color. Passthrough API provides a real-time and perceptually comfortable 3D visualization of the physical world in the Meta Quest headsets so users can see and navigate their surroundings.
- [Shared Spatial Anchors](https://developers.meta.com/horizon/documentation/unity/unity-shared-spatial-anchors/): Create local multiplayer experiences for users in the same room. Shared Spatial Anchors enables a shared, world-locked frame of reference for many users playing Cryptic Cabinet together.   
- [Colocation](https://developers.meta.com/horizon/documentation/unity/unity-set-up-colocation-package/): Enable users to jump into the action together in the same physical space. Using Shared Spatial Anchors, colocation supports sharing physical environment information between headsets to enable accurate virtual positioning so players have a consistent, shared perspective of their surroundings. 
- [Interaction SDK](https://developers.meta.com/horizon/documentation/unity/unity-isdk-interaction-sdk-overview/): Power rich, dynamic interactions between users and their virtual environment. This SDK provides a suite of components that support intuitive navigation and interactions like grabbing, poking, teleportation, and more using your controller or hands. In Cryptic Cabinet, these interactions enable players to interact with virtual elements and progress through the escape room.  
- [Passthrough Styling](https://developers.meta.com/horizon/documentation/unity/unity-customize-passthrough-styling/): Add your unique touch to the headset’s visual feed. Color mapping allows you to customize the output color by adjusting contrast, brightness, saturation, and more.

# Mechanics and Features

Discover some of the mechanics, features, and techniques used to deliver this unique and engaging multiplayer experience. For more information, visit the [Feature Overview](./Documentation/FeatureOverview.md) page.
<p>
    <img src="./Documentation/Images/UVbulb_screw.gif" width="30%">
    <img src="./Documentation/Images/Safe.gif" width="30%">
    <img src="./Documentation/Images/Key.gif" width="30%">
</p>

- [Room Setup](./Documentation/FeatureOverview.md#room-setup): Using returned scene primitives via Scene API, a 3D grid of cells is generated to cover the entire room and track where scene objects are placed so they don’t overlap with real world objects. You can preview the scene after cells are generated. 
- [Networking](./Documentation/FeatureOverview.md#networking): Using Photon Fusion, Cryptic Cabinet provides the game host with a room code that they can share with other users and enable them to join.
- [Rope](./Documentation/FeatureOverview.md#rope): Through Interaction SDK, users can grab the in-game rope at any point along its length using one or two hands, with grabbed positions being synchronized with other users. Realistic collisions are supported to push rope nodes away from each other and from scene objects. 
- [LUT](./Documentation/FeatureOverview.md#lut): Using Passthrough Styling, the Passthrough camera feed is darkened when a user activates the game’s UV light or Orrery projection to deliver the effect of being in a dark room. A central manager ensures the effect is replicated for all users.  
- [Camera darkens when inside objects](./Documentation/FeatureOverview.md#camera-darkens-when-inside-objects): When a user puts their head inside virtual objects, the camera darkens to replicate a more realistic perspective. Passthrough is disabled and enabled as users look in and out of virtual objects. 
- [Safe Dials](./Documentation/FeatureOverview.md#safe-dials): By putting a trigger collider on the index finger of a user’s hand, users can seamlessly swipe the game’s safe dial up and down to crack the safe’s code. 
- [Clock](./Documentation/FeatureOverview.md#clock): Through the use of the OneGrabRotateTransformer feature, users can rotate a handle that subsequently rotates the game’s clock hands to detect when the user has selected the correct time, opening the clock door. 
- [Key & UV Bulb](./Documentation/FeatureOverview.md#key--uv-bulb): The game’s UV bulb and key combine two interaction modes: The first mode enables users to freely manipulate these objects by grabbing, rotating, and moving them, and the second mode locks the objects in position so they can only be rotated around a single axis. 


# How to run the project in Unity

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

# Gameplay Phases

The [**GameManager**](./Assets/CrypticCabinet/Scripts/GameManagement/GameManager.cs) script controls the game phases of the gameplay. Each game phase is responsible for a specific task of the game:
- **ObjectSpawningGamePhase** handles the scene setup via the scene understanding APIs to tweak the placements for the virtual objects around the room before starting the gameplay;
- **WaitForGuestPhase** waits for Guest players that want to join a multiplayer game before the gameplay starts;
- **Act1TimelinePhase** starts the intro animation of the gameplay;
- **PuzzleLoadingGamePhase** spawns the interactive objects around the room after the intro animation has finished playing;
- **Act3TimelinePhase** starts the outro animation once the gameplay completes and all puzzles are resolved.


# UI System

The main UI for the application is defined through the [**UIModalWindow**](.\Assets\CrypticCabinet\UI\UIModalWindow.prefab) prefab, which is controlled by the [UISystem script](.\Assets\CrypticCabinet\Scripts\UI\UISystem.cs).

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

## Code documentation

Within the repo there's detailed Doxygen docs for the code in [Documentation/docs/html/index.html](./Documentation/docs/html/index.html).
This can be opened in a web browser once the code has been cloned.

<img src="./Documentation/Images/Doxygen.png" >

# Dependencies
This project was built using the [Unity engine](https://unity.com/) with [Photon Fusion](https://doc.photonengine.com/fusion/current/getting-started/fusion-intro).

This project makes use of the following plugins and software:

- [Unity 2022.3.16f1](https://unity.com/download) or newer
- [Meta XR Utilities](https://npm.developer.oculus.com/-/web/detail/com.meta.xr.sdk.utilities)
- [Meta XR Platform SDK](https://npm.developer.oculus.com/-/web/detail/com.meta.xr.sdk.platform)
- [Meta XR Interaction SDK](https://npm.developer.oculus.com/-/web/detail/com.meta.xr.sdk.interaction)
- [Meta XR Simulator](https://npm.developer.oculus.com/-/web/detail/com.meta.xr.simulator) (Used for test multi-user)
- [Mixed Reality Utility Kit](https://assetstore.unity.com/packages/tools/integration/meta-mr-utility-kit-272450)
- [Photon Fusion](https://www.photonengine.com/fusion)
- [UniTask](https://github.com/Cysharp/UniTask)

The following is required to test this project within Unity:

- [Meta Quest Link app](https://www.meta.com/quest/setup/)

# License
The majority of Cryptic Cabinet is licensed under [MIT LICENSE](./LICENSE), however files from [Text Mesh Pro](http://www.unity3d.com/legal/licenses/Unity_Companion_License), and [Photon SDK](./Assets/Photon/LICENSE), are licensed under their respective licensing terms.

# Contribution
See the [CONTRIBUTING](./CONTRIBUTING.md) file for how to help out.
