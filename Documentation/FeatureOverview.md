# Room Setup
This section describes the system used to automatically populate a generic real world room with game objects.
The [Scene API](https://developer.oculus.com/documentation/unity/unity-scene-overview/) is used to understand the space the user is in. Using the returned scene primitives, a 3d grid of cells is generated to cover the entire room and track where scene objects are placed.
This is to allow the room setup system to detect safe locations where to place game objects and to minimize overlapping with real world objects. This requires the user to set up their scene accurately to ensure the best possible experience.
Free space in the 3d grid is visualized as green cells, while non-valid locations are shown in red. For example, in the following image you can see that the wall has some available space (green), but we are excluding the window (in red).Same for the floor which has red areas underneath desks and shelving.

Note: To visualize the 3d grid in build, toggle the debug view by pressing down and holding on the left joystick, then press A on the right controller.

<img src="../Documentation/Images/RoomSetup.png" width="40%" height="40%" >

There are four categories of placement location; “floor”, “wall”, “desks” and “against wall”. “Against wall” objects are the ones that need to be on the floor but also against a wall: a good example of this is the cabinet.
As an object is placed it blocks out the cells that it covers to prevent other objects from being placed in the same location.
Objects are placed in the following order:
1. Objects on floor against a wall 
2. Objects on walls 
3. Objects on desks (if no desks available, fallback to floor)
4. Objects on floor 
5. Objects on any horizontal location.

Within each category objects are placed in order of size: largest to smallest.
If an object fails to find a safe location, it is placed in a  random valid location. For example, wall objects will stay on walls and floor objects will stay on floor, even if they are overlapping with other objects or scene objects.

After all objects have been automatically placed, there’s a few seconds of “easing”: this allows for any objects that are overlapping to repel away from each other.The easing is done using the Unity physics engine function [ComputePenetration](https://docs.unity3d.com/ScriptReference/Physics.ComputePenetration.html), this gives a direction and distance to move objects to cause them to no longer overlap. Rather than jumping the objects to these new locations, this system lerps towards the location allowing opportunities for multiple objects to move away from each other over time.


Once the placement is complete, the user then has an opportunity to preview the generated layout. Objects in this phase are visualized as colored boxes:
 - Blue boxes: items that need to be physically reached during the experience because the user will interact with them using hands/controllers.
 - Green boxes: items that need to be visible, but don’t need to be reached.
 - Red boxes: items that are in a non valid location, are overlapping with each other or are overlapping with scene objects.
In this phase, the user has an opportunity to manually move these boxes around, if necessary, to make sure there’s no red boxes and that all blue boxes can be physically reached.

<img src="../Documentation/Images/SpawningBoxes.png" width="50%" height="50%">

After this, the user can confirm the layout and start the experience.  Object positions are then stored and used during gameplay to avoid re-doing these heavy calculations.

### Relevant Files
- [ObjectPlacementManager.cs](../Assets/CrypticCabinet/Scripts/Utils/ObjectPlacementManager.cs)
- [SceneUnderstandingLocationPlacer.cs](../Assets/CrypticCabinet/Scripts/SceneManagement/SceneUnderstandingLocationPlacer.cs)
- [FloorSpaceFinder.cs](../Assets/CrypticCabinet/Scripts/SceneManagement/FloorSpaceFinder.cs)
- [WallSpaceFinder.cs](../Assets/CrypticCabinet/Scripts/SceneManagement/WallSpaceFinder.cs)
- [DeskSpaceFinder.cs](../Assets/CrypticCabinet/Scripts/SceneManagement/DeskSpaceFinder.cs)

# Networking
Multiplayer is connected and managed via [Photon Fusion](https://doc.photonengine.com/realtime/current/getting-started/quick-start), given the app is designed for colocation it’s not necessary to have a lobby screen. When the host creates a new game they are presented with a room code that they then share with their guests. This is handled within [PhotonConnector](../Assets/CrypticCabinet/Scripts/Photon/PhotonConnector.cs). Also colocation events are triggered from here as well. A deep dive can be found [here](https://developer.oculus.com/documentation/unity/unity-colocation-deep-dive/) and called from within [ColocationDriverNetObj](../Assets/CrypticCabinet/Scripts/Colocation/ColocationDriverNetObj.cs).

### Relevant Files
- [GrabPassOwnership.cs](../Assets/CrypticCabinet/Scripts/Utils/GrabPassOwnership.cs)
- [NetworkedSnapHandler.cs](../Assets/CrypticCabinet/Scripts/Utils/NetworkedSnapHandler.cs)
- [NetworkedSnappedObject.cs](../Assets/CrypticCabinet/Scripts/Utils/NetworkedSnappedObject.cs)

# Rope
<img src="../Documentation/Images/Rope.gif" width="40%" height="40%">

After an initial prototype based on a chain of physics colliders, it was decided to opt for a Verlet rope implementation instead, heavily based on [this open source example](https://github.com/GaryMcWhorter/Verlet-Chain-Unity). The issue with the first prototype was that the result looked like the rope was made up of sticks and was quite difficult to tune, while the second approach gave much more realistic results. 
It took a few iterations to fine tune the correct amount of bones for the skeleton of the final asset, to make sure it looked fluid without affecting performance. 
Another aspect to improve its look and feel was to avoid making it feel elastic: that’s why, when pulling, it was preferred to make it come out of the ceiling instead of stretching it.
In terms of interaction, the rope can be grabbed from any point along its length, using one or two hands. This is implemented by placing a Grabbable on the rope following the user's hand, once the user grabs the Grabbable it locks to the nearest node in the rope and that node then follows the user's hand. This is duplicated with a second Grabbable for the other hand. These grabbed positions are synchronized with the other users over the network so remote users can see the active player grabbing the rope.
Another important element to make the rope feel more real was to ensure that it collided correctly with walls and other objects in the scene. Collisions are calculated using [Physics.OverlapSphereNonAlloc](https://docs.unity3d.com/ScriptReference/Physics.OverlapSphereNonAlloc.html) and [Physics.ComputePenetration](https://docs.unity3d.com/ScriptReference/Physics.ComputePenetration.html) to push rope nodes away from each other and from scene objects. In terms of multiplayer,each client calculates their own rope updates, but they share some fixed locations when the rope is being held by a user.

### Relevant Files
- [Rope.cs](../Assets/CrypticCabinet/Scripts/Puzzles/SandPuzzle/Rope.cs)

# LUT
When the user activates the UV light or the Orrery projection, the passthrough camera feed is darkened to give the effect of being in a dark room, this is achieved using the [Passthrough Styling Feature](https://developer.oculus.com/documentation/unity/unity-customize-passthrough-styling/) of the  Meta Quest SDK with a [Look Up Table](https://developer.oculus.com/documentation/unity/unity-customize-passthrough-color-mapping/#color-look-up-tables-luts) (LUT). We use a central manager to control this to ensure consistency if both interactions are enabled at the same time. The manager is also used for multi-user scenarios to make sure that the effect is replicated for all users in the experience.

### Relevant Files
- [PassthroughChanger.cs](../Assets/CrypticCabinet/Scripts/Passthrough/PassthroughChanger.cs)
- [PassthroughConfigurator.cs](../Assets/CrypticCabinet/Scripts/Passthrough/PassthroughConfigurator.cs)

# Camera darkens when inside objects
The camera darkens if the user tries to put their head inside a piece of furniture such as the Orrery. Initially the use of a Unity [Volume](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/Volumes.html) (part of the Universal Render Pipeline) was explored. This provided an easy way to fade out the majority of visible geometry as the user got closer. Unfortunately this approach did not have an effect on the [passthrough](https://developer.oculus.com/documentation/unity/unity-passthrough/) rendering, since developers can not directly access the passthrough video feed for privacy reasons. To overcome this limitation,it was decided to add an additional trigger volume using OnTriggerEnter and OnTriggerExit to enable and disable the passthrough. This trigger volume is slightly smaller than the fade volume so that the passthrough smoothly switches off in the middle of the 3D geometry fading effect.

### Relevant Files
- [BlackoutVolume.cs](../Assets/CrypticCabinet/Scripts/Utils/BlackoutVolume.cs)

# Safe Dials
<img src="../Documentation/Images/Safe.gif" width="50%" height="50%">

There is no off the shelf solution that would allow for a single finger to be used to swipe up and down to scroll through the numbers on the safe in a natural way. The [final implementation](../Assets/CrypticCabinet/Scripts/Puzzles/Safe/SwipeDetector.cs) simply puts a trigger collider on the index finger of the user's hands and then detects if they start in the middle of the dial and swipe up or down. When a swipe has been detected the number carousel is animated up or down and once all dials read the correct values the safe door animates open.

<img src="../Documentation/Images/DialColliders.png" width="50%" height="50%">

### Relevant Files
- [SafeLockChecker.cs](../Assets/CrypticCabinet/Scripts/Puzzles/Safe/SafeLockChecker.cs)
- [SwipeDetector.cs](../Assets/CrypticCabinet/Scripts/Puzzles/Safe/SwipeDetector.cs)
- [SafeStateMachine.cs](../Assets/CrypticCabinet/Scripts/Puzzles/Safe/SafeStateMachine.cs)

# Clock
<img src="../Documentation/Images/Clock.gif" width="50%" height="50%">

The clock time selection feature is enabled by the [OneGrabRotateTransformer](https://developer.oculus.com/documentation/unity/unity-isdk-grabbable/#one-grab-transformers) feature of the Meta Quest API. The local rotation of the handle is then used to drive the rotation of the clock hands, which are used to detect when the user has selected the correct time. The use of a specific “click” sound helps the user understand when the clock hands are in the correct position. At that point, the clock door animates open.

### Relevant Files
- [ClockSpinner.cs](../Assets/CrypticCabinet/Scripts/Puzzles/Clock/ClockSpinner.cs)
- [ClockHandMover.cs](../Assets/CrypticCabinet/Scripts/Puzzles/Clock/ClockHandMover.cs)


# Key & UV Bulb
<p>
    <img src="../Documentation/Images/UVbulb_screw.gif" width="40%" height="40%" >
    <img src="../Documentation/Images/UVbulb_unscrew.gif" width="40%" height="40%" >
</p>

Both theUV bulb and the key require two interaction modes combined together.
First, a mode where they can be freely manipulated, allowing the user to grab, move and rotate the objects as normal in the space. Second, a mode where the object is locked to a position and can only be rotated around a single axis by the user.
Although both of these exist in the Meta Quest API in the form of the [OneGrabFreeTransformer and OneGrabRotateTransformer](https://developer.oculus.com/documentation/unity/unity-isdk-grabbable/#one-grab-transformers), it is not possible to dynamically switch between them after the [Grabbable](https://developer.oculus.com/documentation/unity/unity-isdk-grabbable/) has been initialized. The solution to this is to create a new script [OneGrabToggleRotateTransformer](../Assets/CrypticCabinet/Scripts/Interactions/OneGrabToggleRotateTransformer.cs) which combines the functionality of the two aforementioned scripts with additional logic to toggle between them at runtime.
For the use case of the key, once it's snapped into the lock, it’s changed to rotation only mode and the rotation is tracked until it’s spun anti-clock wise enough to unlock the drawer.
In the case of the bulbs, it’s a little more complex as they need to be snapped in place, screwed and/or unscrewed.. This requires toggling between free movement and locked rotation when snapped/unsnapped, then raising/lowering the bulb as the screwing/unscrewing motion takes place.

<img src="../Documentation/Images/Key.gif" width="50%" height="50%">

### Relevant Files
- [ScrewableObject.cs](../Assets/CrypticCabinet/Scripts/Interactions/ScrewableObject.cs)
- [ScrewSnapZone.cs](../Assets/CrypticCabinet/Scripts/Interactions/ScrewSnapZone.cs)
- [OneGrabToggleRotateTransformer.cs](../Assets/CrypticCabinet/Scripts/Interactions/OneGrabToggleRotateTransformer.cs)
