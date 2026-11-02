# Mixamo — fichiers à déposer ici

Réglages communs : **FBX for Unity**, **30 fps**, Keyframe Reduction **none**, **In Place** coché quand la case existe.
Animations en **Without Skin**, personnages en **With Skin** (T-pose).
Si une recherche ne donne rien de convaincant, saute la ligne : elle est couverte en procédural.

L'import (rig Humanoid, boucles, matériaux toon, AnimatorController) se fait tout seul à partir des noms de fichiers.

| Fichier à enregistrer | Recherche Mixamo | Ce qu'il faut |
|---|---|---|
| `runner_character.fbx` | onglet Characters | perso athlétique en tenue moulante / sci-fi, sans cape ni vêtement ample ; à défaut **Y Bot** ou **X Bot** |
| `idle.fbx` | idle | idle respirant neutre |
| `idle_ready.fbx` | warm up | échauffement sur place (pré-départ) |
| `look_around.fbx` | look around | regarde autour de lui |
| `jog.fbx` | jog | jog avant |
| `run.fbx` | running | course standard |
| `run_fast.fbx` | fast run | course rapide |
| `sprint.fbx` | sprint | le sprint le plus explosif, buste penché |
| `jump.fbx` | running jump | saut en course |
| `jump_flip.fbx` | front flip | salto avant (double saut) |
| `fall.fbx` | falling idle | chute en boucle |
| `glide.fbx` | skydiving / free fall | bras écartés, stable, en boucle |
| `dive.fbx` | dive | plongeon tête la première |
| `land_soft.fbx` | falling to landing | réception |
| `land_hard.fbx` | hard landing | réception lourde |
| `land_roll.fbx` | falling to roll | réception en roulade |
| `slide.fbx` | running slide | glissade |
| `roll.fbx` | forward roll | roulade avant |
| `turn_spin.fbx` | spin | vrille / pirouette 360° gracieuse |
| `wall_run.fbx` | wall run | course sur mur (si ça existe) |
| `grind.fbx` | surfing / skateboarding | posture équilibrée bras ouverts, en boucle |
| `hang.fbx` | hanging idle | suspendu à deux mains (tyrolienne) |
| `stumble.fbx` | stumble | encaisse un choc |
| `victory.fbx` | victory | victoire |
| `defeat.fbx` | defeated | défaite |
| `taunt.fbx` | taunt | pose de défi (intro) |
| `rider_character.fbx` + `rider_pose.fbx` (optionnels) | biker / motorcycle, driving | pilote casqué + pose penchée, seulement si la moto Sketchfab n'a pas de pilote |
