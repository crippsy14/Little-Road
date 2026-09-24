# PS5 DualSense support on Windows

## Developer utility follow-up — 19 September 2026

The user suggested a reusable controller utility. Recommended scope: an opt-in in-game diagnostic panel showing the active device, raw/processed stick and trigger values, button states, dead zones and connection events; plus reusable scripted input scenarios through Unity's existing test path. The current `AdventureGame` and `VillageReview` already create synthetic Unity gamepads and queue states for menu/driving checks. Extend that foundation rather than building a system-wide input driver as part of this milestone. Synthetic tests do not validate the physical DualSense, USB/Bluetooth or actual reconnect behaviour.

A connected physical controller provides input; it is not a device the automation can remotely press. The present computer-use API supports keyboard/mouse, not gamepad injection. Cross-application emulator control would need a separate input route such as emulator keyboard bindings or a virtual controller. Microsoft's [SendInput](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-sendinput) injects keyboard/mouse events; the established [ViGEmBus virtual-controller project](https://github.com/nefarius/ViGEmBus) is archived. Defer a system-wide virtual-controller utility unless recurring reference/testing needs justify researching a maintained solution. No driver, emulator mapping or controller setting was changed during this planning pass.

Utility acceptance: identify hardware versus synthetic devices; show stick/trigger calibration clearly; retain correct focus on reconnect; release all synthetic controls and remove the test device on cancellation/error; do not intercept other apps or log unrelated input; keep test scenarios opt-in and out of normal gameplay. This is a planned developer aid, not an implemented utility.

## Hardware requirement

Explicit requirement added 16 September 2026. Target USB and Bluetooth on the user's Windows 11 PC. This refers to using a PS5 controller on PC, not exporting the game to a PlayStation console.

Sony documents Windows support over USB and Bluetooth, with some advanced features depending on the game/connection. [Official DualSense PC guide](https://www.playstation.com/en-gb/support/hardware/pair-dualsense-controller-bluetooth/). Godot's desktop input uses SDL 3 in current releases. [Godot controller documentation](https://docs.godotengine.org/en/stable/tutorials/inputs/controllers_gamepads_joysticks.html).

## Default mapping

| Action | DualSense |
| --- | --- |
| Steer | Left stick |
| Accelerate | R2, analogue |
| Brake, then reverse | L2, analogue |
| Handbrake | Circle |
| Talk / confirm | Cross |
| Menu back | Circle |
| Reset to valid road position | Triangle |
| Pause | Options |
| Menu navigation | D-pad or left stick |
| Camera look | Right stick, when camera look is implemented |
| Future horn / special equipment | L1 / R1, remappable |

Menus, shop comparisons, fitting, dialogue, race selection, results, retry and settings must work without a mouse. Provide PlayStation prompts when selected or detected; allow manual prompt override. All gameplay actions should be remappable, with persistent dead zone, steering sensitivity and trigger threshold settings. Basic keyboard use remains available alongside the controller.

## Acceptance matrix

Test the standalone Windows export with a real DualSense, separately for USB and Bluetooth:

- Cold start connected; connect after launch; disconnect while driving; reconnect and resume.
- No steering drift at rest; full analogue steering range; triggers read independently from zero to full pressure.
- Drive, brake, reverse, recover and complete a race; navigate every menu without mouse input.
- Correct Cross/Circle/Triangle/Options prompts; keyboard/controller switching works.
- Connection loss pauses active driving safely. Reconnection requires a deliberate resume, with no stuck throttle.
- Avoid duplicate input if another mapping layer is present; test the normal direct connection first.
- Persist remapping and calibration across restarts; reset defaults remains accessible.

Ordinary rumble is desirable once tested and should have an off switch. Adaptive trigger resistance and advanced DualSense haptics are optional later integrations; they are not claimed from generic gamepad mappings. Physical controller acceptance remains unverified until hardware is connected and exercised.
