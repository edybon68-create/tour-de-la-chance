# Lot 1 — Prompts d'images Gemini (concept, icônes du shop, logo, ciel)

> **À quoi sert ce fichier ?** Chaque bloc ci-dessous = **une image**. On copie le prompt
> (le texte en anglais dans le cadre gris) et on le colle tel quel dans Gemini, dans le
> navigateur. On enregistre ensuite l'image sous le **nom de fichier indiqué**.
>
> Les prompts sont en anglais parce que les générateurs d'images le comprennent mieux.
> Les explications autour sont en français.
>
> Référence : `docs/art-bible.md` (palette §2, formes §3, éclairage §5, interdits §8) et
> `docs/specs/shop.md` §4 (couleurs et chapeaux du shop).

---

## 0. Mode d'emploi (à lire une fois)

1. **Une conversation Gemini par groupe d'images** (une pour le concept, une pour les 9 icônes,
   une pour le logo, une pour le ciel). Dans une même conversation, Gemini garde mieux le même
   style d'une image à l'autre.
2. **Ordre conseillé** : générer d'abord le concept art (image 1). S'il est réussi, on peut le
   joindre (bouton « + » / ajouter une image) en tête des conversations suivantes en écrivant
   avant le prompt : *« Use the attached image as the style reference. »*. Ce n'est pas
   obligatoire : chaque prompt est complet à lui seul.
3. **Le format est écrit dans le texte** (« square image », « wide 16:9 image ») : Gemini dans
   le navigateur n'a pas de réglage séparé. Si l'image sort dans le mauvais format, répondre
   simplement : *« Same image, but square 1:1. »* (ou *« wide 16:9 »*).
4. **Vérifier chaque image** avec la liste « Critères de réussite » sous le prompt. Si un point
   rate, demander une correction courte dans la même conversation (exemples donnés), plutôt que
   de tout reformuler.
5. **Les codes hexadécimaux** (`#FFC83A`…) sont des indications : les générateurs ne les
   respectent pas au pixel près. Ce qui compte, c'est que la teinte soit **reconnaissable** et
   proche de la palette. Les couleurs exactes du jeu restent celles des matériaux Unity.
6. **Enregistrement** : en PNG, dans le dossier indiqué (à créer s'il n'existe pas), avec le nom
   exact (minuscules, sans accent, tirets bas). Les noms des icônes reprennent les
   **identifiants du shop** (`couleur_rouge`, `chapeau_fete`…) : le programmeur pourra les
   retrouver automatiquement.
7. **Détourage des icônes** : elles sont demandées sur un fond uni lavande très clair
   (`#E6E1EE`), choisi parce qu'il ne ressemble à aucune couleur des objets (ni l'or, ni le
   violet, ni le blanc pur du pompon). On enlève ce fond ensuite (outil « Supprimer
   l'arrière-plan » d'Aperçu sur Mac, ou tout autre outil de détourage) pour obtenir un PNG
   transparent.

---

## 1. Bloc de style commun (recopié au début de chaque prompt)

