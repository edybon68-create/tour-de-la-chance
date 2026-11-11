# Spécification — Les boîtes à surprises, l'inventaire et l'index (shop v2)

> **Remplace `docs/specs/shop.md` (shop v1, achat direct).** Ce qui reste valable de la v1 :
> la construction du personnage (§2 de shop.md, complétée ici au §5.4) et la construction en
> primitives des 4 chapeaux et des 5 couleurs existants (§4.1 et §4.2 de shop.md).
> Tout le reste (achat direct, prix par objet, panneau en bas d'écran) est remplacé.
>
> Cette spec décrit **quoi** faire et **avec quels chiffres**, pas **comment** l'écrire en C#.
> Elle s'appuie sur le code existant : `GameManager`, `Wallet`, `Shop`, `ShopCatalog`,
> `ShopItem`, `ShopCategory`, `PlayerAvatar`, `HudController`, `ShopController`.
> Le nouveau flux d'étage (plus de bouton « Continuer ») est décrit dans
> `docs/specs/sons-animations.md` §2 ; les sons des boîtes dans le même fichier, §5.
>
> **Mise à jour v3 (`docs/specs/v3.md`) — ce qui change ici :**
> - prix des boîtes **250** (au lieu de 100) et revente **25 / 40 / 60 / 100 / 150 / 200**
>   (§5.1, §5.2 corrigés ; l'équilibrage du §6 est remplacé par `v3.md` §6) ;
> - le panneau à trois onglets devient **4 panneaux séparés** ouverts depuis une colonne de
>   boutons à gauche, et la règle d'accès du §4.1 est remplacée (`v3.md` §4) ;
> - la révélation du **Secret** devient une cinématique de 5,4 s (`v3.md` §8.3) ;
> - les identifiants inconnus sont **conservés** dans la sauvegarde (§4.6, correctif B6) ;
> - le personnage est un **cheval** : positions et échelles des points d'accroche dans `v3.md` §7.
> Dans les exemples et critères ci-dessous, lire « 250 » partout où il est écrit « Ouvrir — 100 ».
>
> **Mise à jour v4 (`docs/specs/v4.md`) — ce qui change ici :**
> - **noms affichés simplifiés** (§5.3, §5.4, §5.5) : liste complète ancien → nouveau dans
>   `v4.md` §8 (ex. « Rouge tomate » → « Rouge », « Arc-en-ciel néon » → « Arc-en-ciel »,
>   « Couronne royale » → « Couronne ») ; identifiants inchangés ;
> - règle d'or n°2 (§2) : les cosmétiques équipés donnent un **bonus de pièces sur les
>   trésors** (bonus de tenue, `v4.md` §9), jamais sur les chances ;
> - **Galaxie** (§5.3 n°16) : nébuleuse animée calculée par le code, `v4.md` §7 ;
> - les panneaux deviennent des **pop-ups centrées** (`v4.md` §5.6).

---

## 1. Objectif pour le joueur

Chaque pièce encaissée rapproche d'une **ouverture de boîte**, et chaque ouverture est un petit
moment de suspense : « Est-ce que ce sera enfin le Légendaire ? ». L'index montre les cases
vides de la collection : on a toujours un objectif à viser, et une bonne raison de choisir
entre encaisser maintenant ou risquer un étage de plus.

---

## 2. Règle d'or et garde-fous (à ne jamais enfreindre)

1. **Pièces virtuelles uniquement**, gagnées en jouant. Aucun achat en argent réel, aucune
   pièce à acheter, aucun bouton qui mène vers un paiement.
2. **Les cosmétiques ne changent jamais les chances**, ni le piège. (v4 : équipés, ils
   donnent un bonus de pièces sur les trésors, plafonné à +50 % ; `v4.md` §2 et §9.)
3. **Les taux affichés sont exactement ceux du code** : l'interface ne contient aucun
   pourcentage écrit « en dur » ; elle lit les chiffres dans le catalogue, là où le tirage
   les lit aussi (une seule source de vérité).
4. **Pas de pression** : pas de compte à rebours, pas d'« offre limitée », pas de prix barré,
   pas de faux « presque gagné » (l'animation n'imite jamais une rareté qu'on n'a pas eue).
5. **Le résultat est tiré et sauvegardé avant l'animation**. L'animation de suspense
   (secousse de la boîte) est **identique** quel que soit le résultat : on ne peut pas deviner
   la rareté avant l'ouverture.
6. **On ne perd jamais un objet** : on ne peut vendre que les exemplaires en trop (doublons),
   jamais le dernier exemplaire.

---

## 3. Petit vocabulaire

| Mot | Sens |
|---|---|
| **Boîte** | Un paquet qu'on ouvre contre des pièces ; il donne **un** objet tiré au hasard dans sa liste. |
| **Rareté** | Le « niveau » d'un objet : Commun, Peu commun, Rare, Super rare, Légendaire, Secret. Plus c'est rare, moins on a de chances de le tirer. |
| **Taux de drop** | La chance (en %) de tirer une rareté à chaque ouverture. (*Drop* = « ce qui tombe ».) |
| **Doublon** | Un exemplaire en plus d'un objet qu'on possède déjà. |
| **Skin** | L'apparence du corps du personnage : couleur ou matière. |
| **Accessoire** | Un objet porté (chapeau, lunettes, ailes…), placé sur un **emplacement**. |
| **Emplacement** | Endroit du corps où se porte un accessoire : Tête, Visage, Cou, Dos. |

---

## 4. Règles

### 4.1 Quand peut-on ouvrir le panneau ?
> **Remplacé en v3** par `docs/specs/v3.md` §4.2 : les panneaux s'ouvrent depuis la colonne de
> boutons dès l'écran titre, pendant le choix d'un coffre à **tous** les étages, et sur les
> écrans Perdu / Encaissé ; jamais pendant le suspense, la révélation ni une transition. Le
> bouton « Boutique » du HUD disparaît. Le texte ci-dessous décrit la règle v2, pour mémoire.

Inchangé par rapport à la v1, adapté au nouveau flux : le bouton **« Boutique »** du HUD est
visible seulement dans ces trois situations.

| Situation | Condition exacte |
|---|---|
| Début de partie | `Floor == 1`, `State == Choosing`, aucun verrou de transition actif |
| Écran « Perdu » | `State == Lost` |
| Écran « Encaissé » | `State == CashedOut` |

Jamais pendant une partie en cours : pas aux étages 2 et plus (même en `Choosing`, quand le
bouton « Encaisser » est proposé), jamais en `Revealing` ni pendant la montée (`Climbing`).

**Pourquoi on garde cette règle avec le nouveau flux ?** À partir de l'étage 2, le moment du
choix du coffre est LE moment de décision (encaisser ou risquer). Y glisser une boutique
mélangerait la cagnotte (en jeu) et le porte-monnaie (à l'abri) et casserait la tension.
Entre deux parties, la cagnotte vaut toujours 0 : aucune confusion possible.

Le panneau reste **un panneau par-dessus l'état courant**, pas un état de partie : en le
fermant, on revient au même écran (même étage, même piège). Pendant qu'il est ouvert :
- les coffres ne réagissent pas (clic, touches 1/2/3, survol) — blocage `ShopOpen`, déjà en place ;
- les boutons du HUD (Encaisser, Rejouer, Boutique) et le message central sont cachés ;
- la barre du haut (étage, cagnotte, porte-monnaie) reste visible et se met à jour tout de suite ;
- **Échap** ferme le panneau (sauf pendant une ouverture de boîte, voir §7.6).

### 4.2 Ouvrir une boîte
Une seule action, dans cet ordre, **en une fois** dans le script de règles (pas dans l'UI) :
1. Vérifier `Wallet.Coins >= prix de la boîte`. Sinon : refuser, ne rien changer.
2. Retirer le prix du porte-monnaie (`Wallet` doit refuser toute dépense qui rendrait le solde
   négatif — méthode du type `TrySpend`).
3. **Tirer la rareté** selon les taux du §5.1 : nombre entier au hasard entre 0 et 999
   compris, comparé aux poids cumulés (500, 750, 890, 960, 990, 1000).
4. **Tirer l'objet** : au hasard, avec la même chance pour chaque objet de cette rareté
   **dans cette boîte**.
5. Ajouter 1 exemplaire de l'objet à la collection (compteur +1).
6. Sauvegarder immédiatement (`PlayerPrefs.Save()`).
7. Renvoyer le résultat (objet, rareté, nouveau ou doublon, nombre d'exemplaires) à
   l'interface, qui lance l'animation (§7.6).

Précisions :
- On paie **uniquement avec le porte-monnaie**, jamais avec la cagnotte (qui vaut 0 aux moments
  où le panneau est ouvrable).
- Le tirage utilise son **propre générateur** (`System.Random` créé une fois), séparé de celui
  du piège et de celui des effets visuels.
- **Chaque ouverture est indépendante** : pas de « garantie » ni de compteur caché qui change
  les chances. (Une garantie est hors périmètre, §11.)
- Ouvrir une boîte **n'équipe pas** l'objet ; l'écran de résultat propose « Équiper ».
- On peut ouvrir une boîte même si la collection est complète (tout sera doublon) ; la carte de
  la boîte le signale (§7.3).

### 4.3 Taux de drop
Les mêmes six raretés et les mêmes taux pour **toutes** les boîtes (plus simple à retenir,
plus simple à afficher). Chaque boîte a **16 objets** : 5 Communs, 4 Peu communs, 3 Rares,
2 Super rares, 1 Légendaire, 1 Secret. Voir le tableau du §5.1.

Ces nombres d'objets font que **chaque objet est moins probable que tous les objets de la
rareté inférieure** (10 % > 6,25 % > 4,67 % > 3,5 % > 3 % > 1 %) : la rareté veut vraiment
dire quelque chose.

Contrôle du catalogue au démarrage : si une boîte n'a pas au moins 1 objet dans chaque rareté,
ou si la somme des poids n'est pas 1000, afficher une erreur dans la console
(`Debug.LogError`) — c'est une erreur de données, pas un cas de jeu.

### 4.4 Doublons et vente
- La collection retient un **nombre d'exemplaires** par objet (0 = non obtenu).
- Les exemplaires au-delà du premier sont des **doublons** ; ils sont vendables.
- Vendre 1 doublon : nombre d'exemplaires −1, porte-monnaie + **valeur de vente de la rareté**
  (§5.1), sauvegarde immédiate. Refusé si l'objet a 1 exemplaire ou moins.
- **« Vendre tous les doublons »** (onglet Inventaire) : ramène chaque objet à 1 exemplaire et
  crédite la somme totale. Pas de fenêtre de confirmation : aucun objet n'est perdu.
- La valeur de vente est **toujours inférieure au prix d'une boîte** (même un Secret en double).
- On peut vendre un doublon d'un objet équipé : il en reste 1, il reste équipé.
- La vente est possible depuis l'écran de résultat d'une ouverture et depuis l'Inventaire.

### 4.5 Équiper
- On n'équipe qu'un objet possédé (au moins 1 exemplaire) ou un objet gratuit (§5.5).
- **Skin** : un seul à la fois (il remplace le précédent).
- **Accessoires : un par emplacement.** On peut donc porter en même temps un chapeau (Tête),
  des lunettes (Visage), un nœud papillon (Cou) et des ailes (Dos). Équiper un accessoire
  remplace celui du **même** emplacement seulement.
  *Pourquoi ce choix* : c'est à peine plus compliqué qu'un seul accessoire (4 cases au lieu
  d'une), et c'est beaucoup plus amusant de composer son personnage.
- Chaque emplacement a un objet gratuit « Aucun » : l'équiper = retirer l'accessoire.
- Équiper est gratuit, illimité, et sauvegardé immédiatement.

### 4.6 Cas limites
| Cas | Comportement attendu |
|---|---|
| Pas assez de pièces (ex. 90 pour une boîte à 100) | Bouton « Ouvrir — 100 » grisé + « Il te manque 10 pièces » |
| Solde exactement égal au prix | Ouverture autorisée, solde = 0 |
| Double-clic rapide sur « Ouvrir » | Une seule ouverture : l'écran d'animation recouvre tout dès le 1er clic, et le script de règles refuse si le solde ne suffit plus |
| Le jeu est quitté pendant l'animation | L'objet est déjà dans la collection et les pièces déjà débitées (tout a été sauvegardé à l'étape 6) |
| Objet tiré déjà possédé | Badge « Doublon » + bouton « Vendre (+X) » sur l'écran de résultat |
| Objet tiré nouveau | Badge « Nouveau ! » |
| Tenter de vendre le dernier exemplaire | Impossible : bouton « Vendre » absent ou grisé |
| Collection d'une boîte complète | La boîte reste ouvrable ; sa carte affiche « Collection complète ! Chaque ouverture donnera un doublon. » |
| Identifiant inconnu dans la sauvegarde | Ignoré pour le jeu (non affiché, non compté, non vendable) **mais conservé tel quel dans la sauvegarde** à chaque écriture (v3, correctif B6) ; s'il était équipé, retour à l'objet gratuit de la catégorie / de l'emplacement |
| Catalogue introuvable au chargement | Aucune migration, aucune écriture, aucune suppression de clé ; on réessaie au prochain accès (v3, correctif B5) |
| Nombre d'exemplaires illisible, nul ou négatif dans la sauvegarde | Ignoré (l'objet compte comme non obtenu) |
| Accessoire sauvegardé sur le mauvais emplacement | Ignoré, retour à « Aucun » pour cet emplacement |
| Première partie (aucune sauvegarde) | Skin `couleur_bleu`, les 4 emplacements à « Aucun », collection vide |
| Ancienne sauvegarde v1 (`shop_owned`) | Migrée : chaque objet acheté = 1 exemplaire (§9.2) ; les objets équipés restent équipés |

---

## 5. Contenu

### 5.1 Les raretés

| Identifiant | Nom affiché | Couleur | Taux de drop | Poids (sur 1000) | Objets par boîte | Chance par objet | Valeur de vente d'un doublon |
|---|---|---|---|---|---|---|---|
| `commun` | Commun | gris `#9AA0B0` | **50 %** | 500 | 5 | 10 % | **25** pièces (v2 : 10) |
| `peu_commun` | Peu commun | vert `#4CC26A` | **25 %** | 250 | 4 | 6,25 % | **40** (v2 : 15) |
| `rare` | Rare | bleu `#3B8BEB` | **14 %** | 140 | 3 | 4,67 % | **60** (v2 : 25) |
| `super_rare` | Super rare | violet `#A35CE0` | **7 %** | 70 | 2 | 3,5 % | **100** (v2 : 40) |
| `legendaire` | Légendaire | orange doré `#FF9F1C` | **3 %** | 30 | 1 | 3 % | **150** (v2 : 60) |
| `secret` | Secret | rose magique `#FF4FD8` (+ arc-en-ciel animé à la révélation) | **1 %** | 10 | 1 | 1 % | **200** (v2 : 80) |

- Somme des poids : 1000 (= 100 %).
- « Chance par objet » = taux de la rareté ÷ nombre d'objets de cette rareté dans la boîte. Elle
  est **calculée** par l'interface à partir du catalogue (affichée avec 2 décimales au plus,
  sans zéros inutiles : « 10 % », « 6,25 % », « 4,67 % »).
- La couleur de rareté n'est jamais le seul indice : le **nom** de la rareté est toujours
  écrit à côté (lisible aussi pour les daltoniens).
- Le vert « Peu commun » `#4CC26A` est volontairement différent du vert « Encaisser / Acheter »
  `#2E9E5B` de l'interface.

### 5.2 Les boîtes

| Identifiant | Nom affiché | Prix | Contenu | Image (à produire) |
|---|---|---|---|---|
| `boite_skins_1` | Boîte de skins n°1 | **250 pièces** (v3 ; v2 : 100) | 16 skins (§5.3) | `boite_skins_1.png` |
| `boite_accessoires_1` | Boîte d'accessoires n°1 | **250 pièces** (v3 ; v2 : 100) | 16 accessoires (§5.4) | `boite_accessoires_1.png` |

Chaque objet appartient à **une seule** boîte. Dans l'index, les objets sont numérotés
n°01 à n°16 dans l'ordre des tableaux ci-dessous (du Commun au Secret).

### 5.3 Boîte de skins n°1 (catégorie `skin`)

Le skin s'applique au `Corps` (la capsule) seulement : les yeux restent noirs `#111111`, les
accessoires gardent leurs couleurs. « Smooth. » = smoothness du shader URP Lit ; « Métal » =
metallic. Les 4 objets marqués ★ existent déjà (v1) : on garde **leur identifiant** (pour ne
pas casser les sauvegardes) et leur rendu.

| N° | Identifiant | Nom affiché | Rareté | Rendu (pour le programmeur et l'artiste) |
|---|---|---|---|---|
| 01 | `couleur_rouge` ★ | Rouge tomate | Commun | `#D64545`, mat (smooth. 0,3) |
| 02 | `couleur_vert` ★ | Vert menthe | Commun | `#3CC48A`, mat (0,3) |
| 03 | `couleur_jaune` | Jaune citron | Commun | `#F2D03B`, mat (0,3) |
| 04 | `couleur_orange` | Orange mandarine | Commun | `#F28C38`, mat (0,3) |
| 05 | `couleur_rose` | Rose bonbon | Commun | `#F27BB0`, mat (0,3) |
| 06 | `couleur_violet` ★ | Violet magique | Peu commun | `#8E5BD6`, brillant (0,6) |
| 07 | `couleur_turquoise` | Turquoise lagon | Peu commun | `#2BB8C4`, brillant (0,6) |
| 08 | `couleur_caramel` | Caramel | Peu commun | `#C98A4B`, très brillant comme un bonbon (0,7) |
| 09 | `couleur_nacre` | Blanc nacré | Peu commun | `#EDE6F2`, métal 0,2, smooth. 0,75 (reflets doux de perle) |
| 10 | `motif_sucre_orge` | Sucre d'orge | Rare | **Rayures horizontales** rouge `#D64545` / blanc cassé `#F4EEE6`, 8 bandes sur la hauteur de la capsule, smooth. 0,5 |
| 11 | `motif_pois` | Pois de fête | Rare | Fond rose bonbon `#F27BB0`, **pois** blanc cassé `#F4EEE6` d'environ 12 cm de diamètre, en quinconce, smooth. 0,5 |
| 12 | `matiere_argent` | Argent poli | Rare | `#C9CED8`, métal 1, smooth. 0,85 (miroir doux) |
| 13 | `couleur_or` ★ | Or massif | Super rare | `#FFC83A`, métal 1, smooth. 0,8 |
| 14 | `matiere_cristal` | Cristal | Super rare | **Transparent** : `#9FE3FF` avec opacité 0,55, smooth. 0,95, métal 0. On voit les yeux « à travers » le corps : c'est voulu |
| 15 | `matiere_arc_en_ciel` | Arc-en-ciel néon | **Légendaire** | **Couleur qui change** en continu : teinte qui fait le tour du cercle des couleurs en 6 s (saturation 0,65, luminosité 1). **Émission** (lumière propre) de la même couleur, intensité 0,8 → il brille dans le noir. Smooth. 0,6 |
| 16 | `matiere_galaxie` | Galaxie | **Secret** | Fond bleu nuit violet `#2E2366`, semé de petites **étoiles** `#FFF1D6` (points de 2 à 5 cm, placés au hasard mais toujours les mêmes) qui **émettent** de la lumière et **scintillent** (intensité d'émission qui oscille entre 0,6 et 1,2, période 2 s). Smooth. 0,8 |

**Notes de réalisation (pour le programmeur)**
- **Motifs (n°10, 11, 16)** : une petite image (texture) **fabriquée par le code** au démarrage
  (`Texture2D` de 64 × 64 pixels) et répétée sur la capsule (*tiling*). Rayures : 2 bandes
  par tuile, tuile répétée 4 fois en hauteur. Pois : 1 pois par tuile, répétition à régler en
  capture pour obtenir des pois d'environ 12 cm. Galaxie : texture noire avec 6 à 8 points
  blancs placés par un générateur à graine fixe (même ciel à chaque lancement), utilisée comme
  carte d'émission (`_EmissionMap`) sur fond `#2E2366`.
- **Transparence (n°14)** : URP Lit en *Surface Type* « Transparent » (`_Surface` = 1,
  `_Blend` = 0 (Alpha), `_SrcBlend` = 5, `_DstBlend` = 10, `_ZWrite` = 0, mot-clé
  `_SURFACE_TYPE_TRANSPARENT`, `renderQueue` = 3000). Couleur de base avec alpha 0,55.
- **Émission (n°15, 16)** : mot-clé `_EMISSION` et `_EmissionColor` ; avec le Bloom du
  post-traitement (seuil 1,0), une intensité de 1,5 × la couleur donne une lueur douce.
- **Animation de couleur (n°15, 16)** : faite par le script du personnage, chaque image ; elle
  continue pendant toute la partie (y compris pendant les épreuves). C'est du pur affichage.
- Au changement de skin, toujours remettre à zéro ce que le skin précédent avait activé
  (transparence, émission, texture), pour qu'il ne « bave » pas sur le suivant.

### 5.4 Boîte d'accessoires n°1 (catégorie `accessoire`)

#### Emplacements et points d'accroche
Nouveaux objets vides, enfants de `Joueur` (positions **locales à `Joueur`**, origine à la
base de la capsule ; l'avant est vers z négatif). `PointChapeau` existe déjà.

| Emplacement | Identifiant | Point d'accroche | Position locale | Ce qu'on y met |
|---|---|---|---|---|
| Tête | `tete` | `PointChapeau` (existant) | (0 ; 2,0 ; 0) — sommet de la tête | chapeaux, couronne, auréole |
| Visage | `visage` | `PointVisage` (nouveau) | (0 ; 1,6 ; −0,49) — devant, à hauteur des yeux | lunettes, masque |
| Cou | `cou` | `PointCou` (nouveau) | (0 ; 1,3 ; 0) — centre du corps à hauteur du « col » | nœud, écharpe, collier |
| Dos | `dos` | `PointDos` (nouveau) | (0 ; 1,3 ; 0,5) — milieu du dos | ailes, et ce qui flotte autour du personnage |

Repère de visibilité (voir `sons-animations.md` §4) : depuis la caméra, même avec les couvercles
ouverts, tout ce qui est au-dessus de y local ≈ 0,9 est visible. Les 4 points d'accroche sont
bien au-dessus. Comme on ne voit jamais l'arrière du personnage, **les accessoires de dos
doivent dépasser sur les côtés** pour être vus.

#### Le catalogue
Les 4 objets marqués ★ existent déjà (v1) : identifiant, construction et couleurs inchangés
(voir `shop.md` §4.2 et `art-bible.md` §6.4 à 6.7).

| N° | Identifiant | Nom affiché | Rareté | Emplacement | Description courte |
|---|---|---|---|---|---|
| 01 | `chapeau_casquette` ★ | Casquette | Commun | Tête | Casquette rouge, visière vers l'avant |
| 02 | `chapeau_fete` ★ | Chapeau de fête | Commun | Tête | Cône rayé rose et jaune, pompon, penché |
| 03 | `chapeau_bonnet` | Bonnet de laine | Commun | Tête | Bonnet turquoise à revers et gros pompon |
| 04 | `lunettes_soleil` | Lunettes de soleil | Commun | Visage | Grosses lunettes noires brillantes, air « cool » |
| 05 | `noeud_papillon` | Nœud papillon | Commun | Cou | Nœud rouge satiné sous le menton |
| 06 | `chapeau_haut_de_forme` ★ | Haut-de-forme | Peu commun | Tête | Chapeau de magicien noir, ruban rouge |
| 07 | `lunettes_coeur` | Lunettes cœur | Peu commun | Visage | Verres en forme de cœur, roses |
| 08 | `echarpe` | Écharpe rayée | Peu commun | Cou | Écharpe vert menthe / blanc, un pan qui pend |
| 09 | `collier_fleurs` | Collier de fleurs | Peu commun | Cou | Couronne de fleurs roses, jaunes et blanches autour du cou |
| 10 | `chapeau_sorcier` | Chapeau de sorcier | Rare | Tête | Grand cône violet nuit à la pointe tordue, étoile dorée |
| 11 | `masque_heros` | Masque de héros | Rare | Visage | Bandeau bleu percé de deux trous pour les yeux |
| 12 | `ailes_ange` | Petites ailes d'ange | Rare | Dos | Deux petites ailes blanches qui battent doucement |
| 13 | `chapeau_couronne` ★ | Couronne royale | Super rare | Tête | Couronne d'or, 5 perles, rubis devant |
| 14 | `ailes_papillon` | Ailes de papillon | Super rare | Dos | Grandes ailes turquoise à taches orange, battement rapide |
| 15 | `aureole` | Auréole lumineuse | **Légendaire** | Tête | Anneau lumineux qui flotte et tourne au-dessus de la tête |
| 16 | `planete` | Petite planète | **Secret** | Dos | Mini-planète à anneau qui tourne en orbite autour de la tête |

Répartition par emplacement : Tête 7, Visage 3, Cou 3, Dos 3.

### 5.5 Objets gratuits (possédés d'office, hors boîtes)
Ils ne sont dans **aucune** boîte, ne sont jamais vendables et n'apparaissent pas dans l'Index
(seulement dans l'Inventaire, en premier).

| Identifiant | Nom affiché | Catégorie / emplacement |
|---|---|---|
| `couleur_bleu` | Bleu ciel (`#4A90D9`, mat) | skin (défaut) |
| `chapeau_aucun` | Rien sur la tête | accessoire, Tête (identifiant v1 conservé) |
| `visage_aucun` | Rien sur le visage | accessoire, Visage |
| `cou_aucun` | Rien au cou | accessoire, Cou |
| `dos_aucun` | Rien dans le dos | accessoire, Dos |

### 5.6 Fiches des nouveaux accessoires (pour l'artiste et le programmeur)

Règles communes : positions **locales au point d'accroche** de l'emplacement ; unité = mètre ;
épaisseur minimale 4 cm ; au plus 3 couleurs par objet ; aucun collider ; style jouet biseauté
de `art-bible.md` §3. En attendant les modèles Blender, le programmeur peut faire une version en
primitives Unity qui respecte la taille, la position et les couleurs (comme en v1) ; les
modèles Blender les remplaceront ensuite **avec le même identifiant**.

**`chapeau_bonnet` — Bonnet de laine (Commun, Tête)**
Demi-sphère douce Ø 0,95, hauteur 0,45, posée en y = −0,1 ; revers (anneau épais) de 10 cm de
haut à la base, Ø 1,0 ; gros pompon bosselé Ø 0,25 au sommet (y ≈ 0,4). Corps turquoise
`#2BB8C4` (smooth. 0,15, aspect laine mate), revers et pompon blanc cassé `#F4EEE6`.
Facultatif : côtes verticales suggérées sur le revers.

**`lunettes_soleil` — Lunettes de soleil (Commun, Visage)**
Deux verres « rectangles arrondis » de 0,26 × 0,18, 4 cm d'épaisseur, centrés en x = ±0,18
(devant chaque œil), légèrement collés à la courbe du visage (z = −0,03) ; pont de 8 cm entre
les deux ; branches qui repartent vers l'arrière sur les côtés (8 cm visibles). Verres et
monture `#2A2A3A` très brillants (smooth. 0,9) ; un petit reflet blanc `#FFFFFF` en diagonale
sur chaque verre (5 × 2 cm). Les yeux sont cachés : c'est le côté « cool ».

**`lunettes_coeur` — Lunettes cœur (Peu commun, Visage)**
Même placement que les lunettes de soleil ; deux verres en **forme de cœur** de 0,28 de large ×
0,24 de haut, 4 cm d'épaisseur, rose `#FF6FB5` brillant (smooth. 0,8), petite monture blanche
cassée `#F4EEE6` (bord de 2 cm, pont, branches), reflet blanc sur chaque verre.

**`masque_heros` — Masque de héros (Rare, Visage)**
Bandeau qui épouse la courbe de la capsule : 0,95 de large (il fait presque le demi-tour du
devant), 0,22 de haut, 4 cm d'épaisseur, centré sur les yeux (y = 0). **Deux trous ovales**
(0,20 × 0,17) exactement autour des yeux, pour qu'on les voie. Deux petits pans noués qui
dépassent sur les côtés à l'arrière (0,15 de long). Bleu héros `#3A5BD9` (smooth. 0,5), bord
fin bleu foncé `#22357F`. Doit être modélisé dans Blender (les trous ne se font pas avec des
primitives) ; version primitive provisoire : deux cubes bleus fins de part et d'autre des yeux
+ un cube au-dessus du pont du nez.

**`noeud_papillon` — Nœud papillon (Commun, Cou)**
Devant le corps : centre en (0 ; 0 ; −0,52). Deux « ailes » triangulaires arrondies de 0,18 de
large × 0,16 de haut × 6 cm, de part et d'autre d'un nœud central (cube arrondi 8 × 8 × 8 cm).
Rouge satiné `#C0392B` (smooth. 0,5), nœud central rouge plus foncé `#8E2A20`.

**`echarpe` — Écharpe rayée (Peu commun, Cou)**
Anneau épais autour du corps : rayon 0,53 (il colle à la capsule), 0,14 de haut, 8 cm
d'épaisseur. Un pan pend devant, à gauche (x = −0,2), de 0,35 de long et 0,12 de large,
légèrement de biais. Rayures alternées vert menthe `#3CC48A` / blanc cassé `#F4EEE6`
(4 rayures sur le pan, 12 sur l'anneau). Aspect laine (smooth. 0,15).

**`collier_fleurs` — Collier de fleurs (Peu commun, Cou)**
10 petites fleurs réparties sur un cercle de rayon 0,54 autour du corps, le devant descendant
de 8 cm plus bas que l'arrière (le collier « pend »). Chaque fleur : 5 pétales ronds aplatis
(Ø totale 0,14, épaisseur 4 cm) autour d'un cœur jaune. Couleurs alternées rose `#FF6FB5`,
blanc cassé `#F4EEE6`, cœurs jaune `#FFD54A`. Version primitive : 10 sphères aplaties
alternées rose / blanc.

**`chapeau_sorcier` — Chapeau de sorcier (Rare, Tête)**
Large bord Ø 1,2, 5 cm d'épaisseur, en y = −0,05, légèrement ondulé ; cône de 0,75 de haut,
base Ø 0,7, dont le dernier tiers **se plie vers l'arrière** (pointe tordue, 30°). Violet nuit
`#4B2E83` (smooth. 0,2, feutre). Ruban jaune `#FFD54A` de 8 cm à la base du cône, avec une
petite **étoile à 5 branches** jaune (Ø 0,14, 4 cm d'épaisseur) devant, sur le ruban.
Version primitive : 3 cylindres empilés comme le chapeau de fête, sans pli.

**`ailes_ange` — Petites ailes d'ange (Rare, Dos)**
Deux ailes symétriques, accrochées au point Dos, chacune 0,50 de long × 0,35 de haut × 6 cm,
bord inférieur festonné en 3 « plumes » arrondies. Orientées **à 35° vers l'extérieur et
l'arrière** pour dépasser nettement de chaque côté du corps vu de face, pointe vers le haut.
Blanc cassé `#F4EEE6` (smooth. 0,3), ombre douce des plumes `#D9D2E4`.
Animation : battement lent, rotation de ±8° autour de l'axe vertical de l'attache, période 1,6 s.

**`ailes_papillon` — Ailes de papillon (Super rare, Dos)**
Quatre lobes plats (4 cm d'épaisseur) : 2 grands en haut (0,45 × 0,40), 2 petits en bas
(0,30 × 0,25), ouverts à 30° vers l'arrière, dépassant largement des côtés.
Turquoise `#2BB8C4` (smooth. 0,6), une tache ronde orange `#F28C38` sur chaque grand lobe,
bord sombre `#34286A` de 3 cm.
Animation : battement rapide, ±20°, période 0,9 s ; les deux côtés battent en miroir.

**`aureole` — Auréole lumineuse (Légendaire, Tête)**
Anneau (tore) Ø 0,70, section 8 cm, horizontal, qui **flotte** à 0,30 au-dessus du sommet
de la tête (y = 0,30), sans le toucher. Couleur jaune pâle `#FFF1A8` avec **émission**
intensité 1,5 (il luit grâce au Bloom). Animation : monte et descend de ±0,04 m (période
2 s, sinus) et tourne sur lui-même à 30°/s. Version primitive : cylindre aplati
Ø 0,70 × 4 cm (sans trou), en attendant le tore de Blender.

**`planete` — Petite planète (Secret, Dos)**
Une boule Ø 0,30 violet lavande `#B48CFF` (smooth. 0,6) entourée d'un **anneau** plat Ø 0,52,
4 cm d'épaisseur, jaune `#FFD54A` avec émission légère (0,5), incliné de 20°.
Elle ne colle pas au dos : elle **tourne en orbite** autour de la tête, sur un cercle de rayon
0,9 centré en (0 ; 1,9 ; 0) **local à `Joueur`** (pas au point Dos), incliné de 15°, un tour
en 6 s ; elle tourne aussi sur elle-même (90°/s). Elle passe parfois devant le visage : c'est
voulu. Son emplacement « Dos » sert seulement à décider avec quoi elle se combine (on ne peut
pas porter des ailes et la planète en même temps).

**Animations des accessoires** : purement visuelles, elles continuent pendant les épreuves
et suivent les animations du personnage (ce sont des enfants de `Joueur`).

### 5.7 Exceptions à la bible de style (à valider par le directeur artistique)
`docs/art-bible.md` interdit pour l'instant l'émission, les textures et la transparence. Ces
boîtes en ont besoin pour que les hautes raretés se sentent spéciales. Proposition :
- **Émission** autorisée uniquement pour les raretés **Légendaire et Secret** (`matiere_arc_en_ciel`,
  `matiere_galaxie`, `aureole`, anneau de `planete`).
- **Textures générées par le code** (pas d'image plaquée) autorisées pour les skins à motif.
- **Transparence** autorisée pour `matiere_cristal` seulement.
Le DA met à jour `art-bible.md` (§4 « Émission », §8 « À éviter ») en conséquence.

---

## 6. Équilibrage

> **Remplacé en v3** par `docs/specs/v3.md` §6 (boîtes à 250, revente ×2,5, améliorations qui
> accélèrent les gains). Les calculs ci-dessous (boîte à 100) restent valables comme méthode.

### 6.1 Hypothèses
- Gain par étage **inchangé** : `10 × étage` (GDD §5). Le nouveau flux ne change pas les
  chances : encaisser au début de l'étage N, c'est encaisser les étages 1 à N−1, exactement
  comme avant.
- Un joueur moyen gagne **≈ 15 pièces par partie** (fourchette 10–20 ; aucune stratégie ne
  dépasse ~20, voir le tableau de `shop.md` §4.3, toujours valable).
- Une partie dure **≈ 22 s** avec le nouveau flux (pas de clic « Continuer », mais la montée et
  le verrou anti-clic) → ≈ **2,7 parties par minute** → **≈ 40 pièces par minute de jeu**.

Pourquoi garder le gain et monter le prix plutôt que l'inverse ? Les gains sont la base de la
tension de la tour (GDD §5) ; changer les prix des boîtes ne touche à rien d'autre.

### 6.2 Prix d'une boîte : 100 pièces
- Première boîte : **≈ 7 parties, ~2,5 minutes** → on découvre vite le système.
- Effet net d'un doublon vendu en moyenne en fin de collection : la vente rapporte en
  moyenne 17,65 pièces par ouverture (0,5 × 10 + 0,25 × 15 + 0,14 × 25 + 0,07 × 40 + 0,03 × 60
  + 0,01 × 80), donc une boîte « coûte » ~82 pièces nettes quand on a déjà tout.

### 6.3 Estimations (par boîte, chiffres moyens, ~40 pièces/minute)

| Objectif | Ouvertures en moyenne (médiane) | Pièces nettes (doublons revendus) | Temps de jeu estimé |
|---|---|---|---|
| Premier objet | 1 | 100 | ~2,5 min |
| Premier Rare **ou mieux** (25 %) | 4 (médiane 3) | ~400 | ~10 min |
| Premier Super rare ou mieux (11 %) | 9 (médiane 6) | ~880 | ~22 min |
| **Le Légendaire** (3 %) | **33** (médiane 23) | **~3 100** (médiane ~2 200) | **~1 h 15** (médiane ~55 min) |
| Le Secret (1 %) | 100 (médiane 69) | ~8 300 | ~3 h 30 |
| **Toute la boîte** (16/16) | **≈ 118** (médiane ≈ 95) | **≈ 10 100** | **≈ 4 h 15** (médiane ≈ 3 h 20) |
| Les deux boîtes complètes | ≈ 236 | ≈ 20 200 | ≈ 8 h 30 |

Comment c'est calculé (pour vérifier) :
- « En moyenne » pour un objet de chance p : 1 ÷ p ouvertures (3 % → 33). Médiane : la moitié
  des joueurs l'ont avant ce nombre (≈ 0,69 ÷ p).
- « Toute la boîte » : c'est le problème du collectionneur d'images ; il est dominé par le
  Secret à 1 %. Calcul numérique : ≈ 118 ouvertures en moyenne, dont ≈ 102 doublons revendus
  ≈ 1 700 pièces.
- **La chance compte beaucoup** : 1 joueur sur 10 attendra plus de 75 ouvertures pour le
  Légendaire (~2 h 30). C'est le prix d'un vrai hasard ; une garantie anti-malchance pourra
  être étudiée plus tard (§11).

Intention : un premier objet en 2–3 minutes, un objet « wow » (Rare) en ~10 minutes, le
Légendaire comme objectif de plusieurs sessions, la boîte complète comme défi de long terme.
À réajuster après les tests (temps réel d'une partie) et l'arrivée des autres épreuves.

---

## 7. Écrans et flux

> **v3** : §7.1 et §7.2 sont remplacés par `docs/specs/v3.md` §4 (colonne de 4 boutons à gauche,
> 4 panneaux séparés à gauche, 660 px de large en référence 1920 × 1080). Le contenu des onglets
> (§7.3 Boutique, §7.4 Inventaire, §7.5 Index) devient le contenu des panneaux du même nom.

### 7.1 Le bouton « Boutique » du HUD
Inchangé : texte « Boutique », couleur `#C99A1E`, visible seulement dans les situations du §4.1,
à côté de « Rejouer » sur les écrans Perdu / Encaissé, seul au début de l'étage 1.
Un clic ouvre le panneau latéral sur l'onglet **Boutique**.

### 7.2 Le panneau latéral
Fichiers suggérés : `Assets/UI/Shop.uxml` + `Shop.uss` (remplacent la v1).
- Il occupe le **côté droit** de l'écran : **440 px de large** (pour 1280 × 720), toute la
  hauteur sous la barre du haut. Fond `#2A2A3A` à 92 %, coin arrondi à gauche (16 px).
- Le personnage, au centre, reste visible et sert d'aperçu. (Le coffre de droite est caché
  derrière le panneau : ce n'est pas grave, les coffres sont bloqués.)
- Ouverture : glisse depuis la droite en 0,25 s (ease-out) ; fermeture : 0,2 s (ease-in).
- En haut du panneau : **trois onglets** côte à côte — **Boutique**, **Inventaire**, **Index** —
  et le bouton **« × »** (Fermer) à droite. Onglet actif : fond `#3A3A50` et soulignement or
  `#FFD54A` de 3 px.
- Rappel du porte-monnaie en haut du panneau (icône pièce + nombre), synchronisé avec la barre
  du haut.
- Le contenu de chaque onglet défile verticalement s'il dépasse (`ScrollView`).

### 7.3 Onglet « Boutique »
Une **carte par boîte**, l'une sous l'autre. Chaque carte contient, de haut en bas :
1. L'image de la boîte (120 × 120) et, à droite, son **nom** (« Boîte de skins n°1 ») et son
   **prix** (« 100 » + icône pièce, en or `#FFD54A`).
2. La **progression** : « Collection : 7 / 16 » + une barre de progression fine.
3. Le **tableau des taux de drop**, toujours visible (pas caché derrière un bouton) :

   | ● | Rareté | Chance | Objets |
   |---|---|---|---|
   | ● gris | Commun | 50 % | 5 |
   | ● vert | Peu commun | 25 % | 4 |
   | ● bleu | Rare | 14 % | 3 |
   | ● violet | Super rare | 7 % | 2 |
   | ● orange | Légendaire | 3 % | 1 |
   | ● rose | Secret | 1 % | 1 |

   Sous le tableau, en petit : « Chaque ouverture donne 1 objet. Les chances sont toujours les
   mêmes, à chaque ouverture. »
   Les pourcentages et les nombres d'objets sont **lus dans le catalogue**, jamais écrits dans l'UXML.
4. Les boutons :

| Situation | Bouton principal | Effet |
|---|---|---|
| Assez de pièces | « Ouvrir — 100 » (vert `#2E9E5B`, actif) | Ouvre la boîte (§4.2), lance l'animation (§7.6) |
| Pas assez | « Ouvrir — 100 » grisé + « Il te manque X pièces » en `#D64545` | Aucun |

   + un lien secondaire **« Voir le contenu »** → bascule sur l'onglet Index, sur cette boîte.
5. Si la collection est complète : bandeau « Collection complète ! Chaque ouverture donnera un
   doublon. » au-dessus du bouton (le bouton reste actif).

### 7.4 Onglet « Inventaire »
Ce qu'on possède, pour **équiper** et **vendre**.
- Deux sous-onglets : **Skins** | **Accessoires**. « Skins » par défaut.
- **Skins** : une grille de cartes (4 par ligne). En premier « Bleu ciel » (gratuit), puis les
  skins possédés, triés par rareté (du Secret au Commun), puis par numéro.
- **Accessoires** : quatre rangées titrées **Tête**, **Visage**, **Cou**, **Dos**. Chaque rangée
  commence par la carte « Rien » (gratuite) puis les accessoires possédés de cet emplacement,
  triés comme ci-dessus.
- Seuls les objets **possédés** apparaissent (les autres sont dans l'Index).
- **Une carte** montre : l'icône de l'objet (ou, en attendant les icônes, un carré de la couleur
  pour un skin / le nom en grand pour un accessoire, comme en v1), le nom, un **liseré de 3 px
  de la couleur de rareté** et le nom de la rareté en petit, un badge **« ×3 »** si on en a
  plusieurs exemplaires, et **« Équipé ✓ »** si c'est l'objet équipé.
- Cliquer une carte la **sélectionne** (contour or épais) et **l'affiche sur le personnage**
  (aperçu, non sauvegardé, comme en v1). Fermer le panneau ou changer d'onglet remet les objets
  équipés.
- **Zone d'action** en bas de l'onglet, pour la carte sélectionnée :

| État | Boutons | Effet |
|---|---|---|
| Possédé, non équipé | « Équiper » (bleu `#3B7DD8`) | Équipe (§4.5) |
| Équipé | « Équipé ✓ » (grisé) | Aucun |
| 2 exemplaires ou plus | + « Vendre 1 doublon (+15) » (montant de sa rareté) | Vend 1 doublon (§4.4) ; le badge passe de ×3 à ×2, puis disparaît à ×1 |
| 1 seul exemplaire ou objet gratuit | pas de bouton « Vendre » | — |

- En bas de l'onglet, toujours visible : **« Vendre tous les doublons (+X) »** où X est la
  somme totale, calculée par la logique. Grisé avec « Aucun doublon » si X = 0.
- Après une vente : défilement du porte-monnaie (0,4 s) + son `ui/vente`.

### 7.5 Onglet « Index »
La collection complète, boîte par boîte, pour savoir ce qui reste à trouver.
- Deux sous-onglets : **Boîte de skins n°1** | **Boîte d'accessoires n°1**.
- En haut : « 9 / 16 trouvés » + barre de progression.
- Grille de **16 cases** (4 × 4), dans l'ordre des numéros (n°01 Commun → n°16 Secret).
  Chaque case affiche son numéro « n°07 » et le liseré de sa rareté.

| État de l'objet | Ce qu'on voit dans la case |
|---|---|
| Obtenu | L'icône en couleurs, le nom, la rareté, la chance par objet (« 6,25 % ») |
| Non obtenu (toutes raretés sauf Secret) | L'icône en **silhouette** (icône teintée `#15121F`, opacité 0,9), le nom, la rareté, la chance par objet |
| Secret non obtenu | Un grand « ? » à la place de l'icône, nom **« ??? »**, rareté « Secret », chance « 1 % » |

- Cliquer une case **obtenue** : l'aperçu sur le personnage (comme dans l'Inventaire) et un
  raccourci « Voir dans l'inventaire ». Cliquer une case **non obtenue** : rien d'autre qu'un
  texte « Pas encore trouvé. Il est dans la Boîte de skins n°1. » (pas d'aperçu : la surprise
  reste entière).
- La silhouette se fait en teintant l'icône détourée (propriété USS
  `-unity-background-image-tint-color`), donc pas d'image en plus à produire.

### 7.6 L'animation d'ouverture d'une boîte
Un écran qui recouvre **tout** (HUD et panneau compris) : voile `#1F1730` à 85 %. Il est
construit en UI Toolkit (images et labels), pas en 3D. Le résultat est déjà connu et sauvegardé
(§4.2) quand il commence.

**Phases communes (identiques pour toutes les raretés — principe d'équité)**

| Temps | Durée | Ce qui se passe | Son |
|---|---|---|---|
| 0 | 0,3 s | Le voile apparaît (0,2 s) ; l'image de la boîte arrive au centre (300 × 300) : échelle 0 → 1,1 → 1 | `pieces_achat` |
| 0,3 | 1,2 s | **Secousse** : rotation alternée gauche/droite à 10 allers-retours par seconde, amplitude qui grandit de 2° à 10° ; petits sauts de 12 px toutes les 0,4 s ; lueur blanche neutre derrière la boîte qui grandit (aucune couleur de rareté) | `boite/boite_secoue` |
| 1,5 | 0,25 s | **Ouverture** : la boîte grossit (1 → 1,25) et s'efface ; flash blanc plein écran (opacité 0 → 0,7 → 0) | `boite/boite_ouverture` |
| 1,75 | selon la rareté | **Révélation** (tableau suivant) | `boite/revelation_<rarete>` |

**Révélation selon la rareté (plus c'est rare, plus c'est spectaculaire)**
La carte de l'objet (icône 220 × 220, nom, rareté) apparaît au centre : échelle 0,5 → 1,15 → 1
en 0,3 s, entourée d'un **halo** (disque flou teinté de la couleur de rareté), avec ou sans
**rayons** (une image de rayons qui tourne) et des **étincelles** (petites étoiles UI qui
jaillissent du centre, vitesse 200 à 500 px/s, gravité 600 px/s², durée de vie 0,8 à 1,2 s,
réserve de 60 éléments réutilisés).

| Rareté | Avant la carte | Halo | Rayons | Étincelles | Flash coloré | Secousse de l'écran | Texte de rareté | Durée de la révélation | Son |
|---|---|---|---|---|---|---|---|---|---|
| Commun | — | gris, Ø 260 px, opacité 0,5 | non | 0 | non | non | « Commun » (taille 24) | 0,5 s | `revelation_commun` |
| Peu commun | — | vert, Ø 300, 0,6 | non | 10 | non | non | « Peu commun » (24) | 0,7 s | `revelation_peu_commun` |
| Rare | — | bleu, Ø 340, 0,7 | 8 rayons, 30°/s | 18 | bleu, 0,3 | non | « Rare ! » (30) | 1,0 s | `revelation_rare` |
| Super rare | — | violet, Ø 380, pulse 1 ↔ 1,1 | 12 rayons, 45°/s | 28 | violet, 0,4 | ±6 px, 0,3 s | « Super rare ! » (34) | 1,4 s | `revelation_super_rare` |
| Légendaire | — | orange doré, Ø 440, pulse | 16 rayons, 60°/s | 45 (or et orange) | orange, 0,6 | ±10 px, 0,4 s | « LÉGENDAIRE ! » (40), lettres qui sautent une à une | 2,0 s ; la carte monte à 1,3 avant de revenir à 1 | `revelation_legendaire` |
| Secret | écran noir 0,4 s avec « … » au centre | arc-en-ciel (la teinte fait le tour en 2 s), Ø 480, pulse | 20 rayons de couleurs alternées, 60°/s | 60 multicolores | blanc, 0,8 | ±12 px, 0,5 s | « SECRET !!! » (44), aux couleurs de l'arc-en-ciel | 2,6 s (pause noire comprise) | `revelation_secret` |

> **v3** : la ligne « Secret » est remplacée par la cinématique de `docs/specs/v3.md` §8.3
> (coupure, battements, fissures, éclat, carte qui tombe en tournant, confettis ; 5,4 s).
> Correctifs : les panneaux ne se redessinent pas pendant l'ouverture (B1), les boutons de
> l'écran d'ouverture ne prennent jamais le focus clavier (B2) — `v3.md` §13.

Les halos, rayons et étincelles restent animés (doucement) tant que l'écran de résultat est affiché.

**Passer l'animation** : à partir de **0,5 s** après le clic sur « Ouvrir », un clic n'importe
où, Espace ou Échap **saute directement à l'écran de résultat** (la carte apparaît avec son halo
et ses rayons, sans étincelles). Si on saute avant 1,75 s, on entend quand même
`revelation_<rarete>`. (Avant 0,5 s, les clics sont ignorés : le double-clic sur « Ouvrir » ne
doit pas sauter l'animation.)

Pendant l'animation, la musique est atténuée comme pour les sons `resultat_*`
(`sons-animations.md` §6.1).

### 7.7 L'écran de résultat
Il apparaît 0,2 s après la fin de la révélation, sur le même voile, sous la carte :
- le **nom** de l'objet, la **rareté** (dans sa couleur), « Boîte de skins n°1 · n°07 » ;
- un badge **« Nouveau ! »** (or `#FFD54A`) ou **« Doublon — tu en as 3 »** (gris) ;
- les boutons (même animation d'apparition que `sons-animations.md` §3.10, cliquables à la
  fin de leur animation) :

| Bouton | Visible si | Effet |
|---|---|---|
| « Équiper » (bleu) | l'objet n'est pas déjà équipé | Équipe (§4.5), le bouton devient « Équipé ✓ » ; le personnage fait son petit saut d'accueil |
| « Vendre (+15) » | l'objet a au moins 2 exemplaires | Vend 1 doublon (§4.4), le bouton disparaît si on retombe à 1 |
| « Rouvrir — 100 » (vert) | toujours ; grisé si pas assez de pièces | Relance une ouverture de la **même** boîte (retour au temps 0 du §7.6) |
| « OK » | toujours | Ferme l'écran, retour à l'onglet Boutique ; l'objet équipé (ou non) est affiché sur le personnage |

Échap = « OK ».

### 7.8 Flux résumé
```
 Étage 1 (Choosing) ─┐
 Écran Perdu        ─┼─[Boutique]─> Panneau latéral ──[× / Échap]──> retour au même écran
 Écran Encaissé     ─┘                   │
                     ┌───────────────────┼─────────────────────┐
                 Boutique            Inventaire              Index
                     │               ├ clic carte → aperçu    ├ case obtenue → aperçu
             [Ouvrir — 100]          ├ [Équiper]              └ case vide → « pas encore trouvé »
                     │               ├ [Vendre 1 doublon]
         pièces −100, tirage,        └ [Vendre tous les doublons]
         sauvegarde
                     │
         Animation (secousse identique → révélation selon la rareté)
                     │
         Résultat : [Équiper] [Vendre] [Rouvrir] [OK]
```

### 7.9 Répartition dans le code (rappel des règles du projet)
- **Logique, sans UI** (sur le modèle de `Wallet` / `Shop`) : catalogue (raretés, boîtes,
  objets), tirage, collection (nombre d'exemplaires), vente, équipement par catégorie et par
  emplacement, sauvegarde et migration. C'est elle qui calcule « Il te manque X pièces », la
  chance par objet et le total « Vendre tous les doublons ».
- Le **catalogue** est la seule source des taux : un `ScriptableObject` (comme `ShopCatalog`)
  qui décrit les raretés (identifiant, nom, couleur, poids, valeur de vente), les boîtes
  (identifiant, nom, prix, liste d'objets) et les objets (identifiant, nom, rareté, catégorie,
  emplacement, rendu).
- Le droit d'ouvrir le panneau et le blocage des coffres restent décidés par le `GameManager`.
- L'**interface** (`ShopController` ou un contrôleur par onglet) affiche et transmet les clics.
- L'**animation d'ouverture** a son propre script d'affichage, qui reçoit le résultat déjà tiré.
- Le **personnage** (`PlayerAvatar`) applique un skin + 4 accessoires et gère leurs animations
  propres (couleur arc-en-ciel, scintillement, battement d'ailes, orbite).

---

## 8. Sons
Fichiers fabriqués par le sound designer (voir `sons-animations.md` §5.2) :
`boite/boite_secoue`, `boite/boite_ouverture`, `boite/revelation_commun`,
`boite/revelation_peu_commun`, `boite/revelation_rare`, `boite/revelation_super_rare`,
`boite/revelation_legendaire`, `boite/revelation_secret`, `ui/vente` ; et, déjà existants,
`pieces_achat` (paiement d'une boîte), `ui_boutique_ouvre` / `ui_boutique_ferme` (panneau),
`ui_clic` (onglets, cartes, équiper).

---

## 9. Données à sauvegarder (PlayerPrefs)

### 9.1 Clés

| Clé | Type | Contenu | Défaut |
|---|---|---|---|
| `wallet_coins` | int | inchangée | 0 |
| `collection_counts` | string | nombre d'exemplaires par objet, `identifiant:nombre` séparés par des virgules, ex. `couleur_rouge:3,chapeau_casquette:1` (seulement les objets à 1 ou plus ; jamais les objets gratuits) | `""` |
| `shop_equipped_couleur` | string | identifiant du **skin** équipé (nom de clé v1 conservé) | `couleur_bleu` |
| `shop_equipped_chapeau` | string | accessoire de l'emplacement **Tête** (nom de clé v1 conservé) | `chapeau_aucun` |
| `shop_equipped_visage` | string | accessoire Visage | `visage_aucun` |
| `shop_equipped_cou` | string | accessoire Cou | `cou_aucun` |
| `shop_equipped_dos` | string | accessoire Dos | `dos_aucun` |

- Sauvegarde (`PlayerPrefs.Save()`) après **chaque** ouverture, vente et équipement.
- On sauvegarde des **identifiants** (jamais des numéros de position), pour pouvoir ajouter
  des objets et des boîtes plus tard sans casser les sauvegardes.
- **v3 (B6)** : un identifiant inconnu du catalogue présent dans `collection_counts` est
  recopié tel quel à chaque écriture (jamais effacé), même s'il est ignoré par le jeu.
- **v3 (B5)** : la migration du §9.2 n'a lieu que si le catalogue est chargé.

### 9.2 Migration depuis la v1
Au chargement, si la clé `collection_counts` **n'existe pas** (`PlayerPrefs.HasKey`) :
1. lire `shop_owned` (v1) ; chaque identifiant connu devient `identifiant:1` ;
2. écrire `collection_counts`, supprimer `shop_owned`, sauvegarder.
Les clés `shop_equipped_couleur` / `shop_equipped_chapeau` sont reprises telles quelles.

Remise à zéro après des tests : supprimer `wallet_coins`, `collection_counts`, `shop_owned`
et les 5 clés `shop_equipped_*`.

---

## 10. Critères d'acceptation

> **v3** : les critères 1 à 3 (accès) sont remplacés par `docs/specs/v3.md` §19 (critères 1 à 7).
> Dans les autres, lire les prix et reventes v3 : boîte 250, revente 25 / 40 / 60 / 100 / 150 /
> 200 (ex. critère 7 : avec 240 pièces, « Il te manque 10 pièces » ; critère 18 : « +150 »).

**Accès et panneau**
1. Au début de l'étage 1 (avant tout choix), sur l'écran Perdu et sur l'écran Encaissé, le
   bouton « Boutique » est visible ; il ne l'est jamais aux étages 2 et plus, ni en
   `Revealing`, ni pendant la montée.
2. Le panneau s'ouvre sur le côté droit, onglet « Boutique » actif ; le personnage reste visible.
3. Panneau ouvert : clic sur un coffre ou touches 1/2/3 n'ouvrent rien ; les boutons Encaisser
   / Rejouer / Boutique sont cachés. « × » et Échap ferment le panneau et ramènent au même
   écran (même étage, même piège).

**Boutique et taux**
4. Chaque carte de boîte affiche son nom, son prix (100), sa progression et le tableau des
   6 raretés avec 50 % / 25 % / 14 % / 7 % / 3 % / 1 % et 5 / 4 / 3 / 2 / 1 / 1 objets.
5. Si je modifie un poids dans le catalogue (ex. Commun 500 → 490 et Rare 140 → 150), les
   pourcentages affichés changent sans toucher à l'UXML ni à l'UI (preuve qu'ils sont lus dans
   le catalogue).
6. Simulation dans l'éditeur (`script eval`) de **100 000 tirages** par la fonction de tirage
   du jeu : chaque rareté sort à ±0,5 point de son taux affiché (ex. Commun entre 49,5 % et
   50,5 %, Secret entre 0,5 % et 1,5 %), et chaque objet d'une même rareté sort à peu près
   autant que les autres (±1 point). La simulation ne touche ni au porte-monnaie ni à la sauvegarde.
7. Avec 90 pièces, « Ouvrir — 100 » est grisé et « Il te manque 10 pièces » s'affiche.
8. Avec 100 pièces, j'ouvre une boîte : le porte-monnaie affiche 0 (panneau et barre du haut),
   la collection gagne 1 objet.
9. Un double-clic rapide sur « Ouvrir » avec 150 pièces ne débite que 100 pièces et ne donne
   qu'un objet.
10. Le porte-monnaie n'est jamais négatif, quelle que soit la suite de clics.

**Animation**
11. La phase de secousse dure 1,2 s et est identique (durée, mouvement, son) pour toutes les
    raretés ; aucune couleur de rareté n'apparaît avant l'ouverture.
12. Chaque rareté a son halo, son son `revelation_<rarete>` et ses effets du tableau §7.6 ;
    un Secret commence par un écran noir de 0,4 s, un Commun n'a ni rayons ni étincelles.
13. Un clic 0,2 s après « Ouvrir » ne saute pas l'animation ; un clic après 0,5 s saute au
    résultat.
14. Si je quitte le mode Play pendant la secousse et le relance, l'objet tiré est dans ma
    collection et les 100 pièces sont débitées.

**Doublons et vente**
15. Si je tire un objet déjà possédé, l'écran de résultat affiche « Doublon — tu en as 2 » et
    « Vendre (+X) » avec X = valeur de sa rareté (10, 15, 25, 40, 60 ou 80).
16. Vendre un doublon Commun ajoute 10 pièces et fait passer le badge de ×2 à rien (×1).
17. Un objet à 1 exemplaire n'a jamais de bouton « Vendre » ; on ne peut pas tomber à 0.
18. « Vendre tous les doublons » avec 3 × Rouge tomate (Commun) et 2 × Couronne (Super rare)
    affiche « +60 » (2 × 10 + 1 × 40), crédite 60 pièces et laisse 1 exemplaire de chacun.
19. Toutes les valeurs de vente sont strictement inférieures au prix d'une boîte.

**Inventaire, équipement, Index**
20. L'Inventaire ne montre que les objets possédés (+ les objets gratuits) ; les accessoires
    sont rangés par emplacement Tête / Visage / Cou / Dos.
21. Je peux porter en même temps un skin, un chapeau, des lunettes, un nœud papillon et des
    ailes ; équiper d'autres lunettes ne retire que les lunettes.
22. Équiper « Rien sur la tête » retire le chapeau ; les autres emplacements ne bougent pas.
23. Cliquer une carte de l'Inventaire l'affiche sur le personnage ; fermer le panneau remet
    les objets équipés.
24. L'Index affiche 16 cases par boîte, numérotées n°01 à n°16 ; les objets non obtenus sont
    en silhouette avec leur nom et leur chance ; le Secret non obtenu affiche « ? » et « ??? ».
25. Une case non obtenue ne déclenche aucun aperçu sur le personnage.
26. Le compteur « 9 / 16 trouvés » correspond aux objets à 1 exemplaire ou plus.

**Rendu des objets**
27. Chacun des 16 skins et des 16 accessoires s'affiche sur le personnage aux couleurs et
    positions des §5.3 et §5.6 (vérifié en capture 1280 × 720) ; aucun accessoire ne traverse
    la capsule ni ne flotte à côté (sauf l'auréole et la planète, prévues pour flotter).
28. Passer de « Cristal » ou « Galaxie » à « Rouge tomate » rend un rouge mat opaque, sans
    transparence ni émission résiduelle.
29. « Arc-en-ciel néon » change de couleur en continu ; les ailes battent ; la planète tourne
    autour de la tête ; tout cela continue pendant les épreuves et suit les sauts du personnage.

**Sauvegarde**
30. J'ouvre des boîtes, j'équipe un skin et 4 accessoires, je quitte et relance : collection,
    nombres d'exemplaires, objets équipés et porte-monnaie sont identiques.
31. Une sauvegarde v1 avec `shop_owned = couleur_rouge,chapeau_casquette` et la casquette
    équipée devient : Rouge tomate ×1, Casquette ×1, casquette toujours équipée ; `shop_owned`
    est supprimée.
32. Un identifiant inconnu ou un nombre illisible dans `collection_counts` est ignoré sans
    erreur ; un objet équipé inconnu revient à l'objet gratuit de son emplacement.

**Règle d'or**
33. Avec n'importe quels objets équipés, l'épreuve reste à 1 coffre piégé sur 3 et le gain à
    10 × étage.
34. Il n'existe aucun moyen d'obtenir des pièces autrement qu'en jouant ou en vendant un
    doublon ; aucun lien, bouton ou texte ne parle d'argent réel.
35. Aucun compte à rebours, aucune offre limitée, aucun prix barré dans le panneau.

---

## 11. Hors périmètre (pas dans cette version)

- Une **garantie anti-malchance** (« un Légendaire assuré au bout de N boîtes ») : elle
  changerait les chances réelles et devrait alors être affichée ; à étudier après les tests.
- D'autres boîtes (n°2, boîtes à thème, boîtes Dés ou Effets de victoire du GDD), des
  boîtes temporaires ou saisonnières.
- L'ouverture multiple (« Ouvrir ×10 »).
- L'aperçu sur le personnage d'un objet **non obtenu**.
- Les échanges, la fabrication (« crafting » : combiner des doublons), l'achat direct d'un
  objet précis, les récompenses quotidiennes.
- Une boîte 3D modélisée dans Blender et une ouverture en 3D (l'animation est en UI).
- Des filtres et tris avancés dans l'Inventaire, une fenêtre de détail par objet.
- Un indicateur « nouvel objet non vu » (point rouge sur l'onglet).
- Les vrais modèles Blender des nouveaux accessoires et les icônes Gemini : ils sont
  demandés à l'équipe art (§12), le jeu doit fonctionner avec les versions provisoires.
- Toute monnaie premium, tout achat en argent réel (définitivement, pas seulement pour cette
  version).

---

## 12. Images et modèles à produire (pour l'équipe art)

| Fichier | Type | Description |
|---|---|---|
| `boite_skins_1.png` | icône 1:1 (fond lavande `#E6E1EE`, à détourer) | Boîte cadeau jouet, trapue, violet `#6A4C9C`, ruban et gros nœud laiton `#C99A1E`, sur le couvercle un petit emblème « goutte de peinture » multicolore en relief. Aucun texte ni chiffre |
| `boite_accessoires_1.png` | icône 1:1 | Même boîte, turquoise `#2BB8C4`, même ruban laiton, emblème « mini haut-de-forme » en relief sur le couvercle. Aucun texte |
| Icônes des 12 nouveaux skins | icône 1:1 | La capsule avec ses yeux, dans le rendu du §5.3 (comme les icônes `couleur_*.png` existantes) |
| Icônes des 12 nouveaux accessoires | icône 1:1 | L'accessoire seul, selon les fiches du §5.6 |
| `ui_halo.png`, `ui_rayons.png`, `ui_etincelle.png` | UI, blanc sur fond transparent | Disque flou ; 8 rayons en éventail réguliers ; petite étoile à 4 branches. Blancs pour être teintés par le code |
| Modèles Blender des 12 nouveaux accessoires | `.glb` | Selon §5.6 ; origine au point d'accroche ; nommés par leur identifiant |

Rangement : icônes détourées dans `Assets/Art/icones_detourees/` (nom = identifiant), images UI
dans `Assets/Art/ui/`.
