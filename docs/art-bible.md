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

>
> **Mise à jour « lot 3 » (v3, arbitrée par le DA)** — les demandes de `docs/specs/v3.md` sont
> tranchées :
> - le personnage devient un **cheval de jeu d'échecs jouet** : nouvelle fiche §6.2 (rédigée
>   d'après le modèle `blender/personnage_cheval.py` et les rendus `docs/captures/cheval_*.png`) ;
> - le **palier 1 passe en plein jour**, avec un ciel bleu nettement plus franc que ce que
>   proposait la spec (demande de l'auteur), puis coucher de soleil, crépuscule et nuit : §1,
>   §2.9 et §5.1 ;
> - les **5 raretés de coffre** (Bois, Bronze, Argent, Or, Diamant) : §2.10 (une retouche : le
>   corps du Bronze) ;
> - le **masque de héros** change de couleurs (il disparaissait sur le corps bleu) : §2.8 ;
> - le **style des boutons** (biseau, reflet, épaisseur, survol) : §11 ;
> - les effets des coffres précieux et la cinématique Secret restent **dessinés par le code**
>   (aucune image, aucune émission sur les coffres) : §10.3 ;
> - nouvelles images : `art/prompts/lot3.md`.

>
> **Mise à jour « lot 4 » (v4, interface)** — l'auteur veut une interface « dans l'esprit des
> jeux Roblox », sans rien copier de la marque. Nouvelle section **§12 « Interface v4 »** :
> palette UI et variables USS, police **Fredoka** (OFL), boutons à contour épais et socle,
> boutons ronds d'icône (colonnes gauche et droite, Paramètres, Fermer), **pop-ups centrées**,
> onglets, cartes à liseré de rareté (4 px), **barre de défilement stylée**, **badge d'étage**,
> compteurs, toasts, pop-ups Paramètres et Classement. Elle remplace la réalisation du §11 et le
> §2.5. Nouvelles images (icône Classement, roue dentée, médailles, icônes de son) :
> `art/prompts/lot4.md`.

>
> **Mise à jour « lot 5 » (v5, trois chantiers de style)** :
> - **§13 « Accessoires v5 »** : pourquoi les 16 accessoires ont l'air « vieillots », les règles
>   chiffrées du nouveau style (3 niveaux de formes, gros biseaux + normales pondérées, contraste
>   de matières, animations, effets par rareté, budgets revus) et une fiche de refonte par
>   accessoire. Elle modifie légèrement §4.1 (émission discrète permise au Super rare) et §7.
> - **§12.17 « Boutons d'icône v5 »** : boutons ronds sans texte, icône seule en grand, nouveau
>   bouton **Rebirth** sous le Classement, info-bulle au survol, plus d'icône en haut à gauche
>   des pop-ups. Remplace la partie « libellé » du §12.5 et l'icône d'en-tête du §12.6.
> - **§14 « Ouverture des boîtes en 3D »** : la boîte 3D, sa fiche de modèle, la chronologie
>   chiffrée et les règles d'équité.
> - Nouvelles images : `art/prompts/lot5.md` (icône Diamant, icône Rebirth, packs de diamants).

>
> **Mise à jour « v5.1 » (retouches d'interface)** — nouvelle section **§12.18 « Pop-ups et
> cartes façon jeu Roblox »** : fond blanc cassé à motif en filigrane, titre qui déborde avec
> sa grande icône (l'icône d'en-tête **revient**, elle annule le §12.17.6), cartes en dégradé
> vif, pilule de prix verte, badges de taux, messages importants, Boutique en une seule page.
> Elle remplace les lignes « Cadre », « En-tête », « Icône d'en-tête » et « Titre » du §12.6.

>
> **Mise à jour « v6 » (chantiers visuels)** — nouvelle section **§15 « Interface v6 »** :
> les **bulles des boutons** deviennent des **images rendues dans Blender** (boutons ronds des
> colonnes, croix de fermeture, roue dentée, Rejouer / Continuer / Encaisser en 9-slice), une
> **couleur de face par fonction** (elle remplace l'anneau du §12.17), le placement des colonnes
> et de la barre du haut, les onglets de la Boutique, les nouvelles icônes Boutique et
> Paramètres, la **refonte de la Renaissance**, la **Route des trophées**, les flèches et l'indice
> « glisse pour tourner » de la fiche agrandie, et le choix **Graphismes** des Paramètres.
> Elle remplace les §12.17.1, §12.17.2, §12.17.5 (aspect des boutons ronds) et le §12.18.2
> (croix « inchangée » : sa forme reste, son rendu change). Nouvelles images :
> `art/prompts/lot7.md`.

---

## 1. L'ambiance en une phrase

**Une fête foraine magique au sommet d'une tour, du plein jour à la nuit étoilée : un cheval
d'échecs jouet et des coffres en bois, en métal et en or, posés sur une pierre bleutée,
éclairés par une lumière chaude.**

Trois mots-clés : **jouet**, **chaleureux**, **lisible**.

**Depuis la v3, le temps passe en montant** : en bas de la tour il fait grand jour (ciel bleu,
nuages blancs), dans les nuages le soleil se couche, au-dessus c'est le crépuscule, et tout en
haut la nuit étoilée. Ce qui ne change jamais : le décor reste **plus froid et plus calme** que
les objets de jeu, et les coffres chauds ressortent sur tous les ciels (le bleu du jour et
l'orange du bois sont des couleurs opposées : c'est le contraste idéal).

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
**v4** : la palette de l'interface est remplacée par celle du §12.2 (panneaux `#2E2552`,
contours `#1F1730`).

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
| Pois de fête | `#F27BB0` + pois `#F4EEE6` | Masque de héros | **v3 :** `#D63A6E` framboise, bord `#FFD54A` (voir ci-dessous) |
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

> **Arbitrage du DA (v3) — masque de héros.** Le bleu héros `#3A5BD9` et son bord `#22357F`
> se voyaient à peine sur le cheval bleu ciel `#4A90D9` (même teinte, valeur proche : voir
> `docs/captures/cheval_planche.png`). Nouvelles couleurs :
> - **bandeau framboise `#D63A6E`** (satiné, smoothness 0,5) : une teinte chaude opposée au bleu
>   par défaut, qui se lit aussi sur le vert, le jaune, le turquoise, l'argent et la galaxie ;
> - **bord jaune or `#FFD54A`** (le jaune du chapeau de fête et de l'interface), d'au moins
>   **4 cm**, tout autour du bandeau **et autour des deux trous** : c'est lui qui garantit la
>   lecture sur les skins proches (Rose bonbon, Pois de fête, Sucre d'orge, Rouge tomate), et il
>   encadre les yeux noirs au lieu de les noyer.
>
> Pourquoi pas un masque noir, le grand classique ? Les yeux du cheval sont noirs `#111111` : un
> bandeau sombre autour les ferait disparaître. Pourquoi pas rouge vif ? Le rouge vif est réservé
> au piège (§2.6). Le framboise reste un petit accent porté par le personnage, comme le rubis.
> Le modèle 3D (remodelé pour épouser la tête du cheval, `v3.md` §16) et l'icône
> `masque_heros.png` (lot 3) prennent ces couleurs. Toujours **sans emblème ni lettre**.

### 2.9 Décor par palier d'altitude (v3 : du jour à la nuit)

Valeurs à utiliser pour les images de fond (prompts) et dans Unity (`AltitudeDecor`). Les
colonnes « Lumière » et « Ambiance » reprennent la spec `v3.md` §9, **sauf le palier 1**, que
le DA rend plus bleu à la demande de l'auteur.

| Palier | Étages | Ciel (image) : haut → centre → bas | Accents de l'image | Lumière principale | Ambiance (ciel / horizon / sol) | Fond caméra (si l'image manque) |
|---|---|---|---|---|---|---|
| 1 « En plein jour » | 1 – 3 | bleu ciel franc `#5FAEE8` → bleu clair `#A6D4F4` → bleu-gris lointain `#5A7598` | nuages blancs `#F4F8FC`, ombres des nuages `#B8CDE6`, fête foraine lointaine aux couleurs pastel voilées de bleu | `#FFF4E0`, **1,35** | `#BFDDF5` / `#90A8C8` / `#4A5470` | **`#7DBDEC`** (au lieu de `#8EC5EC`) |
| 2 « Dans les nuages ! » | 4 – 6 | pêche `#F2B9A0` → rose poudré `#C98A96` → prune `#4A3040` | soleil couchant `#FFE2B0` bas **à droite**, dessus des nuages `#F7D3C2`, creux `#B07A88` | `#FFD7A8`, 1,2 | `#F0B9A0` / `#B07A88` / `#4A3040` | `#C98A96` |
| 3 « Au-dessus des nuages ! » | 7 – 9 | violet-bleu `#2E2850` → `#6A5A98` → mer de nuages `#9A90D0` puis `#2E2850` tout en bas | fine bande rose-orangé `#E8A48C` à l'horizon (plus vive à droite), croissant de lune `#F0ECFF` en haut à droite, premières étoiles `#FFF1D6` | `#E6E0FF`, 1,15 | `#9A90D0` / `#6A5A98` / `#2E2850` | `#3E3668` |
| 4 « Dans les étoiles ! » | 10 et + | inchangé (`ciel_palier4.png`) | aurore verte et violette en haut | `#F0E6FF`, 1,1 | `#6A5AB0` / `#3A6A80` / `#1A1430` | `#1F1730` |

**Arbitrage du palier 1.** La spec demandait un « bleu moyen un peu sourd » plafonné à
`#D6ECFA` ; l'auteur veut un ciel **bien plus visible**. Décision :
- le **haut** du ciel est un vrai bleu ciel saturé `#5FAEE8` (on comprend tout de suite « il fait
  beau ») ; le **centre**, derrière la tête du cheval, est plus clair et plus doux `#A6D4F4` ;
- **pourquoi le centre plus clair** : depuis la caméra, la tête du cheval se découpe sur le ciel.
  Le cheval par défaut est bleu `#4A90D9` : sur un centre bleu foncé, il disparaîtrait. Sur un
  bleu pâle, il est nettement plus sombre que le fond (contraste ≈ 2:1, renforcé par le liseré lavande du contre-jour) et sa silhouette se lit ;
- les **nuages blancs** ont le droit d'être presque blancs (`#F4F8FC`), mais **seulement en haut
  et sur les côtés** (jamais au centre, jamais en bas derrière les boutons) ;
- le **bas** de l'image reste le plus sombre (`#5A7598`, collines et fête foraine lointaines
  voilées de bleu) : les boutons et messages s'y lisent ;
- rien de rouge vif ni d'or dans le ciel (signaux réservés) : la fête foraine lointaine est en
  pastels voilés.

**La course du soleil** donne la cohérence entre les 4 images : au palier 1 il est hors champ en
haut à gauche (comme la lumière principale du jeu) ; au palier 2 il se couche bas **à droite**
(le côté gauche de l'écran porte la colonne de boutons, on le garde calme) ; au palier 3 il ne
reste qu'une lueur rose-orangé à l'horizon, plus vive à droite, et un fin croissant de lune en
haut à droite ; au palier 4, la nuit.

**Les ciels existants** : `ciel_palier4.png` est **gardé** tel quel. `ciel_palier2.png` (brume
lavande de nuit) et `ciel_palier3.png` (pleine lune bleu nuit) sont **remplacés** par le lot 3
(on garde les anciens fichiers en `_v2` pour mémoire, voir `lot3.md`). `ciel_nuit.png` n'est plus
utilisé par les paliers mais reste dans le projet (il va bien avec le logo et l'écran titre).

### 2.10 Les 5 raretés de coffre (v3, repris de `v3.md` §5.3.1, une retouche)

| Rareté | Corps fermé | Ferrures, coins, serrure | Détail (modèles 3D et icônes) | Couleur « UI » (étincelles, halo, étiquette) |
|---|---|---|---|---|
| Bois | `#8C5933` | métal sombre `#4A4E5A`, serrure laiton `#C99A1E` | — | — (pas d'effet) |
| Bronze | **`#7E3F28` acajou** (au lieu de `#7A4A2E`) | bronze `#B0703A`, métal 1, smoothness 0,55 | rivets ronds sur les bandes | `#B0703A` |
| Argent | bleu nuit peint `#3E5C8A` | argent `#C9CED8`, métal 1, smoothness 0,85 | étoile en relief sur la serrure | `#C9CED8` |
| Or | violet royal laqué `#5B2D82` | doré `#E8B830`, métal 1, smoothness 0,8 | frise sur le bord du couvercle, 2 pierres `#D62839` | `#E8B830` |
| Diamant | nacre glacée `#E6F0F7`, smoothness 0,7 | argent `#C9CED8` | 5 gemmes taillées `#9FE3FF` (smoothness 0,95) sur le couvercle et les coins | `#9FE3FF` (alterné avec `#F4EEE6`) |

- **Retouche du Bronze** : `#7A4A2E` était presque le même brun que le Bois `#8C5933` ; tant que
  le programmeur ne fait que reteinter le modèle actuel (avant les modèles §16), on n'aurait vu
  la différence qu'aux ferrures. L'acajou `#7E3F28`, plus rouge et plus sombre, se distingue au
  premier coup d'œil, sans approcher le rouge piège `#D91A1A` (beaucoup plus vif et plus clair).
- **Progression lisible** : brun → brun-rouge → bleu → violet → blanc glacé ; métal sombre →
  bronze → argent → doré → argent + gemmes. Chaque palier change **deux** choses (corps et
  métal), donc on reconnaît la rareté même en silhouette colorée de 64 px.
- Le doré des ferrures Or `#E8B830` reste différent de l'or trésor `#FFC83A` et ne couvre que de
  petites surfaces. Le trésor (`#FFC83A`) et le piège (`#D91A1A`) se lisent sur les 5 corps.
- Le Diamant n'émet pas de lumière : son éclat vient de sa nacre claire, de ses gemmes très
  lisses et des étincelles UI (§10.3). **Jamais d'émission sur un coffre** (§4.1).
- Ces couleurs de coffre **ne sont pas** les couleurs de rareté des objets des boîtes (§2.7) :
  on ne les mélange pas (l'argent des coffres n'a rien à voir avec le bleu « Rare »).

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

**v5 (§13.6)** : un accessoire **Super rare** a droit à une émission **discrète** (intensité
**≤ 0,5**) sur de **petits détails** (au plus 10 % de sa surface visible : une pierre, des taches
d'ailes). Les objets Légendaires et Secrets gardent le plafond de 1,5. Les **effets de
l'ouverture des boîtes** (faisceau, lueur intérieure, étincelles 3D, §14) ont aussi droit à
l'émission (≤ 1,5) : ce sont des effets de victoire, pas des matières d'objets.

**Ce qui reste interdit** : l'émission pour un objet Commun à Rare ou « pour faire joli »,
les images dessinées ou générées plaquées sur un modèle, la transparence ailleurs que sur le
Cristal, les couleurs de sommets.

---

## 5. Éclairage et rendu

Objectif : une lumière **chaude de fête foraine** sur les objets, une **nuit violette** autour.

| Élément | Réglage indicatif |
|---|---|
| Lumière principale (Directional Light) | Couleur `#FFF1D6` (blanc chaud, déjà proche de l'actuelle), intensité **1,2**, venant d'en haut à gauche et de devant (rotation actuelle conservée, ~50° de hauteur). Ombres douces (*Soft Shadows*). |
| Lumière d'ambiance (Lighting → Environment) | Mode **Gradient** : ciel `#8C7AB8`, horizon `#5A4A70`, sol `#2A2030`. Elle teinte légèrement les ombres en violet et évite les ombres noires. |
| Contre-jour (**obligatoire depuis la v5**, §13.4) | Une Point Light `#B48CFF`, intensité 0,6, derrière et au-dessus du personnage (≈ (0 ; 3,5 ; 4)) : un liseré violet détache le personnage et les chapeaux du fond. |
| Reflets | Une **Reflection Probe** au centre de l'étage (ou le ciel en Gradient comme source). **Indispensable** : sans elle, les matériaux metallic 1 (or, laiton, couronne) paraissent ternes ou noirs. |
| Fond de caméra | *Solid Color* `#1F1730`. |
| Post-traitement (Volume global) | Tonemapping **Neutral** (respecte nos codes hex) ; Bloom léger (seuil 1,0, intensité 0,3) pour faire briller l'or ; Vignette 0,25 pour concentrer le regard au centre. |

Règle : on ne règle **pas** les couleurs des modèles pour compenser un mauvais éclairage. Si un
objet paraît faux en jeu, on corrige l'éclairage d'abord.

### 5.1 Le décor change avec l'altitude (v3 : `v3.md` §9, couleurs au §2.9)

Le tableau ci-dessus (lumière `#FFF1D6`, ambiance violette) est le **réglage de référence** des
images et des icônes. **Dans le jeu**, chaque palier remplace l'image du fond, la lumière
principale, la lumière d'ambiance, la couleur de fond de caméra et les nuages 3D (valeurs au
§2.9). Le sol, les coffres, le personnage, le contre-jour et le post-traitement **ne changent
pas**.

| Palier | Étages | Image de fond | Idée visuelle | Valeur générale |
|---|---|---|---|---|
| 1 | 1 – 3 | `ciel_palier1.png` **(nouveau, lot 3)** | **plein jour** : ciel bleu franc, gros nuages blancs **au-dessus** et au loin, fête foraine minuscule tout en bas | **la plus claire** des 4 |
| 2 | 4 – 6 | `ciel_palier2.png` **(refait, lot 3)** | **dans** les nuages au **coucher du soleil** : brume pêche et rose, soleil bas à droite | moyenne-claire |
| 3 | 7 – 9 | `ciel_palier3.png` **(refait, lot 3)** | **au-dessus** de la mer de nuages au **crépuscule** : haut violet-bleu, bande rose-orangé à l'horizon, premières étoiles, croissant de lune en haut à droite | sombre |
| 4 | 10 et + | `ciel_palier4.png` (gardé) | **dans les étoiles** : nuit profonde, aurore douce verte et violette | la plus sombre |

Règles communes aux 4 fonds :
- La montée se lit **de bas en haut** (nuages au-dessus → autour → dessous → presque disparus)
  **et dans le temps** (jour → coucher → crépuscule → nuit).
- **Centre calme et sans détail net** (c'est là que se découpent le cheval et les coffres) ;
  soleil, lune, aurore et gros nuages blancs sont **en haut ou sur les côtés**.
- Le **bas** reste le plus sombre de l'image (barre de boutons, panneau, messages).
- **Plafonds de clarté** : palier 1, rien de plus clair que `#A6D4F4` au centre (les nuages
  blancs `#F4F8FC` seulement en haut et sur les côtés) ; palier 2, rien de plus clair que
  `#F7D3C2` hors du disque du soleil ; paliers 3 et 4, rien de plus clair que `#9A90D0` hors de
  la lune et des étoiles.
- **Lisibilité de l'interface sur les ciels clairs** (paliers 1 et 2) : « Étage N » reçoit un
  contour de 2 px et une ombre portée (`text-shadow: 0 3px 0 rgba(31,23,48,0.6)`), la bannière
  de montée un fond `#1F1730` à 55 % (`v3.md` §9). Les panneaux et compteurs ont déjà un fond
  sombre : on ne change rien d'autre.
- Mêmes nuages « pouf » arrondis et low-poly sur les 4 images (même famille que le nuage du
  piège et les nuages 3D), mêmes étoiles et scintillements à 4 branches la nuit.

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

### 6.2 Le personnage (`Joueur`) — le cheval de jeu d'échecs jouet (v3)

> Fiche réécrite par le DA d'après le modèle livré (`blender/personnage_cheval.py`) et ses
> rendus (`docs/captures/cheval_face.png`, `cheval_3_4.png`, `cheval_planche.png`). Elle
> remplace la capsule des versions 1 et 2.

**À quoi il ressemble** : le **cavalier d'un jeu d'échecs en bois peint**, devenu vivant et
mignon. Un socle rond de pion, un cou épais et rond qui monte en se bombant au poitrail, une
tête de cheval aux formes de « boîte arrondie » avec un **long museau doux** qui avance vers
nous et pique un peu du nez, deux **grands yeux noirs brillants** haut sur le front, deux
petites **narines** sombres au bout du museau, deux **oreilles** courtes et épaisses, et une
**crinière sculptée** en grosses mèches arrondies qui part du front, passe entre les oreilles et
descend le long de la nuque. **Il regarde de face**, vers la caméra : on voit toujours ses deux
yeux. Sa personnalité vient toujours des couleurs et des accessoires.

**Pourquoi de face et pas de profil comme un vrai cavalier d'échecs ?** De profil, on ne verrait
qu'un œil depuis la caméra, et les lunettes ou le masque n'auraient plus de sens. De face, le
museau qui avance suffit à dire « cheval » (testé à 28° de rotation : moins lisible, on garde 0°).

**Dimensions** (`v3.md` §7.2)
- Hauteur totale **2,0 m** (pointe des oreilles), comme l'ancienne capsule. Origine **à la
  base**, au centre (0 ; 0 ; 0) : l'écrasement des animations garde le socle au sol.
- Socle Ø **1,05**, ~0,25 de haut, avec une collerette de pion (deux anneaux étagés).
- Cou : ~0,75 de large au poitrail, ~0,53 sous la mâchoire (le plus fin, à y ≈ 1,25).
- Crâne ~0,62 de large ; museau ~0,41 de large, qui dépasse d'environ 0,6 devant le cou.
- Yeux à y ≈ **1,68**, x = ±0,165 (au-dessus de 1,2 m : visibles derrière les couvercles ouverts).

**Hiérarchie** (les noms sont ceux que le code cherche)
```
Joueur              origine à la base (0 ; 0 ; 0)
├── Corps           socle + cou + tête + museau + oreilles + crinière — UN SEUL matériau
│                   (M_PersoCorps, repeint par le shop), UV cylindriques, couture à l'arrière
├── OeilGauche / OeilDroit       M_Yeux #111111, brillants
├── RefletGauche / RefletDroit   M_Reflet #FFFFFF
├── Narines         deux ovales sombres au bout du museau (M_Yeux, ne changent jamais)
├── PointChapeau    (0 ; 1,88 ; 0,05), échelle 0,75 — entre les oreilles
├── PointVisage     (0 ; 1,68 ; −0,32), échelle 0,7 — devant les yeux
├── PointCou        (0 ; 1,15 ; −0,05), échelle 0,65 — centre de l'encolure
└── PointDos        (0 ; 1,25 ; 0,35), échelle 0,9 — derrière l'encolure
```
(Positions en coordonnées Unity, `v3.md` §7.3 ; l'intégrateur les recale sur le modèle.)

- **Tout ce qui est « en bois » est dans `Corps`** (crinière, oreilles et socle compris) : un
  skin repeint le cheval entier, rayures, pois, cristal et arc-en-ciel compris, sans code en plus.
- **UV cylindriques** (axe vertical, V de 0 au socle à 1 aux oreilles, couture à l'arrière) :
  les 8 bandes du « Sucre d'orge » restent horizontales et les pois ronds sur la face avant.

**Détails qui le rendent lisible**
1. **Yeux** : ovales verticaux ≈ 17 × 21 cm, 6 cm d'épaisseur, à moitié enfoncés dans le front,
   au-dessus de la racine du museau. Ce sont les plus gros détails du visage : ils doivent rester
   lisibles à 64 px.
2. **Reflets** : un point blanc de 4 cm en haut à gauche de chaque œil (les deux du même côté).
3. **Narines** : deux petits ovales sombres (≈ 6 × 9 cm) au bout du museau, côte à côte. Pas de
   bouche, pas de dents, pas de langue : le cheval reste doux et neutre.
4. **Museau** : il avance vers la caméra et pique de ~18° vers le bas (air gentil, pas fier).
   C'est **la** forme qui dit « cheval » vu de face.
5. **Oreilles** : deux pointes épaisses (≥ 4 cm partout), à x ≈ ±0,14, sommet à y = 2,0. Elles
   **traversent les chapeaux** : voulu, comme dans les dessins animés.
6. **Crinière** : une crête de 5 à 6 grosses mèches arrondies (≈ 16 cm d'épaisseur), avec un
   toupet sur le front entre les oreilles. Pas de mèches fines ni de poils.
7. **Socle** : collerette de pion à deux anneaux étagés, bord biseauté. Il dit « pièce de jeu ».
8. **Lissage** : *Smooth by Angle* 30° (volumes ronds lisses, petites arêtes nettes aux jonctions
   socle/cou/tête : c'est l'aspect « pièces de bois assemblées »).

**Ce qu'il n'a pas** : ni cavalier, ni selle, ni bride, ni pattes, ni queue, ni bouche, ni
corne (ce n'est pas une licorne), ni ressemblance avec un cheval ou un poney de dessin animé
connu.

**Triangles visés** : **900 à 1 400** (`v3.md` §7.2).

**Pour les images** (icônes de skins du lot 3) : on montre ce cheval **entier, socle compris**,
vu de 3/4 face légèrement d'en haut, les deux yeux bien visibles (voir §9.3).

### 6.3 Les chapeaux — règles communes

- Un objet racine par chapeau, nommé par son **identifiant du shop** (`chapeau_casquette`,
  `chapeau_fete`, `chapeau_haut_de_forme`, `chapeau_couronne`).
- **Origine = point d'accroche = sommet de la tête** (y = 0). Le chapeau sera posé sur
  `PointChapeau`. Comme le haut de la capsule est une demi-sphère de rayon 0,5, les chapeaux
  à bord **descendent un peu sous y = 0** (de −0,05 à −0,15) pour bien coiffer la tête au lieu
  de flotter dessus. Les positions de la spec du shop restent la référence.
- **v3 (cheval)** : `PointChapeau` est entre les oreilles, à l'échelle 0,75 (§6.2) ; les
  chapeaux ne sont pas remodelés, et les oreilles qui les traversent sont voulues.
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
| Personnage (cheval, v3) | 900 – 1 400 | PersoCorps, Yeux (yeux + narines), Reflet |
| Coffres de rareté (Bronze, Argent, Or, Diamant) | 800 – 1 400 chacun | mêmes rôles que le coffre + matériaux du §2.10 |
| **Accessoires v5** (remplace les lignes ci-dessous) | 600 – 3 000 selon la rareté (§13.7) | §13.4 |
| Boîte 3D d'ouverture (v5) | 1 500 – 2 200 | §14.3 |
| Casquette | 250 – 400 | TissuRouge, TissuRougeFonce, Blanc |
| Chapeau de fête | 200 – 350 | CartonRose, CartonJaune, Blanc |
| Haut-de-forme | 250 – 400 | Feutre, Satin |
| Couronne | 500 – 800 | Or, Rubis |
| Sol de l'étage | 2 000 – 3 500 | PierreDalle, PierreDalleVariante, PierreJoint, PierreClaire, PierreFlanc, Incrustation |

Total de la scène visible : moins de 10 000 triangles. Large marge pour la suite (dé, roue,
pièce géante, tour).

> **v5** : les budgets des accessoires sont **relevés** par rareté (§13.7 : de 600 – 1 000 pour
> un Commun à 1 500 – 3 000 pour le Secret) et les 6 coffres de rareté sont déjà dans la scène.
> Nouveau plafond de la scène visible : **25 000 triangles** (cheval + 4 accessoires au maximum +
> jusqu'à 6 coffres + sol + nuages). C'est encore très peu pour n'importe quel ordinateur.

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
  1,5), aux petits détails des Super rares (v5 : ≤ 0,5, §13.6) et aux effets de victoire
  (ouverture des boîtes, §14). Jamais sur le décor ni sur les coffres.
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
- Pour le cheval (v3) : pas de pièce d'échecs copiée d'un jeu ou d'une marque connue, pas de
  cheval ou poney de dessin animé reconnaissable, pas de corne, pas de cavalier, pas de bouche.
- Le macabre et l'effrayant : le piège reste « cartoon » (ressort, nuage, pétard), jamais de
  sang, de crâne réaliste, de pièges qui blessent.
- Les objets qui traversent la tête du personnage ou flottent au-dessus : chaque chapeau est
  testé sur la capsule.

---

## 9. Pour les images (Gemini)

Les prompts sont écrits dans `art/prompts/<lot>.md` (`lot1.md` : concept, 1res icônes, logo,
ciel ; `lot2.md` : icônes des boîtes, boîtes, ciels des paliers ; `lot3.md` : skins du cheval,
ciels du jour à la nuit, coffres de rareté, améliorations, icônes des panneaux, masque de héros
v3 ; `lot4.md` : icône Classement, roue dentée, médailles du podium, icônes Musique et Effets ;
`lot5.md` : icône Diamant, icône Rebirth, packs de diamants ; `lot6.md` : icônes des 56 objets
des boîtes Pirates et Animaux et images des 4 boîtes, prompts autonomes sans image jointe ;
`lot7.md` : v6, icônes Boutique, Paramètres, Graphismes, rotation, Route des trophées,
illustration et macaron de la Renaissance, plus le plan B des bulles, §15).
Le **premier prompt de chaque
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
**Depuis le lot 3** : pour les skins du cheval, on joint les rendus Blender
`docs/captures/cheval_3_4.png` et `cheval_face.png` (forme) **et** `art/icones/couleur_rouge.png`
(fond, lumière, cadrage) ; puis la première icône cheval validée (`couleur_bleu.png`) sert de
référence en cas de relance. Pour les ciels v3, on joint `art/fonds/ciel_palier3.png` (version
v2, pour le style des nuages seulement).

### 9.1 Bloc de style commun (en anglais)

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.
```

Seule exception : le logo avec titre, qui lève la règle « no text » pour le seul texte
« Tour de la Chance ».

**Variante « ciels v3 »** (lot 3, fonds des paliers seulement). Le bloc commun décrit une scène
de nuit ; pour les ciels de jour, de coucher de soleil et de crépuscule, on garde **la première
phrase (STYLE) et la dernière (RULES) mot pour mot**, et on remplace LIGHTING / PALETTE / MOOD
par la ligne suivante, suivie de la description du palier (couleurs du §2.9) :

```
SKY MOOD: the same toy world, a magical fairground on top of a fairy-tale tower, seen at a different time of day; friendly, cozy, readable. Low contrast, soft gradients, calm empty center, darkest at the bottom.
```

Les icônes, elles, gardent le bloc commun complet et inchangé : c'est ce qui les rend
identiques à celles des lots 1 et 2.

### 9.2 Conventions par type d'image

| Type | Format | Fond | Angle |
|---|---|---|---|
| Concept art, scènes | 16:9 | violet nuit `#1F1730` avec halo `#3A2850` | la caméra du jeu : 3/4 plongeant ~35° depuis l'avant |
| Icônes du shop et de l'interface | carré 1:1 | **uni lavande très clair `#E6E1EE`**, sans dégradé ni ombre portée | 3/4, ~25° au-dessus, objet tourné ~20° vers la gauche, centré, ~70 % du cadre |
| Logo, emblème | 16:9 (logo) / carré (emblème) | uni violet nuit `#1F1730` | de face, symétrique |
| Fond de ciel | 16:9 (1376 × 768 en sortie de Gemini) | — (c'est le fond) | peu contrasté, centre calme, bas le plus sombre ; un fond par palier d'altitude, du jour à la nuit (§2.9, §5.1) |
| Icônes de skins du cheval (v3) | carré 1:1 | lavande `#E6E1EE` (bleu-gris `#5B6380` pour le Blanc nacré) | 3/4 **face** : ~25° au-dessus, cheval tourné ~20° vers la gauche, museau et **deux yeux** vers le spectateur, entier socle compris, ~75 % de la hauteur |
| Icône d'un objet **très clair ou lumineux** (Blanc nacré, Petites ailes d'ange, Auréole ; v3 : `coffre_diamant`, `amelioration_coffres`) | carré 1:1 | **uni bleu-gris moyen `#5B6380`** | comme les autres icônes |
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
- Capsule (lots 1-2) : seulement deux yeux noirs avec reflet ; ni bouche, ni bras, ni visière-hublot, ni sac à dos.
- Cheval (lot 3) : pièce d'échecs jouet entière (socle de pion, cou, tête, museau, oreilles,
  crinière) dans **une seule matière** ; deux grands yeux noirs avec reflet, deux narines
  sombres ; ni bouche, ni pattes, ni queue, ni selle, ni cavalier, ni corne (fiche §6.2).
- Casquette vierge (aucune lettre ni logo sur le devant).
- Pièce d'or : motif « tour + étoile », jamais de chiffre, de signe monétaire ou de visage.
- Le texte d'un titre généré se vérifie lettre par lettre ; en cas d'échec, emblème sans texte
  + titre écrit par l'interface (or `#FFD54A`, contour `#3A2200`).
- Les images ne sont **pas** plaquées sur les modèles 3D (§3.1) : elles servent à l'interface,
  au fond et à la référence de style.
- Les 17 icônes de skins (v3) montrent **le même cheval** que la première icône validée
  (`couleur_bleu.png` du lot 3) : seule la matière change, yeux et narines toujours noirs.
  Les icônes capsule des lots 1-2 sont remplacées (anciennes gardées dans `art/icones/_capsule/`).
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

**v3 — mêmes formes, deux nouveaux usages (validés par le DA)** :
- **Ouverture des coffres précieux** (`v3.md` §5.3.2) : halo, rayons et étincelles réutilisés,
  teintés par la **couleur « UI » de la rareté de coffre** (§2.10 : bronze `#B0703A`, argent
  `#C9CED8`, doré `#E8B830`, diamant `#9FE3FF` / `#F4EEE6`). Ce ne sont pas des couleurs de
  rareté d'objet : pas de conflit avec la règle §10.1. Le flash plein écran reste blanc.
- **Cinématique du Secret** (`v3.md` §8.3) : point lumineux, fissures, morceaux, confettis,
  carte qui tourne, tout en `Painter2D` / textures calculées. Couleurs : point rose `#FF4FD8`,
  fissures, confettis et lettres de « SECRET !!! » dans un arc-en-ciel à saturation 0,75 (pas de
  néon à 100 %, §8), fond arc-en-ciel à saturation 0,5. **Aucune image à générer.**

---

## 11. Les boutons « bonbons de fête foraine » (v3, arbitrage de `v3.md` §10)

**Validé tel quel** dans son principe : chaque bouton ressemble à une touche de jouet en
plastique verni, posée sur son socle. C'est la traduction en interface de nos biseaux 3D (§3.1) :
la lumière vient d'en haut à gauche, donc le haut du bouton est clair et le bas sombre.

### 11.1 Anatomie (référence 1920 × 1080)

| Partie | Réglage | Pourquoi |
|---|---|---|
| **Biseau** (haut) | bordure haute 2 px, couleur « clair » | le bord qui attrape la lumière, comme le biseau d'un coffre |
| **Épaisseur** (bas) | bordure basse **8 px**, couleur « sombre » ; côtés 0 | le bouton a l'air posé sur un socle : on a envie d'appuyer |
| **Reflet** | élément ajouté par le code, en haut (4 px du haut, 10 px des côtés, 38 % de la hauteur), blanc à 18 %, coins 12 px | le vernis du jouet |
| **Ombre** | il n'y a pas d'ombre portée en USS : l'« ombre » est l'épaisseur basse **plus** l'ombre du texte `text-shadow: 0 2px 0 <sombre>` | évite un élément en plus par bouton |
| Coins | 18 px (colonne de boutons et panneaux : 20 px) | formes rondes et jouet |
| Texte | blanc `#FFFFFF` gras (Or : `#3A2200`) | contraste ≥ 4,5:1 sur toutes les bases |

### 11.2 États
Ceux de `v3.md` §10.2, validés : **survol** = échelle 1,06 avec petit rebond, fond « clair »,
reflet à 0,30 ; **appui** = descend de 6 px, épaisseur 8 → 2 px, échelle 0,97 ; **désactivé** =
gris `#5A5A6E` (biseau `#6E6E84`, épaisseur `#3A3A48`, texte `#B8B4C8`), sans reflet. L'éclat qui
passe est réservé au bouton « Jouer » (jamais sur un achat).

### 11.3 Familles de couleurs (validées)

| Famille | Base | Clair (biseau, survol) | Sombre (épaisseur, ombre du texte) |
|---|---|---|---|
| Vert « sûr » | `#2E9E5B` | `#4CC27A` | `#1E6E3E` |
| Violet neutre | `#5B5BD6` | `#7E7EEA` | `#3B3BA0` |
| Or (« Jouer ») | `#FFD54A` | `#FFE27A` | `#C99A1E` |
| Bleu (« Équiper ») | `#3B7DD8` | `#64A0EE` | `#2858A0` |
| Sombre (colonne, onglets, Vendre, sons) | `#2A2A3A` | `#3A3A50` | `#15121F` |

**Remarques du DA**
- Le vert « clair » `#4CC27A` est voisin du vert Peu commun `#4CC26A`, et le bleu « clair »
  `#64A0EE` du texte Rare `#5FA2F0`. **Toléré** : sur un bouton, ce sont des **fonds pleins** qui
  n'apparaissent qu'au survol, alors que les raretés ne sont que des liserés fins et des textes
  (même raisonnement que §10.2 règle 4). On ne crée **pas** de bouton de la couleur d'une rareté.
- Sur les ciels clairs des paliers 1 et 2, les boutons gardent leurs couleurs : leur épaisseur
  sombre et leur texte ombré suffisent à les détacher. La colonne de boutons, en famille
  « sombre » à 90 %, est même plus lisible sur le bleu du jour que sur la nuit.
- Les **icônes** de la colonne (64 × 64) sont des images détourées du lot 3, posées sur le fond
  sombre `#2A2A3A` : elles doivent donc rester lisibles sur du sombre (pas d'objet noir, d'où la
  malle en bois clair et l'album coloré).

> **v4** : le §11 reste valable pour l'idée (un bouton = un jouet posé sur son socle), mais sa
> réalisation est remplacée par le §12.4 (contour épais, socle séparé, police arrondie).

---

## 12. Interface v4 « jouet de fête foraine » (dans l'esprit des jeux Roblox)

> **À quoi sert cette section ?** C'est le guide **directement recopiable en USS** pour refaire
> toute l'interface en v4. Chaque élément a : sa structure (les éléments UXML imbriqués), ses
> valeurs exactes et un extrait USS. Les tailles sont en **pixels de référence 1920 × 1080**
> (comme `HudPanelSettings` : Unity agrandit ou réduit tout seul sur les autres écrans).
>
> **Ce qu'on garde** : les icônes détourées Boutique / Inventaire / Index / Améliorations, les
> couleurs de rareté (§2.7, §10), les familles de boutons (§11.3), le logo, toutes les règles de
> jeu. **Ce qui change** : la forme des boutons, des panneaux (qui deviennent des **pop-ups
> centrées**), la police, la barre de défilement, l'étiquette d'étage, les compteurs, et deux
> nouveaux boutons (**Paramètres** et **Classement**).

### 12.1 L'esprit « Roblox »… sans copier Roblox

Ce qui donne cet air « jeu Roblox » et qu'on **reprend** (ce sont des idées de mise en forme,
pas des éléments protégés) :
1. **Un contour sombre épais autour de tout** (boutons, cadres, textes des titres) : c'est LE
   trait de style. Chez nous, ce contour est le **violet nuit `#1F1730`** (la couleur du ciel de
   nuit, §2.1), jamais du noir pur.
2. **Des couleurs franches et saturées** sur les boutons, des fonds de panneaux sombres et calmes.
3. **Une police ronde et épaisse**, du texte blanc cerclé de sombre.
4. **Des icônes qui « débordent »** de leur cadre (elles dépassent par le haut) : ça donne du relief.
5. **Des animations qui rebondissent** (tout grossit un peu trop puis revient).

Ce qu'on **n'utilise jamais** : le logo Roblox ni son carré penché, le nom « Roblox », la
monnaie hexagonale de Roblox ou tout symbole qui la rappelle, ses polices (Builder Sans,
Gotham), sa barre du haut grise transparente et ses icônes de menu, de chat ou de sac, ses
personnages (avatars « blocs »). Notre jeu doit pouvoir être pris pour **un** jeu de ce genre,
jamais pour un produit de cette marque.

**Ce qui ne va pas sur les captures v3** (`docs/captures/v3_images_jour.png`,
`v3_panneau_ameliorations_achat.png`, `v3_titre.png`) et que la v4 corrige :
- le **reflet gris** des boutons de la colonne est dessiné **par-dessus** l'icône et la salit
  (rectangle gris sur le cadeau, la malle, l'album) → en v4, le reflet est dans la face du
  bouton, **derrière** l'icône (§12.5) ;
- le panneau collé à gauche coupe la scène en deux → **pop-up centrée** sur fond assombri (§12.6) ;
- la **barre de défilement blanche** par défaut de Unity → piste et curseur stylés (§12.9) ;
- les boutons « Musique : oui / Effets : oui » encombrent le haut → **bouton Paramètres** (§12.5) ;
- « Étage 12 » est un simple texte → **badge** (§12.10) ;
- la police fine par défaut manque de caractère → **Fredoka** (§12.3).

### 12.2 Palette de l'interface v4

**Couleurs de structure** (les panneaux restent dans le violet nuit du jeu, un peu plus clairs et
plus bleus qu'en v3 pour que les contours `#1F1730` se voient) :

| Rôle | Hex | Où |
|---|---|---|
| **Encre** (contour universel) | `#1F1730` | tous les contours épais, ombre des textes de titre, contour des textes |
| Voile derrière une pop-up | `#120D20` à **60 %** → `rgba(18, 13, 32, 0.6)` | fond plein écran assombri |
| Corps d'une pop-up | `#2E2552` | grand fond des pop-ups |
| Puits (zone creusée) | `#221A3D` | piste des onglets, tableaux, piste de défilement, intérieur des compteurs |
| Carte | `#3D3270` | cartes d'objets, de boîtes, d'améliorations, lignes du classement |
| Carte survolée | `#4A3E85` | survol d'une carte |
| Filet clair (biseau des cartes) | `#5A4E96` | bord haut des cartes, séparateurs |
| Face des boutons d'icône | `#3B2F66` | disque intérieur des boutons ronds |
| Texte principal | `#FFFFFF` | titres, boutons, noms (petites surfaces : autorisé, §2.6) |
| Texte secondaire | `#D9D2F0` | descriptions, libellés |
| Texte discret | `#A79FC8` | notes, en-têtes de tableaux, onglet inactif |
| Or « argent du jeu » | `#FFD54A` (contour `#3A2200`) | montants, cagnotte, sélection, barres de progression (inchangé) |
| Alerte | `#FF6B6B` | « Il te manque 400 pièces » (plus clair que `#D64545`, lisible sur `#2E2552`) |

**Familles de couleurs des boutons** (celles du §11.3, plus trois nouvelles). Chaque famille a
4 tons : **face**, **clair** (biseau et survol), **mi** (bas de la face : faux dégradé),
**sombre** (socle et ombre du texte).

| Famille | Face | Clair | Mi | Sombre | Texte | Utilisée pour |
|---|---|---|---|---|---|---|
| Vert « sûr » | `#2E9E5B` | `#4CC27A` | `#278A4F` | `#1E6E3E` | blanc | Encaisser, Ouvrir, Améliorer, Acheter ; en-tête « Améliorations » |
| Violet neutre | `#5B5BD6` | `#7E7EEA` | `#4E4EC0` | `#3B3BA0` | blanc | Rejouer, OK ; en-tête « Boutique » |
| Or | `#FFD54A` | `#FFE27A` | `#F0C030` | `#C99A1E` | `#3A2200` | Jouer, onglet actif ; en-tête « Classement » |
| Bleu | `#3B7DD8` | `#64A0EE` | `#3270C4` | `#2858A0` | blanc | Équiper ; en-tête « Inventaire » |
| **Turquoise** (nouveau) | `#1E9EB0` | `#45C3D2` | `#1A8A9A` | `#136B78` | blanc | en-tête « Index » |
| **Ardoise** (remplace « sombre ») | `#4A4470` | `#625C8C` | `#3F3A62` | `#2A2545` | blanc | Vendre, boutons secondaires ; en-tête « Paramètres » |
| **Rouge « fermer »** (nouveau) | `#D64545` | `#F07070` | `#C03A3A` | `#962A2A` | blanc | **uniquement** la croix de fermeture |
| Désactivé | `#6E6A84` | `#8A86A0` | `#5E5A74` | `#45425A` | `#C9C5D8` | tout bouton inactif |

Règles :
- **Un en-tête de pop-up = une famille**, la même que l'anneau de son bouton d'ouverture
  (§12.5) : l'œil associe le bouton violet à la fenêtre violette.
- **Rouge « fermer »** : petite surface ronde, toujours au même endroit (coin haut droit). C'est
  un code universel qu'un enfant reconnaît ; il n'apparaît jamais dans la scène 3D, où le rouge
  reste réservé au piège `#D91A1A` (§2.6).
- **Les couleurs de rareté restent réservées** aux liserés, pastilles, halos et noms de rareté
  (§10.1). Le turquoise `#1E9EB0` est assez loin du bleu Rare `#3B8BEB` et n'est qu'un en-tête.
- Le texte blanc sur une face colorée a **toujours** un contour de la couleur « sombre » de sa
  famille : c'est ce qui le rend lisible même sur le turquoise ou le vert clair du survol.

Pour éviter de recopier les couleurs partout, on utilise les **variables USS** (elles s'écrivent
`--nom` et se lisent avec `var(--nom)` ; elles se transmettent aux éléments enfants) :

```css
/* Theme.uss — à importer en premier dans chaque écran (ou dans HudTheme.tss). */
:root {
    --encre: #1F1730;
    --voile: rgba(18, 13, 32, 0.6);
    --corps: #2E2552;
    --puits: #221A3D;
    --carte: #3D3270;
    --carte-survol: #4A3E85;
    --filet: #5A4E96;
    --face-icone: #3B2F66;
    --texte: #FFFFFF;
    --texte-2: #D9D2F0;
    --texte-3: #A79FC8;
    --or: #FFD54A;
    --or-contour: #3A2200;
}
```

### 12.3 Typographie : Fredoka

**Police choisie : Fredoka** (Google Fonts), graisses **SemiBold** (texte courant) et **Bold**
(titres, boutons, chiffres).
- Page : <https://fonts.google.com/specimen/Fredoka> → « Get font » → « Download all ». Dans le
  zip, prendre les fichiers **statiques** du dossier `static/` : `Fredoka-SemiBold.ttf` et
  `Fredoka-Bold.ttf` (pas le fichier « variable » `Fredoka[wdth,wght].ttf` : Unity ne sait pas
  choisir une graisse dans un fichier variable).
- Sources officielles : <https://github.com/google/fonts/tree/main/ofl/fredoka>.
- **Licence : SIL Open Font License 1.1** (OFL) : utilisation gratuite, y compris dans un jeu
  distribué, et on a le droit de l'intégrer au jeu. Seules obligations : ne pas vendre la police
  seule et **garder le texte de la licence** à côté → copier `OFL.txt` du zip dans
  `Assets/UI/Fonts/OFL.txt`.
- Pourquoi Fredoka : ronde, épaisse, très lisible pour un enfant, accents français complets
  (é è ê à ù ç œ « »), et elle ressemble à nos formes « jouet » biseautées. Alternatives
  (même licence OFL) si on veut plus de caractère : **Lilita One** (une seule graisse, très
  grasse, bien pour les titres seulement) ou **Baloo 2** (plus étroite).

**Installation dans Unity (une fois)**
1. Mettre les deux `.ttf` dans `Assets/UI/Fonts/`.
2. Clic droit sur chaque `.ttf` → *Create → Text → Font Asset* (atlas « SDF », qui garde des
   bords nets à toutes les tailles). Réglages : *Sampling Point Size* 90, *Padding* **9**
   (nécessaire pour les contours épais de 3-4 px), *Atlas* 1024 × 1024, *Render Mode*
   SDFAA, *Atlas Population Mode* **Dynamic** (les lettres sont ajoutées quand le jeu en a
   besoin). Noms : `Fredoka-SemiBold_SDF.asset`, `Fredoka-Bold_SDF.asset`.
3. Dans `Fredoka-SemiBold_SDF`, section *Font Weights*, mettre `Fredoka-Bold_SDF` dans la case
   **Bold** : ainsi tous les `-unity-font-style: bold;` déjà écrits utiliseront la vraie graisse
   grasse au lieu d'un faux gras « étalé ».
4. Dans *Fallback Font Assets* de chaque police, ajouter la police utilisée aujourd'hui par le jeu :
   Fredoka n'a pas forcément les symboles `★ → · ● ×` que le jeu affiche ; la police de secours
   les dessinera. Vérifier dans le jeu : « ★ Nouveau record ! ★ », « ×1,2 → ×1,4 ».

**Échelle des tailles** (1920 × 1080)

| Style | Taille | Graisse | Couleur | Contour (`-unity-text-outline`) | Ombre (`text-shadow`) |
|---|---|---|---|---|---|
| Bannière « Étage 12 » | 88 px | Bold | `#FFD54A` | 4 px `#3A2200` | `0 6px 0 #1F1730` |
| Titre de pop-up | 52 px | Bold | blanc | 4 px `#1F1730` | `0 4px 0 #1F1730` |
| Bouton L (Jouer, Encaisser) | 40 px | Bold | blanc / `#3A2200` | 3 px « sombre » de la famille | `0 3px 0` « sombre » |
| Bouton M (Ouvrir, Équiper…) | 30 px | Bold | idem | 3 px | `0 3px 0` |
| Bouton S, onglet | 24 px | Bold | idem | 2 px | `0 2px 0` |
| Titre de carte | 30 px | Bold | blanc | — | `0 2px 0 #1F1730` |
| Texte courant | 24 px | SemiBold | `#D9D2F0` | — | — |
| Petit texte, notes | 20 px | SemiBold | `#A79FC8` | — | — |
| Libellé sous bouton d'icône | 22 px | Bold | blanc | 3 px `#1F1730` | `0 2px 0 #1F1730` |
| Nom de rareté | 16 px (min.) | Bold | couleur « texte » §2.7 | 2 px `#1F1730` | — |
| Montant de pièces | 34 px | Bold | blanc / `#FFD54A` | 3 px `#1F1730` | `0 3px 0 #1F1730` |

```css
/* Theme.uss (suite) */
:root {
    -unity-font-definition: url("Fonts/Fredoka-SemiBold_SDF.asset");
    color: var(--texte-2);
    font-size: 24px;
}
.titre-popup {
    font-size: 52px;
    color: #FFFFFF;
    -unity-font-style: bold;
    -unity-text-outline-width: 4px;
    -unity-text-outline-color: #1F1730;
    text-shadow: 0 4px 0 #1F1730;
}
```

### 12.4 Les boutons v4

**Structure** : un bouton = **deux couches**. Le `Button` lui-même est le **socle** (couleur
« sombre », contour d'encre) ; à l'intérieur, la **face** colorée, qui contient le reflet et le
texte. La face est 8 px plus courte que le socle : la bande sombre qu'on voit dessous, c'est
**l'épaisseur** du jouet. Quand on appuie, la face descend dans le socle.

```xml
<ui:Button class="bouton bouton--vert bouton--m">
    <ui:VisualElement class="bouton__face" picking-mode="Ignore">
        <ui:VisualElement class="bouton__reflet" picking-mode="Ignore" />
        <ui:Label class="bouton__texte" text="Ouvrir — 250" picking-mode="Ignore" />
    </ui:VisualElement>
</ui:Button>
```
(Le `text` du `Button` reste vide : c'est le `Label` qui écrit. `picking-mode="Ignore"` dit aux
couches décoratives de laisser passer le clic jusqu'au bouton. Le script `ButtonStyler` peut
fabriquer ces couches automatiquement pour tous les `.bouton`.)

| Partie | Valeur |
|---|---|
| Contour (socle) | **4 px** `#1F1730` sur les 4 côtés |
| Épaisseur visible (socle sous la face) | **8 px** (L : 10 px ; S : 6 px) = `padding-bottom` du socle |
| Coins | socle L 26 px, M 22 px, S 18 px ; face = coin du socle − 4 px |
| Faux dégradé de la face | haut : bordure haute **3 px** « clair » ; bas : bordure basse **5 px** « mi » ; au milieu : « face » |
| Reflet | dans la face, en haut : `top 3px`, `left/right 10px`, hauteur **40 %**, blanc **22 %**, coins 12 px |
| Hauteurs totales | L **104 px** (min. 300 px de large) ; M **80 px** (min. 240) ; S **60 px** (min. 140) |
| Marges du texte | 0 28 px (L), 0 22 px (M), 0 16 px (S) |

**États**

| État | Changement | Durée, courbe |
|---|---|---|
| Normal | — | — |
| Survol | socle `scale 1.06` ; face en « clair » ; reflet 32 % | 0,15 s, `ease-out-back` (petit rebond) |
| Appui | face `translate 0 6px` (elle s'enfonce, l'épaisseur passe de 8 à 2 px) ; socle `scale 0.97` | 0,06 s, `ease-out` |
| Relâché | retour à l'état survol | 0,18 s, `ease-out-back` |
| Désactivé | famille « Désactivé », pas de reflet, `opacity 0.8`, pas d'agrandissement | immédiat |

```css
/* Buttons.uss (v4) */
.bouton {
    --face: #5B5BD6; --clair: #7E7EEA; --mi: #4E4EC0; --sombre: #3B3BA0; --ecrit: #FFFFFF;
    height: 80px;
    min-width: 240px;
    margin: 0;
    padding: 0 0 8px 0;               /* l'épaisseur visible sous la face */
    border-width: 4px;
    border-color: #1F1730;            /* le contour d'encre */
    border-radius: 22px;
    background-color: var(--sombre);  /* le socle */
    transition-property: scale;
    transition-duration: 0.15s;
    transition-timing-function: ease-out-back;
}
.bouton__face {
    flex-grow: 1;
    justify-content: center;
    align-items: center;
    padding: 0 22px;
    border-radius: 18px;
    background-color: var(--face);
    border-top-width: 3px;    border-top-color: var(--clair);
    border-bottom-width: 5px; border-bottom-color: var(--mi);
    transition-property: translate, background-color;
    transition-duration: 0.15s, 0.15s;
    transition-timing-function: ease-out-back, ease-out;
}
.bouton__reflet {
    position: absolute;
    top: 3px; left: 10px; right: 10px;
    height: 40%;
    border-radius: 12px;
    background-color: rgba(255, 255, 255, 0.22);
}
.bouton__texte {
    font-size: 30px;
    color: var(--ecrit);
    -unity-font-style: bold;
    -unity-text-align: middle-center;
    -unity-text-outline-width: 3px;
    -unity-text-outline-color: var(--sombre);
    text-shadow: 0 3px 0 var(--sombre);
    margin: 0; padding: 0;
}

.bouton:hover { scale: 1.06 1.06; }
.bouton:hover .bouton__face { background-color: var(--clair); }
.bouton:hover .bouton__reflet { background-color: rgba(255, 255, 255, 0.32); }

.bouton:active { scale: 0.97 0.97; transition-duration: 0.06s; transition-timing-function: ease-out; }
.bouton:active .bouton__face { translate: 0 6px; transition-duration: 0.06s, 0.06s; }

.bouton:disabled {
    --face: #6E6A84; --clair: #8A86A0; --mi: #5E5A74; --sombre: #45425A; --ecrit: #C9C5D8;
    opacity: 0.8;
    scale: 1 1;
}
.bouton:disabled .bouton__reflet { display: none; }

/* Tailles */
.bouton--l { height: 104px; min-width: 300px; padding-bottom: 10px; border-radius: 26px; }
.bouton--l .bouton__face { border-radius: 22px; padding: 0 28px; }
.bouton--l .bouton__texte { font-size: 40px; }
.bouton--s { height: 60px; min-width: 140px; padding-bottom: 6px; border-radius: 18px; }
.bouton--s .bouton__face { border-radius: 14px; padding: 0 16px; border-bottom-width: 4px; }
.bouton--s .bouton__texte { font-size: 24px; -unity-text-outline-width: 2px; text-shadow: 0 2px 0 var(--sombre); }

/* Familles : on ne change que les variables. */
.bouton--vert      { --face: #2E9E5B; --clair: #4CC27A; --mi: #278A4F; --sombre: #1E6E3E; }
.bouton--violet    { --face: #5B5BD6; --clair: #7E7EEA; --mi: #4E4EC0; --sombre: #3B3BA0; }
.bouton--or        { --face: #FFD54A; --clair: #FFE27A; --mi: #F0C030; --sombre: #C99A1E; --ecrit: #3A2200; }
.bouton--or .bouton__texte { -unity-text-outline-width: 0; text-shadow: 0 2px 0 rgba(255, 255, 255, 0.45); }
.bouton--bleu      { --face: #3B7DD8; --clair: #64A0EE; --mi: #3270C4; --sombre: #2858A0; }
.bouton--turquoise { --face: #1E9EB0; --clair: #45C3D2; --mi: #1A8A9A; --sombre: #136B78; }
.bouton--ardoise   { --face: #4A4470; --clair: #625C8C; --mi: #3F3A62; --sombre: #2A2545; }
```
(La règle `:disabled` est écrite **après** les familles dans le fichier, ou rendue plus précise
— `.bouton.bouton--vert:disabled` — pour qu'elle gagne toujours.)

**Un bouton avec une pièce** (« Ouvrir — 250 ● », « Encaisser 120 ● ») : dans la face, une
rangée `Label` + `VisualElement.coin` (34 px, `margin-left: 8px`) ; la face est en
`flex-direction: row`.

**L'éclat qui passe** reste réservé au bouton « Jouer » (§11.2), placé dans la face
(`overflow: hidden` sur `.bouton__face`).

### 12.5 Les boutons ronds d'icône

> **v5** : les boutons des colonnes gauche et droite **perdent leur libellé** et deviennent de
> grands boutons ronds à icône centrée, plus un bouton **Rebirth** : valeurs au **§12.17**, qui
> remplace ce tableau pour les colonnes. Paramètres et Fermer ne changent pas.

Quatre usages, une seule construction : un **socle rond** (couleur « sombre » de la famille,
contour d'encre), une **face ronde** sombre `#3B2F66` cerclée d'un **anneau** de la couleur de
la famille, l'**icône** qui dépasse par le haut, et un **libellé** à cheval sur le bas.

```xml
<ui:Button class="rond rond--violet">            <!-- Boutique -->
    <ui:VisualElement class="rond__face" picking-mode="Ignore">
        <ui:VisualElement class="rond__reflet" picking-mode="Ignore" />
    </ui:VisualElement>
    <ui:VisualElement class="rond__icone" picking-mode="Ignore" />   <!-- APRÈS la face : dessinée par-dessus le reflet -->
    <ui:Label class="rond__libelle" text="Boutique" picking-mode="Ignore" />
</ui:Button>
```
**L'ordre compte** : un élément est dessiné par-dessus ceux qui le précèdent. Le reflet est dans
la face, l'icône vient après : le reflet ne salit plus jamais l'icône (défaut de la v3).

| Usage | Ø socle | Ø icône | Dépassement de l'icône | Famille (anneau + socle) | Libellé |
|---|---|---|---|---|---|
| Colonne gauche : Boutique | **112 px** | 92 px | 18 px vers le haut | violet | « Boutique » |
| Colonne gauche : Inventaire | 112 | 92 | 18 | bleu | « Inventaire » |
| Colonne gauche : Index | 112 | 92 | 18 | turquoise | « Index » |
| Colonne gauche : Améliorations | 112 | 92 | 18 | vert | « Améliorations » (20 px) |
| **Colonne droite : Classement** | 112 | 92 | 18 | **or** | « Classement » |
| **Paramètres** (roue dentée) | **72 px** | 52 px | 0 (centrée) | ardoise | aucun |
| **Fermer** (croix) | **72 px** | — (croix en USS) | — | rouge « fermer » | aucun |

**Placement** (1920 × 1080) :
- colonne gauche : `left: 36px; top: 250px;` un bouton tous les **150 px** (112 + libellé 22 +
  marge) ; colonne droite : `right: 36px; top: 250px;` (sous les compteurs) ;
- Paramètres : à droite du badge d'étage, `margin-left: 16px`, centré verticalement sur lui ;
- Fermer : coin haut droit de la pop-up, `top: -26px; right: -26px` (il mord sur le cadre).

**États** : survol `scale 1.08` + icône `scale 1.1` et `rotate -6deg` (petit coup de tête
joyeux), 0,15 s `ease-out-back` ; appui `scale 0.94`, 0,06 s ; **panneau ouvert** = anneau or
`#FFD54A` de 6 px ; interdit (suspense) = `opacity 0.4` (règle de la v3 conservée).
La roue dentée, elle, **tourne de 90°** au survol (`rotate: 90deg`, 0,3 s `ease-out-back`).

```css
/* Panels.uss (v4) — boutons ronds */
.rond {
    --anneau: #5B5BD6; --sombre: #3B3BA0;
    width: 112px; height: 112px;
    margin: 0 0 38px 0;                /* laisse la place du libellé */
    padding: 0 0 7px 0;                /* épaisseur du socle */
    border-width: 4px; border-color: #1F1730;
    border-radius: 56px;
    background-color: var(--sombre);
    overflow: visible;
    transition-property: scale; transition-duration: 0.15s; transition-timing-function: ease-out-back;
}
.rond__face {
    flex-grow: 1;
    border-radius: 50px;
    border-width: 5px; border-color: var(--anneau);
    background-color: #3B2F66;
    overflow: hidden;
}
.rond__reflet {
    position: absolute; top: 4px; left: 16px; right: 16px; height: 36%;
    border-radius: 20px;
    background-color: rgba(255, 255, 255, 0.14);
}
.rond__icone {
    position: absolute;
    width: 92px; height: 92px;
    left: 6px; top: -18px;             /* l'icône dépasse par le haut */
    -unity-background-scale-mode: scale-to-fit;
    transition-property: scale, rotate; transition-duration: 0.15s; transition-timing-function: ease-out-back;
}
.rond__libelle {
    position: absolute;
    bottom: -30px; left: -30px; right: -30px;   /* plus large que le bouton : « Améliorations » tient */
    font-size: 22px; color: #FFFFFF;
    -unity-font-style: bold; -unity-text-align: middle-center;
    -unity-text-outline-width: 3px; -unity-text-outline-color: #1F1730;
    text-shadow: 0 2px 0 #1F1730;
    margin: 0; padding: 0;
}
.rond:hover { scale: 1.08 1.08; }
.rond:hover .rond__icone { scale: 1.1 1.1; rotate: -6deg; }
.rond:active { scale: 0.94 0.94; transition-duration: 0.06s; }
.rond--ouvert .rond__face { border-width: 6px; border-color: #FFD54A; }
.rond:disabled { opacity: 0.4; scale: 1 1; }

.rond--violet    { --anneau: #5B5BD6; --sombre: #3B3BA0; }
.rond--bleu      { --anneau: #3B7DD8; --sombre: #2858A0; }
.rond--turquoise { --anneau: #1E9EB0; --sombre: #136B78; }
.rond--vert      { --anneau: #2E9E5B; --sombre: #1E6E3E; }
.rond--or        { --anneau: #FFD54A; --sombre: #C99A1E; }

/* Paramètres : plus petit, icône centrée qui tourne. */
.rond--param { --anneau: #625C8C; --sombre: #2A2545; width: 72px; height: 72px; border-radius: 36px; margin: 0; padding-bottom: 5px; }
.rond--param .rond__face { border-radius: 32px; border-width: 4px; }
.rond--param .rond__icone { width: 52px; height: 52px; left: 6px; top: 4px; background-image: url("../Art/icones_detourees/icone_parametres.png"); }
.rond--param:hover .rond__icone { rotate: 90deg; scale: 1.05 1.05; transition-duration: 0.3s; }

/* Fermer : face rouge pleine et croix dessinée par deux barres (pas d'image). */
.rond--fermer { --sombre: #962A2A; width: 72px; height: 72px; border-radius: 36px; margin: 0; padding-bottom: 6px; }
.rond--fermer .rond__face { border-radius: 32px; border-width: 0; border-top-width: 3px; border-top-color: #F07070; background-color: #D64545; align-items: center; justify-content: center; }
.rond--fermer:hover .rond__face { background-color: #F07070; }
.croix-barre {
    position: absolute;
    width: 34px; height: 9px; border-radius: 5px;
    background-color: #FFFFFF;
    border-width: 2px; border-color: #962A2A;
}
.croix-barre--a { rotate: 45deg; }
.croix-barre--b { rotate: -45deg; }
/* Les deux barres sont dans un conteneur .croix (40 × 40, centré dans la face) qui tourne au survol. */
.croix { width: 40px; height: 40px; align-items: center; justify-content: center; transition-property: rotate; transition-duration: 0.2s; transition-timing-function: ease-out-back; }
.rond--fermer:hover .croix { rotate: 90deg; }
```
Structure de la croix : `rond__face` › `croix` › deux `croix-barre` (`--a` et `--b`), tous en
`picking-mode="Ignore"`.

### 12.6 Les pop-ups centrées

Toutes les fenêtres (Boutique, Inventaire, Index, Améliorations, Classement, Paramètres,
confirmations) sont des **pop-ups au centre de l'écran**, sur un voile qui assombrit la scène.
**Une seule ouverte à la fois.** Un clic sur le voile (hors de la pop-up) la ferme, comme la
croix et la touche Échap. Les règles de la v3 sur les moments où l'on peut ouvrir un panneau
(pas pendant le suspense) ne changent pas.

```xml
<ui:VisualElement name="voile" class="voile">
    <ui:VisualElement class="popup popup--m popup--violet">
        <ui:VisualElement class="popup__entete">
            <ui:VisualElement class="popup__entete-reflet" picking-mode="Ignore" />
            <ui:VisualElement class="popup__icone" />
            <ui:Label class="titre-popup" text="Boutique" />
        </ui:VisualElement>
        <ui:VisualElement class="popup__corps"> … onglets, ScrollView … </ui:VisualElement>
        <ui:Button class="rond rond--fermer popup__fermer"> … </ui:Button>
    </ui:VisualElement>
</ui:VisualElement>
```

| Partie | Valeur |
|---|---|
| Voile | plein écran, `rgba(18, 13, 32, 0.6)` |
| Tailles | **S** 680 px de large (Paramètres, confirmations), hauteur selon le contenu ; **M** 1080 × 800 (Boutique, Améliorations, Classement) ; **L** 1360 × 860 (Inventaire, Index : 7 cartes par ligne) ; jamais plus haut que **920 px** |
| Cadre | contour **6 px** `#1F1730`, bas **14 px** (l'épaisseur du « plateau »), coins **32 px**, fond `#2E2552` |
| En-tête | hauteur **96 px**, fond = face de la famille, bordure basse 6 px « sombre » de la famille, coins hauts 26 px ; reflet blanc 16 % sur 45 % de la hauteur |
| Icône d'en-tête | ~~supprimée en v5 (§12.17.6)~~ — **revient en v5.1** : 120 px, elle déborde en haut à gauche avec le titre (§12.18.2) |
| Titre | 52 px, blanc, contour 4 px `#1F1730` (§12.3) ; v5 : centré, plus de `padding-left` |
| Corps | `padding: 20px 28px 24px 28px` |
| Fermer | bouton rond rouge 72 px, `top: -26px; right: -26px` |
| Apparition | voile : `opacity` 0 → 1 en **0,2 s** ; pop-up : `scale 0.85 → 1` et `translate 0 40px → 0` en **0,28 s `ease-out-back`** |
| Fermeture | pop-up : `scale 1 → 0.9`, `opacity 1 → 0` en **0,15 s `ease-in`** ; voile 0,2 s ; puis `display: none` |

```css
.voile {
    position: absolute; left: 0; top: 0; right: 0; bottom: 0;
    background-color: rgba(18, 13, 32, 0.6);
    align-items: center; justify-content: center;
    opacity: 0;
    transition-property: opacity; transition-duration: 0.2s;
}
.popup {
    --entete: #5B5BD6; --entete-sombre: #3B3BA0;
    max-height: 920px;
    background-color: #2E2552;
    border-width: 6px; border-bottom-width: 14px; border-color: #1F1730;
    border-radius: 32px;
    scale: 0.85 0.85; translate: 0 40px; opacity: 0;
    transition-property: scale, translate, opacity;
    transition-duration: 0.15s, 0.15s, 0.15s;
    transition-timing-function: ease-in, ease-in, ease-in;
}
.popup--s { width: 680px; }
.popup--m { width: 1080px; height: 800px; }
.popup--l { width: 1360px; height: 860px; }

/* La classe « --ouvert » est ajoutée par le code UNE image après l'affichage (voir plus bas). */
.voile--ouvert { opacity: 1; }
.voile--ouvert .popup {
    scale: 1 1; translate: 0 0; opacity: 1;
    transition-duration: 0.28s, 0.28s, 0.15s;
    transition-timing-function: ease-out-back, ease-out-back, ease-out;
}

.popup__entete {
    height: 96px; flex-shrink: 0;
    flex-direction: row; align-items: center;
    padding-left: 112px;
    background-color: var(--entete);
    border-bottom-width: 6px; border-bottom-color: var(--entete-sombre);
    border-top-left-radius: 26px; border-top-right-radius: 26px;
}
.popup__entete-reflet {
    position: absolute; top: 4px; left: 16px; right: 16px; height: 45%;
    border-radius: 18px; background-color: rgba(255, 255, 255, 0.16);
}
.popup__icone {
    position: absolute; left: -24px; top: -40px;
    width: 120px; height: 120px;
    -unity-background-scale-mode: scale-to-fit;
}
.popup__corps { flex-grow: 1; flex-shrink: 1; padding: 20px 28px 24px 28px; }
.popup__fermer { position: absolute; top: -26px; right: -26px; }

.popup--violet    { --entete: #5B5BD6; --entete-sombre: #3B3BA0; }  /* Boutique */
.popup--bleu      { --entete: #3B7DD8; --entete-sombre: #2858A0; }  /* Inventaire */
.popup--turquoise { --entete: #1E9EB0; --entete-sombre: #136B78; }  /* Index */
.popup--vert      { --entete: #2E9E5B; --entete-sombre: #1E6E3E; }  /* Améliorations */
.popup--or        { --entete: #FFD54A; --entete-sombre: #C99A1E; }  /* Classement */
.popup--ardoise   { --entete: #4A4470; --entete-sombre: #2A2545; }  /* Paramètres */
```
Sur l'en-tête **or** (Classement), le titre reste blanc avec son contour d'encre de 4 px : il se
lit sur le jaune grâce au contour (c'est le style « titre de jeu » voulu).

**Pourquoi une image d'attente avant d'ajouter `--ouvert` ?** Une transition USS anime un
*changement* de valeur. Si on affiche la pop-up (`display: flex`) et qu'on ajoute la classe dans
la même image, Unity n'a jamais vu l'état « petit et transparent » : pas d'animation. Côté code
(rappel pour le programmeur, la logique ne change pas) :

```csharp
// Ouvrir : afficher, puis ajouter la classe à l'image suivante.
voile.style.display = DisplayStyle.Flex;
voile.schedule.Execute(() => voile.AddToClassList("voile--ouvert")).StartingIn(16);
// Fermer : retirer la classe, puis cacher quand l'animation est finie (0,2 s).
voile.RemoveFromClassList("voile--ouvert");
voile.schedule.Execute(() => voile.style.display = DisplayStyle.None).StartingIn(200);
```

Le **porte-monnaie** de la pop-up (Boutique, Améliorations) se met dans le corps, en haut à
droite, avec le style des compteurs (§12.11) ; la note « Ta cagnotte en jeu… » juste dessous en
petit texte `#A79FC8`.

### 12.7 Les onglets

Des **pilules** dans un puits (Inventaire : Skins / Accessoires ; Index ; Classement si besoin).

| Partie | Valeur |
|---|---|
| Piste | fond `#221A3D`, coins 22 px, `padding: 6px`, contour 3 px `#1F1730` |
| Onglet | hauteur 56 px, coins 16 px, texte 24 px Bold, `flex-grow: 1`, marge 0 4 px |
| Inactif | fond transparent, texte `#A79FC8` ; survol : fond `#3D3270`, texte blanc |
| Actif | famille **or** : fond `#FFD54A`, bordure haute 3 px `#FFE27A`, bordure basse **6 px** `#C99A1E`, texte `#3A2200` ; il « saute » (`scale 1.04`) en 0,15 s `ease-out-back` |

```css
.onglets {
    flex-direction: row; flex-shrink: 0;
    background-color: #221A3D;
    border-width: 3px; border-color: #1F1730; border-radius: 22px;
    padding: 6px; margin-bottom: 14px;
}
.onglet {
    flex-grow: 1; height: 56px; margin: 0 4px;
    border-radius: 16px; border-width: 0;
    background-color: rgba(0, 0, 0, 0);
    font-size: 24px; color: #A79FC8; -unity-font-style: bold;
    transition-property: scale, background-color; transition-duration: 0.15s; transition-timing-function: ease-out-back;
}
.onglet:hover { background-color: #3D3270; color: #FFFFFF; }
.onglet--actif {
    background-color: #FFD54A; color: #3A2200;
    border-top-width: 3px; border-top-color: #FFE27A;
    border-bottom-width: 6px; border-bottom-color: #C99A1E;
    scale: 1.04 1.04;
}
.onglet--actif:hover { background-color: #FFE27A; color: #3A2200; }
```

### 12.8 Les cartes d'objets (liseré de rareté)

| Partie | Valeur |
|---|---|
| Carte | **168 × 216 px**, fond `#3D3270`, coins 18 px, marge 0 12 px 12 px 0 |
| Liseré de rareté | **4 px** de la couleur de base §2.7 sur les 4 côtés (v4 : remplace les 3 px du §10.1) |
| Biseau | bordure haute **6 px** de la même couleur de rareté : la carte a l'air d'un jeton bombé |
| Halo de rareté (facultatif) | élément `.carte__halo` en haut de la carte, hauteur 60 %, couleur de base à **18 %** : autorisé, c'est un « halo » au sens du §10.1 ; **jamais** pour Commun (gris : inutile) |
| Icône | 112 × 112, centrée, en haut |
| Nom | 18 px Bold blanc, 2 lignes max |
| Rareté | 16 px Bold, couleur « texte » §2.7, contour 2 px `#1F1730` |
| Survol | fond `#4A3E85`, `scale 1.04`, icône `scale 1.08`, 0,15 s `ease-out-back` |
| Sélection | cadre extérieur **5 px** `#FFD54A` (coins 22 px), carte `scale 1.04` |
| Badge « ×3 » | pilule en haut à droite : fond `#FFFFFF`, contour 3 px `#1F1730`, texte 18 px Bold `#1F1730`, coins 14 px, `top: -8px; right: -8px` (il déborde) |
| Badge « Équipé » | pilule en haut à gauche : famille verte (fond `#2E9E5B`, contour 3 px `#1F1730`), texte 15 px Bold blanc |
| Non obtenu | icône teintée `#15121F` à 0,9 (§10.2 règle 6), fond `#221A3D` ; le liseré reste visible |

```css
.carte-cadre { padding: 0; margin: 0 12px 12px 0; border-width: 5px; border-color: rgba(0, 0, 0, 0); border-radius: 22px; }
.carte-cadre--choisie { border-color: #FFD54A; }
.carte {
    width: 168px; height: 216px;
    align-items: center; padding: 8px 6px;
    background-color: #3D3270;
    border-radius: 18px;
    border-width: 4px; border-top-width: 6px;
    border-color: #9AA0B0;                /* remplacé par la classe de rareté */
    overflow: hidden;
    transition-property: scale, background-color; transition-duration: 0.15s; transition-timing-function: ease-out-back;
}
.carte-cadre:hover .carte, .carte-cadre--choisie .carte { scale: 1.04 1.04; }
.carte-cadre:hover .carte { background-color: #4A3E85; }
.carte__halo { position: absolute; left: 0; right: 0; top: 0; height: 60%; opacity: 0.18; }
.carte__icone { width: 112px; height: 112px; -unity-background-scale-mode: scale-to-fit; transition-property: scale; transition-duration: 0.15s; }
.carte-cadre:hover .carte__icone { scale: 1.08 1.08; }
.carte__nom { font-size: 18px; color: #FFFFFF; -unity-font-style: bold; -unity-text-align: middle-center; white-space: normal; margin: 4px 0 0 0; }
.carte__rarete { font-size: 16px; -unity-font-style: bold; -unity-text-outline-width: 2px; -unity-text-outline-color: #1F1730; }

/* Une classe par rareté, même identifiant que le catalogue (§10.2 règle 2). */
.rarete-commun      { border-color: #9AA0B0; }
.rarete-peu-commun  { border-color: #4CC26A; }
.rarete-rare        { border-color: #3B8BEB; }
.rarete-super-rare  { border-color: #A35CE0; }
.rarete-legendaire  { border-color: #FF9F1C; }
.rarete-secret      { border-color: #FF4FD8; }
.rarete-commun .carte__rarete     { color: #9AA0B0; }
.rarete-peu-commun .carte__rarete { color: #4CC26A; }
.rarete-rare .carte__rarete       { color: #5FA2F0; }
.rarete-super-rare .carte__rarete { color: #B57DE8; }
.rarete-legendaire .carte__rarete { color: #FF9F1C; }
.rarete-secret .carte__rarete     { color: #FF4FD8; }
.rarete-peu-commun .carte__halo { background-color: #4CC26A; }
.rarete-rare .carte__halo       { background-color: #3B8BEB; }
.rarete-super-rare .carte__halo { background-color: #A35CE0; }
.rarete-legendaire .carte__halo { background-color: #FF9F1C; }
.rarete-secret .carte__halo     { background-color: #FF4FD8; }

.badge-nombre {
    position: absolute; top: -8px; right: -8px;
    font-size: 18px; color: #1F1730; -unity-font-style: bold;
    background-color: #FFFFFF; border-width: 3px; border-color: #1F1730; border-radius: 14px;
    padding: 0 8px;
}
```
**Cartes larges** (boîtes de la Boutique, améliorations, lignes du classement) : même fond
`#3D3270`, coins 22 px, bordure haute 4 px `#5A4E96` (biseau clair, **pas** de couleur de
rareté), `padding: 18px`, `margin-bottom: 16px`. Tableaux (taux de drop, chances de coffres) :
fond puits `#221A3D`, coins 14 px, lignes de 36 px, en-têtes 18 px `#A79FC8`.

### 12.9 La barre de défilement intégrée

Plus de barre blanche : une **piste creusée** (puits) et un **curseur lavande bombé** qui devient
**or quand on le tient**. Pas de flèches haut/bas (on fait défiler avec la molette, le doigt ou le
curseur). On ajoute la classe `defil` à **chaque** `ScrollView`, et
`horizontal-scroller-visibility="Hidden"` dans l'UXML (jamais de barre horizontale).

**Comment Unity construit une barre** (les noms à viser en USS) :
```
ScrollView
└── .unity-scroller.unity-scroller--vertical            la barre entière
    ├── .unity-scroller__low-button                      flèche du haut   → cachée
    ├── .unity-scroller__slider (.unity-base-slider--vertical)
    │   └── .unity-base-slider__drag-container
    │       ├── .unity-base-slider__tracker              la PISTE
    │       ├── .unity-base-slider__dragger-border       cadre de focus   → caché
    │       └── .unity-base-slider__dragger              le CURSEUR
    └── .unity-scroller__high-button                     flèche du bas    → cachée
```
Le préfixe `.defil` rend nos règles **plus précises** que celles du thème par défaut de Unity :
c'est ce qui leur permet de gagner.

| Partie | Valeur |
|---|---|
| Largeur totale | **26 px**, à 10 px du contenu |
| Piste | fond `#221A3D`, contour 3 px `#1F1730`, coins 13 px, toute la hauteur |
| Curseur | **18 px** de large (4 px de jeu de chaque côté), hauteur min. **64 px**, coins 9 px, fond `#B9AEE6`, biseau haut 2 px `#DCD5F7`, épaisseur basse 4 px `#7A6FB0` |
| Survol | fond `#D9D2F5` |
| Tenu | famille or : fond `#FFD54A`, haut `#FFE27A`, bas `#C99A1E` |

```css
.defil .unity-scroller--vertical {
    width: 26px;
    margin-left: 10px;
    border-width: 0;
    background-color: rgba(0, 0, 0, 0);
}
.defil .unity-scroller--vertical .unity-scroller__low-button,
.defil .unity-scroller--vertical .unity-scroller__high-button {
    display: none;
}
.defil .unity-scroller--vertical .unity-scroller__slider {
    flex-grow: 1;
    margin: 0;
    padding: 0;
}
.defil .unity-scroller--vertical .unity-base-slider__drag-container {
    margin: 4px 0;                 /* le curseur ne touche pas les bouts de la piste */
}
.defil .unity-scroller--vertical .unity-base-slider__tracker {
    position: absolute;
    left: 0; right: 0; top: -4px; bottom: -4px;   /* la piste rattrape les 4 px de marge */
    width: auto; margin: 0;
    background-color: #221A3D;
    border-width: 3px; border-color: #1F1730;
    border-radius: 13px;
}
.defil .unity-scroller--vertical .unity-base-slider__dragger-border {
    display: none;
}
.defil .unity-scroller--vertical .unity-base-slider__dragger {
    left: 4px; width: 18px;
    min-height: 64px;
    margin: 0;
    background-color: #B9AEE6;
    border-width: 0;
    border-top-width: 2px;    border-top-color: #DCD5F7;
    border-bottom-width: 4px; border-bottom-color: #7A6FB0;
    border-radius: 9px;
}
.defil .unity-scroller--vertical .unity-base-slider__dragger:hover {
    background-color: #D9D2F5;
}
.defil .unity-scroller--vertical .unity-base-slider__dragger:active {
    background-color: #FFD54A;
    border-top-color: #FFE27A;
    border-bottom-color: #C99A1E;
}
/* Un peu d'air entre les cartes et la barre. */
.defil .unity-scroll-view__content-container { padding-right: 4px; }
```
Vérification : la piste couvre toute la hauteur de la zone défilante ; le curseur ne déborde ni
en haut ni en bas ; si la liste est courte, Unity cache la barre tout seul
(`vertical-scroller-visibility="Auto"`, valeur par défaut). Si l'état « tenu » ne s'affiche pas
sur cette version de Unity, ce n'est pas bloquant : le survol suffit.

### 12.10 Le badge d'étage

« Étage 12 » devient un **badge** : une grosse **médaille or** qui porte le numéro, accrochée à
gauche d'une **plaque** sombre qui écrit « ÉTAGE » et le nom du palier. À droite du badge, le
bouton Paramètres.

```xml
<ui:VisualElement class="haut-gauche">
    <ui:VisualElement class="badge-etage">
        <ui:VisualElement class="badge-etage__plaque">
            <ui:Label class="badge-etage__mot" text="ÉTAGE" />
            <ui:Label class="badge-etage__palier" text="Dans les nuages !" />
        </ui:VisualElement>
        <ui:VisualElement class="badge-etage__medaille">
            <ui:VisualElement class="badge-etage__reflet" picking-mode="Ignore" />
            <ui:Label class="badge-etage__numero" text="12" />
        </ui:VisualElement>
    </ui:VisualElement>
    <ui:Button class="rond rond--param"> … </ui:Button>
</ui:VisualElement>
```

| Partie | Valeur |
|---|---|
| Position | `left: 32px; top: 28px` |
| Médaille | hauteur **112 px**, largeur min. 112 px (elle s'allonge en pilule pour « 100 »), coins 56 px, fond `#FFD54A`, contour 5 px `#1F1730`, bas **11 px** `#C99A1E`, reflet blanc 30 % sur 40 % de la hauteur |
| Numéro | **60 px** Bold `#3A2200`, ombre `0 3px 0 rgba(255,255,255,0.5)` (lettre « gravée ») |
| Plaque | hauteur 84 px, fond `#2E2552` à 92 %, contour 5 px `#1F1730`, coins 42 px ; elle passe **sous** la médaille (`margin-left: -40px`, `padding-left: 56px`, `padding-right: 28px`) |
| « ÉTAGE » | 20 px Bold `#D9D2F0`, `letter-spacing: 3px` |
| Nom du palier | 26 px Bold blanc, contour 3 px `#1F1730` (« En plein jour », « Dans les nuages ! »…) |
| Changement d'étage | la médaille fait `scale 1 → 1.25 → 1` (classe `--saut` 0,12 s `ease-out`, retirée après 0,12 s, retour 0,25 s `ease-out-back`) |

Lisibilité sur les ciels clairs (§5.1) : le contour d'encre de 5 px et le fond sombre de la plaque
remplacent l'ancien contour de texte ; rien d'autre à faire.

```css
.haut-gauche { position: absolute; left: 32px; top: 28px; flex-direction: row; align-items: center; }
.badge-etage { flex-direction: row-reverse; align-items: center; margin-right: 16px; }  /* la médaille est à gauche */
.badge-etage__medaille {
    height: 112px; min-width: 112px;
    padding: 0 22px 6px 22px;
    align-items: center; justify-content: center;
    background-color: #FFD54A;
    border-width: 5px; border-bottom-width: 11px; border-color: #1F1730; border-bottom-color: #C99A1E;
    border-radius: 56px;
    transition-property: scale; transition-duration: 0.25s; transition-timing-function: ease-out-back;
}
.badge-etage__reflet {
    position: absolute; top: 4px; left: 14px; right: 14px; height: 40%;
    border-radius: 30px; background-color: rgba(255, 255, 255, 0.3);
}
.badge-etage__numero {
    font-size: 60px; color: #3A2200; -unity-font-style: bold;
    text-shadow: 0 3px 0 rgba(255, 255, 255, 0.5);
    margin: 0; padding: 0;
}
.badge-etage__plaque {
    height: 84px; justify-content: center;
    margin-left: -40px; padding: 0 28px 0 56px;
    background-color: rgba(46, 37, 82, 0.92);
    border-width: 5px; border-color: #1F1730; border-radius: 42px;
}
.badge-etage__mot { font-size: 20px; color: #D9D2F0; -unity-font-style: bold; letter-spacing: 3px; margin: 0; padding: 0; }
.badge-etage__palier {
    font-size: 26px; color: #FFFFFF; -unity-font-style: bold;
    -unity-text-outline-width: 3px; -unity-text-outline-color: #1F1730;
    margin: 0; padding: 0;
}
.badge-etage__medaille.badge-etage--saut { scale: 1.25 1.25; transition-duration: 0.12s; transition-timing-function: ease-out; }
```
Note : avec `row-reverse`, la médaille (2e enfant) s'affiche à gauche **et** par-dessus la
plaque (elle est dessinée après elle) : c'est voulu.

**La bannière centrale « Étage N »** (montée) prend le même langage : texte 88 px or contour
4 px `#3A2200` + ombre `0 6px 0 #1F1730`, nom du palier dans une plaque `#2E2552` à 92 % avec
contour 5 px `#1F1730`, coins 36 px ; apparition `scale 0.6 → 1` en 0,3 s `ease-out-back`.

### 12.11 Les compteurs de pièces

Une **pilule** avec la pièce qui **dépasse à gauche**, un petit libellé et un gros chiffre.

| Partie | Valeur |
|---|---|
| Position | `right: 32px; top: 28px`, empilés avec 14 px d'écart (Cagnotte au-dessus) |
| Pilule | hauteur **76 px**, largeur min. 260 px, fond `#221A3D` à 90 %, contour 5 px `#1F1730`, coins 38 px, `padding: 0 26px 0 52px` |
| Pièce | **72 px**, `position: absolute; left: -26px`, centrée verticalement (`top: -3px`) |
| Libellé | 17 px Bold `#A79FC8` (« CAGNOTTE », « PORTE-MONNAIE », `letter-spacing: 2px`) |
| Chiffre | 34 px Bold, contour 3 px `#1F1730` ; Cagnotte en or `#FFD54A`, Porte-monnaie en blanc |
| Gain | classe `--saut` : pilule `scale 1.12` en 0,1 s puis retour 0,2 s `ease-out-back` (déjà fait par le code pour le texte : on l'applique à la pilule) |

```css
.compteur {
    height: 76px; min-width: 260px;
    margin-bottom: 14px; padding: 0 26px 0 52px;
    justify-content: center; align-items: flex-end;
    background-color: rgba(34, 26, 61, 0.9);
    border-width: 5px; border-color: #1F1730; border-radius: 38px;
    transition-property: scale; transition-duration: 0.2s; transition-timing-function: ease-out-back;
}
.compteur__piece {
    position: absolute; left: -26px; top: -3px;
    width: 72px; height: 72px;
    background-image: url("../Art/icones_detourees/piece.png");
    -unity-background-scale-mode: scale-to-fit;
}
.compteur__libelle { font-size: 17px; color: #A79FC8; -unity-font-style: bold; letter-spacing: 2px; margin: 0; padding: 0; }
.compteur__chiffre {
    font-size: 34px; color: #FFFFFF; -unity-font-style: bold;
    -unity-text-outline-width: 3px; -unity-text-outline-color: #1F1730;
    margin: 0; padding: 0;
}
.compteur--cagnotte .compteur__chiffre { color: #FFD54A; }
.compteur--saut { scale: 1.12 1.12; transition-duration: 0.1s; transition-timing-function: ease-out; }
```
Les **pastilles de l'étage** (« Trésor : +12 », « 3 coffres · 1 piège ») et la **boîte du
message** prennent le même style de pilule (fond `#221A3D` à 90 %, contour 4 px `#1F1730`,
coins 30 px, texte 26 px Bold blanc ; message : 34 px, contour de texte 3 px `#1F1730`).

### 12.12 Les toasts (petits messages)

« Accessoire équipé ! », « Amélioration achetée ! », « Il te manque 50 pièces »…

| Partie | Valeur |
|---|---|
| Position | centré en haut, `top: 150px` ; 3 au plus, empilés vers le bas, 12 px d'écart |
| Pilule | hauteur min. 72 px, fond `#2E2552`, contour 4 px `#1F1730`, bas 8 px `#1F1730`, coins 36 px, `padding: 10px 30px 10px 14px` |
| Pastille de type | disque 48 px à gauche, contour 3 px `#1F1730`, couleur de la famille : vert `#2E9E5B` (réussite), or `#FFD54A` (pièces), ardoise `#4A4470` (info), alerte `#FF6B6B` (impossible) ; une icône détourée ou un symbole dedans |
| Texte | 26 px Bold blanc |
| Animation | entrée : `translate 0 -40px → 0`, `opacity 0 → 1`, 0,25 s `ease-out-back` ; reste **2,5 s** ; sortie : `opacity → 0`, `translate → 0 -20px`, 0,2 s `ease-in` |

```css
.toasts { position: absolute; top: 150px; left: 0; right: 0; align-items: center; }
.toast {
    flex-direction: row; align-items: center;
    min-height: 72px; margin-bottom: 12px; padding: 10px 30px 10px 14px;
    background-color: #2E2552;
    border-width: 4px; border-bottom-width: 8px; border-color: #1F1730; border-radius: 36px;
    translate: 0 -40px; opacity: 0;
    transition-property: translate, opacity; transition-duration: 0.25s, 0.25s;
    transition-timing-function: ease-out-back, ease-out;
}
.toast--visible { translate: 0 0; opacity: 1; }
.toast--sortie { translate: 0 -20px; opacity: 0; transition-duration: 0.2s, 0.2s; transition-timing-function: ease-in, ease-in; }
.toast__pastille { width: 48px; height: 48px; border-radius: 24px; border-width: 3px; border-color: #1F1730; margin-right: 14px; -unity-background-scale-mode: scale-to-fit; }
.toast__texte { font-size: 26px; color: #FFFFFF; -unity-font-style: bold; margin: 0; padding: 0; }
.toast--reussite .toast__pastille { background-color: #2E9E5B; }
.toast--pieces   .toast__pastille { background-color: #FFD54A; }
.toast--info     .toast__pastille { background-color: #4A4470; }
.toast--alerte   .toast__pastille { background-color: #FF6B6B; }
```

### 12.13 La pop-up Paramètres (Musique / Effets)

Pop-up **S** (680 px), en-tête **ardoise**, icône `icone_parametres.png`. Deux lignes (cartes
larges §12.8) : icône 64 px (`icone_musique.png`, `icone_effets.png`), libellé 30 px Bold blanc
(« Musique », « Effets »), et à droite un **interrupteur**.

| Interrupteur | Valeur |
|---|---|
| Piste | 112 × 56 px, coins 28 px, contour 4 px `#1F1730` ; **non** : `#45425A` ; **oui** : `#2E9E5B` |
| Bouton rond | 40 px, `#FFFFFF`, bas 4 px `#C9C5D8`, coins 20 px ; à gauche (non) → `translate: 56px 0` (oui), 0,18 s `ease-out-back` |
| Texte | « Oui » / « Non » écrit à côté (20 px `#D9D2F0`) : jamais la couleur seule |

```css
.inter { width: 112px; height: 56px; border-width: 4px; border-color: #1F1730; border-radius: 28px; background-color: #45425A; padding: 4px; transition-property: background-color; transition-duration: 0.18s; }
.inter__bouton { width: 40px; height: 40px; border-radius: 20px; background-color: #FFFFFF; border-bottom-width: 4px; border-bottom-color: #C9C5D8; transition-property: translate; transition-duration: 0.18s; transition-timing-function: ease-out-back; }
.inter--oui { background-color: #2E9E5B; }
.inter--oui .inter__bouton { translate: 56px 0; }
```
L'écran titre perd lui aussi ses deux boutons de son : il reçoit le même bouton Paramètres en
haut à gauche, la colonne d'icônes à gauche, le Classement à droite, et le bouton « Jouer » en
`bouton--or bouton--l` (texte 48 px, hauteur 120 px).

### 12.14 La pop-up Classement (aspect visuel)

> Le **contenu** du classement (qui est classé, sur quel critère, record d'étage ou de pièces)
> n'existe pas encore dans le GDD : c'est au **game-designer** de l'écrire (spec v4) avant le
> code. Rappel du projet : sauvegarde en `PlayerPrefs`, pas de cloud. Ce qui suit ne décrit que
> l'apparence.

Pop-up **M**, en-tête **or**, icône `icone_classement.png`.
- **Podium** en haut : les 3 premiers sur trois marches en USS (cartes `#3D3270`, contour 4 px
  `#1F1730`, coins hauts 18 px), hauteurs **180 / 140 / 110 px** (1er au centre, 2e à gauche,
  3e à droite), chacun avec sa **médaille** 88 px (`medaille_or.png`, `medaille_argent.png`,
  `medaille_bronze.png`) posée au-dessus de la marche. Le **rang est écrit en chiffres par
  l'interface** (« 1 », « 2 », « 3 », 44 px Bold blanc, contour 4 px `#1F1730`) : les médailles
  n'ont aucun chiffre.
- **Liste** dessous (ScrollView `defil`) : lignes de 72 px (cartes larges), rang 30 px Bold
  `#A79FC8`, nom 26 px blanc, score 30 px or `#FFD54A` aligné à droite.
- **La ligne du joueur** : fond `#4A3E85`, contour 4 px `#FFD54A` (la couleur de la sélection),
  étiquette « Toi » en pilule or.
- Couleurs des 3 premiers : or `#FFC83A`, argent `#C9CED8`, bronze `#B0703A` (les métaux des
  coffres §2.10) : ce **ne sont pas** des couleurs de rareté.

### 12.15 Placement général (HUD, 1920 × 1080)

| Zone | Contenu | Position |
|---|---|---|
| Haut gauche | badge d'étage + bouton Paramètres | `left 32, top 28` |
| Haut droite | compteurs Cagnotte puis Porte-monnaie | `right 32, top 28` |
| Bord gauche | Boutique, Inventaire, Index, Améliorations | `left 36, top 250`, pas de 150 px |
| Bord droit | Classement (place prévue pour d'autres boutons plus tard) | `right 36, top 250` |
| Haut centre | toasts | `top 150` |
| Bas centre | pastilles de l'étage, message, boutons d'action (L) | comme en v3 |
| Centre | pop-ups (une à la fois) sur le voile | centrées |

Les deux colonnes laissent le **centre libre** pour le cheval et les coffres, comme le demandent
les fonds de ciel (§5.1 : côté gauche calme, centre vide).

### 12.16 À éviter en v4
- Le noir pur `#000000` pour les contours : toujours l'encre `#1F1730`.
- Les contours fins (< 3 px) sur les boutons et cadres : le style vient de leur épaisseur.
- Le texte blanc sans contour sur une face colorée.
- Un reflet ou un décor dessiné **par-dessus** une icône.
- Les couleurs de rareté en fond de bouton, de pop-up ou d'onglet (§10.1, toujours valable).
- Plusieurs pop-ups empilées ; un panneau collé au bord de l'écran.
- Les animations de plus de 0,3 s sur un bouton (le jeu doit rester vif) ; le rebond sur les
  boutons d'achat est permis, l'éclat qui passe ne l'est pas (§11.2).
- Tout élément de la marque Roblox (§12.1).

### 12.17 Boutons d'icône v5 : l'icône seule, en grand, dans un bouton rond

> **Point de départ réel.** Le jeu v4 a été codé d'après `docs/specs/v4.md` §5.3 à §5.6 (classes
> `.btn`, `.btn--sombre`, `.col-btn`, `.col-btn-icon`, `.col-btn-text`, `.popup-icon` dans
> `Assets/UI/Buttons.uss` et `Popups.uss`, contours `#110D1F`). Les valeurs ci-dessous **partent
> de ce code** (et non des §12.4 – 12.6, restés en partie théoriques) pour que le programmeur
> n'ait que des retouches à faire. Capture de référence : `docs/captures/v4_popup_boutique.png`.

**Ce qui change et pourquoi.** Aujourd'hui, chaque bouton de colonne est un carré sombre de
128 px avec une icône de 84 px et un mot dessous : l'icône n'occupe que la moitié du bouton et
le texte fait « menu de logiciel ». Les jeux de ce genre misent sur **une grosse icône
colorée dans un bouton rond**, reconnue d'un coup d'œil, et sur **une couleur par fonction**.
Sans texte, c'est l'icône **et la couleur de l'anneau** qui disent à quoi sert le bouton ; le
nom apparaît dans une **info-bulle** au survol (accessibilité).

#### 12.17.1 Anatomie (1920 × 1080)

```xml
<ui:VisualElement class="btn-holder">
    <ui:VisualElement class="btn-shadow btn-shadow--rond" picking-mode="Ignore" />   <!-- l'ombre, AVANT le bouton -->
    <ui:Button class="btn btn--sombre col-btn col-btn--boutique" name="BoutonBoutique">
        <ui:VisualElement class="col-btn-anneau" picking-mode="Ignore" />
        <ui:VisualElement class="col-btn-reflet" picking-mode="Ignore" />
        <ui:VisualElement class="col-btn-icon" picking-mode="Ignore" />            <!-- APRÈS le reflet : jamais sali -->
        <ui:VisualElement class="col-btn-badge" picking-mode="Ignore">             <!-- facultatif, caché par défaut -->
            <ui:Label class="col-btn-badge__texte" text="!" picking-mode="Ignore" />
        </ui:VisualElement>
    </ui:Button>
</ui:VisualElement>
```
Le `Label` `.col-btn-text` **disparaît** de l'UXML (et `.col-btn-text--small` du USS).

| Partie | Valeur | Pourquoi |
|---|---|---|
| Bouton (colonnes gauche et droite) | **Ø 120 px** (`width/height 120`, `border-radius 60`) | assez gros pour un doigt et pour l'œil ; un peu plus petit que le carré de 128 px, mais le cercle paraît plus grand que le carré qu'il remplace car l'icône le remplit |
| Contour | **4 px** `#110D1F` (gauche, droite, haut) — inchangé (`.btn`) | le trait d'encre du style |
| Épaisseur 3D | bordure basse **10 px**, couleur « épaisseur » **de la fonction** (tableau 12.17.2) | le bouton est un jeton posé ; la couleur de la fonction se voit aussi dessous |
| Face | dégradé sombre actuel (`Degrades/bouton_sombre.png`, base `#3A3358`) | les icônes ont été dessinées pour un fond sombre (§11.3, remarque) : une face colorée salirait le cadeau violet ou l'album |
| **Anneau** de couleur | élément `.col-btn-anneau` collé au contour (`inset 0`), **6 px**, couleur « anneau » de la fonction, coins 56 px | c'est le code couleur qui remplace le mot |
| Reflet | ovale blanc **18 %**, `top 8px`, `left/right 20px`, hauteur **34 %**, coins 30 px, **sous** l'icône | le vernis du jouet, sans salir l'icône (défaut v3) |
| **Icône** | image de **112 × 112 px**, centrée sur la face (`left 0; top -3px`) ; l'objet dessiné y occupe ~70 % (marges des images Gemini), soit **~80 px visibles**, les **deux tiers du bouton** | « l'icône seule, en grand » ; elle peut **déborder** légèrement de l'anneau au survol (effet autocollant) |
| Ombre portée | disque Ø 120 px, `rgba(17, 13, 31, 0.45)`, décalé de **0 / +8 px**, frère placé **avant** le bouton (système `btn-shadow` de la v4) | le bouton flotte au-dessus du ciel, même sur le bleu clair du palier 1 |
| Espacement | **20 px** entre deux boutons (pas de **140 px**) | sans libellé, les boutons peuvent se rapprocher |

**Placement**
- Colonne gauche : `left: 32px; top: 170px` → Boutique (y = 170), Inventaire (310), Index (450),
  Améliorations (590) ; la colonne finit à y = 710.
- Colonne droite : `right: 32px; top: 170px` → **Classement** (170), **Rebirth** (310). La place
  dessous reste libre pour de futurs boutons (même pas de 140 px).
- Le bouton **Paramètres** (roue dentée, Ø 72 px, à côté du badge d'étage) **ne change pas** : il
  est volontairement plus petit (fonction secondaire), mais il est déjà rond, sans texte, avec
  l'icône qui remplit le bouton : c'est le modèle que l'on généralise.

#### 12.17.2 Une couleur par fonction (anneau = bandeau de la pop-up)

L'anneau reprend la couleur du **bandeau de titre** de la pop-up qu'il ouvre (`v4.md` §5.6, déjà
codée dans `.popup-header--*`) : l'enfant associe le bouton rose à la fenêtre rose.

| Bouton | Anneau | Anneau « clair » (survol) | Épaisseur (bas) | Icône |
|---|---|---|---|---|
| Boutique | `#C2477A` | `#E57AA5` | `#8A2E55` | `icone_boutique.png` |
| Inventaire | `#3B7DD8` | `#7DB5FF` | `#2858A0` | `icone_inventaire.png` |
| Index | `#2E9E8E` | `#5CCBBB` | `#1F6E63` | `icone_index.png` |
| Améliorations | `#D99A1A` | `#FFC64D` | `#9A6A0E` | `icone_ameliorations.png` |
| Classement | `#6C4FE0` | `#A08CFF` | `#4530A8` | `icone_classement.png` |
| **Rebirth** (nouveau) | **`#EF6A32`** « flamme » | `#FF9A66` | `#B4461C` | `icone_rebirth.png` (lot 5) |

- **Le « flamme » de Rebirth** : orange-rouge (teinte ≈ 18°), choisi pour l'idée de phénix et de
  renaissance. Il reste distinct de l'orange Légendaire `#FF9F1C` (teinte ≈ 35°, réservé aux
  raretés et jamais en fond de bouton) et du rouge « fermer » `#F0484E`. La future pop-up Rebirth
  prend ce même `#EF6A32` comme bandeau (`.popup-header--rebirth`).
- Contenu et règles du Rebirth : **au game designer** (spec v5). Ici, seulement l'apparence.

#### 12.17.3 États

| État | Changement | Durée, courbe |
|---|---|---|
| Repos | — | — |
| Survol | bouton `scale 1.08` (`.btn`, inchangé) ; anneau → « clair » ; icône `scale 1.15` et **dandinement** −8° → +8° → 0° (code, comme en v4 mais 8° au lieu de 6°) ; info-bulle après 0,3 s (12.17.4) | 0,18 s `ease-out-back` ; dandinement 0,35 s |
| Appui | `scale 0.92`, descend de 6 px, épaisseur 10 → 4 px (`.btn`, inchangé) ; icône `scale 1.0` | 0,06 s `ease-out` |
| Relâché | rebond par 1,12 (`.btn--rebond`, inchangé) | 0,25 s |
| Pop-up ouverte (`.col-btn--active`) | anneau **or `#FFD54A` 6 px**, contour or 5 px (v4, inchangé), face `#5A4F88`, icône `scale 1.1` | 0,15 s |
| Interdit (suspense) | `opacity 0.45` (v4) **et** icône désaturée : `-unity-background-image-tint-color: #B8B4C8` ; pas de dandinement ; l'info-bulle s'affiche quand même (on doit pouvoir savoir ce qu'est le bouton) | 0,15 s |
| Pastille « ! » (facultatif) | disque **Ø 36 px** `#F0484E`, contour 4 px `#110D1F`, « ! » 24 px Bold blanc contour 2 px `#5E1013`, en haut à droite (`top -6px; right -6px`), pulsation `scale 1 → 1.15 → 1` toutes les 1,2 s | **quand** l'afficher (nouvel objet, Rebirth possible…) = logique de jeu, décidée par le game designer ; l'interface ne fait que l'afficher |

#### 12.17.4 L'info-bulle (accessibilité)

Sans mot sous l'icône, un nouveau joueur doit quand même pouvoir lire le nom. **Attention** :
la propriété `tooltip` de UI Toolkit ne marche que dans l'éditeur, pas dans le jeu. Il faut donc
un **petit élément maison**, unique, posé dans le HUD et déplacé par le code à côté du bouton
survolé (`InfoBulle`, un seul `VisualElement` + un `Label`, `picking-mode="Ignore"`).

| Partie | Valeur |
|---|---|
| Texte | le nom du bouton : « Boutique », « Inventaire », « Index », « Améliorations », « Classement », « Rebirth » ; **26 px** Bold blanc, contour 3 px `#110D1F` |
| Pilule | hauteur **52 px**, `padding: 0 22px`, fond `#2A2440`, contour **4 px** `#110D1F`, bas **7 px** `#110D1F`, coins 26 px ; **bordure haute 4 px de la couleur « anneau »** du bouton (rappel du code couleur) |
| Position | colonne gauche : **à droite** du bouton, à 14 px de son bord, centrée sur lui en hauteur ; colonne droite : **à gauche** du bouton ; roue dentée : **en dessous** (texte « Réglages ») |
| Apparition | après **0,3 s** de survol (pas de clignotement quand la souris passe vite) : `opacity 0 → 1`, `translate 10px → 0` (vers le bouton), **0,15 s `ease-out-back`** |
| Disparition | dès que la souris sort ou au clic : `opacity → 0` en **0,08 s** |
| Clavier / manette | s'affiche quand le bouton a le **focus** |
| Écran tactile | **appui long 0,4 s** = info-bulle (le bouton ne s'ouvre pas au relâché) ; appui court = ouvre |
| Première fois (proposition au game designer) | au premier lancement de la v5, les 6 info-bulles s'affichent **ensemble 3 s** puis s'effacent (une clé `PlayerPrefs` à prévoir dans la spec) |

#### 12.17.5 Extrait USS (à ajouter dans `Popups.uss`, remplace le bloc « colonnes » v4)

```css
/* ---------- Les colonnes v5 : boutons ronds, icône seule (art bible §12.17) ---------- */
.col { position: absolute; top: 170px; width: 120px; }
.col--gauche { left: 32px; }
.col--droite { right: 32px; }

.btn.col-btn {
    --anneau: #6C4FE0;
    --anneau-clair: #A08CFF;
    width: 120px;
    height: 120px;
    margin: 0 0 20px 0;
    padding: 0;
    border-radius: 60px;          /* un cercle : la moitié de 120 */
    align-items: center;
    justify-content: center;
    overflow: visible;            /* l'icône a le droit de déborder au survol */
}
/* L'anneau de couleur, collé à l'intérieur du contour d'encre. */
.col-btn-anneau {
    position: absolute; left: 0; top: 0; right: 0; bottom: 0;
    border-radius: 56px;
    border-width: 6px;
    border-color: var(--anneau);
    transition-property: border-color; transition-duration: 0.15s;
}
/* Le reflet est AVANT l'icône dans l'UXML : il est dessiné dessous. */
.col-btn-reflet {
    position: absolute; top: 8px; left: 20px; right: 20px; height: 34%;
    border-radius: 30px;
    background-color: rgba(255, 255, 255, 0.18);
}
.col-btn-icon {
    position: absolute;
    left: 0; top: -3px;
    width: 112px; height: 112px;  /* l'objet dessiné y occupe ~80 px */
    -unity-background-scale-mode: scale-to-fit;
    transition-property: scale, rotate; transition-duration: 0.15s; transition-timing-function: ease-out-back;
}
.btn.col-btn:hover .col-btn-anneau { border-color: var(--anneau-clair); }
.btn.col-btn:hover > .col-btn-icon { scale: 1.15 1.15; }
.btn.col-btn:active > .col-btn-icon { scale: 1 1; }

/* Une couleur par fonction. Deux classes (.btn.col-btn--x) pour gagner sur .btn--sombre. */
.btn.col-btn--boutique      { --anneau: #C2477A; --anneau-clair: #E57AA5; --epaisseur: #8A2E55; }
.btn.col-btn--inventaire    { --anneau: #3B7DD8; --anneau-clair: #7DB5FF; --epaisseur: #2858A0; }
.btn.col-btn--index         { --anneau: #2E9E8E; --anneau-clair: #5CCBBB; --epaisseur: #1F6E63; }
.btn.col-btn--ameliorations { --anneau: #D99A1A; --anneau-clair: #FFC64D; --epaisseur: #9A6A0E; }
.btn.col-btn--classement    { --anneau: #6C4FE0; --anneau-clair: #A08CFF; --epaisseur: #4530A8; }
.btn.col-btn--rebirth       { --anneau: #EF6A32; --anneau-clair: #FF9A66; --epaisseur: #B4461C; }

/* Pop-up ouverte : on garde le contour or de la v4 et on passe l'anneau en or. */
.btn.col-btn.col-btn--active .col-btn-anneau { border-color: #FFD54A; }

/* Interdit : grisé ET icône désaturée. */
.btn.col-btn:disabled > .col-btn-icon { -unity-background-image-tint-color: #B8B4C8; }

/* Pastille « ! » (cachée par défaut ; le code ajoute .col-btn-badge--visible). */
.col-btn-badge {
    display: none;
    position: absolute; top: -6px; right: -6px;
    width: 36px; height: 36px; border-radius: 18px;
    background-color: #F0484E;
    border-width: 4px; border-color: #110D1F;
    align-items: center; justify-content: center;
}
.col-btn-badge--visible { display: flex; }
.col-btn-badge__texte { font-size: 24px; color: #FFFFFF; -unity-font-style: bold; -unity-text-outline-width: 2px; -unity-text-outline-color: #5E1013; margin: 0; padding: 0; }

/* L'ombre ronde (frère placé avant le bouton). */
.btn-shadow--rond { width: 120px; height: 120px; border-radius: 60px; background-color: rgba(17, 13, 31, 0.45); translate: 0 8px; }

/* L'info-bulle (un seul élément pour tout le HUD, placé par le code). */
.info-bulle {
    position: absolute;
    height: 52px; padding: 0 22px;
    justify-content: center;
    background-color: #2A2440;
    border-width: 4px; border-bottom-width: 7px; border-color: #110D1F;
    border-top-color: var(--anneau);   /* le code recopie la couleur du bouton survolé */
    border-radius: 26px;
    opacity: 0; translate: 10px 0;
    transition-property: opacity, translate; transition-duration: 0.08s, 0.08s;
}
.info-bulle--visible { opacity: 1; translate: 0 0; transition-duration: 0.15s, 0.15s; transition-timing-function: ease-out-back, ease-out-back; }
.info-bulle__texte { font-size: 26px; color: #FFFFFF; -unity-font-style: bold; -unity-text-outline-width: 3px; -unity-text-outline-color: #110D1F; margin: 0; padding: 0; }
```
(Pour la colonne droite, le code met `translate: -10px 0` au départ, l'info-bulle arrivant par la
gauche. Si les variables USS ne passent pas dans `border-top-color` sur cette version de Unity,
le code écrit la couleur directement dans `style.borderTopColor`.)

#### 12.17.6 Les pop-ups n'ont plus d'icône en haut à gauche (annulé en v5.1, voir §12.18.2)

- L'élément `.popup-icon` est **retiré** de toutes les pop-ups (Boutique, Inventaire, Index,
  Améliorations, Classement, Réglages, Pseudo, et la future Rebirth).
- Le bandeau garde sa **hauteur de 96 px**, sa couleur et son contour bas ; le titre (48 px,
  blanc, contour 4 px `#110D1F`) reste **centré** : sans icône, il n'y a plus rien à décaler.
- Pourquoi : l'icône était déjà dans le bouton de la colonne, juste à côté (et entourée d'or quand
  la pop-up est ouverte) ; en double, elle encombrait le coin du bandeau et se cognait au bouton
  de fermeture sur les petites pop-ups. La couleur du bandeau suffit à faire le lien.
- Les icônes **à l'intérieur** des pop-ups (cartes de boîtes, améliorations, lignes Musique /
  Effets, médailles du podium) **restent**.

#### 12.17.7 À vérifier en capture
- À 1920 × 1080 et à 1280 × 720 : chaque icône se reconnaît sans texte ; la colonne gauche ne
  touche pas le badge d'étage (le badge finit vers y = 120, la colonne commence à 170).
- Sur le ciel clair du palier 1 : l'ombre et le contour suffisent à détacher les boutons.
- Survol : l'info-bulle ne passe jamais sous une pop-up ouverte (elle est dans la couche la plus
  haute du HUD) et ne sort pas de l'écran.

### 12.18 Pop-ups et cartes « façon jeu Roblox » (v5.1)

> **Pourquoi ?** L'auteur a fourni trois captures d'un jeu de ce genre
> (`docs/references/v5_1_ref_*.png|jpg`). On reprend leurs **codes d'interface très répandus**
> (panneau clair très arrondi, titre autocollant qui déborde, cartes vives, pilule de prix
> verte) **sans rien copier** : ni textes, ni personnages, ni logo, ni monnaie de la marque
> (§12.1 reste valable). Code : `Assets/UI/Popups.uss` (fenêtre, titre) et `Assets/UI/V51.uss`
> (tout le reste), pièces partagées dans `Scripts/UI/PopupDecor.cs`, images fabriquées par
> `UiFxTextures.BakeAll()` dans `Assets/UI/Degrades/`.

#### 12.18.1 Le panneau

| Partie | Valeur |
|---|---|
| Fond | **blanc cassé** : dégradé `#FFFDF8` (haut) → `#F1ECE2` (bas), `popup_corps.png` |
| Contour | **6 px `#110D1F`** (l'encre), coins **28 px** |
| Motif en filigrane | `motif_popup.png` (étoile, pièce, diamant en encre, tuile 192 px affichée à **150 px**), répété (`background-repeat: repeat`), **opacité 6 %** ; la fenêtre a `overflow: hidden` pour qu'il suive les coins |
| Place | la fenêtre descend de 60 px (`margin-top: 120px`) pour que le titre ne touche pas la roue dentée du HUD |
| Corps | `padding: 60px 32px 26px 32px` (le haut laisse la place au titre) |
| Textes posés sur le fond | **foncés** : `#2A2440` (normal), `#3A3358` (phrases), `#6E6A80` (discret) ; jamais de texte clair sur le blanc |
| Fondus haut/bas des listes | de la couleur du fond (`#F8F4ED` / `#F4F0E8`), sinon une bande sombre salit la liste |
| Barre de défilement | piste `rgba(17,13,31,0.1)`, curseur or inchangé, contour encre |

#### 12.18.2 Le titre qui déborde

- **L'icône du menu revient** (la même que le bouton de colonne ; Pseudo = Classement) :
  **120 × 120 px**, `scale-to-fit`, posée **en haut à gauche, à cheval sur le bord** :
  la rangée « icône + titre » est à `left: -34px; top: -64px`, haute de 128 px, donc centrée sur
  le contour du haut. Les icônes reçoivent leur **propre contour noir** (détourage v5.1) : le
  contour fait partie de l'image, la boîte de 120 px le garde entier.
- **Titre** : **62 px** Bold blanc, `-unity-text-outline-width: 5px` (le maximum propre de la
  police : marge de l'atlas 9 px pour 90 pt), `-unity-text-outline-color: #110D1F`,
  **ombre** `text-shadow: 0 5px 0 #110D1F` (l'épaisseur « autocollant »), 10 px après l'icône.
- **Croix de fermeture : inchangée** (rond rouge 72 px à cheval sur le coin, croix blanche).
  L'auteur la trouve parfaite et elle rappelle la roue dentée.
- Les bandeaux de couleur par pop-up (`.popup-header--*`) ne sont plus dessinés.

#### 12.18.3 Les cartes (boîtes, packs, améliorations, cadres de la Renaissance)

| Partie | Valeur |
|---|---|
| Fond | **dégradé vif en diagonale** (haut gauche → bas droite) + **lueur blanche** ronde (35 % au centre, un peu au-dessus du milieu) : images `carte_*.png` 256 px |
| Couleurs | rose `#FFC94A → #FF4F8B` (Boîtes classiques), bleue `#62E6FF → #3B5BDB` (Pirates, amélioration « pièges », « Tu gardes »), verte `#D2F55A → #1FAE6A` (Animaux), violette `#D2B4FF → #6C4FE0` (packs de diamants, « coffres »), or `#FFE66B → #FF9F1C` (« gains », « Tu gagnes »), rouge `#FFA08A → #E0404A` (« Tu perds ») |
| Cadre | contour **5 px `#110D1F`**, coins **24 px**, `overflow: hidden` |
| Titre | 30 px Bold blanc, contour 4 px encre, ombre `0 3px 0` encre, centré |
| Illustration | 116 px au centre d'une zone de 132 px, sur un **rayonnement** (`RaysElement`, 12 rayons blancs à 85 % qui s'effacent vers l'extérieur) ; la **courte description** à côté (22 px blanc contour 3 px) |
| Textes | toujours **blancs contourés d'encre** (2 à 4 px selon la taille) : lisibles sur toutes les couleurs vives |
| Tuiles d'objets (Inventaire, Index) | **restent sombres** (`#2A2440`, liseré de rareté) : les couleurs de rareté du §10 sont faites pour un fond sombre |

#### 12.18.4 La pilule de prix et les boutons d'action

- **Prix = gros bouton vert en relief** (famille `.btn--vert` : dégradé `#5AD68C → #2E9E5B`,
  **bord bas 10 px plus foncé** `#1E6E3E`, contour `#0E3D21`) : `[verbe] [icône de la monnaie
  36 px] [montant 32 px]`, texte blanc contour 3–4 px. Hauteur 70–88 px, coins 24 px.
  Exemples : « Ouvrir ● 250 », « Ouvrir ◆ 5 », « Améliorer ● 900 », « Renaître ! ● 10 000 ».
- Grisée (pas assez) : famille désactivée (gris, sans contour de texte), icône pâlie, et la
  raison écrite dessous (« Il te manque 3 pièces »).
- **Boutons d'action** (Équiper, Ouvrir, Renaître, Oui renaître !) : **verts**, gros, texte blanc
  contour 4 px. Les boutons de navigation (Contenu et taux, Retour, Annuler) restent sombres.

#### 12.18.5 Les taux en badges

Sous l'illustration de chaque boîte : **une vignette par rareté** (un objet de cette rareté,
cadre 60 px liseré de la couleur de rareté, fond blanc 50 %) et, mordant sur le bas du cadre,
un **badge** (17 px Bold blanc contour 3 px, fond = couleur de rareté, contour 2 px encre) avec
le **taux de la rareté** lu dans les données de la boîte. **Les taux affichés sont toujours
vrais** : c'est le taux de la rareté (pas celui de l'objet montré, qui est dans « Contenu et
taux »). Un Secret jamais obtenu reste un « ? », comme dans l'Index.

#### 12.18.6 Les messages importants

Une phrase à ne pas rater (ex. Renaissance) : **26 px Bold blanc, contour 4 px encre**, centrée ;
la partie **positive en vert `#6BE34A`** avec le même contour : « Les pièces et les améliorations
sont remises à zéro. **Tu gardes tout le reste !** ». Les confirmations (« Sûr ? … ») utilisent le
même style, dans une petite fenêtre au même panneau blanc cassé.

#### 12.18.7 Boutique en une seule page

- Une **seule liste** qui défile : **Boîtes classiques** (déco : pièces) → **Boîtes de pirates**
  (déco : couronnes dessinées) → **Boîtes Animaux** (déco : pattes dessinées) → **Diamants**
  (déco : diamants). La police n'a pas d'émoji : les décorations sont des **images** ou des
  **formes dessinées par le code** (`PopupDecor`), 42 px, de part et d'autre d'un titre de
  section **34 px Bold `#2A2440`**.
- Les **onglets** deviennent des **raccourcis** : un clic fait défiler en douceur jusqu'à la
  section en **0,3 s, ralenti en fin de course** (ease-out) ; le titre s'arrête 20 px sous le haut
  (sous le petit fondu). En défilant à la main, l'onglet de la **section visible s'allume**.
- **Aucun paiement réel** : l'onglet « Diamants · Bientôt » reste **grisé** (opacité 55 %), les
  packs sont affichés grisés, **sans prix**, avec « Bientôt disponible » désactivé.

#### 12.18.8 Les monnaies dans le texte

La police n'a pas de joli « ◆ » : dans une phrase, les monnaies sont des **pictogrammes en
ligne** (balise `<sprite name="diamant">` / `<sprite name="piece">`, constantes
`ShopUi.Diamant` et `ShopUi.Piece`). Sprite Asset : `Assets/UI/Monnaies/monnaies.asset` (atlas
`monnaies.png` fait à partir de `diamant.png` et `piece.png` avec leur contour noir), branché
comme « Default Sprite Asset » de `Assets/UI/TexteReglages.asset`, les réglages de texte de
`HudPanelSettings`. Taille d'environ **1,1 fois la hauteur des chiffres**, centré sur eux (glyphe
128 px, `pointSize` 128, échelle 1,2, `bearingY` 100). Ne plus écrire « ◆ » dans un texte affiché.

#### 12.18.9 À éviter (v5.1)

- Du texte clair (`#B8B4C8`, `#E6E1EE`…) posé directement sur le fond blanc cassé.
- Un contour de texte de plus de 5 px (la police se déforme).
- Un taux « arrangé » ou un objet montré avec un taux qui n'est pas le sien sans le dire.
- Toucher à la croix de fermeture.

---

## 13. Accessoires v5 : « plus jeu moderne »

> **À quoi sert cette section ?** L'auteur trouve les 16 accessoires « vieillots » : il leur
> « manque quelque chose dans la conception 3D ». Cette section explique **pourquoi**, puis donne
> à l'artiste 3D des **règles chiffrées** et une **fiche de refonte** par accessoire. Elle
> complète §3 (formes), §4 (matériaux) et §6.3 à 6.7 (fiches v1), qui restent valables pour les
> dimensions, les points d'accroche et les couleurs de base.
> Référence de l'état actuel : `docs/captures/cheval_planche.png`.

### 13.1 Le diagnostic : pourquoi ils font « vieux »

En regardant la planche des 16 accessoires sur le cheval :

1. **Une seule forme par objet.** La casquette est une demi-sphère + une plaque, le nœud deux
   triangles, l'écharpe un anneau, l'auréole un tore. Il n'y a que la **forme principale**. Les
   jeux 3D stylisés récents construisent chaque objet en **trois niveaux** : une grosse forme
   (la silhouette), quelques formes secondaires (bords, rubans, revers, montures) et des petits
   détails tertiaires (rivets, pois, coutures, perles, étoiles). C'est ce troisième niveau qui
   donne l'impression d'un objet « fini » et riche, même de loin.
2. **Trop petits.** À l'échelle des points d'accroche (0,65 au cou, 0,7 au visage), le nœud
   papillon fait ~12 cm de large à l'écran, le collier n'est qu'une ligne de points, les ailes
   d'ange dépassent à peine. Les jeux modernes **exagèrent** les accessoires (souvent 1,5 à 2 fois
   la taille « logique ») : ce sont des récompenses, elles doivent se voir.
3. **Une seule matière, une seule valeur.** Presque chaque objet est une couleur unie avec un seul
   réglage de brillance. Il manque le **contraste de matières** (mat contre verni, tissu contre
   métal) et le **contraste de valeurs** (clair contre foncé) entre la pièce principale et ses
   bords. Le haut-de-forme et les lunettes deviennent des taches sombres.
4. **Le métal a l'air marron.** La couronne paraît en bronze terne : un métal (metallic 1) ne
   montre que ce qu'il reflète. Sans environnement (monde de Blender noir pour la planche, ou pas
   de Reflection Probe dans Unity), l'or devient brun.
5. **Des arêtes trop dures ou trop molles.** Biseaux de 2 cm à 1 segment : à cette distance, ils
   ne se voient plus. Les objets modernes ont des **biseaux larges et ronds** qui attrapent un
   reflet doux sur chaque bord (l'aspect « jouet en vinyle »).
6. **Rien ne bouge (ou tout bouge pareil).** À part les ailes et l'auréole, les objets sont
   figés, collés à la tête. Il manque le **mouvement secondaire** : un pompon qui rebondit après
   un saut, un pan d'écharpe qui balance, une pointe de chapeau qui suit avec un temps de retard.
7. **Pas de « récompense » visuelle selon la rareté.** Un Super rare (couronne) n'a pas l'air plus
   précieux qu'un Commun. Il manque l'**échelle d'effets** : éclats, lueurs, petites particules,
   qui grandit avec la rareté.

### 13.2 Formes : trois niveaux, silhouette exagérée

| Niveau | Part du volume | Nombre | Rôle | Exemple (bonnet) |
|---|---|---|---|---|
| **Primaire** | **60 – 70 %** | 1 (2 pour les paires) | la silhouette, lisible en noir à 64 px | le dôme du bonnet |
| **Secondaire** | **25 – 35 %** | 2 à 4 | casse la forme, donne le « style », change de couleur ou de matière | le revers côtelé, le pompon |
| **Tertiaire** | **≤ 10 %** | 3 à 12 | le « fini » : petits reliefs répétés, reflets peints | les bosses du pompon, une étiquette en relief |

Règles chiffrées :
- **Taille de lecture à l'écran** (taille du modèle × échelle du point d'accroche, §6.2) :
  - **Tête** : largeur ≥ **0,75 m** (1,2 × le crâne de 0,62 m) ;
  - **Visage** : couvre toute la zone des yeux (≥ **0,58 m** de large) ;
  - **Cou** : ≥ **0,45 m** de large vu de face ;
  - **Dos** : dépasse d'au moins **0,35 m de chaque côté** de la silhouette du cou (on ne voit
    jamais le dos du cheval).
- **Le détail-signature exagéré à 150 – 200 %** : visière, pompon, pointe du sorcier, cœurs,
  pierres, nœud. Un seul par objet, le plus reconnaissable.
- **Épaisseur minimale dans le modèle : 6 cm** (au lieu de 4) : une fois réduit à 0,65 au cou,
  il reste 4 cm à l'écran (§3.1). Les détails tertiaires font au moins **6 cm** dans le modèle.
- **Silhouette « poire »** pour les objets de tête : plus large en bas ou en haut qu'au milieu,
  jamais un cylindre droit. Lignes courbes ou en S (pointe du sorcier, pans d'écharpe), pas de
  segments rigides.
- **Asymétrie de vie** permise sur **un** élément (pompon décalé, pan plus long d'un côté, nœud de
  côté) ; le reste symétrique.
- **Écart entre les pièces** : les formes secondaires **se posent sur** la primaire (relief
  ≥ **2 cm** dans le modèle) au lieu d'être peintes à plat : l'ombre de contact dessine le détail.

### 13.3 Biseaux et lissage « jouet en vinyle »

| Réglage Blender | Valeur v5 | Avant (§3.1) |
|---|---|---|
| Modificateur *Bevel* — Amount | **4 à 6 cm** sur les grandes arêtes, **2 cm** sur les tertiaires | 2 à 4 cm |
| *Bevel* — Segments | **2** (3 pour l'or et les pierres) | 1 |
| *Bevel* — Shape (profil) | **0,5** (arrondi régulier) | — |
| *Bevel* — Limit Method | **Angle 30°** | — |
| *Bevel* — Harden Normals | **coché** | — |
| Modificateur *Weighted Normal* | après le Bevel : Weight **50**, **Keep Sharp** coché, mode *Face Area* | — |
| Lissage | *Shade Auto Smooth* 30° pour les objets durs ; *Shade Smooth* complet pour les objets mous | inchangé |

- **Pourquoi les normales pondérées ?** Elles gardent les grandes faces bien plates (reflet net)
  et font tourner la lumière uniquement dans le biseau : c'est le « coup de lumière » sur chaque
  bord des objets stylisés modernes, pour quelques triangles seulement.
- **Export et import** : à l'export `.glb`, cocher *Normals* (Data → Mesh). Dans Unity, sur le
  modèle importé : **Normals = Import** (pas *Calculate*), sinon Unity recalcule et efface le
  travail.

### 13.4 Matières : toujours au moins deux, bien contrastées

**Règle de contraste** (chaque accessoire) :
1. **Au moins 2 matières** dont la smoothness diffère d'au moins **0,3** (ex. laine 0,15 et
   verni 0,85).
2. **Contraste de valeur ≥ 25 %** de luminosité entre la pièce principale et une pièce secondaire
   (ex. turquoise `#2BB8C4` et blanc cassé `#F4EEE6`). Un objet sombre reçoit un **liseré
   clair** (bord, ruban, monture) ; un objet clair reçoit un **creux ou un bord foncé**.
3. **Couleurs** : 3 couleurs au plus (§2.6, règle 4), **plus** au besoin un métal « doré déco »
   `#E8B830` et des reflets peints blancs `#FFFFFF`, qui ne comptent pas.

**Préréglages URP Lit des accessoires** (Unity : *Specular Highlights* et *Environment
Reflections* **cochés** sur tous) :

| Préréglage | Metallic | Smoothness | Roughness (Blender) | Pour |
|---|---|---|---|---|
| Mat doux | 0 | **0,15** | 0,85 | laine, feutre, pompons, plumes |
| Tissu satiné | 0 | **0,50** | 0,50 | rubans, bandeau, nœud, satin |
| Carton / plastique brillant | 0 | **0,65** | 0,35 | chapeau de fête, ailes de papillon |
| Verni « bonbon » | 0 | **0,85** | 0,15 | verres, cœurs, étoiles, boutons |
| Nacre (perles) | **0,2** | **0,85** | 0,15 | perles `#F4EEE6` |
| Pierre précieuse | 0 | **0,95** | 0,05 | rubis, gemmes |
| Doré déco `M_DoreDeco` (nouveau) | **1** | **0,80** | 0,20 | liserés, charnières, attaches : `#E8B830` (le doré des coffres Or, §2.10) |
| Or trésor `M_Or` | 1 | 0,80 | 0,20 | **la couronne seulement** (§2.3) |
| Reflet peint `M_Reflet` | 0 | 0,90 | 0,10 | petites formes blanches (voir ci-dessous) |

- **Reflets peints** : de petites formes blanches en relief de 1 cm (une barre en diagonale et un
  point, comme sur les lunettes de soleil), posées **en haut à gauche** (côté de la lumière) des
  surfaces vernies. **2 au plus par pièce.** C'est un code fort du jouet stylisé : l'objet « brille »
  sous toutes les lumières, même sans reflet réel.
- **Les objets sombres** (haut-de-forme, lunettes de soleil) : smoothness **≥ 0,35**, sinon le
  contre-jour ne dessine pas leur bord et ils deviennent des trous noirs.
- **Le contre-jour devient obligatoire** (§5, ligne « facultatif ») : Point Light `#B48CFF`,
  intensité 0,6, derrière et au-dessus du cheval. C'est lui qui fait le liseré lavande sur tous
  les bords, que URP Lit ne sait pas dessiner seul.
- **Métal** : Reflection Probe obligatoire dans Unity (§5) ; et pour les **rendus de planche dans
  Blender**, un monde en dégradé (ciel `#8C7AB8`, sol `#2A2030`) au lieu du noir, sinon l'or
  ressort marron sur les captures et on juge mal.
- **Dégradé de couleur calculé par le code (facultatif)** : comme pour les skins (§4.1), une
  petite texture verticale 1 × 32 pixels fabriquée au démarrage peut assombrir doucement le bas
  d'une pièce (ailes, cône du sorcier) : **2 couleurs de la palette de l'objet, écart ≤ 25 %**,
  sur une UV verticale simple. Jamais d'image dessinée ou générée.

### 13.5 Ce qui bouge : quatre sortes d'animations (faites par le code)

Toutes sont **purement visuelles**, faites par script sur la position, la rotation ou l'échelle
(pas d'Animator nécessaire), et continuent pendant les épreuves (`boites.md` §5.6).

| Type | Ce que c'est | Réglages | Qui y a droit |
|---|---|---|---|
| **A. Ressort** (mouvement secondaire) | une pièce molle suit son point d'accroche avec du retard et rebondit (après un saut, un atterrissage, l'écrasement du cheval) | raideur **180**, amortissement **14** (stable en ~0,35 s) ; décalage max **0,06 m** ou **10°** | pompons, pans, pointes, antennes, plumes, perles : **toutes raretés** |
| **B. Boucle douce** (idle) | un mouvement qui se répète (balancement, battement, rotation) | sinus ; période **≥ 0,8 s** (sauf papillon 0,9 s déjà) ; parties symétriques **en miroir** ; parties en couches **décalées de 0,06 à 0,1 s** (chevauchement) | dès **Peu commun** |
| **C. Éclat** | une étoile à 4 branches (même forme que les scintillements du ciel, 8 cm dans le modèle) apparaît sur un bord brillant : échelle 0 → 1 → 0 en **0,35 s**, en tournant de 45° | un point au hasard parmi les objets vides `Eclat_1`, `Eclat_2`, `Eclat_3` posés par l'artiste ; matériau `M_Reflet` (Rare) ou émissif (≥ Super rare) | dès **Rare** |
| **D. Particules 3D** | petites formes (étoile 4 branches 6 cm, losange, point) qui naissent sur l'objet, montent ou tombent et s'effacent | réserve d'objets réutilisés, sans ombre ni collider, matériau URP Lit émissif ; vie **0,6 à 1,2 s** ; naissance sur l'objet vide `Fx_Emetteur` | dès **Super rare** |

- **Pourquoi pas le système de particules de Unity ?** Il demande le shader « URP Particles »,
  alors que la règle du projet est URP Lit pour tous les matériaux. On fait comme pour les
  paillettes de l'Arc-en-ciel (`v3.md` §8.1) : de petits objets réutilisés, déplacés par un script.
  Si le chef de projet accepte un jour une exception pour les particules, on pourra changer.
- **Le « pop » d'équipement** (toutes raretés) : quand on équipe un accessoire, il apparaît en
  `scale 0 → 1.15 → 1` en **0,3 s `ease-out-back`**, et lâche une gerbe d'éclats 3D : Commun 0,
  Peu commun 0, Rare **4**, Super rare **6**, Légendaire **10**, Secret **16**.
- **Convention de noms dans Blender** (le code les cherche) : `Anim_<Nom>` pour une pièce animée,
  **origine sur sa charnière** (ex. `Anim_Pompon` à la base du pompon, `Anim_PanGauche` au nœud
  de l'écharpe) ; `Eclat_1…3` (objets vides) ; `Fx_Emetteur` (objet vide).
- **Limites** : jamais d'animation qui cache les yeux plus de 0,2 s ; aucune animation plus rapide
  que 0,8 s de période sauf le battement du papillon ; tout reste dans une sphère de **1,1 m** de
  rayon autour du point d'accroche (sauf l'orbite de la planète, déjà prévue).

### 13.6 Effets autorisés par rareté (plus c'est rare, plus il y en a)

Le tableau est **cumulatif** : chaque rareté a aussi tout ce qu'ont les précédentes.

| | Commun | Peu commun | Rare | Super rare | Légendaire | Secret |
|---|---|---|---|---|---|---|
| Niveaux de forme | primaire + secondaire (+ 1 à 3 tertiaires) | + tertiaires (3 à 6) | tertiaires riches (6 à 12) | + 1 matière précieuse (pierre, nacre, doré) | + une pièce « magique » (flotte, ne touche rien) | + une pièce en orbite ou qui change |
| Matières | 2 | 2 – 3 | 3, dont 1 verni ou doré | 3 + pierre/nacre | idem + émissif | idem + émissif animé |
| A. Ressort | oui (pièces molles) | oui | oui | oui | oui | oui |
| B. Boucle douce | non | **1** | 1 | **2** | 2 – 3 | 3 et + |
| C. Éclat | non | non | **1 toutes les 4 s** (sans émission) | **1 toutes les 2,5 s** (émission 0,5) | **1 toutes les 1,5 s** (émission 1,2) | **1 toutes les 1 s**, couleurs de l'arc-en-ciel |
| Émission | 0 | 0 | 0 | **≤ 0,5** sur ≤ 10 % de la surface | **≤ 1,5** | **≤ 1,5**, intensité qui oscille |
| D. Particules 3D (visibles en même temps) | 0 | 0 | 0 | **≤ 4** | **≤ 10** | **≤ 16** |
| Traînée (quand la tête bouge) | non | non | non | non | courte (0,25 s) | oui (0,4 s) |
| Gerbe au « pop » d'équipement | 0 | 0 | 4 | 6 | 10 | 16 |

- **Traînée** : `TrailRenderer` avec un matériau URP Lit transparent émissif (largeur 6 cm →
  0, couleur de l'objet). À valider en capture ; si le rendu est laid avec URP Lit, on s'en passe
  (ce n'est qu'un bonus).
- **Plafond de Bloom** inchangé (§4.1) : rien au-dessus de 1,5. L'accessoire ne doit jamais briller
  plus que le coffre au trésor ouvert.
- **Règle d'or** : ces effets sont **décoratifs**. Ils ne changent rien aux chances ; l'interface
  n'y associe jamais un bonus caché (le bonus de pièces par rareté, s'il existe, est déjà écrit
  dans la Boutique).

### 13.7 Budgets de triangles revus (par accessoire, avec ses pièces animées)

| Rareté | Triangles | Matériaux max | Pièces animées max |
|---|---|---|---|
| Commun | **600 – 1 000** | 3 (+ reflet) | 2 |
| Peu commun | **800 – 1 400** | 3 (+ reflet, + doré) | 3 |
| Rare | **1 000 – 1 800** | 4 | 4 |
| Super rare | **1 200 – 2 200** | 5 | 6 |
| Légendaire | **1 500 – 2 500** | 5 | 8 |
| Secret | **1 500 – 3 000** | 5 | 10 |

Les petites formes des particules (≈ 8 à 20 triangles chacune) ne comptent pas dans ces budgets.

### 13.8 Fiches de refonte des 16 accessoires

> Lecture : **Garder** = ce qui marche déjà (dimensions et couleurs de `boites.md` §5.6 et §6.4 à
> 6.7, sauf mention) ; **Ajouter** = la refonte. Les tailles sont **dans le modèle** (avant
> l'échelle du point d'accroche). Couleurs nouvelles : uniquement des couleurs déjà dans la
> palette (§2.4, §2.8, §2.10) ou `#E8B830` (doré déco).

#### 1. `chapeau_casquette` — Casquette (Commun, Tête) — 600 – 900 tris
- **Problème vu** : de face, la visière est cachée : on lit un béret rouge.
- **Garder** : calotte rouge `#E04040`, visière `#A02828`, bouton blanc.
- **Ajouter** :
  - visière **plus longue (0,55)** et **inclinée de 12° vers le bas** pour qu'elle se voie de face,
    courbée (bords 5 cm plus bas que le centre) ;
  - secondaire : **6 panneaux** séparés par des rainures de 2 cm ; un **bourrelet** blanc cassé
    `#F4EEE6` (tube Ø 6 cm) tout autour de la base de la calotte ;
  - tertiaire : bouton **Ø 0,14** bombé ; **2 œillets** en creux (Ø 6 cm, `#A02828`) sur les
    panneaux avant ; une **couture** en relief (2 cm) sur le bord de la visière ;
  - matières : calotte mat doux 0,20, visière **satinée 0,50**, bouton verni 0,85.
- **Animation** : A. ressort sur toute la casquette (petit sursaut de 0,03 m à l'atterrissage).

#### 2. `chapeau_fete` — Chapeau de fête (Commun, Tête) — 600 – 900 tris
- **Garder** : cône rayé rose `#FF6FB5` / jaune `#FFD54A`, pompon, penché de 10°.
- **Ajouter** :
  - les bandes jaunes **en relief de 1,5 cm** (secondaire : chaque rayure se lit par son ombre) ;
  - une **collerette de papier** à la base : 12 festons arrondis blanc cassé `#F4EEE6`, 6 cm
    d'épaisseur ;
  - pompon **Ø 0,26** fait de **7 boules** groupées (tertiaire, aspect « duvet ») ;
  - matières : carton **brillant 0,65**, pompon et collerette **mat 0,15** (contraste).
- **Animation** : A. ressort sur `Anim_Pompon` (il rebondit après chaque saut).

#### 3. `chapeau_bonnet` — Bonnet de laine (Commun, Tête) — 600 – 1 000 tris
- **Garder** : dôme turquoise `#2BB8C4`, revers et pompon blanc cassé.
- **Ajouter** :
  - silhouette « bonnet mou » : le haut **retombe légèrement vers l'arrière gauche** (asymétrie de vie) ;
  - revers **côtelé** : 16 côtes verticales en relief de 2 cm ;
  - une **bande** blanc cassé de 6 cm à mi-hauteur du dôme (contraste de valeur) ;
  - pompon **Ø 0,30**, 9 bosses (tertiaire) ;
  - matières : laine mat 0,15 partout, sauf une **petite étiquette** carrée (8 × 8 cm, en relief,
    verni 0,85 turquoise) cousue sur le revers, à gauche : le seul point brillant.
- **Animation** : A. ressort sur `Anim_Pompon`.

#### 4. `lunettes_soleil` — Lunettes de soleil (Commun, Visage) — 600 – 900 tris
- **Problème vu** : une barre noire uniforme sur le visage.
- **Garder** : forme rectangle arrondi, reflets blancs.
- **Ajouter** :
  - **monture épaisse** (6 cm) qui déborde des verres de 3 cm, avec un **haut plus épais**
    (« sourcil ») ; monture `#2A2A3A` **satinée 0,50** ;
  - **verres séparés** de la monture, en violet nuit très sombre `#34286A`, **verni 0,95**
    (contraste matière monture / verre : on voit que ce sont des verres) ;
  - tertiaire : 2 reflets peints par verre (barre diagonale + point) ; **charnières** dorées
    `#E8B830` (cubes biseautés 6 cm) aux deux coins extérieurs ;
  - matières : 3 (monture, verres, doré) + reflet.
- **Animation** : A. ressort très léger (0,02 m) : elles « sautillent » sur le museau au saut.

#### 5. `noeud_papillon` — Nœud papillon (Commun, Cou) — 600 – 900 tris
- **Problème vu** : beaucoup trop petit, plat.
- **Garder** : rouge satiné `#C0392B`, nœud central `#8E2A20`.
- **Ajouter** :
  - taille **× 1,6** : ailes 0,30 × 0,26, épaisseur 10 cm au bord, **bombées** (forme de coussin),
    **pincées** vers le centre ;
  - secondaire : **3 plis** par aile (rainures qui rayonnent depuis le nœud) ;
  - tertiaire : **5 pois** blanc cassé `#F4EEE6` en relief (Ø 6 cm) par aile ; le nœud central
    avec 2 rainures horizontales ;
  - matières : ailes satin 0,55, nœud mat 0,30, pois verni 0,85.
- **Animation** : A. ressort : les ailes pivotent de ±6° au saut.

#### 6. `chapeau_haut_de_forme` — Haut-de-forme (Peu commun, Tête) — 800 – 1 200 tris
- **Problème vu** : une tache noire, ruban trop fin.
- **Garder** : feutre `#1E1E1E`, ruban `#C0392B`, bord relevé, tube évasé.
- **Ajouter** :
  - feutre de soie **smoothness 0,40** (au lieu de 0,15) : le reflet du dessus et le contre-jour
    dessinent enfin le chapeau ;
  - bord **plus relevé** (+10 cm sur les côtés) ;
  - ruban **plus haut (14 cm)**, satin 0,55, bordé en haut et en bas d'un **liseré doré**
    `#E8B830` de 4 cm (contraste de valeur sur le noir) ;
  - **nœud plat à 3 lobes** du même rouge sur le côté gauche (secondaire) ;
  - tertiaire : une **carte à jouer** stylisée glissée dans le ruban, blanc cassé `#F4EEE6`, avec
    un cœur rouge `#C0392B` en relief (sans lettre ni chiffre) — la touche « magicien ».
- **Animations** : A. ressort ; B. léger balancement ±3°, période 3 s.

#### 7. `lunettes_coeur` — Lunettes cœur (Peu commun, Visage) — 800 – 1 200 tris
- **Garder** : cœurs roses `#FF6FB5`, monture blanc cassé.
- **Ajouter** :
  - cœurs **bombés** (10 cm au centre, 6 cm au bord), verni 0,90, 2 reflets peints chacun ;
  - **monture-cadre** blanc cassé de 6 cm qui entoure chaque cœur (secondaire) ;
  - tertiaire : un **petit cœur jaune** `#FFD54A` en relief (Ø 8 cm) à chaque coin extérieur,
    à la place des charnières ;
  - matières : verni (cœurs), mat 0,30 (monture), verni (petits cœurs).
- **Animations** : A. ressort ; B. **battement de cœur** : les deux cœurs font `scale 1 → 1,08 → 1
  → 1,05 → 1` (« boum-boum ») en 0,5 s, puis pause, période **1,6 s**.

#### 8. `echarpe` — Écharpe rayée (Peu commun, Cou) — 900 – 1 400 tris
- **Problème vu** : un anneau fin, on dirait un collier.
- **Garder** : rayures vert menthe `#3CC48A` / blanc cassé.
- **Ajouter** :
  - **deux tours** épais (tube 0,18 × 0,10), légèrement décalés et inclinés l'un sur l'autre
    (le tour du haut penche de 8°) ;
  - un **nœud** devant à gauche et **deux pans** : gauche 0,40 de long, droit 0,28 (asymétrie) ;
  - tertiaire : **franges** au bout de chaque pan (5 petits cylindres arrondis Ø 6 cm) ; une
    rainure de tricot à chaque changement de rayure ;
  - matière : laine mat 0,15 (une seule matière, compensée par le contraste vert / blanc).
- **Animations** : A. ressort sur `Anim_PanGauche` et `Anim_PanDroit` ; B. balancement des pans
  ±5°, période 2,2 s, le pan droit décalé de 0,1 s.

#### 9. `collier_fleurs` — Collier de fleurs (Peu commun, Cou) — 1 000 – 1 400 tris
- **Problème vu** : une ligne de points minuscules.
- **Garder** : fleurs roses et blanc cassé, cœurs jaunes, collier qui « pend » devant.
- **Ajouter** :
  - **8 fleurs Ø 0,22** (au lieu de 10 de 0,14), **tournées de 20° vers l'extérieur et le haut**
    (elles regardent la caméra) ;
  - pétales **bombés** (5 par fleur, 6 cm d'épaisseur), cœur en **dôme** verni 0,85 ;
  - secondaire : entre les fleurs, une **perle** blanc cassé (Ø 8 cm) qui fait le lien ;
  - matières : pétales mat 0,30, cœurs et perles vernis.
- **Animations** : A. ressort sur l'ensemble ; B. chaque fleur pivote de ±8° sur elle-même,
  période 3 s, **décalée de 0,12 s** d'une fleur à l'autre (une vague qui fait le tour).

#### 10. `chapeau_sorcier` — Chapeau de sorcier (Rare, Tête) — 1 000 – 1 600 tris
- **Garder** : feutre violet nuit `#4B2E83`, ruban et étoile jaune `#FFD54A`, pointe tordue.
- **Ajouter** :
  - pointe en **S** (2 plis, le bout retombe vers l'arrière) ; le dernier tiers est une pièce
    séparée `Anim_Pointe` (origine au 2e pli) ;
  - bord **ondulé** (6 vagues de 4 cm) ;
  - **étoile Ø 0,20**, bombée, verni 0,90, avec 1 reflet peint ;
  - tertiaire : **3 petites étoiles** jaunes en relief (Ø 8 cm) semées sur le cône ; une **pièce
    rapiécée** carrée (12 cm) en violet `#6A4C9C` avec 4 points de couture en relief (le côté
    « vieux chapeau aimé », pas « vieux modèle ») ;
  - matières : feutre mat 0,20, ruban satin 0,50, étoiles vernies.
- **Animations** : A. ressort sur `Anim_Pointe` ; B. la pointe se balance de ±6°, période 2,5 s ;
  C. éclat sur la grande étoile (points `Eclat_1` à `Eclat_3` sur ses branches).

#### 11. `masque_heros` — Masque de héros (Rare, Visage) — 1 000 – 1 500 tris
- **Garder** : bandeau framboise `#D63A6E`, bord jaune `#FFD54A` de 4 cm minimum, trous autour des
  yeux, aucun emblème (§2.8).
- **Ajouter** :
  - coins extérieurs qui **remontent en pointe** (forme dynamique, « vitesse ») ;
  - le **bord jaune en relief de 2 cm** (au lieu d'être à plat) et **verni 0,80**, bandeau satin
    0,50 (contraste matière et valeur) ;
  - une **rainure** au milieu du bandeau, au-dessus du museau ;
  - deux **pans noués** derrière, plus longs (0,30), qui **dépassent sur les côtés** (visibles de
    face), pièces `Anim_PanGauche` / `Anim_PanDroit`.
- **Animations** : A. ressort sur les pans ; B. les pans « flottent au vent » ±10°, période 1,2 s,
  décalés de 0,08 s ; C. éclat qui court sur le bord jaune.

#### 12. `ailes_ange` — Petites ailes d'ange (Rare, Dos) — 1 200 – 1 800 tris
- **Problème vu** : deux petites plaques blanches.
- **Garder** : blanc cassé `#F4EEE6`, ombre des plumes `#D9D2E4`, battement ±8° en 1,6 s.
- **Ajouter** :
  - taille **× 1,4** (0,70 × 0,48 par aile) ;
  - **3 rangées de plumes en couches** décalées de 2 cm : 5 grandes plumes rondes (bas),
    4 moyennes, 3 petites (haut) ; la rangée du bas en `#D9D2E4` (ombre), les deux autres en
    blanc cassé (contraste de valeur par couches) ;
  - tertiaire : une **broche dorée** `#E8B830` ronde (Ø 10 cm) à la racine de chaque aile ;
  - matières : plumes mat 0,30, broche dorée.
- **Animations** : B. battement ±8° (1,6 s) **avec chevauchement** : chaque rangée suit la
  précédente avec **0,08 s** de retard (l'aile « ondule ») ; A. ressort sur les plumes du bas ;
  C. éclat sur la broche.

#### 13. `chapeau_couronne` — Couronne royale (Super rare, Tête) — 1 400 – 2 200 tris
- **Problème vu** : paraît en bronze terne ; pointes petites.
- **Garder** : or `#FFC83A` (le seul objet porté en or trésor), 5 pointes, rubis `#D62839` devant.
- **Ajouter** :
  - **biseau 3 segments + normales pondérées** : c'est ce qui fera briller l'or (avec la
    Reflection Probe) ;
  - pointes **plus hautes (0,22)**, arrondies, légèrement **évasées vers l'extérieur** (silhouette
    en couronne de conte, sans copier un modèle connu) ;
  - secondaire : un **bandeau en relief** autour de l'anneau avec une frise de losanges ;
  - perles en **nacre** `#F4EEE6` (metallic 0,2, smoothness 0,85) au lieu d'or : contraste blanc
    sur or ;
  - rubis avant **Ø 0,16** taillé (3 segments), plus **4 petits rubis** (Ø 7 cm) sur le bandeau ;
  - matières : or, nacre, pierre.
- **Animations et effets** : B1. le rubis avant « respire » : émission **0,2 ↔ 0,5** (couleur
  `#D62839`), période 2 s ; B2. légère rotation de ±2° (elle tient « royalement ») ; C. éclat toutes
  les 2,5 s sur les pointes ; D. **3 étincelles** 3D dorées qui montent autour de la couronne.

#### 14. `ailes_papillon` — Ailes de papillon (Super rare, Dos) — 1 400 – 2 200 tris
- **Garder** : lobes turquoise `#2BB8C4`, taches orange `#F28C38`, bord `#34286A`, battement ±20° en 0,9 s.
- **Ajouter** :
  - lobes au **bord festonné** (vagues de 5 cm) ; **3 nervures** en relief `#34286A` par lobe
    (6 cm de large) qui partent de la racine ;
  - taches **bombées** et **émissives 0,4** (≤ 10 % de la surface : autorisé au Super rare) : elles
    luisent doucement ;
  - un petit **corps** central (capsule `#34286A`, 0,25 de haut) avec **2 antennes** terminées par
    une boule (Ø 7 cm), pièces `Anim_AntenneGauche` / `Anim_AntenneDroite` ;
  - matières : lobes brillants 0,65, nervures et corps mat 0,30, taches émissives.
- **Animations et effets** : B1. battement avec **courbe** (descente rapide, remontée lente :
  `ease-in-out` déséquilibré 40 % / 60 %) ; B2. les taches passent de 0,25 à 0,5 d'émission sur
  chaque battement ; A. ressort sur les antennes ; D. **4 écailles** (petits losanges turquoise
  émissifs 0,4) qui tombent des ailes en tournoyant.

#### 15. `aureole` — Auréole lumineuse (Légendaire, Tête) — 1 500 – 2 200 tris
- **Problème vu** : un anneau blanc plat sans relief (la version v3 aux 8 perles est un bon début).
- **Garder** : tore Ø 0,70, `#FFF1A8`, flottement ±0,04 m (2 s), rotation 30°/s, retard de ressort,
  8 perles en chenillard (`v3.md` §8.2).
- **Ajouter** :
  - section **plus épaisse (10 cm)** en **deux matières** : un **cœur en doré déco** `#E8B830`
    (métal, la face du dessous et l'extérieur) et une **bande émissive** `#FFF1A8` (1,2) sur le
    dessus et l'intérieur : le contraste métal / lumière fait « objet précieux » au lieu de
    « néon » ;
  - les 8 perles **posées sur** le cœur doré, Ø 8 cm ;
  - **4 petits rayons** (losanges plats de 10 cm) qui pointent vers l'extérieur, entre les perles ;
- **Animations et effets** : B1. flottement + rotation (gardés) ; B2. chenillard (gardé) ; B3. les
  4 rayons tournent en sens inverse de l'anneau (−20°/s) ; C. éclat toutes les 1,5 s ;
  D. **pluie de lumière** : jusqu'à **8** petites étoiles qui tombent de l'anneau (0,4 m en 1 s)
  en s'effaçant ; traînée courte (0,25 s) quand la tête bouge.

#### 16. `planete` — Petite planète (Secret, Dos) — 1 800 – 3 000 tris
- **Problème vu** : petite bille lointaine, on ne la remarque presque pas sur la planche.
- **Garder** : boule lavande `#B48CFF`, anneau `#FFD54A` émissif, orbite de rayon 0,9 en 6 s,
  rotation propre 90°/s.
- **Ajouter** :
  - boule **Ø 0,40** (au lieu de 0,30) avec **2 bandes** en relief violet magique `#8E5BD6` et
    **3 cratères** en creux (tertiaire) ;
  - anneau **en deux parties** : un anneau principal Ø 0,70 et un fin anneau extérieur Ø 0,82,
    séparés par un vide de 3 cm (silhouette « planète » immédiate) ;
  - une **petite lune** (Ø 0,09, blanc cassé, mat) qui tourne autour de la planète (rayon 0,32,
    un tour en 2 s, plan incliné de 30°) ;
  - matières : boule satinée 0,60 **sans émission** (le contraste entre une boule éclairée et un
    anneau qui luit est ce qui la rend précieuse), anneaux émissifs, lune mate.
- **Animations et effets** : B1. orbite (gardée) ; B2. rotation propre (gardée) ; B3. la lune ;
  B4. émission des anneaux qui oscille **0,5 ↔ 1,2** (période 2 s, l'anneau extérieur décalé
  d'une demi-période) ; C. éclat toutes les 1 s, teinte qui fait le tour de l'arc-en-ciel
  (saturation 0,75) ; D. **traînée de poussière d'étoile** : jusqu'à **16** petites étoiles
  laissées derrière la planète sur son orbite, qui s'effacent en 0,6 s ; traînée 0,4 s.

### 13.9 Méthode et contrôle (pour l'artiste 3D)

1. **Ordre de travail** : silhouette (primaire) → vérifier en noir à 64 px → secondaires → bevel +
   normales pondérées → tertiaires → matières → pièces `Anim_*`, `Eclat_*`, `Fx_Emetteur`.
2. **Mise à jour de `blender/commun.py`** : ajouter les préréglages du §13.4 (`M_DoreDeco`,
   `M_Nacre`, verni, satin) et une fonction de biseau v5 (Amount, 2 segments, Harden Normals,
   Weighted Normal) partagée par tous les scripts d'accessoires.
3. **Planche de contrôle v5** (`docs/captures/cheval_planche_v5.png`, même cadrage que
   `cheval_planche.png`) rendue avec le **monde en dégradé** (§13.4), plus une planche sur 4 skins
   difficiles : **bleu ciel, rose bonbon, argent poli, galaxie**. Critères : chaque accessoire se
   reconnaît en silhouette noire à 64 px ; on distingue au moins 2 matières ; l'or brille ; rien
   ne cache les yeux (sauf les lunettes de soleil, voulu) ; un accessoire Super rare a l'air plus
   précieux qu'un Commun **sans** regarder sa couleur de rareté.
4. **Dans Unity** : capture en mode Play, caméra de jeu, avec les 4 accessoires équipés en même
   temps (tête + visage + cou + dos) : rien ne se traverse, le cheval reste lisible, le Bloom ne
   dépasse pas l'éclat du coffre au trésor.
5. **Icônes** : une fois les modèles refaits, les icônes d'accessoires (lots 2 et 3) ne leur
   ressembleront plus. Elles seront **régénérées dans un lot suivant** en joignant les rendus
   Blender v5 comme référence de forme (même méthode que les skins du cheval au lot 3).

### 13.10 À éviter (accessoires v5)
- Ajouter des détails **plus petits que 6 cm** dans le modèle : ils scintillent et disparaissent.
- Le réalisme (fils de laine, plumes fines, grain de feutre) : on reste « jouet », §8.
- Les effets sur un objet Commun ou Peu commun (éclats, émission, particules) : ils doivent se mériter.
- Plus de 2 reflets peints par pièce ; un reflet peint ailleurs qu'en haut à gauche.
- Une animation qui fait sortir l'accessoire de sa zone ou qui cache les yeux longtemps.
- Les contours noirs « dessin animé » (coque inversée) sur les seuls accessoires : le cheval et
  les coffres n'en ont pas, ils auraient l'air collés. Le liseré vient du contre-jour.
- Copier un objet reconnaissable d'un autre jeu (couronne, ailes, masque, lunettes de marque).

---

## 14. Ouverture des boîtes en 3D

> **À quoi sert cette section ?** L'ouverture d'une boîte (`boites.md` §7.6, `v3.md` §8.3) se
> faisait avec une image plate de la boîte. En v5, **une vraie boîte 3D** apparaît au centre,
> tremble, perd son nœud, son couvercle saute, un faisceau de lumière en sort et l'objet gagné
> s'élève en tournant. Cette section donne au programmeur le **plateau**, la **fiche du modèle**,
> la **chronologie chiffrée** et les **règles d'équité**. Les temps des phases communes
> **restent ceux de `boites.md` §7.6** (0 / 0,3 / 1,5 / 1,75 s) : les sons existants restent calés.

### 14.1 La règle d'équité (à relire avant de coder)

**Jusqu'à l'instant R = 1,75 s, rien ne doit permettre de deviner la rareté.** Concrètement :
1. **Même boîte** (modèle, couleurs, taille) pour toutes les raretés d'une même boîte. Seul le
   *type* de boîte change le modèle (skins / accessoires, et plus tard Pirates / Animaux).
2. **Même chronologie, mêmes amplitudes, mêmes sons** jusqu'à R, quel que soit le tirage.
3. **Toute la lumière avant R est blanche neutre** (`#FFF6E8`) : la lueur qui fuit sous le
   couvercle, le faisceau qui jaillit, le flash. **Aucune couleur de rareté avant R.**
4. Les petites variations « pour faire vivant » (sens des sauts, trajectoire du nœud et du
   couvercle) viennent d'un **générateur aléatoire séparé** (`new System.Random()`), **jamais** du
   résultat du tirage ni de la même graine que lui.
5. L'objet gagné est créé **désactivé** et caché ; il n'existe à l'écran qu'à partir de R. Pas
   de silhouette dans la boîte, pas d'ombre, pas de lumière qui le trahirait.
6. La caméra fait **le même mouvement** jusqu'à R ; les différences (poussée, secousse) commencent
   à R.

### 14.2 Le plateau 3D (où et comment on le filme)

- **Pourquoi pas sur la tour ?** Le décor, les coffres et le cheval gêneraient, et l'interface UI
  Toolkit se dessine **toujours par-dessus** la 3D : un voile UI cacherait la boîte. On filme donc
  la boîte sur un **petit plateau à part**, par sa propre caméra, en plein écran.
- **Emplacement** : un objet `PlateauOuverture` placé **à 500 m sur le côté** de la tour (par ex.
  (500 ; 0 ; 0)), hors de la vue de la caméra du jeu et loin de ses ombres. Désactivé hors ouverture.
- **Caméra `CameraOuverture`** : position locale (0 ; 1,9 ; −4,4), regarde (0 ; 1,0 ; 0), champ
  de vision **34°**, fond *Solid Color* **`#1F1730`**, post-traitement activé, **priorité (Depth)
  plus haute** que la caméra du jeu. Activée seulement pendant l'ouverture et l'écran de résultat ;
  le HUD est caché comme aujourd'hui.
- **Socle** : disque Ø **1,8 m**, 0,25 m de haut, dessus à y = 0, en pierre claire `#8A90B0`
  (`M_PierreClaire`) avec un filet incrusté `#6A4C9C` (`M_Incrustation`) à 8 cm du bord, biseau 3 cm.
  Il pose la boîte « quelque part » au lieu de la faire flotter dans le vide.
- **Lumières** : la lumière principale du jeu éclaire déjà le plateau (même direction, même
  couleur : cohérent) ; on ajoute une **Spot Light** `#FFF1D6`, intensité 4, portée 8 m, angle 50°,
  au-dessus et devant à gauche (−1,5 ; 4 ; −2), ombres douces ; une **Point Light** `LumiereInterieure`
  dans la boîte (0 ; 0,55 ; 0), portée 3 m, éteinte au départ ; une **Reflection Probe** au centre
  (le ruban et le médaillon sont en métal).
- **Fond** : le `#1F1730` uni ; la vignette du post-traitement (0,25) suffit à concentrer le regard.

**Ce qui reste en UI Toolkit** (par-dessus la 3D) : le fondu d'entrée, le **flash blanc**, le
texte de rareté, le nom de l'objet, les étincelles 2D, les confettis et toute la cinématique
Secret, l'écran de résultat et ses boutons. **Ce qui passe en 3D** : la boîte, le nœud, le
couvercle, la lueur, le faisceau, le **halo et les rayons** (deux plans derrière l'objet, mêmes
textures calculées par le code qu'au §10.3, matériau URP Lit transparent avec émission), les
étincelles 3D et l'objet gagné.

### 14.3 Fiche du modèle : `boite3d` (un modèle, plusieurs jeux de couleurs)

**À quoi elle ressemble** : la boîte-cadeau des images `boite_skins_1` / `boite_accessoires_1`
(§2.8) en vrai volume : un cube aux arêtes très arrondies, un couvercle qui déborde, un ruban
en croix, un gros nœud à deux boucles et un médaillon en laiton sur la face avant.

**Dimensions** : corps **1,00 × 0,80 × 1,00** (y de 0 à 0,80) ; couvercle **1,10 × 0,24 × 1,10**
(il déborde de 5 cm, de y = 0,80 à 1,04) ; nœud jusqu'à y ≈ **1,40**. Biseau **5 cm, 2 segments**
+ normales pondérées (§13.3).

**Hiérarchie et pivots** (le code anime ces objets séparément)
```
Boite3D             origine au sol, au centre (0 ; 0 ; 0)
├── Corps           le cube (M_BoiteCorps)
├── RubanCorps      bandes verticales avant/arrière/côtés, 16 cm de large, 2 cm de relief (M_Laiton)
├── Medaillon       disque Ø 0,30, 4 cm, sur le ruban au milieu de la face avant (M_Laiton)
│   └── Embleme     l'emblème en relief (goutte 3 couleurs / mini haut-de-forme, §2.8)
├── Interieur       parois de 5 cm et fond à y = 0,15, mat `#2A1E3A` (on voit dedans une fois ouvert)
├── Lueur           plan sous le couvercle, à y = 0,78, `#FFF6E8`, émission pilotée par le code
└── Couvercle       origine au CENTRE DU BAS du couvercle (0 ; 0,80 ; 0)
    ├── RubanCouvercle  la croix du ruban sur le dessus (M_Laiton)
    └── Noeud           origine à la base du nœud (0 ; 1,04 ; 0) : 2 boucles, 2 pans, nœud central
```
- **Couleurs** : celles du §2.8 (skins : corps `#8E5BD6`, bord du couvercle `#6A4C9C` ;
  accessoires : `#2BB8C4`, `#1F8A94` ; ruban, nœud, médaillon laiton `#C99A1E`). Corps mat 0,30,
  laiton metallic 1 / smoothness 0,60. Les futures boîtes **Pirates** et **Animaux** réutilisent le
  même modèle avec leurs couleurs et leur emblème (fournis avec le catalogue du game designer).
- **Lueur** : c'est une lumière d'effet (§4.1 v5), pas la matière de la boîte : la boîte elle-même
  n'émet jamais.
- **Triangles** : **1 500 – 2 200**. Script : `blender/boite_3d.py` → `boite3d_skins_1.glb`,
  `boite3d_accessoires_1.glb`.

**L'objet gagné en 3D**
- **Accessoire** : son propre modèle, **avec ses animations** (§13.5), mis à l'échelle pour tenir
  dans une sphère de **Ø 1,1 m**, sans le cheval. Planète : sans orbite (elle tourne sur elle-même,
  sa lune autour).
- **Skin** : un **mini cheval** (le modèle `Joueur`, échelle 0,55, ~1,1 m de haut) portant la
  matière gagnée **avec tous ses effets** (motif, cristal, arc-en-ciel, galaxie).
- Une fois sorti, il tourne en plateau tournant (45°/s) et flotte de ±0,05 m (période 2 s).

### 14.4 Chronologie commune (identique pour toutes les raretés)

Temps en secondes depuis le clic sur « Ouvrir ». Échelles notées (x ; y ; z).

| Temps | Durée | Boîte | Nœud / couvercle | Lumière, effets | Caméra | Son |
|---|---|---|---|---|---|---|
| **0,00** | 0,10 | — | — | fondu UI : voile `#1F1730` 0 → 1 ; à 0,10 on active le plateau et `CameraOuverture`, on cache le HUD | — | `pieces_achat` |
| **0,10** | 0,20 | tombe de y = 2,2 à 0 en **0,14 s** (`ease-in`), touche le socle à 0,24 : écrasement (1,18 ; 0,78 ; 1,18) → (0,94 ; 1,08 ; 0,94) → 1 à **0,36** | suivent la boîte | voile UI 1 → 0 en 0,12 s (on découvre le plateau) | fixe | — |
| **0,30** | **1,20** | **secousse** : rotation Z alternée à **10 allers-retours/s**, amplitude **2° → 10°** (linéaire) ; rotation Y ±4° à 2,5 Hz ; **3 petits sauts** de 0,10 m (0,1 s montée + 0,1 s descente) à **0,55 / 0,95 / 1,35 s**, chacun suivi d'un écrasement (1,08 ; 0,90 ; 1,08) de 0,06 s | à chaque saut, le couvercle se soulève de **0,03 m** avec 0,05 s de retard puis retombe ; le nœud fait ressort (§13.5 A) | `Lueur` : émission 0,3 → 1,0 ; `LumiereInterieure` **blanche** `#FFF6E8` 0 → 0,8 ; elle ne se voit que par la fente du couvercle | **poussée lente** de 0 → 0,4 m vers la boîte (tension) | `boite/boite_secoue` |
| **1,50** | 0,08 | **anticipation** : la secousse s'arrête, la boîte s'écrase (1,12 ; 0,82 ; 1,12) en `ease-in` | couvercle plaqué | lueur 1,0 → 1,2 | — | `boite/boite_ouverture` |
| **1,58** | 0,50 | — | **le nœud s'envole** : vitesse vers le haut 5 m/s, de côté ±0,8 m/s (aléatoire neutre), rotation 900°/s autour d'un axe au hasard, gravité 9 m/s² ; échelle 1 → 0 de 1,90 à 2,10, puis désactivé | — | — | — |
| **1,62** | 0,60 | **détente** : (0,92 ; 1,15 ; 0,92) puis retour à 1 à **1,80** (`ease-out-back`) | **le couvercle saute** : vers le haut 6 m/s, vers l'arrière 1,5 m/s, bascule en arrière à −360°/s (axe X), ±40°/s en Z (aléatoire neutre), gravité 9 m/s² ; sort du cadre derrière, désactivé à 2,30 | **faisceau blanc** : un cône ouvert sort de la boîte, hauteur 0 → **3,5 m** en 0,13 s, rayon bas 0,42 m, rayon haut 0,9 m, `#FFF6E8`, émission 1,2, opacité 0,5 ; **flash blanc UI** 0 → **0,7** (à 1,68) → 0 (à **1,75** = R, v6 §13) | — | — |
| **1,75 = R** | selon la rareté | voir 14.5 | — | **révélation** (14.5) | voir 14.5 | `boite/revelation_<rarete>` |

Notes :
- Le flash blanc commence à 1,62 au lieu de 1,50 (il accompagne le couvercle) : c'est le seul
  temps qui change par rapport à `boites.md` §7.6, et il reste identique pour toutes les raretés.
- Les mouvements aléatoires (nœud, couvercle) sont tirés **au clic**, par le générateur séparé
  (14.1 règle 4).

### 14.5 La révélation selon la rareté (à partir de R)

**Commun à toutes** :
- **R → R + 0,15 s** : le faisceau passe du blanc à la **couleur de base de la rareté** (§2.7) ;
  `LumiereInterieure` prend la même couleur.
- **R + 0,05 s** : l'objet gagné apparaît dans la boîte (y = 0,4, échelle 0,3), **monte à y = 1,55
  en 0,45 s** (`ease-out-back`) en grandissant jusqu'à 1 ; il tourne d'abord vite (**360°/s**) puis
  ralentit jusqu'à **45°/s** en 0,8 s (il « se présente »).
- **R + 0,1 s** : halo et rayons 3D derrière l'objet (plans face caméra), gerbe d'étincelles 3D
  (étoiles 4 branches 8 cm, émissives, couleur de rareté, vitesse 1,5 à 3,5 m/s, gravité 4 m/s²,
  vie 0,8 à 1,2 s) ; les étincelles 2D, le texte de rareté et le flash coloré de `boites.md` §7.6
  restent en UI, **sous** l'objet à l'écran (texte à y ≈ 800 px).

| Rareté | Durée (inchangée) | Faisceau : rayon haut / émission | Lumière intérieure | Halo 3D | Rayons 3D | Étincelles 3D | Secousse caméra | Poussée caméra | Objet |
|---|---|---|---|---|---|---|---|---|---|
| Commun | 0,5 s | 0,6 m / 0,6 | 0,6 | Ø 1,6 m, opacité 0,5 | — | 0 | — | — | monte à 1,55 |
| Peu commun | 0,7 s | 0,7 m / 0,8 | 0,9 | Ø 1,8 m, 0,6 | — | 8 | — | — | idem |
| Rare | 1,0 s | 0,8 m / 1,0 | 1,2 | Ø 2,0 m, 0,7 | 8 rayons, 30°/s | 14 | — | +0,2 m | idem |
| Super rare | 1,4 s | 0,9 m / 1,2 | 1,5 | Ø 2,2 m, pulse 1 ↔ 1,1 | 12, 45°/s | 22 | ±0,02 m, 0,3 s | +0,3 m | idem |
| Légendaire | 2,0 s | 1,0 m / **1,5** | 2,0 | Ø 2,6 m, pulse | 16, 60°/s | 36 (orange `#FF9F1C` et or `#FFC83A`) | ±0,035 m, 0,4 s | +0,5 m | monte à **1,75**, échelle **1,3** puis revient à 1 et 1,55 en 0,4 s |
| Secret | voir 14.6 | — | — | — | — | — | — | — | — |

- Le plafond d'émission **1,5** est respecté partout.
- **Fin de la révélation** : le faisceau baisse à 60 % de son émission, l'objet continue de
  tourner et de flotter ; l'écran de résultat apparaît **0,2 s** après (`boites.md` §7.7), ses
  textes et boutons sous l'objet (à partir de y ≈ 760 px).
- **« Rouvrir »** : fondu UI `#1F1730` 0 → 1 en 0,15 s, on remet la boîte fermée (nœud et
  couvercle à leur place), et on reprend à **0,10 s** de la chronologie.
- **« OK »** : fondu 0,15 s, plateau et `CameraOuverture` désactivés, HUD réaffiché.

### 14.6 Le Secret (garde la cinématique de `v3.md` §8.3)

- À **R**, la coupure noire UI recouvre tout, comme aujourd'hui. Derrière, on **éteint** le faisceau
  et la lumière intérieure (rien ne se voit pendant le noir).
- **R + 0,5 → R + 2,2** (battement, fissures) : inchangé, en UI.
- **R + 2,2 — l'éclat** : quand le noir se brise, on découvre le plateau : faisceau **arc-en-ciel**
  (teinte qui fait le tour en 2 s, saturation 0,75, émission 1,2), halo 3D arc-en-ciel Ø 2,8 m,
  20 rayons de couleurs alternées à 60°/s.
- **R + 2,4 — l'arrivée** : **l'objet 3D remplace la carte** : il tombe de y = 4 à 1,55 en 1,0 s
  (`ease-out` avec léger dépassement) en faisant **3 demi-tours** sur lui-même qui ralentissent.
- **R + 3,4 — l'impact** : la boîte s'écrase (1,15 ; 0,85 ; 1,15) en 0,1 s puis revient ; **40
  étincelles 3D** multicolores ; secousse caméra ±0,05 m pendant 0,5 s ; les confettis UI commencent.
- **R + 3,7 → R + 5,4** : « SECRET !!! » et « Une chance sur 100 ! » en UI, inchangés ; puis calme.

### 14.7 Passer l'animation

Mêmes règles que `boites.md` §7.6 (à partir de 0,5 s après le clic). **État final directement** :
boîte ouverte sans couvercle ni nœud, faisceau à la couleur de rareté (60 %), halo et rayons,
objet en place qui tourne, textes ; **pas** d'étincelles, de confettis ni de secousse.

### 14.8 Résumé pour le programmeur (qui fait quoi)
- **Logique** : le tirage est fait et sauvegardé **avant** l'animation (inchangé). Le script
  d'animation reçoit seulement « quelle boîte, quel objet, quelle rareté » et ne décide rien.
- **Un script par rôle** (un MonoBehaviour par fichier) : par exemple `OuvertureBoite3D`
  (chronologie), `FaisceauOuverture` (cône, couleur, émission), `EtincellesOuverture` (réserve
  d'étincelles 3D) ; l'UI existante (`BoxOpening`) garde le flash, les textes et les boutons.
- **Matériaux** : URP Lit partout. Faisceau, halo, rayons : *Surface Type Transparent*, *Render
  Face Both* (faisceau), émission ; textures de dégradé calculées par le code (aucune image).
- **Réserves** : 40 étincelles 3D, réutilisées (pas de création pendant l'animation).

### 14.9 Contrôle d'équité (à faire en capture)
- Ouvrir **10 fois** la même boîte et filmer jusqu'à R : les images doivent être **indiscernables**
  d'un tirage à l'autre (à part les trajectoires aléatoires du nœud et du couvercle).
- Vérifier qu'avant R aucune couleur autre que le blanc neutre n'apparaît (faisceau, lueur, flash).
- Vérifier que l'objet gagné est invisible (désactivé) avant R, même par la fente du couvercle.

---

## 15. Interface v6

> **Pourquoi cette section ?** L'auteur a testé la v5.1 (`docs/specs/v6-a-faire.md`, points 1 à
> 6, 8, 14, 18 et 24). Il trouve les bulles des boutons « dégueu »
> (`docs/references/v6_ref_bulles_colonne.png`), la Renaissance « vraiment moche »
> (`docs/captures/v5_1_renaissance.png`) et la barre du haut collée au bord
> (`v6_ref_barre_haut.png`). Ici, toutes les **valeurs exactes** pour le programmeur.
> Les tailles sont en **pixels de référence 1920 × 1080**, comme partout dans le §12.
> Le style reste celui du §12.18 : contour d'encre **`#110D1F`** (la valeur du code), couleurs
> franches, tout en relief, rien de copié d'un jeu existant (§12.1).

### 15.1 Les bulles des boutons

#### 15.1.1 Pourquoi elles font « sale » aujourd'hui (capture de l'auteur)

1. La **face est violet sombre et plate** : on dirait un trou, pas un bonbon.
2. Le **reflet** est un rectangle gris à 18 % aux bords nets, qui **dépasse sur l'anneau** en
   haut : il fait tache au lieu de briller.
3. L'**épaisseur** (bordure basse de 10 px d'une autre couleur) dessine un croissant qui ne
   suit pas le cercle : on dirait un défaut d'impression.
4. L'**ombre** est un disque dur, décalé, sans flou.
5. UI Toolkit ne sait faire ni dégradé rond, ni flou, ni lumière : en USS pur, on ne peut pas
   aller beaucoup plus loin que ce résultat.

#### 15.1.2 La méthode retenue : des images rendues dans Blender

**Décision : les bulles sont des images PNG à fond transparent, rendues par un script Blender
(`blender/rendu_bulles.py`, à écrire par l'artiste 3D), sans icône dedans.** L'icône (ou la
croix, ou le texte) est posée **par-dessus** par l'interface, comme aujourd'hui.

Pourquoi Blender plutôt que Gemini ou le USS :
- **Mesures au pixel près** : obligatoire pour le 9-slice (Rejouer, Continuer, Encaisser) et
  pour que l'icône tombe pile au centre de la face.
- **La même bulle dans toutes les couleurs** : on change un paramètre, pas un prompt. Gemini
  dessinerait 8 bulles légèrement différentes (taille, reflet, angle).
- **Fond déjà transparent** : pas de détourage. C'est justement le détourage qui a abîmé la
  roue dentée (point 6).
- **Même lumière que les icônes** rendues dans Blender au lot 6 (`blender/rendu_icones.py`) : la
  bulle et l'icône semblent sortir de la même boîte de jouets.
- Aucune limite de génération, aucun changement de compte (règle de l'auteur).

**Plan B** (si le rendu Blender ne plaît pas à l'auteur) : les prompts Gemini du
`art/prompts/lot7.md` §5. Dans ce cas, les variantes « appuyé » sont faites en USS (§15.1.7) et les
bords du 9-slice sont mesurés sur l'image obtenue.

#### 15.1.3 Anatomie commune (vue de face, en couches, de l'arrière vers l'avant)

| Couche | Rendu | Pourquoi |
|---|---|---|
| 1. Ombre portée | la silhouette du contour, décalée vers le bas, **floutée**, `#110D1F` à 35 % | le bouton flotte au-dessus du ciel, même sur le bleu clair du palier 1 |
| 2. Contour | `#110D1F` **opaque**, autour de la face **et** du socle réunis | le trait d'encre du style |
| 3. Socle (épaisseur) | même forme que la face, **décalée vers le bas**, couleur « épaisseur » de la famille | c'est l'épaisseur du jeton : elle suit exactement la courbe, plus de croissant bancal |
| 4. Face | **dégradé vertical** clair (haut) → face (55 %) → mi (bas), légèrement bombée (bord plus sombre de 8 %) | un bonbon, pas un trou |
| 5. Biseau intérieur | anneau de **4 px** collé au bord de la face : moitié haute « clair » à 70 %, moitié basse « mi » à 60 % | l'arête qui accroche la lumière |
| 6. Lueur centrale | disque flou blanc à **14 %** derrière l'emplacement de l'icône | détache l'icône de la face |
| 7. Reflet | ovale **entièrement dans la face** (≥ 8 px du bord), blanc **60 % en haut → 0 % en bas**, bords doux | le vernis ; il ne touche jamais le contour |
| 8. Éclat | petit point blanc à **90 %**, en haut à gauche du reflet | l'étincelle de la lampe |
| 9. Lumière renvoyée | arc de **3 px** « clair » à 40 % le long du bas de la face | le bas ne fait pas « sale » |

Lumière : celle du §9.1 (clé chaude `#FFF1D6` en haut à gauche, remplissage violet). Gestion des
couleurs **Standard** (pas AgX ni Filmic), sinon les codes hex dérivent.

#### 15.1.4 Couleurs par fonction (la face entière prend la couleur)

La couleur n'est plus un anneau autour d'une face sombre : **toute la bulle** prend la couleur
de la fonction. Les icônes ont maintenant leur propre contour noir (v5.1) : elles se lisent sur
une face colorée. Deux couleurs changent par rapport au §12.17.2, pour que l'icône ne disparaisse
pas dans sa bulle :
- **Améliorations** passe de l'or au **vert** (la flèche est dorée : or sur or, on ne la voyait plus) ;
- **Renaissance** passe de l'orange « flamme » à la **nuit étoilée** (l'icône est orange).

| Bulle | Clair (haut) | Face | Mi (bas) | Épaisseur | Contour du texte posé dessus |
|---|---|---|---|---|---|
| Boutique (rose) | `#FF8FB8` | `#FF4F8B` | `#E0336F` | `#A3214F` | — |
| Inventaire (bleu) | `#7DB5FF` | `#3B7DD8` | `#2F68BE` | `#1F4A8C` | — |
| Index (turquoise) | `#5FE0CF` | `#1FB5A3` | `#17998A` | `#0F6B61` | — |
| Améliorations (vert) | `#8BEA6B` | `#3DBE4F` | `#2EA040` | `#1E7030` | — |
| Classement (violet) | `#B49CFF` | `#7B5BF0` | `#6545D6` | `#4530A8` | — |
| Renaissance (nuit étoilée) | `#6A5AD8` | `#3A2C9A` | `#2A1F78` | `#17104A` | — (+ 6 étoiles, ci-dessous) |
| Paramètres, flèches (ardoise) | `#8A84B8` | `#5A5488` | `#4A4472` | `#2E2A4E` | `#1E1A33` |
| Fermer (rouge, inchangé) | `#FF8A8A` | `#F0484E` | `#D83A40` | `#9A2228` | `#5E1013` |
| Route : palier atteint (or) | `#FFE66B` | `#FFC83A` | `#F0A820` | `#B87410` | `#6B4300` |
| Encaisser (vert) | `#7BE38F` | `#2FB85A` | `#26A04D` | `#1A6E36` | `#0E3D21` |
| Rejouer (violet) | `#A08CFF` | `#6C4FE0` | `#5A3FCC` | `#3F2A9E` | `#21155E` |
| Continuer (aqua « diamant ») | `#8FEAFF` | `#29B6E8` | `#1E9BD0` | `#12689A` | `#0B4A6E` |
| Désactivé (gris) | `#A8A4BC` | `#7E7A94` | `#6E6A84` | `#4E4A64` | `#45425A` |

- **Nuit étoilée** : 6 petites étoiles à 4 branches `#FFF1D6` à 70 %, de 6 à 10 px, réparties
  dans la couronne extérieure de la face (entre 8 et 20 px du bord), jamais au centre (l'icône
  les cacherait).
- **Continuer** est aqua parce qu'il se paie en diamants (`#4FD8F0`, lot 5) : la couleur dit le
  prix avant même de lire. Il se distingue du vert Encaisser et du violet Rejouer posés à côté.
- **Rouge** : réservé à la croix (règle du §12.2). L'or est réservé à la Route (palier atteint).
- L'info-bulle (§12.17.4) prend la couleur « Face » de la bulle pour sa bordure haute.

#### 15.1.5 Les fichiers et leurs mesures

Rendus à **2 fois la taille d'affichage** (net sur les grands écrans), affichés à la taille
indiquée. Dossier : `art/bulles/`, copiés dans `TourDeLaChance-Unity/Assets/Art/bulles/`.
Import Unity : *Texture Type* Default, *Alpha Is Transparency* coché, *Wrap Mode* Clamp,
*Generate Mip Maps* coché, *Filter* Bilinear.

**A. Grande bulle ronde (colonnes)** : `bulle_rond_<fonction>.png` et `bulle_rond_<fonction>_appui.png`,
avec `<fonction>` = `boutique`, `inventaire`, `index`, `ameliorations`, `classement`,
`renaissance` (12 images). Image **320 × 320**, affichée **160 × 160**.

| Mesure (affichage) | Repos | Appuyé |
|---|---|---|
| Face | disque Ø **112**, centre (80, **70**) | disque Ø 112, centre (80, **76**) |
| Socle | disque Ø 112, centre (80, 80) : **10 px** d'épaisseur visible | même socle : **4 px** visibles |
| Contour | **6 px** autour de face + socle : de x 18 à 142, de y 8 à 142 | de y 14 à 142 |
| Ombre | décalée de **+6 px**, flou **8 px** | décalée de +3 px, flou 4 px |
| Reflet | ovale 76 × 34, centre (80, 38) | même forme, **40 %** au lieu de 60 %, centre (80, 44) |
| Éclat | Ø 10, centre (52, 34) | Ø 8, centre (52, 40) |
| Face entière | couleurs du tableau | **assombries de 6 %** |
| Place de l'icône | carré **92 × 92**, coin à (34, 24) | coin à (34, **30**) |

**B. Petite bulle ronde** (croix, roue dentée, flèches de la fiche, nœuds de la Route) :
`bulle_petit_<couleur>.png` et `_appui`, avec `<couleur>` = `rouge`, `ardoise`, `or`
(6 images). Image **192 × 192**, affichée **96 × 96** (ou 120 × 120 pour les flèches, même image).

| Mesure (affichage 96) | Repos | Appuyé |
|---|---|---|
| Face | Ø **62**, centre (48, **40**) | centre (48, **45**) |
| Socle | Ø 62, centre (48, 48) : **8 px** visibles | **3 px** visibles |
| Contour | **5 px** : de x 12 à 84, de y 4 à 84 | de y 9 à 84 |
| Ombre | +5 px, flou 6 px, 30 % | +2 px, flou 3 px |
| Reflet | ovale 42 × 18, centre (48, 23) | 40 %, centre (48, 28) |
| Éclat | Ø 6, centre (33, 22) | centre (33, 27) |

**C. Bulle rectangulaire en 9-slice** (Encaisser, Rejouer, Continuer, et tout gros bouton
d'action qui voudra ce rendu) : `bulle_rect_vert.png`, `bulle_rect_violet.png`,
`bulle_rect_aqua.png` (+ `_appui` pour chacune) et `bulle_rect_gris.png` (désactivé, sans
variante) : 7 images. Image **400 × 240**, affichée **200 × 120**, étirée **en largeur seulement**.

| Mesure (affichage) | Repos | Appuyé |
|---|---|---|
| Face | rectangle x 12 → 188, y 9 → 95, coins **24 px** | y 15 → 101 |
| Socle | même rectangle, y 19 → 105 : **10 px** visibles | **4 px** visibles |
| Contour | **5 px** : x 7 → 193, y 4 → 110 | y 10 → 110 |
| Ombre | +6 px, flou 6 px, 35 % | +3 px, flou 3 px |
| Reflet | bande x 22 → 178, y 13 → 44, coins 16 px, blanc 50 % → 6 % | 35 %, y 19 → 50 |
| Éclat | gélule 16 × 7 entre x **22 et 38**, y 17 → 24 | y 23 → 30 |
| Lumière renvoyée | trait de 3 px, y 91 → 94, x 30 → 170 | y 97 → 100 |

**Règle du 9-slice** (à respecter au rendu, sinon l'étirement déforme) : tout ce qui n'est pas
**identique d'une colonne à l'autre** (coins, bouts arrondis du reflet, éclat) doit rester dans
les 40 px de gauche ou de droite (affichage). Entre x = 40 et x = 160, chaque colonne de pixels
doit être la même. Bords de découpe, en pixels **de l'image** : gauche **80**, droite **80**,
haut **80**, bas **100**.

#### 15.1.6 La croix de fermeture : même forme, plus belle

- **Bulle** : `bulle_petit_rouge.png` (rond rouge, comme aujourd'hui, mais en relief et brillant).
- **Croix** : on garde les deux barres en USS (`.croix-barre`), en les **embellissant** :
  barres **36 × 11 px**, coins 5,5 px, blanc `#FFFFFF`, contour **3 px `#7A1A1F`** ; sous chaque
  barre, une copie « épaisseur » `#9A2228` décalée de **0 / +3 px** (deux éléments
  `.croix-barre--ombre` placés **avant** les barres blanches). Résultat : une croix d'autocollant
  en relief.
- Centrée sur la face : centre (48, 40) de l'élément de 96 px ; appuyé : (48, 45).
- Survol : la croix tourne de **90°** (inchangé) et la bulle grossit à 1,08.
- Place : `.popup-close { top: -24px; right: -32px; width: 96px; height: 96px; }` (le centre de
  la face reste là où était le centre du rond de 72 px).

#### 15.1.7 Les états

| État | Bulles rondes | Bulles rectangulaires |
|---|---|---|
| Repos | image de repos | image de repos |
| Survol | `scale 1.08`, 0,18 s `ease-out-back` ; icône `scale 1.12` + dandinement (inchangé) | `scale 1.05`, 0,18 s `ease-out-back` |
| Appui | **image `_appui`**, `scale 0.96`, **pas de `translate`** (l'image montre déjà l'enfoncement) ; l'icône descend de **6 px** (grande) ou **5 px** (petite) | image `_appui`, `scale 0.97` ; le texte descend de **6 px** |
| Relâché | rebond 1,12 (`.btn--rebond`, inchangé) | idem |
| Pop-up ouverte (colonnes) | **cercle or** posé sur la bulle : élément `.col-btn-actif`, `left 14px; top 4px; width 132px; height 132px; border-radius 66px; border-width 6px; border-color #FFD54A` + icône `scale 1.1` | — |
| Interdit | `opacity 0.45` + icône `-unity-background-image-tint-color: #B8B4C8` (inchangé) | image `bulle_rect_gris.png`, texte `#E6E2F0` contour 3 px `#45425A` |

**Plan B seulement** (pas d'image `_appui`) : appui = `scale 0.94`, `translate 0 4px`,
`-unity-background-image-tint-color: #E6E6E6`.

#### 15.1.8 Extrait USS (remplace le bloc « colonnes » de `Popups.uss` et le style des boutons d'action)

```css
/* ---------- v6 : les bulles sont des IMAGES (art bible §15.1). Le bouton n'a plus ni
   bordure ni couleur de fond : toute l'apparence est dans le PNG. ---------- */
.btn.bulle {
    border-width: 0;
    border-radius: 0;
    background-color: rgba(0, 0, 0, 0);
    -unity-background-scale-mode: stretch-to-fill;
    padding: 0;
}
.btn.bulle:active { translate: 0 0; scale: 0.96 0.96; border-bottom-width: 0; }

/* Les colonnes : grande bulle de 160 px (dont 18 px de marge transparente sur les côtés). */
.col { position: absolute; top: 232px; width: 160px; }
.col--gauche { left: 14px; }
.col--droite { right: 14px; top: 232px; }   /* même hauteur que la gauche (§15.2) */
.btn-holder { width: 160px; height: 160px; margin: 0 0 -4px 0; }   /* pas de 156 px */
.btn.col-btn { width: 160px; height: 160px; }
.col-btn-icon { position: absolute; left: 34px; top: 24px; width: 92px; height: 92px; }
.btn.col-btn:active > .col-btn-icon { translate: 0 6px; scale: 1 1; }

.btn.col-btn--boutique      { background-image: url("../Art/bulles/bulle_rond_boutique.png"); }
.btn.col-btn--boutique:active { background-image: url("../Art/bulles/bulle_rond_boutique_appui.png"); }
/* … même chose pour inventaire, index, ameliorations, classement, renaissance … */

/* Pop-up ouverte : le cercle or (élément ajouté par le code, caché par défaut). */
.col-btn-actif { display: none; position: absolute; left: 14px; top: 4px; width: 132px; height: 132px;
    border-radius: 66px; border-width: 6px; border-color: #FFD54A; }
.col-btn--active .col-btn-actif { display: flex; }

/* Petites bulles : fermer et roue dentée. */
.btn.bulle-petit { width: 96px; height: 96px; }
.btn.bulle-petit--rouge   { background-image: url("../Art/bulles/bulle_petit_rouge.png"); }
.btn.bulle-petit--rouge:active   { background-image: url("../Art/bulles/bulle_petit_rouge_appui.png"); }
.btn.bulle-petit--ardoise { background-image: url("../Art/bulles/bulle_petit_ardoise.png"); }
.btn.bulle-petit--ardoise:active { background-image: url("../Art/bulles/bulle_petit_ardoise_appui.png"); }
.bulle-petit .croix, .bulle-petit .roue-icone { position: absolute; }
.bulle-petit .croix { left: 28px; top: 20px; }               /* centre (48, 40) */
.bulle-petit .roue-icone { left: 16px; top: 8px; width: 64px; height: 64px; }
.btn.bulle-petit:active .croix, .btn.bulle-petit:active .roue-icone { translate: 0 5px; }
.croix-barre { width: 36px; height: 11px; border-radius: 5.5px; border-width: 3px; border-color: #7A1A1F; }
.croix-barre--ombre { background-color: #9A2228; border-color: #9A2228; translate: 0 3px; }

/* Bulles rectangulaires en 9-slice (Encaisser, Rejouer, Continuer). */
.btn.bulle-rect {
    height: 120px;
    min-width: 300px;
    margin: 0 12px;
    padding: 0 36px 16px 36px;      /* le texte se centre sur la FACE, pas sur le socle */
    flex-direction: row; align-items: center; justify-content: center;
    -unity-slice-left: 80; -unity-slice-right: 80; -unity-slice-top: 80; -unity-slice-bottom: 100;
    -unity-slice-scale: 0.5px;      /* l'image est rendue en ×2 */
}
.btn.bulle-rect:hover  { scale: 1.05 1.05; }
.btn.bulle-rect:active { scale: 0.97 0.97; }
.btn.bulle-rect:active > * { translate: 0 6px; }
.btn.bulle-rect--vert   { background-image: url("../Art/bulles/bulle_rect_vert.png");   --contour: #0E3D21; }
.btn.bulle-rect--vert:active   { background-image: url("../Art/bulles/bulle_rect_vert_appui.png"); }
.btn.bulle-rect--violet { background-image: url("../Art/bulles/bulle_rect_violet.png"); --contour: #21155E; }
.btn.bulle-rect--violet:active { background-image: url("../Art/bulles/bulle_rect_violet_appui.png"); }
.btn.bulle-rect--aqua   { background-image: url("../Art/bulles/bulle_rect_aqua.png");   --contour: #0B4A6E; }
.btn.bulle-rect--aqua:active   { background-image: url("../Art/bulles/bulle_rect_aqua_appui.png"); }
.btn.bulle-rect:disabled { background-image: url("../Art/bulles/bulle_rect_gris.png"); --contour: #45425A; color: #E6E2F0; }
.bulle-rect__texte { font-size: 38px; color: #FFFFFF; -unity-font-style: bold;
    -unity-text-outline-width: 4px; -unity-text-outline-color: var(--contour); margin: 0; padding: 0; }
```

Notes pour le programmeur :
- `ButtonStyler` ne doit **plus** ajouter de reflet ni d'ombre aux boutons qui ont la classe
  `.bulle` (tout est dans l'image) : sinon le défaut de la v3 revient (reflet par-dessus).
- Les éléments `.col-btn-anneau`, `.col-btn-reflet` et `.btn-shadow--rond` sont **supprimés**.
- Si cette version de Unity refuse `-unity-slice-scale: 0.5px`, mettre les bords à 40 / 40 / 40 /
  50 et demander à l'artiste un rendu en ×1 (200 × 120) : moins net en 1440p, mais correct.
- Contenu des boutons du bas : « Encaisser » + montant + pièce 40 px ; « Rejouer » ;
  « Continuer » + diamant 40 px + prix (1, 2, 4…). Largeurs minimales : Encaisser **300**,
  Rejouer **280**, Continuer **340**. Écart entre deux boutons : **24 px**.

#### 15.1.9 Fiche pour l'artiste 3D (`blender/rendu_bulles.py`)

- **Une seule fonction paramétrée** `rendre_bulle(forme, couleurs, appui, chemin)` ; `forme` =
  `rond`, `petit` ou `rect`. Les 25 images sortent d'une seule exécution.
- Caméra **orthographique de face**, *Film → Transparent*, Eevee, *View Transform* **Standard**,
  résolution du §15.1.5.
- Ombre, contour et socle : plaques plates en **émission** (couleur exacte, aucune lumière).
  Face : disque légèrement bombé (calotte de hauteur 20 % du rayon), *Principled* (rugosité 0,25,
  *Coat* 0,6), éclairé par la clé `#FFF1D6` en haut à gauche ; contrôler à la pipette que le haut
  de la face est à ± 8 % du « clair » et le bas à ± 8 % du « mi ». Reflet et éclat : plaques en
  émission blanche avec un dégradé de transparence (forme garantie). Flou de l'ombre : nœud *Blur*
  du compositeur, ou plaque à dégradé radial.
- Contrôle : poser chaque bulle sur `#6EC3FF` (ciel du palier 1) et sur `#140F33` (nuit), avec
  l'icône par-dessus : le contour et l'ombre détachent la bulle dans les deux cas.
- Dans le 9-slice, vérifier par script que les colonnes x = 80 à 320 (image) sont identiques.

### 15.2 Placement

#### 15.2.1 Les deux colonnes à la même hauteur

Aujourd'hui la gauche commence à y = 170 et la droite à y = 200, juste sous le compteur de
diamants (qui finit vers y = 204) : les bulles le touchent presque.

| | Valeur |
|---|---|
| Haut visible de la 1re bulle (les deux colonnes) | **y = 240** (élément à `top: 232px`, 8 px de marge dans l'image) |
| Bord visible gauche / droit | **32 px** du bord (élément à 14 px, 18 px de marge dans l'image) |
| Pas d'une bulle à la suivante | **156 px** (bulle visible de 134 px + **22 px** d'écart) |
| Colonne gauche (haut visible) | Boutique 240, Inventaire 396, Index 552, Améliorations 708 (fin : 842) |
| Colonne droite (haut visible) | Classement 240, Renaissance 396 |
| Écart minimal avec le compteur de diamants | **28 px**. Si le compteur finit plus bas que y = 212 (badge ×2, autre taille de police), on descend **les deux colonnes ensemble** : elles restent alignées |

#### 15.2.2 La barre du haut (carte d'étage compacte) décollée du bord

| Partie | Valeur |
|---|---|
| Bord haut | **y = 40** à l'écran (aujourd'hui ~24 et l'œil la croit collée). `.recap-bar` est dans le HUD qui a 32 px de marge : `top: 8px`. Vérifier en capture ; si elle est placée par rapport au bord de l'écran, `top: 40px` |
| Taille | hauteur **64 px**, `padding: 0 28px`, coins **24 px** |
| Contour | **5 px `#110D1F`** (au lieu de 4 px `#1F1730`) |
| Ombre | élément frère placé avant, même forme, `rgba(17, 13, 31, 0.35)`, `translate: 0 6px` |
| Fond | `rgba(31, 23, 48, 0.88)` (inchangé) |

#### 15.2.3 Les onglets de la Boutique sur toute la largeur

La rangée `.btq-top` = **[onglets, qui prennent toute la place] + [soldes, à droite]**.

```css
.btq-top { flex-direction: row; align-items: center; }
.btq-tabs { flex-direction: row; flex-grow: 1; flex-shrink: 1; }
.btq-top .btn.v5-tab {
    flex-grow: 1; flex-basis: 0; min-width: 0;   /* 4 onglets de même largeur */
    height: 64px; padding: 0 8px; margin-right: 10px;
    flex-direction: row; justify-content: center; align-items: center;
}
.btq-top .btn.v5-tab:last-child { margin-right: 0; }
.btq-soldes { flex-shrink: 0; margin-left: 18px; }
.v5-tab__texte { font-size: 26px; }
/* « Diamants · Bientôt » devient « Diamants » + une étiquette penchée sur le coin. */
.v5-tab__bientot {
    position: absolute; top: -12px; right: -8px; rotate: 8deg;
    font-size: 15px; color: #3A2200; -unity-font-style: bold;
    background-color: #FFD54A; border-width: 3px; border-color: #110D1F; border-radius: 10px;
    padding: 1px 8px;
}
```
L'icône de monnaie de chaque onglet reste (30 px, à gauche du mot).

### 15.3 Nouvelles icônes Boutique et Paramètres

**Boutique : une échoppe de fête foraine** (`art/icones/icone_boutique.png`, prompt : lot 7, image 1).
- Un petit stand en bois vu de 3/4, avec un **grand auvent festonné à rayures turquoise `#2EC4C4`
  et crème `#FFF4DC`** qui occupe toute la moitié haute : c'est lui qui dit « magasin » au premier
  coup d'œil, même à 92 px. Sur le comptoir (bois `#8C5933`) : le **petit cadeau violet à ruban
  doré** (rappel de l'ancienne icône), un mini haut-de-forme et une sucette étoile dorée ; une
  étiquette de prix **vierge** pend au bord.
- Turquoise et crème ressortent sur la bulle **rose** de la Boutique.
- On **évite** : le panier rouge (c'est l'icône du jeu de référence de l'auteur), le chariot de
  supermarché, le sac à logo, tout symbole d'argent réel.
- Variante facultative (image 2) : un sac de courses en papier rose dont dépassent des objets.

**Paramètres : une roue dentée propre** (`art/icones/icone_reglages.png`, image 3).
- **8 dents épaisses** aux coins arrondis, métal argent-lavande (`#C9CDE0` éclairé,
  `#8A90B0` ombre), **moyeu doré `#FFC83A` plein au centre**.
- **Pas de trou au centre** : le trou était la cause du mauvais détourage (le fond lavande restait
  coincé dedans). Un moyeu plein = un détourage propre d'un seul trait.
- Posée sur la bulle ardoise (§15.1.4) : argent clair sur ardoise foncée, bon contraste.

### 15.4 Refonte de la Renaissance

**Le problème** (capture v5.1) : trois cartes identiques remplies de petits textes, une phrase
grise, un bouton moyen, et la moitié basse de la fenêtre vide. Rien ne donne envie : on lit un
contrat. **L'idée** : une grande image au centre, ce qu'on gagne **au milieu et plus gros**, des
icônes au lieu des phrases, et un énorme bouton.

**Fenêtre** : **1100 × 840** (le cadre standard du §12.18, un peu plus large). Titre « Renaissance »
avec `icone_rebirth.png` qui déborde (inchangé).

```
┌──────────────────────────────────────────────────────────┐
│ [icône] Renaissance                                   (×) │
│                     ✦   rayons dorés   ✦                  │
│                  ┌───────────────┐  ╭────╮                │
│                  │  ILLUSTRATION │  │ ×2 │ ← macaron or   │
│                  │  (cheval doré │  ╰────╯                │
│                  │   qui renaît) │                         │
│                  └───────────────┘                         │
│ ┌ Tu perds ──┐  ┌══ Tu gagnes ════┐  ┌ Tu gardes ─┐       │
│ │ pièce  amé.│  │ pièce×2 diam.×2 │  │ skin diam. │       │
│ │            │  │ boîtes          │  │ records    │       │
│ └────────────┘  └═════════════════┘  └────────────┘       │
│   Pièces et améliorations reviennent à zéro. Tu gardes tout le reste !
│            ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓               │
│            ┃   Renaître !   ●  10 000      ┃               │
│            ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛               │
└──────────────────────────────────────────────────────────┘
```

**A. La scène centrale** (hauteur **300 px**, en haut du corps)
| Élément | Valeur |
|---|---|
| Lueur | disque flou `#FFE9A0` → transparent, Ø **460** |
| Rayons | `RaysElement` Ø **520**, 12 rayons `#FFD54A` à 55 %, **un tour en 24 s** |
| Illustration | `art/illustrations/renaissance_heros.png` (lot 7, image 8), **300 × 300**, centrée ; flotte de ±6 px en 2,4 s (`ease-in-out`, aller-retour) |
| Étincelles | 8 petites étoiles dessinées par le code, qui montent lentement et s'effacent |
| **Macaron ×2** | `art/ui/macaron_or.png` (image 9) **170 × 170**, collé en haut à droite de l'illustration (centre décalé de +200 / −40 px), `rotate -8deg` ; dedans « **×2** » **76 px** Bold blanc, contour 5 px `#8A4B00`, `text-shadow 0 5px 0 #8A4B00` ; dessous, une pièce et un diamant de 34 px côte à côte. Pulsation `scale 1 → 1.08 → 1` en **1,4 s**, en boucle |

**B. Les trois cartes** (hauteur **230 px**, sous la scène ; ordre **perds · gagnes · gardes**)
| | Tu perds | **Tu gagnes** (au centre) | Tu gardes |
|---|---|---|---|
| Largeur | 300 | **380**, remontée de 14 px (`translate 0 -14px`), contour **6 px** | 300 |
| Fond | `carte_rouge` (`#FFA08A → #E0404A`) | `carte_or` (`#FFE66B → #FF9F1C`) + rayonnement léger derrière les icônes | `carte_bleue` (`#62E6FF → #3B5BDB`) |
| Titre | 32 px Bold blanc, contour 4 px encre | **36 px** | 32 px |
| Icônes (64 px, 72 px au centre), légende dessous 20 px Bold blanc contour 3 px | pièce « 20 247 » ; `icone_ameliorations` « Améliorations » | pièce avec « **×2** » ; diamant avec « **×2** » ; boîte Animaux « Nouvelles boîtes » | skin équipé « 16 objets » ; diamant « 12 » ; `icone_classement` « Records » |
| Pastille sur chaque icône (Ø 34, en bas à droite, contour 3 px encre) | rouge `#E0404A`, un **−** blanc (barre 16 × 5 px) | « ×2 » écrit **sur** l'icône : 34 px Bold `#FFD54A`, contour 4 px `#3A2200` | verte `#6BE34A`, une **coche** blanche (deux barres USS) |

Les nombres sont lus dans la sauvegarde (vrais), comme aujourd'hui.

**C. La phrase** : style « message important » du §12.18.6 (26 px Bold blanc contour 4 px, la
fin en vert `#6BE34A`) : « Pièces et améliorations reviennent à zéro. **Tu gardes tout le reste !** »

**D. Le gros bouton** : `bulle_rect_vert`, **560 × 120**, « Renaître ! » **46 px** + pièce 44 px +
« 10 000 » 46 px, contour 4 px `#0E3D21`. L'**éclat** qui passe (comme Jouer) toutes les **2,5 s**.
- Pas assez de pièces : bouton gris (`bulle_rect_gris`) et, juste au-dessus, une **jauge**
  560 × 26 : piste `rgba(17, 13, 31, 0.15)`, contour 3 px encre, coins 13 px, remplissage
  dégradé `#FFE66B → #FF9F1C`, texte centré « 6 420 / 10 000 » 20 px Bold `#2A2440`.
- Dessous, la note de rentabilité en petit (18 px `#6E6A80`), une seule ligne.

**E. Après la renaissance** (elle n'arrive qu'une fois) : la scène reste (illustration, rayons),
le macaron porte une étiquette « Fait ! » ; les trois cartes sont remplacées par **une** carte or
de 760 px : « Renaissance faite ! ×2 pièces et ×2 diamants, pour toujours » ; pas de bouton.

**F. Le moment de renaître** (après la confirmation) : flash blanc 0,15 s, l'illustration grossit
à 1,3 et les rayons tournent 4 fois plus vite pendant 0,8 s, pluie de pièces dorées dessinées par
le code, puis la fenêtre passe à l'état E.

### 15.5 La Route des trophées (clic sur le badge d'étage)

**L'idée** : un chemin qui **monte dans le ciel**, comme la tour : en bas l'étage 1 sous un ciel de
jour, en haut la nuit étoilée. On voit où on est, son record, et ce qui change aux étages suivants.

**Fenêtre** : 1000 × 840, titre « Route des trophées » avec `art/icones/icone_route.png`
(lot 7, image 6) qui déborde.

| Zone | Valeur |
|---|---|
| Bandeau résumé (haut, 64 px) | 3 pilules côte à côte (hauteur 56, coins 28, contour 4 px encre, texte 24 px Bold blanc contour 3 px) : « Étage **12** » bleu `#3B7DD8` ; « Record **23** » or `#FFC83A` texte `#3A2200` avec `drapeau_record` 40 px ; « Prochain palier : étage **15** » violet `#6C4FE0` |
| Fenêtre de la route | tout le reste du corps, coins **22 px**, contour **5 px** encre, `overflow: hidden`, défile **verticalement** (barre de défilement du §12.18.1 sur toute la hauteur) |
| Fond | un long dégradé vertical qui traverse les 4 paliers du décor : jour `#6EC3FF → #3B7DD8`, coucher `#FFB08A → #E0705A`, crépuscule `#8E7BFF → #6C4FE0`, nuit `#2E2366 → #140F33` + petites étoiles ; fondu de **120 px** entre deux paliers, chaque palier couvrant ses étages (§2.9) |
| Le chemin | tracé par le code (`Painter2D`) : une **sinusoïde douce** autour du centre, amplitude **110 px**, une vague tous les **12 étages** ; trait **34 px** encre, puis trait **26 px** par-dessus : **dégradé or** `#FFE66B → #FFB020` de l'étage 1 à l'étage actuel, **blanc à 35 %** au-delà ; bouts arrondis |
| Hauteur d'un étage | **70 px** ; étage 1 à 120 px du bas |
| Étage ordinaire | point Ø 22 sur le chemin (blanc, or si passé), contour 4 px encre ; numéro **20 px** Bold blanc contour 3 px, du côté extérieur de la courbe |
| **Palier** (un étage où quelque chose change : décor, coffre en plus, nouvelle rareté de coffre, chance de diamants… liste et textes : game designer) | bulle `bulle_petit_or` (atteint) ou `bulle_petit_ardoise` (pas encore) de 96 px, avec l'icône du changement (56 px : coffre de rareté, diamant, ciel…) ; à côté, une **carte** 330 × 96, coins 18, contour 4 px encre, fond dégradé des cartes v5.1 (or : gains, violette : coffres, bleue : diamants, verte : décor), « Étage 15 » 24 px + « 4 coffres ! » 20 px, blancs contourés ; les cartes alternent à gauche et à droite, du côté opposé à la courbe |
| Palier pas encore atteint | même carte à **70 %** d'opacité, icône désaturée (`#B8B4C8`) ; le texte reste **vrai et lisible** (on montre ce qui attend le joueur, sans rien promettre de faux) |
| **Position actuelle** | `bulle_petit_or` affichée à **120 px** avec l'icône du **skin équipé** (80 px) ; halo `#FFD54A` qui pulse (Ø 100 → 150, 40 % → 0 %, 1,6 s) ; au-dessus, pilule « Tu es ici » 22 px Bold `#3A2200` sur `#FFD54A`, contour 3 px encre, coins 14, avec une petite pointe vers le bas |
| **Record** | `drapeau_record.png` 64 px planté à côté du chemin + pilule « Record : 23 » (même style que « Tu es ici ») ; une **ligne en tirets** traverse la route à cet étage (tirets 16 / 10 px, 4 px, `#FFD54A`). Si l'étage actuel est le record, le drapeau se met à côté du cheval |
| Plus haut | on affiche jusqu'au plus grand de (record, étage actuel) + **15 étages** ; au-delà, un nuage blanc dessiné et « Et plus haut… ? » 26 px Bold blanc contour 4 px |
| Ouverture | la route s'ouvre **centrée sur l'étage actuel** (il est à 40 % du bas), après un court glissé de 200 px vers le haut en **0,5 s `ease-out`** ; si on s'éloigne en défilant, un bouton rond `bulle_petit_or` avec l'icône du skin apparaît en bas à droite (« Ma position »), qui y ramène en 0,3 s |

### 15.6 La fiche agrandie : flèches et « glisse pour tourner »

**Flèches gauche et droite** (la droite manquait)
| Partie | Valeur |
|---|---|
| Bulle | `bulle_petit_ardoise` affichée à **120 × 120** |
| Place | à cheval sur les bords gauche et droit de la fiche (`left: -60px` / `right: -60px`), centrées en hauteur sur la vue 3D (`top: 284px`, comme aujourd'hui) |
| Chevron | deux barres USS **34 × 12 px**, coins 6 px, blanches, contour 3 px `#1E1A33`, tournées de ±40° autour de leur bout pointu (`transform-origin`) ; copie « épaisseur » `#2E2A4E` décalée de 0 / +3 px dessous ; centré sur la face (centre à 50 % / 42 % de l'élément) |
| Survol | bulle 1,08 ; le chevron avance de **4 px** dans sa direction |
| Bout de la liste | la flèche **disparaît** (`opacity 0`, non cliquable, 0,15 s) plutôt que d'être grisée |
| Clavier | ← et → font la même chose (à confirmer par le game designer) |

**Indice « glisse pour tourner »**
| Partie | Valeur |
|---|---|
| Place | sous le modèle, centré, à 16 px au-dessus du bas de la vue 3D |
| Pilule | hauteur 48, `padding 0 18px 0 8px`, fond `rgba(17, 13, 31, 0.75)`, coins 24 |
| Contenu | `icone_rotation.png` (lot 7, image 5) **40 px** + « Glisse pour tourner » **22 px** Bold blanc |
| Animation | l'icône se balance de −10° à +10° en 1,2 s (aller-retour) ; la pilule glisse de ±8 px dans le même rythme |
| Disparition | fondu de **0,3 s** dès que le joueur a fait tourner le modèle. Quand la réafficher (chaque ouverture, ou jusqu'à la première fois) : game designer |
| Socle tournant | un disque plat lavande `#D9D2F0` à 50 % sous le modèle (ellipse 220 × 50), qui tourne avec lui : on comprend que c'est un plateau |

### 15.7 Paramètres : le choix Graphismes

La fenêtre Réglages (taille S, **640 × 600** au lieu de la hauteur actuelle, trop grande) a
3 rangées : Musique, Effets, puis **Graphismes**.

| Partie | Valeur |
|---|---|
| Rangée | `icone_graphismes.png` (lot 7, image 4) 64 px + « Graphismes » 30 px Bold `#2A2440` |
| Sélecteur | une barre **560 × 84** sous le titre : fond creusé `#E9E3D6`, contour 4 px encre, coins 22, `padding 8px` ; **3 segments de même largeur** (`flex-grow 1`), 6 px d'écart, hauteur 68, coins 16 |
| Segment au repos | fond transparent, texte **26 px** Bold `#3A3358` ; à gauche du mot, **3 petites barres montantes** (8 px de large, 14 / 22 / 30 px de haut, coins 3, 4 px d'écart) : autant de barres pleines `#3A3358` que le niveau (Faible 1, Moyen 2, Élevé 3), les autres `rgba(17, 13, 31, 0.18)` |
| Segment choisi | famille **or** : dégradé `bouton_or.png`, contour 4 px `#6B4300`, bas 6 px `#D99A1A`, texte et barres `#3A2200` ; `scale 1.04`, 0,15 s `ease-out-back` |
| Survol d'un segment non choisi | fond `rgba(17, 13, 31, 0.06)` |
| Phrase dessous | 20 px `#6E6A80`, centrée, qui change selon le choix : Faible « Pour les appareils peu puissants : plus fluide, moins d'effets. » ; Moyen « Résolution classique : le bon équilibre. » ; Élevé « Tous les détails et les effets au maximum. » (textes définitifs : game designer) |
| Effet du clic | immédiat, petit toast « Graphismes : Élevé » ; ce que change chaque niveau (résolution, ombres, particules) : spec v6 |
| Pied de fenêtre | « version 6 » (corrige le point 48 du QA) |

### 15.8 Images de la v6 (récapitulatif)

| Fichier | Fait par | Où |
|---|---|---|
| 25 bulles `art/bulles/bulle_*.png` (§15.1.5 : 12 rondes, 6 petites, 7 rectangulaires) | **artiste 3D**, `blender/rendu_bulles.py` | colonnes, croix, roue, flèches, Route, boutons du bas, Renaissance |
| `art/icones/icone_boutique.png` (+ variante `_b` facultative) | Gemini, lot 7 | bulle Boutique, titre de la pop-up |
| `art/icones/icone_reglages.png` | Gemini, lot 7 | roue dentée |
| `art/icones/icone_graphismes.png` | Gemini, lot 7 | rangée Graphismes |
| `art/icones/icone_rotation.png` | Gemini, lot 7 | indice de la fiche agrandie |
| `art/icones/icone_route.png`, `art/icones/drapeau_record.png` | Gemini, lot 7 | Route des trophées |
| `art/illustrations/renaissance_heros.png` | Gemini, lot 7 | scène de la Renaissance |
| `art/ui/macaron_or.png` | Gemini, lot 7 | macaron ×2 |

Tout le reste (rayons, lueurs, jauges, chemin de la Route, chevrons, croix, coches, étoiles,
segments) est **dessiné par le code ou en USS**.

### 15.9 À éviter (v6)

- Un reflet ou une ombre ajoutés **par le code** sur une bulle en image (double reflet).
- Étirer une bulle ronde (elle doit garder **exactement** son format carré) ou étirer une bulle
  rectangulaire en hauteur.
- Une icône de la même couleur que sa bulle (or sur or, orange sur orange).
- Du rouge ailleurs que sur la croix ; de l'or ailleurs que sur ce qui est atteint ou gagné.
- Un palier « pas encore atteint » qui annonce quelque chose de faux ou d'arrangé.
- Le panier rouge ou tout autre symbole tiré du jeu de référence.

### 15.10 À vérifier en capture (1920 × 1080 et 1280 × 720)

- Les deux colonnes commencent à la même hauteur et ne touchent pas le compteur de diamants
  (≥ 28 px), même avec le badge ×2.
- Chaque icône se reconnaît sur sa bulle, sur le ciel de jour **et** sur la nuit.
- La barre du haut a de l'air au-dessus (≈ 40 px) et ne touche pas le titre d'une pop-up.
- Les 4 onglets de la Boutique vont jusqu'aux soldes, sans texte coupé.
- Rejouer, Continuer et Encaisser : coins nets et reflet non déformé quelle que soit la largeur.
- Appui sur une bulle : l'icône (ou le texte) descend **avec** la face, sans saut.