C'est la « signature visuelle » du jeu, tirée de l'art bible. Il est **déjà inclus** dans chaque
prompt ci-dessous : il n'y a rien à ajouter à la main. Il est rappelé ici pour référence et pour
les futurs lots.

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.
```

Pourquoi ces choix (rappel de l'art bible) :
- *toy style / bevels / no textures* : notre style « jouet low-poly » (§3), sans réalisme (§8).
- *warm key light upper left + violet ambient + lavender rim* : l'éclairage du jeu (§5).
- *no money symbols* : la règle d'or, nos pièces sont des pièces de conte, pas de l'argent réel.

---

## 2. Concept art général (priorité 1)

### Image 1 — `art/concept/concept_etage.png`

- **Format** : large, 16:9 (paysage).
- **Rôle** : la référence de style pour tout le reste (modèles Blender, icônes, interface).
- **Cohérent avec** : tout le lot. C'est lui qui fixe l'angle de caméra, la lumière et les
  matières. Il reproduit la scène réelle du jeu (caméra en (0 ; 5 ; −7), regard plongeant ~35°).

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

SUBJECT: a wide 16:9 game concept art image. The top floor of a fairy-tale tower at night, seen from a fixed high three-quarter camera in front, looking down at about 35 degrees, like a tabletop diorama.
- The floor is a round stone platform made of blue-grey stone slabs (#5B6380, some slightly darker #535A75) arranged in three concentric rings, with dark grooves between the slabs (#3C4258). A raised border ring of lighter stone blocks (#8A90B0) runs around the edge. In the center of the floor, a subtle eight-pointed star rosette inlaid flush with the ground, in light stone and thin violet lines (#6A4C9C). The vertical side of the platform shows big stone blocks like a castle wall, in shadow (#434A63).
- In the middle of the platform, three identical closed treasure chests stand side by side in a straight row, evenly spaced, facing the camera. Each chest is a chunky toy pirate chest: warm brown wooden body (#8C5933) with three horizontal planks, a rounded domed lid in darker wood (#6B4226) that overhangs slightly, two vertical dark metal bands (#4A4E5A) going over the front and the lid, small metal corner brackets, and a big shield-shaped brass lock plate (#C99A1E) with a keyhole on the front.
- Directly behind the middle chest stands a cute capsule-shaped character, about twice as tall as a chest: a smooth upright pill shape, twice as tall as it is wide, sky blue (#4A90D9), matte. Its only features are two glossy black oval eyes (#111111), each with a small white highlight in the upper left. No mouth, no nose, no arms, no legs, no visor, no backpack. The lower part of its body is hidden behind the middle chest. It wears a black magician's top hat (#1E1E1E) with a red satin band (#C0392B); the brim curls up on the left and right sides, and the crown widens slightly toward the top.
- The front half of the platform, closest to the camera, is empty so the stone slabs are clearly visible.
- Background: a deep night violet sky (#1F1730) with a soft lighter violet glow (#3A2850) behind the tower, a few small soft stars, and far below, very blurred warm fairground lights glowing in the distance. The background is calm and dark so the warm chests and the character stand out.
- The warm objects (wood, brass, character) contrast with the cool, muted stone and violet night. Clean composition, everything centered, nothing cut off at the edges.
```

**Critères de réussite**
- Trois coffres **identiques**, alignés, couvercles **bombés** et serrures en laiton visibles.
- Le personnage est une capsule simple avec **seulement deux yeux** (pas de bouche, pas de
  visière-hublot, pas de bras). Il est derrière le coffre du milieu.
- Pierre bleu-gris froide, fond violet nuit, objets chauds : le contraste guide l'œil.
- Aucun texte, aucune lettre, aucun signe monétaire.

**Corrections fréquentes** (à taper dans la même conversation)
- *« Same image, but the three chests must be identical and all closed. »*
- *« Remove the mouth and the arms from the capsule character: only two black eyes. »*
- *« Less realistic: smoother toy-like shapes, no wood grain, no wear. »*

---

## 3. Icônes du shop (priorité 2) — 9 images

### Règles communes au groupe (à respecter pour les 9)

- **Format** : carré 1:1.
- **Fond** : uni, plat, lavande très clair `#E6E1EE`, **sans dégradé, sans sol, sans ombre
  portée** sur le fond (pour un détourage propre).
- **Cadrage** : objet seul, **centré**, qui occupe environ **70 % de la hauteur**, avec une marge
  égale tout autour. Rien n'est coupé.
- **Angle** : vue de 3/4 légèrement en hauteur (environ 25° au-dessus), objet tourné d'environ
  20° vers la gauche, l'avant de l'objet vers le spectateur. **Le même pour les 9.**
- **Lumière** : identique pour les 9 (lumière chaude en haut à gauche, liseré lavande derrière).
- **Taille d'affichage** : ces icônes seront petites dans les cartes du shop. Silhouette simple
  et lisible avant tout.
- Les 4 capsules doivent être **la même capsule** (même forme, mêmes yeux, même angle) : seule la
  couleur change. Le plus simple : générer d'abord `couleur_rouge`, puis pour les trois autres,
  dans la même conversation, coller le prompt complet (il commence par « Same framing as the
  previous image »).
- Même principe pour les 4 chapeaux : générer `chapeau_casquette` d'abord, puis les autres.

Bloc « icône » ajouté après le bloc de style dans les 9 prompts :
`ICON FORMAT: a square 1:1 image ... no cast shadow on the background.` (déjà inclus ci-dessous).

---

