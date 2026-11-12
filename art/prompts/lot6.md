# Lot 6 — Prompts d'images Gemini (v5 : 56 objets des boîtes Pirates et Animaux + 4 images de boîtes)

> **À quoi sert ce fichier ?** Chaque bloc ci-dessous = **une image**. On copie le prompt
> (le texte en anglais dans le cadre gris) et on le colle tel quel dans Gemini, dans le
> navigateur. On enregistre ensuite l'image sous le **nom de fichier indiqué**.
>
> Les prompts sont en anglais parce que les générateurs d'images le comprennent mieux.
> Les explications autour sont en français.
>
> Références : `docs/specs/v5-catalogue.md` (fiche de chaque objet : couleurs, formes, effets),
> `docs/specs/v5.md` §17.1 (aspect des 4 boîtes) et §17.3 (liste des images), `docs/art-bible.md`
> §9 (règles des images), §13 (accessoires v5 : trois niveaux de forme, deux matières).
> Méthode et format : identiques aux lots 3 à 5.

---

## 0. Mode d'emploi (à lire une fois)

1. **Pas d'image jointe dans ce lot.** Pour l'instant on ne peut pas joindre d'image à Gemini.
   Donc le **premier prompt de chaque conversation est autonome** : il contient tout (bloc de
   style commun + bloc icône + description complète du cheval ou des règles d'accessoire). Les
   suivants sont **courts** (*« Same image as the previous one… only change… »*) : Gemini
   garde le style, l'angle, la lumière et le fond de l'image précédente.
   Si un jour on peut à nouveau joindre une image, on joint en plus au premier message
   `art/icones/couleur_bleu.png` (conversations de skins) ou `art/icones/chapeau_casquette.png`
   (accessoires, boîtes) : ça ne peut qu'aider.
2. **Cinq conversations Gemini**, une par groupe ; on ne mélange jamais les groupes :

   | Conversation | Contenu | Images |
   |---|---|---|
   | **P1** | Skins Pirates (le cheval portant le skin) | 12 |
   | **P2** | Accessoires Pirates (l'objet seul) | 12 |
   | **A1** | Skins Animaux (le cheval portant le skin) | 16 |
   | **A2** | Accessoires Animaux (l'objet seul) | 16 |
   | **B** | Images des 4 boîtes | 4 |

3. **Format des skins = le cheval entier qui porte la matière**, exactement comme les 17 icônes
   de skins du lot 3 (`couleur_bleu.png`, `motif_pois.png`, `matiere_galaxie.png`…) : même
   pièce d'échecs jouet, socle compris, vue 3/4 face, deux yeux visibles. Ce n'est **pas** une
   pastille ou une boule de matière. Pour les 4 skins qui ont des **extras 3D** (tentacules du
   Kraken, lanterne des Abysses, roue du Paon, crinière du Lion), l'icône montre le cheval
   **avec** ses extras ; l'image suivante demande explicitement de les retirer.
4. **Format des accessoires = l'objet seul**, comme `chapeau_casquette.png` ou `echarpe.png` :
   sans cheval ni tête ; ce qui entoure le cou ou la tête forme un **anneau vide** « autour d'un
   cou invisible ». Les descriptions suivent le catalogue (les modèles Blender sont en cours :
   **l'icône doit ressembler au futur modèle**, donc on ne rajoute rien qui n'est pas dans la fiche).
5. **Ordre dans chaque conversation** : les **Secrets et Légendaires d'abord** (priorité), puis
   les autres du plus rare au plus commun, et **les images sur fond bleu-gris toujours à la fin**
   (sinon le changement de fond « contamine » les suivantes). Deux exceptions assumées :
   - dans **A1**, la première image est le **Caméléon** (Super rare) et pas le Lion solaire :
     la première image sert de **modèle du cheval** pour les 15 autres ; un cheval nu, sans
     crinière de flammes ni disque, donne une base plus fiable ;
   - les objets **Légendaires/Secrets très clairs** (Fantôme, Navire fantôme, Bocal) sont en fin
     de conversation à cause de leur fond bleu-gris.
6. **Deux fonds** (art bible §9.2), pour un détourage propre :
   - presque tout : **uni lavande très clair `#E6E1EE`** ;
   - objets **trop clairs ou transparents** : **uni bleu-gris moyen `#5B6380`**. Dans ce lot
     (9 images) : `pirate_fantome`, `navire_fantome`, `animal_meduse`, `animal_panda`,
     `animal_dalmatien`, `animal_vache`, `bocal_poisson`, `oreilles_lapin`, `moustaches`.
     (`animal_zebre` et `pirate_mariniere` restent sur lavande, comme le Sucre d'orge du lot 3 :
     les bandes foncées dessinent le contour. S'ils se détourent mal, les refaire en bleu-gris.)
7. **Règles de contenu** (art bible §9.3, catalogue §0.1) : aucun texte, aucune lettre, aucun
   chiffre (même sur la boussole ou la médaille), aucun logo, aucune marque ; pas de copie d'un
   jeu, d'un film ou d'une mascotte existants ; **pas de tête de mort, pas d'arme, rien
   d'effrayant** : pirates et animaux de dessin animé, ton mignon. Les icônes **ne montrent
   jamais la rareté** (pas de cadre, pas de halo, pas d'étoiles autour) : c'est l'interface qui
   ajoute le liseré. Les lueurs et étincelles restent **sur l'objet**, jamais sur le fond.
8. **Les codes hexadécimaux** sont des indications : ce qui compte, c'est que la teinte soit
   reconnaissable. Les couleurs exactes du jeu restent celles des matériaux Unity.
9. **Enregistrement** : PNG, nom exact = **identifiant du catalogue** (minuscules, sans accent,
   tirets bas), dans `art/icones/`. Le programmeur retrouve les icônes par ce nom.
10. **Si une image dérive** (autre angle, autre fond, cheval différent, style « photo ») :
    d'abord une correction courte (exemples dans chaque groupe) ; si ça ne suffit pas après 2
    essais, **nouvelle conversation** avec le **prompt de relance** du groupe.

---

## 1. Blocs communs

**Bloc de style commun** (inchangé depuis le lot 1, art bible §9.1) — déjà recopié dans les
premiers prompts :

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.
```

**Bloc « icône cheval »** (identique au lot 3) et **description du cheval** (identique au lot 3,
image 1) : utilisés dans les premiers prompts de P1 et A1.

**Bloc « icône »** (identique aux lots 1 à 5) : premiers prompts de P2, A2 et B.

**Nouveaux petits blocs de règles** (en anglais, déjà inclus là où il faut) :
- `SKIN RULES` : les motifs (rayures, taches, carte…) sont **peints à plat** comme une
  décalcomanie de jouet, jamais une texture photo. Ça précise le « no textures » du bloc de style
  sans le modifier : on veut des motifs, mais propres et plats.
- `ACCESSORY RULES` : objet seul, anneau vide autour d'une tête ou d'un cou invisible, pas de
  rareté visible, rien d'effrayant.

**Ce qui doit être identique dans tout le lot** : la lumière (chaude en haut à gauche, liseré
lavande derrière), l'angle (3/4, ~25° au-dessus, tourné ~20° vers la gauche), l'objet seul et
centré (~70 % du cadre, ~75 % pour le cheval), le fond uni sans ombre portée, le rendu « jouet
verni » des lots précédents. **Dans P1 et A1 : le même cheval** sur toutes les images (yeux et
narines toujours noirs avec reflet blanc ; seule la matière change).

---

## 2. Conversation P1 — Skins Pirates (12 images)

### Règles communes au groupe
- Le même cheval que les icônes de skins du lot 3. **La première image (Or maudit) devient la
  référence** du groupe.
- Après le Kraken et les Abysses, le prompt suivant **retire** les extras (la phrase « The piece
  has no extra parts » est dans chaque prompt court).
- Fantôme en dernier (fond bleu-gris).

---

### Image 1 — `art/icones/pirate_or_maudit.png` (Or maudit, Légendaire)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **À faire** : nouvelle conversation, coller :

```
Create this image from scratch (no reference image is attached). It belongs to an existing set of game shop icons, so follow every instruction below exactly.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single character, alone, perfectly centered, the whole piece visible from the bottom of its round base to the tips of its ears, filling about 75 percent of the image height with equal margins, nothing cropped. Three-quarter front view from slightly above (about 25 degrees), the piece turned about 20 degrees to the left, its face and muzzle toward the viewer so that both eyes are clearly visible. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a cute toy chess knight piece, like a chunky painted wooden board-game piece that has come to life. From bottom to top: a round flat pawn base with a stepped collar (two stacked rounded rings with beveled edges); a thick, round, smooth neck rising from the base, bulging forward at the chest and slightly thinner under the jaw; a horse head shaped like a softly rounded box, with a long, soft, rounded muzzle that sticks out toward the viewer and tilts slightly downward, for a gentle, friendly look; two small dark oval nostrils (#111111) side by side at the tip of the muzzle; two big glossy black oval eyes (#111111), slightly taller than wide, high on the front of the head just above the base of the muzzle, half sunk into the head, each with a small white highlight in the upper left, both eyes clearly visible; two short, thick, pointed ears on top of the head; a sculpted mane made of five or six chunky rounded tufts, starting with a small forelock tuft on the forehead between the ears and running down the back of the neck. The piece is about twice as tall as its base is wide. The whole piece (base, neck, head, muzzle, ears and mane) is made of ONE single material and color; only the eyes and nostrils are black. No mouth, no teeth, no legs, no tail, no saddle, no bridle, no rider, no horn, no hat, no accessories. Smooth surfaces with soft bevels, low-poly toy look, not a realistic horse.

SKIN RULES: any pattern or marking on the body is a flat painted design on the surface, like a clean toy decal, never a photographic texture. Any glow stays on the piece itself. The icon never shows rarity: no frame, no halo, no light rays, no sparkles on the background.

BODY: polished shiny solid gold (#FFC83A), metallic, with warm golden reflections and a bright highlight from the upper left, crossed by thin branching veins of glowing emerald green (#3CFF9A), like delicate cracks of magic light, spread sparsely over the base, the neck and the head, never over the eyes. Two or three small soft curls of pale green mist (#3CFF9A, half transparent), shaped like stylized toy smoke curls, wrap around the bottom of the round base and touch it; they do not rise above the base. No coins, no skull, no jewels, nothing floating on the background.
```

**Critères de réussite** : notre cheval (socle de pion, museau qui avance, crinière en grosses
mèches) ; or brillant + fines veines vertes lumineuses ; volutes vertes collées au socle ; deux
yeux visibles ; fond lavande uni.

**Corrections fréquentes**
- *« Turn the horse a bit more toward the viewer: both eyes must be clearly visible. »*
- *« Show the whole piece including the round base: nothing cropped, a bit smaller in the frame. »*
- *« Remove the mouth and the legs: it is a chess piece, only eyes and nostrils on the face. »*
- *« Fewer and thinner green veins; keep the green mist only around the base. »*

---

### Image 2 — `art/icones/pirate_kraken.png` (Kraken, SECRET) — avec extras

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size and position in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Change the body material on the whole piece, remove the green mist, and add the extra parts described below.
BODY: glossy, wet-looking deep purple skin (#5B2D82) shifting to magenta (#B0306A) on the top of the head and the ears, very shiny. Three rows of round pale pink suckers (#F2B8D8), each with a slightly darker pink ring, run down the front of the neck. About fifteen small round glowing cyan spots (#4FFFE8) are scattered on the neck, the head and the base, never on the eyes; the glow stays on the piece.
EXTRAS (exception for this skin only): four thick, cute, rounded octopus tentacles in the same glossy purple-magenta material come out of the round base: two at the front, a little apart, curling forward and up toward the viewer, and two at the back on each side, curling up so they are visible from the front. Each tentacle is thick at the base and tapers to a curled tip, with pale pink suckers on its inner side. All tentacle tips stay inside the frame and never cover the eyes. Friendly and cute like a plush toy, not scary: no teeth, no beak, no ink, no water splashes, nothing floating on the background.
```

**Critères de réussite** : cheval violet-magenta luisant, ventouses sur l'encolure, taches cyan,
**4 tentacules** qui sortent du socle (2 devant, 2 derrière visibles), mignon ; cheval même taille.

**Correction fréquente** : *« Make the whole group a little smaller so all four tentacle tips
fit inside the frame. »*

---

### Image 3 — `art/icones/pirate_abysses.png` (Abysses, SECRET) — avec extras

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Remove the four tentacles completely, change the body material on the whole piece, and add the extra part described below.
BODY: deep blue-black (#0A1430), very glossy, covered with thin glowing bioluminescent lines in electric blue (#3D9BFF) and magenta (#E04BD8) forming soft jellyfish-like curves and rings, plus about thirty tiny glowing pale aqua dots (#BFFFF5) like plankton. The glow stays on the piece. The black eyes keep their bright white highlight and get a thin glowing pale blue rim so they stay readable on the dark body.
EXTRAS (exception for this skin only): a thin curved stalk, like the lure of an anglerfish, grows from the forehead between the ears and bends forward above the muzzle; at its tip hangs a round glowing lantern bulb in pale yellow (#FFF1A8), the brightest point of the image, casting a soft warm light on the muzzle. No fish, no bubbles, no tentacles, no teeth, nothing floating on the background.
```

**Critères de réussite** : cheval presque noir bleuté à lignes bleu/magenta lumineuses ; lanterne
jaune pâle au bout d'une tige qui part du front ; yeux lisibles ; **plus de tentacules**.

**Correction fréquente** : *« Make the eyes more readable: add a thin glowing pale blue rim
around each eye. »*

---

### Image 4 — `art/icones/pirate_tempete.png` (Tempête, Légendaire)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: remove the stalk and the lantern; nothing is added to its shape.
BODY: stormy grey-blue (#4A5470), satin finish, marbled with soft lighter cloud shapes (#7A86A8) painted flat. One bright jagged zigzag lightning bolt in glowing white-blue (#CFE0FF) is painted on the surface, running from near one ear down the neck to the base. The glow stays on the piece. No rain, no clouds around the piece, no sparks on the background.
```

**Critères de réussite** : gris orage marbré, **un** éclair net qui zigzague sur le corps ; plus de
lanterne.

---

### Image 5 — `art/icones/pirate_lagon.png` (Lagon, Légendaire)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: glossy tropical water: a smooth vertical gradient from bright turquoise (#2BC4C4) on the head and ears to deep blue (#1F5FA8) on the base, covered with a soft network of light caustic reflections (#E0FFFF), like sunlight dancing on the bottom of a lagoon, faintly glowing. Four or five small round bubbles sit directly on the surface of the base and the lower neck, slightly raised. No lightning, no fish, no bubbles floating on the background.
```

---

### Image 6 — `art/icones/pirate_mariniere.png` (Marinière, Super rare)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: sailor stripes: 10 horizontal bands of equal height from the bottom of the base to the tips of the ears, alternating navy blue (#1F3A6E) and off-white (#F4EEE6), satin finish. The stripes are perfectly level, as if the piece had been sliced by horizontal planes: they stay horizontal across the base, the neck, the muzzle, the head and the mane; not spiral, not diagonal. Flat painted stripes, no relief, no bubbles, no caustics. The black eyes and nostrils sit on top of the stripes.
```

**Critères de réussite** : rayures **horizontales**, plus fines que le Sucre d'orge (10 bandes),
bleu marine / blanc cassé ; socle rayé aussi.

**Correction fréquente** : *« Make the stripes perfectly horizontal and level everywhere, even on
the muzzle: 10 bands in total. »*

---

### Image 7 — `art/icones/pirate_bois_flotte.png` (Bois flotté, Super rare)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: pale weathered driftwood, grey-beige (#B9AE9C), matte, with long smooth stylized grain lines in a darker grey-brown (#8C8170) painted flat and flowing vertically from the base up the neck to the ears, and five or six tiny pink seashells (#F2B8A8) painted flat here and there. For this skin the stylized grain is wanted, but it stays smooth, clean and flat like a painted toy: no cracks, no splinters, no holes, no realistic wood, no stripes.
```

---

### Image 8 — `art/icones/pirate_corail.png` (Corail, Super rare)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: coral pink (#FF7A6B), satin finish, covered with a pattern of small rounded lighter pores (#FFC2B0), like a stylized honeycomb of coral polyps, painted flat with a very slight irregularity. No branches growing out, no seaweed, no shells, no grain.
```

---

### Image 9 — `art/icones/pirate_carte.png` (Carte au trésor, Super rare)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: old treasure map parchment, warm pale yellow (#E9D3A0), matte, slightly toasted to a darker golden brown (#C9A66B) near the bottom of the base and the tips of the ears. Painted flat on it: a red dotted path (#C0392B) winding around the neck and the base, two or three small red X marks, and one small simple compass rose (a plain four-pointed star in a thin circle) on the side of the neck. Absolutely no letters, no N S E W, no words, no numbers, no skull, no torn edges.
```

**Critères de réussite** : parchemin jaune, chemin en pointillés rouge, croix « X », petite rose
des vents **sans lettre**.

**Correction fréquente** : *« Remove all letters from the compass rose and the map: only shapes. »*

---

### Image 10 — `art/icones/pirate_vert_de_gris.png` (Vert-de-gris, Super rare)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: patinated copper, like an old ship figurehead: a shiny metallic copper base color (#B0703A) with soft rounded patches of matte turquoise verdigris (#5FB8A0) covering about half of the surface, more on the base and in the hollows of the mane; the polished copper shines between the patches. Clean rounded patches, no rust, no drips, no scratches.
```

---

### Image 11 — `art/icones/pirate_ecailles.png` (Écailles, Super rare)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: rounded fish scales in staggered rows all over the piece, painted flat (no relief), each scale with a thin lighter edge, in a smooth vertical gradient from turquoise (#2BB8C4) on the base to bright blue (#3B5BD9) on the head and ears; glossy with a slight metallic sheen like a fish. No fins, no tail, no patina.
```

---

### Image 12 — `art/icones/pirate_fantome.png` (Fantôme, Légendaire) — **fond bleu-gris**

- **Format** : carré 1:1, **fond uni `#5B6380`** (translucide très clair). Toujours en dernier.

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle and same lighting. Only change the body material, on the whole piece, AND the background color. The piece has no extra parts: nothing is added to its shape.
BODY: translucent pale sea-green ghost material (#9AF5E0), about half transparent, softly glowing from inside, with one wide soft horizontal band of brighter light across the middle of the neck. Crisp bright reflections along the edges of the base, the muzzle, the ears and the mane outline the shape. The black eyes and nostrils look like they float inside. Friendly, not scary: no sheet, no chains, no wisps, no glow on the background.
BACKGROUND: plain flat solid medium blue-grey (#5B6380), no gradient, no floor, no scenery, no cast shadow on the background.
```

**Critères de réussite** : vert d'eau translucide et lumineux, contour bien lisible sur le
bleu-gris, yeux visibles.

---

### Prompt de relance (conversation P1)
Nouvelle conversation, coller le prompt complet de l'**image 1** en remplaçant sa dernière ligne
`BODY: …` par la ligne `BODY: …` de l'image voulue (et ses lignes `EXTRAS: …` /
`BACKGROUND: …` si elle en a). Si on peut joindre une image : joindre `pirate_or_maudit.png`
validée et remplacer la première phrase par *« Use the attached image as the exact reference:
same horse, same framing, same angle, same lighting and same background. Only the body material
changes. »*

---

## 3. Conversation P2 — Accessoires Pirates (12 images)

### Règles communes au groupe
- Même style, angle, lumière et fond que les chapeaux du lot 1 (`chapeau_casquette.png`).
- L'objet **seul** ; anneau vide pour ce qui entoure la tête ou le cou (bandana, cache-œil,
  foulard, boussole, longue-vue, ancre).
- Pirates de dessin animé : **pas de crâne, pas d'épée, pas de canon visible**.
- Navire fantôme en dernier (fond bleu-gris).

---

### Image 13 — `art/icones/kraken_bebe.png` (Petit Kraken, SECRET)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **À faire** : nouvelle conversation, coller :

```
Create this image from scratch (no reference image is attached). It belongs to an existing set of game shop icons, so follow every instruction below exactly.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

ACCESSORY RULES: this is a cosmetic accessory for a game character, shown alone and never worn: no horse, no head, no mannequin, no stand. Parts that go around a head or a neck form an empty loop, as if around an invisible head or neck. Built in three levels: one big readable main shape, a few secondary parts in a contrasting material, and a few small finishing details. The icon never shows rarity: no frame, no halo, no stars or sparkles around the object; any glow or glint stays on the object itself. Cute and friendly for all ages: no skull, no weapon, nothing scary.

OBJECT: a cute chunky baby kraken toy, sitting upright: a round plump head-body in glossy violet (#7A3FC0), two big shiny black eyes with large white highlights, soft round pink cheeks (#FF9FC8), a small happy closed smile, and a few small glowing cyan spots (#4FFFE8) on its forehead. Six short thick rounded tentacles with pale pink suckers: two curl outward on each side forming open loops (as if wrapped around two invisible ears), two hang down in front, and two are raised above its head, holding up one thick shiny gold coin (#FFC83A) like a trophy. The coin shows our game symbol in relief: a small round tower top with a little five-pointed star above it; no number, no currency sign. Plush-toy cute, not scary: no teeth, no beak, no ink cloud, nothing floating on the background.
```

**Critères de réussite** : bébé kraken rond et mignon, 6 tentacules (2 en boucle sur les côtés,
2 devant, 2 qui lèvent la pièce) ; pièce « tour + étoile » sans chiffre ; fond lavande uni.

**Corrections fréquentes**
- *« Make it rounder and cuter, like a plush toy, with bigger eyes. »*
- *« The coin must show only a small tower top and a star: no number, no sign. »*

---

### Image 14 — `art/icones/chapeau_capitaine.png` (Capitaine, Légendaire)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a big chunky cartoon captain's bicorne hat (two-cornered, wider than tall, worn crosswise), in dark violet-black felt (#2A1F3D) with a satin sheen, its whole edge trimmed with a thick polished gold braid (#E8B830). On the front, a big raised golden anchor emblem that glows softly. A big fluffy off-white plume (#F4EEE6) rises from the top in a generous panache. Three small round ice-blue gems (#9FE3FF) are set in the gold trim. No skull, no crossbones, no feathers floating, no text.
```

**Critères de réussite** : grand bicorne sombre bordé d'or, **ancre dorée** devant, gros panache
blanc ; bord bien dessiné (pas un trou noir).

**Correction fréquente** : *« Add a stronger lavender rim light on the dark felt so the hat's edge
stays readable. »*

---

### Image 15 — `art/icones/loup_corsaire.png` (Loup, Légendaire)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a chunky cartoon masquerade eye mask, alone, not worn, its front facing the viewer at the same three-quarter angle: a thick curved band shaped like a domino mask, bending around an invisible rounded face, in soft violet velvet (#4B2E83), with a thick polished gold rim (#E8B830) all around its outer edge and around each of the two large oval eye holes. Through the eye holes the plain background is visible. Two small round glossy ruby-red gems (#D62839) at the outer corners. Two long violet ribbons flow back from the sides in soft curves. No emblem, no letter, no face, no eyes inside the holes. It must not look like the mask of any existing hero.
```

**Critères de réussite** : loup velours violet bordé d'or, **deux trous vides**, pierres rouges
aux coins, rubans qui flottent.

---

### Image 16 — `art/icones/perroquet.png` (Perroquet, Légendaire)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object: nothing from the previous object remains.
OBJECT: a cute chunky toy macaw parrot, round and plump, standing as if perched on an invisible shoulder, its feet tucked under it: bright red body (#D64545), wings in blue (#3B7DD8) with yellow (#FFD54A) feathers along the edges, folded against its sides, a short tail of three thick rounded feathers in red and blue. A big rounded cream beak (#F4E6CC), two big shiny black eyes with white highlights and a pale cream ring around each, friendly expression, its head turned slightly toward the viewer. Thick rounded feathers, no thin details. No perch, no branch, no pirate hat, no text.
```

---

### Image 17 — `art/icones/ancre_doree.png` (Ancre, Légendaire)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object: nothing from the previous object remains.
OBJECT: a big chunky cartoon ship anchor in polished shiny gold (#E8B830), metallic, with thick rounded arms ending in rounded flukes, a thick crossbar near the top and a ring at the top; a soft bright gleam runs along its edge. Attached to the ring, a chunky chain of big rounded links in the same gold forms a wide open loop above the anchor, like a shoulder strap around an invisible body. No rope, no rust, no text.
```

---

### Image 18 — `art/icones/tricorne.png` (Tricorne, Super rare)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a chunky cartoon three-cornered pirate hat (tricorne), its brim folded up into three rounded points, in deep night-blue felt (#1F2A50) with a soft matte finish, the whole edge of the brim trimmed with a thick shiny gold braid (#E8B830). A generous fluffy red feather (#D64545) is tucked into the brim on the left side and curls backward. No skull, no crossbones, no emblem, no text.
```

**Critères de réussite** : tricorne bleu nuit à galon doré, grosse plume rouge à gauche ; bord
lisible.

---

### Image 19 — `art/icones/bandana_pirate.png` (Bandana, Super rare)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a chunky cartoon pirate bandana, shown alone as if tied around an invisible head: a soft rounded fabric cap in bright red (#D64545) with big round white polka dots (#F4EEE6), satin finish, with a thick knot at the back on the right side and two short fabric tails hanging down from the knot, slightly curved. No skull, no emblem, no text.
```

---

### Image 20 — `art/icones/cache_oeil.png` (Cache-œil, Super rare)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object: nothing from the previous object remains.
OBJECT: a big chunky cartoon eye patch, shown alone: a thick rounded oval patch of soft brown leather (#5A3A28) with a visible stitched border, a small raised golden five-pointed star (#E8B830) sewn in its center, and a thin dark brown cord attached on both sides that forms an empty loop as if around an invisible head. The patch is large and clearly the main shape. No skull, no crossbones, no eye, no text.
```

---

### Image 21 — `art/icones/foulard_marin.png` (Foulard, Super rare)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a chunky cartoon sailor neckerchief shown as an empty ring, as if around an invisible neck: soft rounded navy blue fabric (#1F3A6E) with a thin off-white edge trim (#F4EEE6), satin finish, tied at the front with a big chunky knot, and one triangular point of fabric hanging down from the knot over where the chest would be. No anchor, no emblem, no text.
```

**Critères de réussite** : même principe que `echarpe.png` (anneau vide), gros nœud devant et
une pointe qui pend.

---

### Image 22 — `art/icones/boussole.png` (Boussole, Super rare)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object: nothing from the previous object remains.
OBJECT: a big chunky cartoon compass medallion in polished gold (#E8B830), round and thick, with a domed glass cover, a cream dial (#F4E6CC) with four simple small triangular marks around the edge, and a bold red needle (#D64545) in the center. A thin gold chain attached to the top ring forms an empty loop above it, like a necklace around an invisible neck. No letters, no N S E W, no numbers, no text on the dial.
```

**Correction fréquente** : *« Remove all letters and numbers from the dial: only four small
triangle marks and the red needle. »*

---

### Image 23 — `art/icones/longue_vue.png` (Longue-vue, Super rare)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a chunky cartoon pirate spyglass lying diagonally, made of three nested tubes of polished brass (#C99A1E), each a little thinner than the previous one, with two soft brown leather grip bands (#5A3A28) and a thick rim at the wide end showing a glossy pale blue lens. A brown leather strap attached at both ends forms a wide open loop behind it, like a shoulder strap across an invisible body. No text, no engravings.
```

---

### Image 24 — `art/icones/navire_fantome.png` (Navire fantôme, SECRET) — **fond bleu-gris**

- **Format** : carré 1:1, **fond uni `#5B6380`** (translucide très clair). Toujours en dernier.

```
Same image as the previous one: same style, same angle and same lighting; the object fills about 70 percent of the image width. Only change the object AND the background color: nothing from the previous object remains.
OBJECT: a small chunky cartoon toy galleon, cute and rounded, made of translucent pale sea-green ghost material (#9AF5E0), about half transparent, softly glowing from inside, with crisp bright reflections along its edges. Three puffed rounded sails with gently frayed, wavy edges (no holes, no skull, no flag), a curved hull with three small round portholes on the side (no cannons), and a little warm yellow lantern (#FFF1A8) glowing at the stern. The ship rides on a small curved swirl of soft translucent mist under its hull, attached to it. Friendly and magical, not scary: no crew, no skeleton, nothing floating on the background.
BACKGROUND: plain flat solid medium blue-grey (#5B6380), no gradient, no floor, no scenery, no cast shadow on the background.
```

**Critères de réussite** : petit galion translucide vert d'eau, voiles gonflées un peu
effilochées, lanterne jaune ; **ni crâne ni canon** ; contour lisible sur le bleu-gris.

---

### Prompt de relance (conversation P2)
Nouvelle conversation, coller le prompt complet de l'**image 13** en remplaçant sa dernière ligne
`OBJECT: …` par la ligne `OBJECT: …` de l'image voulue (plus sa ligne `BACKGROUND: …` pour le
Navire fantôme, et dans ce cas remplacer aussi la dernière phrase du bloc ICON FORMAT par celle de
la ligne `BACKGROUND`).

---

## 4. Conversation A1 — Skins Animaux (16 images)

### Règles communes au groupe
- Même cheval que P1 et que le lot 3. **Le cheval reste un cheval** : on lui peint le pelage d'un
  animal, on ne lui ajoute ni museau de chat, ni cornes, ni queue, ni pattes (sauf les extras du
  Lion et du Paon).
- Première image : **Caméléon** (référence du cheval, voir §0 point 5), puis Lion solaire et Paon,
  puis les autres du plus rare au plus commun ; les 4 images sur fond bleu-gris à la fin.

---

### Image 25 — `art/icones/animal_cameleon.png` (Caméléon, Super rare)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **À faire** : nouvelle conversation, coller :

```
Create this image from scratch (no reference image is attached). It belongs to an existing set of game shop icons, so follow every instruction below exactly.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single character, alone, perfectly centered, the whole piece visible from the bottom of its round base to the tips of its ears, filling about 75 percent of the image height with equal margins, nothing cropped. Three-quarter front view from slightly above (about 25 degrees), the piece turned about 20 degrees to the left, its face and muzzle toward the viewer so that both eyes are clearly visible. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a cute toy chess knight piece, like a chunky painted wooden board-game piece that has come to life. From bottom to top: a round flat pawn base with a stepped collar (two stacked rounded rings with beveled edges); a thick, round, smooth neck rising from the base, bulging forward at the chest and slightly thinner under the jaw; a horse head shaped like a softly rounded box, with a long, soft, rounded muzzle that sticks out toward the viewer and tilts slightly downward, for a gentle, friendly look; two small dark oval nostrils (#111111) side by side at the tip of the muzzle; two big glossy black oval eyes (#111111), slightly taller than wide, high on the front of the head just above the base of the muzzle, half sunk into the head, each with a small white highlight in the upper left, both eyes clearly visible; two short, thick, pointed ears on top of the head; a sculpted mane made of five or six chunky rounded tufts, starting with a small forelock tuft on the forehead between the ears and running down the back of the neck. The piece is about twice as tall as its base is wide. The whole piece (base, neck, head, muzzle, ears and mane) is made of ONE single material and color; only the eyes and nostrils are black. No mouth, no teeth, no legs, no tail, no saddle, no bridle, no rider, no horn, no hat, no accessories. Smooth surfaces with soft bevels, low-poly toy look, not a realistic horse.

SKIN RULES: the piece wears the coat or skin of an animal, but it stays exactly the same chess knight: no animal ears, no snout, no whiskers, no tail, no legs added. Any pattern or marking is a flat painted design on the surface, like a clean toy decal, never a photographic texture. Any glow stays on the piece itself. The icon never shows rarity: no frame, no halo, no light rays, no sparkles on the background.

BODY: chameleon skin covered with small round flat painted scales, in large soft patches of color that blend into each other: mint green (#4CC26A) on the base, turquoise (#2BB8C4) on the neck, sunny yellow (#FFD54A) on the head and tangerine orange (#F28C38) on the ears and the tips of the mane; smooth blends between the patches, satin finish. No lizard eyes, no curled tail, no crest.
```

**Critères de réussite** : notre cheval (socle de pion, museau qui avance, crinière en grosses
mèches) ; petites écailles rondes ; 4 couleurs en grandes plaques ; deux yeux noirs visibles.

**Corrections fréquentes**
- *« Turn the horse a bit more toward the viewer: both eyes must be clearly visible. »*
- *« Show the whole piece including the round base: nothing cropped, a bit smaller in the frame. »*
- *« Keep our chess knight shape: no lizard features at all, only the skin pattern. »*

---

### Image 26 — `art/icones/animal_lion_soleil.png` (Lion solaire, SECRET) — avec extras

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and black eyes and nostrils, same angle, same lighting and same plain light lavender background (#E6E1EE); the whole group (piece, mane and sun disc) fills about 80 percent of the image height, nothing cropped. Change the body material on the whole piece and add the extra parts described below.
BODY: warm glowing gold (#FFB830) shifting to bright orange (#FF7A1C) on the base, slightly metallic and glossy, with faint wavy lighter streaks (#FFE58A) rising up the body like heat shimmer, softly glowing.
EXTRAS (exception for this skin only): a mane of stylized flames replaces the sculpted mane: about fourteen chunky, rounded, teardrop-shaped flame tufts with pointed tips, in two rings around the head and down the back of the neck, glowing pale yellow (#FFF1A8) at their base, orange (#FF7A1C) in the middle and red (#E0303A) at their tips; toy-like solid flames, not real fire. Behind the head and neck, a large flat sun disc with twelve rounded rays, pale yellow and half transparent, softly glowing, entirely behind the piece and inside the frame. The flames never cover the eyes. Still our cute chess knight, not a real lion face: no lion muzzle, no teeth, no paws. No sparks or embers floating on the background.
```

**Critères de réussite** : cheval or-orange lumineux, **crinière de flammes** en 2 couronnes,
disque solaire à 12 rayons derrière la tête, yeux visibles ; tout tient dans le cadre.

**Correction fréquente** : *« Make the flames chunkier and rounder like toy flames, and keep the
whole sun disc inside the frame. »*

---

### Image 27 — `art/icones/animal_paon.png` (Paon, Légendaire) — avec extras

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and black eyes and nostrils, same angle, same lighting and same plain light lavender background (#E6E1EE); the whole group fills about 80 percent of the image height, nothing cropped. Remove the flame mane and the sun disc completely (the normal sculpted mane is back), change the body material on the whole piece and add the extra part described below.
BODY: peacock blue (#1E6EB0) shifting to turquoise (#1E9EB0) on the neck, glossy with a soft iridescent sheen and a slight metallic shine, covered with small flat painted feather scales whose edges glow faintly.
EXTRAS (exception for this skin only): behind the piece, a half-open peacock tail fan of thirteen long rounded feathers, raised like a small fan behind the base and neck and showing a little on each side; each feather ends with a round eye spot in green (#2E9E5B), blue and gold (#FFC83A), softly glowing. Chunky rounded toy feathers, not thin realistic ones; the fan stays entirely behind the piece and inside the frame. No beak, no bird head, no crest, nothing floating on the background.
```

**Critères de réussite** : cheval bleu paon irisé, **éventail** de plumes à ocelles derrière ;
plus de flammes ni de disque.

---

### Image 28 — `art/icones/animal_flamant.png` (Flamant, Rare)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same angle, same lighting and same plain light lavender background (#E6E1EE); the piece fills about 75 percent of the image height. Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: remove the feather fan completely; nothing is added to its shape.
BODY: flamingo pink (#FF8FB0) shifting to deep pink (#E0507A) on the base, satin finish, with a light pattern of elongated lighter feather scales painted flat. No beak, no wings, no legs.
```

---

### Image 29 — `art/icones/animal_chat_roux.png` (Chat roux, Rare)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: ginger tabby cat coat: warm ginger orange (#E8873A) with darker orange tabby stripes (#B8602A) painted flat, curved around the neck and the head; the muzzle and the base are cream (#F4E6CC). Soft velvety matte look, like a flocked toy, with only a very fine fur grain. No whiskers, no cat ears, no cat nose.
```

---

### Image 30 — `art/icones/animal_tigre.png` (Tigre, Peu commun)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: tiger coat: bright orange (#F28C38) with bold dark stripes (#1F1730) that taper to points, painted flat and curving around the neck and the head; the muzzle, the front of the chest and the lower base are off-white (#F4EEE6). Matte. No fur grain, no whiskers.
```

**Critères de réussite** : à distinguer du Chat roux : orange **vif** à rayures **noires en
pointe**, museau blanc (le chat roux a des rayures orange foncé et un aspect velours).

---

### Image 31 — `art/icones/animal_girafe.png` (Girafe, Peu commun)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: giraffe coat: rounded polygon patches of warm brown (#8C5933) separated by a network of light sand-yellow lines (#E8B860), like a giraffe pattern, painted flat, smaller patches on the head; matte. No horns, no stripes.
```

---

### Image 32 — `art/icones/animal_leopard.png` (Léopard, Peu commun)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: leopard coat: golden beige (#E0B070) covered with rosettes, each a broken ring of dark brown (#4A2E1C) around an ochre center (#C08840), painted flat, evenly spread, smaller on the head; slightly satin. Not giraffe patches, no stripes.
```

---

### Image 33 — `art/icones/animal_grenouille.png` (Grenouille, Peu commun)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: frog skin: bright green (#4CC26A), very glossy and wet-looking with crisp highlights, shifting smoothly to pale yellow (#E8F0A0) on the lower base, with a few small rounded darker green spots (#2E7A40) painted flat on the neck and head. No frog eyes, no webbed feet.
```

---

### Image 34 — `art/icones/animal_abeille.png` (Abeille, Commun)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: bee stripes: 6 wide horizontal bands of equal height from the bottom of the base to the tips of the ears, alternating golden yellow (#FFC83A) and very dark brown (#2A1F1A), perfectly level like the piece was sliced by horizontal planes, soft velvety satin finish. The eyes keep their bright white highlight so they stay readable even on a dark band. No wings, no antennae, no stinger.
```

---

### Image 35 — `art/icones/animal_coccinelle.png` (Coccinelle, Commun)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: ladybug: glossy bright red (#D62839) with exactly seven big round dark dots (#1F1730) painted flat: one on the forehead between the eyes and the forelock, two on the neck, two on the chest and two on the base; not a repeating pattern, the dots never touch the eyes. No antennae, no wings, no stripes.
```

---

### Image 36 — `art/icones/animal_zebre.png` (Zèbre, Commun)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: zebra coat: wavy stripes in dark (#1F1730) and off-white (#F4EEE6), painted flat, following the shape of the piece: they curve around the neck and become narrower and closer together on the head and muzzle; matte. The dark stripes run over the edges so the outline of the piece stays readable. No dots, no stripes on the eyes.
```

**Correction fréquente** (si le bord blanc se perd dans le fond) : refaire l'image à la fin, en
ajoutant la ligne `BACKGROUND: …` bleu-gris de l'image 37.

---

### Image 37 — `art/icones/animal_meduse.png` (Méduse, Super rare) — **fond bleu-gris**

- **Format** : carré 1:1, **fond uni `#5B6380`** (translucide clair). Les 4 dernières images de
  A1 gardent ce fond.

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle and same lighting. Only change the body material, on the whole piece, AND the background color. The piece has no extra parts: nothing is added to its shape.
BODY: translucent pink jelly (#FFB0E0), about half transparent, like a jellyfish, with a softly lighter frilly rim pattern painted around the top of the head and the ears and a gentle light inside; very smooth and glossy, crisp bright reflections along the edges of the base, the muzzle, the ears and the mane outline the shape. The black eyes and nostrils look like they float inside. No tentacles, no glow on the background.
BACKGROUND: plain flat solid medium blue-grey (#5B6380), no gradient, no floor, no scenery, no cast shadow on the background.
```

---

### Image 38 — `art/icones/animal_panda.png` (Panda, Rare) — fond bleu-gris

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain medium blue-grey background (#5B6380). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: panda coat, opaque and matte: mostly off-white (#F4EEE6), with dark zones (#1F1730) painted flat: both ears entirely dark, a big teardrop-shaped dark patch around each eye, and the lower neck and the base dark. The eyes sit inside the dark patches and keep their bright white highlight plus a thin off-white rim, so they stay clearly visible. No bamboo, no panda ears added.
```

**Critères de réussite** : panda reconnaissable (oreilles noires, taches en larme autour des yeux,
bas noir) ; **les yeux se voient** dans les taches.

---

### Image 39 — `art/icones/animal_dalmatien.png` (Dalmatien, Commun) — fond bleu-gris

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain medium blue-grey background (#5B6380). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: dalmatian coat, matte: off-white (#F4EEE6) with round dark spots (#1F1730) of varied sizes, from small to medium, painted flat and scattered irregularly; one ear entirely dark. No patches around the eyes, no dog ears added.
```

---

### Image 40 — `art/icones/animal_vache.png` (Vache, Commun) — fond bleu-gris

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain medium blue-grey background (#5B6380). Only change the body material, on the whole piece (base, neck, head, ears and mane). The piece has no extra parts: nothing is added to its shape.
BODY: cow coat, matte: off-white (#F4EEE6) with a few big irregular brown patches (#6B4A35) with soft rounded wavy edges, painted flat, and the round base in pale pink (#F2B8C0) like a cow's muzzle. No horns, no bell, no small round spots.
```

**Critères de réussite** : à distinguer du Dalmatien : **grandes** taches **brunes** irrégulières
et socle rose (le dalmatien a de petits points ronds noirs).

---

### Prompt de relance (conversation A1)
Nouvelle conversation, coller le prompt complet de l'**image 25** en remplaçant sa dernière ligne
`BODY: …` par la ligne `BODY: …` de l'image voulue (plus ses lignes `EXTRAS: …` /
`BACKGROUND: …`). Si on peut joindre une image : joindre `animal_cameleon.png` (ou
`couleur_bleu.png`) et remplacer la première phrase comme pour P1.

---

## 5. Conversation A2 — Accessoires Animaux (16 images)

### Règles communes au groupe
- Même style que P2 et les chapeaux du lot 1. L'objet seul, anneau vide pour ce qui entoure la
  tête ou le cou (serre-têtes, collier, écharpe renard).
- Animaux **mignons de jouet** : gros yeux brillants, formes rondes, rien de réaliste.
- Les 3 images sur fond bleu-gris (Bocal, Oreilles de lapin, Moustaches) à la fin.

---

### Image 41 — `art/icones/baleine_celeste.png` (Baleine céleste, SECRET)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **À faire** : nouvelle conversation, coller :

```
Create this image from scratch (no reference image is attached). It belongs to an existing set of game shop icons, so follow every instruction below exactly.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image width with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

ACCESSORY RULES: this is a cosmetic accessory for a game character, shown alone and never worn: no horse, no head, no mannequin, no stand. Parts that go around a head or a neck form an empty loop, as if around an invisible head or neck. Built in three levels: one big readable main shape, a few secondary parts in a contrasting material, and a few small finishing details. The icon never shows rarity: no frame, no halo, no stars or sparkles around the object; any glow or glint stays on the object itself. Cute and friendly for all ages: no skull, no weapon, nothing scary.

OBJECT: a cute chunky toy whale swimming through the air, its body gently curved with the tail raised: a smooth rounded body, dark navy blue on the back (#2E3A8A), a pale blue belly with soft rounded grooves (#BFD4FF), wide rounded flippers, a big gentle shiny black eye with a white highlight and a tiny happy smile. Its back is sprinkled with small glowing warm star dots (#FFF1D6), a few of them with a tiny four-pointed twinkle, like a starry sky; the glow stays on the whale. No water, no splash, no star trail, nothing floating on the background.
```

**Critères de réussite** : baleine ronde et douce, dos bleu nuit étoilé, ventre clair rayé, gros
œil gentil ; fond lavande uni sans traînée.

**Corrections fréquentes**
- *« Make it rounder and cuter, like a plush toy whale. »*
- *« Remove the stars around the whale: the stars are only on its back. »*

---

### Image 42 — `art/icones/ruche.png` (Abeilles, Super rare)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the group fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a compact group: a small chunky round honey pot in glossy cream ceramic (#F4E6CC) with a brown band (#8C5933), filled to the brim with shiny golden honey (#FFB830) dripping a little over the rim, and three cute round bees flying close around it at different heights, almost touching it: each bee is a small ball with yellow (#FFC83A) and dark brown (#2A1F1A) stripes, two shiny black eyes and two small translucent rounded wings. No hive, no text, no label on the pot.
```

**Critères de réussite** : petit pot de miel + **3 abeilles** rondes serrées autour (groupe
compact, détourable d'un seul tenant).

---

### Image 43 — `art/icones/chaton_sac.png` (Chaton, Super rare)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object: nothing from the previous object remains.
OBJECT: a chunky round wicker basket worn as a backpack, in warm light wicker (#C99A6A) with a chunky stylized weave in big smooth braids, two soft brown shoulder straps (#6B4226) forming loops at the back. A cute grey kitten (#9AA0B0) pokes its head and both front paws out over the rim on the right side: round head, two pointed ears with pink insides, big shiny eyes with white highlights, tiny pink nose. Plush-toy cute. No text, no blanket, nothing floating on the background.
```

---

### Image 44 — `art/icones/ailes_chauve_souris.png` (Ailes de chauve-souris, Rare)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a pair of cute chunky cartoon bat wings spread open, joined at a small rounded center piece: dark violet frame (#3A2858) with thick visible finger ribs, and lighter violet membranes (#6A4A98) between them, with scalloped edges that end in soft rounded points; satin finish with a lavender rim light on the edges. Cute, not scary: no bat body, no fangs, no claws.
```

---

### Image 45 — `art/icones/bois_cerf.png` (Bois de cerf, Rare)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object: nothing from the previous object remains.
OBJECT: a pair of chunky cartoon deer antlers standing side by side, as if on an invisible head, mirror images of each other, in light warm brown (#B89060), each with four thick rounded tines. Three small pink flowers (#FF9FC8) with yellow centers and one small green leaf (#4CC26A) are tucked into the antlers. Smooth, rounded, toy-like, no sharp points. No head, no headband, no text.
```

---

### Image 46 — `art/icones/poussin.png` (Poussin, Rare)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object: nothing from the previous object remains.
OBJECT: a cute chunky toy baby chick, perfectly round like a ball, soft pastel yellow (#FFE070), sitting, with a small rounded orange beak (#F28C38), two big shiny black eyes with white highlights, two tiny stubby wings and two tiny orange feet. On top of its head sits the top half of a broken eggshell in off-white (#F4EEE6) with a zigzag edge, like a little hat. No nest, no text.
```

---

### Image 47 — `art/icones/bonnet_grenouille.png` (Grenouille, Peu commun)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a chunky cartoon frog beanie hat in soft matte green fabric (#4CC26A), with a slightly darker folded brim (#3A9A55). On top, two big round frog eyes in relief: glossy white balls with big black pupils and white highlights. A simple smiling mouth is stitched in dark green along the front of the brim. Soft, rounded, cute. No pompom, no text.
```

---

### Image 48 — `art/icones/lunettes_hibou.png` (Lunettes hibou, Peu commun)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a pair of big chunky cartoon round glasses, alone, front facing the viewer at the same three-quarter angle: a thick warm brown frame (#8C5933) with short temple arms, two large round lenses tinted amber (#F2B84A), opaque, each showing one big cute glossy black cartoon eye with a white highlight painted on the glass. Above each lens, a fluffy eyebrow made of three chunky rounded orange feathers (#F28C38). No owl body, no beak, no text.
```

---

### Image 49 — `art/icones/echarpe_renard.png` (Écharpe renard, Peu commun)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a chunky cartoon fox-shaped scarf forming an empty ring, as if wrapped around an invisible neck: a soft rounded fox body in orange (#E8732A) with a soft matte fabric look, a cute sleeping fox head resting at the front (closed curved eyes, cream muzzle #F4E6CC, small dark nose, pointed ears), and a fluffy tail with a white tip (#F4EEE6) hanging down at the front. Plush-toy cute, no realistic fur, no legs, no text.
```

---

### Image 50 — `art/icones/carapace.png` (Carapace, Peu commun)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a chunky cartoon turtle shell worn as a backpack: a domed rounded shell in green (#5A8A3A), satin finish, made of raised hexagonal plates each bordered with a light yellow-green rim (#C8D86A), and two soft brown shoulder straps (#8C5933) forming loops on the inner side. No turtle, no head, no legs, no text.
```

---

### Image 51 — `art/icones/oreilles_chat.png` (Oreilles de chat, Commun)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a chunky cartoon headband shaped as an arc, as if on an invisible head, in dark charcoal violet (#2A2438) with a satin sheen and a lavender rim light on its edges, topped with two big rounded triangular cat ears in the same dark color, each with a soft pink inner ear (#FF9FC8). No face, no bow, no text.
```

---

### Image 52 — `art/icones/truffe.png` (Truffe, Commun)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 60 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a big chunky cartoon dog nose, alone: a rounded heart-like shape in very glossy dark charcoal (#2A2438), with two rounded nostrils and a soft groove in the middle, one bright white highlight in the upper left and a lavender rim light on its edges so its shape reads clearly. No face, no muzzle, no text.
```

---

### Image 53 — `art/icones/collier_medaille.png` (Collier à médaille, Commun)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a chunky cartoon pet collar shown as an empty ring, as if around an invisible neck: a thick rounded band in red (#D64545), satin finish, with a small shiny gold buckle (#E8B830), and at the front a shiny gold bone-shaped tag hanging from a small ring. Nothing engraved on the tag: no name, no letters, no numbers, no text.
```

---

### Image 54 — `art/icones/bocal_poisson.png` (Bocal, Légendaire) — **fond bleu-gris**

- **Format** : carré 1:1, **fond uni `#5B6380`** (verre transparent). Les 3 dernières images de
  A2 gardent ce fond.

```
Same image as the previous one: same style, same angle and same lighting; the object fills about 70 percent of the image height. Only change the object AND the background color: nothing from the previous object remains.
OBJECT: a chunky cartoon round glass fishbowl, clear and transparent, very glossy, with crisp bright white reflections on its curved glass and a thick rounded rim at the top opening. It is two thirds full of softly glowing light blue water (#8FD4F4), with a cute round orange goldfish (#FF7A1C) with a big eye and a flowing tail swimming inside, two small green water plants (#4CC26A) and a little bed of pale sand at the bottom; three small bubbles rise inside the water. No castle, no stand, nothing outside the bowl, no text.
BACKGROUND: plain flat solid medium blue-grey (#5B6380), no gradient, no floor, no scenery, no cast shadow on the background.
```

**Critères de réussite** : bocal rond transparent dont le **contour se lit** (reflets blancs),
poisson rouge, algues, sable ; bulles **dans** l'eau seulement.

---

### Image 55 — `art/icones/oreilles_lapin.png` (Oreilles de lapin, Commun) — fond bleu-gris

```
Same image as the previous one: same style, same angle, same lighting and same plain medium blue-grey background (#5B6380); the object fills about 70 percent of the image height. Only change the object: nothing from the previous object remains.
OBJECT: a chunky cartoon headband shaped as an arc, as if on an invisible head, in off-white (#F4EEE6) with a satin sheen, topped with two long rounded rabbit ears in the same off-white, each with a soft pink inner ear (#FF9FC8); the left ear stands straight up, the right ear is folded over at mid-height and flops down. No face, no bow, no text.
```

---

### Image 56 — `art/icones/moustaches.png` (Moustaches, Commun) — fond bleu-gris

```
Same image as the previous one: same style, same angle, same lighting and same plain medium blue-grey background (#5B6380); the group fills about 70 percent of the image width. Only change the object: nothing from the previous object remains.
OBJECT: a set of cute cartoon cat whiskers, as if on an invisible muzzle: a small rounded triangular pink nose (#FF9FC8) in the center, and six whiskers, three on each side, fanning out with a gentle curve, thick and rounded like soft toy sticks, in off-white (#F4EEE6) with a satin sheen. No face, no mouth, no text.
```

---

### Prompt de relance (conversation A2)
Nouvelle conversation, coller le prompt complet de l'**image 41** en remplaçant sa dernière ligne
`OBJECT: …` par la ligne `OBJECT: …` de l'image voulue (pour les images 54 à 56, remplacer aussi
la dernière phrase du bloc ICON FORMAT par la ligne `BACKGROUND: …` de l'image 54).

---

## 6. Conversation B — Images des 4 boîtes (4 images)

### Règles communes au groupe
- Aspect donné par `v5.md` §17.1 : deux **caisses pirates** et deux **paniers**. **Jamais la
  forme d'un coffre du jeu** (couvercle bombé, serrure en écusson) : on ne doit pas confondre une
  boîte de la boutique avec un coffre d'étage. D'où le **couvercle plat**.
- Même famille que `boite_skins_1.png` / `boite_accessoires_1.png` : objet trapu, biseaux
  marqués, un emblème en laiton sur la face avant, un mince filet de lumière chaude sous le
  couvercle (la lueur reste sur la boîte).
- Les deux boîtes d'une même famille sont **le même objet**, seules changent la couleur des
  bandes et l'emblème.
- **Noms de fichiers** : les identifiants du catalogue sont `boite_pirates_skins`,
  `boite_pirates_accessoires`, `boite_animaux_skins`, `boite_animaux_accessoires` (ordre
  « thème puis type », déjà utilisé dans `ShopCatalog.asset`).

---

### Image 57 — `art/icones/boite_pirates_skins.png` (Pirates : skins)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **À faire** : nouvelle conversation, coller :

```
Create this image from scratch (no reference image is attached). It belongs to an existing set of game shop icons, so follow every instruction below exactly.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image width with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

OBJECT: a chunky toy pirate cargo crate, closed, clearly NOT a treasure chest: a box slightly wider than tall with a FLAT lid (not domed) that overhangs a little, made of thick dark wooden planks (#6B4226) separated by shallow grooves (#4A2E1C), smooth and clean with soft rounded bevels, no wood grain. Two thick polished brass hoops (#C99A1E) wrap around the body, with brass corner caps on every corner. In the middle of the front face, a big chunky anchor emblem in raised brass. A round shiny golden padlock (#FFC83A) hangs from a clasp at the front of the lid. A thin line of soft warm light (#FFF1D6) leaks from under the lid; the glow stays on the crate. No skull, no crossbones, no stencil letters, no coins, no rope, no sparkles on the background.
```

**Critères de réussite** : caisse en bois sombre à **couvercle plat**, cerclée de laiton, ancre
en relief devant, cadenas doré ; on ne la confond pas avec un coffre du jeu.

**Correction fréquente** : *« The lid must be flat, like a cargo crate, not a rounded treasure
chest lid. »*

---

### Image 58 — `art/icones/boite_pirates_accessoires.png` (Pirates : accessoires)

```
Same image as the previous one: same crate shape and size, same dark wooden planks, same flat lid, same corner caps, same golden padlock, same thin line of light, same framing, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the bands and the emblem.
OBJECT: the two hoops around the body are now wide painted navy blue bands (#1F3A6E) with a thin brass edge, and the emblem on the front is now a chunky ship's steering wheel in raised brass (a ring with eight rounded handles). No anchor, no skull, no text.
```

**Critères de réussite** : la même caisse, bandes **bleu marine**, **gouvernail** en relief.

---

### Image 59 — `art/icones/boite_animaux_skins.png` (Animaux : skins)

```
Same image as the previous one: same style, same framing, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object: nothing from the crate remains.
OBJECT: a chunky toy picnic basket with a lid, closed: a rounded rectangular basket slightly wider than tall in warm honey wicker (#C99A6A), its weave shown as big smooth chunky braids (clean and toy-like, no fine texture), a darker woven rim (#8C5933) around the top and the bottom, a slightly domed matching wicker lid, and a sturdy rounded handle arching over the lid. On the front, a round brass medallion (#C99A1E) with a big raised paw print in pink (#FF9FC8): one large pad and four round toes. A thin line of soft warm light (#FFF1D6) leaks from under the lid; the glow stays on the basket. No animal, no fabric sticking out, no text.
```

**Critères de réussite** : panier en osier « jouet », couvercle, anse, **empreinte de patte** rose
sur médaillon laiton.

---

### Image 60 — `art/icones/boite_animaux_accessoires.png` (Animaux : accessoires)

```
Same image as the previous one: same basket shape and size, same chunky weave, same lid, same handle, same thin line of light, same framing, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the color, the decoration and the emblem.
OBJECT: the basket and its lid are now painted forest green (#2E6B4A) with a satin finish, the rim slightly darker green (#1F4A34). A big chunky bow in cream satin ribbon (#F4E6CC) is tied on the handle. On the front, the medallion now shows a chunky golden bone (#FFC83A) in relief instead of the paw print. No animal, no text.
```

**Critères de réussite** : le même panier en **vert sapin**, gros nœud, **os doré** sur le
médaillon.

---

### Prompt de relance (conversation B)
Nouvelle conversation, coller le prompt complet de l'**image 57** ; pour l'image 58, ajouter à la
fin sa ligne `OBJECT: …` ; pour les paniers, remplacer la ligne `OBJECT: …` de l'image 57 par celle
de l'image 59 (puis, à la suite, l'image 60 en prompt court).

---

## 7. Récapitulatif — ordre de génération

Priorité : **1** = Secret ou Légendaire, **2** = Super rare ou Rare, **3** = Peu commun ou Commun,
**B** = boîte (indispensable dès que la boutique v5 est affichée : la conversation B est courte
et indépendante, on peut la faire en premier).
Dans chaque conversation, l'ordre ci-dessous est l'ordre de génération (« ancre » = premier
prompt autonome).

| # | Conv. | Fichier (`art/icones/`) | Rareté | Fond | Priorité |
|---|---|---|---|---|---|
| 1 | P1 (ancre) | `pirate_or_maudit.png` | Légendaire | `#E6E1EE` | 1 |
| 2 | P1 | `pirate_kraken.png` | Secret | `#E6E1EE` | 1 |
| 3 | P1 | `pirate_abysses.png` | Secret | `#E6E1EE` | 1 |
| 4 | P1 | `pirate_tempete.png` | Légendaire | `#E6E1EE` | 1 |
| 5 | P1 | `pirate_lagon.png` | Légendaire | `#E6E1EE` | 1 |
| 6 | P1 | `pirate_mariniere.png` | Super rare | `#E6E1EE` | 2 |
| 7 | P1 | `pirate_bois_flotte.png` | Super rare | `#E6E1EE` | 2 |
| 8 | P1 | `pirate_corail.png` | Super rare | `#E6E1EE` | 2 |
| 9 | P1 | `pirate_carte.png` | Super rare | `#E6E1EE` | 2 |
| 10 | P1 | `pirate_vert_de_gris.png` | Super rare | `#E6E1EE` | 2 |
| 11 | P1 | `pirate_ecailles.png` | Super rare | `#E6E1EE` | 2 |
| 12 | P1 (dernier) | `pirate_fantome.png` | Légendaire | **`#5B6380`** | 1 |
| 13 | P2 (ancre) | `kraken_bebe.png` | Secret | `#E6E1EE` | 1 |
| 14 | P2 | `chapeau_capitaine.png` | Légendaire | `#E6E1EE` | 1 |
| 15 | P2 | `loup_corsaire.png` | Légendaire | `#E6E1EE` | 1 |
| 16 | P2 | `perroquet.png` | Légendaire | `#E6E1EE` | 1 |
| 17 | P2 | `ancre_doree.png` | Légendaire | `#E6E1EE` | 1 |
| 18 | P2 | `tricorne.png` | Super rare | `#E6E1EE` | 2 |
| 19 | P2 | `bandana_pirate.png` | Super rare | `#E6E1EE` | 2 |
| 20 | P2 | `cache_oeil.png` | Super rare | `#E6E1EE` | 2 |
| 21 | P2 | `foulard_marin.png` | Super rare | `#E6E1EE` | 2 |
| 22 | P2 | `boussole.png` | Super rare | `#E6E1EE` | 2 |
| 23 | P2 | `longue_vue.png` | Super rare | `#E6E1EE` | 2 |
| 24 | P2 (dernier) | `navire_fantome.png` | Secret | **`#5B6380`** | 1 |
| 25 | A1 (ancre) | `animal_cameleon.png` | Super rare | `#E6E1EE` | 2 |
| 26 | A1 | `animal_lion_soleil.png` | Secret | `#E6E1EE` | 1 |
| 27 | A1 | `animal_paon.png` | Légendaire | `#E6E1EE` | 1 |
| 28 | A1 | `animal_flamant.png` | Rare | `#E6E1EE` | 2 |
| 29 | A1 | `animal_chat_roux.png` | Rare | `#E6E1EE` | 2 |
| 30 | A1 | `animal_tigre.png` | Peu commun | `#E6E1EE` | 3 |
| 31 | A1 | `animal_girafe.png` | Peu commun | `#E6E1EE` | 3 |
| 32 | A1 | `animal_leopard.png` | Peu commun | `#E6E1EE` | 3 |
| 33 | A1 | `animal_grenouille.png` | Peu commun | `#E6E1EE` | 3 |
| 34 | A1 | `animal_abeille.png` | Commun | `#E6E1EE` | 3 |
| 35 | A1 | `animal_coccinelle.png` | Commun | `#E6E1EE` | 3 |
| 36 | A1 | `animal_zebre.png` | Commun | `#E6E1EE` | 3 |
| 37 | A1 | `animal_meduse.png` | Super rare | **`#5B6380`** | 2 |
| 38 | A1 | `animal_panda.png` | Rare | **`#5B6380`** | 2 |
| 39 | A1 | `animal_dalmatien.png` | Commun | **`#5B6380`** | 3 |
| 40 | A1 (dernier) | `animal_vache.png` | Commun | **`#5B6380`** | 3 |
| 41 | A2 (ancre) | `baleine_celeste.png` | Secret | `#E6E1EE` | 1 |
| 42 | A2 | `ruche.png` | Super rare | `#E6E1EE` | 2 |
| 43 | A2 | `chaton_sac.png` | Super rare | `#E6E1EE` | 2 |
| 44 | A2 | `ailes_chauve_souris.png` | Rare | `#E6E1EE` | 2 |
| 45 | A2 | `bois_cerf.png` | Rare | `#E6E1EE` | 2 |
| 46 | A2 | `poussin.png` | Rare | `#E6E1EE` | 2 |
| 47 | A2 | `bonnet_grenouille.png` | Peu commun | `#E6E1EE` | 3 |
| 48 | A2 | `lunettes_hibou.png` | Peu commun | `#E6E1EE` | 3 |
| 49 | A2 | `echarpe_renard.png` | Peu commun | `#E6E1EE` | 3 |
| 50 | A2 | `carapace.png` | Peu commun | `#E6E1EE` | 3 |
| 51 | A2 | `oreilles_chat.png` | Commun | `#E6E1EE` | 3 |
| 52 | A2 | `truffe.png` | Commun | `#E6E1EE` | 3 |
| 53 | A2 | `collier_medaille.png` | Commun | `#E6E1EE` | 3 |
| 54 | A2 | `bocal_poisson.png` | Légendaire | **`#5B6380`** | 1 |
| 55 | A2 | `oreilles_lapin.png` | Commun | **`#5B6380`** | 3 |
| 56 | A2 (dernier) | `moustaches.png` | Commun | **`#5B6380`** | 3 |
| 57 | B (ancre) | `boite_pirates_skins.png` | boîte | `#E6E1EE` | B |
| 58 | B | `boite_pirates_accessoires.png` | boîte | `#E6E1EE` | B |
| 59 | B | `boite_animaux_skins.png` | boîte | `#E6E1EE` | B |
| 60 | B | `boite_animaux_accessoires.png` | boîte | `#E6E1EE` | B |

**Total** : 60 images (28 icônes de skins, 28 icônes d'accessoires, 4 boîtes), dont 9 sur fond
bleu-gris `#5B6380`.

**Pas d'image** (fait par le code, art bible §10.3 et §14) : fumée, éclairs, bulles, poissons
lumineux, braises, traînée d'étoiles, vague et empreintes des révélations Secrets, halos et
rayons de rareté.

**Pas dans ce lot** : les 16 icônes des accessoires modernisés (`v5.md` §4.2), à refaire d'après
les nouveaux modèles Blender une fois ceux-ci validés ; les textures répétables des skins « T »
(en attente de l'arbitrage du DA, `v5.md` §17.2).

**Après génération**
1. Détourer (Aperçu sur Mac → « Supprimer l'arrière-plan », ou `blender/detourer_icones.py`) →
   `art/icones_detourees/<même nom>.png`. Vérifier : les **trous** du loup, l'intérieur des
   **anneaux** (foulard, collier, écharpe renard, chaînes, sangles), le creux entre les oreilles
   du cheval, les bords des objets **translucides** (Fantôme, Méduse, Navire, Bocal), les
   moustaches fines.
2. Copier les icônes détourées dans `TourDeLaChance-Unity/Assets/Art/icones_detourees/`.
3. Contrôle dans le jeu : chaque icône réduite à 64 px et teintée `#15121F` (silhouette de
   l'Index) reste reconnaissable — en particulier distinguer **Tigre / Chat roux**, **Dalmatien /
   Vache**, **Fantôme / Méduse**, **Tricorne / Capitaine** ; les icônes de skins montrent bien
   **le même cheval** que `couleur_bleu.png`.
