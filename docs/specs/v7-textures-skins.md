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
