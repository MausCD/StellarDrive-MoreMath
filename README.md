# MoreMath

MoreMath expands the Signal Combinator and Signal Processor with additional math operations and stateful logic.

All My Mods ar listed on https://stellardrive.mauscd.de

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

### Signal Processor

- Floor: Rounds the input down to the nearest integer.
- Toggle: Switches the output between 0 and 1 whenever the input changes from inactive to active. Holding the input active does not repeatedly switch the output.

For RS, SR, and Toggle, positive input values count as active. Zero and negative values count as inactive.

## Persistent States

The mod includes save/load support for RS, SR, and Toggle states. Toggle also saves whether its previous input was active.

These states use a mod-specific encoding in the save data. Keep MoreMath installed when loading saves that use this feature.

Signals that change while the signal network initializes after loading may still affect the restored state.

## Usage

For RS and SR:

- Input A: Set
- Input B: Reset
- Both inputs inactive: Keep the current output

For Toggle:

- Send a positive pulse to switch the output.
- Return the input to zero or below before sending the next pulse.

## Requirements

- MelonLoader

## Installation

1. Install MelonLoader for the game.
2. Download and extract the MoreMath release ZIP.
3. Place `MoreMath.dll` in the game's `Mods` folder.
4. Launch the game.

or use the **Stellar Drive Mod Manager**

## Save Compatibility

Back up your saves before using the persistence feature.

MoreMath stores state information in unused bits of the saved operation IDs. Other mods that modify these IDs or the same signal-processing methods may conflict with it.
