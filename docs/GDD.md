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
| Personnage | Un **cheval** de jouet, style cavalier de jeu d'échecs (v3 ; avant : une capsule) |
| Style visuel | Low-poly coloré, ambiance fête foraine magique ; de jour au pied de la tour, la nuit tombe en montant |
| Moteur | Unity 6.3 LTS, rendu URP |
| Plateforme | Mac / PC (desktop) |
| Joueurs | 1 joueur, hors ligne ; classement local (v4), prévu en ligne plus tard (`docs/specs/v4.md` §12) |
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
                                                  └──────> Rejouer <──────┘

 Colonne de gauche : Boutique · Inventaire · Index · Améliorations ; colonne de droite :
 Classement. Elles apparaissent APRÈS « Jouer » (v4 : rien sur l'écran titre, sauf la roue
 dentée des Réglages) et ouvrent des pop-ups centrées aux moments calmes (voir §6).
```
**v4** (`docs/specs/v4.md`) : interface façon jeu de plateforme (gros boutons arrondis à
contour épais qui rebondissent, pop-ups centrées sur fond assombri, barre de défilement dorée),
badge « ÉTAGE N » coloré selon l'altitude, roue dentée Réglages (musique / effets) à côté.
**Nouveau flux (v2)** : après une épreuve gagnée, on monte **directement** à l'étage suivant
(transition « on monte »). Pendant le choix du coffre suivant, un bouton **« Encaisser X »**
(X = cagnotte) reste proposé : choisir un coffre = continuer, cliquer « Encaisser » = mettre
la cagnotte à l'abri. À l'étage 1, rien à encaisser (bouton caché). Coffres et bouton sont
bloqués pendant la transition et 0,5 s après, pour éviter les clics accidentels.
Détails : `docs/specs/sons-animations.md` §2.1 et §3.11.

**Le décor change avec l'altitude** (pur affichage) — **v3 : on commence de jour** : étages
1-3 en plein jour (ciel bleu clair), 4-6 dans les nuages au coucher du soleil, 7-9 au-dessus
des nuages au crépuscule, 10 et plus dans les étoiles. Détails : `docs/specs/v3.md` §9
(remplace le tableau de `docs/specs/sons-animations.md` §3.14).

**« Rejouer »** après un piège à l'étage 1 : pas de redescente (on est déjà en bas) mais un
« remélange sur place » des coffres (`docs/specs/v3.md` §12). Chaque montée affiche une phrase
drôle tirée au hasard, chaque piège aussi (`docs/specs/v3.md` §11).

## 4. Les épreuves (une par étage, tirée au hasard)
| Épreuve | Principe | Chance de gagner |
|---|---|---|
| **Les 3 coffres** | Choisir 1 coffre sur 3 ; un seul est piégé (v4 : de 4 à 6 coffres avec l'amélioration « Coffre en plus », toujours un seul piégé) | 66 % (jusqu'à 83,3 % au niveau 10 des améliorations) |
| **Le dé** | Lancer un dé, il faut faire 3 ou plus | 66 % |
| **La roue** | Faire tourner une roue à secteurs (gain / perte / bonus) | ~60 % |
| **Pile ou face** | Une pièce géante, on choisit un côté | 50 % (gain doublé) |

Plus on monte, plus les épreuves risquées apparaissent souvent.

## 5. L'économie (v4, détails et calculs : `docs/specs/v4.md` §9 à §11 ; v3 : `v3.md` §5 et §6)
- Gain de l'étage N = `arrondi(10 × N × (1 + bonus Bourse + bonus de tenue) × rareté des
  coffres)` pièces. Sans amélioration ni tenue : `10 × N` (étage 1 = 10, étage 5 = 50…). Le
  gain est figé quand l'étage est préparé et affiché sur l'étiquette d'étage (« Trésor : +54 »).
- **Bonus de tenue (v4)** : chaque cosmétique équipé ajoute Commun +1 %, Peu commun +2 %,
  Rare +4 %, Super rare +6 %, Légendaire +10 %, Secret +15 % ; total plafonné à +50 %. Il ne
  touche que les gains des trésors, jamais les chances.
- **Rareté des coffres** (même rareté pour tous les coffres d'un étage, tirée et affichée avant
  le choix) : Bois ×1, Bronze ×1,5, Argent ×2, Or ×3, Diamant ×5.
- **Cagnotte** : la somme des gains de la partie en cours, perdue si on échoue.
- **Porte-monnaie** : les pièces encaissées, gardées pour toujours (sauvegarde).
- **Paliers de sécurité** : tous les 5 étages, une partie de la cagnotte est mise à l'abri
  (toujours prévu, pas encore fait).
- Repères : au départ ~15 pièces par partie (~41 par minute) ; tout au niveau 5 ~143 par
  partie (~293 par minute) ; tout au niveau 10 ~565 par partie (~924 par minute). Niveaux 1 à 5 :
  ~3 h (26 300 pièces) ; tout au niveau 10 : ~10 h 40 sans boîte (286 800 pièces).

## 6. Le shop : boîtes à surprises (cosmétiques uniquement)
Aucun objet ne change les chances de gagner. **v4** : équipés, ils donnent un petit bonus de
pièces sur les trésors (bonus de tenue, §5), affiché en chiffres.

**v4** : les 4 panneaux deviennent des **pop-ups centrées** (`docs/specs/v4.md` §5), ouvrables
seulement après « Jouer » ; les noms sont simplifiés (« Rouge », « Arc-en-ciel », « Couronne »…,
liste complète : `v4.md` §8) ; la Galaxie devient une nébuleuse animée (`v4.md` §7).

Détails complets (règles, catalogue, écrans, animation, sauvegarde, critères) :
`docs/specs/boites.md` (la v1 à achat direct, `docs/specs/shop.md`, est remplacée).

**Version 2** : on ouvre des **boîtes** contre des pièces du porte-monnaie ; chaque boîte
donne **un** cosmétique tiré au hasard selon sa **rareté**.

**Version 3 : quatre panneaux séparés** (`docs/specs/v3.md` §4), chacun ouvert par son bouton
dans une **colonne toujours visible sur le bord gauche** : **Boutique** (les boîtes, leur prix,
les taux affichés), **Inventaire** (équiper, vendre les doublons), **Index** (la collection :
non obtenus en silhouette, Secrets en « ??? ») et **Améliorations** (§6 ter).
**Quand peut-on les ouvrir ?** (v4 : plus sur l'écran titre) Pendant le choix d'un coffre à **n'importe
quel étage**, et sur les écrans Perdu / Encaissé. **Jamais pendant qu'une épreuve se joue**
(suspense, ouverture, révélation), ni pendant une transition (montée, redescente, remélange,
fondu du titre, verrou anti-clic). Pas de triche possible : le piège est déjà tiré, rien ne le
montre ni ne le change, on ne paie qu'avec le porte-monnaie (jamais la cagnotte), et une
amélioration achetée ne s'applique qu'à l'étage suivant.

| Boîte | Prix | Contenu |
|---|---|---|
| Boîte de skins n°1 | **250 pièces** (v3 ; 100 en v2) | 16 skins (couleurs, motifs, matières) |
| Boîte d'accessoires n°1 | **250 pièces** | 16 accessoires sur 4 emplacements : Tête, Visage, Cou, Dos (un accessoire par emplacement) |

| Rareté | Taux de drop | Objets par boîte | Revente d'un doublon (v3) |
|---|---|---|---|
| Commun | 50 % | 5 | 25 |
| Peu commun | 25 % | 4 | 40 |
| Rare | 14 % | 3 | 60 |
| Super rare | 7 % | 2 | 100 |
| Légendaire | 3 % | 1 | 150 |
| Secret | 1 % | 1 | 200 |

Les 5 couleurs et 4 chapeaux de la v1 sont gardés (même identifiant) et rangés dans les
boîtes avec une rareté ; ceux déjà achetés restent possédés.

Repère d'équilibrage v3 (`docs/specs/v3.md` §6.5) : première boîte en ~6 min au départ ; le
Légendaire d'une boîte en ~3 h sans amélioration, ~1 h à mi-parcours, ~25 min au maximum ;
améliorations + 2 boîtes complètes en 6 à 7 h de jeu.

**Secret** : son obtention déclenche une vraie cinématique (coupure noire, battements de cœur,
fissures, l'écran qui vole en éclats, carte qui tombe en tournant, confettis), 5,4 s, toujours
passable. Les Légendaires « Arc-en-ciel » et « Auréole » sont embellis
(`docs/specs/v3.md` §8).

**Garde-fous** : taux affichés = taux du code (une seule source), tirage fait et sauvegardé
avant l'animation, suspense identique quelle que soit la rareté, pas de compte à rebours ni
d'offre limitée, on ne peut vendre que les doublons (jamais le dernier exemplaire), revente
toujours inférieure au prix d'une boîte.

**Plus tard** (hors version 2) :

| Catégorie | Exemples | Prix indicatif |
|---|---|---|
| Dés | Dé doré, dé en cristal | (en boîte) |
| Effets de victoire | Confettis, feux d'artifice | (en boîte) |
| Boîtes n°2 | Nouveaux skins et accessoires | 250 |

## 6 ter. Les améliorations (v4 : 10 niveaux, progression assumée)
Achetées avec le porte-monnaie, **10 niveaux** chacune (5 en v3, niveaux 1 à 5 inchangés),
prix croissants, effet à partir du prochain étage, affiché en chiffres dans la pop-up et sur
l'étiquette d'étage du HUD. Détails : `docs/specs/v3.md` §5 et `docs/specs/v4.md` §10.

| Amélioration | Effet niveau 1 → 5 → 10 | Prix des niveaux 1 à 5 | Prix des niveaux 6 à 10 |
|---|---|---|---|
| **Bourse magique** | Trésors ×1,2 → ×2,0 → ×3,0 (+20 % par niveau) | 150 · 400 · 900 · 1 800 · 3 500 | 6 000 · 9 000 · 13 000 · 18 000 · 25 000 |
| **Coffres précieux** | Gain moyen ×1,13 → ×2,01 → ×2,79 (plus de bois au niveau 10) | 200 · 500 · 1 100 · 2 200 · 4 000 | 7 000 · 10 500 · 15 000 · 21 000 · 29 000 |
| **Coffre en plus** | 4, 5 puis 6 coffres, toujours 1 seul piégé ; risque moyen 31 % → 22,5 % → 16,7 % | 300 · 750 · 1 500 · 3 000 · 6 000 | 9 000 · 13 500 · 19 500 · 27 000 · 38 000 |

Garde-fous : toujours exactement 1 piège par étage, risque jamais sous **1 sur 6 (16,7 %)**
(v4 ; 20 % en v3, à valider par l'auteur), un piège fait toujours perdre toute la cagnotte,
chiffres affichés = chiffres du code (une seule source).

## 6 quater. Le classement (v4)
Trois classements : **étage le plus haut**, **total des pièces encaissées** (pas le
porte-monnaie, qui baisse à chaque achat), **Secrets obtenus** (doublons compris). En v4 il est
**local** : les meilleures parties de cet ordinateur, jamais de faux joueurs. L'architecture
(interface de service) est prête pour Unity Gaming Services (connexion anonyme, pseudo filtré,
3 classements en ligne) : checklist dans `docs/specs/v4.md` §12.11.

## 6 bis. Sons et animations (game feel)
Chaque moment de la boucle a son animation et son son : suspense (le coffre tremble, plus
longtemps quand on monte), ouverture avec rebond, pièces qui jaillissent sur un trésor,
« pouf » cartoon sur un piège, personnage qui saute ou baisse la tête, compteurs qui
défilent, transition automatique qui donne l'impression de **monter** dans la tour (avec un
décor qui change par paliers d'altitude), ouverture de boîte avec une révélation d'autant
plus spectaculaire que l'objet est rare. Musique de boîte à
musique en boucle + une couche de tension qui grandit avec l'étage. Musique et effets se
règlent (oui / non, mémorisés) dans la pop-up Réglages ouverte par la roue dentée (v4).

Règle : le spectacle ne triche jamais — le suspense dure autant pour un trésor que pour un
piège, et rien d'affiché ou d'entendu ne change les chances. (v3 : les coffres précieux ont un
suspense un peu plus long et une ouverture plus spectaculaire, **identiques pour un trésor et
pour un piège** de la même rareté, rareté connue avant le choix.) Les boutons (v4) sont gros
et arrondis : contour épais, épaisseur 3D, dégradé, reflet, ombre, rebond au survol et au
relâché, enfoncement à l'appui (`docs/specs/v4.md` §5, remplace `v3.md` §10).
La caméra est fixe pendant le jeu ; elle ne bouge que pendant les transitions d'étage.

Détails complets : `docs/specs/sons-animations.md`.

## 7. Liste des éléments à produire
**Modèles 3D (Blender, par script)** : la tour et ses étages, le personnage (**cheval**, v3),
le coffre et ses 4 variantes de rareté (bronze, argent, or, diamant), le dé, la roue, la pièce
géante, les chapeaux et les 12 nouveaux accessoires (`docs/specs/boites.md` §5.6), plus les
retouches pour le cheval (`docs/specs/v3.md` §16).

**Images (Gemini)** : concept art général, textures (pierre de la tour, bois des coffres),
icônes des cosmétiques (skins à refaire avec le cheval) et des 2 boîtes, 4 fonds de ciel
(jour, coucher de soleil, crépuscule, nuit étoilée), icônes des 4 panneaux, des 3
améliorations et des 5 coffres de rareté, logo du jeu (`docs/specs/v3.md` §15).

**Code (Unity C#)** : gestion de partie (nouveau flux d'étage), épreuves, économie,
améliorations, sauvegarde, boîtes / inventaire / index, 4 panneaux, décor par altitude,
interface.

## 8. Étapes de développement (jalons)
1. **Prototype gris** : des cubes, une seule épreuve (les 3 coffres), encaisser / continuer.
2. **Boucle complète** : les 4 épreuves, cagnotte, porte-monnaie, sauvegarde.
3. **Shop** : achat et équipement des cosmétiques.
4. **Habillage** : modèles Blender, textures Gemini, sons, interface soignée.
5. **Équipe d'agents** : sous-agents spécialisés pour accélérer la suite.
6. **Version 2** : nouveau flux d'étage (montée automatique + « Encaisser X »), décor par
   altitude, boîtes à surprises avec raretés, inventaire et index.
7. **Version 3** (`docs/specs/v3.md`) : correctifs, 4 panneaux et colonne de boutons, boutons
   stylés, boîtes à 250, améliorations, décor de jour, textes aléatoires, cheval, Légendaires
   embellis et cinématique Secret.
8. **Version 4** (`docs/specs/v4.md`) : écran titre sans menus, interface façon jeu de
   plateforme (boutons, pop-ups, défilement), badge d'étage, Réglages, Galaxie animée, noms
   simplifiés, bonus de tenue, améliorations jusqu'au niveau 10 (6 coffres), classement local
   prêt pour le en-ligne.

## 9. Règle d'or (mise à jour v4, **à valider par l'auteur**)
Le jeu reste un jeu : **aucun achat en argent réel**, aucune pièce achetable.
Les pièces se gagnent uniquement en jouant (et en revendant ses doublons). Les boîtes ne
s'ouvrent qu'avec ces pièces virtuelles, leurs taux sont affichés exactement comme dans le
code.
- Les **cosmétiques** ne changent **jamais les chances** : ni le piège, ni le nombre de
  coffres, ni la rareté des coffres, ni les taux des boîtes. **v4** : équipés, ils donnent un
  **bonus de pièces sur les trésors** (bonus de tenue : +1 % à +15 % par objet selon sa rareté,
  total plafonné à +50 %), affiché en chiffres avant le choix ; il ne touche que les **gains**.
- Les **améliorations**, elles, changent le jeu : c'est une **progression assumée**, achetée
  uniquement avec des pièces gagnées en jeu (jamais d'argent réel). Leur effet est affiché
  clairement, en chiffres lus dans les mêmes données que le code. Il reste toujours exactement
  un coffre piégé par étage (risque jamais sous **1 sur 6, soit 16,7 %**, v4).
- Ni une amélioration achetée ni un cosmétique équipé ne changent l'étage en cours : ils
  s'appliquent au prochain étage préparé.
- Le classement ne montre jamais de faux joueurs.
- **Le spectacle ne triche jamais** : le résultat est tiré avant l'animation, et rien de ce
  qu'on voit ou entend ne permet de le deviner.
- Pas de pression : pas de compte à rebours, d'offre limitée, de prix barré ni de pastille
  d'incitation.
