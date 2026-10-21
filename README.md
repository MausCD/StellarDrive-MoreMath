# MoreMath

MoreMath expands the Signal Combinator and Signal Processor with additional math operations, timers, and stateful logic.

All my mods are listed at [stellardrive.mauscd.de](https://stellardrive.mauscd.de).

## Features

### Signal Combinator

- Power (^): Raises A to the power of B.
- Round: Rounds A to B decimal places.
- Less or equal (≤): Outputs 1 if A ≤ B, otherwise 0.
- Greater or equal (≥): Outputs 1 if A ≥ B, otherwise 0.
- Remainder (%): Returns the remainder of A divided by B. Outputs 0 if B is 0.
- Clamp: Limits A to the range −|B| to +|B|.
- RS: Reset-priority latch. A sets the output to 1, B resets it to 0. Reset wins when both inputs are active.
- SR: Set-priority latch. A sets the output to 1, B resets it to 0. Set wins when both inputs are active.
- OnDelay (TON): Outputs 1 after A has remained active continuously for B seconds. When A becomes inactive, the output immediately returns to 0 and the timer resets.
- OffDelay (TOF): Outputs 1 immediately when A becomes active. When A becomes inactive, the output remains 1 for B seconds before returning to 0. Reactivating A cancels the pending switch-off.
- Log: Calculates the logarithm of A to base B. For example, A = 1000 and B = 10 produces 3. Requires finite inputs with A > 0, B > 0, and B ≠ 1; otherwise, outputs 0.

### Signal Processor

- Floor: Rounds the input down to the nearest integer.
- Toggle: Switches the output between 0 and 1 whenever the input changes from inactive to active. Holding the input active does not repeatedly switch the output.

For RS, SR, Toggle, OnDelay, and OffDelay, positive signal input values count as active. Zero and negative values count as inactive.

## Custom Texture

MoreMath includes an optional texture replacement that adds I (Input) and Q (Output) labels to the components, making signal connections easier to identify.

## Configuration

The following options can be adjusted in the Melon Preferences:

| Setting | Description |
|---|---|
| `SaveStatus` | Enables or disables saving and restoring RS, SR, and Toggle states. |
| `ReplaceTexture` | Enables or disables the custom texture with I (Input) and Q (Output) labels. |

## Persistent States

When `SaveStatus` is enabled in the Melon Preferences, the mod saves and restores RS, SR, and Toggle states across game sessions. Toggle also preserves whether its previous input was active.

These states use a mod-specific encoding in the save data. Keep MoreMath installed when loading saves that use this feature.

OnDelay and OffDelay timer progress is not saved. Their timers reset when a game is loaded.

## Usage

### RS and SR

- Input A: Set.
- Input B: Reset.
- Both inputs inactive: Keep the current output.
- Both inputs active: RS resets the output to 0; SR sets it to 1.

### Toggle

- Send a positive pulse to switch the output.
- Return the input to zero or below before sending the next pulse.

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

### Log

- Input A: Value.
- Input B: Logarithm base.

Examples:

| A | B | Output |
|---|---|---|
| 1000 | 10 | 3 |
| 8 | 2 | 3 |
| 1 | 10 | 0 |

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

Persistence currently covers RS, SR, and Toggle only, not OnDelay or OffDelay timer progress.