# Tour de la Chance — Document de conception (GDD)

> Le GDD (Game Design Document) est la « bible » du jeu : tout le monde dans l'équipe
> (humains et agents) s'y réfère avant de coder, modéliser ou dessiner.

## 1. Le pitch (en une phrase)
Grimpe une tour magique étage par étage en tentant ta chance à chaque épreuve : encaisse tes
pièces à temps… ou risque tout pour aller plus haut.

## 2. Fiche technique
| | |
|---|---|
| Genre | Jeu de hasard « quitte ou double » (push-your-luck) |
| Vue | 3D, caméra fixe en 3/4 au-dessus du personnage |
| Style visuel | Low-poly coloré, ambiance fête foraine / casino magique |
| Moteur | Unity 6.3 LTS, rendu URP |
| Plateforme | Mac / PC (desktop) |
| Joueurs | 1 joueur, hors ligne |
| Monnaie | **Pièces virtuelles uniquement** — jamais d'argent réel |

## 3. La boucle de jeu
```
 Menu ──> Lancer une partie ──> Étage N : choisir un coffre ─── ou ───> [ENCAISSER X]
                                     │                       (bouton visible dès l'étage 2,
                         ┌───────────┴───────────┐            cagnotte -> porte-monnaie)
                      Gagné                    Perdu                     │
                         │                        │                     │
              Gain de l'étage ajouté      Gains de la partie perdus     │
              à la « cagnotte »           (sauf filet de sécurité)      │
                         │                        │                     │
              Montée AUTOMATIQUE                  │                     │
              vers l'étage N+1 ──> (boucle)       │                     │
                                                  └──> Boutique / Rejouer <┘
```
**Nouveau flux (v2)** : après une épreuve gagnée, on monte **directement** à l'étage suivant
(transition « on monte »). Pendant le choix du coffre suivant, un bouton **« Encaisser X »**
(X = cagnotte) reste proposé : choisir un coffre = continuer, cliquer « Encaisser » = mettre
la cagnotte à l'abri. À l'étage 1, rien à encaisser (bouton caché). Coffres et bouton sont
bloqués pendant la transition et 0,5 s après, pour éviter les clics accidentels.
Détails : `docs/specs/sons-animations.md` §2.1 et §3.11.

**Le décor change avec l'altitude** (pur affichage) : étages 1-3 sous les nuages, 4-6 dans
les nuages, 7-9 au-dessus des nuages, 10 et plus dans les étoiles. Détails :
`docs/specs/sons-animations.md` §3.14.

## 4. Les épreuves (une par étage, tirée au hasard)
| Épreuve | Principe | Chance de gagner |
|---|---|---|
| **Les 3 coffres** | Choisir 1 coffre sur 3 ; un seul est piégé | 66 % |
| **Le dé** | Lancer un dé, il faut faire 3 ou plus | 66 % |
| **La roue** | Faire tourner une roue à secteurs (gain / perte / bonus) | ~60 % |
| **Pile ou face** | Une pièce géante, on choisit un côté | 50 % (gain doublé) |

Plus on monte, plus les épreuves risquées apparaissent souvent.

## 5. L'économie (première version, à équilibrer)
- Gain de l'étage N = `10 × N` pièces (étage 1 = 10, étage 5 = 50…).
- **Cagnotte** : la somme des gains de la partie en cours, perdue si on échoue.
- **Porte-monnaie** : les pièces encaissées, gardées pour toujours (sauvegarde).
- **Paliers de sécurité** : tous les 5 étages, une partie de la cagnotte est mise à l'abri.

## 6. Le shop : boîtes à surprises (cosmétiques uniquement)
Aucun objet ne change les chances de gagner : c'est purement esthétique.

Détails complets (règles, catalogue, écrans, animation, sauvegarde, critères) :
`docs/specs/boites.md` (la v1 à achat direct, `docs/specs/shop.md`, est remplacée).

**Version 2** : on ouvre des **boîtes** contre des pièces du porte-monnaie ; chaque boîte
donne **un** cosmétique tiré au hasard selon sa **rareté**. Un panneau latéral à trois
onglets : **Boutique** (les boîtes, leur prix, les taux affichés), **Inventaire** (équiper,
vendre les doublons), **Index** (la collection : non obtenus en silhouette, Secrets en
« ??? »). Ouvrable au début de l'étage 1 et sur les écrans Perdu / Encaissé, jamais pendant
une partie en cours.

