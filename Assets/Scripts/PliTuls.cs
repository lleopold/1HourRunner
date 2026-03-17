using System;
using UnityEngine.InputSystem;

namespace ZombieGame
{
    static class PliTuls
    {
        /// <summary>
        /// Initializes the PlayerInput and registers all input event handlers.
        /// Call this from Awake() in your PlayerControllerInput class.
        /// </summary>
        public static global::ZombieGame.PlayerInputIS InitializeInput(
            Action<InputAction.CallbackContext> move,
            Action<InputAction.CallbackContext> jump,
            Action<InputAction.CallbackContext> sprint,
            Action<InputAction.CallbackContext> aiming,
            Action<InputAction.CallbackContext> aimingControllerRightStick,
            Action<InputAction.CallbackContext> aimingControllerTrigger,
            Action<InputAction.CallbackContext> shooting,
            Action<InputAction.CallbackContext> weaponSwitch)
        {
            var playerInput = new global::ZombieGame.PlayerInputIS();
            RegisterInputEvents(playerInput, move, jump, sprint, aiming, aimingControllerRightStick, aimingControllerTrigger, shooting, weaponSwitch);
            return playerInput;
        }

        /// <summary>
        /// Safely attaches input event handlers if playerInput is not null.
        /// </summary>
        public static void RegisterInputEvents(global::ZombieGame.PlayerInputIS playerInput,
            Action<InputAction.CallbackContext> move,
            Action<InputAction.CallbackContext> jump,
            Action<InputAction.CallbackContext> sprint,
            Action<InputAction.CallbackContext> aiming,
            Action<InputAction.CallbackContext> aimingControllerRightStick,
            Action<InputAction.CallbackContext> aimingControllerTrigger,
            Action<InputAction.CallbackContext> shooting,
            Action<InputAction.CallbackContext> weaponSwitch)
        {
            if (playerInput == null) return;

            playerInput.Gameplay.Move.performed += move;
            playerInput.Gameplay.Move.canceled += move;
            playerInput.Gameplay.Move.started += move;
            playerInput.Gameplay.Jump.performed += jump;
            playerInput.Gameplay.Jump.started += jump;
            playerInput.Gameplay.Sprint.performed += sprint;
            playerInput.Gameplay.Sprint.canceled += sprint;
            playerInput.Gameplay.Aim.started += aiming;
            playerInput.Gameplay.Aim.canceled += aiming;
            playerInput.Gameplay.Aim.performed += aiming;
            playerInput.Gameplay.AimControllerRightStick.started += aimingControllerRightStick;
            playerInput.Gameplay.AimControllerRightStick.performed += aimingControllerRightStick;
            playerInput.Gameplay.AimControllerRightStick.canceled += aimingControllerRightStick;
            playerInput.Gameplay.AimingTrigger.performed += aimingControllerTrigger;
            playerInput.Gameplay.AimingTrigger.canceled += aimingControllerTrigger;
            playerInput.Gameplay.AimingTrigger.started += aimingControllerTrigger;
            playerInput.Gameplay.Fire.performed += shooting;
            playerInput.Gameplay.Fire.started += shooting;
            playerInput.Gameplay.Fire.canceled += shooting;

            // ✨ NEW: Register weapon switch
            // You'll need to add "WeaponSwitch" action to your Input Actions asset
            // For now, using keyboard keys directly in Update as fallback

            playerInput.Gameplay.Enable();
            playerInput.UI.Enable();
        }

        /// <summary>
        /// Safely detaches input event handlers if playerInput is not null.
        /// </summary>
        public static void UnregisterInputEvents(global::ZombieGame.PlayerInputIS playerInput,
            Action<InputAction.CallbackContext> move,
            Action<InputAction.CallbackContext> jump,
            Action<InputAction.CallbackContext> sprint,
            Action<InputAction.CallbackContext> aiming,
            Action<InputAction.CallbackContext> aimingControllerRightStick,
            Action<InputAction.CallbackContext> aimingControllerTrigger,
            Action<InputAction.CallbackContext> shooting,
            Action<InputAction.CallbackContext> weaponSwitch) // ✨ NEW parameter
        {
            if (playerInput == null) return;

            playerInput.Gameplay.Move.performed -= move;
            playerInput.Gameplay.Move.canceled -= move;
            playerInput.Gameplay.Move.started -= move;
            playerInput.Gameplay.Jump.performed -= jump;
            playerInput.Gameplay.Jump.started -= jump;
            playerInput.Gameplay.Sprint.performed -= sprint;
            playerInput.Gameplay.Sprint.canceled -= sprint;
            playerInput.Gameplay.Aim.started -= aiming;
            playerInput.Gameplay.Aim.canceled -= aiming;
            playerInput.Gameplay.Aim.performed -= aiming;
            playerInput.Gameplay.AimControllerRightStick.started -= aimingControllerRightStick;
            playerInput.Gameplay.AimControllerRightStick.performed -= aimingControllerRightStick;
            playerInput.Gameplay.AimControllerRightStick.canceled -= aimingControllerRightStick;
            playerInput.Gameplay.AimingTrigger.performed -= aimingControllerTrigger;
            playerInput.Gameplay.AimingTrigger.canceled -= aimingControllerTrigger;
            playerInput.Gameplay.AimingTrigger.started -= aimingControllerTrigger;
            playerInput.Gameplay.Fire.performed -= shooting;
            playerInput.Gameplay.Fire.started -= shooting;
            playerInput.Gameplay.Fire.canceled -= shooting;

            playerInput.Gameplay.Disable();
            playerInput.UI.Disable();
        }

    }
}
