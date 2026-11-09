# Tour de la Chance — Bible de style (art bible)

> **À quoi sert ce document ?** C'est la référence visuelle commune. Tous les modèles 3D
> (Blender), les images générées (Gemini) et l'interface (UI Toolkit) doivent la suivre, pour
> que le jeu ait l'air d'avoir été fait par une seule main. Avant de modéliser un objet,
> de rédiger un prompt d'image ou de choisir une couleur d'interface : on lit cette page.
>
> Elle complète le GDD (`docs/GDD.md`) et ne le contredit pas. Les couleurs des objets du shop
> viennent de `docs/specs/shop.md` (v1) et de `docs/specs/boites.md` (boîtes à surprises, v2)
> et sont reprises **telles quelles**.
>
> **Mise à jour « lot 2 »** : arbitrage des exceptions demandées par `boites.md` §5.7
> (émission, textures générées par le code, transparence → §4.1), palette des raretés et des
> nouveaux objets (§2.7, §2.8), couleurs de rareté dans l'interface (§10), décor par altitude
> (§5.1) et règles des prompts courts (§9).

---

## 1. L'ambiance en une phrase

**Une fête foraine magique, la nuit, au sommet d'une tour : des objets-jouets en bois, en
métal et en or, posés sur une pierre bleutée, éclairés par une lumière chaude.**

Trois mots-clés : **jouet**, **chaleureux**, **lisible**.

- *Jouet* : formes simples, rondes, épaisses, comme des jouets en bois peint ou des pièces de
  jeu de société. Rien de réaliste, rien d'inquiétant.
- *Chaleureux* : les objets importants (coffres, or, personnage) sont dans des tons chauds ou
  saturés ; le décor (sol, fond) est dans des tons froids et sourds, pour que les objets
  « sortent » de l'image.
- *Lisible* : la caméra est fixe, assez loin, en 3/4 au-dessus. Chaque objet doit être
  reconnaissable **en silhouette noire de 64 pixels de haut**. Si un détail ne se voit pas
  depuis la caméra, on ne le modélise pas.

