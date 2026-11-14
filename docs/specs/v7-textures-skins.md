# V7 : textures dessinées des skins (point 10, reste v5)

> Écrit le 14/11/2026 par l'artiste 3D. **À brancher par le programmeur.** Le code C# n'a pas
> été touché, parce qu'un mode Play étranger tournait dans l'éditeur.

## Pourquoi
`SkinPatterns.cs` calcule chaque motif au démarrage, en **256 × 256**, avec des bords « tout ou
rien ». De près, cela donne :
- des rayures en escalier (zèbre, tigre) ;
- des anneaux de compas (léopard) ;
- un réseau raide (girafe) ;
- des écailles plates sans chevauchement (écailles, flamant, paon) ;
- une patine et un marbre en aplats durs ;
- des veines d'or « en taches ».

Les nouvelles images font **1024 × 1024**, avec des bords adoucis (calcul en 2048 puis réduction)
et des formes naturelles. Elles n'ont **aucun raccord autour du corps** : le bord gauche rejoint
le bord droit.

- Script : `blender/textures_skins.py`. On peut le relancer, il donne le même dessin à chaque fois.
- Contrôle : `blender/rendu_skins_textures.py`.
- Planches avant (motif calculé actuel, à gauche) / après (à droite) :
  `docs/captures/v7_textures_skins_animaux.png` et `docs/captures/v7_textures_skins_pirates.png`.

## Les fichiers (`Assets/Art/SkinsTextures/`)

| Skin (id de la fiche) | Couleurs | Lumière (émission) | Ce qui change |
|---|---|---|---|
| `animal_zebre` | `animal_zebre.png` | — | rayures qui s'affinent et se cassent, bords nets sans escalier |
| `animal_tigre` | `animal_tigre.png` | — | rayures en pointe, ventre plus clair devant |
| `animal_chat_roux` | `animal_chat_roux.png` | — | rayures douces, fond légèrement nuancé |
| `animal_dalmatien` | `animal_dalmatien.png` | — | taches de tailles variées, bords irréguliers |
| `animal_vache` | `animal_vache.png` | — | grandes plaques aux contours naturels |
| `animal_leopard` | `animal_leopard.png` | — | vraies rosettes ouvertes (3 à 5 morceaux) et petites taches |
| `animal_girafe` | `animal_girafe.png` | — | polygones irréguliers, lignes claires d'épaisseur variable |
| `animal_flamant` | `animal_flamant.png` | — | plumes qui se chevauchent, avec rachis et barbes |
| `animal_paon` | `animal_paon.png` | `animal_paon_lumiere.png` | plumes avec un « œil » de paon ; la lumière suit les liserés |
| `pirate_bois_flotte` | `pirate_bois_flotte.png` | — | fibres du bois et coquillages roses en spirale |
| `pirate_corail` | `pirate_corail.png` | — | alvéoles creuses à bord clair |
| `pirate_vert_de_gris` | `pirate_vert_de_gris.png` | — | cuivre nuancé, plaques de patine bordées |
| `pirate_carte` | `pirate_carte.png` | — | papier taché, chemin en tirets arrondis, 3 croix |
| `pirate_tempete` | `pirate_tempete.png` | — | nuages tourbillonnants avec liserés clairs |
| `pirate_or_maudit` | `pirate_or_maudit.png` | `pirate_or_maudit_lumiere.png` | or nuancé, fines veines vertes lumineuses |
| `pirate_ecailles` | `pirate_ecailles.png` | — | écailles en tuiles, ombre et liseré |
| `pirate_abysses` | `pirate_abysses.png` | `pirate_abysses_lumiere.png` | lignes de méduse avec un halo, plancton lumineux |

Les autres skins à motif restent calculés : marinière et abeille (bandes nettes, déjà propres),
coccinelle, panda, kraken, caméléon, grenouille, lagon et lion. Leur motif est simple ou placé
exprès (yeux, ventouses), et il est déjà correct.