### Image 2 — `art/icones/couleur_rouge.png` (Rouge tomate, 50 pièces)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **Cohérent avec** : images 3, 4, 5 (même capsule, même angle, même lumière, même fond).

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a cute capsule-shaped toy character standing upright: a smooth pill shape twice as tall as it is wide, with a very slightly wider rounded base like a board-game pawn. Its body is tomato red (#D64545), matte. Its only features are two glossy black oval eyes (#111111), slightly taller than wide, half sunk into the body at the top front, each with a small white highlight in the upper left. No mouth, no nose, no arms, no legs, no visor, no backpack, no hat, no accessories. Perfectly smooth surface.
```

**Critères de réussite** : capsule entière visible, rouge tomate (pas rouge sang), seulement deux
yeux avec reflets, fond uni sans ombre, aucun texte.

---

### Image 3 — `art/icones/couleur_vert.png` (Vert menthe, 75 pièces)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **Cohérent avec** : image 2 (à générer juste après, dans la même conversation).

```
Same framing, same angle, same lighting and same background as the previous image; only the body color changes.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a cute capsule-shaped toy character standing upright: a smooth pill shape twice as tall as it is wide, with a very slightly wider rounded base like a board-game pawn. Its body is fresh mint green (#3CC48A), matte. Its only features are two glossy black oval eyes (#111111), slightly taller than wide, half sunk into the body at the top front, each with a small white highlight in the upper left. No mouth, no nose, no arms, no legs, no visor, no backpack, no hat, no accessories. Perfectly smooth surface.
```

**Critères de réussite** : identique à l'image 2 sauf la couleur ; vert menthe (pas vert sapin,
pas fluo).

---

### Image 4 — `art/icones/couleur_violet.png` (Violet magique, 150 pièces)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **Cohérent avec** : images 2 et 3.

```
Same framing, same angle, same lighting and same background as the previous image; only the body color and finish change.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a cute capsule-shaped toy character standing upright: a smooth pill shape twice as tall as it is wide, with a very slightly wider rounded base like a board-game pawn. Its body is magical purple (#8E5BD6) with a slightly glossy, satin finish and a soft highlight. Its only features are two glossy black oval eyes (#111111), slightly taller than wide, half sunk into the body at the top front, each with a small white highlight in the upper left. No mouth, no nose, no arms, no legs, no visor, no backpack, no hat, no accessories, no sparkles. Perfectly smooth surface.
```

**Critères de réussite** : violet franc, un peu plus brillant que le rouge et le vert, mais
**pas métallique** ; pas d'étincelles ajoutées.

---

### Image 5 — `art/icones/couleur_or.png` (Or massif, 300 pièces)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **Cohérent avec** : images 2 à 4 (même capsule) et image 9 (même or que la couronne) et 10
  (même or que la pièce).

```
Same framing, same angle, same lighting and same background as the previous image; only the body material changes.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a cute capsule-shaped toy character standing upright: a smooth pill shape twice as tall as it is wide, with a very slightly wider rounded base like a board-game pawn. Its body is made of polished shiny solid gold (#FFC83A), metallic, with warm golden reflections and a bright highlight from the upper left, like a golden trophy. Its only features are two glossy black oval eyes (#111111), slightly taller than wide, half sunk into the body at the top front, each with a small white highlight in the upper left. No mouth, no nose, no arms, no legs, no visor, no backpack, no hat, no engravings, no jewels, no accessories. Perfectly smooth surface.
```

**Critères de réussite** : or chaud et brillant (pas jaune plat, pas orange cuivré), yeux
toujours noirs, aucune gravure ni décoration.

---

### Image 6 — `art/icones/chapeau_casquette.png` (Casquette, 100 pièces)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **Cohérent avec** : images 7, 8, 9 (même angle, même lumière, même fond, même taille dans le
  cadre). À générer en premier dans une **nouvelle conversation** « chapeaux ».

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image width with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a chunky cartoon baseball cap, alone, not worn by anyone, no head inside. A rounded dome crown in bright red (#E04040) with six soft seams running from a small round white button (#FFFFFF) on top. A wide, thick, slightly curved visor in darker red (#A02828) pointing toward the viewer and slightly to the left, exaggerated in size like a toy. Matte fabric look but smooth and simple, no stitching details, no logo, no letter, no patch, no emblem on the front.
```

**Critères de réussite** : casquette **vierge** (aucune lettre ni logo sur le devant, c'est
important : pas de ressemblance avec un personnage connu), visière large et bien visible.

---

### Image 7 — `art/icones/chapeau_fete.png` (Chapeau de fête, 150 pièces)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **Cohérent avec** : image 6.

```
Same framing, same angle, same lighting and same background as the previous image; only the hat changes.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a chunky cartoon birthday party hat, alone, not worn by anyone. A pointed cone made of shiny cardboard, decorated with four wide horizontal stripes alternating pink (#FF6FB5) and yellow (#FFD54A) from bottom to top: pink, yellow, pink, yellow. A big fluffy, slightly bumpy white pompom (#FFFFFF), matte, on the tip. The whole hat is tilted about 10 degrees to the side for a playful look. The bottom rim shows a visible thickness. No elastic string, no confetti, no glitter, no pattern other than the stripes.
```

**Critères de réussite** : rayures **horizontales** (pas en spirale), 4 bandes rose/jaune,
pompon blanc mat, léger penché.

---

### Image 8 — `art/icones/chapeau_haut_de_forme.png` (Haut-de-forme, 250 pièces)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **Cohérent avec** : image 6, et avec le haut-de-forme du concept art (image 1).

```
Same framing, same angle, same lighting and same background as the previous image; only the hat changes.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a chunky cartoon magician's top hat, alone, not worn by anyone. Soft black felt (#1E1E1E), very dark grey rather than pure black. A tall cylindrical crown that widens slightly toward the flat top, with a soft bevel around the top edge that catches the light. A wide brim that curls gently upward on the left and right sides. A red satin band (#C0392B) around the base of the crown, with a small flat bow of the same red on the left side. A clear lavender rim light outlines the hat so its silhouette stands out. No rabbit, no wand, no stars, no cards, no buckle.
```

**Critères de réussite** : le chapeau ne doit pas être une tache noire : on doit voir le biseau
du dessus, le liseré lavande et la forme du bord relevé. Aucun lapin ni baguette.

**Correction fréquente** : *« Make the felt dark grey instead of pure black, and add a stronger
lavender rim light so the shape is readable. »*

---

### Image 9 — `art/icones/chapeau_couronne.png` (Couronne royale, 500 pièces)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **Cohérent avec** : image 6 (cadrage) et image 5 (même or).

```
Same framing, same angle, same lighting and same background as the previous image; only the hat changes.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image width with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a chunky fairy-tale king's crown, alone, not worn by anyone, the most precious-looking item of the shop. Polished shiny gold (#FFC83A), metallic, with warm reflections. A thick round band with a small raised rim at the top and the bottom; the inside of the band is visible from above. Five rounded triangular points on top, evenly spaced, one of them exactly in the center front. A round gold pearl on the tip of each point. One big faceted ruby (#D62839), very glossy, set in the center of the front of the band, with a small gold setting around it. Simple, chunky and toy-like, no fine filigree, no extra gems, no velvet cap, no cross on top.
```

**Critères de réussite** : 5 pointes avec perles dorées, **un seul** rubis bien au centre
devant, même or que la capsule dorée, forme simple (pas une couronne ouvragée réaliste ni copiée
d'un jeu connu).

---

### Image 10 — `art/icones/piece.png` (pièce d'or de l'interface)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **Rôle** : affichée à côté du porte-monnaie, de la cagnotte et des prix. Elle sera **très
  petite** (environ 24 à 48 pixels) : la silhouette doit rester lisible.
- **Cohérent avec** : images 2 à 9 (même fond, même lumière) et image 5 (même or). À générer à
  la suite des chapeaux.

```
Same lighting and same background as the previous image.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game interface icon. One single object, alone, perfectly centered, filling about 75 percent of the image with equal margins, nothing cropped. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: one single thick fairy-tale gold coin, like a chunky toy game token, standing upright and turned slightly (about 20 degrees) so both its round face and its thick edge are visible. Polished shiny gold (#FFC83A), metallic, with a bright highlight in the upper left. A wide raised rounded rim around the face. In the center of the face, a simple raised embossed symbol: a small round tower top with a little five-pointed star above it, bold and simple enough to read at a very small size. No numbers, no letters, no currency sign, no portrait, no face, no other coins. It must not look like real-world money or a casino chip.
```

**Critères de réussite** : une seule pièce, épaisse, motif tour + étoile (pas de chiffre, pas de
`$`/`€`, pas de visage). Tester en la réduisant à 32 pixels : on doit encore reconnaître une
pièce d'or.

---

## 4. Logo / titre (priorité 3) — 2 images

### Règles communes au groupe

- **Fond** : uni violet nuit `#1F1730` (la couleur du fond du jeu) : le logo pourra être utilisé
  tel quel sur l'écran titre, ou détouré.
- **Mêmes matières** que les objets du jeu : or `#FFC83A`, laiton, bois, pierre bleu-gris.
- **Attention au texte** : les générateurs d'images font souvent des fautes (lettres en trop,
  « Chanse », « Tuor »…). **Vérifier chaque lettre** de « Tour de la Chance ». Si après 3 ou 4
  essais le texte est toujours faux, garder l'**emblème sans texte** (image 12) et écrire le
  titre dans l'interface UI Toolkit avec une police arrondie et épaisse, en or `#FFD54A` avec
  un contour `#3A2200` (les couleurs déjà utilisées pour les textes or du jeu). C'est la
  solution la plus fiable.
- L'emblème et le logo doivent avoir **le même dessin** (même tour, même étoile) : générer
  l'image 11 d'abord, puis l'image 12 dans la même conversation.

---

### Image 11 — `art/logo/logo_titre.png` (logo avec texte)

- **Format** : large, 16:9 (paysage).
- **Cohérent avec** : image 12 (même emblème), image 10 (même or).

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

EXCEPTION TO THE NO-TEXT RULE: this image is the game's title logo and must contain exactly one piece of text.

SUBJECT: a wide 16:9 video game title logo, centered, on a plain flat solid night violet background (#1F1730), nothing else in the background.
The title text reads exactly "Tour de la Chance" (four words: T-o-u-r, d-e, l-a, C-h-a-n-c-e), spelled exactly like this, in French, with a capital T and a capital C and the other letters lowercase, on two lines: "Tour" on the first line, "de la Chance" on the second line. The letters are big, chunky, rounded 3D toy letters made of polished shiny gold (#FFC83A), with soft bevels and a thick dark brown outline (#3A2200), easy to read, slightly bouncy like fairground lettering.
Above the text, a small emblem: the round top of a little fairy-tale stone tower in blue-grey stone (#5B6380, #8A90B0) with crenellations, and a big five-pointed gold star shining just above it. A few tiny soft sparkles around the star. Clean, symmetrical composition with generous margins, nothing cropped. No other words, no subtitle, no extra letters, no numbers.
```

**Critères de réussite** : orthographe **exacte** « Tour de la Chance », lettres or lisibles,
aucun mot en plus, fond uni violet.

**Corrections fréquentes**
- *« The text is misspelled. It must read exactly: Tour de la Chance. Keep everything else the
  same. »*
- *« Remove the subtitle and any extra words. »*

---

### Image 12 — `art/logo/embleme.png` (emblème sans texte)

- **Format** : carré 1:1.
- **Rôle** : variante fiable sans texte. Sert d'icône de l'application, de décor de l'écran
  titre, ou de logo si le texte de l'image 11 est raté (le titre est alors écrit par l'interface).
- **Cohérent avec** : image 11 (même tour, même étoile, même or, même fond).

```
Same emblem design, same colors and same background as the previous image, but with no text at all.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

SUBJECT: a square 1:1 game emblem, centered, on a plain flat solid night violet background (#1F1730), nothing else in the background. A round badge with a thick polished gold rim (#FFC83A) and soft bevels. Inside the badge, on a deep violet field (#3A2850): the round top of a little fairy-tale stone tower in blue-grey stone (#5B6380, #8A90B0) with chunky crenellations, and a big five-pointed gold star shining just above it, with a few tiny soft sparkles. Across the bottom of the badge, a wide blank ribbon banner in warm wood brown (#8C5933) with brass edges (#C99A1E), completely empty, with no writing on it. Symmetrical, chunky and toy-like, generous margins, nothing cropped. Absolutely no text, no letters, no numbers anywhere.
```

**Critères de réussite** : **aucune lettre**, même sur le ruban (il doit rester vide : on
pourra y écrire le titre plus tard dans l'interface), même tour et même étoile que l'image 11.

**Correction fréquente** : *« Remove all letters from the ribbon: it must be completely blank. »*

---

## 5. Fond de ciel nocturne (priorité 4)

### Image 13 — `art/fonds/ciel_nuit.png`

- **Format** : large, 16:9 (paysage).
- **Rôle** : remplacer le fond uni `#1F1730` derrière la tour (fond de caméra ou image derrière
  la scène). Ce n'est **pas** une texture tuilable : c'est une image de fond unique.
- **Cohérent avec** : le fond du concept art (image 1). Il doit rester **sombre et calme** : la
  tour, les coffres et le personnage (modèles 3D) seront devant, et l'interface (panneaux
  `#2A2A3A`, textes or) par-dessus. Le **centre** est un peu plus clair (halo `#3A2850`) parce
  que c'est là que se trouve la tour à l'écran ; le **bas** est le plus sombre parce que le
  panneau du shop et les boutons s'y affichent.

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

SUBJECT: a wide 16:9 background image of a calm magical night sky, to be placed behind a 3D game scene. No tower, no building, no character, no object in the foreground: only sky and very distant scenery. A smooth gradient of deep night violet (#1F1730), slightly lighter in the center where a soft, wide violet glow (#3A2850) sits, and darkest at the bottom and in the corners. Many small, soft, low-poly stars of different sizes, sparse and gentle, a few slightly brighter four-pointed twinkles. A few soft, rounded, stylized low-poly clouds in muted violet tones near the top. Along the very bottom, far away and very blurred, the faint warm glow of fairground lights and the soft silhouettes of distant rooftops and a tiny Ferris wheel, very dark and low in contrast. No moon face, no bright moon in the center, no planets, no shooting star trails, no saturated neon colors. Low contrast overall so that objects placed in front stand out clearly.
```

**Critères de réussite** : sombre, peu contrasté, rien de lumineux au centre qui gênerait la
lecture des coffres ; pas de tour ni de personnage dans l'image.

**Correction fréquente** : *« Darker and calmer: lower the contrast and make the stars smaller
and fewer. »*

---

## 6. Facultatif (si le temps le permet)

### Image 14 — `art/icones/couleur_bleu.png` (Bleu ciel, gratuit)

La carte de la couleur gratuite aura sinon un simple carré de couleur alors que les autres ont
une capsule. À générer dans la conversation des capsules, après l'image 5.

```
Same framing, same angle, same lighting and same background as the previous image; only the body color and material change.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a cute capsule-shaped toy character standing upright: a smooth pill shape twice as tall as it is wide, with a very slightly wider rounded base like a board-game pawn. Its body is sky blue (#4A90D9), matte, not metallic. Its only features are two glossy black oval eyes (#111111), slightly taller than wide, half sunk into the body at the top front, each with a small white highlight in the upper left. No mouth, no nose, no arms, no legs, no visor, no backpack, no hat, no accessories. Perfectly smooth surface.
```

Pour « Aucun chapeau », pas d'image : l'interface affichera un symbole simple (par exemple un
cercle barré) dessiné en USS.

---

## 7. Récapitulatif — ordre de génération

| # | Conversation Gemini | Fichier | Format |
|---|---|---|---|
| 1 | A — Concept | `art/concept/concept_etage.png` | 16:9 |
| 2 | B — Capsules | `art/icones/couleur_rouge.png` | carré |
| 3 | B — Capsules | `art/icones/couleur_vert.png` | carré |
| 4 | B — Capsules | `art/icones/couleur_violet.png` | carré |
| 5 | B — Capsules | `art/icones/couleur_or.png` | carré |
| 6 | C — Chapeaux | `art/icones/chapeau_casquette.png` | carré |
| 7 | C — Chapeaux | `art/icones/chapeau_fete.png` | carré |
| 8 | C — Chapeaux | `art/icones/chapeau_haut_de_forme.png` | carré |
| 9 | C — Chapeaux | `art/icones/chapeau_couronne.png` | carré |
| 10 | C — Chapeaux | `art/icones/piece.png` | carré |
| 11 | D — Logo | `art/logo/logo_titre.png` | 16:9 |
| 12 | D — Logo | `art/logo/embleme.png` | carré |
| 13 | E — Ciel | `art/fonds/ciel_nuit.png` | 16:9 |
| 14 | B — Capsules (facultatif) | `art/icones/couleur_bleu.png` | carré |

Après génération : détourer les icônes (fond `#E6E1EE` → transparent), puis copier les images
dans `TourDeLaChance-Unity/Assets/Art/` en gardant les mêmes sous-dossiers et les mêmes noms.