| Boîte | Prix | Contenu |
|---|---|---|
| Boîte de skins n°1 | 100 pièces | 16 skins (couleurs, motifs, matières) |
| Boîte d'accessoires n°1 | 100 pièces | 16 accessoires sur 4 emplacements : Tête, Visage, Cou, Dos (un accessoire par emplacement) |

| Rareté | Taux de drop | Objets par boîte | Revente d'un doublon |
|---|---|---|---|
| Commun | 50 % | 5 | 10 |
| Peu commun | 25 % | 4 | 15 |
| Rare | 14 % | 3 | 25 |
| Super rare | 7 % | 2 | 40 |
| Légendaire | 3 % | 1 | 60 |
| Secret | 1 % | 1 | 80 |

Les 5 couleurs et 4 chapeaux de la v1 sont gardés (même identifiant) et rangés dans les
boîtes avec une rareté ; ceux déjà achetés restent possédés.

Repère d'équilibrage (~15 pièces par partie, ~40 pièces par minute) : première boîte en
~2,5 min, le Légendaire d'une boîte en ~1 h 15 en moyenne, une boîte complète en ~4 h.

**Garde-fous** : taux affichés = taux du code (une seule source), tirage fait et sauvegardé
avant l'animation, suspense identique quelle que soit la rareté, pas de compte à rebours ni
d'offre limitée, on ne peut vendre que les doublons (jamais le dernier exemplaire), revente
toujours inférieure au prix d'une boîte.

**Plus tard** (hors version 2) :

| Catégorie | Exemples | Prix indicatif |
|---|---|---|
| Dés | Dé doré, dé en cristal | (en boîte) |
| Effets de victoire | Confettis, feux d'artifice | (en boîte) |
| Boîtes n°2 | Nouveaux skins et accessoires | 100 |

## 6 bis. Sons et animations (game feel)
Chaque moment de la boucle a son animation et son son : suspense (le coffre tremble, plus
longtemps quand on monte), ouverture avec rebond, pièces qui jaillissent sur un trésor,
« pouf » cartoon sur un piège, personnage qui saute ou baisse la tête, compteurs qui
défilent, transition automatique qui donne l'impression de **monter** dans la tour (avec un
décor qui change par paliers d'altitude), ouverture de boîte avec une révélation d'autant
plus spectaculaire que l'objet est rare. Musique de boîte à
musique en boucle + une couche de tension qui grandit avec l'étage. Deux boutons
« Musique » / « Effets » mémorisés.

Règle : le spectacle ne triche jamais — le suspense dure autant pour un trésor que pour un
piège, et rien d'affiché ou d'entendu ne change les chances.
La caméra est fixe pendant le jeu ; elle ne bouge que pendant les transitions d'étage.

Détails complets : `docs/specs/sons-animations.md`.

## 7. Liste des éléments à produire
**Modèles 3D (Blender, par script)** : la tour et ses étages, le personnage, 3 coffres, le dé,
la roue, la pièce géante, les chapeaux et les 12 nouveaux accessoires (`docs/specs/boites.md` §5.6).

**Images (Gemini)** : concept art général, textures (pierre de la tour, bois des coffres),
icônes des cosmétiques et des 2 boîtes, images UI de révélation (halo, rayons, étincelle),
4 fonds de ciel (un par palier d'altitude), logo du jeu.

**Code (Unity C#)** : gestion de partie (nouveau flux d'étage), épreuves, économie,
sauvegarde, boîtes / inventaire / index, décor par altitude, interface.

## 8. Étapes de développement (jalons)
1. **Prototype gris** : des cubes, une seule épreuve (les 3 coffres), encaisser / continuer.
2. **Boucle complète** : les 4 épreuves, cagnotte, porte-monnaie, sauvegarde.
3. **Shop** : achat et équipement des cosmétiques.
4. **Habillage** : modèles Blender, textures Gemini, sons, interface soignée.
5. **Équipe d'agents** : sous-agents spécialisés pour accélérer la suite.
6. **Version 2** : nouveau flux d'étage (montée automatique + « Encaisser X »), décor par
   altitude, boîtes à surprises avec raretés, inventaire et index.

## 9. Règle d'or
Le jeu reste un jeu : **aucun achat en argent réel**, aucune pièce achetable.
Les pièces se gagnent uniquement en jouant (et en revendant ses doublons). Les boîtes ne
s'ouvrent qu'avec ces pièces virtuelles, leurs taux sont affichés exactement comme dans le
code, et les cosmétiques ne changent jamais les chances.
