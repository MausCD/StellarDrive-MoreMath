# MoreMath

MoreMath expands the Signal Combinator and Signal Processor with additional math operations, logic gates, timers, stateful logic, and an operation selection menu.

All my mods are listed at [stellardrive.mauscd.de](https://stellardrive.mauscd.de).

## Features

### Operation Selection Menu

Interacting with a Signal Combinator or Signal Processor opens a selection menu in the style of the game's build menu. All available operations are shown as tiles with their symbol and full name. Click a tile to apply the operation.

- The menu shows only the operations of the selected component.
- Close the menu with `Esc` or `F`.
- While the menu is open, the cursor is enabled and camera movement, hotbar, and other gameplay inputs are blocked.
- The menu can be disabled with `EnableMenu` to cycle through operations instead, as in the original game.

### Signal Combinator

- Power (^): Raises A to the power of B.
- Round: Rounds A to B decimal places.
- Less or equal (≤): Outputs 1 if A ≤ B, otherwise 0.
- Greater or equal (≥): Outputs 1 if A ≥ B, otherwise 0.
- Not equal (≠): Outputs 1 if A ≠ B, otherwise 0.
- Remainder (%): Returns the remainder of A divided by B. Outputs 0 if B is 0.
- Clamp: Limits A to the range −|B| to +|B|.
- Hypot: Calculates the hypotenuse √(A² + B²), for example the length of a vector with the components A and B.
- Deadband: Outputs 0 while |A| < |B|; otherwise, passes A through unchanged.
- Log: Calculates the logarithm of A to base B. For example, A = 1000 and B = 10 produces 3. Requires finite inputs with A > 0, B > 0, and B ≠ 1; otherwise, outputs 0.
- OR: Outputs 1 if A or B is active, otherwise 0.
- XOR: Outputs 1 if exactly one of A and B is active, otherwise 0.
- NAND: Outputs 0 if A and B are both active, otherwise 1.
- NOR: Outputs 1 if A and B are both inactive, otherwise 0.
- RS: Reset-priority latch. A sets the output to 1, B resets it to 0. Reset wins when both inputs are active.
- SR: Set-priority latch. A sets the output to 1, B resets it to 0. Set wins when both inputs are active.
- Schmitt Trigger: Switches the output to 1 when A rises above B and back to 0 when A falls below −|B|. Between the two thresholds, the output keeps its current value.
- OnDelay (TON): Outputs 1 after A has remained active continuously for B seconds. When A becomes inactive, the output immediately returns to 0 and the timer resets.
- OffDelay (TOF): Outputs 1 immediately when A becomes active. When A becomes inactive, the output remains 1 for B seconds before returning to 0. Reactivating A cancels the pending switch-off.
- Timer: Counts the seconds A has been active. The value is held while A is inactive. B resets the timer to 0.
- Counter: Counts up by 1 each time A becomes positive and down by 1 each time A becomes negative. Holding A does not count repeatedly. B resets the counter to 0.

### Signal Processor

- Floor: Rounds the input down to the nearest integer.
- Ceil: Rounds the input up to the nearest integer.
- Sign (sgn): Outputs 1 for positive input, −1 for negative input, and 0 for zero.
- Exp (eˣ): Calculates e raised to the power of the input.
- NOT: Outputs 1 if the input is inactive, otherwise 0.
- OneWay: Passes the input through unchanged.
- Toggle: Switches the output between 0 and 1 whenever the input changes from inactive to active. Holding the input active does not repeatedly switch the output.
- Integrator (∫): Adds the input multiplied by the elapsed time to the output every update. A constant input of 1 increases the output by 1 per second.

For OR, XOR, NAND, NOR, NOT, RS, SR, Toggle, OnDelay, OffDelay, Timer, and the reset input of Counter, positive signal input values count as active. Zero and negative values count as inactive.

## Custom Texture

MoreMath includes an optional texture replacement that adds I (Input) and Q (Output) labels to the components, making signal connections easier to identify.

Operations with special inputs get matching labels automatically:

| Labels | Operations |
|---|---|
| I / P / Q (Input, Parameter, Output) | Round, Clamp, Deadband, Log, Schmitt Trigger |
| I / PT / Q (Input, Preset Time, Output) | OnDelay, OffDelay |
| I / R / Q (Input, Reset, Output) | Timer, RS, SR |
| C / R / CV (Count, Reset, Current Value) | Counter |
| I1 / I2 / Q1 | All other operations |

## Configuration

The following options can be adjusted in the Melon Preferences:

| Setting | Default | Description |
|---|---|---|
| `EnableMenu` | `true` | Opens the operation selection menu on interaction. When disabled, each interaction cycles to the next operation. |
| `SaveStatus` | `true` | Enables or disables saving and restoring RS, SR, and Toggle states. |
| `ReplaceTexture` | `true` | Enables or disables the custom textures with input and output labels. |

## Persistent States

When `SaveStatus` is enabled in the Melon Preferences, the mod saves and restores RS, SR, and Toggle states across game sessions. Toggle also preserves whether its previous input was active.

These states use a mod-specific encoding in the save data. Keep MoreMath installed when loading saves that use this feature.

OnDelay and OffDelay timer progress is not saved. Their timers reset when a game is loaded. Schmitt Trigger, Integrator, Timer, and Counter values are not covered by this feature.

## Usage

### Selecting an Operation

1. Look at a Signal Combinator or Signal Processor.
2. Press the interaction key to open the menu.
3. Click the desired operation.

With `EnableMenu` set to `false`, pressing the interaction key cycles through all vanilla and MoreMath operations instead.

### RS and SR

- Input A: Set.
- Input B: Reset.
- Both inputs inactive: Keep the current output.
- Both inputs active: RS resets the output to 0; SR sets it to 1.

### Schmitt Trigger

- Input A: Signal to monitor.
- Input B: Switch-on threshold. The switch-off threshold is −|B|.
- A > B: Output switches to 1.
- A < −|B|: Output switches to 0.
- In between: Keep the current output.

Example with B = 5: The output switches on above 5 and only switches off again below −5.

### Deadband

- Input A: Signal.
- Input B: Deadband width.

Example with B = 0.1: Small values such as 0.05 or −0.08 produce 0; values such as 0.3 or −0.5 are passed through unchanged. Useful for suppressing small fluctuations, for example from joysticks or sensors.

### Toggle

- Send a positive pulse to switch the output.
- Return the input to zero or below before sending the next pulse.

### Integrator

- Input: Value to integrate per second.
- The output keeps growing or shrinking as long as the input is not zero.
- An input of 0 keeps the current output.

### OnDelay (TON)

- Input A: Signal to monitor.
- Input B: Delay in seconds. Negative values are treated as 0.
- Keep A active for the full delay to switch the output to 1.
- Deactivating A immediately resets the output and timer.

### OffDelay (TOF)

- Input A: Signal to monitor.
- Input B: Delay in seconds. Negative values are treated as 0.
- Activating A immediately switches the output to 1.
- Deactivating A starts the switch-off delay.
- Reactivating A during that delay keeps the output on and cancels the pending switch-off.

### Timer

- Input A: Signal to measure.
- Input B: Reset.
- A active: The output increases by 1 per second.
- A inactive: The output keeps its current value.
- B active: The output is reset to 0 and stays there while B is active.

Example: Keep A active for 3 seconds, pause, then keep it active for 2 more seconds. The output shows 5.

### Counter

- Input A: Count pulses. A positive pulse counts up, a negative pulse counts down.
- Input B: Reset.
- Only the change of A is counted. Return A to 0 before the next pulse.
- B active: The output is reset to 0. Reset wins when both inputs are active.

Example: Three positive pulses followed by one negative pulse produce 2.

### Log

- Input A: Value.
- Input B: Logarithm base.

Examples:

| A | B | Output |
|---|---|---|
| 1000 | 10 | 3 |
| 8 | 2 | 3 |
| 1 | 10 | 0 |

### Hypot

| A | B | Output |
|---|---|---|
| 3 | 4 | 5 |
| 6 | 8 | 10 |

## Requirements

- MelonLoader

## Installation

1. Install MelonLoader for the game.
2. Download and extract the MoreMath release ZIP.
3. Place `MoreMath.dll` in the game's `Mods` folder.
4. Launch the game.

Alternatively, install the mod using the **Stellar Drive Mod Manager**.

## Save Compatibility

Back up your saves before using the persistence feature.

MoreMath stores state information in unused bits of the saved operation IDs. Other mods that modify these IDs or the same signal-processing methods may conflict with it.

Persistence currently covers RS, SR, and Toggle only, not OnDelay or OffDelay timer progress, Schmitt Trigger, Integrator, Timer, or Counter values.
