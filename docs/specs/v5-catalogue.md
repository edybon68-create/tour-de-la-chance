# Catalogue v5 : Boîtes Pirates et Boîtes Animaux (56 nouveaux objets)

> Annexe de `docs/specs/v5.md` (règles, taux, économie, écrans). Ce fichier décrit **chaque
> objet** : identifiant, nom affiché (court), rareté, emplacement, ce que l'artiste et le DA
> doivent fabriquer, et comment le programmeur le rend.
>
> **Les identifiants ne changent jamais une fois publiés** (ils sont écrits dans la sauvegarde).
> Les noms affichés peuvent encore être ajustés par l'auteur avant la sortie.

---

## 0. Règles communes à tous les nouveaux objets

### 0.1 Style « jeu moderne » (intention pour le DA et l'artiste 3D)
Les nouveaux objets (et les 16 accessoires modernisés, `v5.md` §4) suivent le même esprit :
- **Silhouettes franches et un peu exagérées** : formes plus grosses et plus rondes que nature
  (proportions « jouet de collection »), lisibles à 1280 × 720 depuis la caméra du jeu.
- **Matières en couches** : un plastique brillant (smoothness 0,6 à 0,8) + un détail d'une autre
  matière (métal doré, tissu mat, gemme) ; biseaux marqués (art bible §3) qui accrochent la lumière.
- **Couleurs saturées mais harmonieuses** : 3 couleurs principales au plus par objet (+ émission
  pour Légendaire et Secret). Pas de noir pur (`#1F1730` à la place), pas de blanc pur (`#F4EEE6`).
- **Mouvement secondaire** : ce qui pend ou dépasse (plume, pan de tissu, queue, chaîne) bouge un
  peu avec un « ressort » (retard + rebond) quand le cheval saute. Un objet Super rare ou plus a
  au moins une petite animation propre.
- **Gentil et pour tous les âges** : pas de crâne, d'arme qui blesse, de sang, d'alcool, de
  tabac. Les pirates sont des pirates de dessin animé (ancres, longues-vues, perroquets).
- **Plafond d'émission 1,5** (art bible §4.1) ; émission seulement pour Légendaire et Secret.

### 0.2 Budgets techniques
| Type | Triangles | Matériaux | Particules simultanées | Remarque |
|---|---|---|---|---|
| Accessoire Super rare et moins | ≤ 1 200 | ≤ 3 | 0 | aucun collider |
| Accessoire Légendaire | ≤ 2 000 | ≤ 4 | ≤ 20 | |
| Accessoire Secret | ≤ 3 000 | ≤ 4 | ≤ 60 | |
| « Extras » d'un skin (pièces 3D ajoutées au corps, §0.4) | ≤ 2 000 | ≤ 3 | ≤ 60 | seulement Légendaire et Secret |

Tout doit rester dans une **enveloppe** de 2,6 m de haut × 2,4 m de large autour du cheval
(pour ne toucher ni les coffres ni le bord de l'écran) et **ne jamais cacher les deux yeux**.

### 0.3 Comment un skin est rendu (pour le programmeur)
Lettres utilisées dans la colonne « Rendu » des tableaux :

| Lettre | Sens | Comment |
|---|---|---|
| **U** | Uni | une couleur, metallic / smoothness (comme les skins n°1) |
| **T** | Texture répétable | une image **sans couture** 512 × 512 fournie par le DA (`Assets/Art/Skins/<id>.png`), posée en `_BaseMap` avec une répétition (*tiling*) ; **exception à l'art bible à faire valider par le DA** (motifs plats, 2-3 couleurs, aucun rendu photo) |
| **C** | Texture calculée | fabriquée par le code au démarrage (comme Sucre d'orge, Pois, Galaxie) |
| **A** | Animée | la texture, la couleur ou l'émission bouge (calcul régulier, comme la Galaxie v4) |
| **E** | Émission | brille dans le noir (≤ 1,5) |
| **Tr** | Transparent | comme le Cristal (opacité donnée) |
| **X** | Extras 3D | des pièces 3D ajoutées au cheval tant que le skin est équipé (§0.4) |
| **P** | Particules | petits éléments qui s'échappent (réserve réutilisée, comme les paillettes de l'Arc-en-ciel) |

### 0.4 Les « extras » d'un skin (nouveauté v5)
Certains skins Légendaires et Secrets ajoutent des **pièces 3D** au cheval (les tentacules du
Kraken, la crinière de flammes du Lion…). Règles :
- nouveau champ de la fiche d'objet : `skinExtrasPrefab` (un prefab posé comme enfant de
  `Joueur`, calque « Cheval », sans collider) ;
- ils ne comptent **pas** comme un accessoire (ils font partie du skin) : on peut porter le
  Kraken **et** 2 accessoires ;
