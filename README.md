# Spore Tribal Stage Recreation

A fan recreation of the **Tribal Stage** from Maxis' *Spore* (2008), built in Unity. Command a small tribe, gather food, arm your villagers, and decide whether to win your neighbours over with music or crush them with clubs.

This is a non-commercial fan project made for learning. It is not affiliated with or endorsed by Maxis or Electronic Arts. *Spore* is a trademark of Electronic Arts Inc.

## What is in it

### Commanding your tribe
- RTS-style control: click or box-select villagers, then right-click to give an order.
- One right-click, many meanings. The same click moves, gathers, attacks or razes, depending on what is under the cursor and who it belongs to.
- Orders are settled in one place and can be declined with a reason (can't afford it, target is gone, already equipped).
- Groups arrive in formation rather than piling up on a single point. NavMesh navigation and task interruption underpin all of it.

### Gathering and economy
- Villagers harvest food nodes and carry the food back to the village drop-off point.
- Food is the one currency. It buys new villagers, tools and gifts.
- Recruit new villagers from the main hut, up to the tribe's population cap.
- Tool buildings hand out equipment for a food cost. One tool per villager, as in the original.

### Tools and roles
- An axe or other weapon turns a gatherer into a fighter. An instrument (wooden horn, maraca, didgeridoo) turns them into a musician.
- The chieftain starts with a staff and is the only one who can lead a band.
- Tools are ScriptableObjects (damage, raze damage, cooldown, held model, instrument type, sounds), so adding one is a data change.

### Fighting
- Villagers attack hostile villagers and raze enemy buildings.
- Health bars, hit and death effects, and a hut that acts as a sanctuary where wounded villagers heal.
- Every hit sours the victim tribe's opinion of the attacker.

### Raids and AI
- Rival tribes run a data-driven raid director: when to check, how often to launch, how big a squad to send, which tools its members carry, and which relationship range triggers it.
- Two raid plans so far: a straight raid and a food-stealing run.
- On-screen alerts tell you when a raid is coming.

### Musical diplomacy
- Bring an instrument-carrying band to a rival village and the crowd turns out to watch.
- The crowd calls for an instrument (horn, maraca or didgeridoo). Answer with the matching one using keys `1`, `2` or `3`.
- Correct answers fill the friendship meter, and more matching musicians fill it faster. Mistakes drain it. Fill it and the rival warms to you; get the relationship high enough and they become allies.
- Not into music? Gift food (`G`, hold `Shift` for a big gift) or sing the soothing song (`H`) to calm a hostile tribe.

### Tribe culture and personality
- Each tribe has a culture alignment from aggressive to friendly, shifted by how it treats and is treated by others.
- That alignment weights its idle behaviour: friendly tribes dance, aggressive tribes play-fight. Activities are data assets with their own appeal curves.

### Relationships and winning
- Relationship tiers run from At War to Allied.
- Win by conquest (raze a rival's main hut and the tribe surrenders) or by alliance (befriend the tribe).

### Look and feel
- A stylised world with a curved-horizon "world bend", grass that reacts to movement, cloud shadows, wind, distance fog and ground footprints.
- Tween-driven feedback, thought balloons, floating music notes while musicians play, and particle effects for hits, deaths and ambience.

## Controls

| Input | Action |
| --- | --- |
| Left click / drag | Select villagers (drag for a selection box) |
| Right click | Command selection (move, gather, attack, raze, perform, equip) |
| Right drag, middle drag, arrow keys | Pan camera |
| `Shift` + middle drag, or left + right drag | Orbit camera |
| Scroll wheel | Zoom |
| `1` / `2` / `3` | Answer a call with horn / maraca / didgeridoo |
| `G` (`Shift+G`) | Gift food to the rival (large gift) |
| `H` | Sing the soothing song |
| `F1` | Toggle the debug panel |

## Getting started

1. Install **Unity `6000.6.0f1`** (see `ProjectSettings/ProjectVersion.txt`).
2. Clone the repository. It uses **Git LFS**, so run `git lfs install` first.
3. Open the project in Unity and load `Assets/_Project/Scenes/Main.unity`.
4. Press Play.

## Project layout

Own code and content live under `Assets/_Project/`:

| Folder | Contents |
| --- | --- |
| `Scripts/Orders`, `Commands`, `Selection`, `Inputs` | Click resolution, dispatching, formations, input |
| `Scripts/Villagers`, `Buildings`, `Economy` | Units, huts and tool buildings, villages and food |
| `Scripts/Combat`, `AI` | Health, raids, performance director |
| `Scripts/Culture` | Tribe temperament and idle activities |
| `Scripts/Feedback`, `UI`, `Rendering` | Effects, HUD, visuals |
| `Data` | ScriptableObjects: tools, raids, culture activities, VFX |

## Tech

Unity 6, Universal Render Pipeline, Input System, Cinemachine, NavMesh, PrimeTween, ProBuilder, VFX Graph.

## Credits

- *Spore* and the Tribal Stage design: Maxis / Electronic Arts. All credit for the original idea goes to them.
- Low-poly buildings, nature and tool models: [KayKit](https://kaylousberg.com).
- Particle effects: Cartoon FX Remaster FREE by Jean Moreno (JMO Assets).
- Tweening: [PrimeTween](https://github.com/KyryloKuzyk/PrimeTween).
- Character models: Creative Characters FREE by ithappy.
- Humanoid animations: Human Animations (FREE version) by Kevin Iglesias.
