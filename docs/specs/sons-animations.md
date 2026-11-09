# Spécification : sons et animations (le « game feel ») — première passe

> Jalon 4 du GDD (« Habillage »). Cette spec décrit **quoi** faire et **avec quels chiffres**,
> pas **comment** l'écrire en C#. Elle s'appuie sur le code existant : `GameManager` (états
> `Title`, `Choosing`, `Revealing`, `Decision`, `Lost`, `CashedOut` + le drapeau `ShopOpen`),
> `ChestTrial`, `Chest`, `PlayerAvatar`, `HudController`, `TitleController`, et sur la scène
> `Tour.unity` (coffres en x = −2,6 / 0 / +2,6, z = 0 ; personnage en (0 ; 0,3 ; 2,8) sur son
> estrade, voir §4 ; caméra en (0 ; 5 ; −7)).
>
> **Mise à jour v2 (nouveau flux d'étage, décor par altitude, boîtes).** L'état `Decision` et
> le bouton « Continuer » **disparaissent** : après un trésor, on monte directement à l'étage
> suivant, et le bouton « Encaisser X » est proposé pendant le choix du coffre suivant (§2.1).
> Nouvel état `Climbing` (« on monte »). Le décor change avec l'altitude (§3.14). Les sons des
> boîtes à surprises sont ajoutés (§5) ; leur animation est dans `docs/specs/boites.md` §7.6.
>
> **Mot de vocabulaire.** Le *game feel* (ou *juice*, « jus ») regroupe tout ce qui ne change pas
> les règles mais rend chaque action agréable : un objet qui rebondit, un son au bon moment,
> un compteur qui défile. Un jeu sans *juice* marche, mais il paraît « mort ».

---

## 1. Objectif pour le joueur

Que chaque moment de la boucle « quitte ou double » **se sente** : le cœur qui bat avant
d'ouvrir un coffre, la joie d'un trésor qui crache des pièces, la petite frustration (drôle)
du piège, le plaisir d'encaisser en voyant les pièces filer dans le porte-monnaie, et l'envie
de grimper encore un étage.

### Trois principes à respecter partout
1. **Le spectacle ne triche jamais.** Le piège est tiré au sort *avant* l'animation (c'est
   déjà le cas dans `ChestTrial.Begin`). Le suspense, le tremblement et leur durée sont
   **identiques** que le coffre soit piégé ou non : le joueur ne doit pas pouvoir deviner le
   résultat en regardant ou en écoutant (sinon ce serait un indice, donc une triche).
2. **La logique décide, l'affichage suit.** Les animations sont pilotées par des scripts
   d'affichage qui *écoutent* le `GameManager` et le `ChestTrial`. Ils ne changent jamais
   l'étage, la cagnotte, le porte-monnaie ni le piège.
3. **Tentation oui, piège psychologique non.** Continuer (choisir un coffre) est naturellement
   attirant, mais « Encaisser » reste bien visible et aussi facile pendant tout le choix :
   pas de compte à rebours, pas de clignotement agressif, pas de faux « presque gagné ».
   Règle d'or : pièces virtuelles uniquement.

---

## 2. Vue d'ensemble : la chronologie d'un étage

Les temps sont comptés à partir du clic sur un coffre. `S` = durée du suspense (§3.3).

| Temps | Ce qui se passe | Son |
|---|---|---|
| 0 | Clic sur un coffre → état `Revealing`, message « … » | `ui_clic` |
| 0 → S | Suspense : le coffre tremble, le personnage se penche en avant, les 2 autres coffres reculent | `coffre_tremble` |
| S → S + 0,22 | Le couvercle s'ouvre d'un coup jusqu'à 120° | `coffre_ouvre` |
| S + 0,22 | Couleur révélée (or ou rouge) + jaillissement de pièces **ou** nuage du piège + réaction du personnage | `resultat_tresor` + `pieces_jaillissement` **ou** `resultat_piege` |
| S + 0,22 → S + 0,47 | Rebond du couvercle (120° → 104° → 110°) | — |
| S + 0,77 | Les 2 autres coffres s'ouvrent (sans suspense) | `coffre_ouvre` (plus doux) |
| S + 1,47 | `ChestTrial.Finished` → état `Climbing` (trésor) ou `Lost` (piège) : compteurs qui défilent, texte « +20 » | `pieces_tic` (défilement) |
| **Trésor** : S + 2,07 | La montée commence toute seule (§3.11, 1,05 s) | `ui_etage_monte` |
| **Trésor** : S + 3,12 | Fin de la montée : nouvel étage, état `Choosing`, le bouton « Encaisser X » apparaît | — |
| **Trésor** : S + 3,62 | Fin du verrou anti-clic : coffres et « Encaisser » cliquables | — |
| **Piège** : S + 1,77 | Les boutons « Rejouer » et « Boutique » apparaissent (§3.10) | — |

Avec S = 0,6 s (étage 1), après un trésor, on peut choisir le coffre suivant (ou encaisser)
environ **4,2 s** après son clic. C'est à peu près le temps de l'ancien flux (résultat +
clic « Continuer » + transition), avec un clic de moins.

**Changement dans le code existant** : dans `ChestTrial.Reveal`, les attentes actuelles
(0,3 s puis 0,7 s) sont remplacées par cette chronologie.

### 2.1 Le nouveau flux d'étage (règles de jeu)
Ces règles appartiennent à la **logique** (`GameManager`), pas à l'affichage.

**États** : `Title`, `Choosing`, `Revealing`, **`Climbing`** (nouveau), `Lost`, `CashedOut`.
`Decision` est supprimé, ainsi que la méthode `Continue()` et le bouton « Continuer ».

**Après un trésor** (`ChestTrial.Finished(true)`) :
1. Cagnotte += `10 × étage` ; état → `Climbing`. Le verrou de transition est posé (coffres et
   « Encaisser » ignorés).
2. **+0,6 s** : le `GameManager` annonce « la montée commence » (événement écouté par
   l'affichage, §3.11). Ce délai laisse voir le trésor et le « +20 ».
3. **+0,5 s après le début de la montée** (écran sombre) : étage + 1, nouveau tirage du piège
   (`chestTrial.Begin()`), palier de décor recalculé (§3.14).
4. **+1,05 s après le début de la montée** : état → `Choosing`. Le verrou reste posé.
5. **+0,5 s** de plus (verrou **anti-clic**) : le verrou est retiré ; coffres et « Encaisser »
   deviennent cliquables.
Les durées (0,6 / 0,5 / 1,05 / 0,5 s) sont des constantes du `GameManager`, lues aussi par les
scripts d'affichage pour rester synchronisés. C'est la logique qui mène la danse ; si un
script d'animation manque, la partie avance quand même.

**Le bouton « Encaisser X »** (X = cagnotte, ex. « Encaisser 30 » avec l'icône pièce) :
- Visible seulement en `Choosing`, **à partir de l'étage 2** (à l'étage 1, la cagnotte vaut 0 :
  bouton caché), panneau fermé.
- Cliquable seulement quand le verrou de transition est retiré (fin du point 5).
- Clic : porte-monnaie += cagnotte, cagnotte = 0, état → `CashedOut` (fête de l'encaissement,
  §3.7 et §3.9). Les coffres de l'étage restent fermés et **le piège n'est pas montré**.
- Dès qu'un coffre est choisi (état `Revealing`), le bouton disparaît (opacité → 0 en 0,1 s) :
  on ne peut pas encaisser pendant le suspense.
- Place : dans la rangée de boutons en bas de l'écran, centré, loin des coffres (le clic sur
  « Encaisser » ne doit jamais tomber sur un coffre et inversement). Vert `#2E9E5B`, taille
  normale, **pas** de clignotement ni de frétillement.
- Pas de raccourci clavier (les touches 1, 2, 3 restent pour les coffres) : on évite les
  encaissements accidentels.
