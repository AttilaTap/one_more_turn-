# plan.md — ONE MORE TURN (PC / Steam)
## Coding Plan with AI Support (Technology-Included)

> Goal: build a small, fun, replayable risk‑management game for PC (Steam‑ready).
> Focus: **implementation plan + AI‑assisted coding workflow**.
> Explicitly **no business, marketing, or monetization scope**.

---

## 0) Guardrails

- Ship a **playable MVP fast** (single‑screen loop, 15–20 modifiers).
- **Data‑driven** gameplay (JSON for modifiers/events).
- **Deterministic simulation** using seeded RNG.
- Strict separation: **simulation logic ≠ UI ≠ engine**.
- Optimize for **iteration speed**, not premature polish.

---

## 1) Technology Stack

### 1.1 Game Engine

**Unity 2022 LTS**
- Strong PC + Steam support
- Mature UI tooling
- Excellent C# ecosystem
- Easy audio/FX polish

**Why Unity fits this project**
- UI‑heavy, logic‑driven game
- Minimal rendering complexity
- Fast iteration
- Proven Steam pipeline

---

### 1.2 Language

- **C# (.NET Standard 2.1)**
- Strong typing helps with:
  - deterministic logic
  - modifier composition
  - validation and testing

---

### 1.3 UI Technology

**Decision: uGUI**
- Faster initial setup for MVP
- More community resources
- Good enough for single-screen game
- Can migrate to UI Toolkit post-MVP if needed

---

### 1.4 Testing Framework

**Unity Test Framework (NUnit-based)**
- Edit Mode tests for `/Game/Core` (no Play Mode needed)
- Tests live in `/Tests/Editor/` folder
- Core logic has zero `UnityEngine` references, so tests are fast

Alternative for pure isolation:
- Separate .NET 6 console project referencing Core as source files
- Run via `dotnet test` outside Unity entirely

---

### 1.5 Data Format

