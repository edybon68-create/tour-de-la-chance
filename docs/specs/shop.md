# Spécification — Le shop de cosmétiques (version 1)

> **REMPLACÉE par `docs/specs/boites.md` (shop v2 : boîtes à surprises, raretés, inventaire,
> index).** Ce document est gardé comme historique et comme référence pour deux parties qui
> restent valables :
> - **§2 Le personnage** (construction, position, absence de collider) — complété par
>   `boites.md` §5.4 (nouveaux points d'accroche Visage, Cou, Dos) ;
> - **§4.1 et §4.2** : le rendu des 5 couleurs et la construction en primitives des 4 chapeaux
>   existants (identifiants inchangés ; leurs **prix sont abandonnés** : ils sont maintenant
>   dans les boîtes avec une rareté).
>
> Tout le reste (achat direct, prix, panneau en bas d'écran, clé `shop_owned`, boutons
> « Encaisser / Continuer », état `Decision`) est **périmé**.

> Jalon 3 du GDD. Cette spec décrit **quoi** faire, pas **comment** le coder.
> Elle s'appuie sur le prototype existant : `GameManager` (états `Choosing`, `Revealing`,
> `Decision`, `Lost`, `CashedOut`), `Wallet` (statique, PlayerPrefs), `HudController`
> (UI Toolkit, `Assets/UI/Hud.uxml` + `Hud.uss`), scène `Tour.unity`.

---

## 1. Objectif pour le joueur

Donner une **raison de garder ses pièces** : chaque pièce encaissée rapproche d'un chapeau ou
d'une couleur qu'on voit ensuite sur son personnage à chaque partie. C'est ce qui rend la
décision « Encaisser ou Continuer ? » intéressante : encaisser, c'est se rapprocher d'un objet.

**Règle d'or rappelée** : les objets sont purement visuels. Ils ne changent **jamais** les
chances, les gains ni rien d'autre dans les règles. On ne peut pas acheter de pièces.

---

## 2. Le personnage joueur (nouveau, prérequis du shop)

Il n'existe pas encore de personnage. On en crée un très simple, qui porte les cosmétiques.

### 2.1 Construction (primitives Unity)

| Élément | Primitive | Position (dans la scène) | Taille | Couleur |
|---|---|---|---|---|
| `Joueur` (objet parent vide) | — | **(0 ; 0,3 ; 2,8)**, sur une estrade (voir `docs/specs/sons-animations.md` §4.3 ; avant : (0 ; 0 ; 2)) | — | — |
| `Corps` (enfant de `Joueur`) | Capsule | locale (0 ; 1 ; 0) | échelle (1 ; 1 ; 1) → 1 m de large, 2 m de haut | la couleur équipée (défaut : bleu, voir §4.1) |
| `OeilGauche` (enfant de `Joueur`) | Sphère | locale (-0,18 ; 1,6 ; -0,45) | échelle 0,15 | noir #111111 (ne change jamais) |
| `OeilDroit` (enfant de `Joueur`) | Sphère | locale (0,18 ; 1,6 ; -0,45) | échelle 0,15 | noir #111111 (ne change jamais) |
| `PointChapeau` (enfant de `Joueur`) | objet vide | locale (0 ; 2 ; 0) = le sommet de la tête | — | — |

Repères :
- Les coffres sont en z = 0 (le coffre du milieu en x = 0) et la caméra en (0 ; 5 ; -7).
  Le personnage est donc **derrière le coffre du milieu**, face à la caméra (les yeux
  regardent vers les z négatifs). Le bas de la capsule est caché par le coffre : c'est voulu.
- Les couvercles des coffres s'ouvrent vers l'arrière jusqu'à z ≈ 0,9 (jusqu'à ≈ 1,2 avec le
  bombé) : le personnage ne doit pas être traversé. Mise à jour : en (0 ; 0 ; 2), le
  personnage était caché par les couvercles ouverts ; il est désormais reculé et surélevé
  (avant de la capsule en z = 2,3), voir `docs/specs/sons-animations.md` §4.
- **Aucun collider** sur le personnage, les yeux ni les chapeaux : un clic sur eux ne doit pas
  gêner le choix des coffres (le clic des coffres utilise un rayon physique).
- Les yeux servent à montrer où est « l'avant » (utile pour la visière de la casquette).
- Matériaux : shader URP Lit, comme le reste du projet.