### État actuel (prototype gris) et ce qui change
Les captures `docs/captures/fix_1_message_bas.png` et `fix_2_shop_coffres_fermes.png` montrent :
des coffres-cubes marron, un couvercle-plaque plus foncé, une capsule bleue aux yeux noirs, un
disque gris-bleu sur un fond brun-gris uniforme. Les **positions, tailles et couleurs de base
sont bonnes** et sont conservées. Ce qui change :
- les cubes deviennent des **objets reconnaissables** (couvercle bombé, ferrures, serrure) ;
- le fond brun-gris devient un **violet nuit** (ambiance magique, et l'or ressort mieux) ;
- le disque uni devient un **dallage de pierre** avec une bordure ;
- l'éclairage gagne une **lumière d'ambiance colorée** pour que le métal et l'or brillent.

---

## 2. Palette (codes hexadécimaux par rôle)

> Les codes sont en **sRGB**, c'est-à-dire tels qu'on les tape dans le champ « Hex » de Blender
> (Principled BSDF → Base Color) ou de Unity (Base Color du shader URP Lit). Pas de conversion à faire.

### 2.1 Décor

| Rôle | Hex | Où |
|---|---|---|
| Fond (ciel de nuit) | `#1F1730` | Couleur de fond de la caméra (Solid Color) |
| Fond, halo autour de la tour (pour les images) | `#3A2850` | Dégradé dans les concept arts et icônes de lot « décor » |
| Pierre du sol — dalle principale | `#5B6380` | La majorité des dalles |
| Pierre du sol — dalle variante | `#535A75` | 1 dalle sur 3 environ, pour casser l'uniformité |
| Pierre du sol — joints (creux entre dalles) | `#3C4258` | Rainures entre les dalles |
| Pierre claire — bordure et rosace | `#8A90B0` | Margelle du disque, motif incrusté |
| Pierre — flanc de la plateforme | `#434A63` | Côté vertical du disque (dans l'ombre) |
| Incrustation magique | `#6A4C9C` | Fins filets de la rosace du sol (accent violet) |

### 2.2 Coffres

| Rôle | Hex | Où |
|---|---|---|
| Bois — corps (couleur « fermé ») | `#8C5933` | Le corps du coffre. **= la couleur actuelle du code** (`Chest.closedColor` 0,55 / 0,35 / 0,2) |
| Bois foncé — couvercle | `#6B4226` | Le dessus bombé du couvercle |
| Bois clair — chants, arêtes | `#A8703F` | Petites faces biseautées, bord du couvercle (facultatif) |
| Intérieur du coffre | `#3A2416` | Fond et parois intérieures (presque noir chaud, on voit dedans quand c'est ouvert) |
| Métal sombre — ferrures, charnières | `#4A4E5A` | Bandes de renfort, coins, charnières |
| Laiton — serrure | `#C99A1E` | Plaque de serrure. **= l'or du bouton « Boutique »** de l'interface |
| Trou de serrure | `#1A1208` | Petit creux dans la plaque |

### 2.3 Signaux de jeu (réservés : on ne les utilise pas pour décorer)

| Rôle | Hex | Où |
|---|---|---|
| **Or / trésor / pièces** | `#FFC83A` | Coffre gagnant, pièces, couronne, couleur « Or massif ». **Le seul or « métal » du jeu** |
| **Piège** | `#D91A1A` | Coffre piégé. **= la couleur actuelle du code** (`Chest.trapColor` 0,85 / 0,1 / 0,1) |
| Sûr / encaisser | `#2E9E5B` | Boutons « Encaisser », « Acheter » (déjà dans l'UI) |
| Or « texte » de l'interface | `#FFD54A` | Textes de pièces, cagnotte, contour de sélection (déjà dans l'UI) |

> Recommandation au programmeur (non bloquante) : `Chest.treasureColor` vaut aujourd'hui
> 1 / 0,8 / 0,1 (`#FFCC1A`). L'aligner sur `#FFC83A` unifierait l'or du coffre avec celui
> de la couronne et de la couleur « Or massif ».

### 2.4 Personnage et shop (repris tels quels de `docs/specs/shop.md`)

| Objet | Hex | Rendu |
|---|---|---|
| Bleu ciel (défaut) | `#4A90D9` | mat |
| Rouge tomate | `#D64545` | mat |
| Vert menthe | `#3CC48A` | mat |
| Violet magique | `#8E5BD6` | légèrement brillant |
| Or massif | `#FFC83A` | métal brillant |
| Yeux | `#111111` | brillants (ne changent jamais) |
| Casquette — calotte | `#E04040` | tissu |
| Casquette — visière | `#A02828` | tissu |
| Casquette — bouton | `#FFFFFF` | tissu |
| Chapeau de fête — rose | `#FF6FB5` | carton brillant |
| Chapeau de fête — jaune | `#FFD54A` | carton brillant |
| Chapeau de fête — pompon | `#FFFFFF` | duveteux (mat) |
| Haut-de-forme — feutre | `#1E1E1E` | feutre |
| Haut-de-forme — ruban | `#C0392B` | satin |
| Couronne — or | `#FFC83A` | métal brillant |
| Couronne — pierre | `#D62839` | rubis très brillant |
| Reflet dans l'œil (nouveau) | `#FFFFFF` | petit point, voir fiche personnage |

### 2.5 Interface (existante, pour mémoire)
Panneaux `#2A2A3A` / cartes survolées `#3A3A50`, bouton neutre `#5B5BD6`, bouton « Équiper »
`#3B7DD8`, bouton « Boutique » `#C99A1E`, rouge d'alerte `#D64545`, contour des textes or
`#3A2200`. Le fond violet nuit `#1F1730` s'accorde avec ces panneaux bleu-violet : on garde.

### 2.6 Règles d'usage des couleurs
1. **Le décor est froid et sourd** (bleu-gris, violet sombre), **les objets de jeu sont chauds**
   (bois, laiton, or). C'est ce contraste qui guide l'œil vers les coffres.
2. **Rouge vif = danger, or = gain.** On n'en met pas de grandes surfaces ailleurs. Les petits
   rouges des cosmétiques (casquette, ruban, rubis) sont tolérés car ils sont petits et
   portés par le personnage, loin du coffre.
3. **Pas de noir pur `#000000` ni de blanc pur `#FFFFFF` en grande surface.** Le noir le plus
   sombre est `#111111` (yeux) ; le blanc pur est réservé aux petits détails (pompon, bouton, reflet).
4. Au maximum **3 couleurs par objet** (hors or des signaux). Seule exception : le skin
   « Arc-en-ciel néon » (Légendaire), dont la couleur unique change avec le temps.
5. **Les couleurs de rareté (§2.7) sont réservées aux raretés** : liserés, pastilles, halos et
   noms de rareté. On ne s'en sert pas pour décorer un bouton ou un panneau (voir §10).
6. Le **rouge du skin « Sucre d'orge »** et les teintes rouge/or que traverse l'« Arc-en-ciel
   néon » sont tolérés pour la même raison que les petits rouges des chapeaux : ils sont portés
   par le personnage, derrière le coffre du milieu, et ne touchent jamais le bois des coffres.

### 2.7 Raretés (repris tels quels de `docs/specs/boites.md` §5.1)

| Rareté | Couleur de base | Couleur « texte » sur panneau sombre (§10) |
|---|---|---|
| Commun | `#9AA0B0` gris | `#9AA0B0` (identique) |
| Peu commun | `#4CC26A` vert | `#4CC26A` (identique) |
| Rare | `#3B8BEB` bleu | **`#5FA2F0`** (plus clair, voir §10) |
| Super rare | `#A35CE0` violet | **`#B57DE8`** (plus clair, voir §10) |
| Légendaire | `#FF9F1C` orange doré | `#FF9F1C` (identique) |
| Secret | `#FF4FD8` rose magique (+ arc-en-ciel animé à la révélation) | `#FF4FD8` (identique) |
| Silhouette d'un objet non obtenu (Index) | `#15121F`, opacité 0,9 | — |

Le Légendaire est **orange** et non or : l'or `#FFC83A` reste le signal « trésor » du coffre.
Le vert Peu commun `#4CC26A` est volontairement différent du vert « Encaisser » `#2E9E5B`.

### 2.8 Nouveaux objets des boîtes (repris tels quels de `docs/specs/boites.md` §5.3, §5.4, §12)

**Couleur partagée** : blanc cassé `#F4EEE6` (rayures, pois, revers du bonnet, pompons, ailes
d'ange, monture des lunettes cœur, fleurs). C'est le « blanc » des grandes surfaces, puisque le
blanc pur `#FFFFFF` est réservé aux petits détails (§2.6, règle 3).

| Skin | Rendu | Accessoire | Couleurs |
|---|---|---|---|
| Jaune citron | `#F2D03B` mat | Bonnet de laine | `#2BB8C4` + `#F4EEE6` |
| Orange mandarine | `#F28C38` mat | Lunettes de soleil | `#2A2A3A` très brillant, reflet `#FFFFFF` |
| Rose bonbon | `#F27BB0` mat | Nœud papillon | `#C0392B` + nœud `#8E2A20` |
| Turquoise lagon | `#2BB8C4` brillant | Lunettes cœur | `#FF6FB5` + monture `#F4EEE6` |
| Caramel | `#C98A4B` très brillant (bonbon) | Écharpe rayée | `#3CC48A` / `#F4EEE6` |
| Blanc nacré | `#EDE6F2`, reflets de perle | Collier de fleurs | `#FF6FB5` / `#F4EEE6`, cœurs `#FFD54A` |
| Sucre d'orge | rayures `#D64545` / `#F4EEE6` | Chapeau de sorcier | `#4B2E83`, ruban et étoile `#FFD54A` |
| Pois de fête | `#F27BB0` + pois `#F4EEE6` | Masque de héros | `#3A5BD9`, bord `#22357F` |
| Argent poli | `#C9CED8` métal miroir | Petites ailes d'ange | `#F4EEE6`, ombre des plumes `#D9D2E4` |
| Cristal | `#9FE3FF` transparent 0,55 | Ailes de papillon | `#2BB8C4`, taches `#F28C38`, bord `#34286A` |
| Arc-en-ciel néon | teinte qui tourne, émission | Auréole lumineuse | `#FFF1A8`, émission |
| Galaxie | `#2E2366` + étoiles `#FFF1D6` émissives | Petite planète | `#B48CFF`, anneau `#FFD54A` émission légère |

**Les deux boîtes** (images d'interface, pas de modèle 3D) :

| Boîte | Corps | Couvercle (bord) | Ruban, nœud, médaillon | Emblème du médaillon |
|---|---|---|---|---|
| `boite_skins_1` | `#8E5BD6` violet magique | `#6A4C9C` | laiton `#C99A1E` | goutte de peinture en 3 bandes `#D64545` / `#3CC48A` / `#4A90D9` |
| `boite_accessoires_1` | `#2BB8C4` turquoise | `#1F8A94` (turquoise ombré) | laiton `#C99A1E` | mini haut-de-forme `#1E1E1E`, ruban `#C0392B` |

> Arbitrage du DA : `boites.md` §12 proposait un corps violet `#6A4C9C` pour la boîte de skins.
> Sur le voile `#1F1730` de l'animation d'ouverture et sur les cartes `#2A2A3A`, ce violet
> sombre se perdait (contraste ≈ 2:1). Le corps passe au violet magique `#8E5BD6`, plus clair ;
> `#6A4C9C` est gardé pour le bord du couvercle, ce qui donne du relief. Le médaillon en laiton
> est placé **sur le ruban, au milieu de la face avant** (et non sur le dessus du couvercle) :
> en vue de 3/4, c'est la face la plus visible, et le gros nœud occupe déjà le dessus.

---

## 3. Formes : le style « jouet low-poly »

### 3.1 Principes
- **Proportions jouet** : tout est un peu trapu, épais et arrondi. Un objet réel fait
  « 100 % » ; chez nous, les détails importants (serrure, bord du chapeau, pointes de la
  couronne) font **130 à 150 %** de leur taille réaliste.
- **Épaisseur minimale : 4 cm** (0,04 unité). En dessous, le détail scintille ou disparaît à la
  distance de la caméra (~8 m).
- **Biseaux partout sur les objets durs** (coffre, chapeau rigide, couronne, dalles) :
  modificateur *Bevel* de **2 à 4 cm, 1 seul segment** (2 pour la couronne). Le biseau
  « attrape » la lumière et donne cet aspect de jouet en bois verni. Pas d'arête vive à 90°.
- **Silhouette avant les détails** : on vérifie d'abord que la forme générale est
  reconnaissable, ensuite seulement on ajoute ferrures, rubans, boutons.
- **Détails en géométrie, pas en texture** : une planche = une rainure modélisée, une
  ferrure = une bande en relief de 2 à 3 cm. Aucune image **dessinée ou générée** n'est plaquée
  sur les modèles. Seule exception : les **motifs des skins** (rayures, pois, étoiles de la
  galaxie), fabriqués par le code (§4.1).
- **Asymétrie légère et volontaire** seulement là où elle donne de la vie (chapeau de fête
  penché, dalles du sol à peine décalées). Le reste est symétrique.

### 3.2 Lissage (shading)
- **Objets durs** (coffre, dalles, haut-de-forme, couronne) : faces plates + biseaux, avec
  *Smooth by Angle* (lissage automatique) à **30°**. Résultat : de grandes faces nettes,
  des arêtes douces.
- **Objets ronds** (corps du personnage, calotte de casquette, pompon, perles, yeux) :
  *Shade Smooth* complet. Un personnage à facettes visibles a l'air abîmé, pas « jouet ».

### 3.3 Conventions techniques (pour que tout s'emboîte dans Unity)
- **1 unité Blender = 1 mètre = 1 unité Unity.** Appliquer l'échelle et la rotation
  (*Apply → All Transforms*) avant l'export.
- **On garde les tailles actuelles du prototype** (voir les fiches) : les positions dans la
  scène, les colliders et le code restent valables.
- **Avant de l'objet = côté caméra.** Dans Unity, la caméra est en (0 ; 5 ; −7) : l'avant des
  objets regarde vers les **z négatifs**. En export glTF (`.glb`), cela correspond normalement
  à **+Y dans Blender** ; à **vérifier sur le premier export** (la serrure du coffre doit être
  face à la caméra), puis garder la même convention pour tous les modèles.
- **Origine (point de pivot)** au bon endroit, indiqué dans chaque fiche : c'est elle qui
  détermine où l'objet se pose et autour de quoi il tourne.
- **Noms des objets en français, sans accents**, identiques à la hiérarchie Unity
  (`Corps`, `Couvercle`, `Charniere`…). Pour les chapeaux : l'identifiant du shop
  (`chapeau_casquette`…).
- **Un matériau = un rôle de la palette**, nommé `M_<Role>` (ex. `M_BoisCorps`, `M_MetalSombre`,
  `M_Or`). Les objets d'un même rôle **partagent** le même matériau.
- **Pas de couleurs de sommets (vertex colors)** : le shader URP Lit ne les affiche pas.
  Les variations de teinte se font avec plusieurs matériaux.

---

## 4. Matériaux (valeurs indicatives URP Lit)

> Unity parle de **smoothness** (lisse), Blender de **roughness** (rugueux) : c'est l'inverse,
> `roughness = 1 − smoothness`. Les deux colonnes sont données pour éviter les erreurs.
> Rappel du projet : dans Unity, tous les matériaux utilisent le shader **URP Lit**. Si
> l'import `.glb` crée d'autres shaders, l'intégrateur les remplace par URP Lit en reprenant
> ces valeurs.

| Matériau | Couleur | Metallic | Smoothness (Unity) | Roughness (Blender) |
|---|---|---|---|---|
| `M_BoisCorps` | `#8C5933` | 0 | 0,25 | 0,75 |
| `M_BoisCouvercle` | `#6B4226` | 0 | 0,25 | 0,75 |
| `M_BoisClair` | `#A8703F` | 0 | 0,30 | 0,70 |
| `M_Interieur` | `#3A2416` | 0 | 0,10 | 0,90 |
| `M_MetalSombre` | `#4A4E5A` | 0,8 | 0,45 | 0,55 |
| `M_Laiton` | `#C99A1E` | 1 | 0,60 | 0,40 |
| `M_TrouSerrure` | `#1A1208` | 0 | 0,10 | 0,90 |
| `M_Or` | `#FFC83A` | 1 | 0,80 | 0,20 |
| `M_PierreDalle` | `#5B6380` | 0 | 0,15 | 0,85 |
| `M_PierreDalleVariante` | `#535A75` | 0 | 0,15 | 0,85 |
| `M_PierreJoint` | `#3C4258` | 0 | 0,05 | 0,95 |
| `M_PierreClaire` | `#8A90B0` | 0 | 0,25 | 0,75 |
| `M_PierreFlanc` | `#434A63` | 0 | 0,10 | 0,90 |
| `M_Incrustation` | `#6A4C9C` | 0,3 | 0,50 | 0,50 |
| `M_PersoCorps` | `#4A90D9` (défaut, recoloré par le shop) | 0 | 0,30 | 0,70 |
| `M_Yeux` | `#111111` | 0 | 0,85 | 0,15 |
| `M_Reflet` | `#FFFFFF` | 0 | 0,90 | 0,10 |
| `M_TissuRouge` / `M_TissuRougeFonce` | `#E04040` / `#A02828` | 0 | 0,20 | 0,80 |
| `M_Blanc` | `#FFFFFF` | 0 | 0,20 | 0,80 |
| `M_CartonRose` / `M_CartonJaune` | `#FF6FB5` / `#FFD54A` | 0 | 0,45 | 0,55 |
| `M_Feutre` | `#1E1E1E` | 0 | 0,15 | 0,85 |
| `M_Satin` | `#C0392B` | 0 | 0,50 | 0,50 |
| `M_Rubis` | `#D62839` | 0 | 0,90 | 0,10 |

Couleurs du personnage (rappel shop) : bleu / rouge / vert → smoothness 0,3 ; violet → 0,6 ;
or → metallic 1, smoothness 0,8.

### 4.1 Matières spéciales des boîtes (arbitrage du DA sur `boites.md` §5.7)

Les trois exceptions demandées sont **acceptées**, avec des limites précises. L'idée : une
matière spéciale doit se « mériter ». Plus l'objet est rare, plus il a le droit d'être
différent ; un objet Commun ou Peu commun reste toujours une couleur unie, mate ou brillante.

**1. Émission (lumière propre) — acceptée pour le Légendaire et le Secret uniquement.**

| Objet | Rareté | Émission |
|---|---|---|
| `matiere_arc_en_ciel` | Légendaire | même couleur que la base (teinte qui tourne), intensité **0,8** |
| `aureole` | Légendaire | `#FFF1A8`, intensité **1,5** |
| `matiere_galaxie` (les étoiles seulement, pas le fond) | Secret | `#FFF1D6`, intensité qui oscille **0,6 ↔ 1,2** |
| `planete` (l'anneau seulement, pas la boule) | Secret | `#FFD54A`, intensité **0,5** |

Règles :
- **Plafond : intensité 1,5.** Au-delà, avec le Bloom (seuil 1,0), le personnage deviendrait
  plus lumineux que le coffre au trésor ouvert : il volerait la vedette au moment du résultat.
- Le personnage seul peut émettre (et les futurs effets de victoire). **Jamais le décor, les
  coffres ni le sol.**
- *Pourquoi ces deux raretés* : à 3 % et 1 %, voir un personnage qui brille dans la nuit doit
  dire tout de suite « celui-là a eu de la chance ». Si un Rare brillait aussi, la lueur ne
  voudrait plus rien dire.

**2. Textures fabriquées par le code — acceptées pour les 3 skins à motif** (`motif_sucre_orge`,
`motif_pois`, `matiere_galaxie`).
- Ce sont de petites images (64 × 64 pixels) **calculées au démarrage** par un script, pas des
  fichiers dessinés ni générés par Gemini : on garde ainsi des aplats nets, sans grain ni usure.
- Uniquement des **aplats de couleurs de la palette** (§2.8) : 2 couleurs par motif au plus,
  bords nets (filtre *Bilinear*, avec *mipmaps* pour éviter le scintillement de loin).
- **Tailles minimales** (règle des 4 cm, §3.1) : bandes de rayures ≥ 20 cm (8 bandes sur 2 m
  = 25 cm), pois ≈ 12 cm, étoiles de la galaxie 2 à 5 cm (elles ont le droit d'être petites :
  elles brillent, le Bloom les grossit).
- Elles demandent une UV cylindrique sur le `Corps` du personnage (§6.2).

**3. Transparence — acceptée pour `matiere_cristal` seulement.**
- Opacité **0,55, jamais moins** : en dessous, la capsule disparaît sur le fond violet et sa
  silhouette n'est plus lisible à 64 pixels.
- Smoothness 0,95 obligatoire : ce sont les **reflets** qui dessinent la forme d'un objet
  transparent. Sans eux, il ressemble à un fantôme flou.
- Voir les yeux « à travers » le corps est voulu. Un seul objet transparent à la fois dans la
  scène : on n'empile pas de transparences (problèmes d'ordre d'affichage).

**Remise à zéro** : au changement de skin, le code éteint tout ce que le skin précédent avait
activé (émission, texture, transparence), comme le demande `boites.md` §5.3.

**Ce qui reste interdit** : l'émission pour un objet Commun à Super rare ou « pour faire joli »,
les images dessinées ou générées plaquées sur un modèle, la transparence ailleurs que sur le
Cristal, les couleurs de sommets.

---

## 5. Éclairage et rendu

Objectif : une lumière **chaude de fête foraine** sur les objets, une **nuit violette** autour.

| Élément | Réglage indicatif |
|---|---|
| Lumière principale (Directional Light) | Couleur `#FFF1D6` (blanc chaud, déjà proche de l'actuelle), intensité **1,2**, venant d'en haut à gauche et de devant (rotation actuelle conservée, ~50° de hauteur). Ombres douces (*Soft Shadows*). |
| Lumière d'ambiance (Lighting → Environment) | Mode **Gradient** : ciel `#8C7AB8`, horizon `#5A4A70`, sol `#2A2030`. Elle teinte légèrement les ombres en violet et évite les ombres noires. |
| Contre-jour (facultatif) | Une Point Light `#B48CFF`, intensité 0,6, derrière et au-dessus du personnage (≈ (0 ; 3,5 ; 4)) : un liseré violet détache le personnage et les chapeaux du fond. |
| Reflets | Une **Reflection Probe** au centre de l'étage (ou le ciel en Gradient comme source). **Indispensable** : sans elle, les matériaux metallic 1 (or, laiton, couronne) paraissent ternes ou noirs. |
| Fond de caméra | *Solid Color* `#1F1730`. |
| Post-traitement (Volume global) | Tonemapping **Neutral** (respecte nos codes hex) ; Bloom léger (seuil 1,0, intensité 0,3) pour faire briller l'or ; Vignette 0,25 pour concentrer le regard au centre. |

Règle : on ne règle **pas** les couleurs des modèles pour compenser un mauvais éclairage. Si un
objet paraît faux en jeu, on corrige l'éclairage d'abord.

### 5.1 Le décor change avec l'altitude (`sons-animations.md` §3.14)

Les réglages ci-dessus sont ceux du **palier 1**. Aux paliers suivants, seuls changent l'image
du fond, la lumière principale, la lumière d'ambiance et les nuages 3D (valeurs dans
`sons-animations.md` §3.14, validées par le DA). Le sol, les coffres, le personnage, le
contre-jour et le post-traitement **ne changent pas**.

| Palier | Étages | Image de fond | Idée visuelle | Valeur générale |
|---|---|---|---|---|
| 1 | 1 – 3 | `ciel_nuit.png` (= `ciel_palier1`) | nuit violette, nuages **au-dessus**, fête foraine au loin en bas | sombre |
| 2 | 4 – 6 | `ciel_palier2.png` | **dans** les nuages : brume rose-lavande lumineuse, gros nuages tout autour | **moyenne** (le plus clair des 4) |
| 3 | 7 – 9 | `ciel_palier3.png` | **au-dessus** de la mer de nuages : ciel bleu nuit dégagé, grosse lune ronde sur le côté | sombre |
| 4 | 10 et + | `ciel_palier4.png` | **dans les étoiles** : ciel profond, aurore douce verte et violette | le plus sombre |

Règles communes aux 4 fonds :
- La montée se lit **de bas en haut** : au palier 1 les nuages sont au-dessus de nous ; au
  palier 2 ils nous entourent ; au palier 3 ils sont sous nous ; au palier 4 ils ont presque
  disparu tout en bas.
- **Rien de lumineux ni de net au centre** (c'est là que sont les coffres) ; la lune du palier 3
  et l'aurore du palier 4 sont **en haut, sur un côté**.
- Le **bas** reste le plus sombre de l'image (barre de boutons, panneau, messages).
- Même au palier 2, aucune zone plus claire que `#B49AC8` : les textes or à contour sombre
  de l'interface doivent rester lisibles, et les coffres chauds doivent toujours ressortir.
- Mêmes nuages « pouf » arrondis et low-poly sur les 4 images (même famille que le nuage du
  piège et les nuages 3D).

---

## 6. Fiches des objets à modéliser

> Repère caméra : position (0 ; 5 ; −7), regard plongeant d'environ 35°. On voit surtout
> **le dessus et la face avant** des objets, un peu les côtés des coffres extérieurs, jamais
> l'arrière. On soigne donc dans cet ordre : dessus > avant > côtés > arrière (minimal).

### 6.1 Le coffre (`Coffre`) — modèle unique, utilisé 3 fois

**À quoi il ressemble** : le coffre au trésor de pirate « jouet », trapu, en planches de bois
cerclées de métal, avec un couvercle bombé et une grosse serrure en laiton bien visible devant.

**Dimensions (identiques au prototype)**
- Corps : 1,40 (largeur x) × 0,80 (hauteur) × 1,00 (profondeur), posé sur le sol (y de 0 à 0,8).
- Couvercle : 1,50 × 1,05 de débord (il dépasse de 5 cm de chaque côté et devant : effet jouet).
- Hauteur du couvercle **0,35 maximum** (bombé compris). Au-delà, une fois ouvert à 110°, il
  reculerait trop et toucherait le personnage (avant de la capsule en z = 1,5).

**Hiérarchie et pivots (le code `Chest` en dépend)**
```
Coffre              origine au sol, au centre du corps (0 ; 0 ; 0)
├── Corps           le bois du corps — UN SEUL matériau (M_BoisCorps)
├── Ferrures        bandes, coins, charnières fixes, plaque de serrure (métal, laiton)
├── Interieur       fond et parois intérieures (M_Interieur)
└── Charniere       objet vide = l'axe de rotation, en (0 ; 0,8 ; 0,5) : arrière, en haut
    └── Couvercle   le couvercle complet (bois + ferrures), modélisé fermé
```
- **Pourquoi `Corps` n'a qu'un matériau** : au moment de la révélation, le code repeint tout
  le `Corps` en or (trésor) ou en rouge (piège). Si les ferrures faisaient partie du même
  objet, elles seraient repeintes aussi. En les séparant, seul le bois change de couleur et
  les ferrures restent en métal : c'est plus lisible et plus joli.
- **Pourquoi `Charniere` est un objet vide à l'arrière** : le couvercle tourne autour de son
  parent (rotation de 110° sur l'axe X). Son origine doit donc être exactement sur l'axe
  des charnières, sinon le couvercle « flotte » en s'ouvrant.

**Détails qui le rendent lisible en 3/4**
1. **Couvercle bombé** (demi-cylindre aplati, 6 segments sur l'arrondi) : c'est LA forme qui
   dit « coffre au trésor ». Bois foncé `#6B4226`.
2. **Deux bandes de métal verticales** (à ~25 % du bord gauche et droit), de 8 cm de large et
   2 cm de relief, qui passent sur le devant du corps **et** sur le dessus du couvercle (vu
   d'en haut, ce sont elles qu'on voit le mieux).
3. **Plaque de serrure en laiton** au centre de la face avant, en haut du corps, forme
   d'écusson (blason arrondi en bas), 26 × 30 cm (taille exagérée), 3 cm de relief, avec un
   trou de serrure en creux. Un petit fermoir en laiton descend du couvercle sur la plaque.
4. **Planches** : 3 planches horizontales sur la face avant et les côtés, séparées par des
   rainures de 2 cm de large et 1 cm de profondeur.
5. **Coins en métal** : petites équerres sur les 4 arêtes verticales avant (on ne voit pas
   celles de derrière, on peut s'en passer).
6. **Intérieur creux** : quand le couvercle est ouvert, la caméra voit dedans. Parois de 6 cm,
   fond intérieur à y ≈ 0,55, en `#3A2416`. On pourra plus tard y poser un tas de pièces
   ou un ressort-piège.
7. **Charnières visibles** : deux petits cylindres en métal sombre à l'arrière, sur l'axe
   (ils dépassent un peu en haut, on les aperçoit quand le coffre est ouvert).
8. Biseau de 3 cm sur toutes les arêtes du corps et du couvercle.

**Triangles visés** : **800 à 1 200** au total (corps + intérieur ~350, couvercle ~350,
ferrures + serrure + charnières ~400).

### 6.2 Le personnage (`Joueur`) — capsule avec des yeux

**À quoi il ressemble** : un petit pion de jeu de société vivant. Une capsule douce et
brillante, deux yeux noirs ronds avec un point de lumière qui lui donnent l'air curieux et
sympathique. C'est tout : sa personnalité vient des couleurs et des chapeaux.

**Dimensions (identiques au prototype)**
- Capsule de 1,00 de diamètre et 2,00 de haut. Origine **à la base**, au centre (0 ; 0 ; 0) ;
  le sommet de la tête est en y = 2,0.
- Variante « jouet » autorisée : la base peut s'élargir très légèrement (diamètre 1,05 en bas)
  pour un air plus stable, comme un pion. Pas plus.

**Hiérarchie**
```
Joueur              origine à la base (0 ; 0 ; 0)
├── Corps           la capsule — UN SEUL matériau (M_PersoCorps, recoloré par le shop)
├── OeilGauche      en (−0,18 ; 1,6 ; surface avant)
├── OeilDroit       en (0,18 ; 1,6 ; surface avant)
├── RefletGauche / RefletDroit   petits points blancs
└── PointChapeau    objet vide en (0 ; 2,0 ; 0)
```
- `Corps` seul porte la couleur du shop : les yeux et reflets sont des objets séparés pour ne
  jamais changer de couleur.
- **Coordonnées de texture (UV) du `Corps`** (nouveau, pour les skins à motif, §4.1) : une
  projection **cylindrique** (*UV → Cylinder Projection*, axe vertical), U qui fait le tour du
  corps, V qui va de la base (0) au sommet (1), sans étirement visible sur la face avant. Sans
  cette UV, les rayures du « Sucre d'orge » seraient de travers et les pois déformés. La couture
  (là où U revient de 1 à 0) est placée **à l'arrière**, que la caméra ne voit jamais.

**Détails qui le rendent lisible en 3/4**
1. **Yeux** : ovales légèrement verticaux (≈ 15 cm de large × 19 cm de haut, 6 cm d'épaisseur),
   **à moitié enfoncés** dans la capsule (pas collés dessus, pas flottants). `#111111`, brillants.
2. **Reflets** : dans chaque œil, un point blanc de 4 cm, en haut à gauche de l'œil (les deux
   reflets du même côté, comme la lumière principale). C'est le détail qui rend vivant.
3. Pas de bouche, pas de bras, pas de nez : la capsule doit rester un « porte-manteau »
   neutre pour les chapeaux.
4. Lissage complet (*Shade Smooth*), aucun biseau.

**Triangles visés** : **700 à 1 000** (capsule 24 segments × 16 anneaux ≈ 750, yeux et reflets
≈ 200).

### 6.3 Les chapeaux — règles communes

- Un objet racine par chapeau, nommé par son **identifiant du shop** (`chapeau_casquette`,
  `chapeau_fete`, `chapeau_haut_de_forme`, `chapeau_couronne`).
- **Origine = point d'accroche = sommet de la tête** (y = 0). Le chapeau sera posé sur
  `PointChapeau`. Comme le haut de la capsule est une demi-sphère de rayon 0,5, les chapeaux
  à bord **descendent un peu sous y = 0** (de −0,05 à −0,15) pour bien coiffer la tête au lieu
  de flotter dessus. Les positions de la spec du shop restent la référence.
- Avant = côté caméra (z négatif dans Unity), comme les yeux.
- Les dimensions reprennent celles de la spec (`docs/specs/shop.md` §4.2), arrondies et
  enrichies. Tous visibles **depuis le dessus** : soigner la face supérieure.
- Tous les volumes ont une épaisseur réelle (pas de surface simple face) : on voit parfois
  l'intérieur d'un bord.

### 6.4 `chapeau_casquette` — Casquette (100 pièces)

**À quoi elle ressemble** : la casquette de baseball de dessin animé, rouge, visière bien
large vers l'avant, petit bouton blanc sur le dessus.

- **Calotte** : demi-sphère aplatie (Ø 1,0, hauteur ~0,35 au-dessus de y = −0,1), 16 segments,
  rouge `#E04040`. 6 coutures suggérées par de fines arêtes (les côtes qui partent du bouton).
- **Visière** : vers l'avant, 0,60 de large × 0,45 de profondeur, **5 cm d'épaisseur**,
  légèrement **courbée** (bords plus bas que le centre, 4 segments en largeur), rouge foncé
  `#A02828`. C'est le détail qui dit « casquette » vu d'en haut : on l'exagère.
- **Bouton** : petite sphère aplatie Ø 0,10 au sommet, blanc `#FFFFFF`.
- **Triangles visés** : **250 à 400**.

### 6.5 `chapeau_fete` — Chapeau de fête (150 pièces)

**À quoi il ressemble** : le chapeau pointu d'anniversaire, rayé rose et jaune, avec un gros
pompon blanc, posé de travers.

- **Cône** : un vrai cône cette fois (16 côtés), base Ø 0,60, hauteur 0,55, creux (épaisseur
  3 cm visible au bord inférieur).
- **Rayures** : 4 bandes horizontales alternées rose `#FF6FB5` / jaune `#FFD54A` / rose / jaune
  (par faces, avec deux matériaux). Les rayures horizontales se lisent mieux d'en haut que
  des spirales.
- **Pompon** : sphère bosselée (icosphère subdivisée 1 fois, sommets légèrement décalés au
  hasard) Ø 0,20, blanc `#FFFFFF`, mat.
- **Inclinaison** : tout le chapeau penché de **10° sur le côté** (rotation autour de l'axe
  avant-arrière), comme dans la spec.
- **Triangles visés** : **200 à 350**.

### 6.6 `chapeau_haut_de_forme` — Haut-de-forme (250 pièces)

**À quoi il ressemble** : le chapeau de magicien de fête foraine, noir, haut, avec un ruban
rouge, un peu caricatural.

- **Bord** : Ø 1,10, épaisseur 5 cm, **relevé sur les côtés gauche et droit** (courbe douce, les
  côtés remontent de ~6 cm) : c'est ce qui le rend « magicien » plutôt que « tuyau ».
- **Tube** : Ø 0,70 en bas, **Ø 0,76 en haut** (il s'évase légèrement, exagération jouet),
  hauteur 0,70. Dessus plat avec biseau de 2 cm. Noir feutre `#1E1E1E`.
- **Ruban** : bande de 10 cm de haut à la base du tube, 1,5 cm de relief, rouge satiné `#C0392B`.
  Facultatif : un petit nœud plat du même rouge sur le côté gauche.
- Comme le feutre est très sombre, le liseré de lumière (contre-jour, §5) et le biseau du
  dessus sont importants pour qu'il ne devienne pas une tache noire.
- **Triangles visés** : **250 à 400** (24 segments).

### 6.7 `chapeau_couronne` — Couronne royale (500 pièces)

**À quoi elle ressemble** : la couronne de roi de conte de fées, en or brillant, avec cinq
pointes terminées par des perles et un gros rubis devant. L'objet le plus cher : il doit
avoir l'air le plus précieux.

- **Anneau** : Ø 0,80, hauteur 0,25, **épaisseur de paroi 4 cm** (on voit l'intérieur d'en
  haut), or `#FFC83A`. Un petit bourrelet en relief en haut et en bas de l'anneau.
- **Pointes** : 5 pointes triangulaires arrondies sur le dessus (hauteur 0,15), réparties tous
  les 72°, **la première bien au centre devant**.
- **Perles** : une sphère Ø 0,12 à 0,15 au bout de chaque pointe (la spec les place sur un
  cercle de rayon 0,38 à y ≈ 0,17 ; avec les pointes, elles montent à y ≈ 0,30 : l'intégrateur
  vérifiera qu'elles ne sortent pas du cadre). Or.
- **Rubis** : pierre taillée à facettes (octaèdre étiré, ~16 faces) Ø 0,12, sertie au centre
  de la face avant de l'anneau, rouge `#D62839` très brillant. Facultatif : un petit
  serti d'or autour.
- Biseau 2 cm, 2 segments (l'or a besoin de plus de facettes pour scintiller).
- **Triangles visés** : **500 à 800**.

### 6.8 Le sol de l'étage (`Etage`)

**À quoi il ressemble** : le sommet rond d'une tour de conte, une plateforme en dalles de
pierre bleutée, bordée d'une margelle plus claire, avec une rosace magique incrustée.

**Dimensions (identiques au prototype)** : disque de **Ø 10**, épaisseur **0,5**, **dessus à
y = 0** (origine au centre du dessus). 32 côtés (on voyait les facettes du disque actuel à
24 : c'est trop peu pour la bordure).

**Détails qui le rendent lisible en 3/4**
1. **Margelle** : anneau extérieur de 35 cm de large (de Ø 9,3 à Ø 10), légèrement surélevé
   (+4 cm), biseauté, en pierre claire `#8A90B0`, découpé en 16 blocs par des joints.
2. **Dalles** : à l'intérieur, 3 couronnes concentriques de dalles (cercle central Ø 3, puis
   jusqu'à Ø 6, puis jusqu'à Ø 9,3), chaque couronne découpée en 8 à 16 dalles. Joints de
   3 cm de large, 2 cm de creux, en `#3C4258`. Chaque dalle a un biseau de 2 cm et une
   hauteur qui varie de ±1 cm (à peine visible, mais la lumière accroche).
   Environ 1 dalle sur 3 en teinte variante `#535A75`.
3. **Rosace** : une étoile à 8 branches incrustée dans le cercle central (Ø 2,6), en filets de
   pierre claire `#8A90B0` et violet `#6A4C9C`, à fleur du sol (relief 0 à 5 mm, pas plus :
   les coffres et le personnage s'y posent). Elle est en partie cachée par les coffres :
   c'est voulu, elle donne du « magique » sans voler la vedette.
4. **Flanc** : le côté vertical du disque, en `#434A63`, découpé en gros blocs (2 rangées
   décalées, comme un mur de tour). Visible en bas de l'écran.
5. La moitié avant (côté caméra) est vide et visible : c'est là que se lisent le mieux les
   dalles. Ne rien y mettre de haut (le message et les boutons de l'interface sont dessus).

**Triangles visés** : **2 000 à 3 500**.

---

## 7. Récapitulatif des budgets

| Objet | Triangles | Matériaux |
|---|---|---|
| Coffre (×3 dans la scène) | 800 – 1 200 | BoisCorps, BoisCouvercle, (BoisClair), Interieur, MetalSombre, Laiton, TrouSerrure |
| Personnage | 700 – 1 000 | PersoCorps, Yeux, Reflet |
| Casquette | 250 – 400 | TissuRouge, TissuRougeFonce, Blanc |
| Chapeau de fête | 200 – 350 | CartonRose, CartonJaune, Blanc |
| Haut-de-forme | 250 – 400 | Feutre, Satin |
| Couronne | 500 – 800 | Or, Rubis |
| Sol de l'étage | 2 000 – 3 500 | PierreDalle, PierreDalleVariante, PierreJoint, PierreClaire, PierreFlanc, Incrustation |

Total de la scène visible : moins de 10 000 triangles. Large marge pour la suite (dé, roue,
pièce géante, tour).

---

## 8. À éviter

**Style**
- Le réalisme : veinage de bois détaillé, rouille, usure, saleté, rayures, proportions réelles.
- Les objets fins ou pointus (< 4 cm) : ils scintillent et disparaissent à la distance de la caméra.
- Les arêtes vives sans biseau sur les objets durs ; à l'inverse, les facettes visibles sur
  le personnage et les objets ronds.
- Trop de détails : si on ne le voit pas depuis la caméra en 3/4, on ne le modélise pas
  (dessous des objets, arrière du coffre, intérieur des chapeaux au-delà du bord).
- Les subdivisions lourdes (*Subdivision Surface* appliqué) : on est en low-poly, les budgets
  du §7 sont des plafonds.

**Couleurs et matériaux**
- Le rouge vif et l'or métal en grande surface hors signaux de jeu (ils sont réservés au
  piège et au trésor).
- Le noir pur et le blanc pur en grande surface ; les couleurs « néon » saturées à 100 %.
- Inventer de nouvelles couleurs pour les objets du shop : on reprend les hex de la spec.
- Metallic 1 sans Reflection Probe (rendu terne ou noir).
- Les images dessinées ou générées plaquées sur les modèles, et les couleurs de sommets (pas
  affichées par URP Lit). Seule exception : les motifs de skins fabriqués par le code (§4.1).
- L'émission « pour faire joli » : réservée aux objets Légendaires et Secrets (§4.1, plafond
  1,5) et aux futurs effets de victoire. Jamais sur le décor ni sur les coffres.
- La transparence, sauf le skin Cristal (opacité ≥ 0,55).
- Les couleurs de rareté utilisées comme décoration (§10).

**Contenu**
- **Tout symbole d'argent réel** : billets, signes `$` `€` `£`, cartes bancaires, jetons de
  casino réalistes, machines à sous. Nos pièces sont des pièces de conte (étoile ou tour
  frappée dessus), pas de la monnaie. C'est la règle d'or du projet.
- Logos, marques, textes, lettres ou chiffres sur les modèles.
- Toute ressemblance avec un personnage existant : pas de visière-hublot sur la capsule
  (trop proche d'un personnage de jeu célèbre), pas de moustache, pas de casquette « M »,
  pas de couronne copiée d'un jeu connu. Pour les boîtes : masque de héros **sans emblème ni
  lettre** (simple bandeau bleu), chapeau de sorcier **sans visage ni pli qui forme un visage**,
  lunettes de soleil sans logo sur les branches, boîtes cadeaux sans aucune inscription.
- Le macabre et l'effrayant : le piège reste « cartoon » (ressort, nuage, pétard), jamais de
  sang, de crâne réaliste, de pièges qui blessent.
- Les objets qui traversent la tête du personnage ou flottent au-dessus : chaque chapeau est
  testé sur la capsule.

---

## 9. Pour les images (Gemini)

Les prompts sont écrits dans `art/prompts/<lot>.md` (`lot1.md` : concept, 1res icônes, logo,
ciel ; `lot2.md` : icônes des boîtes, boîtes, ciels des paliers). Le **premier prompt de chaque
conversation Gemini** commence par le **bloc de style commun** ci-dessous, recopié mot pour mot.
On ne le modifie pas d'un lot à l'autre sans mettre à jour cette section.

**Prompts courts de suite (depuis le lot 2)** : dans une même conversation, les images
suivantes utilisent un prompt court qui commence par *« Same image as the previous one… only
change… »* et ne décrit que ce qui change. Gemini garde le style, l'angle, la lumière et le fond
de l'image précédente : c'est ce qui a donné des capsules et des chapeaux identiques au lot 1.
Si une image dérive (autre angle, autre fond, style qui change), on ouvre une nouvelle
conversation, on joint l'icône de référence du lot 1, et on recolle le prompt complet de
relance fourni dans le fichier du lot.

**Image de référence jointe** : chaque conversation commence en joignant une image déjà
validée (bouton « + » de Gemini) : `couleur_rouge.png` pour les skins, `chapeau_casquette.png`
pour les accessoires et les boîtes, `ciel_nuit.png` pour les ciels.

### 9.1 Bloc de style commun (en anglais)

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.
```

Seule exception : le logo avec titre, qui lève la règle « no text » pour le seul texte
« Tour de la Chance ».

### 9.2 Conventions par type d'image

| Type | Format | Fond | Angle |
|---|---|---|---|
| Concept art, scènes | 16:9 | violet nuit `#1F1730` avec halo `#3A2850` | la caméra du jeu : 3/4 plongeant ~35° depuis l'avant |
| Icônes du shop et de l'interface | carré 1:1 | **uni lavande très clair `#E6E1EE`**, sans dégradé ni ombre portée | 3/4, ~25° au-dessus, objet tourné ~20° vers la gauche, centré, ~70 % du cadre |
| Logo, emblème | 16:9 (logo) / carré (emblème) | uni violet nuit `#1F1730` | de face, symétrique |
| Fond de ciel | 16:9 (1376 × 768 en sortie de Gemini) | — (c'est le fond) | sombre, peu contrasté, centre légèrement plus clair, bas le plus sombre ; un fond par palier d'altitude (§5.1) |
| Icône d'un objet **très clair ou lumineux** (Blanc nacré, Petites ailes d'ange, Auréole) | carré 1:1 | **uni bleu-gris moyen `#5B6380`** | comme les autres icônes |
| Effets de révélation (halo, rayons, étincelle) | — | — | **pas d'image** : dessinés par le code (§10.3) |

**Pourquoi un 2e fond pour les objets clairs ?** Le blanc nacré `#EDE6F2`, le blanc cassé des
ailes `#F4EEE6` et le jaune pâle lumineux de l'auréole `#FFF1A8` sont presque de la même
valeur que le lavande `#E6E1EE` : le détourage mangerait leurs bords. Sur un bleu-gris moyen,
ils se détachent nettement. Une fois détourées, les icônes n'ont plus de fond : la différence
ne se voit pas dans le jeu, tant que l'angle, le cadrage et la lumière sont les mêmes.

**Pourquoi un fond lavande clair pour les icônes et pas le violet nuit ?** Les icônes doivent
être détourées (fond rendu transparent) pour aller dans les cartes du shop. Un fond violet
sombre se confondrait avec la capsule violette et le haut-de-forme noir ; le lavande très
clair ne ressemble à aucun objet du shop (ni or, ni violet, ni blanc pur du pompon).

### 9.3 Règles de contenu pour les images
- Les objets du shop reprennent exactement la forme et les couleurs des fiches §6 et de
  `docs/specs/shop.md` : les icônes doivent ressembler aux modèles 3D, pas l'inverse.
- Capsule : seulement deux yeux noirs avec reflet ; ni bouche, ni bras, ni visière-hublot, ni sac à dos.
- Casquette vierge (aucune lettre ni logo sur le devant).
- Pièce d'or : motif « tour + étoile », jamais de chiffre, de signe monétaire ou de visage.
- Le texte d'un titre généré se vérifie lettre par lettre ; en cas d'échec, emblème sans texte
  + titre écrit par l'interface (or `#FFD54A`, contour `#3A2200`).
- Les images ne sont **pas** plaquées sur les modèles 3D (§3.1) : elles servent à l'interface,
  au fond et à la référence de style.
- Les 16 icônes de skins montrent **la même capsule** que `couleur_rouge.png` (corps en pilule,
  socle rond de pion, deux yeux noirs avec reflet) : seule la matière change.
- Les icônes d'accessoires montrent l'objet **seul**, sans personnage ni tête, avec le même
  cadrage que les chapeaux du lot 1. Les objets qui entourent le cou (écharpe, collier) sont
  montrés en anneau vide « comme autour d'un cou invisible ».
- Les icônes ne montrent **jamais la rareté** (pas de cadre coloré, pas d'étoiles, pas de halo) :
  c'est l'interface qui ajoute le liseré de rareté (§10). Une même icône sert dans l'Index, en
  silhouette sombre, et doit donc avoir une silhouette lisible.

---

## 10. Les couleurs de rareté dans l'interface

### 10.1 Où on les utilise (et seulement là)

| Élément d'interface | Couleur utilisée |
|---|---|
| Liseré de 3 px autour d'une carte d'objet (Inventaire, Index, résultat) | couleur de base (§2.7) |
| Pastille ● du tableau des taux de drop | couleur de base |
| Nom de la rareté écrit en petit sur une carte, dans le tableau des taux et sur l'écran de résultat | couleur **« texte »** (§2.7) |
| Halo, rayons, étincelles et flash coloré de la révélation | couleur de base (Secret : arc-en-ciel animé) |
| Texte « Rare ! », « LÉGENDAIRE ! »… de la révélation | couleur « texte », avec le contour sombre `#3A2200` des textes or |

On ne les utilise **pas** pour : le fond d'un bouton, le fond d'un panneau, un onglet, une barre
de progression (elle reste or `#FFD54A`), les icônes elles-mêmes.

### 10.2 Règles de lisibilité
1. **Jamais la couleur seule** : le nom de la rareté est toujours écrit à côté (daltonisme,
   `boites.md` §5.1).
2. **Couleur « texte » plus claire pour Rare et Super rare.** Sur le panneau `#2A2A3A`, le bleu
   `#3B8BEB` (contraste ≈ 4:1) et le violet `#A35CE0` (≈ 3,5:1) sont trop sombres pour un petit
   texte. On écrit donc le nom de ces deux raretés en `#5FA2F0` et `#B57DE8` (contraste ≥ 4,5:1).
   Les liserés, pastilles et halos gardent la couleur de base. Les quatre autres couleurs passent
   déjà (≥ 4,9:1). Le plus simple pour le programmeur : une classe USS par rareté
   (`.rarete-rare { color: #5FA2F0; border-color: #3B8BEB; }`), avec le **même** identifiant que le
   catalogue.
3. Noms de rareté en **gras**, taille 13 px minimum.
4. **Bleu Rare ≠ bouton « Équiper ».** Le bleu Rare `#3B8BEB` est proche du bleu du bouton
   « Équiper » `#3B7DD8`. Ce n'est pas gênant tant que la rareté n'apparaît que sous forme de
   liseré fin et de texte, et le bouton sous forme de **fond plein** : c'est une raison de plus
   pour ne jamais remplir un bouton avec une couleur de rareté.
5. **Hiérarchie** : plus c'est rare, plus c'est chaud et lumineux (gris → vert → bleu → violet →
   orange → rose/arc-en-ciel). On ne réordonne pas ces couleurs.
6. **Silhouette** d'un objet non obtenu : l'icône détourée teintée `#15121F` à 0,9 d'opacité
   (`-unity-background-image-tint-color`), sur le fond de case `#2A2A3A` ; le liseré de rareté
   reste visible (on sait quelle rareté il manque). Secret non obtenu : un grand « ? » en
   `#FF4FD8`.
7. **L'arc-en-ciel** est réservé au Secret, et seulement pendant la révélation et sur son
   écran de résultat. Partout ailleurs, le Secret est rose `#FF4FD8`.

### 10.3 Halo, rayons, étincelles : faits en code, pas en image

`boites.md` §12 prévoyait trois images (`ui_halo.png`, `ui_rayons.png`, `ui_etincelle.png`).
**Décision du DA : on ne les génère pas avec Gemini**, on les dessine par le code. Raisons :
- Gemini ne sait pas produire un PNG transparent ; détourer un dégradé flou (le halo) laisse un
  bord sale.
- Le nombre de rayons change selon la rareté (8, 12, 16, 20) : une seule image à 8 rayons ne
  conviendrait pas.
- Ce sont des formes géométriques simples, plus nettes en code, et teintables à volonté.

Ce que le programmeur dessine (blanc, teinté ensuite par la couleur de rareté) :
| Effet | Forme | Comment (au choix) |
|---|---|---|
| Halo | disque flou : opacité 1 au centre, 0 au bord, décroissance douce (`(1 − r)²`) | `Texture2D` 128 × 128 calculée au démarrage, comme les motifs de skins |
| Rayons | N triangles fins partant du centre (N = 8, 12, 16 ou 20), chacun ~10° d'ouverture, bout extérieur qui s'efface | `Painter2D` de UI Toolkit (`generateVisualContent`), ou `Texture2D` calculée par N |
| Étincelle | étoile à **4 branches** aux côtés creusés (même forme que les scintillements du ciel), Ø 16 à 28 px | `Painter2D`, ou `Texture2D` 32 × 32 calculée |

Style : bords doux, pas de dégradé multicolore dans une même forme (la couleur vient de la
teinte), opacité max du halo comme dans `boites.md` §7.6. Les étincelles du Légendaire
alternent `#FF9F1C` et l'or `#FFC83A` ; celles du Secret prennent des teintes de l'arc-en-ciel.
