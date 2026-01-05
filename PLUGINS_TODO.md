# Plugins TODO

This file tracks issues, bugs, and improvement opportunities for AssettoServer plugins.

---

## RaceChallengePlugin

### Bugs (Critical)

- [ ] **Leader/Follower swap bug** (`Race.cs:126-128`)
  ```csharp
  // Current (broken):
  Leader = Follower;
  Follower = Leader;  // Sets Follower to itself, not the old Leader

  // Should be:
  (Leader, Follower) = (Follower, Leader);  // Tuple swap
  // Or use temp variable
  ```

### High Priority

- [ ] **Add configuration class** - Currently all values are hardcoded magic numbers
  - Lineup timeout (15s)
  - Accept timeout (10s)
  - Challenge cooldown (20s)
  - Light flash count to trigger (3)
  - Win gap distance (750m)
  - Overtake timeout (60s)
  - Nearby car detection range (30m)
  - Overtake detection range (50m)
  - Enable/disable light flash challenges
  - Enable/disable race start broadcasts

- [ ] **Add `/cancel` command** - Allow players to abort pending/active races

- [ ] **Add `/decline` command** - Allow challenged player to reject

### Medium Priority

- [ ] **Thread safety** - `CurrentRace` property accessed from multiple threads:
  - Position update events
  - Command handlers
  - Timeout continuation tasks
  - Consider using `lock` or making operations atomic

- [ ] **Race statistics tracking**
  - Track wins/losses per player (Steam ID)
  - Option to persist to file or database
  - Add `/racestats` command to view personal record

- [ ] **Spectator notifications**
  - Option to broadcast race start to server
  - Option to broadcast live overtakes
  - Make result broadcast configurable

- [ ] **Input validation**
  - Prevent challenging AI traffic cars
  - Prevent challenging spectators/disconnected slots
  - Add minimum speed requirement to start race

### Low Priority / Nice-to-Have

- [ ] **Cooldown feedback** - Tell player how long until they can challenge again

- [ ] **Lua client-side UI**
  - Visual countdown overlay
  - Race status HUD (gap distance, current leader)
  - Finish line animation

- [ ] **Discord webhook integration**
  - Post race results to Discord channel
  - Include player names, margin of victory

- [ ] **Leaderboard system**
  - Track top racers by win count
  - Add `/raceleaderboard` command
  - Optional HTTP endpoint for web display

- [ ] **Extended race types**
  - Point-to-point races (define start/finish coords)
  - Circuit lap races
  - Multi-car races (3+ participants)

- [ ] **Betting/wager system**
  - Integration with economy plugins
  - Players can bet on themselves

### Code Quality

- [ ] **Memory optimization** - `EntryCarRace` instances created for ALL entry cars at startup
  - Consider lazy initialization only for connected players
  - Add cleanup on player disconnect

- [ ] **Stale name references** - `ChallengerName`/`ChallengedName` captured at race creation
  - Could become stale if player changes name mid-session
  - Consider always fetching from `Client?.Name`

- [ ] **Edge case handling**
  - Both cars reset simultaneously
  - Server session change mid-race
  - Player kicked/banned during race

---

## Other Plugins

*(Add analysis of other plugins here as they are reviewed)*

---

## Legend

- [ ] Not started
- [x] Completed
- [-] In progress
- [~] Won't fix / Deferred
