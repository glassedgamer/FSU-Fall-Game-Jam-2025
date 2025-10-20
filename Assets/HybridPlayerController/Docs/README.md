Hybrid Player Controller

This package provides a modular character controller that works in either first or third person for Unity. The included DemoLevel scene showcases all traversal and movement features.



Please Note: if the playerController is not functioning properly, or you are getting errors, please make sure the layers are set appropriately according to the "Layer \& Tag Setup" section under "Requirements \& Setup" in the HybridPlayerControllerDoc.pdf (found at HybridPlayerController/Docs).



Key Features
-First and third person camera modes with configurable positions, with field of view and sensitivity feilds.
-State Machine based movement with many player states.
-Traversal abilities such as ledge climbing, wall running, vaulting, swing bars and grappling.
-Ground and wall detection for slopes, steep surfaces, steps, and vault logic.
-Abilities can be locked and unlocked in-engine on the player's PlayerUtils component and unlocked during play with the UnlockStateTrigger component.
-Smooth interaction with moving platforms.
-Uses new input system.
-Checkpoint handling provided.



Player States
-IdleState       – Player stands still.
-WalkState       – Walking movement.
-SprintState     – Fast movement while sprinting.
-JumpState       – Normal jump from most states.
-RisingState     – Upward movement after jumping.
-FallingState    – Downward movement with coyote time (a grace jump window).
-DiveState       – Quick dive forward.
-CrouchState     – Player crouches in place.
-CrouchWalkState – Movement while crouched.
-SlideState      – Sliding on the ground from sprinting.
-SlipState       – Automatic slide down steep slopes.
-LedgeState      – Grab and move along a ledge before climbing up or jumping off.
-VaultState      – Vault over a low wall or obstacle.
-VaultJumpState  – Optional jump off of wall at the peak of a vault.
-SwingBarState   – Swing from a bar.
-WallRunState    – Run along walls from sprinting.
-GrappleState    – Shoot at a grapple surface and swing.



Contents
-Demo scenes "Debug" and "DemoLevel" located in HybridPlayerController/Extras/DemoScenes.
-Prefabs for the player, checkPoints, killZones, unlockStateTriggers, swing bars, moving platforms and grapple surfaces.
-Scripts implementing the controller, state machine, utilities and editor helpers.



Default Inputs
Keyboard and Mouse

* Move: WASD or Arrow Keys
* Look: Mouse movement
* Jump: Space
* Sprint: Shift
* Crouch/Dive: Ctrl
* Fire: Left Mouse Button
* Pause: Tab



Controller

* Move: Left Stick
* Look: Right Stick
* Jump: South Button
* Sprint: Left Stick Press
* Crouch/Dive: West Button
* Fire: Right Trigger
* Pause: Start



Requirements

* Supports Unity 5 \& 6
* Requires New Input System
* Demos require Text Mesh Pro



Open the DemoLevel scene and press Play to test the controller. Input mappings are stored in HybridPlayerControls.inputactions and can be remapped with the Input System.

