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
 Menu ──> Lancer une partie ──> Étage N : épreuve de hasard
                                     │
                         ┌───────────┴───────────┐
                      Gagné                    Perdu
                         │                        │
              Gain de l'étage ajouté      Gains de la partie perdus
              à la « cagnotte »           (sauf filet de sécurité)
                         │                        │
           ┌─────────────┴────────────┐           │
       ENCAISSER                  CONTINUER       │
   (cagnotte -> porte-monnaie)   (étage N+1)      │
           │                          │           │
           └──────> Shop / Menu <─────┴───────────┘
```

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

## 6. Le shop (cosmétiques uniquement)
Aucun objet du shop ne change les chances de gagner : c'est purement esthétique.

Détails complets (règles, écrans, sauvegarde, critères) : `docs/specs/shop.md`.

**Version 1 (jalon 3)** : deux catégories, portées par un personnage simple (une capsule
derrière les coffres). On achète, puis on équipe ; un seul objet équipé par catégorie.
Le shop s'ouvre au début de partie (étage 1, avant de choisir) et sur les écrans
Perdu / Encaissé, jamais pendant une épreuve.

| Catégorie | Objets (prix en pièces) |
|---|---|
| Couleurs du personnage | Bleu ciel (gratuit, défaut), Rouge tomate 50, Vert menthe 75, Violet magique 150, Or massif 300 |
| Chapeaux | Aucun (gratuit, défaut), Casquette 100, Chapeau de fête 150, Haut-de-forme 250, Couronne royale 500 |

Repère d'équilibrage : un joueur moyen gagne ~15 pièces par partie en moyenne (il perd
souvent) → premier achat en ~3-4 parties, couronne en ~33 parties, tout le shop en ~105.

**Plus tard** (hors version 1) :

| Catégorie | Exemples | Prix indicatif |
|---|---|---|
| Dés | Dé doré, dé en cristal | 150 – 400 |
| Effets de victoire | Confettis, feux d'artifice | 200 – 600 |

## 7. Liste des éléments à produire
**Modèles 3D (Blender, par script)** : la tour et ses étages, le personnage, 3 coffres, le dé,
la roue, la pièce géante, les chapeaux.

**Images (Gemini)** : concept art général, textures (pierre de la tour, bois des coffres),
icônes du shop, logo du jeu.

**Code (Unity C#)** : gestion de partie, épreuves, économie, sauvegarde, shop, interface.

## 8. Étapes de développement (jalons)
1. **Prototype gris** : des cubes, une seule épreuve (les 3 coffres), encaisser / continuer.
2. **Boucle complète** : les 4 épreuves, cagnotte, porte-monnaie, sauvegarde.
3. **Shop** : achat et équipement des cosmétiques.
4. **Habillage** : modèles Blender, textures Gemini, sons, interface soignée.
5. **Équipe d'agents** : sous-agents spécialisés pour accélérer la suite.

## 9. Règle d'or
Le jeu reste un jeu : **aucun achat en argent réel**, aucune pièce achetable.
Les pièces se gagnent uniquement en jouant.