- ils sont retirés quand on change de skin (même règle que l'émission ou la transparence) ;
- ils réagissent aux moments du jeu par des **événements d'affichage** déjà existants (saut de
  joie après un trésor, tête basse après un piège) : jamais avant la révélation (règle d'équité).

### 0.5 Ce que tout objet doit fournir
1. Une **icône** 1:1 (Gemini, art bible §9) : l'objet seul pour un accessoire, **le cheval
   portant le skin** pour un skin, même angle et même lumière que les icônes existantes.
2. Pour un accessoire : un **modèle Blender** (script dans `blender/`, export `.fbx` vers
   `Assets/Models/`), avec son point d'accroche et ses réglages `attachScale` / `attachOffset`.
3. Pour un skin T : sa texture répétable ; pour un skin X : son prefab d'extras.
4. Une **phrase de fiche** (affichée dans la fiche agrandie de l'Index, `v5.md` §8.4), donnée
   ci-dessous dans la colonne « Phrase ».

---

## 1. Boîte de skins Pirates (`boite_pirates_skins`)

- Nom affiché : **« Pirates : skins »** · Prix : **5 diamants** · 12 objets.
- Taux propres à la boîte (`v5.md` §12.2) : Super rare **60 %**, Légendaire **30 %**, Secret
  **10 %** → chance par objet **10 %**, **7,5 %**, **5 %**.

| N° | Identifiant | Nom | Rareté | Rendu | Description visuelle (artiste / DA) | Phrase de fiche |
|---|---|---|---|---|---|---|
| 01 | `pirate_mariniere` | Marinière | Super rare | C | Rayures horizontales **bleu marine `#1F3A6E` / blanc cassé `#F4EEE6`**, 10 bandes sur la hauteur (plus fines que le Sucre d'orge), smoothness 0,45 ; le socle reste rayé aussi | « Prêt à larguer les amarres. » |
| 02 | `pirate_bois_flotte` | Bois flotté | Super rare | T | Bois **délavé gris-beige** `#B9AE9C` avec veines plus sombres `#8C8170` qui suivent la hauteur du cheval, quelques **petits coquillages** roses `#F2B8A8` dessinés sur la texture, mat (0,25) | « Rejeté par la mer, sculpté par les vagues. » |
| 03 | `pirate_corail` | Corail | Super rare | T | Rose corail `#FF7A6B` avec de petites **alvéoles** plus claires `#FFC2B0` (motif de corail), smoothness 0,5, très légère irrégularité | « Il a poussé au fond du lagon. » |
| 04 | `pirate_carte` | Carte au trésor | Super rare | T | **Parchemin** jaune ancien `#E9D3A0` aux bords brunis, avec un **chemin en pointillés** rouge `#C0392B`, une petite rose des vents et des **croix « X »** ; mat (0,2) | « X marque l'endroit… c'est toi ! » |
| 05 | `pirate_vert_de_gris` | Vert-de-gris | Super rare | T | Cuivre **patiné** : base cuivre `#B0703A` (metallic 0,8) avec des plaques vert-de-gris turquoise `#5FB8A0` (metallic 0,2) ; on voit le cuivre briller entre les taches | « Une vieille statue de proue. » |
| 06 | `pirate_ecailles` | Écailles | Super rare | C | **Écailles** arrondies en quinconce, dégradé turquoise `#2BB8C4` → bleu `#3B5BD9` du socle vers les oreilles, liseré clair sur chaque écaille, smoothness 0,75, metallic 0,3 (reflets de poisson) | « Il nage plus vite que les dauphins. » |
| 07 | `pirate_or_maudit` | Or maudit | **Légendaire** | U + E + P | Or `#FFC83A` (metallic 1, smooth. 0,8) parcouru de **fines veines vert émeraude** `#3CFF9A` lumineuses (texture calculée en émission 1,0) ; une **fumée verte** légère s'échappe du socle (réserve de 12 volutes, montent de 0,5 m en 1,5 s en s'effaçant) | « Ne le dépense surtout pas… » |
| 08 | `pirate_fantome` | Fantôme | **Légendaire** | Tr + E + A | **Translucide** vert d'eau `#9AF5E0`, opacité 0,5, émission 0,6 ; une **bande de lumière** horizontale monte lentement le long du corps (1 passage toutes les 2,5 s) ; le cheval entier flotte de ±3 cm (période 2 s) | « On le voit… et puis on ne le voit plus. » |
| 09 | `pirate_tempete` | Tempête | **Légendaire** | C + A + E | Gris orage `#4A5470` marbré de nuages plus clairs `#7A86A8` qui glissent lentement ; toutes les **2 à 4 s** (au hasard, générateur visuel), un **éclair** blanc-bleu `#CFE0FF` parcourt le corps (zigzag calculé, émission 1,4 pendant 0,15 s) avec un petit son `pirate_eclair` (volume 0,25) | « Il apporte l'orage avec lui. » |
| 10 | `pirate_lagon` | Lagon | **Légendaire** | C + A + E + P | Eau turquoise `#2BC4C4` → bleu profond `#1F5FA8` (dégradé vertical) avec des **caustiques** (reflets de lumière dansants, réseau clair `#E0FFFF` en émission 0,5) qui bougent ; des **bulles** montent du socle (réserve de 10, Ø 3 à 6 cm, une toutes les 0,3 s) | « Un morceau de mer des Caraïbes. » |
| 11 | `pirate_kraken` | Kraken | **SECRET** | C + A + E + X + P | Voir fiche §1.1 | « Le monstre des abysses… en version câline. » |
| 12 | `pirate_abysses` | Abysses | **SECRET** | C + A + E + X + P | Voir fiche §1.2 | « Là où la lumière n'arrive jamais. » |

### 1.1 `pirate_kraken` — Kraken (Secret) : la fiche complète
**L'idée** : le cheval **devient** un petit kraken. C'est l'objet « de fou » de la boîte.

**Le corps (texture calculée et animée)**
- Peau **violet pourpre** `#5B2D82` → magenta `#B0306A` sur le haut de la tête, très brillante
  (smoothness 0,85, aspect « mouillé »).
- **Ventouses** : cercles clairs `#F2B8D8` avec un anneau plus sombre, 3 rangées qui descendent le
  long de l'encolure (texture).
- **Taches bioluminescentes** cyan `#4FFFE8` (une vingtaine, Ø 4 à 8 cm) qui **s'allument tour à
  tour** comme une vague qui remonte le corps (émission 0 → 1,3 → 0, une vague toutes les 3 s).

**Les extras 3D : 4 tentacules**
- 4 tentacules épais (base Ø 18 cm, pointe Ø 3 cm, 1,1 m de long), qui sortent **du socle** (deux
  à l'avant, un peu écartés, deux à l'arrière sur les côtés pour être vus de face). Même matériau
  que le corps, ventouses sur la face intérieure.
- Chaque tentacule a **8 segments** (os d'animation ou chaîne de transforms) et ondule en
  permanence : vague qui part de la base vers la pointe, amplitude 15°, période 2,2 s, décalage
  de phase différent pour chaque tentacule (ils ne bougent jamais en même temps).
- **Saut de joie** (après un trésor) : les 4 tentacules se lèvent en même temps en « V » (0,3 s),
  puis l'un d'eux **attrape une pièce** qui jaillit du coffre (pur affichage) et la ramène vers le
  cheval.
- **Tête basse** (après un piège) : les tentacules retombent mollement au sol (0,4 s) et se
  recroquevillent, puis reprennent leur ondulation.
- Tous les ~6 s, **3 bulles** sortent d'un tentacule au hasard.

**Particules** : gouttes d'eau qui tombent des pointes (une toutes les 0,5 s, réserve de 12) ;
petites bulles.

**Révélation (écran d'ouverture)** : après l'impact de la carte de la cinématique Secret, la
**touche thématique Pirates** (`v5.md` §6.6) : une vague d'eau traverse l'écran de bas en haut et
**deux tentacules dessinés** (UI) jaillissent des coins bas de l'écran, s'enroulent puis
redescendent (1,2 s). Son : `pirate_secret_kraken` (grondement grave + « blop » rigolo).

**Lisibilité** : les yeux du cheval restent visibles (taches et ventouses jamais sur les yeux) ;
l'ensemble tient dans l'enveloppe (§0.2) : tentacules à 1,1 m du centre au maximum.

### 1.2 `pirate_abysses` — Abysses (Secret) : la fiche complète
**L'idée** : le cheval vient du fond de l'océan, la nuit éternelle, éclairé par sa propre lanterne.

**Le corps** : noir bleuté profond `#0A1430` (smoothness 0,9) parcouru de **lignes
bioluminescentes** bleu électrique `#3D9BFF` et magenta `#E04BD8` (motif de méduse, texture
calculée) qui **pulsent** doucement (émission 0,4 ↔ 1,2, période 2 s). Sur tout le corps, un
**plancton lumineux** : 40 points minuscules `#BFFFF5` qui dérivent lentement sur la texture
(même technique que la nébuleuse de la Galaxie, `v4.md` §7).

**Les extras 3D : la lanterne de baudroie**
- Une **tige** fine et courbée (Ø 2 cm, 0,6 m) qui part du front, entre les oreilles, et se
  penche vers l'avant ; au bout, une **lanterne** sphérique Ø 12 cm, jaune pâle `#FFF1A8`,
  émission **1,5** (avec le Bloom, elle éclaire « vraiment »).
- La tige se balance avec un ressort (retard sur les mouvements de tête, comme l'Auréole v3).
- **Une vraie petite lumière** (*Point Light* URP, portée 1,5 m, intensité 1,2, couleur
  `#FFF1A8`, **sans ombre**) suit la lanterne : elle éclaire le museau et les coffres proches.
  Seule exception au « pas de lumière en plus » : à valider par le DA (coût faible, une seule).
- Saut de joie : la lanterne clignote 3 fois (0,1 s) ; tête basse : elle baisse à 0,3 d'intensité
  pendant 1 s.

**Particules** : des **bulles** lumineuses montent (réserve de 15) et 3 petits **poissons
lumineux** (losanges plats cyan émissifs) tournent autour du cheval sur des cercles de rayon 0,9
à 1,1 m, à des hauteurs et vitesses différentes.

**Révélation** : touche thématique Pirates (vague), mais **l'écran s'assombrit** à 80 % juste
après l'impact et seule la lanterne de la carte éclaire, puis retour (1,2 s). Son
`pirate_secret_abysses` (chant grave et doux, écho).

---

## 2. Boîte d'accessoires Pirates (`boite_pirates_accessoires`)

- Nom affiché : **« Pirates : accessoires »** · Prix : **5 diamants** · 12 objets.
- Mêmes taux que la boîte de skins Pirates (60 / 30 / 10 %).
- Emplacements : Tête 4, Visage 2, Cou 2, Dos 4.

| N° | Identifiant | Nom | Rareté | Empl. | Description visuelle (artiste / DA) | Animation / effets | Phrase |
|---|---|---|---|---|---|---|---|
| 01 | `tricorne` | Tricorne | Super rare | Tête | Chapeau à 3 cornes en feutre **bleu nuit `#1F2A50`**, galon doré `#E8B830` sur tout le bord (biseau marqué), une **plume rouge** `#D64545` piquée sur le côté gauche, généreuse | plume à ressort | « Le chapeau des vrais capitaines… en apprentissage. » |
| 02 | `bandana_pirate` | Bandana | Super rare | Tête | Bandana **rouge vif `#D64545` à gros pois blancs**, noué derrière la tête, **deux pans** qui pendent sur la nuque ; les oreilles passent par-dessus | pans qui flottent (±10°, 1,4 s) | « Noué serré, même dans la tempête. » |
| 03 | `cache_oeil` | Cache-œil | Super rare | Visage | Cache-œil **en cuir brun `#5A3A28`** sur l'œil gauche du cheval (côté écran droit), cordon fin qui fait le tour de la tête ; une **petite étoile dorée** cousue dessus. **L'autre œil reste visible** (on garde toujours au moins un œil) | — | « Il voit très bien, c'est pour le style. » |
| 04 | `foulard_marin` | Foulard | Super rare | Cou | Foulard **bleu marine `#1F3A6E` à liseré blanc**, noué devant la gorge avec un gros nœud et **une pointe** qui tombe sur le poitrail. Doit **épouser l'encolure** (comme l'Écharpe corrigée, `v5.md` §4.3) | pointe à ressort | « Pour saluer les mouettes. » |
| 05 | `boussole` | Boussole | Super rare | Cou | Gros **médaillon boussole** doré `#E8B830` (Ø 0,22) sur une chaîne fine, cadran crème, **aiguille rouge** | l'aiguille tourne en cherchant le nord (oscille, puis repart), chaîne à ressort | « Elle indique toujours… le trésor suivant. » |
| 06 | `longue_vue` | Longue-vue | Super rare | Dos | **Longue-vue** en laiton `#C99A1E` et cuir brun, 3 tubes emboîtés, portée **en bandoulière** en travers du dos : elle dépasse nettement des deux côtés vus de face | se balance un peu au saut | « Terre en vue ! » |
| 07 | `chapeau_capitaine` | Capitaine | **Légendaire** | Tête | Grand **bicorne** noir-violet `#2A1F3D`, bordure dorée épaisse, **ancre dorée** en relief devant, grosse **plume blanche** `#F4EEE6` en panache ; petites gemmes `#9FE3FF` | plume à ressort ; l'ancre **luit** (émission 0,8) et une étincelle la traverse toutes les 2 s | « Seul maître à bord. » |
| 08 | `loup_corsaire` | Loup | **Légendaire** | Visage | Loup (masque des yeux) en **velours violet `#4B2E83`** bordé d'or, **deux trous** autour des yeux du cheval (les yeux restent visibles), petites pierres `#D62839` aux coins, rubans qui flottent derrière. Même technique de modélisation que le Masque corrigé (`v5.md` §4.3) | rubans à ressort ; reflets dorés qui défilent (émission 0,6) | « Corsaire masqué, cœur en or. » |
| 09 | `perroquet` | Perroquet | **Légendaire** | Dos | **Perroquet ara** rouge `#D64545`, ailes bleues `#3B7DD8` et jaunes `#FFD54A`, perché sur le garrot (côté droit de l'écran, bien visible), gros bec crème, yeux brillants | tourne la tête vers le coffre survolé ; cligne des yeux ; **bat des ailes** au saut de joie (son `perroquet_cri`, volume 0,3, « rouah ! » mignon) ; se tasse après un piège | « Il répète tout ce que tu dis. » |
| 10 | `ancre_doree` | Ancre | **Légendaire** | Dos | Grande **ancre dorée** (0,9 m) portée en bandoulière par une **chaîne** épaisse, l'ancre dépasse en bas à gauche, la chaîne passe sur l'encolure | chaîne qui se balance (maillons qui suivent avec retard) ; reflet qui glisse sur l'or (émission 0,5) | « Lourde ? Pas pour un champion. » |
| 11 | `kraken_bebe` | Petit Kraken | **SECRET** | Tête | Voir fiche §2.1 | | « Il s'est installé et ne veut plus partir. » |
| 12 | `navire_fantome` | Navire fantôme | **SECRET** | Dos | Voir fiche §2.2 | | « Il navigue sur les nuages. » |

### 2.1 `kraken_bebe` — Petit Kraken (Secret, Tête)
- Un **bébé kraken** violet `#7A3FC0` (tête ronde Ø 0,35, gros yeux brillants avec reflet, joues
  roses) **assis sur le crâne** du cheval, entre les oreilles.
- **6 petits tentacules** : deux s'enroulent autour des oreilles du cheval, deux pendent devant le
  front (sans cacher les yeux), deux tiennent une **pièce d'or** au-dessus de sa tête comme un
  trophée.
- Animations : les tentacules libres ondulent (période 1,8 s, décalés) ; il **cligne** des yeux
  toutes les 3 à 5 s ; il gonfle et dégonfle doucement (respiration, échelle 1 ↔ 1,04, 2 s).
- Saut de joie : il lance la pièce en l'air et la rattrape (0,6 s), un petit **nuage d'encre en
  forme de cœur** (particules violettes) apparaît. Tête basse : il cache ses yeux avec deux
  tentacules (1 s).
- Émission : ses taches `#4FFFE8` sur le front (0,9), la pièce (0,6).
- Son au saut : `kraken_bebe` (« blop-blop » aigu, volume 0,3).

### 2.2 `navire_fantome` — Navire fantôme (Secret, Dos)
- Un **galion miniature** (0,55 m de long) **translucide vert d'eau** `#9AF5E0` (opacité 0,6,
  émission 0,7), voiles gonflées un peu déchirées (bords effilochés, pas de crâne), lanterne
  jaune à la poupe (émission 1,4).
- Il **navigue en orbite** autour du cheval sur un **anneau de brume** (cercle de rayon 1,0 m,
  centré à la hauteur de l'encolure, incliné de 12°), un tour en 7 s, en tanguant (±8°, 1,5 s).
  Il passe devant puis derrière le cheval (comme la Planète). Il remplace l'emplacement Dos.
- La brume : 20 petits nuages translucides qui suivent le navire en traînée et s'effacent.
- Saut de joie : il **tire une salve** de confettis dorés par ses 3 sabords (« pouf » + 12
  confettis), son `navire_canon` (petit « pouf » rigolo, volume 0,3).
- Tête basse : il tangue fort et une vague (particules) passe par-dessus le pont.

---

## 3. Boîte de skins Animaux (`boite_animaux_skins`)

- Nom affiché : **« Animaux : skins »** · Prix : **500 pièces** · 16 objets.
- **Disponible seulement après la première renaissance** (`v5.md` §13).
- Taux habituels (Commun 50 %, Peu commun 25 %, Rare 14 %, Super rare 7 %, Légendaire 3 %,
  Secret 1 %) et répartition 5 / 4 / 3 / 2 / 1 / 1, comme les boîtes n°1.

| N° | Identifiant | Nom | Rareté | Rendu | Description visuelle (artiste / DA) | Phrase |
|---|---|---|---|---|---|---|
| 01 | `animal_dalmatien` | Dalmatien | Commun | T | Blanc cassé `#F4EEE6`, **taches noires** `#1F1730` rondes de tailles variées (4 à 14 cm), mat (0,3) ; une oreille entièrement noire si possible (UV) | « 101 % mignon. » |
| 02 | `animal_zebre` | Zèbre | Commun | T | Rayures **noires et blanches** ondulées qui suivent la forme (plus serrées sur la tête), mat (0,3) | « Pyjama ou zèbre ? Les deux. » |
| 03 | `animal_abeille` | Abeille | Commun | C | Rayures horizontales **jaune `#FFC83A` / brun très foncé `#2A1F1A`**, 6 bandes, smoothness 0,4 (aspect velours doux) | « Bzzz, au travail ! » |
| 04 | `animal_vache` | Vache | Commun | T | Blanc cassé avec **grandes taches** brun `#6B4A35` aux bords irréguliers, mat (0,3) ; socle rose pâle si possible (« museau ») | « Meuh-gnifique. » |
| 05 | `animal_coccinelle` | Coccinelle | Commun | C | Rouge vif `#D62839` brillant (0,7) avec **7 gros points noirs** `#1F1730` (pas un motif répété : 7 points placés, dont un sur le front) | « Elle porte bonheur ! » |
| 06 | `animal_tigre` | Tigre | Peu commun | T | Orange `#F28C38` avec **rayures noires** en pointe, ventre et museau blanc cassé, mat (0,35) | « Grrr… mais gentil. » |
| 07 | `animal_girafe` | Girafe | Peu commun | T | Jaune sable `#E8B860` avec **taches brunes** `#8C5933` en réseau (polygones séparés par des lignes claires), mat | « Il voit les coffres de très haut. » |
| 08 | `animal_leopard` | Léopard | Peu commun | T | Beige doré `#E0B070` avec **rosettes** (anneaux brun foncé à centre ocre), smoothness 0,4 | « Rapide comme l'éclair. » |
| 09 | `animal_grenouille` | Grenouille | Peu commun | C | Vert vif `#4CC26A` **très brillant** (0,85, aspect mouillé) qui passe au jaune pâle `#E8F0A0` sur le bas (dégradé vertical), quelques petites taches vert foncé | « Croâ-ment bien. » |
| 10 | `animal_chat_roux` | Chat roux | Rare | T | Pelage **roux** `#E8873A` à rayures plus foncées `#B8602A` (chat tigré), museau et socle crème ; aspect **duveteux** : texture avec un léger grain de poils, smoothness 0,2 | « Il ronronne quand il gagne. » |
| 11 | `animal_panda` | Panda | Rare | T | Blanc cassé avec **zones noires** : oreilles, tour des yeux (sans cacher les yeux : grandes taches en larme autour), encolure basse et socle noirs ; mat (0,3). Demande des UV soignées du cheval | « Bambou, sieste, victoire. » |
| 12 | `animal_flamant` | Flamant | Rare | C + T | Rose `#FF8FB0` dégradé vers rose foncé `#E0507A` en bas, **motif de plumes** léger (écailles allongées claires), smoothness 0,5 | « Toujours sur une patte… enfin presque. » |
| 13 | `animal_cameleon` | Caméléon | Super rare | C + A | Peau à **petites écailles rondes** ; la couleur **change lentement** par grandes plaques qui glissent : vert `#4CC26A` → turquoise → jaune → orange → retour (un cycle en 10 s, chaque plaque avec un décalage) ; smoothness 0,6 | « Tu le vois ? Et maintenant ? » |
| 14 | `animal_meduse` | Méduse | Super rare | Tr + A | **Translucide rose** `#FFB0E0`, opacité 0,55 (comme le Cristal), avec un **liseré** plus clair qui ondule sur le haut ; une lumière douce interne qui bat (couleur de base 0,9 ↔ 1,1, période 2 s). Pas d'émission (Super rare) | « Légère comme une bulle. » |
| 15 | `animal_paon` | Paon | **Légendaire** | C + A + E + X | Voir fiche §3.1 | « Il fait la roue à chaque trésor. » |
| 16 | `animal_lion_soleil` | Lion solaire | **SECRET** | C + A + E + X + P | Voir fiche §3.2 | « Le roi des animaux… et du soleil. » |

### 3.1 `animal_paon` — Paon (Légendaire)
- Corps **bleu paon** `#1E6EB0` → turquoise `#1E9EB0` sur l'encolure, **irisé** : la teinte glisse
  légèrement selon l'angle de vue (approximation : la couleur de base bouge entre les deux bleus
  selon la rotation du cheval) ; smoothness 0,8, metallic 0,35. Petites **plumes** en écailles
  (texture), dont les bords luisent faiblement (émission 0,4).
- **Extras 3D : la roue** : un éventail de 13 grandes plumes (0,9 m) repliées à l'arrière, à
  l'horizontale, qui dépasse un peu de chaque côté. Chaque plume a un **ocelle** (œil de paon)
  vert `#2E9E5B` / bleu / or `#FFC83A` à l'extrémité, en émission 0,8.
- **Saut de joie** : la roue **se déploie** en éventail vertical derrière le cheval (0,35 s,
  `ease-out-back`), les ocelles s'allument à 1,4 une par une (0,04 s d'écart), reste 1,2 s, puis
  se replie (0,3 s). Son `paon_roue` (froufrou + petit « ta-da »).
- Au repos, la roue frémit (±2°, période 3 s).

### 3.2 `animal_lion_soleil` — Lion solaire (Secret) : la fiche complète
**L'idée** : un lion fait de soleil. Le Secret le plus « chaud » du jeu.

**Le corps** : or chaud `#FFB830` → orange `#FF7A1C` en bas (dégradé), metallic 0,5, smoothness
0,7 ; une **chaleur qui ondule** : des stries plus claires `#FFE58A` montent lentement le long du
corps (texture calculée, émission 0,5, comme l'air qui tremble au-dessus du sable).

**Extras 3D : la crinière de flammes**
- Une **crinière** autour de la tête et de l'encolure : 14 mèches de flamme stylisées (formes en
  goutte pointue, 0,25 à 0,45 m), en 2 couronnes. Dégradé jaune `#FFF1A8` (base) → orange
  `#FF7A1C` → rouge `#E0303A` (pointe), émission 1,2 à la base, 0,6 en pointe.
- Chaque mèche **danse** : elle s'allonge et se raccourcit (0,9 ↔ 1,15) et se tord (±12°) avec
  sa propre période (0,6 à 1,1 s) : effet de feu vivant.
- **Escarbilles** : petites étincelles orange qui s'échappent de la crinière et montent en
  tournoyant (réserve de 30, une toutes les 0,12 s).
- **Disque solaire** : derrière la tête, un grand disque plat de **rayons** (12 rayons, Ø 1,6 m,
  jaune pâle à 25 % d'opacité, émission 0,5) qui tourne lentement (10°/s). Toujours **derrière**
  le cheval (il ne cache jamais les coffres).

**Réactions** :
- **Saut de joie** : la crinière **s'embrase** (toutes les mèches à 1,3 et ×1,3 en taille pendant
  0,5 s), le disque solaire fait un tour rapide, le cheval pousse un **petit rugissement**
  mignon (`lion_rugit`, volume 0,4) et une onde de chaleur (anneau UI orange) part de lui.
- **Tête basse** : la crinière baisse (×0,7, émission 0,4) pendant 1 s, comme un feu qui couve,
  puis repart.

**Révélation** : touche thématique Animaux (`v5.md` §6.6) : des **empreintes de pattes**
lumineuses traversent l'écran, puis un **lever de soleil** (fond orange qui monte du bas) derrière
la carte ; le rugissement remplace la fin de la fanfare. Son `animal_secret_lion`.

**Contraintes** : émission max 1,3 (sous 1,5) ; la crinière ne cache pas les yeux ; triangles
≤ 2 000 pour les extras ; particules ≤ 60 au total.

---

## 4. Boîte d'accessoires Animaux (`boite_animaux_accessoires`)

- Nom affiché : **« Animaux : accessoires »** · Prix : **500 pièces** · 16 objets.
- Disponible seulement après la première renaissance ; taux habituels ; 5 / 4 / 3 / 2 / 1 / 1.
- Emplacements : Tête 7, Visage 3, Cou 2, Dos 4.

| N° | Identifiant | Nom | Rareté | Empl. | Description visuelle (artiste / DA) | Animation / effets | Phrase |
|---|---|---|---|---|---|---|---|
| 01 | `oreilles_chat` | Oreilles de chat | Commun | Tête | Serre-tête **noir `#2A2438`** avec deux grandes **oreilles de chat** triangulaires à intérieur rose `#FF9FC8`, posées **devant** les oreilles du cheval | — | « Miaou ? Miaou. » |
| 02 | `oreilles_lapin` | Oreilles de lapin | Commun | Tête | Serre-tête blanc avec **deux longues oreilles de lapin** (0,45 m) blanc cassé à intérieur rose ; l'une est **pliée** à mi-hauteur | l'oreille pliée rebondit au saut | « Il saute encore plus haut. » |
| 03 | `truffe` | Truffe | Commun | Visage | Grosse **truffe de chien** noire brillante `#2A2438` (smooth. 0,9) posée au bout du museau, avec un reflet blanc | se « renifle » (échelle 1 ↔ 1,06) toutes les 4 s | « Il flaire les trésors. » |
| 04 | `collier_medaille` | Collier à médaille | Commun | Cou | Collier **rouge** `#D64545` à boucle dorée qui épouse l'encolure, **médaille en os** dorée qui pend devant | la médaille se balance | « Si tu me trouves, rapporte-moi à la tour. » |
| 05 | `moustaches` | Moustaches | Commun | Visage | **6 moustaches** de chat fines mais épaisses de 1,5 cm (3 de chaque côté du museau), blanc cassé, et un petit nez rose triangulaire | frémissent toutes les 3 s | « Elles sentent le vent tourner. » |
| 06 | `bonnet_grenouille` | Grenouille | Peu commun | Tête | Bonnet **vert** `#4CC26A` en tissu avec **deux gros yeux** de grenouille en relief sur le dessus (blanc + pupille noire) et une bouche souriante brodée sur le bord | les yeux clignent toutes les 4 s | « Croâ-ffure du jour. » |
| 07 | `lunettes_hibou` | Lunettes hibou | Peu commun | Visage | **Grosses lunettes rondes** à monture brune `#8C5933` avec des **sourcils en plumes** orange `#F28C38` au-dessus de chaque verre ; verres légèrement teintés ambre (opaques : les yeux du cheval sont dessinés derrière, agrandis, sur le verre) | sourcils qui se haussent au survol d'un coffre | « Hou-hou, il réfléchit. » |
| 08 | `echarpe_renard` | Écharpe renard | Peu commun | Cou | Écharpe en forme de **renard** (fausse fourrure orange `#E8732A`, bout de queue blanc) enroulée autour de l'encolure, **tête de renard** endormie devant le poitrail, queue qui pend | la queue se balance | « Il fait la sieste autour de ton cou. » |
| 09 | `carapace` | Carapace | Peu commun | Dos | **Carapace de tortue** verte `#5A8A3A` à écailles hexagonales bordées de jaune `#C8D86A`, portée comme un sac à dos (bretelles brunes) ; dépasse sur les côtés | — | « Lent ? Jamais au moment d'encaisser. » |
| 10 | `ailes_chauve_souris` | Ailes de chauve-souris | Rare | Dos | Deux ailes **violet sombre** `#3A2858` à membranes plus claires `#6A4A98` (doigts visibles), bords en festons pointus | battement saccadé (±18°, 0,7 s, avec une petite pause) | « Il dort la tête en bas. » |
| 11 | `bois_cerf` | Bois de cerf | Rare | Tête | Deux **bois de cerf** brun clair `#B89060` à 4 pointes chacun, avec **3 petites fleurs** roses et une feuille accrochées | les fleurs tournent doucement | « Le roi de la forêt. » |
| 12 | `poussin` | Poussin | Rare | Tête | Un **poussin** jaune `#FFE070` tout rond (Ø 0,22) assis sur le crâne, bec orange, petites ailes, une **coquille d'œuf** cassée sur la tête | il piaille et sautille au saut de joie (son `poussin_piou`, 0,25) ; regarde le coffre survolé | « Il te prend pour sa maman. » |
| 13 | `chaton_sac` | Chaton | Super rare | Dos | Un **panier** en osier `#C99A6A` porté sur le dos, d'où **un chaton** gris `#9AA0B0` sort la tête et les pattes avant, bien visible au-dessus de l'épaule droite (écran) | il suit le curseur des yeux, cligne, et lève la patte au saut de joie (`chaton_miaou`, 0,25) | « Passager clandestin. » |
| 14 | `ruche` | Abeilles | Super rare | Tête | **Trois abeilles** rondes (Ø 0,1, rayées jaune / brun, ailes translucides) qui tournent autour de la tête sur des orbites différentes, et un petit **pot de miel** posé sur le crâne | ailes qui vibrent (très vite) ; orbites de 2,5 à 4 s ; au saut de joie elles font un cœur (trajectoire) | « Bzz bzz, tout le monde au travail. » |
| 15 | `bocal_poisson` | Bocal | **Légendaire** | Tête | Un **bocal rond** en verre transparent (Ø 0,55, opacité 0,3, smooth. 0,95) posé sur la tête comme un casque, avec de l'eau bleutée, **un poisson rouge** `#FF7A1C` qui nage dedans, des algues vertes et du sable | le poisson nage en rond (4 s), fait des bulles (réserve de 8) ; l'eau **luit** doucement (émission 0,5) ; au saut, l'eau fait une vague et le poisson saute hors de l'eau et y retombe | « Glouglou ? Glouglou ! » |
| 16 | `baleine_celeste` | Baleine céleste | **SECRET** | Dos | Voir fiche §4.1 | | « Elle chante entre les étoiles. » |

### 4.1 `baleine_celeste` — Baleine céleste (Secret, Dos)
**L'idée** : une petite baleine qui **nage dans l'air** autour du cheval, comme dans le ciel.

- **Modèle** : une baleine bleue (0,8 m de long), dos bleu nuit `#2E3A8A`, ventre rayé clair
  `#BFD4FF`, nageoires larges, gros œil doux. Sur le dos, **des taches d'étoiles** (petits
  points `#FFF1D6` en émission 1,2, qui scintillent comme la Galaxie).
- **Nage** : elle tourne autour du cheval sur une **trajectoire en 8** inclinée (largeur 2,2 m,
  hauteur entre 1,2 et 2,3 m), un tour en 9 s, en ondulant du corps (queue ±20°, 1,2 s) ; elle
  passe devant puis derrière le cheval (jamais devant les yeux plus de 0,5 s : la trajectoire
  passe au-dessus de la tête quand elle est devant).
- **Traînée d'étoiles** : derrière elle, une traînée de 30 petites étoiles et bulles qui
  s'effacent en 1,2 s (réserve réutilisée).
- **Saut de joie** : elle fait un **bond** (looping de 0,8 s) et **souffle un jet** d'étoiles et
  de confettis par son évent (20 particules), son `baleine_chant` (chant court et doux, 1,2 s,
  volume 0,35).
- **Tête basse** : elle vient se poser contre la joue du cheval (1 s) comme pour le consoler,
  puis repart.
- **Révélation** : touche thématique Animaux, version « ciel » : des bulles d'étoiles montent du
  bas de l'écran et la baleine (dessinée en UI, silhouette) traverse le fond derrière la carte.
  Son `animal_secret_baleine`.
- Contraintes : ≤ 3 000 triangles avec la traînée, émission ≤ 1,2, jamais plus de 0,5 s devant
  les yeux.

---

## 5. Récapitulatif pour la production

| Boîte | Objets | Super rare | Légendaire | Secret | Prix |
|---|---|---|---|---|---|
| Pirates : skins | 12 | 6 | 4 | 2 | 5 diamants |
| Pirates : accessoires | 12 | 6 | 4 | 2 | 5 diamants |
| Animaux : skins | 16 (5 C, 4 PC, 3 R) | 2 | 1 | 1 | 500 pièces (après renaissance) |
| Animaux : accessoires | 16 (5 C, 4 PC, 3 R) | 2 | 1 | 1 | 500 pièces (après renaissance) |
| **Total** | **56** | **16** | **10** | **6** | |

**À fabriquer** :
- **28 modèles 3D d'accessoires** (12 Pirates + 16 Animaux), dont 4 Secrets animés.
- **Extras de skins** (prefabs) : tentacules du Kraken, lanterne des Abysses, roue du Paon,
  crinière + disque du Lion solaire.
- **Textures répétables (T)** : 15 (Bois flotté, Corail, Carte au trésor, Vert-de-gris,
  Dalmatien, Zèbre, Vache, Tigre, Girafe, Léopard, Chat roux, Panda, Flamant, Écailles si le
  programmeur préfère une image, Kraken (ventouses) si plus simple) — si le DA refuse les
  textures images, elles deviennent calculées (C) et le programmeur en a pour plus longtemps.
- **Textures calculées / animées (C, A)** : Marinière, Écailles, Abeille, Coccinelle,
  Grenouille, Caméléon, Méduse, Or maudit, Fantôme, Tempête, Lagon, Kraken, Abysses, Paon, Lion.
- **56 icônes** (Gemini) + 4 images de boîtes (liste dans `v5.md` §17.3).
