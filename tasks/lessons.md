# Lessons

Rules learned from the user's corrections. Read at the start of every session on this project.

## Level design
- A level-design pass is a full rewrite against the stated philosophy, never two or three tweaks at the margin.
  When the user says curves, loops or twists are too violent, change the generators and caps, not one stage.
- A feature the user asked for once stays in every later iteration (upward launch ramps / kickers were asked
  for several times and kept disappearing). Keep a checklist of standing requests and check it before delivering.
- Philosophy (2026-11-12): an "ant-hill" stage with many routes at different heights: ground lane, sky lanes,
  ropes hung in the void (no pylons, or platform to platform), ring chains with nothing under them, pad lines,
  kickers that throw you up, side loops. Never a single path.
- Things under rings or ropes that the player can end up beneath are bugs.

## Speed
- No single kick (pad, dash, ring, drift) may cross a speed tier. Tiers are reached by chaining, and only on
  the stages where they are allowed (hyper from 2-4, light from 3-4).
- Speed is a crescendo over the game: early stages slow and small, late stages fast and huge.

## Camera and readability
- "Like before" means the previous delivered build. Compare against it (git show / old build) before changing.
- Check the camera against every loop: it must never go through the ground.
- No freeze frames or impact frames on routine actions (pads, dashes, rings, ropes). Impact must not cost
  readability: prefer edge flashes, line bursts, FOV punches, world shockwaves, sound.
- The player must always see what is ahead.

## Audio
- Never Haas-delay the low end (bass goes antiphase) and never let Unity force stereo effects to mono.
- Measure loudness per category (LUFS, true peak); never normalise every file to the same RMS.

## Speed (verification)
- Check every tier gate with the numbers, not the intent: a light charge of N pads needs a pad line of N plus
  the pads that open hyper plus a margin. Run the autopilot through it and read the charge in the log.
- Anything that runs at light speed needs a run-out: brake back to hyper before the highway ends, end the light
  with it. Never hand a 20 km/s runner to a normal track.

## Process
- Unity MCP for this project runs over HTTP on 127.0.0.1:8080 even when its tools are not exposed in the
  session; drive it with a small JSON-RPC client (initialize, tools/call).
- MCP screenshots are written asynchronously: delete the target file first and wait for the new one, or you copy
  a stale image (17 stage cards were overwritten with HUD shots that way).
- The autopilot matrix (Dev build, -hsStage N -hsRoute ground|sky) is the proof a stage is finishable. Every
  death in it is a bug to read in the log ([HS-LD] out (...)), not noise.