### 2.2 Ce que fait le personnage
Afficher la couleur et le chapeau équipés. (Ses animations — respiration, saut de joie,
tête basse — sont décrites dans `docs/specs/sons-animations.md` §3.7.)
Il est visible en permanence (pendant les épreuves aussi), et se met à jour immédiatement quand
on équipe un objet ou quand on prévisualise un objet dans le shop (§5.4).

---

## 3. Règles

### 3.1 Quand peut-on ouvrir le shop ?
Le bouton **« Boutique »** est visible uniquement dans ces trois situations :

| Situation | Condition exacte |
|---|---|
| Début de partie | `Floor == 1` **et** `State == Choosing` (aucun coffre encore choisi) |
| Écran « Perdu » | `State == Lost` |
| Écran « Encaissé » | `State == CashedOut` |

Jamais pendant une épreuve : pas à l'étage 2 et plus en `Choosing`, jamais en `Revealing`
ni en `Decision`. Dès que le joueur choisit un coffre à l'étage 1, le bouton disparaît.

Le shop **n'est pas un nouvel état de partie** : c'est un panneau ouvert « par-dessus » l'état
courant. Quand on le ferme, on revient exactement là où on était (même étage, même état,
même coffre piégé — le piège n'est pas retiré au sort).

### 3.2 Pendant que le shop est ouvert
- Les coffres **ne réagissent pas** : ni clic souris, ni touches 1, 2, 3.
  (Attention : `ChestTrial` lit le clavier et la souris lui-même dans `Update` ; un clic sur
  le panneau UI Toolkit ne bloque pas le rayon physique. Il faut donc un blocage explicite.)
- Les boutons du HUD (Encaisser, Continuer, Rejouer, Boutique) et le message central sont cachés.
- La barre du haut (étage, cagnotte, porte-monnaie) reste visible ; le porte-monnaie se met à
  jour immédiatement après un achat.
- La touche **Échap** ferme le shop (comme le bouton « Fermer »).

### 3.3 Acheter
- On paie **uniquement avec le porte-monnaie** (`Wallet`), jamais avec la cagnotte.
  (De toute façon, aux trois moments où le shop est ouvrable, la cagnotte vaut 0.)
- Achat possible seulement si : l'objet n'est pas déjà possédé **et** `Wallet.Coins >= prix`.
- Un achat retire le prix du porte-monnaie et ajoute l'objet aux objets possédés, puis
  **sauvegarde tout de suite** (comme `Wallet.Add`).
- Le porte-monnaie ne peut jamais devenir négatif. Il faudra ajouter à `Wallet` une façon de
  dépenser qui **refuse** (et ne change rien) si le solde est insuffisant.
- Acheter **n'équipe pas** l'objet : c'est une action séparée (« Acheter, puis Équiper »).
- Un double-clic rapide sur « Acheter » ne doit débiter qu'une seule fois (au 2e clic, l'objet
  est déjà possédé → refusé).

### 3.4 Équiper
- On ne peut équiper qu'un objet possédé.
- **Un seul objet équipé par catégorie** : équiper un chapeau remplace le chapeau précédent ;
  équiper une couleur remplace la couleur précédente. Les deux catégories sont indépendantes
  (on peut avoir une couleur ET un chapeau).
- Les objets gratuits (`couleur_bleu` et `chapeau_aucun`) sont possédés d'office, dès la
  première partie. Équiper `chapeau_aucun` = enlever son chapeau.
- Équiper est gratuit, illimité, et sauvegardé immédiatement.

### 3.5 Cas limites
| Cas | Comportement attendu |
|---|---|
| Pas assez de pièces | Bouton « Acheter » grisé (désactivé) + texte « Il te manque X pièces » |
| Objet déjà possédé | Pas de bouton « Acheter » ; bouton « Équiper » à la place |
| Objet déjà équipé | Bouton « Équipé » grisé |
| Sauvegarde contient un identifiant inconnu (objet supprimé, faute de frappe) | Ignoré ; si c'était l'objet équipé, on revient au défaut de la catégorie |
| Première partie (aucune sauvegarde) | Couleur `couleur_bleu`, chapeau `chapeau_aucun`, rien d'autre possédé |
| Fermer le shop après une prévisualisation | Le personnage reprend les objets **équipés** |
| Prix exactement égal au solde (ex. 50 pièces, objet à 50) | Achat autorisé, solde = 0 |