- **JSON** via `System.Text.Json` (not Unity's JsonUtility)
- Human‑readable
- Easy AI generation
- Easy validation

Used for:
- modifiers
- events
- presets / run seeds
- persistence (save data)

---

### 1.6 Version Control

- **Git**
- Single main branch + feature branches
- Commit early, commit often
- `.gitignore` for Library/, Temp/, Logs/, obj/

---

### 1.7 AI Support Tools

- Claude / ChatGPT for:
  - skeleton code
  - validators
  - unit test scaffolding
  - modifier JSON generation
- AI **assists**, does not decide balance

---

### 1.8 Steam Integration

**Steamworks.NET** (post-MVP)
- Achievements
- Cloud saves
- Leaderboards (daily seed runs)
- Not required for MVP; stub interfaces early

---

## 2) Repository Structure

```
/OneMoreTurn
  /Assets
    /Game
      /Core                 # pure logic, no UnityEngine refs
        RunState.cs
        TurnResolver.cs
        ModifierSystem/
        EventSystem/
        RNG/
        Serialization/
        Validation/
      /Presentation         # Unity-facing code
        Screens/
        Widgets/
        Audio/
        FX/
        ViewModels/
      /Content              # JSON + ScriptableObjects
        Modifiers/
        Events/
        Presets/
    /Plugins
      Steamworks.NET/       # added post-MVP
  /Tests
    /Editor                 # Edit Mode tests (Core logic)
    /Runtime                # Play Mode tests (UI, integration)
  /docs
    plan.md
    design.md
    modifier_schema.md
```

**Hard rule:** `/Game/Core` must compile without `UnityEngine`.

---

## 3) Core Gameplay Requirements (MVP)

### Run Start
1. Player selects **3 starting modifiers** from a draft of 5
2. Seed is set (random, daily, or manual)
3. Run begins at turn 1, score 0, risk 0

### Player Actions (Per Turn)

**Core Actions (must choose one to end turn):**
- **ONE MORE TURN** — resolve turn, gain score, increase risk
- **CASH OUT** — end run, collect at-risk score + banked score

**Optional Actions (before core action):**
- **BANK** — lock in portion of score safely (see below)
- **PUSH** — trade risk for gain bonus (see below)
- **SACRIFICE** — destroy modifier for emergency effect (see below)

---

### Action: BANK (Partial Cash-Out)

Lock in a portion of at-risk score. Banked score is **safe even if you bust**.

| Option | Effect |
|--------|--------|
| Bank 25% | Move 25% of at-risk score to banked (20% tax) |
| Bank 50% | Move 50% of at-risk score to banked (20% tax) |

- Once per turn
- Tax is configurable (default 20% — bank 100, receive 80)
- Final score = at-risk score + banked score (on cash-out or bust)
- On bust: at-risk score → 0, banked score preserved

**Design intent:** Reduces all-or-nothing feel while keeping stakes. Creates "how greedy?" micro-decisions.

---

### Action: PUSH (Voluntary Risk)

Deliberately increase risk in exchange for bonus gain this turn.

| Effect |
|--------|
| +15% risk immediately |
| +100% gain multiplier this turn only |

- Can stack up to 2× per turn (max +30% risk, +200% gain)
- Applied before turn resolution
- Risk increase happens immediately (can cause bust before turn resolves)

**Design intent:** Transforms passive observation into active risk-taking. "I'm at 70% but I'm going for it."

---

### Action: SACRIFICE (Burn Modifier)

Destroy an active modifier for a one-time emergency effect.

| Modifier Rarity | Sacrifice Effect (choose one) |
|-----------------|-------------------------------|
| Common | -10% risk OR +50 score |
| Uncommon | -20% risk OR +150 score |
| Rare | -30% risk OR +400 score |

- Modifier is permanently removed from run
- Available any time during action phase
- Cannot sacrifice if only 1 modifier remains (optional rule)

**Design intent:** Creates "ongoing value vs. emergency button" tension. Drafting gains second dimension.

---

### Visible State
- **At-risk score** (lost on bust)
- **Banked score** (safe)
- Risk meter (0.0–1.0, displayed as percentage)
- Active modifiers (with sacrifice values on hover)
- Turn counter
- Push stacks available
- Last turn breakdown (gain, risk delta, modifier contributions)

### Run End Conditions
- **Bust** — risk ≥ 1.0 → at-risk score = 0, keep banked score
- **Cash Out** — player choice → final score = at-risk + banked

### Modifier Acquisition (MVP)
- **Draft at run start only** (3 picks from 5 offered)
- Post-MVP: mid-run modifier offers, shops, events that grant modifiers

---

## 4) Simulation Model

### 4.1 RunState (Core)

```csharp
public class RunState
{
    // Core
    public int Seed;
    public int Turn;
    public float Risk;                              // 0.0 to 1.0
    public SeededRandom RNG;                        // deterministic random source

    // Score (split)
    public long AtRiskScore;                        // lost on bust
    public long BankedScore;                        // safe, kept on bust

    // Action tracking (reset each turn)
    public bool HasBankedThisTurn;
    public int PushStacksThisTurn;                  // 0-2, resets each turn
    public float PushBonusMultiplier;               // calculated from stacks

    // Modifiers
    public List<ModifierInstance> ActiveModifiers;

    // Extensible state
    public Dictionary<string, int> Counters;        // named integers for modifier logic
    public HashSet<string> Flags;                   // boolean tags for conditions
}
```

**Counters** — named integers modifiers can read/write:
- `"consecutive_no_bust"` — turns since last near-bust
- `"times_risk_reduced"` — for diminishing returns
- `"total_banked"` — lifetime banked amount this run
- `"sacrifices_made"` — number of modifiers sacrificed
- `"pushes_made"` — total pushes across all turns

**Flags** — boolean tags for conditional logic:
- `"first_turn"` — true only on turn 1
- `"high_roller"` — set when risk > 0.8
- `"pushed_this_turn"` — true if player pushed
- `"banked_this_turn"` — true if player banked
- `"sacrificed_this_turn"` — true if player sacrificed

Counters and flags enable modifier synergies without hardcoded dependencies.

---

### 4.2 Turn Resolution Order

```
┌─────────────────────────────────────────────────────────────┐
│ PHASE 1: TURN START                                         │
├─────────────────────────────────────────────────────────────┤
│ 1. Reset per-turn state                                     │
│    - HasBankedThisTurn = false                              │
│    - PushStacksThisTurn = 0                                 │
│    - Clear turn-specific flags                              │
│                                                             │
│ 2. Event check                                              │
│    - Roll for random event                                  │
│    - If triggered, apply effects (may add/remove modifiers) │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│ PHASE 2: PLAYER ACTION (new!)                               │
├─────────────────────────────────────────────────────────────┤
│ Player may perform any combination of:                      │
│                                                             │
│ • BANK (once per turn)                                      │
│   - Move 25% or 50% of AtRiskScore to BankedScore           │
│   - Apply tax (default 20%)                                 │
│   - Set HasBankedThisTurn = true                            │
│   - Trigger OnBank hooks                                    │
│                                                             │
│ • PUSH (up to 2× per turn)                                  │
│   - Add 15% to Risk immediately per stack                   │
│   - PushStacksThisTurn++                                    │
│   - Trigger OnPush hooks                                    │
│   - BUST CHECK after each push!                             │
│                                                             │
│ • SACRIFICE (any number of modifiers)                       │
│   - Remove modifier from ActiveModifiers                    │
│   - Apply effect (-risk or +score based on rarity)          │
│   - Trigger OnSacrifice hooks                               │
│                                                             │
│ Then choose: ONE MORE TURN or CASH OUT                      │
│                                                             │
│ If CASH OUT → skip to Phase 5 (end run)                     │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│ PHASE 3: TURN RESOLUTION (if ONE MORE TURN)                 │
├─────────────────────────────────────────────────────────────┤
│ 3. Pre-turn hooks                                           │
│    - Each modifier's OnPreTurn runs in priority order       │
│                                                             │
│ 4. Compute base values                                      │
│    - baseGain = 10 * (1 + turn * 0.15)                      │
│    - baseRiskDelta = 0.03 + turn * 0.002                    │
│                                                             │
│ 5. Apply push bonus                                         │
│    - pushMultiplier = 1 + (PushStacksThisTurn * 1.0)        │
│    - baseGain *= pushMultiplier                             │
│                                                             │
│ 6. Apply gain modifiers                                     │
│    - finalGain = modifiers.Aggregate(baseGain, OnComputeGain)│
│                                                             │
│ 7. Apply risk modifiers                                     │
│    - finalRiskDelta = modifiers.Aggregate(baseRiskDelta,    │
│                                           OnComputeRiskDelta)│
│                                                             │
│ 8. Update state                                             │
│    - AtRiskScore += finalGain                               │
│    - Risk += finalRiskDelta                                 │
│    - Risk = clamp(Risk, 0, 1)                               │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│ PHASE 4: BUST CHECK                                         │
├─────────────────────────────────────────────────────────────┤
│ 9. Check bust condition                                     │
│    - If Risk >= 1.0:                                        │
│      - Trigger OnBust hooks (Safety Net can intervene)      │
│      - If still bust: AtRiskScore = 0, run ends             │
│      - Final score = BankedScore only                       │
│                                                             │
│ 10. Post-turn hooks                                         │
│     - Each modifier's OnPostTurn runs                       │
│     - Update counters, check modifier expiry                │
│                                                             │
│ 11. Increment turn                                          │
│     - Turn++                                                │
│     - Return to Phase 1                                     │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│ PHASE 5: RUN END (on cash out or bust)                      │
├─────────────────────────────────────────────────────────────┤
│ • Cash out: FinalScore = AtRiskScore + BankedScore          │
│ • Bust:     FinalScore = BankedScore (AtRiskScore lost)     │
│                                                             │
│ Trigger OnRunEnd hooks, record stats, show summary          │
└─────────────────────────────────────────────────────────────┘
```

All randomness flows through `RunState.RNG` (seeded).

---

## 5) Modifier System

### Goals
- Content‑driven (JSON defines behavior)
- Stackable (multiple modifiers compose)
- Easy to extend (new effects without code changes)
- Easy to test (deterministic, isolated)

### Hooks (MVP)

**Turn Lifecycle Hooks:**
| Hook | Signature | Purpose |
|------|-----------|---------|
| `OnPreTurn` | `(RunState) → RunState` | Setup, flag checks |
| `OnComputeGain` | `(baseGain, RunState) → modifiedGain` | Modify score gain |
| `OnComputeRiskDelta` | `(baseDelta, RunState) → modifiedDelta` | Modify risk change |
| `OnPostTurn` | `(RunState) → RunState` | Cleanup, counter updates |

**Action Hooks (new):**
| Hook | Signature | Purpose |
|------|-----------|---------|
| `OnBank` | `(amount, RunState) → modifiedAmount` | Modify banked amount (reduce tax, bonus) |
| `OnPush` | `(riskCost, gainBonus, RunState) → (modRisk, modGain)` | Modify push costs/benefits |
| `OnSacrifice` | `(modifier, effect, RunState) → modifiedEffect` | Enhance sacrifice effects |
| `OnBust` | `(RunState) → (shouldBust, RunState)` | Intercept bust (Safety Net) |

### Modifier JSON Schema

```json
{
  "id": "greedy_gambler",
  "name": "Greedy Gambler",
  "description": "+50% gain, +25% risk",
  "rarity": "common",
  "tags": ["risk", "gain", "aggressive"],
  "effects": [
    {
      "hook": "OnComputeGain",
      "operation": "multiply",
      "value": 1.5
    },
    {
      "hook": "OnComputeRiskDelta",
      "operation": "multiply",
      "value": 1.25
    }
  ]
}
```

### Effect Operations (MVP)

| Operation | Behavior | Example |
|-----------|----------|---------|
| `add` | `value += effect.value` | `+10 flat gain` |
| `multiply` | `value *= effect.value` | `×1.5 gain` |
| `set` | `value = effect.value` | `risk = 0.5` |
| `add_percent` | `value += base * effect.value` | `+20% of base` |

### Conditional Effects

```json
{
  "id": "risk_taker",
  "name": "Risk Taker",
  "description": "+100% gain when risk > 50%",
  "effects": [
    {
      "hook": "OnComputeGain",
      "operation": "multiply",
      "value": 2.0,
      "condition": {
        "type": "risk_above",
        "threshold": 0.5
      }
    }
  ]
}
```

### Condition Types (MVP)
- `risk_above` / `risk_below` — threshold checks
- `turn_multiple` — every N turns
- `flag_set` / `flag_not_set` — boolean checks
- `counter_above` / `counter_below` — counter checks
- `has_modifier` — synergy checks

### Priority System
- Effects execute in priority order (lower = earlier)
- Default priority: 100
- Additive effects should run before multiplicative
- Example: flat +10 (priority 50) → then ×1.5 (priority 100)

### Modifier Instance

```csharp
public class ModifierInstance
{
    public string ModifierId;           // reference to JSON definition
    public int StackCount;              // for stackable modifiers
    public int TurnsRemaining;          // -1 = permanent
    public Dictionary<string, int> LocalCounters;  // instance-specific state
}
```

---

## 6) Risk Model (Initial)

### Base Formulas
```
baseGain = 10 * (1 + turn * 0.15)
baseRiskDelta = 0.03 + turn * 0.002
```

### Bust Condition
```
risk >= 1.0 → BUST (score = 0)
```

### Example Progression (No Modifiers)
| Turn | Base Gain | Risk Delta | Cumulative Risk |
|------|-----------|------------|-----------------|
| 1    | 11.5      | 0.032      | 0.032           |
| 5    | 17.5      | 0.040      | 0.180           |
| 10   | 25.0      | 0.050      | 0.410           |
| 15   | 32.5      | 0.060      | 0.710           |
| 20   | 40.0      | 0.070      | 1.060 (BUST)    |

Theoretical max turns without modifiers: ~19
Modifiers shift this curve dramatically.

---

## 6.5) Event System

### Purpose
Events add variance and decision points mid-run. They're opt-in complexity.

### Event Trigger
- Each turn, after pre-turn hooks, roll for event
- Base chance: 10% per turn (modified by flags/modifiers)
- Max 1 event per turn

### Event JSON Schema

```json
{
  "id": "lucky_break",
  "name": "Lucky Break",
  "description": "Fortune smiles on you.",
  "weight": 10,
  "conditions": [
    { "type": "turn_above", "value": 3 }
  ],
  "effects": [
    {
      "type": "reduce_risk",
      "value": 0.1
    },
    {
      "type": "set_flag",
      "flag": "had_lucky_break"
    }
  ]
}
```

### Event Effect Types (MVP)
| Type | Behavior |
|------|----------|
| `add_score` | Immediate score bonus |
| `reduce_risk` | Subtract from current risk |
| `add_risk` | Add to current risk |
| `grant_modifier` | Add modifier to active list |
| `remove_modifier` | Remove by ID or tag |
| `set_flag` | Set a boolean flag |
| `set_counter` | Set a counter value |
| `modify_counter` | Add/subtract from counter |

### Event Selection
1. Filter events by conditions
2. Weight remaining events
3. Roll from weighted pool
4. Apply effects

### MVP Event Count
- 5–8 events for MVP
- Expand post-MVP with choice-based events ("pick one of two outcomes")

---

## 7) UI Plan

### Single-Screen Layout
```
┌──────────────────────────────────────────────────────────────┐
│  TURN 7                                                      │
│                                                              │
│  AT RISK: 1,234        BANKED: 320        TOTAL: 1,554      │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│                 ██████████████████░░░░░░░░                   │
│                       RISK: 67%                              │
│                                                              │
│              [ PUSH: +15% RISK → +100% GAIN ]                │
│                     (0/2 this turn)                          │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│  LAST TURN:                          MODIFIERS:              │
│    Base gain:      +25               ● Greedy Gambler        │
│    Push bonus:     ×2.0                [SACRIFICE: -10%]     │
│    Greedy Gambler: ×1.5              ● Lucky Charm           │
│    Final gain:     +75                 [SACRIFICE: -10%]     │
│    Risk change:    +6%               ● Safety Net            │
│                                        [SACRIFICE: -30%]     │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│    [ BANK 25% ]     [ BANK 50% ]                             │
│       (−20%)          (−20%)                                 │
│                                                              │
│         [ ONE MORE TURN ]       [ CASH OUT ]                 │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

### Action Button States
- **BANK**: Disabled if already banked this turn, or if at-risk score is 0
- **PUSH**: Shows stack count (0/2), disabled at 2/2 or if would cause bust
- **SACRIFICE**: Each modifier shows its sacrifice value, click to burn
- **ONE MORE TURN**: Always available unless bust
- **CASH OUT**: Always available

### ViewModel (Immutable)

```csharp
public record GameViewModel
{
    // Turn state
    public int Turn { get; init; }
    public float Risk { get; init; }

    // Score (split display)
    public long AtRiskScore { get; init; }
    public long BankedScore { get; init; }
    public long TotalScore => AtRiskScore + BankedScore;

    // Action availability
    public bool CanBank { get; init; }          // false if already banked this turn
    public bool CanPush { get; init; }          // false if at 2 stacks or would bust
    public int PushStacksUsed { get; init; }    // 0, 1, or 2
    public int PushStacksMax { get; init; }     // typically 2

    // Game state
    public bool IsGameOver { get; init; }
    public string GameOverReason { get; init; }  // "bust" or "cash_out"
    public long FinalScore { get; init; }        // only set when game over

    // Sub-views
    public TurnBreakdownViewModel LastTurn { get; init; }
    public IReadOnlyList<ModifierViewModel> Modifiers { get; init; }
    public EventViewModel ActiveEvent { get; init; }  // null if no event
}

public record TurnBreakdownViewModel
{
    public long BaseGain { get; init; }
    public float PushMultiplier { get; init; }   // 1.0 if no push, 2.0/3.0 if pushed
    public long FinalGain { get; init; }
    public float BaseRiskDelta { get; init; }
    public float FinalRiskDelta { get; init; }
    public IReadOnlyList<EffectContribution> GainContributions { get; init; }
    public IReadOnlyList<EffectContribution> RiskContributions { get; init; }
}

public record ModifierViewModel
{
    public string Id { get; init; }
    public string Name { get; init; }
    public string Description { get; init; }
    public string Rarity { get; init; }          // for sacrifice value display
    public int StackCount { get; init; }
    public int TurnsRemaining { get; init; }     // -1 = permanent

    // Sacrifice info
    public float SacrificeRiskReduction { get; init; }  // e.g., 0.1, 0.2, 0.3
    public long SacrificeScoreGain { get; init; }       // e.g., 50, 150, 400
}

public record EffectContribution
{
    public string SourceName { get; init; }      // modifier name or "Base"
    public string Operation { get; init; }       // "+", "×", etc.
    public string Value { get; init; }           // formatted for display
}
```

### UI ↔ Simulation Boundary
- UI **never** mutates `RunState` directly
- UI sends **commands**: `OneTurnCommand`, `CashOutCommand`
- Simulation returns new `RunState` + `GameViewModel`
- All animations driven by diffing ViewModels

---

## 8) Audio & Feedback

Minimal but punchy:
- Gain ping (pitch scales with risk)
- Risk warning ticks
- Bust sound + screen shake
- Cash‑out lock‑in sound

---

## 9) Persistence

MVP persistence:
- Best score
- Last 10 runs
- Daily seed (optional)

Stored as JSON in user data folder.

---

## 10) Testing Strategy

Unit tests for:
- Deterministic turn results
- Modifier stacking order
- Schema validation
- Bust logic
- Cash‑out logic

Golden‑seed tests required.

---

## 11) Development Milestones

### Milestone A: Headless Simulation Core
**Goal:** Complete game loop with all actions, runnable from tests

Deliverables:
- [ ] `RunState` class with all fields (including split score, action tracking)
- [ ] `SeededRandom` wrapper
- [ ] `TurnResolver` with full resolution order
- [ ] Core actions: ONE MORE TURN, CASH OUT
- [ ] Player actions: BANK, PUSH, SACRIFICE
- [ ] Bust logic (with at-risk vs banked score handling)
- [ ] Golden-seed test (known input → known output)

Exit criteria: Can simulate full run with all actions from CLI/test with deterministic results.

---

### Milestone B: Data-Driven Modifiers
**Goal:** Modifiers loaded from JSON, not hardcoded

Deliverables:
- [ ] Modifier JSON schema + loader
- [ ] Schema validator (fail fast on bad JSON)
- [ ] Effect system (add, multiply, set, conditional)
- [ ] 5 starter modifiers in JSON
- [ ] Tests: modifier stacking, priority order, conditions

Exit criteria: Add new modifier by editing JSON only, no code changes.

---

### Milestone C: Playable UI
**Goal:** Mouse-clickable game in Unity with all actions

Deliverables:
- [ ] Main game screen (uGUI)
- [ ] Score display (at-risk + banked + total)
- [ ] Risk meter visualization (color transitions)
- [ ] Modifier list with sacrifice buttons and tooltips
- [ ] Turn breakdown display (including push multiplier)
- [ ] Action buttons: BANK 25%, BANK 50%
- [ ] Action buttons: PUSH (with stack counter)
- [ ] Core buttons: ONE MORE TURN, CASH OUT
- [ ] Button state management (disable when unavailable)
- [ ] Game over screen (bust vs cash out, show banked score saved)
- [ ] Modifier draft screen (pick 3 from 5)

Exit criteria: Full run playable with all actions from draft to end.

---

### Milestone D: Events System
**Goal:** Random events add mid-run variance

Deliverables:
- [ ] Event JSON schema + loader
- [ ] Event trigger roll in turn resolution
- [ ] Event effect application
- [ ] 5 starter events
- [ ] Event popup UI

Exit criteria: Events trigger and affect gameplay.

---

### Milestone E: Juice & Feedback
**Goal:** Game feels satisfying

Deliverables:
- [ ] Score tick-up animation
- [ ] Risk meter color transitions (green → yellow → red)
- [ ] Gain/risk delta floating text
- [ ] Screen shake on bust
- [ ] Button press feedback
- [ ] Basic SFX (gain, risk warning, bust, cash out)

Exit criteria: Playtest feedback says "it feels good."

---

### Milestone F: Persistence & Polish
**Goal:** Progress saves, runs tracked

Deliverables:
- [ ] Best score persistence (JSON file)
- [ ] Run history (last 10 runs)
- [ ] Daily seed mode
- [ ] Run summary screen with stats (turns, banked vs busted, actions used)
- [ ] 20+ total modifiers (including action synergies)
- [ ] 10+ total events

Exit criteria: MVP complete, ready for external playtesting.  

---

## 12) AI‑Assisted Workflow

AI used for:
- Code scaffolding
- JSON content generation
- Validator logic
- Test boilerplate

Human responsibility:
- Balance
- Fun
- UX clarity
- Final decisions

---

## 13) Definition of Done (MVP)

- [ ] Playable without tutorial (self-explanatory UI)
- [ ] Runs last 2–10 minutes
- [ ] All player actions work: BANK, PUSH, SACRIFICE, ONE MORE TURN, CASH OUT
- [ ] 20+ modifiers with meaningful variety (including action synergies)
- [ ] 10+ events
- [ ] Deterministic per seed (replay any run)
- [ ] Core logic fully tested (90%+ coverage on `/Game/Core`)
- [ ] Zero `UnityEngine` dependency in `/Game/Core`
- [ ] Persistence works (best score, run history)
- [ ] No crashes or soft-locks
- [ ] Audio/visual feedback complete

---

## 14) Starter Content List

### Modifiers (20 MVP Target)

**Core Modifiers (original):**

| ID | Name | Effect | Rarity |
|----|------|--------|--------|
| `greedy_gambler` | Greedy Gambler | +50% gain, +25% risk | Common |
| `lucky_charm` | Lucky Charm | -15% risk delta | Common |
| `slow_burn` | Slow Burn | -30% gain, -40% risk delta | Common |
| `risk_taker` | Risk Taker | +100% gain when risk > 50% | Uncommon |
| `early_bird` | Early Bird | +200% gain on turns 1-3, then expires | Uncommon |
| `snowball` | Snowball | +5% gain per turn (stacking) | Uncommon |
| `safety_net` | Safety Net | First bust becomes 99% risk instead | Rare |
| `double_down` | Double Down | ×2 gain, ×2 risk delta | Uncommon |
| `turtle` | Turtle | +10 flat gain, -50% gain multiplier | Common |
| `steady_hand` | Steady Hand | Risk delta capped at 5% per turn | Uncommon |
| `hot_streak` | Hot Streak | +25% gain when score > 500 | Common |
| `last_stand` | Last Stand | +500% gain when risk > 90% | Rare |

**Action Synergy Modifiers (new):**

| ID | Name | Effect | Rarity |
|----|------|--------|--------|
| `tax_haven` | Tax Haven | Banking has no tax (0% instead of 20%) | Uncommon |
| `compound_interest` | Compound Interest | Banked score grows +5% per turn | Rare |
| `adrenaline_junkie` | Adrenaline Junkie | Push grants +150% gain instead of +100% | Uncommon |
| `reckless_abandon` | Reckless Abandon | Push costs only +10% risk instead of +15% | Uncommon |
| `martyr` | Martyr | Sacrifice effects are doubled | Rare |
| `phoenix_ash` | Phoenix Ash | When you sacrifice a modifier, gain +20% of its effects permanently | Rare |
| `hedge_fund` | Hedge Fund | +25% gain for every 500 banked score | Uncommon |
| `all_or_nothing` | All or Nothing | Cannot bank, but +75% gain | Common |

### Events (10 MVP Target)

| ID | Name | Effect |
|----|------|--------|
| `lucky_break` | Lucky Break | -10% risk |
| `windfall` | Windfall | +50 to at-risk score |
| `bad_omen` | Bad Omen | +15% risk |
| `second_wind` | Second Wind | -20% risk, but -25% gain next turn |
| `modifier_gift` | Mysterious Gift | Gain random common modifier |
| `gamblers_choice` | Gambler's Choice | Choose: +30% risk for +200% gain this turn, or skip |
| `calm_moment` | Calm Moment | Risk delta = 0 this turn |
| `greed_curse` | Curse of Greed | +100% gain for 3 turns, then +50% risk |
| `safe_deposit` | Safe Deposit | Instantly bank 50% of at-risk score (no tax) |
| `double_or_nothing` | Double or Nothing | Choose: 50% chance to double at-risk score, 50% chance to lose it all |

---
