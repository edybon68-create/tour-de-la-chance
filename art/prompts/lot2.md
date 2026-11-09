# Lot 2 — Prompts d'images Gemini (icônes des boîtes, boîtes, ciels des paliers)

> **À quoi sert ce fichier ?** Chaque bloc ci-dessous = **une image**. On copie le prompt
> (le texte en anglais dans le cadre gris) et on le colle tel quel dans Gemini, dans le
> navigateur. On enregistre ensuite l'image sous le **nom de fichier indiqué**.
>
> Les prompts sont en anglais parce que les générateurs d'images le comprennent mieux.
> Les explications autour sont en français.
>
> Références : `docs/art-bible.md` (palette §2.7 et §2.8, matières spéciales §4.1, décor par
> altitude §5.1, règles des images §9, raretés dans l'interface §10), `docs/specs/boites.md`
> (§5.3 skins, §5.6 fiches des accessoires, §12 images à produire) et
> `docs/specs/sons-animations.md` §3.14 (paliers de décor).

---

## 0. Mode d'emploi (à lire une fois)

1. **Quatre conversations Gemini**, une par groupe : A — Skins (12 images), B — Accessoires
   (12 images), C — Boîtes (2 images), D — Ciels (3 images). On ne mélange pas les groupes :
   dans une même conversation, Gemini garde le même style, le même angle et le même fond.
2. **On joint une image de référence au premier message** de chaque conversation (bouton « + »
   de Gemini, puis on colle le prompt dans le même message) :

   | Conversation | Image à joindre |
   |---|---|
   | A — Skins | `art/icones/couleur_rouge.png` |
   | B — Accessoires | `art/icones/chapeau_casquette.png` |
   | C — Boîtes | `art/icones/chapeau_casquette.png` |
   | D — Ciels | `art/fonds/ciel_nuit.png` |

   C'est ce qui garantit que les nouvelles icônes ressemblent à celles du lot 1.
3. **Le premier prompt est long, les suivants sont courts.** Le premier contient le bloc de
   style complet. Les suivants commencent par *« Same image as the previous one… only
   change… »* et ne décrivent que ce qui change (une ligne `BODY:` pour les skins, `OBJECT:`
   pour les accessoires). C'est la méthode qui a donné des capsules identiques au lot 1.
4. **Si une image dérive** (autre angle, fond qui change, capsule différente, style « photo ») :
   d'abord demander une correction courte (exemples sous chaque groupe). Si ça ne suffit pas
   après 2 essais, **ouvrir une nouvelle conversation**, rejoindre l'image de référence, et
   coller le **prompt de relance** du groupe (le prompt n°1 du groupe dans lequel on remplace
   la ligne `BODY:` ou `OBJECT:` par celle de l'image voulue). Chaque groupe l'explique.
5. **Le format est écrit dans le texte** (« square 1:1 », « wide 16:9 »). Si l'image sort dans
   le mauvais format : *« Same image, but square 1:1. »* (ou *« wide 16:9 »*).
6. **Les codes hexadécimaux** (`#F2D03B`…) sont des indications : ce qui compte, c'est que la
   teinte soit reconnaissable. Les couleurs exactes du jeu restent celles des matériaux Unity.
7. **Deux fonds d'icônes** :
   - presque toutes les icônes : uni lavande très clair `#E6E1EE` (comme au lot 1) ;
   - **trois exceptions** (Blanc nacré, Petites ailes d'ange, Auréole) : uni bleu-gris moyen
     `#5B6380`, parce que ces objets très clairs se confondraient avec le lavande au détourage
     (art bible §9.2). Ce sont les **dernières** images de leur conversation, pour que le
     changement de fond ne « contamine » pas les autres.
8. **Enregistrement** : PNG, nom exact (minuscules, sans accent, tirets bas), dans le dossier
   indiqué. Les noms des icônes sont les **identifiants du catalogue** (`couleur_jaune`,
   `lunettes_soleil`…) : le programmeur les retrouve automatiquement.
9. **Détourage** : comme au lot 1 (Aperçu sur Mac → « Supprimer l'arrière-plan »), résultat dans
   `art/icones_detourees/` avec le même nom. Vérifier en particulier : les **trous** du masque
   de héros et l'intérieur de l'écharpe, du collier et de l'auréole doivent devenir transparents.
10. **Pas de halo, rayons ni étincelles à générer** : ils sont dessinés par le code (§5 de ce
    fichier et art bible §10.3).

---

## 1. Bloc de style commun (inchangé depuis le lot 1)

Déjà inclus dans le premier prompt de chaque conversation : rien à ajouter à la main.

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.
```

Bloc « icône » (identique au lot 1) :

```
ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.
```

**Ce qui doit être identique dans tout le lot** : la lumière (chaude en haut à gauche, liseré
lavande derrière), l'angle (3/4, ~25° au-dessus, tourné ~20° vers la gauche), l'objet seul,
centré, ~70 % du cadre, et le fond uni sans ombre portée.

**Ce que les icônes ne montrent jamais** : la rareté (pas de cadre coloré, pas d'étoiles, pas de
halo autour) — c'est l'interface qui ajoute le liseré de rareté (art bible §10).

---

## 2. Conversation A — Skins (12 images)

### Règles communes au groupe
- **La même capsule que `couleur_rouge.png`** : pilule deux fois plus haute que large, **socle
  rond de pion** en bas, deux yeux noirs ovales avec un reflet blanc en haut à gauche. Ni bouche,
  ni bras, ni chapeau. **Seule la matière du corps change.**
- Le socle a la même matière que le corps.
- Les yeux restent noirs `#111111` brillants sur tous les skins (même Cristal et Galaxie).
- Ordre : du plus simple (couleurs mates, proches du rouge de référence) au plus spécial, puis
  le Blanc nacré en dernier (changement de fond).

---

### Image 1 — `art/icones/couleur_jaune.png` (Jaune citron, Commun)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **À faire** : nouvelle conversation, **joindre `art/icones/couleur_rouge.png`**, puis coller :

```
Use the attached image as the exact reference: same capsule character, same shape with its round pawn base, same eyes, same size in the frame, same angle, same lighting and same plain light lavender background. Only the body color changes.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a cute capsule-shaped toy character standing upright: a smooth pill shape twice as tall as it is wide, standing on a round flat pawn base, like a board-game pawn. Its only features are two glossy black oval eyes (#111111), slightly taller than wide, half sunk into the body at the top front, each with a small white highlight in the upper left. No mouth, no nose, no arms, no legs, no visor, no backpack, no hat, no accessories. Perfectly smooth surface.
BODY: lemon yellow (#F2D03B), matte like painted wood, with only a soft highlight. Not gold, not metallic, not orange.
```

**Critères de réussite** : la capsule est **la même** que la rouge (forme, socle, yeux, taille,
angle) ; jaune citron franc, mat, **pas doré**.

---

### Image 2 — `art/icones/couleur_orange.png` (Orange mandarine, Commun)

- **Format** : carré 1:1, fond uni `#E6E1EE`. À la suite, même conversation.

```
Same image as the previous one: same capsule, same shape with its round pawn base, same eyes, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material.
BODY: tangerine orange (#F28C38), matte. Not red, not brown, not metallic.
```

**Critères de réussite** : orange mandarine (ni rouge, ni marron), mat.

---

### Image 3 — `art/icones/couleur_rose.png` (Rose bonbon, Commun)

```
Same image as the previous one: same capsule, same shape with its round pawn base, same eyes, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material.
BODY: candy pink (#F27BB0), matte. Not purple, not red, not pastel white.
```

**Critères de réussite** : rose bonbon soutenu, mat ; il doit se distinguer du fond lavande.

---

### Image 4 — `art/icones/couleur_turquoise.png` (Turquoise lagon, Peu commun)

```
Same image as the previous one: same capsule, same shape with its round pawn base, same eyes, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material.
BODY: lagoon turquoise (#2BB8C4) with a slightly glossy satin finish and a soft highlight. Not metallic, not green, not dark teal.
```

**Critères de réussite** : turquoise, un peu plus brillant que les mats (comme le violet du lot 1),
pas métallique.

---

### Image 5 — `art/icones/couleur_caramel.png` (Caramel, Peu commun)

```
Same image as the previous one: same capsule, same shape with its round pawn base, same eyes, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material.
BODY: glossy caramel (#C98A4B), very shiny like a hard candy, with a bright glossy highlight in the upper left. Smooth and clean: not metallic, not wood, no grain, no drips, no sugar crystals.
```

**Critères de réussite** : effet « bonbon » très brillant, pas bois, pas cuivre.

---

### Image 6 — `art/icones/motif_sucre_orge.png` (Sucre d'orge, Rare)

```
Same image as the previous one: same capsule, same shape with its round pawn base, same eyes, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material.
BODY: candy cane stripes: 8 wide straight horizontal bands of equal height from the bottom to the top of the body, alternating tomato red (#D64545) and off-white (#F4EEE6), satin finish. The stripes are perfectly horizontal, not spiral, not diagonal; they wrap all around the body and follow its curve. Flat painted stripes, no relief. The black eyes sit on top of the stripes.
```

**Critères de réussite** : rayures **horizontales** (pas en spirale), larges, rouge / blanc
cassé, yeux toujours bien visibles.

**Correction fréquente** : *« Make the stripes perfectly horizontal rings, not a spiral, and
fewer and wider: 8 bands in total. »*

---

### Image 7 — `art/icones/motif_pois.png` (Pois de fête, Rare)

```
Same image as the previous one: same capsule, same shape with its round pawn base, same eyes, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material.
BODY: candy pink (#F27BB0) with round off-white polka dots (#F4EEE6), each dot about one eighth of the body width, evenly spaced in staggered rows all over the body, satin finish. The dots are flat and painted, not bumps. No stripes, no dots on the eyes.
```

**Critères de réussite** : pois réguliers en quinconce, ni trop petits ni trop gros, plats.

---

### Image 8 — `art/icones/matiere_argent.png` (Argent poli, Rare)

```
Same image as the previous one: same capsule, same shape with its round pawn base, same eyes, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material.
BODY: polished silver (#C9CED8), metallic like a soft mirror, reflecting a simple studio with warm light and dark violet areas so that its shape reads clearly against the light background; bright highlight in the upper left. Not gold, not blue chrome, no engravings, no scratches.
```

**Critères de réussite** : argent miroir **lisible** sur le fond clair (zones sombres dans les
reflets) ; même forme que l'or du lot 1.

**Correction fréquente** : *« Add darker reflections on the silver so it does not blend into
the background. »*

---

### Image 9 — `art/icones/matiere_cristal.png` (Cristal, Super rare)

```
Same image as the previous one: same capsule, same shape with its round pawn base, same eyes, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material.
BODY: translucent ice-blue crystal glass (#9FE3FF), about half transparent: we can faintly see through the body, and the two black eyes look like they float inside the glass. Very smooth and glossy, with crisp bright reflections along the edges that outline its shape. Not frosted, no facets, no cracks, no bubbles, no rainbow colors.
```

**Critères de réussite** : bleu glace translucide, contour bien dessiné par les reflets, yeux
visibles ; pas un diamant à facettes.

---

### Image 10 — `art/icones/matiere_arc_en_ciel.png` (Arc-en-ciel néon, Légendaire)

```
Same image as the previous one: same capsule, same shape with its round pawn base, same eyes, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material.
BODY: a smooth rainbow gradient flowing from bottom to top through soft neon hues (pink, orange, yellow, mint green, sky blue, violet), medium saturation, not harsh. The body glows softly from inside as if lit by its own light, but the glow stays on the body: no halo, no light rays and no sparkles on the background. Satin finish, not metallic.
```

**Critères de réussite** : dégradé arc-en-ciel doux qui semble lumineux, **aucune lueur ni
étincelle sur le fond** (sinon le détourage est sale).

**Correction fréquente** : *« Remove the glow around the capsule: the background must stay
plain flat lavender. »*

---

### Image 11 — `art/icones/matiere_galaxie.png` (Galaxie, Secret)

```
Same image as the previous one: same capsule, same shape with its round pawn base, same eyes, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material.
BODY: deep night blue-violet (#2E2366), glossy, sprinkled with small warm glowing stars (#FFF1D6): tiny round dots of different sizes, scattered randomly but sparsely, a few of them slightly brighter with a tiny four-pointed twinkle. The stars glow but the glow stays on the body. No nebula, no galaxy spiral, no planets, no rainbow; plain flat lavender background with no glow.
```

**Critères de réussite** : bleu nuit avec petites étoiles chaudes, peu nombreuses ; les yeux
noirs restent visibles (le reflet blanc les détache du fond sombre).

**Correction fréquente** : *« Fewer and smaller stars, and keep the eyes clearly visible. »*

---

### Image 12 — `art/icones/couleur_nacre.png` (Blanc nacré, Peu commun) — **fond bleu-gris**

- **Format** : carré 1:1, **fond uni `#5B6380`** (exception, voir §0 point 7). Toujours en
  dernier dans la conversation.

```
Same image as the previous one: same capsule, same shape with its round pawn base, same eyes, same size in the frame, same angle and same lighting. Only change the body material AND the background color.
BODY: pearly white (#EDE6F2) with a soft pearl sheen: very subtle pink and pale blue iridescent reflections on the curves, slightly glossy. Not chrome, not plain grey, not pure white.
BACKGROUND: plain flat solid medium blue-grey (#5B6380), no gradient, no floor, no scenery, no cast shadow on the background.
```

**Critères de réussite** : blanc nacré avec de légers reflets rose/bleu, bien détaché du fond
bleu-gris ; capsule toujours identique.

---

### Prompt de relance (conversation A)
Si les capsules dérivent : nouvelle conversation, joindre `couleur_rouge.png`, coller le prompt
de l'**image 1** en remplaçant sa dernière ligne `BODY: …` par la ligne `BODY: …` de l'image
voulue. Pour le Blanc nacré, ajouter aussi sa ligne `BACKGROUND: …` à la fin.

---

## 3. Conversation B — Accessoires (12 images)

### Règles communes au groupe
- **L'objet seul**, sans personnage, sans tête, sans mannequin : même cadrage, même angle et
  même lumière que `chapeau_casquette.png`.
- Taille : l'objet occupe **~70 % du cadre dans sa plus grande dimension** (hauteur pour le
  chapeau de sorcier, largeur pour les ailes, les lunettes, l'écharpe).
- Les lunettes, le masque, le nœud et les ailes sont vus **de face, en 3/4** (l'avant vers le
  spectateur, tourné ~20° vers la gauche), comme s'ils étaient portés par un personnage
  invisible. L'écharpe, le collier et l'auréole sont des **anneaux vides** vus un peu d'en haut.
- Formes et couleurs : fiches de `docs/specs/boites.md` §5.6 et art bible §2.8.
- Ordre : chapeaux d'abord (les plus proches de la référence), puis cou, visage, dos, et les
  deux objets clairs en dernier (fond bleu-gris).

---

### Image 13 — `art/icones/chapeau_bonnet.png` (Bonnet de laine, Commun)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **À faire** : nouvelle conversation, **joindre `art/icones/chapeau_casquette.png`**, puis coller :

```
Use the attached image as the exact reference for the style, the size in the frame, the angle, the lighting and the plain light lavender background. Draw a different object.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image width with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

OBJECT: a chunky cartoon wool beanie, alone, not worn by anyone, no head inside. A soft rounded dome in lagoon turquoise (#2BB8C4), matte; a thick folded cuff around the bottom in off-white (#F4EEE6) with a few soft vertical ribs; a big round, slightly bumpy off-white pompom (#F4EEE6) on top. Smooth and simple: no knit texture, no label, no logo, no pattern.
```

**Critères de réussite** : même taille dans le cadre et même angle que la casquette ; revers et
pompon blanc cassé, pas de texture de tricot.

---

### Image 14 — `art/icones/chapeau_sorcier.png` (Chapeau de sorcier, Rare)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image at its largest dimension. Only change the object.
OBJECT: a chunky cartoon wizard hat, alone, not worn by anyone. A wide, slightly wavy round brim and a tall cone in deep night violet felt (#4B2E83), matte. The top third of the cone bends backward, away from the viewer, at about 30 degrees, a soft playful bend. A yellow band (#FFD54A) around the base of the cone, with one small chunky five-pointed yellow star (#FFD54A) on the front of the band. No face, no eyes, no mouth, no wrinkles forming a face, no patches, no moons, no other stars.
```

**Critères de réussite** : pointe pliée vers l'arrière, une seule étoile devant, **aucun visage**
dans les plis (pas de ressemblance avec un chapeau célèbre).

---

### Image 15 — `art/icones/noeud_papillon.png` (Nœud papillon, Commun)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object.
OBJECT: a chunky cartoon bow tie, alone, not worn by anyone, its front facing the viewer at the same three-quarter angle. Two thick rounded triangular wings in red satin (#C0392B) with a soft sheen, on each side of a small rounded central knot in darker red (#8E2A20). Clearly visible thickness, toy-like. No collar, no shirt, no strap, no pattern.
```

**Critères de réussite** : nœud épais, deux rouges (ailes + nœud plus foncé), rien d'autre.

---

### Image 16 — `art/icones/echarpe.png` (Écharpe rayée, Peu commun)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object.
OBJECT: a chunky cartoon striped scarf, alone, not worn by anyone, wrapped in a thick round loop as if around an invisible round neck: the middle of the loop is empty. Seen from slightly above, so the loop forms an ellipse. One short end hangs down in front, slightly to the left and a little slanted. Alternating wide stripes of mint green (#3CC48A) and off-white (#F4EEE6) all around the loop and across the hanging end (about 4 stripes on the end). Soft wool look but smooth and simple: no knit texture, no fringe, no logo.
```

**Critères de réussite** : anneau vide au milieu, un pan qui pend devant à gauche, rayures
vert menthe / blanc cassé.

---

### Image 17 — `art/icones/collier_fleurs.png` (Collier de fleurs, Peu commun)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object.
OBJECT: a chunky cartoon flower necklace (a garland of flowers), alone, not worn by anyone, in a round loop as if around an invisible round neck: the middle of the loop is empty. Seen from slightly above; the front of the loop hangs a little lower than the back. Ten chunky flowers touching each other, each with five round flat petals around a yellow heart (#FFD54A), alternating pink (#FF6FB5) and off-white (#F4EEE6). Thick toy-like petals, no leaves, no visible string, no tiny details.
```

**Critères de réussite** : une dizaine de grosses fleurs alternées rose / blanc cassé à cœur
jaune, anneau vide, pas de feuilles.

---

### Image 18 — `art/icones/lunettes_soleil.png` (Lunettes de soleil, Commun)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object.
OBJECT: a pair of chunky cartoon sunglasses, alone, not worn by anyone, their front facing the viewer at the same three-quarter angle, the short arms going backward. Two big rounded-rectangle lenses, thick, very glossy dark blue-grey (#2A2A3A), with a matching thick frame and a short bridge; one small diagonal white highlight streak (#FFFFFF) on each lens. Cool and simple: no logo on the arms, no scene reflected in the lenses, no gradient lenses.
```

**Critères de réussite** : lunettes épaisses gris-bleu très sombre, reflet blanc en diagonale,
**aucun logo**.

---

### Image 19 — `art/icones/lunettes_coeur.png` (Lunettes cœur, Peu commun)

```
Same image as the previous one: same style, same angle, same lighting, same size and same plain light lavender background (#E6E1EE). Only change the object.
OBJECT: a pair of chunky cartoon heart-shaped glasses, alone, not worn by anyone, their front facing the viewer at the same three-quarter angle, the short arms going backward. Two big thick heart-shaped lenses in glossy pink (#FF6FB5), each with a thin off-white frame (#F4EEE6) around it, an off-white bridge and short off-white arms; one small diagonal white highlight streak on each lens.
```

**Critères de réussite** : deux cœurs roses bien reconnaissables, monture blanc cassé fine.

---

### Image 20 — `art/icones/masque_heros.png` (Masque de héros, Rare)

```
Same image as the previous one: same style, same angle, same lighting, same size and same plain light lavender background (#E6E1EE). Only change the object.
OBJECT: a chunky cartoon hero eye mask, alone, not worn by anyone: a thick curved band shaped like a simple domino mask, bending around an invisible round face, in hero blue (#3A5BD9) with a thin darker blue edge (#22357F), satin finish. Two large oval eye holes through which the plain background is visible. Two short tied ribbon ends stick out at the back on each side. No emblem, no letter, no symbol, no face, no eyes inside the holes. It must not look like the mask of any existing superhero.
```

**Critères de réussite** : bandeau bleu courbé, **deux trous** où l'on voit le fond (ils
deviendront transparents au détourage), **aucun emblème**.

---

### Image 21 — `art/icones/ailes_papillon.png` (Ailes de papillon, Super rare)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object.
OBJECT: a pair of chunky cartoon butterfly wings, alone: no butterfly body, no insect, no antennae, no character. Seen from the front at the same three-quarter angle, symmetrical, spread wide, joined at the center by a small rounded dark violet knob, slightly angled backward. Each side has a big rounded upper lobe and a smaller rounded lower lobe, flat but thick (visible thickness), lagoon turquoise (#2BB8C4) with a satin sheen, one round orange spot (#F28C38) on each big upper lobe, and a dark violet border (#34286A) all around. No veins, no other patterns, no glitter.
```

**Critères de réussite** : 4 lobes turquoise bordés de violet sombre, une tache orange par
grand lobe, **pas de corps d'insecte**.

---

### Image 22 — `art/icones/planete.png` (Petite planète, Secret)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object.
OBJECT: a small chunky cartoon planet, alone: a smooth round ball in lavender violet (#B48CFF), slightly glossy, surrounded by a flat thick ring in yellow (#FFD54A) that glows softly, the ring tilted about 20 degrees. The glow stays on the ring: no halo, no rays and no sparkles on the background. No craters, no face, no moons, no stars, no other planets.
```

**Critères de réussite** : boule lavande bien plus soutenue que le fond, anneau jaune incliné,
fond resté uni.

**Correction fréquente** : *« Make the planet a deeper lavender violet so it stands out from the
background. »*

---

### Image 23 — `art/icones/ailes_ange.png` (Petites ailes d'ange, Rare) — **fond bleu-gris**

- **Format** : carré 1:1, **fond uni `#5B6380`** (exception, §0 point 7).

```
Same image as the previous one: same style, same angle and same lighting; the object fills about 70 percent of the image width. Only change the object AND the background color.
OBJECT: a pair of small chunky cartoon angel wings, alone: no body, no character. Seen from the front at the same three-quarter angle, symmetrical, joined at the center by a small rounded off-white knob, tips pointing upward, each wing angled outward and slightly backward. Each wing is a thick rounded shape whose lower edge is scalloped into three big rounded feathers, off-white (#F4EEE6) with soft lavender-grey shading between the feathers (#D9D2E4), matte. No small individual feathers, no halo, no sparkles.
BACKGROUND: plain flat solid medium blue-grey (#5B6380), no gradient, no floor, no scenery, no cast shadow on the background.
```

**Critères de réussite** : deux petites ailes arrondies à 3 grosses plumes, blanc cassé bien
détaché du fond bleu-gris.

---

### Image 24 — `art/icones/aureole.png` (Auréole lumineuse, Légendaire) — **fond bleu-gris**

```
Same image as the previous one: same style, same angle, same lighting and the same plain medium blue-grey background (#5B6380); the object fills about 70 percent of the image width. Only change the object.
OBJECT: a chunky glowing halo ring, alone: a smooth thick torus (a donut-shaped ring), horizontal, seen from slightly above so it forms a wide ellipse with an empty middle, pale warm yellow (#FFF1A8), glowing softly from inside like a light. The glow stays close to the ring: no rays, no big halo, no sparkles on the background. No spikes, no gems, not a crown.
```

**Critères de réussite** : anneau épais et lisse, lumineux mais **sans grand halo** sur le fond,
milieu vide.

---

### Prompt de relance (conversation B)
Nouvelle conversation, joindre `chapeau_casquette.png`, coller le prompt de l'**image 13** en
remplaçant sa dernière ligne `OBJECT: …` par la ligne `OBJECT: …` de l'image voulue (et, pour
les ailes d'ange et l'auréole, ajouter la ligne `BACKGROUND: …` de l'image 23).

---

## 4. Conversation C — Boîtes (2 images)

### Règles communes au groupe
- Deux **coffrets cadeaux magiques** de **même forme** : cube un peu plus large que haut,
  biseauté, couvercle qui déborde légèrement, ruban laiton en croix, gros nœud laiton sur le
  dessus, **médaillon rond en laiton au milieu de la face avant**, sur le ruban, avec un emblème
  en relief. Un mince filet de lumière chaude s'échappe sous le couvercle : « quelque chose de
  magique est dedans ». Seuls la couleur du corps et l'emblème changent.
- Couleurs : art bible §2.8 (le corps de la boîte de skins est en violet magique `#8E5BD6`, plus
  lisible que le `#6A4C9C` proposé par la spec, gardé pour le bord du couvercle).
- Même fond, même angle et même lumière que les icônes (elles seront détourées puis affichées à
  120 × 120 sur les cartes et à 300 × 300 sur le voile sombre de l'animation d'ouverture).
- **Aucun texte**, aucun point d'interrogation, aucune étiquette : on ne doit pas pouvoir
  deviner une rareté sur la boîte.

---

### Image 25 — `art/icones/boite_skins_1.png` (Boîte de skins n°1)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **À faire** : nouvelle conversation, **joindre `art/icones/chapeau_casquette.png`**, puis coller :

```
Use the attached image as the exact reference for the style, the size in the frame, the angle, the lighting and the plain light lavender background. Draw a different object.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

OBJECT: a chunky magical toy gift box, closed. A cube slightly wider than tall with soft beveled edges, the body in magical violet (#8E5BD6) with a satin finish; a slightly overhanging lid whose edge is a darker violet (#6A4C9C). A wide brass ribbon (#C99A1E) crosses the box: down the middle of the front face, down the side, and over the lid; a big chunky brass bow with two rounded loops sits on top. In the middle of the front face, on the ribbon, a round raised brass medallion; in its center, in relief, a simple paint drop emblem divided into three horizontal bands: tomato red (#D64545), mint green (#3CC48A) and sky blue (#4A90D9). A thin line of soft warm light (#FFF1D6) glows from the gap under the lid, as if something magical is inside; the glow stays on the box. No text, no letters, no numbers, no question mark, no tag, no card, no confetti, no sparkles on the background.
```

**Critères de réussite** : on reconnaît tout de suite un **cadeau** (ruban en croix + gros nœud),
violet franc, médaillon laiton visible devant avec la goutte tricolore, **aucun texte**.

**Corrections fréquentes**
- *« Make the brass medallion bigger and clearly visible on the front face. »*
- *« Remove the question mark / the tag: no symbol other than the paint drop. »*

---

### Image 26 — `art/icones/boite_accessoires_1.png` (Boîte d'accessoires n°1)

```
Same image as the previous one: same box shape and size, same lid, same brass ribbon and bow, same medallion, same light under the lid, same framing, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the colors of the box and the emblem.
OBJECT: the body is lagoon turquoise (#2BB8C4) with a satin finish, the lid edge is a darker turquoise (#1F8A94). The emblem in relief on the brass medallion is now a tiny chunky black top hat (#1E1E1E) with a red band (#C0392B). No text, no letters, no numbers, no question mark, no tag.
```

**Critères de réussite** : la **même boîte** que l'image 25 (on doit sentir une collection),
turquoise, mini haut-de-forme lisible sur le médaillon.

---

## 5. Halo, rayons, étincelle (`boites.md` §12) : pas d'image, c'est le code

**Décision du DA : on ne génère pas `ui_halo.png`, `ui_rayons.png` ni `ui_etincelle.png`.**
- Gemini ne fait pas de PNG transparent ; détourer un dégradé flou (le halo) laisse un bord sale.
- Le nombre de rayons change avec la rareté (8, 12, 16, 20) : une image unique à 8 rayons ne
  conviendrait pas.
- Ce sont des formes géométriques simples, plus nettes et plus légères dessinées par le code
  (`Texture2D` calculée au démarrage, ou `Painter2D` de UI Toolkit), en blanc, puis teintées par
  la couleur de rareté.

La description exacte des trois formes est dans `docs/art-bible.md` §10.3, pour le programmeur.

---

## 6. Conversation D — Ciels des paliers d'altitude (3 images)

### Règles communes au groupe
- **Format** : large 16:9 (Gemini sort du 1376 × 768, comme `ciel_nuit.png`). Ce ne sont pas
  des textures tuilables : une image de fond unique par palier.
- **Même monde, de plus en plus haut** : mêmes nuages « pouf » arrondis low-poly, mêmes petites
  étoiles et scintillements à 4 branches que `ciel_nuit.png` (palier 1 : étages 1-3, déjà fait).
- La montée se lit de bas en haut : palier 1, nuages au-dessus ; **palier 2 (étages 4-6)**, dans
  les nuages ; **palier 3 (7-9)**, au-dessus de la mer de nuages ; **palier 4 (10+)**, dans les
  étoiles.
- **Centre calme et sans détail** (les coffres et le personnage sont devant), **bas le plus
  sombre** (boutons et panneau), rien de net au milieu. La lune et l'aurore sont en haut, sur un
  côté.
- Couleurs dominantes = colonne « Ambiance » de `sons-animations.md` §3.14 ; jamais plus clair
  que `#B49AC8` (art bible §5.1).
- Pas de tour, de bâtiment, de personnage, d'objet ni de texte.

---

### Image 27 — `art/fonds/ciel_palier2.png` (« Dans les nuages ! », étages 4 à 6)

- **Format** : large 16:9.
- **À faire** : nouvelle conversation, **joindre `art/fonds/ciel_nuit.png`**, puis coller :

```
Use the attached image as the reference for the style, the format, the kind of puffy low-poly clouds and stars, and the calm composition. This is the same world, but higher up.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

SUBJECT: a wide 16:9 background image, to be placed behind a 3D game scene: we are now inside the clouds, high above the fairground. No tower, no building, no character, no object, no ground. A soft luminous pink-lavender mist fills the whole image: a smooth gradient from light misty lavender (#B49AC8) in the upper middle, to dusty violet (#7A6290), to the darkest deep violet (#3A3048) at the bottom and in the corners. Big puffy rounded stylized low-poly clouds all around the edges: on the left, on the right, in the top corners, and a thick soft bank of clouds along the bottom, in muted lavender and pinkish tones, with soft warm pink highlights (#FFE6E0) on their tops, like a rosy glow through the mist. The center of the image is calm, soft, empty mist, with no cloud edge and no detail. Only two or three very faint stars, no moon, no sun, no birds, no rainbow. Low contrast overall, dreamy and cozy; nothing brighter than a soft light lavender anywhere, so that objects placed in front stand out clearly.
```

**Critères de réussite** : on se sent **dans** les nuages ; brume rose-lavande, plus claire que
le palier 1 mais douce ; centre vide ; bas plus sombre.

**Correction fréquente** : *« Darker and calmer: lower the contrast, make the bottom darker and
keep the center empty. »*

---

### Image 28 — `art/fonds/ciel_palier3.png` (« Au-dessus des nuages ! », étages 7 à 9)

```
Same style, same 16:9 format, same kind of puffy low-poly clouds and stars and same calm composition rules as the previous images, but a new place, higher up: we are now above the sea of clouds, at night.

SUBJECT: a wide 16:9 background image, to be placed behind a 3D game scene. No tower, no building, no character, no object, no text. A clear deep night blue sky: a smooth gradient from soft periwinkle blue (#8FA0D8) in a gentle glow around the moon, through night blue (#5A6AA0) in the middle, to the darkest deep blue (#2A3050) at the bottom and in the corners. A big round pale moon (#E8ECFF) high in the upper right corner, not in the center, softly glowing, with no face and only two or three soft rounded craters. Small soft low-poly stars, sparse, more of them toward the top and the left, with a few four-pointed twinkles. Across the bottom third, a wide calm sea of puffy rounded low-poly clouds seen from above, stretching to the horizon, in muted moonlit blue-lavender (#5A6AA0 to #8A90B0), only their rounded tops catching a little cool moonlight; the bottom edge of the image stays the darkest. The center is calm open sky with nothing in it. No fairground, no buildings, no birds, no airplane, no planets, no shooting star trails, no saturated colors. Low contrast overall.
```

**Critères de réussite** : mer de nuages **sous** nous, lune ronde en haut à droite (pas au
centre, pas de visage), ciel bleu nuit plus froid que les paliers 1 et 2.

**Correction fréquente** : *« Move the moon to the upper right corner and make it smaller and
softer. »*

---

### Image 29 — `art/fonds/ciel_palier4.png` (« Dans les étoiles ! », étages 10 et plus)

```
Same style, same 16:9 format, same kind of soft low-poly stars and same calm composition rules as the previous images, but a new place, much higher: we are now in the stars, high above the clouds.

SUBJECT: a wide 16:9 background image, to be placed behind a 3D game scene. No tower, no building, no character, no object, no text. A deep starry night sky: a smooth gradient from deep blue-violet (#6A5AB0) in a soft glow behind the center, to the darkest night (#1A1430) at the bottom and in the corners. Many small soft low-poly stars of different sizes, a few brighter four-pointed twinkles, and a few faint soft violet wisps of star dust. Across the upper part of the sky, a gentle aurora: two or three soft wavy ribbons of light in muted teal green (#3A6A80 to a soft mint) blending into violet, low saturation, flowing from the upper left toward the upper right and staying in the top third, away from the center. Far below, along the very bottom edge, the faint rounded tops of a few clouds in deep shadow, almost invisible. No moon, no planets, no galaxy spiral, no rocket, no shooting star trails, no saturated neon. The center stays calm and fairly dark. Low contrast overall.
```

**Critères de réussite** : le plus sombre des 4 ciels, aurore douce **en haut**, beaucoup
d'étoiles mais petites ; rien de vif au centre.

**Correction fréquente** : *« Make the aurora softer and keep it in the top third of the
image. »*

---

## 7. Récapitulatif — ordre de génération

| # | Conversation Gemini | Fichier | Format | Fond |
|---|---|---|---|---|
| 1 | A — Skins (joindre `couleur_rouge.png`) | `art/icones/couleur_jaune.png` | carré 1:1 | `#E6E1EE` |
| 2 | A — Skins | `art/icones/couleur_orange.png` | carré 1:1 | `#E6E1EE` |
| 3 | A — Skins | `art/icones/couleur_rose.png` | carré 1:1 | `#E6E1EE` |
| 4 | A — Skins | `art/icones/couleur_turquoise.png` | carré 1:1 | `#E6E1EE` |
| 5 | A — Skins | `art/icones/couleur_caramel.png` | carré 1:1 | `#E6E1EE` |
| 6 | A — Skins | `art/icones/motif_sucre_orge.png` | carré 1:1 | `#E6E1EE` |
| 7 | A — Skins | `art/icones/motif_pois.png` | carré 1:1 | `#E6E1EE` |
| 8 | A — Skins | `art/icones/matiere_argent.png` | carré 1:1 | `#E6E1EE` |
| 9 | A — Skins | `art/icones/matiere_cristal.png` | carré 1:1 | `#E6E1EE` |
| 10 | A — Skins | `art/icones/matiere_arc_en_ciel.png` | carré 1:1 | `#E6E1EE` |
| 11 | A — Skins | `art/icones/matiere_galaxie.png` | carré 1:1 | `#E6E1EE` |
| 12 | A — Skins (en dernier) | `art/icones/couleur_nacre.png` | carré 1:1 | **`#5B6380`** |
| 13 | B — Accessoires (joindre `chapeau_casquette.png`) | `art/icones/chapeau_bonnet.png` | carré 1:1 | `#E6E1EE` |
| 14 | B — Accessoires | `art/icones/chapeau_sorcier.png` | carré 1:1 | `#E6E1EE` |
| 15 | B — Accessoires | `art/icones/noeud_papillon.png` | carré 1:1 | `#E6E1EE` |
| 16 | B — Accessoires | `art/icones/echarpe.png` | carré 1:1 | `#E6E1EE` |
| 17 | B — Accessoires | `art/icones/collier_fleurs.png` | carré 1:1 | `#E6E1EE` |
| 18 | B — Accessoires | `art/icones/lunettes_soleil.png` | carré 1:1 | `#E6E1EE` |
| 19 | B — Accessoires | `art/icones/lunettes_coeur.png` | carré 1:1 | `#E6E1EE` |
| 20 | B — Accessoires | `art/icones/masque_heros.png` | carré 1:1 | `#E6E1EE` |
| 21 | B — Accessoires | `art/icones/ailes_papillon.png` | carré 1:1 | `#E6E1EE` |
| 22 | B — Accessoires | `art/icones/planete.png` | carré 1:1 | `#E6E1EE` |
| 23 | B — Accessoires (avant-dernier) | `art/icones/ailes_ange.png` | carré 1:1 | **`#5B6380`** |
| 24 | B — Accessoires (en dernier) | `art/icones/aureole.png` | carré 1:1 | **`#5B6380`** |
| 25 | C — Boîtes (joindre `chapeau_casquette.png`) | `art/icones/boite_skins_1.png` | carré 1:1 | `#E6E1EE` |
| 26 | C — Boîtes | `art/icones/boite_accessoires_1.png` | carré 1:1 | `#E6E1EE` |
| 27 | D — Ciels (joindre `ciel_nuit.png`) | `art/fonds/ciel_palier2.png` | 16:9 | — |
| 28 | D — Ciels | `art/fonds/ciel_palier3.png` | 16:9 | — |
| 29 | D — Ciels | `art/fonds/ciel_palier4.png` | 16:9 | — |

**Pas d'image** pour le halo, les rayons et l'étincelle (dessinés par le code, §5).

**Après génération**
1. Détourer les 26 icônes (n°1 à 26) → `art/icones_detourees/<même nom>.png` ; vérifier que les
   trous du masque et le milieu de l'écharpe, du collier et de l'auréole sont transparents.
2. Copier dans Unity : icônes détourées → `TourDeLaChance-Unity/Assets/Art/icones_detourees/` ;
   ciels → `TourDeLaChance-Unity/Assets/Art/fonds/`. Le palier 1 utilise l'actuel
   `ciel_nuit.png` (le programmeur peut le référencer sous ce nom ; pas besoin de le dupliquer).
3. Contrôle rapide dans le jeu : réduire chaque icône à 64 px et la teinter en `#15121F`
   (silhouette de l'Index) : on doit encore reconnaître l'objet.
