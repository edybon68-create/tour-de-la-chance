# Tour de la Chance — règles du projet

Jeu 3D « quitte ou double » sous Unity. L'auteur débute en programmation : **tout ce qui est
écrit pour lui (commentaires, docs, messages) est en français et pédagogique.**

## Où est quoi
- `docs/GDD.md` : la bible du jeu. À lire avant toute tâche. On ne contredit pas le GDD sans le mettre à jour.
- `docs/specs/` : les spécifications détaillées des fonctionnalités (écrites par le game-designer).
- `TourDeLaChance-Unity/` : le projet Unity 6000.3.28f1, URP 3D, Input System uniquement.
- `blender/` : scripts Python de modélisation ; exports `.glb`/`.fbx` vers `TourDeLaChance-Unity/Assets/Models/`.
- `art/` : images générées (concept, textures, icônes), puis copiées dans `Assets/Art/`.
- `.claude/agents/` : l'équipe de sous-agents.

## Règles de code Unity
- Namespace `TourDeLaChance`. **Un MonoBehaviour par fichier**, nommé comme sa classe.
- Commentaires en français, expliquant le *pourquoi* pour un débutant.
- La logique décide, l'interface affiche : un script UI ne contient pas de règles de jeu.
- Interface en **UI Toolkit** (UXML + USS dans `Assets/UI/`), jamais `OnGUI`.
- Entrées : `UnityEngine.InputSystem` uniquement (l'ancien `Input` est désactivé).
- Matériaux : shader URP Lit (`GraphicsSettings.currentRenderPipeline.defaultMaterial`).
- Sauvegarde : `PlayerPrefs` (pas de cloud).
- **Règle d'or : pièces virtuelles uniquement, gagnées en jouant, aucun achat en argent réel.**
  Les **cosmétiques** ne changent jamais les chances. Les **améliorations** (spec v3) sont une
  progression assumée, achetée avec ces pièces, dont l'effet est affiché clairement.
  Le spectacle ne triche jamais : même suspense et même animation pour un trésor et un piège.

## Piloter l'éditeur Unity (CLI)
Toujours préfixer : `export PATH="$HOME/.unity/bin:$PATH"; export UNITY_PROJECT_PATH=/Users/edy/TourDeLaChance/TourDeLaChance-Unity`

- Vérifier l'éditeur : `unity status` (état `ready`).
- Compiler : `unity recompile --format json` → lire `data.errors`.
- Exécuter du C# dans l'éditeur : `unity command script eval --no-banner '<code C#; return ...;>'`.
- Ne jamais éditer à la main les `.unity`, `.prefab`, `.asset` : passer par `script eval`
  (et `SerializedObject` pour remplir les champs `[SerializeField]`).
- Mode Play : `unity command editor set_auto_tick`, puis `unity command editor.playmode start`,
  puis `unity status --until-ready --project-path "$UNITY_PROJECT_PATH"`. Arrêt : `editor.playmode stop`.
- Capture : `unity command capture game_view --no-banner -- --source screen --output-path <chemin absolu .png> --width 1280 --height 720`.
- Console : `unity command console get --no-banner`.
- Pièges connus : ne PAS passer `--caller/--skill` aux commandes autres que `script eval` (ils sont pris
  pour des paramètres) ; les paramètres propres à une commande Pipeline vont après `--`.
- Après des tests en mode Play, remettre à zéro les PlayerPrefs de test.

## Git
Un commit par fonctionnalité terminée et testée, message en français.
