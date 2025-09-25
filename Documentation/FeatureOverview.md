# Room Setup

This section explains how to automatically populate a generic real-world room with game objects. The [Scene API](https://developers.meta.com/horizon/documentation/unity/unity-scene-overview/) helps understand the user's space. Using the scene primitives, a 3D grid of cells is created to cover the room and track object placement. This system detects safe locations for game objects, minimizing overlap with real-world objects. Users must set up their scene accurately for the best experience.

Free space in the 3D grid appears as green cells, while non-valid locations are red. For example, in the image below, the wall has some available space (green), but the window is excluded (red). The floor also has red areas under desks and shelving.

Note: To visualize the 3D grid in the build, toggle the debug view by holding the left joystick and pressing A on the right controller.

<img src="../Documentation/Images/RoomSetup.png" width="40%" height="40%">

There are four categories of placement locations: “floor,” “wall,” “desks,” and “against wall.” “Against wall” objects need to be on the floor and against a wall, like a cabinet. As objects are placed, they block the cells they cover to prevent other objects from being placed there.

Objects are placed in this order:
1. Objects on the floor against a wall
2. Objects on walls
3. Objects on desks (fallback to floor if no desks are available)
4. Objects on the floor
5. Objects on any horizontal location

Within each category, objects are placed from largest to smallest. If an object can't find a safe location, it is placed randomly in a valid location. For example, wall objects stay on walls, and floor objects stay on the floor, even if overlapping occurs.

After automatic placement, there are a few seconds of “easing” to allow overlapping objects to repel each other. The Unity physics engine function [ComputePenetration](https://docs.unity3d.com/ScriptReference/Physics.ComputePenetration.html) provides direction and distance to move objects apart. Instead of jumping to new locations, the system lerps towards them, allowing multiple objects to move over time.

Once placement is complete, users can preview the layout. Objects are visualized as colored boxes:
- Blue boxes: items that need to be physically reached during the experience.
- Green boxes: items that need to be visible but not reached.
- Red boxes: items in non-valid locations or overlapping with other objects.

Users can manually move these boxes to ensure no red boxes and that all blue boxes are reachable.

<img src="../Documentation/Images/SpawningBoxes.png" width="50%" height="50%">

After this, users confirm the layout and start the experience. Object positions are stored for gameplay to avoid recalculating.

### Relevant Files
- [ObjectPlacementManager.cs](../Assets/CrypticCabinet/Scripts/Utils/ObjectPlacementManager.cs)
- [SceneUnderstandingLocationPlacer.cs](../Assets/CrypticCabinet/Scripts/SceneManagement/SceneUnderstandingLocationPlacer.cs)
- [FloorSpaceFinder.cs](../Assets/CrypticCabinet/Scripts/SceneManagement/FloorSpaceFinder.cs)
- [WallSpaceFinder.cs](../Assets/CrypticCabinet/Scripts/SceneManagement/WallSpaceFinder.cs)
- [DeskSpaceFinder.cs](../Assets/CrypticCabinet/Scripts/SceneManagement/DeskSpaceFinder.cs)

# Networking

Multiplayer is managed via [Photon Fusion](https://doc.photonengine.com/realtime/current/getting-started/quick-start). Since the app is designed for colocation, a lobby screen isn't necessary. When the host creates a new game, they receive a room code to share with guests. This is handled in [PhotonConnector](../Assets/CrypticCabinet/Scripts/Photon/PhotonConnector.cs). Colocation events are also triggered here. A deep dive is available [here](https://developers.meta.com/horizon/documentation/unity/unity-colocation-deep-dive/) and is called from [ColocationDriverNetObj](../Assets/CrypticCabinet/Scripts/Colocation/ColocationDriverNetObj.cs).

### Relevant Files
- [GrabPassOwnership.cs](../Assets/CrypticCabinet/Scripts/Utils/GrabPassOwnership.cs)
- [NetworkedSnapHandler.cs](../Assets/CrypticCabinet/Scripts/Utils/NetworkedSnapHandler.cs)
- [NetworkedSnappedObject.cs](../Assets/CrypticCabinet/Scripts/Utils/NetworkedSnappedObject.cs)

# Rope

<img src="../Documentation/Images/Rope.gif" width="40%" height="40%">

After an initial prototype using a chain of physics colliders, a Verlet rope implementation was chosen, based on [this open-source example](https://github.com/GaryMcWhorter/Verlet-Chain-Unity). The first prototype made the rope look like sticks and was hard to tune, while the second approach gave more realistic results. It took several iterations to fine-tune the number of bones for the final asset to ensure fluidity without affecting performance.

To improve its look and feel, the rope was designed to avoid elasticity. When pulled, it comes out of the ceiling instead of stretching. The rope can be grabbed from any point using one or two hands. A Grabbable follows the user's hand; once grabbed, it locks to the nearest node, which then follows the user's hand. This is duplicated for the other hand. These positions are synchronized over the network so remote users can see the active player grabbing the rope.

To make the rope feel real, it must collide correctly with walls and other objects. Collisions are calculated using [Physics.OverlapSphereNonAlloc](https://docs.unity3d.com/ScriptReference/Physics.OverlapSphereNonAlloc.html) and [Physics.ComputePenetration](https://docs.unity3d.com/ScriptReference/Physics.ComputePenetration.html) to push rope nodes away from each other and scene objects. In multiplayer, each client calculates their own rope updates but shares fixed locations when the rope is held by a user.

### Relevant Files
- [Rope.cs](../Assets/CrypticCabinet/Scripts/Puzzles/SandPuzzle/Rope.cs)

# LUT

When the user activates the UV light or the Orrery projection, the passthrough camera feed darkens to simulate a dark room. This uses the [Passthrough Styling Feature](https://developers.meta.com/horizon/documentation/unity/unity-customize-passthrough-styling/) of the Meta Quest SDK with a [Look-Up Table](https://developers.meta.com/horizon/documentation/unity/unity-customize-passthrough-color-mapping/#color-look-up-tables-luts) (LUT). A central manager controls this to ensure consistency if both interactions are enabled simultaneously. The manager also ensures the effect is replicated for all users in multi-user scenarios.

### Relevant Files
- [PassthroughChanger.cs](../Assets/CrypticCabinet/Scripts/Passthrough/PassthroughChanger.cs)
- [PassthroughConfigurator.cs](../Assets/CrypticCabinet/Scripts/Passthrough/PassthroughConfigurator.cs)

# Camera Darkens When Inside Objects

The camera darkens if the user tries to put their head inside furniture like the Orrery. Initially, a Unity [Volume](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/Volumes.html) was used to fade out visible geometry as the user approached. However, this didn't affect [passthrough](https://developers.meta.com/horizon/documentation/unity/unity-passthrough/) rendering due to privacy restrictions. To overcome this, an additional trigger volume using OnTriggerEnter and OnTriggerExit was added to enable and disable passthrough. This trigger volume is slightly smaller than the fade volume, ensuring a smooth passthrough switch-off during the 3D geometry fading effect.

### Relevant Files
- [BlackoutVolume.cs](../Assets/CrypticCabinet/Scripts/Utils/BlackoutVolume.cs)

# Safe Dials

<img src="../Documentation/Images/Safe.gif" width="50%" height="50%">

There is no off-the-shelf solution for using a single finger to swipe through safe numbers naturally. The [final implementation](../Assets/CrypticCabinet/Scripts/Puzzles/Safe/SwipeDetector.cs) uses a trigger collider on the user's index finger to detect swipes. When a swipe is detected, the number carousel animates up or down. Once all dials read the correct values, the safe door opens.

<img src="../Documentation/Images/DialColliders.png" width="50%" height="50%">

### Relevant Files
- [SafeLockChecker.cs](../Assets/CrypticCabinet/Scripts/Puzzles/Safe/SafeLockChecker.cs)
- [SwipeDetector.cs](../Assets/CrypticCabinet/Scripts/Puzzles/Safe/SwipeDetector.cs)
- [SafeStateMachine.cs](../Assets/CrypticCabinet/Scripts/Puzzles/Safe/SafeStateMachine.cs)

# Clock

<img src="../Documentation/Images/Clock.gif" width="50%" height="50%">

The clock time selection feature uses the [OneGrabRotateTransformer](https://developers.meta.com/horizon/documentation/unity/unity-isdk-grabbable/#one-grab-transformers) feature of the Meta Quest API. The handle's local rotation drives the clock hands' rotation, detecting when the user selects the correct time. A “click” sound helps users know when the clock hands are in the correct position, triggering the clock door to open.

### Relevant Files
- [ClockSpinner.cs](../Assets/CrypticCabinet/Scripts/Puzzles/Clock/ClockSpinner.cs)
- [ClockHandMover.cs](../Assets/CrypticCabinet/Scripts/Puzzles/Clock/ClockHandMover.cs)

# Key & UV Bulb

<p>
    <img src="../Documentation/Images/UVbulb_screw.gif" width="40%" height="40%" >
    <img src="../Documentation/Images/UVbulb_unscrew.gif" width="40%" height="40%" >
</p>

Both the UV bulb and the key require two interaction modes. First, a mode for free manipulation, allowing users to grab, move, and rotate objects. Second, a mode where the object is locked to a position and can only rotate around a single axis. Although both modes exist in the Meta Quest API as [OneGrabFreeTransformer and OneGrabRotateTransformer](https://developers.meta.com/horizon/documentation/unity/unity-isdk-grabbable/#one-grab-transformers), they can't be dynamically switched after the [Grabbable](https://developers.meta.com/horizon/documentation/unity/unity-isdk-grabbable/) is initialized. The solution is a new script, [OneGrabToggleRotateTransformer](../Assets/CrypticCabinet/Scripts/Interactions/OneGrabToggleRotateTransformer.cs), combining both functionalities with logic to toggle between them at runtime.

For the key, once snapped into the lock, it switches to rotation-only mode, tracking rotation until it's spun anti-clockwise enough to unlock the drawer. For the bulbs, they need to be snapped in place, screwed, and/or unscrewed. This requires toggling between free movement and locked rotation when snapped/unsnapped, then raising/lowering the bulb during the screwing/unscrewing motion.

<img src="../Documentation/Images/Key.gif" width="50%" height="50%">

### Relevant Files
- [ScrewableObject.cs](../Assets/CrypticCabinet/Scripts/Interactions/ScrewableObject.cs)
- [ScrewSnapZone.cs](../Assets/CrypticCabinet/Scripts/Interactions/ScrewSnapZone.cs)
- [OneGrabToggleRotateTransformer.cs](../Assets/CrypticCabinet/Scripts/Interactions/OneGrabToggleRotateTransformer.cs)