## Réglages d'import (à faire quand l'éditeur est libre)
Pour toutes les images du dossier :
- type *Default* et *sRGB* coché (y compris pour les `_lumiere`) ;
- *Wrap Mode* : **Repeat** (les UV du cheval vont jusqu'à U = 1,5 sur les faces de la couture) ;
- *Filter Mode* : **Trilinear** ;
- **mipmaps activés** ;
- *Max Size* : 1024.

Les `.meta` ne sont pas encore créés : Unity ne les a pas importés pendant le mode Play.

## Comment les brancher
1. Elles se posent sur les **mêmes UV** que `SkinPatterns` : U fait le tour du corps (0,5 = devant,
   couture derrière), V va du socle (0) aux oreilles (1). Tiling **(1 ; 1)**, sans décalage.
2. Les couleurs sont **cuites dans l'image** : la couleur de base du matériau doit être blanche,
   comme pour les motifs calculés.
3. La fiche `ShopItem` a déjà le champ `baseTexture` (et `textureTiling`). Il suffit d'y mettre
   l'image et de faire passer `baseTexture` **avant** `SkinPatterns.For(item)` quand il est rempli.
   Le motif calculé reste en secours si l'image est absente.
4. Les 3 images `_lumiere` vont dans la carte d'émission (`_EmissionMap`, mot-clé `_EMISSION`),
   multipliées par `emissionStrength` et `glowColor` comme aujourd'hui. Il faudra peut-être un
   champ `emissionTexture` dans `ShopItem`, ou une recherche par nom (`<id>_lumiere`).
5. Les effets animés (`SkinFx` : étoiles, scintillement, taches du kraken) ne changent pas : ils
   se dessinent par-dessus.
6. Règle d'or : purement visuel, aucune chance ni aucun gain ne change.

---

## V7, point 17 : comparaison icône / skin porté (à corriger par le programmeur)

> Écrit le 14/11 par l'artiste 3D. Planches : `docs/captures/v7_qualite_skins_1..6.png`
> (icône | cheval de 3/4 | cheval vu par la caméra du jeu), faites par `blender/planche_qualite.py`
> avec les VRAIES valeurs des fiches `ShopItem` (couleurs, métal, brillance, texture). Les motifs
> calculés y sont reproduits en version simplifiée ; les effets animés (`SkinFx`) n'y sont pas.
> Aucune de ces corrections ne touche aux chances ni aux gains (règle d'or) : purement visuel.

Classement du **pire au meilleur** (seuls les skins qui ne ressemblent pas assez à leur icône) :

1. **`animal_cameleon`** — la fiche a `color` #F2F2F2 et `secondaryColor` #C8C8C8 : le cheval est
   **gris**, alors que l'icône est jaune en haut, turquoise au milieu et vert menthe en bas, avec des
   petits points. Correction : `color` = #FFD84A (haut), `secondaryColor` = #3CC8B4 (bas),
   `accentColor` = #8AE07A (liseré des écailles). Si `SkinFx` 6 recolore déjà le cheval en jeu,
   vérifier que la couleur de repos est celle-ci.
2. **Métaux** `couleur_or`, `matiere_argent`, `pirate_or_maudit` (metallic 1, smoothness 0,8-0,85) —
   dans notre décor de nuit, un métal pur ne reflète que du sombre : l'or paraît **bronze** et
   l'argent **violet foncé**, alors que les icônes sont lumineuses. Correction : metallic **0,6**,
   smoothness **0,65** pour les trois (même règle que le nouveau `M7_OrVif` des accessoires), ou une
   *Reflection Probe* claire autour du cheval.
3. **`pirate_lagon`** — dégradé uni turquoise → bleu, alors que l'icône montre un **réseau de reflets
   d'eau** (caustiques claires) sur un dégradé, et de petites gouttes sur le socle. Correction :
   une texture dessinée (à ajouter dans `blender/textures_skins.py` : cellules de Voronoï aux bords
   clairs #E0FFFF sur le dégradé), ou au minimum un motif « réseau » clair par-dessus.
4. **`animal_lion_soleil`** — corps beige-orangé terne ; l'icône est **dorée et lumineuse**, avec une
   flamme claire sur le poitrail. Correction : `metallic` 0,5 → **0,2**, `smoothness` 0,85, et
   `emissionStrength` appliquée aussi au corps (lueur #FFE58A, 0,3), pas seulement aux effets.
5. **`animal_grenouille`** — un dégradé vert sombre → jaune pâle sans taches ; l'icône est vert vif
   avec des **taches vert foncé** sur le dos et la tête. Correction : `color` #3CC84A, et des taches
   (motif `Taches`, `secondaryColor` #1E8A3A, ~30 taches), le jaune pâle en bas seulement.
6. **`animal_coccinelle`** — 7 points, trop petits et trop rares vu de face. Correction :
   `patternCount` **12**, rayons 0,10 à 0,14 (au lieu de 0,08 à 0,11).
7. **`motif_sucre_orge`** — 8 grosses bandes ; l'icône en a ~16, fines. Correction : tiling de
   `SkinTextures.Stripes` (1 ; 4) → **(1 ; 8)**.
8. **`matiere_galaxie`** — à vérifier en jeu : l'icône est une nébuleuse **bleue et violette vive**
   avec beaucoup d'étoiles ; si la `GalaxyTexture` animée paraît surtout noire, augmenter la part
   de nébuleuse (couleurs #3A5BFF et #B04CFF) et l'émission (sous le plafond 1,5).
9. **`animal_tigre`** — la texture n'a pas le **poitrail blanc** ni le museau clair de l'icône
   (mineur ; se corrige dans `textures_skins.py`, bande claire devant, de V = 0,25 à 0,6).

Corrects (ressemblent à leur icône) : les 11 couleurs unies, pois, arc-en-ciel, cristal, méduse,
fantôme, abeille, marinière, chat roux, dalmatien, vache, léopard, girafe, flamant, zèbre, paon,
panda, kraken, abysses, bois flotté, carte, corail, écailles, tempête, vert-de-gris.

---

## V7, point 17 (suite) : 5 nouvelles textures dessinées et tigre repris (14/11)

> Artiste 3D. Planche avant (motif calculé ou texture actuelle) / après :
> `docs/captures/v7_qualite_skins_textures_avant_apres.png`. Fichiers 1024 px, sans raccord,
> dans `Assets/Art/SkinsTextures/` (et `art/skins_textures/`), script `blender/textures_skins.py`.
> Mêmes réglages d'import que plus haut (sRGB, Repeat, Trilinear, mipmaps, 1024).

| Skin | Fichier | À faire dans la fiche `ShopItem` |
|---|---|---|
| `animal_tigre` | `animal_tigre.png` (remplacé) | rien : déjà branché. Le poitrail blanc est maintenant une goutte sous le menton + le museau blanc (plus de bande droite du socle au cou). |
| `pirate_lagon` | `pirate_lagon.png` (nouveau) | `baseTexture` = cette image. Garder `skinFx` 3 et l'émission actuelle : la texture porte le dégradé et le réseau de reflets d'eau. |
| `animal_grenouille` | `animal_grenouille.png` (nouveau) | `baseTexture`. Vert vif à taches vert foncé, ventre jaune-vert devant en bas. |
| `animal_cameleon` | `animal_cameleon.png` (nouveau) | `baseTexture`. Jaune en haut, turquoise, vert menthe au socle, petits points. Remplace le gris de la fiche (`color` peut rester blanc). Vérifier que `SkinFx` 6 ne recolore pas tout le cheval en gris au repos. |
| `animal_coccinelle` | `animal_coccinelle.png` (nouveau) | `baseTexture`. Gros points noirs, aucun sur le visage. |
| `motif_sucre_orge` | `motif_sucre_orge.png` (nouveau) | `baseTexture` (et tiling 1 ; 1). Elle doit passer AVANT le cas `SkinStyle.Rayures` dans `PlayerAvatar` (sinon la texture calculée l'écrase). 16 bandes fines. |

Restent des corrections de MATÉRIAU (le programmeur, dans les fiches), déjà décrites plus haut :
métaux trop sombres (`couleur_or`, `matiere_argent`, `pirate_or_maudit` : metallic 0,6, smoothness
0,65), `animal_lion_soleil` (metallic 0,2, lueur légère du corps), `matiere_galaxie` (à vérifier en jeu).
