# Runner : modèle et animations

Le dossier garde son nom pour les règles d'import, mais rien ne vient de Mixamo : le personnage et les animations
viennent de Universal Animation Library 1 et 2 (Quaternius, CC0), voir `Resources/Credits.txt`.

Pour remplacer le personnage ou un clip, dépose un FBX portant le même nom ici : l'import (rig Humanoid, boucles,
AnimatorController) se fait tout seul à partir du nom du fichier (menu **Hyperspeed > Wire Runner** pour forcer).

Noms reconnus : `runner_character`, `idle`, `idle_ready`, `look_around`, `jog`, `run`, `run_fast`, `sprint`, `jump`,
`jump_flip`, `fall`, `glide`, `dive`, `land_soft`, `land_hard`, `land_roll`, `slide`, `roll`, `turn_spin`, `wall_run`,
`wall_climb`, `wall_slide`, `grind`, `hang`, `stumble`, `victory`, `defeat`, `taunt`, `dash`, `punch`.

Il manque aujourd'hui `wall_run` et `wall_slide` : le jeu retombe sur `sprint` et `fall`, plus la pose procédurale du corps.
