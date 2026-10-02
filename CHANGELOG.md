# Changelog

## 1.2.1

- Rename the mod to New LvlUp Effect (plugin ID `local.valheim.newlvlupeffect`, console command `newlvlupeffect_preview`, DLL `NewLvlUpEffect.dll`).
- Fix vanilla notification suppression by patching Player.Message instead of the empty Character base method.
- Apply ReplaceVanillaMessage to on-screen raiseskill and raiseskill all messages too; console output remains available.
- Keep the vanilla message when the custom view cannot be initialised.

## 1.2.0

- Replace the side-by-side level display with an animated old-to-new number transition.
- Add a brief glow as the new number settles.
- Clip rolling numbers to their own display area.
- Preview now demonstrates 5 → 6 without changing skills.
- Add release packaging, author metadata and MIT licensing.

## 1.1.1 — development build

- Move the notification to an independent screen canvas.
- Fix the undersized notification caused by nested canvas dimensions.
- Fix medallion ring transparency and add layout diagnostics.

## 1.1.0 — development build

- Introduce the minimal Nordic design, medallion and soft effects.

## 1.0.1 — development build

- Support console skill increases and pause notifications while the console is open.

## 1.0.0 — development build

- Initial skill notification, queue, configuration and preview command.