- Un double-clic n'encaisse qu'une fois (au 2e clic l'état n'est plus `Choosing`).
- Si un clic sur un coffre et sur « Encaisser » arrivent dans la même image, le premier traité
  gagne ; l'autre est refusé par l'état.

**Choisir un coffre = continuer** : il n'y a pas d'autre bouton pour monter.

**Après un piège** : inchangé (état `Lost`, cagnotte perdue, boutons « Rejouer » et « Boutique »).

**Clics pendant le verrou** : ignorés, **pas mis en file d'attente** (un clic trop tôt ne
déclenche rien plus tard).

---

## 3. Animations, moment par moment

> **Courbes utilisées** (t va de 0 à 1 pendant l'animation) :
> - *ease-out* (démarre vite, finit en douceur) : `1 − (1 − t)³`
> - *ease-in* (démarre doucement, finit vite) : `t³`
> - *ease-in-out* : `t < 0,5 ? 4t³ : 1 − (−2t + 2)³ / 2`
> - *sinus* : `sin(t × π)` (monte puis redescend, idéal pour un « pop »)
>
> **Règle générale** : toute animation qui modifie une position, une rotation ou une échelle
> **mémorise la pose de repos** au démarrage et **y revient exactement** à la fin (pas de
> dérive après 50 parties). Les animations du personnage déforment l'objet `Joueur`, dont
> l'origine est à la base : l'écrasement garde donc les « pieds » au sol et le chapeau suit
> tout seul (c'est un enfant).

### 3.1 Respiration du personnage (au repos)
- Quand : en permanence, sauf pendant une autre animation du personnage.
- Échelle Y : 1,00 ↔ 1,03 ; échelle X et Z : 1,00 ↔ 0,985 (le volume reste à peu près le même).
- Période : 2,4 s, courbe sinus continue.
- Petit balancement : rotation Z de ±1,5°, période 4,8 s (deux fois plus lent, ça évite l'effet
  « robot »).

### 3.2 Survol d'un coffre (invitation au clic)
- Quand : état `Choosing`, shop fermé, pas de transition ni de verrou anti-clic en cours, le curseur de la souris
  est sur le collider d'un coffre (même rayon que le clic, testé dans `Update`).
- **Entrée du survol** (0,12 s, ease-out) :
  - échelle du coffre 1,00 → 1,06 ;
  - le couvercle s'entrouvre de 0° à 8° (rotation X du `Charniere`/couvercle) ;
  - son `coffre_survol` (une seule fois à l'entrée, pas en boucle).
- **Pendant le survol** : le couvercle « respire » entre 5° et 11° (sinus, période 0,5 s) et
  le coffre fait un petit saut de 0,05 m toutes les 0,8 s (0,2 s par saut, sinus).
- **Le personnage regarde le coffre survolé** : rotation Y de 15° (0,2 s, ease-out), dans le
  sens qui déplace ses yeux vers le coffre à l'écran. Coffre du milieu : 0°.
- **Sortie du survol** (0,12 s, ease-out) : tout revient au repos (échelle 1, couvercle 0°,
  personnage de face).
- Les touches 1, 2, 3 ne déclenchent pas de survol (elles choisissent directement).

**Invitation si le joueur hésite** : si aucun coffre n'est survolé ni choisi pendant 3 s en
`Choosing`, les trois coffres font chacun un petit saut (0,08 m, 0,25 s, sinus), de gauche à
droite avec 0,1 s de décalage. Cela se répète toutes les 5 s. Pas de son.

### 3.3 Suspense avant l'ouverture
- Durée **S = 0,6 s + 0,08 s × (étage − 1)**, plafonnée à **1,0 s** (étage 1 : 0,6 s ;
  étage 3 : 0,76 s ; étage 6 et plus : 1,0 s). Plus on monte, plus on retient son souffle.
- **La même durée pour un trésor et pour un piège** (principe n°1).
- **Le coffre choisi** :
  1. *Tassement* (0,08 s) : échelle Y 1,06 → 0,92, échelle X/Z → 1,04 (il « inspire »).
  2. *Tremblement* (le reste de S) : rotation Z alternée gauche/droite à **16 allers-retours
     par seconde**, amplitude qui grandit de **1° à 5°** (linéaire) ; le couvercle claque de
     0° à 6° à 12 Hz (il « veut » s'ouvrir). Échelle qui revient à 1,00.
- **Les deux autres coffres** : échelle 1,00 → 0,95 en 0,15 s (ils s'effacent pour laisser la
  vedette).
- **Le personnage** : se penche en avant de 8° (rotation X, 0,2 s, ease-out) et cesse de
  respirer (il retient son souffle).
- Son : `coffre_tremble`, coupé (fondu de 0,03 s) au moment où le couvercle s'ouvre.

### 3.4 Ouverture du couvercle avec rebond
Remplace l'ouverture actuelle de `Chest.Open` (0,4 s jusqu'à 110°).
1. **Ouverture** : 0° → **120°** en **0,22 s**, ease-out (ça « pète »). Son `coffre_ouvre`.
2. **Révélation** à la fin de l'ouverture : couleur du `Corps` (or `#FFC83A` ou rouge `#D91A1A`)
   et petit « pop » : échelle 1 → 1,15 → 1 en 0,2 s (sinus) — garder celui qui existe.
3. **Rebond** (0,25 s) : 120° → 104° (0,12 s, ease-out) → 110° (0,13 s, ease-in-out).
   Position finale : **110°** (inchangée).
4. Les deux autres coffres s'ouvrent **0,3 s après la fin du rebond**, ensemble, **sans
   suspense** : 0° → 115° en 0,3 s puis 115° → 110° en 0,1 s ; même révélation de couleur ;
   pop réduit à 1,08. Son `coffre_ouvre` à volume réduit (§5.3), joué une seule fois.
   Leur échelle revient de 0,95 à 1,00 pendant l'ouverture.

### 3.5 Trésor : jaillissement de pièces
Au moment de la révélation d'un trésor **choisi par le joueur** (pas pour les deux autres
coffres révélés ensuite, sinon on fêterait aussi le coffre non choisi).
- **Nombre de pièces** : `8 + 2 × étage`, maximum 30 (étage 1 : 10 ; étage 5 : 18).
- **Pièce** : cylindre Unity, diamètre 0,22, épaisseur 0,05 (échelle (0,22 ; 0,025 ; 0,22)),
  matériau `M_Or` (`#FFC83A`, metallic 1, smoothness 0,8), **sans collider**.
- **Départ** : centre du coffre, à y = 0,9 (juste au-dessus du bord).
- **Vitesse** : verticale tirée au hasard entre 3,5 et 5 m/s ; horizontale tirée au hasard
  dans un disque de rayon 1,2 m/s. Gravité 9,81 m/s². (Hauteur max ≈ 0,6 à 1,3 m.)
- **Rotation** : axe au hasard, 540 à 900 °/s.
- **Fin** : la pièce disparaît quand elle redescend sous y = 0,6 ou après 1,2 s ; elle rétrécit
  jusqu'à 0 pendant ses 0,15 dernières secondes (pas de « pop » de disparition).
- **Réserve (pool)** : 30 pièces créées une seule fois au démarrage, cachées, réutilisées
  (créer/détruire des objets en boucle fait ramer).
- Le hasard des pièces utilise un générateur **séparé** de celui du piège (par exemple
  `new System.Random()`), pour qu'aucun effet visuel ne puisse influencer le tirage.
- Sons : `resultat_tresor` + `pieces_jaillissement` en même temps.

### 3.6 Piège : nuage cartoon et secousse
Au moment de la révélation d'un piège **choisi par le joueur**.
- **Nuage** : 10 sphères, diamètre de départ 0,25, couleur lavande grisée `#B9B4C8`
  (matériau URP Lit opaque, smoothness 0,1), sans collider. Elles partent du centre du
  coffre (y = 0,9), s'écartent de 0,8 m en montant de 0,5 m en 0,6 s (ease-out), grossissent
  jusqu'à 0,6 pendant les 0,3 premières secondes puis rétrécissent jusqu'à 0. Réserve de 10.
- **Secousse de caméra** : 0,25 s, déplacement au hasard (nouvelle direction toutes les
  0,033 s), amplitude 0,06 m qui décroît linéairement jusqu'à 0. La caméra revient
  **exactement** en (0 ; 5 ; −7) à la fin.
- Pas de sang, pas de crâne, rien d'effrayant (art bible §8) : c'est un « pouf » de dessin animé.
- Son : `resultat_piege`.

### 3.7 Réactions du personnage
Toutes partent de la pose de repos et y reviennent (sauf « tête basse »).

**Saut de joie** (trésor choisi) — 0,7 s :

| Phase | Durée | Ce qui change |
|---|---|---|
| Préparation | 0,08 s | échelle Y → 0,85, X/Z → 1,10 ; il se redresse (le penché de 8° revient à 0°) |
| Montée | 0,20 s, ease-out | +0,6 m en Y ; échelle Y → 1,10, X/Z → 0,95 (il s'étire) |
| Descente | 0,20 s, ease-in | retour à la hauteur de départ ; échelle → 1,00 |
| Atterrissage | 0,10 s | échelle Y → 0,88, X/Z → 1,08 |
| Retour | 0,12 s, ease-out | échelle → 1,00, reprise de la respiration |

**Écrasement puis tête basse** (piège choisi) :
1. *Écrasement* (0,12 s, ease-out) : échelle Y 1 → 0,75, X/Z → 1,15.
2. *Maintien* 0,25 s.
3. *Tête basse* (0,4 s, ease-in-out) : échelle Y → 0,92, X/Z → 1,04 ; penché en avant de
   **12°** (rotation X). Respiration ralentie (période 3,6 s, amplitude divisée par 2).
4. Il reste ainsi pendant l'état `Lost`, jusqu'à « Rejouer » ou l'ouverture du shop
   (retour au repos en 0,3 s, ease-out).

**Fête de l'encaissement** (bouton Encaisser) — 1,2 s : deux sauts de joie à la suite, le
second plus haut (+0,8 m) avec un **tour complet sur lui-même** (rotation Y 0 → 360°
pendant la montée et la descente).

**Petit saut d'accueil** (début de partie, aperçu ou équipement dans le shop) : 0,15 m,
0,25 s, sinus, sans préparation.

### 3.8 Compteurs qui défilent (HUD)
Le HUD affiche une **valeur affichée** qui court après la **valeur réelle** du `GameManager`
ou du `Wallet`. La valeur réelle change d'un coup (la logique), l'affichage la rattrape.

| Événement | Compteur | Durée | Effet |
|---|---|---|---|
| Gain (entrée en `Climbing`) | Cagnotte : ancienne → nouvelle | 0,5 s, ease-out | Le texte grossit 1 → 1,25 → 1 (0,25 s, sinus). Un `pieces_tic` toutes les 0,06 s pendant le défilement |
| Perte (`Lost`) | Cagnotte : ancienne → 0 | 0,6 s, ease-in | Texte en rouge `#D64545` pendant le défilement + tremblement horizontal ±4 px (16 Hz), puis retour à la couleur normale |
| Encaisser (`CashedOut`) | Cagnotte → 0 **et** Porte-monnaie → nouveau | 1,0 s, ease-in-out, en même temps | Pièces volantes (§3.9). Le porte-monnaie grossit 1 → 1,2 → 1 à chaque arrivée de pièce |
| Ouverture d'une boîte, vente d'un doublon | Porte-monnaie → nouveau | 0,4 s, ease-out | Pas de grossissement |
| Nouvelle partie | (la cagnotte vaut déjà 0) | — | — |

Arrondi : on affiche toujours un nombre entier (`Mathf.RoundToInt`). À la fin, la valeur
affichée est **exactement** la valeur réelle.

**Texte flottant « +20 »** : à l'entrée en `Climbing`, un texte `+{gain}` apparaît à l'écran
au-dessus du coffre choisi (position 3D du coffre + (0 ; 1,6 ; 0) convertie en position
écran). Couleur `#FFD54A`, contour `#3A2200`, taille 48. Il grossit 0,5 → 1,2 → 1 pendant
0,2 s, monte de 60 px en 0,9 s (ease-out) et s'efface pendant les 0,3 dernières secondes.

**Battement de la cagnotte** : en `Choosing`, quand la cagnotte est supérieure à 0 (donc à
partir de l'étage 2), le compteur « Cagnotte » bat doucement : échelle 1 ↔ 1,06, période
1,2 s à l'étage 2, puis 0,1 s plus rapide par étage, minimum 0,7 s. Ça rappelle ce qu'on
risque. Il s'arrête dès qu'un coffre est choisi. Le texte du bouton « Encaisser X » ne bat
pas (il reste calme et lisible, principe n°3).

### 3.9 Encaisser : les pièces filent dans le porte-monnaie
- Nombre de pièces volantes : `cagnotte ÷ 10`, entre 3 et 15.
- Ce sont des **éléments UI** (l'image de pièce déjà utilisée par la classe USS `coin`),
  taille 28 px, qui partent du compteur « Cagnotte » et arrivent sur le compteur
  « Porte-monnaie » (juste en dessous, en haut à droite).
- Trajet : 0,5 s chacune, ease-in-out, avec une courbe vers la gauche (point de contrôle à
  80 px à gauche du milieu du trajet). Une nouvelle pièce part toutes les 0,05 s.
- À chaque arrivée : `pieces_tic` (hauteur variée au hasard de ±5 %) et le porte-monnaie
  grossit (§3.8).
- En même temps : `resultat_encaisse` et la fête du personnage (§3.7).

### 3.10 Apparition des boutons
- Délai : les boutons « Rejouer » / « Boutique » apparaissent **0,3 s** après le changement
  d'état (`Lost`, `CashedOut`), pour laisser lire le résultat.
- Animation : opacité 0 → 1 et échelle 0,6 → 1,08 → 1,00 en 0,25 s ; chaque bouton suivant
  part 0,08 s après le précédent (de gauche à droite).
- Les boutons sont **cliquables seulement quand leur animation est finie** (évite qu'un
  double-clic sur le coffre clique aussi sur un bouton).
- Le message central apparaît en même temps que le premier bouton (opacité 0 → 1, 0,2 s).
- **Survol d'un bouton** : échelle 1,05 (0,08 s) + `ui_survol`. **Appui** : échelle 0,95 +
  `ui_clic`. (Faisable en USS avec `transition` et `:hover` / `:active`.)
- **« Encaisser X »** (étages 2 et plus) : apparaît avec la même animation **à la fin de la
  montée** (en même temps que le message « Choisis un coffre… ou encaisse ! »), et n'est
  cliquable qu'à la fin du verrou anti-clic (§2.1), 0,5 s plus tard. Pendant ces 0,5 s, il
  est affiché à opacité 0,6 puis passe à 1 (0,1 s) quand il devient cliquable : on voit qu'il
  « s'allume ».
- **Plus de « tentation douce »** (l'ancien frétillement de « Continuer ») : le bouton
  « Continuer » n'existe plus ; ce sont les coffres qui invitent au clic (§3.2).
- Le bouton « Boutique » au début de l'étage 1 apparaît avec la même animation.
- Message central en `Choosing` : étage 1 « Choisis un coffre ! Un seul est piégé… » ;
  étages 2 et plus « Choisis un coffre pour monter… ou encaisse tes X pièces ! ».

### 3.11 Passage à l'étage suivant : on monte !
La caméra reste fixe pendant le jeu (GDD §2) ; elle ne bouge **que** pendant cette transition.
L'idée : la caméra file vers le haut, l'écran s'assombrit, puis la caméra arrive *par en
dessous* sur le nouvel étage. Le mouvement est continu vers le haut : on a l'impression de
monter.

Déclenchement : **automatique**, 0,6 s après un trésor (§2.1) — il n'y a plus de bouton
« Continuer ». Durée totale : **1,05 s** (+ bannière), suivie de 0,5 s de verrou anti-clic.

| Temps | Durée | Ce qui se passe |
|---|---|---|
| 0 | 0,25 s | Le personnage fait une préparation + un saut (0,4 m). Le message disparaît (opacité → 0 en 0,1 s). Son `ui_etage_monte` |
| 0,10 | 0,40 s, ease-in | Caméra : y +3 m (de 5 à 8). Les nuages du décor (§3.14) défilent vers le bas. Un voile plein écran couleur nuit `#1F1730` passe d'opacité 0 à 1 |
| 0,50 | 0,05 s | Écran noir-violet : les couvercles sont remis fermés instantanément, couleurs bois, personnage au repos, compteur « Étage » mis à jour, **palier de décor appliqué s'il change** (§3.14), caméra placée à y = 2 (3 m **sous** sa place) |
| 0,55 | 0,50 s, ease-out | Caméra : y de 2 à 5 (elle continue de monter). Voile : opacité 1 → 0 |
| 0,55 | 1,1 s | **Bannière** au centre : « Étage 4 » (taille 72, `#FFD54A`, contour `#3A2200`) : échelle 0,5 → 1,1 → 1 (0,25 s), reste 0,55 s, s'efface 0,3 s. **Si on entre dans un nouveau palier**, une 2e ligne sous le numéro (taille 32, blanc cassé `#F4EEE6`) : le nom du palier, ex. « Dans les nuages ! » |
| 1,05 | — | État `Choosing` ; le message et « Encaisser X » apparaissent (§3.10) |
| 1,55 | — | Fin du verrou anti-clic : coffres (clic, 1/2/3, survol) et « Encaisser » actifs |

Règles :
- **Pendant toute la transition et le verrou anti-clic, les coffres ne réagissent pas** (ni
  clic, ni 1/2/3, ni survol) et « Encaisser » non plus. C'est le `GameManager` qui pose et
  retire ce blocage, comme pour le shop, mais avec un drapeau **séparé** (sinon fermer le
  shop pendant une transition débloquerait les coffres trop tôt). Le piège du nouvel étage
  est tiré normalement par `Begin()`, le blocage ne le change pas.
- Le compteur « Étage » change pendant l'écran sombre.
- Le fond de ciel (`SkyBackdrop`) suit la caméra ; son **image** change selon le palier (§3.14).
- La rotation de la caméra ne change pas (seulement sa hauteur).

**« Rejouer » (retour à l'étage 1)** : version courte et inversée, on **redescend** :
caméra y 5 → 3 + voile 0 → 1 (0,25 s, ease-in), remise à zéro dans le noir (couvercles
fermés, personnage au repos, **retour au décor du palier 1**), caméra y 7 → 5 + voile 1 → 0
(0,3 s, ease-out). Pas de bannière. Son `ui_nouvelle_partie`. Mêmes règles de blocage, plus
le même verrou anti-clic de 0,5 s à la fin.

### 3.12 Écran titre
- À l'apparition : le logo passe de l'échelle 0,8 à 1,05 puis 1,0 (0,6 s) avec opacité
  0 → 1 ; ensuite il flotte doucement : ±6 px en vertical, période 3 s (sinus).
- Le bouton « Jouer » apparaît 0,4 s après le logo (même animation que §3.10), puis
  « respire » : échelle 1 ↔ 1,05, période 1,6 s. Survol / appui : comme §3.10.
- La musique d'ambiance démarre dès le lancement, avec un fondu d'entrée de 1,5 s (§5.3).
- Clic sur « Jouer » : `ui_jouer`, l'écran titre s'efface (opacité 1 → 0 en 0,4 s), puis le
  personnage fait son **petit saut d'accueil** et le bouton « Boutique » apparaît.
  Pendant le fondu, les coffres ne sont pas encore cliquables (même blocage que §3.11).

### 3.13 Shop (panneau latéral, voir `docs/specs/boites.md` §7)
- Ouverture / fermeture : le panneau glisse depuis la droite (0,25 s ease-out / 0,2 s
  ease-in) ; sons `ui_boutique_ouvre` / `ui_boutique_ferme`.
- En `Lost` ou `CashedOut`, ouvrir le shop referme les couvercles : on garde ce comportement,
  mais animé (110° → 0° en 0,2 s, ease-in) avec un seul `coffre_ferme`. (Après un
  encaissement, les couvercles sont déjà fermés : rien à faire.)
- Le personnage sort de la « tête basse » (0,3 s) pour servir d'aperçu.
- Changer d'onglet : `ui_clic`. Cliquer une carte (aperçu) ou « Équiper » : petit saut
  d'accueil (§3.7) + `ui_clic`.
- Ouvrir une boîte : `pieces_achat` + défilement du porte-monnaie (§3.8), puis l'animation
  d'ouverture (`boites.md` §7.6) avec `boite/boite_secoue`, `boite/boite_ouverture`,
  `boite/revelation_<rarete>`.
- Vendre un ou plusieurs doublons : `ui/vente` + défilement du porte-monnaie.

### 3.14 Le décor change avec l'altitude
Plus on monte, plus la tour s'élève dans le ciel. Le décor change **par paliers**, toujours
pendant l'écran sombre de la montée (§3.11, temps 0,50 s) : on ne voit jamais le changement
se faire, on découvre le nouveau décor en arrivant.

**Pourquoi des paliers de 3 étages (et pas 1-4 / 5-9 / 10-14 / 15+) ?** Avec 2 chances sur 3
par étage, on atteint l'étage 4 dans 30 % des parties, l'étage 7 dans 9 %, l'étage 10 dans
2,6 %… mais l'étage 15 dans seulement 0,3 % (1 partie sur 300). Des paliers plus courts
permettent de voir au moins le 2e décor souvent et de rêver du dernier.

| Palier | Étages | Nom (2e ligne de la bannière) | Atteint dans… | Fond (image) | Nuages 3D | Lumière principale | Ambiance (Gradient : ciel / horizon / sol) |
|---|---|---|---|---|---|---|---|
| 1 | 1 – 3 | « Au pied des nuages » (pas de bannière à l'étage 1) | 100 % | `ciel_palier1.png` (= l'actuel `ciel_nuit.png`) : nuit violette, nuages **au-dessus** | aucun | `#FFF1D6`, 1,2 (actuelle) | `#8C7AB8` / `#5A4A70` / `#2A2030` (actuelle) |
| 2 | 4 – 6 | « Dans les nuages ! » | 30 % des parties | `ciel_palier2.png` : brume rose-lavande lumineuse, gros nuages tout autour | 6 groupes, juste sous l'étage (y −1,5 à −2,5), lavande clair `#D8CCE6` | `#FFE6E0` (rosée), 1,15 | `#B49AC8` / `#7A6290` / `#3A3048` |
| 3 | 7 – 9 | « Au-dessus des nuages ! » | 9 % | `ciel_palier3.png` : ciel bleu nuit dégagé, grosse lune ronde, mer de nuages en bas | 12 groupes, mer de nuages (y −2,5 à −4), blanc lavande `#E6E1EE` | `#E8ECFF` (clair de lune), 1,25 | `#8FA0D8` / `#5A6AA0` / `#2A3050` |
| 4 | 10 et + | « Dans les étoiles ! » | 2,6 % | `ciel_palier4.png` : ciel étoilé profond avec une aurore douce verte et violette | 4 groupes très bas (y −5 à −6), dans l'ombre `#8A80B0` | `#F0E6FF`, 1,1 | `#6A5AB0` / `#3A6A80` / `#1A1430` |

**Ce qui change à chaque palier** (et rien d'autre) :
1. **L'image du fond** (`SkyBackdrop` : on change la texture de son matériau).
2. **La lumière principale** (couleur, intensité ; sa direction ne change pas).
3. **La lumière d'ambiance** (`RenderSettings.ambientSkyColor`, `ambientEquatorColor`,
   `ambientGroundColor`).
4. **Les nuages 3D qui défilent**, sous l'étage.
Le sol, les coffres, le personnage et leurs couleurs **ne changent pas** : les objets de jeu
restent chauds et lisibles sur tous les fonds (art bible §2.6). Le contre-jour violet
(art bible §5), s'il existe, reste le même.

**Les nuages 3D** (« nuages qui passent sous l'étage »)
- Un groupe = 3 à 5 sphères Unity serrées (Ø 1,2 à 2,5), légèrement aplaties (échelle y × 0,6),
  matériau URP Lit opaque de la couleur du palier, smoothness 0,1, **sans collider**, sans
  ombre portée. Même style « pouf » cartoon que le nuage du piège.
- Placés au hasard (générateur séparé du piège) sur un anneau autour de la tour : distance au
  centre entre 6 et 14 m, à la hauteur du palier (toujours **sous** le dessus du sol, y < −1),
  donc jamais devant les coffres ni le personnage. On les voit dépasser autour du disque de
  l'étage, en bas et sur les côtés de l'écran.
- Ils dérivent lentement : x + 0,25 m/s ; un groupe qui dépasse x = +14 revient à x = −14.
- Pendant la montée, la caméra monte et les nuages restent en place : à l'écran, **ils
  descendent**, ce qui renforce l'impression de monter. Aucun code de plus n'est nécessaire.
- Réserve : 12 groupes créés une fois ; chaque palier en active le nombre voulu.
- Au retour à l'étage 1 (« Rejouer »), retour au palier 1 dans le noir (§3.11).

**Images de fond à produire par le DA (Gemini)** : 3 nouvelles (`ciel_palier2.png`,
`ciel_palier3.png`, `ciel_palier4.png`), même format que `ciel_nuit.png` (16:9,
1376 × 768), règles de l'art bible §9.2 « Fond de ciel » : peu contrastées, centre un peu plus
clair, aucun objet net au centre (les coffres sont devant), pas de texte. Les couleurs dominantes
reprennent la colonne « Ambiance » du tableau. Rangement : `Assets/Art/fonds/`.

**Règles** : le décor est du pur affichage. Il ne change ni les chances, ni les gains, ni la
durée du suspense. Il est recalculé à partir de l'étage (palier = 1 si étage ≤ 3, 2 si ≤ 6,
3 si ≤ 9, sinon 4) : rien à sauvegarder.

---

## 4. Problème connu : les couvercles ouverts cachent le personnage

### 4.1 Le constat (capture `docs/captures/sol_2_resultat.png`)
Ouvert à 110°, le couvercle du coffre du **milieu** monte jusqu'à y ≈ 1,8 m, à z ≈ 0,9.
Depuis la caméra (0 ; 5 ; −7), tout ce qui se trouve derrière, sur le personnage (avant de la
capsule en z = 1,5), est caché **en dessous de y ≈ 1,5 m**. On ne voit que les 50 cm du haut
de la tête ; les yeux affleurent au bord du couvercle. Avec l'écrasement du piège (§3.7), les
yeux disparaîtraient complètement : la réaction serait invisible.

### 4.2 Pistes écartées
| Piste | Pourquoi non |
|---|---|
| Ouvrir moins (60–90°) | Le couvercle reste aussi haut (sa longueur ne change pas) et cache en plus l'intérieur du coffre |
| Ouvrir plus (160–180°) | Le couvercle se couche vers l'arrière et **traverse** le personnage (pointe à z ≈ 1,5) |
| Couvercle qui s'efface (transparence) | Il faut un matériau transparent (tri, ombres, plus complexe) et un coffre sans couvercle a l'air cassé |
| Coffre du milieu plus bas / plus petit | Les trois coffres doivent être identiques (équité visuelle) |

### 4.3 Solution retenue : le personnage recule et monte sur une estrade
- **`Joueur`** passe de (0 ; 0 ; 2) à **(0 ; 0,3 ; 2,8)**.
- **Nouvelle estrade** `Estrade` sous lui : cylindre Unity, diamètre 1,4, hauteur 0,3
  (échelle (1,4 ; 0,15 ; 1,4)), centre en (0 ; 0,15 ; 2,8), matériau pierre claire
  `M_PierreClaire` (`#8A90B0`), **sans collider** (comme le personnage : rien ne doit gêner
  le rayon du clic sur les coffres). C'est un « podium de pion de jeu de
  société », cohérent avec l'art bible. (Un vrai modèle Blender pourra la remplacer.)
- **Résultat calculé** : depuis la caméra, la ligne qui frôle le haut du couvercle ouvert
  touche l'avant du personnage (z = 2,3) à y ≈ 1,2 m. Tout ce qui est au-dessus est visible :
  les yeux (y ≈ 1,9) avec 0,7 m de marge, et encore 0,3 m de marge quand il est écrasé
  (yeux à y ≈ 1,5).
- Les animations du personnage (§3.7) s'appliquent à `Joueur` ; l'estrade ne bouge pas.
- On **garde** la fermeture des couvercles à l'ouverture du shop (§3.13) : le personnage sert
  d'aperçu en entier.
- À vérifier en capture (critère 4) : le personnage reste entièrement dans le cadre, chapeau
  « Couronne » compris, avec le saut de +0,8 m. Si ce n'est pas le cas, réduire le saut de
  l'encaissement à +0,6 m plutôt que de changer la position.

> Documents mis à jour en conséquence : `docs/specs/shop.md` §2.1 (position) et §2.2.

---

## 5. Sons

### 5.1 Fabrication et rangement
- **Synthétisés par script** (aucun enregistrement, aucun son téléchargé, aucune imitation
  d'un jingle connu) : travail du sous-agent `sound-designer`, scripts dans `audio/`.
- Format : WAV 44,1 kHz, 16 bits, **mono** pour les effets, stéréo permis pour la musique.
  Pic à −1 dBFS, fondu de 5 ms au début et à la fin (pas de « clic »).
- Rangement : `TourDeLaChance-Unity/Assets/Audio/<categorie>/<nom>.wav`.
- Réglages d'import Unity : effets en *Decompress On Load* ; musiques en *Streaming*,
  *Load In Background*.
- **Variation** : les sons marqués « varié » sont joués avec une hauteur (pitch) tirée au
  hasard entre 0,95 et 1,05, pour ne pas lasser à force de répétition.

Petit lexique de synthèse : *sinus* = son pur et doux ; *carré* = son « 8 bits », plus
nasillard ; *triangle* = entre les deux ; *bruit* = « chhh » ; *filtre passe-bas* = rend
plus sourd ; *glissando* = la hauteur monte ou descend en continu ; *arpège* = les notes
d'un accord jouées une par une ; *enveloppe* = comment le volume naît et meurt (attaque
rapide = son percussif).

### 5.2 Liste des fichiers

| Nom | Catégorie | Durée | Ce qu'il évoque | Comment le synthétiser | Quand il joue |
|---|---|---|---|---|---|
| `ui_survol` | ui | 0,05 s | Un tic léger, discret | Sinus 1 800 Hz, attaque 2 ms, chute 40 ms. Varié | Survol d'un bouton |
| `ui_clic` | ui | 0,08 s | Un « toc » de bouton en bois | Sinus 900 Hz qui glisse à 600 Hz + petit bruit filtré passe-bas (2 kHz) de 10 ms. Varié | Clic sur un bouton, clic sur un coffre, aperçu shop |
| `ui_jouer` | ui | 0,6 s | « C'est parti ! » joyeux | Arpège montant de 3 notes (do5, mi5, sol5, puis do6 tenu), timbre de boîte à musique (sinus + harmonique ×4 qui meurt vite), écho léger | Bouton « Jouer » |
| `ui_etage_monte` | ui | 1,0 s | On s'envole vers le haut | Glissando de harpe montant (notes rapides de la gamme de fa majeur, 2 octaves en 0,6 s) + souffle de bruit filtré dont le filtre s'ouvre de 300 à 3 000 Hz | Début de la montée automatique après un trésor (§3.11) |
| `ui_nouvelle_partie` | ui | 0,5 s | On redescend, sans tristesse | Glissando descendant court (1 octave en 0,3 s), même timbre de harpe, plus doux | Clic « Rejouer » |
| `ui_boutique_ouvre` | ui | 0,3 s | Un rideau / une clochette d'entrée | Deux notes de clochette montantes (sol5 → do6), sinus + harmonique ×3 | Ouverture du shop |
| `ui_boutique_ferme` | ui | 0,25 s | La même clochette, à l'envers | Deux notes descendantes (do6 → sol5) | Fermeture du shop |
| `coffre_survol` | coffre | 0,12 s | Le coffre frétille, petit grincement de bois | Bruit filtré passe-bande vers 1 200 Hz, 2 petites impulsions rapprochées. Varié | Entrée du survol d'un coffre |
| `coffre_tremble` | coffre | 1,0 s | Le cœur qui bat, le coffre qui claque | Cliquetis de bois : impulsions de bruit filtré (800 Hz) à 12 par seconde, de plus en plus fortes ; dessous, un sinus grave qui monte de 110 à 220 Hz (tension) | Suspense (§3.3), coupé à l'ouverture |
| `coffre_ouvre` | coffre | 0,35 s | Couvercle qui s'ouvre d'un coup | Grincement court (sinus 400 → 700 Hz avec vibrato rapide, 0,15 s) puis « boum » sourd (sinus 120 Hz, chute 0,15 s) | Ouverture de chaque coffre |
| `coffre_ferme` | coffre | 0,2 s | Couvercle qui retombe | « Boum » sourd seul (sinus 140 → 90 Hz) + petit bruit filtré passe-bas | Couvercles refermés (shop) |
| `resultat_tresor` | resultat | 0,9 s | Joie, « Ta-daa ! » scintillant | Arpège majeur rapide en glockenspiel (fa5, la5, do6, fa6, 0,07 s entre les notes) + scintillement (sinus aigus 4–6 kHz au hasard, très courts) | Trésor choisi |
| `resultat_piege` | resultat | 1,0 s | « Boïng… wah-wah » drôle, pas méchant | « Boïng » de ressort (sinus 300 → 150 Hz avec vibrato large 12 Hz, 0,35 s) puis deux notes descendantes de trombone cartoon (onde carrée filtrée, mi4 → ré#4 avec vibrato lent, glissando final vers le bas) | Piège choisi |
| `resultat_encaisse` | resultat | 1,4 s | Petite fanfare de fête foraine, victoire | Orgue de barbarie (onde carrée douce filtrée + trémolo 5 Hz) : 4 accords rapides fa – si♭ – do – fa (le dernier tenu 0,6 s) + scintillement final | Clic « Encaisser » |
| `pieces_jaillissement` | pieces | 0,8 s | Une gerbe de pièces qui tintent | 14 « ting » métalliques (sinus 2–5 kHz + harmonique non entière ×2,7, chute 60 ms) répartis au hasard sur 0,6 s, de plus en plus espacés | Trésor choisi (avec `resultat_tresor`) |
| `pieces_tic` | pieces | 0,04 s | Une pièce qui tombe dans la bourse | Un seul « ting » (sinus 3 200 Hz + ×2,7), chute 30 ms. Varié | Défilement des compteurs, arrivée de chaque pièce volante |
| `pieces_achat` | pieces | 0,5 s | « Kaching » de conte (sans caisse enregistreuse réaliste) | Deux « ting » rapprochés puis une clochette tenue (do6, sinus + ×4) | Achat dans le shop |
| `musique_ambiance` | musique | 48 s, en boucle | Fête foraine la nuit, douce, magique | Voir ci-dessous | Tout le temps (titre, jeu, shop) |
| `musique_tension` | musique | 48 s, en boucle | Le cœur qui s'accélère quand on monte | Voir ci-dessous | Couche ajoutée selon l'étage |

**Ajouts v2 — boîtes à surprises et vente** (fabriqués par le sound designer ; noms de
fichiers imposés, rangés dans `Assets/Audio/boite/` et `Assets/Audio/ui/`). La description
sonore détaillée est celle du sound designer ; l'intention attendue est :

| Nom | Catégorie | Durée visée | Ce qu'il évoque | Quand il joue |
|---|---|---|---|---|
| `boite_secoue` | boite | 1,2 s | La boîte qui gigote, quelque chose bouge dedans ; tension qui monte. **Le même son pour toutes les raretés** | Phase de secousse (`boites.md` §7.6) |
| `boite_ouverture` | boite | 0,3 s | « Pop ! » du couvercle qui saute + souffle | Ouverture de la boîte |
| `revelation_commun` | boite | ~0,5 s | Petit « ding » simple et gentil | Révélation Commun |
| `revelation_peu_commun` | boite | ~0,7 s | Deux notes montantes, un peu plus brillantes | Révélation Peu commun |
| `revelation_rare` | boite | ~1,0 s | Arpège scintillant | Révélation Rare |
| `revelation_super_rare` | boite | ~1,4 s | Arpège + accord tenu + scintillement plus riche | Révélation Super rare |
| `revelation_legendaire` | boite | ~2,0 s | Petite fanfare triomphale de fête foraine | Révélation Légendaire |
| `revelation_secret` | boite | ~2,6 s | Mystérieux puis éclatant : silence/souffle, puis fanfare magique avec carillon | Révélation Secret (commence avec l'écran noir) |
| `vente` | ui | ~0,4 s | Pièces qui tombent dans la bourse, satisfait (différent de `pieces_achat`) | Vente d'un ou de tous les doublons |

Soit **28 fichiers** : 8 `ui`, 4 `coffre`, 3 `resultat`, 3 `pieces`, 2 `musique`, 8 `boite`.
Les durées des révélations suivent celles du tableau de `boites.md` §7.6 : si le sound
designer livre des durées un peu différentes (±20 %), on garde les durées d'animation et on
laisse le son finir.

**`musique_ambiance`** — valse lente de boîte à musique.
- Mesure à 3 temps (valse), **120 battements par minute**, **32 mesures** = exactement 48 s.
- Tonalité **fa majeur**. Grille d'accords (une mesure chacun) sur 8 mesures, répétée avec
  variations : fa – fa – si♭ – fa – do – do – fa – do. Forme A (8) – A' (8) – B (8, sur
  ré mineur – si♭ – sol mineur – do) – A (8).
- **Mélodie** en boîte à musique (sinus + harmonique ×4 qui meurt en 0,1 s, chaque note
  s'éteint en ~0,8 s), entre do5 et do6, simple et chantante.
- **Accompagnement** en orgue de barbarie léger : « boum – tchak – tchak » (basse sur le
  temps 1, accord sur les temps 2 et 3), onde carrée douce filtrée passe-bas à 1 500 Hz,
  trémolo 5 Hz.
- Pas de batterie. Réverbération légère (impression de plein air la nuit).
- **Boucle sans coupure** : la dernière mesure doit enchaîner naturellement sur la première,
  et la queue de réverbération de la fin doit être recopiée (mixée) sur le début du fichier.

**`musique_tension`** — une couche **qui se superpose** à l'ambiance, pas un autre morceau.
- **Même tempo, même durée (48 s), même grille d'accords**, démarrée au même instant que
  l'ambiance : les deux restent calées.
- Contenu : un « boum-boum » de cœur (sinus 60 Hz, chute rapide) sur les temps 1 et 2 de
  chaque mesure ; une basse pizzicato (triangle, notes courtes) sur le temps 1 ; une nappe
  aiguë en trémolo rapide (deux ondes en dents de scie légèrement désaccordées, filtrées)
  qui tient les notes de l'accord.
- Seule, elle doit sonner un peu inquiétante mais jamais effrayante.

### 5.3 Quand et à quel volume (récapitulatif par moment)
| Moment | Sons |
|---|---|
| Écran titre | `musique_ambiance` (fondu d'entrée 1,5 s) ; `ui_survol` / `ui_jouer` |
| Début d'étage, `Choosing` | Survol coffre : `coffre_survol` |
| Clic coffre | `ui_clic` puis `coffre_tremble` |
| Ouverture | `coffre_ouvre` (coffre choisi, volume plein) ; les deux autres : `coffre_ouvre` une seule fois, volume × 0,5, hauteur 1,1 |
| Trésor | `resultat_tresor` + `pieces_jaillissement` ; puis `pieces_tic` (défilement) |
| Piège | `resultat_piege` ; la couche tension s'éteint (1,0 s) |
| Encaisser (pendant le choix, étage 2 et plus) | `ui_clic`, `resultat_encaisse`, `pieces_tic` × nombre de pièces volantes ; la couche tension s'éteint (1,0 s) |
| Montée automatique après un trésor | `ui_etage_monte` ; la couche tension monte au niveau du nouvel étage pendant la transition |
| Rejouer | `ui_clic`, `ui_nouvelle_partie` ; couche tension à 0 |
| Panneau : ouvrir une boîte | `pieces_achat`, puis `boite_secoue`, `boite_ouverture`, `revelation_<rarete>` (avec atténuation de la musique) |
| Panneau : vendre | `vente` |

**Plafond anti-cacophonie** : un même son ne peut pas être relancé moins de 0,03 s après
lui-même (utile pour `pieces_tic`) ; au plus 8 effets en même temps.

---

## 6. Mixage

### 6.1 Volumes relatifs
Volumes de lecture (`AudioSource.volume`, de 0 à 1), avec des fichiers tous normalisés à
−1 dBFS. Principe : la musique en fond, l'interface discrète, les résultats bien devant.

| Groupe | Sons | Volume |
|---|---|---|
| Musique | `musique_ambiance` | **0,35** |
| Musique | `musique_tension` | 0,35 × niveau de l'étage (tableau ci-dessous) |
| Interface | `ui_survol` | 0,25 |
| Interface | `ui_clic`, `ui_boutique_ouvre`, `ui_boutique_ferme` | 0,5 |
| Interface | `ui_jouer`, `ui_etage_monte`, `ui_nouvelle_partie` | 0,6 |
| Coffres | `coffre_survol` | 0,4 |
| Coffres | `coffre_tremble`, `coffre_ouvre`, `coffre_ferme` | 0,7 |
| Résultats | `resultat_tresor`, `resultat_piege`, `resultat_encaisse` | **0,9** |
| Pièces | `pieces_jaillissement`, `pieces_achat` | 0,7 |
| Pièces | `pieces_tic` | 0,35 |
| Boîtes | `boite_secoue` | 0,7 |
| Boîtes | `boite_ouverture` | 0,8 |
| Boîtes | `revelation_*` (les 6) | **0,9** |
| Interface | `vente` | 0,6 |

**Niveau de la couche tension selon l'étage** (changement en fondu de 1,0 s) :

| Situation | Titre, étage 1, `Lost`, `CashedOut` | Étage 2 | Étage 3 | Étage 4 | Étage 5 et plus |
|---|---|---|---|---|---|
| Niveau | 0 | 0,25 | 0,45 | 0,65 | 0,80 |

**Atténuation de la musique (« ducking »)** : quand un son `resultat_*` ou `revelation_*` joue, les deux
musiques descendent à **× 0,5** en 0,05 s, puis remontent en 0,5 s après la fin du son.
Ça laisse la place au jingle sans couper l'ambiance.

### 6.2 Couper la musique et les effets
- Deux petits boutons texte **sous « Étage N »** (en haut à gauche) : **« Musique : oui »** et
  **« Effets : oui »**. Un clic bascule sur « … : non » (et le bouton passe à opacité 0,5).
  Taille du texte 16, fond `#2A2A3A` à 80 %, coins arrondis, même style de survol que §3.10.
- **Visibles partout** : écran titre, partie, shop ouvert. (Le HUD est caché sur l'écran
  titre : il faut donc les afficher aussi par-dessus l'écran titre, par exemple dans
  `Title.uxml`, branchés sur la même logique.)
- « Musique : non » coupe **les deux** musiques en les rendant muettes (`AudioSource.mute`),
  **sans les arrêter** : elles continuent de tourner en silence et restent calées entre elles.
- « Effets : non » coupe tous les sons `ui`, `coffre`, `resultat`, `pieces`, `boite`. On entend tout de
  même le `ui_clic` du bouton « Effets : oui » quand on **réactive** les effets (pour
  confirmer que ça remarche).
- Le choix est **mémorisé** (§7) et appliqué dès le lancement, avant le premier son.
- Ces réglages ne touchent à rien d'autre (règles, gains, piège).

### 6.3 Répartition dans le code (suggestion)
- Une classe **statique** de réglages, sur le modèle de `Wallet`, qui lit/écrit les
  PlayerPrefs. **Attention au nom** : `AudioSettings` existe déjà dans Unity
  (`UnityEngine.AudioSettings`) ; l'appeler par exemple `SoundSettings`.
- Un MonoBehaviour pour les effets (une petite réserve de 8 `AudioSource`) et un pour la
  musique (2 `AudioSource` démarrées au même instant avec `PlayScheduled`).
- Un MonoBehaviour par animateur : coffre (dans `Chest` ou à côté), personnage (sur `Joueur`,
  il garde la pose de repos), pièces, nuage, caméra (secousse + transitions).
- Un « chef d'orchestre de l'affichage » qui écoute les événements du `GameManager` et du
  `ChestTrial` et déclenche animations et sons. Il faudra probablement ajouter à
  `ChestTrial` des événements d'affichage : survol changé (index ou −1), suspense commencé
  (index, durée), coffre révélé (index, piégé ou non, choisi ou non).

---

## 7. Données à sauvegarder (PlayerPrefs)

| Clé | Type | Contenu | Défaut |
|---|---|---|---|
| `son_musique` | int | 1 = musique active, 0 = coupée | 1 |
| `son_effets` | int | 1 = effets actifs, 0 = coupés | 1 |

Sauvegarde (`PlayerPrefs.Save()`) à chaque clic sur l'un des deux boutons. Rien d'autre à
sauvegarder : les animations n'ont pas d'état durable. Pour remettre à zéro après des
tests : supprimer ces 2 clés.

---

## 8. Critères d'acceptation

**Équité (principe n°1)**
1. Sur 20 étages joués, la durée entre le clic et l'ouverture du couvercle est la même pour un
   trésor et pour un piège au même étage (à 1 image près). Le tremblement et `coffre_tremble`
   sont identiques dans les deux cas.
2. Avec la musique et les effets coupés ou non, le jeu reste à 1 coffre piégé sur 3 et
   gain = 10 × étage.
3. Couper/rétablir le son ou survoler les coffres ne change pas le coffre piégé de l'étage
   (vérifiable en lisant `trapIndex` dans l'éditeur avant et après).

**Problème du couvercle**
4. Les trois couvercles ouverts (écran `Lost`, ou juste après un trésor avant la montée), les **deux yeux** du personnage
   sont entièrement visibles en capture 1280 × 720, y compris pendant l'écrasement du piège.
   Le personnage et le chapeau « Couronne » restent dans le cadre pendant le saut de
   l'encaissement.
5. Aucun couvercle ne traverse le personnage ni l'estrade.

**Coffres**
6. En `Choosing`, survoler un coffre le grossit (1,06) et entrouvre son couvercle ; le
   quitter le remet exactement au repos. `coffre_survol` joue une fois par entrée.
7. Aucun survol ni clic n'a d'effet en `Revealing`, `Climbing`, `Lost`, `CashedOut`, shop
   ouvert, pendant une transition ou pendant les 0,5 s de verrou anti-clic.
8. Le suspense dure 0,6 s à l'étage 1, 0,76 s à l'étage 3, 1,0 s à l'étage 6 et à l'étage 9.
9. Le couvercle finit à 110° après être passé par 120° puis 104°.
10. Les deux coffres non choisis s'ouvrent après le coffre choisi, et `coffre_ouvre` ne joue
    qu'une fois pour les deux.

**Personnage**
11. Au repos, le personnage respire ; après 50 parties, sa position, sa rotation et son
    échelle de repos sont exactement celles du départ (pas de dérive).
12. Trésor choisi : il saute (+0,6 m) ; piège choisi : il s'écrase puis garde la tête basse
    jusqu'à « Rejouer » ou l'ouverture du shop.
13. Encaisser : deux sauts dont un avec un tour complet.
14. Le chapeau suit toutes les animations sans se décoller de la tête.

**Pièces et compteurs**
15. Trésor à l'étage 1 : 10 pièces jaillissent ; à l'étage 5 : 18 ; à l'étage 20 : 30 (plafond).
    Aucune pièce ne reste dans la scène après 1,5 s.
16. Les pièces et le nuage n'ont pas de collider (un clic dessus ne fait rien, ils ne bloquent
    pas le rayon des coffres).
17. Après un gain, la cagnotte affichée défile jusqu'à la nouvelle valeur en 0,5 s et finit
    **exactement** sur la valeur du `GameManager`.
18. Après un piège, la cagnotte affichée descend à 0 en rouge puis reprend sa couleur normale.
19. Encaisser 60 pièces : 6 pièces volent de la cagnotte au porte-monnaie ; à la fin, la
    cagnotte affiche 0 et le porte-monnaie la bonne somme (identique à `Wallet.Coins`).
20. Le texte « +20 » apparaît au-dessus du coffre choisi, monte et disparaît en 0,9 s.

**Boutons et transitions**
21. Sur les écrans Perdu / Encaissé, les boutons apparaissent 0,3 s après le résultat, l'un
    après l'autre ; un clic pendant leur apparition ne fait rien.
22. Il n'existe plus de bouton « Continuer » ni d'état `Decision`.
23. Après un trésor, sans aucun clic : 0,6 s plus tard la caméra monte, l'écran s'assombrit, le
    compteur « Étage » change pendant le noir, la bannière « Étage N » s'affiche, et après la
    transition la caméra est exactement en (0 ; 5 ; −7) avec sa rotation d'origine.
24. Appuyer sur 1, 2 ou 3 (ou cliquer un coffre) pendant la transition et jusqu'à 0,5 s après
    sa fin n'ouvre aucun coffre, et rien ne se déclenche ensuite (pas de clic « en attente ») ;
    0,6 s après la fin, oui.
25. « Rejouer » : transition descendante, étage 1, couvercles fermés, personnage au repos.
26. Écran titre : le logo flotte, « Jouer » respire ; après « Jouer », les coffres ne sont
    cliquables qu'une fois le fondu terminé.

**Sons et mixage**
27. Les 28 fichiers de §5.2 existent dans `Assets/Audio/<categorie>/`, aux durées indiquées
    (±20 %), sans saturation. `boite_secoue` est joué à l'identique quelle que soit la rareté
    tirée ; chaque rareté joue son `revelation_<rarete>` ; vendre un doublon joue `vente`.
28. La musique boucle sans coupure audible ni « clic » au raccord.
29. À l'étage 1 on n'entend que l'ambiance ; à l'étage 3 la couche tension est présente ; elle
    s'éteint en 1 s sur l'écran Perdu ou Encaissé.
30. Pendant `resultat_tresor`, la musique baisse puis revient.
31. « Musique : non » coupe la musique ; la rétablir 10 s plus tard la fait revenir **calée**
    (ambiance et tension ensemble). « Effets : non » coupe tous les autres sons.
32. Je coupe la musique, je quitte le mode Play et je relance : la musique est toujours
    coupée et le bouton affiche « Musique : non ». Idem pour les effets.
33. Les deux boutons de son sont visibles et utilisables sur l'écran titre, en jeu et shop ouvert.
34. Pas d'erreur ni d'avertissement dans la console pendant 10 parties enchaînées.

**Nouveau flux et « Encaisser » (v2)**
35. À l'étage 1, le bouton « Encaisser » n'apparaît jamais.
36. À l'étage 3 après deux trésors, « Encaisser 30 » est visible pendant le choix ; il n'est
    cliquable qu'à partir de 0,5 s après la fin de la montée.
37. Clic sur « Encaisser 30 » : le porte-monnaie gagne exactement 30, la cagnotte passe à 0,
    l'écran Encaissé s'affiche, les coffres restent fermés et le piège n'est pas montré.
38. Dès que je clique un coffre, « Encaisser » disparaît ; je ne peux pas encaisser pendant le
    suspense.
39. Un double-clic sur « Encaisser » ne crédite qu'une fois.

**Décor selon l'altitude (v2)**
40. Étages 1-3 : fond de nuit actuel, pas de nuages 3D. En arrivant à l'étage 4, la bannière
    affiche « Étage 4 » et « Dans les nuages ! », le fond, la lumière et l'ambiance sont ceux
    du palier 2 et 6 groupes de nuages dérivent sous l'étage. Idem aux étages 7 (palier 3) et
    10 (palier 4).
41. Aucun nuage ne passe devant les coffres ni le personnage, et aucun n'a de collider.
42. Le changement de décor n'est jamais visible en direct : il a lieu pendant l'écran sombre.
    « Rejouer » depuis l'étage 8 ramène au décor du palier 1.
43. Pour tester sans dépendre de la chance : une commande `script eval` qui fixe l'étage
    (ex. 9) puis déclenche une montée permet de vérifier chaque palier en capture.

---

## 9. Hors périmètre (pas dans cette première passe)

- Les animations et sons des autres épreuves (dé, roue, pile ou face) : ils viendront avec
  ces épreuves, en réutilisant les mêmes briques (suspense, révélation, réactions).
- **Confettis, feux d'artifice et effets lumineux (émission)** : ils sont réservés à la
  future catégorie du shop « Effets de victoire » (GDD §6). Le jaillissement de pièces est
  l'effet de base, gratuit pour tous. (Exceptions v2 : l'émission des cosmétiques Légendaire
  et Secret, et les étincelles UI de l'animation d'ouverture des boîtes, voir `boites.md`.)
- Les paliers de sécurité (tous les 5 étages) et leur fête spéciale.
- Pour le décor par altitude : étoiles filantes, oiseaux, météo animée, un modèle 3D de la tour
  qui s'allonge sous l'étage, une musique différente par palier, des fondus visibles entre
  deux décors (le changement se fait dans le noir).
- Montrer où était le piège quand on encaisse (on garde la surprise et on évite les regrets
  « j'aurais pu continuer »).
- Un raccourci clavier pour « Encaisser ».
- Des curseurs de volume, un menu d'options complet, un mode « réduire les animations »,
  un bouton pour passer les animations.
- Des sons enregistrés, des voix, une musique différente par étage ou un changement de
  tempo (la couche tension suffit).
- Les systèmes de particules Unity (Particle System, VFX Graph) : les pièces et le nuage sont
  de simples objets réutilisés, plus faciles à comprendre pour débuter.
- Un vrai modèle 3D de pièce, de ressort-piège ou d'estrade (primitives Unity pour l'instant).
- Un *AudioMixer* Unity : les volumes sont réglés directement sur les `AudioSource`.
- Les vibrations de manette et toute version mobile.
