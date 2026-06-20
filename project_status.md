# Project Status: HuXiangLianPianAccessibility

## Project Info
- **Game:** 痴情妹妹纱雪的兄控日记 Demo (狐想恋翩-妹妹篇)
- **Engine:** Unity + Naninovel
- **Architecture:** 64-bit
- **Mod Loader:** MelonLoader v0.7.3
- **Runtime:** net35 (Mono)
- **Game directory:** /home/user/.super_doubao/super-doubao-runtime/workspace/game/
- **User experience level:** A little (has some experience with modding)
- **User game familiarity:** Somewhat (knows most of the game)

## Setup Progress
- [x] Experience level determined
- [x] Game name and path confirmed
- [x] Game familiarity assessed
- [x] Game directory auto-check completed
- [x] Mod loader selected (MelonLoader) - downloading/installing
- [x] Tolk DLLs in place (all x64 files copied)
- [x] .NET SDK available (8.0.422)
- [ ] Decompiler tool ready
- [ ] Game code decompiled to `decompiled/`
- [ ] Tutorial texts extracted (if applicable)
- [ ] Multilingual support decided
- [x] Project directory set up (csproj, Main.cs, etc.)
- [ ] CLAUDE.md updated with project-specific values
- [ ] First build successful
- [ ] "Mod loaded" announcement working in game

## Current Phase
**Phase:** Setup
**Currently working on:** Setting up build environment, waiting for MelonLoader download
**Blocked by:** MelonLoader download speed (GitHub direct download is slow)

## Codebase Analysis Progress
### GATE: Tier 1 MUST be complete before Phase 2 (Framework)!
- [ ] 1.1 Structure overview (namespaces, singletons) → documented in game-api.md
- [ ] 1.2 Input system — ALL game key bindings documented in game-api.md "Game Key Bindings"
- [ ] 1.2 Input system — Safe mod keys identified and listed in game-api.md "Safe Mod Keys"
- [ ] 1.3 UI system (base classes, text access patterns, Reflection needed?)
- [ ] 1.4 State management decision → documented in "Architecture Decisions" below
- [ ] 1.5 Localization: game's language system analyzed (only if multilingual)

### GATE: Relevant Tier 2 items MUST be done before implementing each feature!
- [ ] 1.6 Game mechanics (analyzed as needed per feature)
- [ ] 1.7 Status/feedback systems
- [ ] 1.8 Event system / Harmony patch points
- [ ] 1.9 Results documented in `docs/game-api.md`
- [ ] 1.10 Tutorial analysis (when relevant)

## Game Key Bindings (Original)
<!-- CRITICAL: Fill this during Tier 1 analysis! Every key the game uses.
Without this list, mod keys WILL conflict with game controls. -->
- (not yet documented — MUST be done before Phase 2)

## Implemented Features
List features with their status:
- (none yet)

## Pending Tests
What the user should test in the next game session:
- (none yet)

## Known Issues
- (none yet)

## Architecture Decisions
Document important decisions so future sessions understand the reasoning:
- Using MelonLoader (user preference over BepInEx)
- Tolk from boz700908/tolk repository (user's own build)
- All Tolk DLLs copied to game directory (not just the two mentioned in setup guide)
- GitHub repository for code sync (user clones and tests on Windows)

## Key Bindings (Mod)
- F1: Help
- F12: Toggle debug mode

## Notes for Next Session
- GitHub repo: https://github.com/boz700908/HuXiangLianPian-Accessibility
- Game uses Naninovel visual novel engine - API is publicly documented
- Game internal name: HuXiangLianPian (from app.info)
- Developer: StationWorks
- User will provide Windows game path for direct build and test