---

## 4. Contenu

### 4.1 Couleurs du personnage (catégorie `couleur`)

La couleur s'applique au `Corps` (la capsule) seulement : les yeux restent noirs, le chapeau
garde ses propres couleurs.

| Identifiant | Nom affiché | Prix | Couleur (hex) | Rendu |
|---|---|---|---|---|
| `couleur_bleu` | Bleu ciel | **gratuit** (défaut) | #4A90D9 | mat (smoothness 0,3) |
| `couleur_rouge` | Rouge tomate | 50 | #D64545 | mat (smoothness 0,3) |
| `couleur_vert` | Vert menthe | 75 | #3CC48A | mat (smoothness 0,3) |
| `couleur_violet` | Violet magique | 150 | #8E5BD6 | légèrement brillant (smoothness 0,6) |
| `couleur_or` | Or massif | 300 | #FFC83A | métal brillant (metallic 1, smoothness 0,8) |

### 4.2 Chapeaux (catégorie `chapeau`)

Construits en **primitives Unity**, comme enfants de `PointChapeau`. Les positions ci-dessous
sont **locales à `PointChapeau`** (y = 0 = sommet de la tête ; l'avant du personnage est vers
z négatif). Rappel des tailles de base Unity : sphère et cube = 1 m ; cylindre = 1 m de
diamètre et **2 m de haut** (donc pour un cylindre de hauteur h, échelle y = h ÷ 2).
Toutes les pièces sans collider. De vrais modèles Blender les remplaceront plus tard
(en gardant le même identifiant et le même point d'accroche).

| Identifiant | Nom affiché | Prix |
|---|---|---|
| `chapeau_aucun` | Aucun chapeau | **gratuit** (défaut) |
| `chapeau_casquette` | Casquette | 100 |
| `chapeau_fete` | Chapeau de fête | 150 |
| `chapeau_haut_de_forme` | Haut-de-forme | 250 |
| `chapeau_couronne` | Couronne royale | 500 |

**`chapeau_aucun`** — rien n'est affiché.

**`chapeau_casquette`** — une calotte arrondie avec une visière vers l'avant.

| Pièce | Primitive | Position locale | Échelle | Couleur |
|---|---|---|---|---|
| Calotte | Sphère | (0 ; -0,1 ; 0) | (1,0 ; 0,6 ; 1,0) | rouge #E04040 |
| Visière | Cube | (0 ; -0,12 ; -0,62) | (0,6 ; 0,05 ; 0,45) | rouge foncé #A02828 |
| Bouton du dessus | Sphère | (0 ; 0,2 ; 0) | 0,1 | blanc #FFFFFF |

**`chapeau_fete`** — un « cône » pointu fait de 3 cylindres empilés (Unity n'a pas de cône),
penché de 10° sur le côté pour un air festif. Mettre les 4 pièces dans un objet vide
`Fete` (position locale (0 ; 0 ; 0), rotation (0 ; 0 ; 10°)).

| Pièce | Primitive | Position locale (dans `Fete`) | Taille finale | Échelle | Couleur |
|---|---|---|---|---|---|
| Étage bas | Cylindre | (0 ; 0,05 ; 0) | Ø 0,6 × h 0,2 | (0,6 ; 0,1 ; 0,6) | rose #FF6FB5 |
| Étage milieu | Cylindre | (0 ; 0,25 ; 0) | Ø 0,4 × h 0,2 | (0,4 ; 0,1 ; 0,4) | jaune #FFD54A |
| Étage haut | Cylindre | (0 ; 0,45 ; 0) | Ø 0,2 × h 0,2 | (0,2 ; 0,1 ; 0,2) | rose #FF6FB5 |
| Pompon | Sphère | (0 ; 0,62 ; 0) | Ø 0,18 | 0,18 | blanc #FFFFFF |

**`chapeau_haut_de_forme`** — le chapeau de magicien, noir avec un ruban rouge.

| Pièce | Primitive | Position locale | Taille finale | Échelle | Couleur |
|---|---|---|---|---|---|
| Bord | Cylindre | (0 ; -0,05 ; 0) | Ø 1,1 × h 0,05 | (1,1 ; 0,025 ; 1,1) | noir #1E1E1E |
| Tube | Cylindre | (0 ; 0,3 ; 0) | Ø 0,7 × h 0,7 | (0,7 ; 0,35 ; 0,7) | noir #1E1E1E |
| Ruban | Cylindre | (0 ; 0,05 ; 0) | Ø 0,72 × h 0,1 | (0,72 ; 0,05 ; 0,72) | rouge #C0392B |

**`chapeau_couronne`** — un anneau doré, 5 perles sur le dessus et une pierre rouge devant.
Matériau or : #FFC83A, metallic 1, smoothness 0,8 (le même rendu que `couleur_or`).

| Pièce | Primitive | Position locale | Taille finale | Échelle | Couleur |
|---|---|---|---|---|---|
| Anneau | Cylindre | (0 ; 0 ; 0) | Ø 0,8 × h 0,25 | (0,8 ; 0,125 ; 0,8) | or |
| Perles ×5 | Sphère | sur un cercle de rayon 0,38, à y = 0,17, tous les 72° en commençant devant (0 ; 0,17 ; -0,38) | Ø 0,15 | 0,15 | or |
| Pierre | Sphère | (0 ; 0 ; -0,41) | Ø 0,12 | 0,12 | rouge rubis #D62839, smoothness 0,9 |

> Remarque : si la capsule dorée porte la couronne dorée, c'est voulu (« tout en or »).

### 4.3 Équilibrage des prix

**Hypothèses** (prototype actuel, 3 coffres, 2 chances sur 3 de réussir chaque étage,
gain = 10 × étage) :

| Le joueur encaisse à l'étage… | Cagnotte encaissée | Chance d'y arriver | Pièces moyennes par partie |
|---|---|---|---|
| 1 | 10 | 67 % | 6,7 |
| 2 | 30 | 44 % | 13,3 |
| 3 | 60 | 30 % | 17,8 |
| 4 | 100 | 20 % | 19,8 |
| 5 | 150 | 13 % | 19,8 |

(« Pièces moyennes » = cagnotte × chance d'y arriver, puisque si on perd on repart avec 0.)

Un joueur moyen vise l'étage 2-3 et perd souvent : on retient **environ 15 pièces par partie
en moyenne** (fourchette 10 à 20). Aucune stratégie ne dépasse ~20 pièces par partie, donc
aucune façon de jouer ne « casse » l'économie. Une partie dure environ 20 à 30 secondes
(à vérifier en test).

| Objectif | Prix | Parties nécessaires (≈ 15 pièces/partie) | Temps de jeu estimé |
|---|---|---|---|
| Premier achat (`couleur_rouge`) | 50 | **≈ 3 à 4 parties** (5 au pire) | ~2 minutes |
| Premier chapeau (`chapeau_casquette`) | 100 | ≈ 7 parties | ~3 minutes |
| Objet le plus cher (`chapeau_couronne`) | 500 | **≈ 33 parties** (25 à 50 selon la chance) | ~15 minutes |
| Tout le shop | 1 575 | ≈ 105 parties | ~45 minutes |

Intention : un premier achat très rapide pour faire découvrir le shop, puis des objectifs
de plus en plus longs. Les prix restent dans les fourchettes du GDD (chapeaux 100–500,
couleurs 50–300). À réajuster quand les autres épreuves (dé, roue, pile ou face) et les
paliers de sécurité arriveront, car ils changeront les gains moyens.

---

## 5. Écrans et flux

### 5.1 Le bouton « Boutique » dans le HUD
- Nouveau bouton `shop` dans la rangée `actions` de `Hud.uxml`, texte **« Boutique »**,
  même style que les autres boutons (`btn`), couleur or (#C99A1E) pour l'associer aux pièces.
- Visible seulement dans les situations du §3.1.
- Sur les écrans Perdu / Encaissé, il est **à côté de « Rejouer »**.
- Au début de partie (étage 1, `Choosing`), il est seul dans la rangée ; le message reste
  « Choisis un coffre ! Un seul est piégé… ».

### 5.2 Le panneau du shop
Fichiers suggérés : `Assets/UI/Shop.uxml` + `Assets/UI/Shop.uss` (UI Toolkit, comme le HUD).
Le panneau occupe **la moitié basse de l'écran** (fond noir semi-transparent, coins arrondis),
pour que le personnage, en haut au centre, reste visible et serve d'aperçu.

De haut en bas :
1. **Titre** « Boutique » à gauche, **bouton « Fermer »** (×) à droite.
2. **Deux onglets** : « Couleurs » et « Chapeaux ». Onglet « Couleurs » ouvert par défaut.
   L'onglet actif est mis en évidence.
3. **Une rangée de cartes** (5 cartes par onglet, objet gratuit en premier, puis par prix
   croissant). Chaque carte montre :
   - un **visuel** : pour une couleur, un carré rempli de la couleur ; pour un chapeau,
     son nom en grand (les icônes viendront avec les images Gemini) ;
   - le **nom affiché** ;
   - l'**étiquette d'état** : le prix (« 150 pièces », en or), ou « Gratuit », ou « Possédé »,
     ou « Équipé » (avec une coche ✓) ;
   - la carte **sélectionnée** a un contour or épais.
4. **Une zone d'action** sous les cartes, pour la carte sélectionnée :

| État de l'objet sélectionné | Bouton affiché | Effet du clic |
|---|---|---|
| Non possédé, assez de pièces | « Acheter — 150 » (actif, vert) | Achète (§3.3). Le bouton devient « Équiper ». |
| Non possédé, pas assez | « Acheter — 150 » (grisé) + texte « Il te manque 40 pièces » | Aucun |
| Possédé, non équipé | « Équiper » (actif, bleu) | Équipe (§3.4). Le bouton devient « Équipé ». |
| Équipé | « Équipé ✓ » (grisé) | Aucun |

### 5.3 À l'ouverture
- Onglet « Couleurs », carte sélectionnée = la couleur actuellement équipée.
- En changeant d'onglet, la carte sélectionnée = l'objet équipé de cet onglet.

### 5.4 Prévisualisation (aperçu sur le personnage)
- Cliquer sur une carte la sélectionne **et** l'affiche sur le personnage, même si l'objet
  n'est pas possédé (c'est ce qui donne envie !).
- La prévisualisation n'est **pas** sauvegardée et n'équipe rien.
- À la fermeture du shop (bouton « Fermer » ou Échap), le personnage reprend les objets
  équipés.
- Chaque catégorie se prévisualise indépendamment : regarder un chapeau garde la couleur
  équipée (ou la couleur en aperçu si on l'a choisie juste avant dans l'autre onglet).

### 5.5 Flux résumé
```
 Étage 1 (Choosing)  ──┐
 Écran Perdu         ──┼──[Boutique]──> Panneau shop ──[Fermer / Échap]──> retour au même écran
 Écran Encaissé      ──┘                  │
                                          ├─ clic carte  → sélection + aperçu
                                          ├─ [Acheter]   → pièces -prix, objet possédé, sauvegarde
                                          └─ [Équiper]   → objet équipé, sauvegarde
```

### 5.6 Répartition dans le code (rappel des règles du projet)
- La **logique** (liste des objets, prix, possédés, équipés, acheter, équiper, sauvegarde)
  vit dans un script de règles, sans UI — par exemple une classe statique sur le modèle de
  `Wallet`. Le catalogue (§4) y est décrit en un seul endroit.
- Le droit d'ouvrir le shop (§3.1) et le blocage des coffres (§3.2) sont des **règles** :
  ils sont décidés côté `GameManager`, pas dans l'interface.
- L'**interface** du shop affiche et transmet les clics, sans règle de jeu.
- Le **personnage** a son propre script qui applique une couleur et un chapeau (un
  MonoBehaviour par fichier).

---

## 6. Données à sauvegarder (PlayerPrefs)

| Clé | Type | Contenu | Valeur par défaut |
|---|---|---|---|
| `wallet_coins` | int | déjà existante, inchangée | 0 |
| `shop_owned` | string | identifiants achetés, séparés par des virgules, ex. `couleur_rouge,chapeau_casquette` | `""` (vide) |
| `shop_equipped_couleur` | string | identifiant de la couleur équipée | `couleur_bleu` |
| `shop_equipped_chapeau` | string | identifiant du chapeau équipé | `chapeau_aucun` |

- Les objets gratuits ne sont pas écrits dans `shop_owned` : ils sont toujours possédés.
- On sauvegarde (avec `PlayerPrefs.Save()`) après **chaque** achat et **chaque** équipement.
- On sauvegarde des **identifiants** (pas des numéros de position dans une liste), pour
  pouvoir réordonner ou ajouter des objets plus tard sans casser les sauvegardes.
- Pour remettre à zéro après des tests : supprimer ces 4 clés.

---

## 7. Critères d'acceptation

Personnage
1. Au lancement de la scène `Tour`, une capsule bleue (#4A90D9) avec deux yeux noirs est
   visible derrière le coffre du milieu, face à la caméra, et ne masque aucun coffre.
2. Cliquer sur le personnage ne déclenche rien ; cliquer sur un coffre fonctionne toujours.
3. Ouvrir un coffre ne fait pas traverser le personnage par le couvercle.

Accès au shop
4. Au début d'une partie (étage 1, avant de choisir), le bouton « Boutique » est visible.
5. Dès que je choisis un coffre à l'étage 1, le bouton « Boutique » disparaît.
6. Aux étages 2 et plus, le bouton « Boutique » n'apparaît jamais, ni en `Choosing`,
   ni en `Revealing`, ni en `Decision`.
7. Sur l'écran Perdu et sur l'écran Encaissé, « Boutique » est visible à côté de « Rejouer ».
8. Shop ouvert à l'étage 1 : cliquer sur un coffre (à travers ou à côté du panneau) ou
   appuyer sur 1, 2 ou 3 n'ouvre aucun coffre.
9. Shop ouvert : les boutons Encaisser / Continuer / Rejouer / Boutique sont cachés.
10. « Fermer » et la touche Échap ferment le shop et me ramènent au même écran (même étage,
    même message, mêmes boutons). À l'étage 1, je peux ensuite choisir un coffre normalement.

Achat
11. Première partie (sauvegarde vide) : seuls « Bleu ciel » et « Aucun chapeau » sont marqués
    « Équipé » ; tous les autres objets affichent leur prix.
12. Si je possède 90 pièces et que l'objet coûte 100, le bouton « Acheter » est désactivé et
    le texte « Il te manque 10 pièces » s'affiche.
13. Si je possède 50 pièces et achète « Rouge tomate » (50), le porte-monnaie affiche 0
    (dans le shop et dans la barre du haut) et la carte affiche « Possédé ».
14. Après un achat, l'objet n'est pas équipé automatiquement ; le bouton affiche « Équiper ».
15. Un double-clic rapide sur « Acheter » ne débite le prix qu'une seule fois.
16. Le porte-monnaie n'est jamais négatif, quelle que soit la suite de clics.
17. Un objet possédé n'affiche jamais de bouton « Acheter ».

Équipement et aperçu
18. Cliquer sur « Équiper » pour un chapeau possédé le place sur la tête du personnage et
    retire le chapeau précédent ; la carte affiche « Équipé ✓ ».
19. Je peux porter une couleur et un chapeau en même temps.
20. Équiper « Aucun chapeau » retire le chapeau.
21. Cliquer sur la carte d'un objet non possédé l'affiche sur le personnage ; en fermant le
    shop, le personnage reprend ses objets équipés.
22. La couleur ne s'applique qu'à la capsule : les yeux restent noirs, les chapeaux gardent
    leurs couleurs.

Sauvegarde
23. J'achète et j'équipe « Casquette » et « Vert menthe », je quitte le mode Play et je le
    relance : le personnage porte la casquette et est vert, les deux cartes sont « Équipé »,
    le porte-monnaie a bien été débité.
24. Si `shop_equipped_chapeau` contient un identifiant inconnu, le jeu démarre sans erreur
    avec « Aucun chapeau ».

Règle d'or
25. Avec n'importe quel objet équipé, l'épreuve des coffres et les gains sont identiques
    (1 coffre piégé sur 3, gain = 10 × étage).
26. Il n'existe aucun bouton ou moyen d'obtenir des pièces autrement qu'en jouant.

---

## 8. Hors périmètre (pas dans cette version)

- Les catégories **Dés** et **Effets de victoire** du GDD (plus tard, avec les épreuves du dé
  et les effets visuels).
- Les vrais modèles 3D Blender des chapeaux et du personnage, les icônes Gemini des cartes.
- Les animations du personnage (saut de joie, tristesse quand il perd) et les sons d'achat.
- La revente d'objets, les remboursements, les promotions, les objets rares ou aléatoires
  (pas de « coffre mystère » à acheter : ce serait du hasard payant).
- Une fenêtre de confirmation avant achat (la sélection de la carte puis le clic sur
  « Acheter » font déjà deux étapes).
- Un menu principal : le shop n'est accessible que depuis les écrans listés au §3.1.
- Le cloud ou plusieurs profils de sauvegarde.
