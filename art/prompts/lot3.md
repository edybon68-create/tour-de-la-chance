# Lot 3 — Prompts d'images Gemini (v3 : skins du cheval, ciels du jour à la nuit, coffres de rareté, améliorations, icônes des panneaux)

> **À quoi sert ce fichier ?** Chaque bloc ci-dessous = **une image**. On copie le prompt
> (le texte en anglais dans le cadre gris) et on le colle tel quel dans Gemini, dans le
> navigateur. On enregistre ensuite l'image sous le **nom de fichier indiqué**.
>
> Les prompts sont en anglais parce que les générateurs d'images le comprennent mieux.
> Les explications autour sont en français.
>
> Références : `docs/art-bible.md` (fiche du cheval §6.2, décor par palier §2.9 et §5.1, coffres
> de rareté §2.10, masque de héros v3 §2.8, règles des images §9, boutons §11) et
> `docs/specs/v3.md` (§5 améliorations, §7 cheval, §9 décor, §15 images à produire).

---

## 0. Mode d'emploi (à lire une fois)

1. **Quatre conversations Gemini**, une par groupe. On ne mélange pas les groupes : dans une
   même conversation, Gemini garde le même style, le même angle et le même fond.

   | Conversation | Images | Fichiers à joindre au **premier** message (bouton « + ») |
   |---|---|---|
   | A — Skins du cheval | 17 | `docs/captures/cheval_3_4.png`, `docs/captures/cheval_face.png`, `art/icones/_capsule/couleur_rouge.png` |
   | B — Icônes des panneaux, améliorations, masque | 7 | `art/icones/boite_skins_1.png`, `art/icones/chapeau_casquette.png` |
   | C — Coffres de rareté | 6 (+ 1 facultative) | `art/concept/concept_etage.png`, `art/icones/chapeau_casquette.png` |
   | D — Ciels des paliers | 3 | `art/fonds/ciel_palier3_v2.png` |

2. **Avant de commencer, ranger les anciennes images** (pour ne rien écraser par erreur et
   pouvoir comparer) :
   - créer `art/icones/_capsule/` et y **déplacer** les 17 icônes capsule : `couleur_bleu`,
     `couleur_rouge`, `couleur_vert`, `couleur_violet`, `couleur_or`, `couleur_jaune`,
     `couleur_orange`, `couleur_rose`, `couleur_turquoise`, `couleur_caramel`, `couleur_nacre`,
     `motif_sucre_orge`, `motif_pois`, `matiere_argent`, `matiere_cristal`,
     `matiere_arc_en_ciel`, `matiere_galaxie` (`.png`) ;
   - renommer `art/icones/masque_heros.png` en `art/icones/_capsule/masque_heros_v2.png` ;
   - renommer `art/fonds/ciel_palier2.png` en `ciel_palier2_v2.png` et `art/fonds/ciel_palier3.png`
     en `ciel_palier3_v2.png` (on les garde pour mémoire, le jeu ne les utilisera plus) ;
   - **ne pas toucher** à `art/fonds/ciel_palier4.png` (gardé tel quel) ni à `art/fonds/ciel_nuit.png`
     (gardé dans le projet, plus utilisé par les paliers).
3. **Le premier prompt est long, les suivants sont courts** (*« Same image as the previous
   one… only change… »*). C'est la méthode qui a donné des capsules identiques aux lots 1 et 2.
4. **Si une image dérive** (autre angle, fond qui change, cheval différent, style « photo ») :
   d'abord une correction courte (exemples sous chaque image). Si ça ne suffit pas après 2
   essais, **nouvelle conversation** et **prompt de relance** du groupe (expliqué à la fin de
   chaque groupe).
5. **Le format est écrit dans le texte** (« square 1:1 », « wide 16:9 »). Si l'image sort dans
   le mauvais format : *« Same image, but square 1:1. »* (ou *« wide 16:9 »*).
6. **Les codes hexadécimaux** sont des indications : ce qui compte, c'est que la teinte soit
   reconnaissable. Les couleurs exactes du jeu restent celles des matériaux Unity.
7. **Deux fonds d'icônes, pour un détourage propre** :
   - presque toutes les icônes : **uni lavande très clair `#E6E1EE`** ;
   - les **objets très clairs** : **uni bleu-gris moyen `#5B6380`**. Dans ce lot :
     `couleur_nacre` (cheval blanc nacré), `coffre_diamant` (nacre glacée) et
     `amelioration_coffres` (mini coffre de diamant). Sur le lavande, leurs bords clairs seraient
     mangés par le détourage. Ce sont **toujours les dernières images de leur conversation**,
     pour que le changement de fond ne « contamine » pas les autres.
8. **Enregistrement** : PNG, nom exact (minuscules, sans accent, tirets bas), dans le dossier
   indiqué. Les noms des icônes de skins sont les **identifiants du catalogue** : le programmeur
   les retrouve automatiquement.
9. **Détourage** : comme aux lots 1 et 2 (Aperçu sur Mac → « Supprimer l'arrière-plan »),
   résultat dans `art/icones_detourees/` avec le même nom. Vérifier : les **trous** du masque, le
   creux entre les oreilles du cheval, l'intérieur de la malle ouverte.
10. **Aucune image** pour : le halo, les rayons, les étincelles, les effets d'ouverture des
    coffres précieux, la cinématique du Secret (point, fissures, morceaux, confettis), les
    pastilles de niveau des améliorations. Tout cela est dessiné par le code (art bible §10.3).
    **Pas d'icône par niveau** d'amélioration : la spec §5.7 affiche les niveaux avec 5 pastilles
    rondes en USS, une seule icône par amélioration suffit.
11. **Écran titre et logo : rien à refaire.** `art/logo/logo_titre.png` montre une tour et une
    étoile, pas la capsule, et l'écran titre n'affiche que ce logo (`Assets/UI/Title.uss`). Le
    concept art `art/concept/concept_etage.png` montre encore la capsule : il reste une
    référence de style interne, on ne le refait pas.

---

## 1. Blocs communs

**Bloc de style commun** (inchangé depuis le lot 1, art bible §9.1) — déjà inclus dans les
premiers prompts des conversations A, B et C :

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.
```

**Bloc « icône »** (identique aux lots 1 et 2, pour les objets) :

```
ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.
```

**Bloc « icône cheval »** (nouveau, conversation A) : le cheval est plus haut et plus
découpé que la capsule ; on demande ~75 % de la hauteur et on insiste sur les **deux yeux
visibles**.

```
ICON FORMAT: a square 1:1 game shop icon. One single character, alone, perfectly centered, the whole piece visible from the bottom of its round base to the tips of its ears, filling about 75 percent of the image height with equal margins, nothing cropped. Three-quarter front view from slightly above (about 25 degrees), the piece turned about 20 degrees to the left, its face and muzzle toward the viewer so that both eyes are clearly visible. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.
```

**Variante « ciels v3 »** (conversation D, art bible §9.1) : on garde la phrase STYLE et la
phrase RULES, et on remplace LIGHTING / PALETTE / MOOD par :

```
SKY MOOD: the same toy world, a magical fairground on top of a fairy-tale tower, seen at a different time of day; friendly, cozy, readable. Low contrast, soft gradients, calm empty center, darkest at the bottom.
```

**Ce qui doit être identique dans tout le lot** : la lumière des icônes (chaude en haut à
gauche, liseré lavande derrière), l'angle (3/4, ~25° au-dessus, tourné ~20° vers la gauche),
l'objet seul et centré, le fond uni sans ombre portée. **Les icônes ne montrent jamais la
rareté** (pas de cadre coloré, pas d'étoiles autour, pas de halo) : c'est l'interface qui ajoute
le liseré (art bible §10).

---

## 2. Conversation A — Skins du cheval (17 images)

### Règles communes au groupe
- **Le même cheval sur les 17 images** : pièce d'échecs jouet entière (socle rond de pion à
  collerette, cou épais bombé au poitrail, tête en « boîte arrondie », long museau doux qui
  avance et pique un peu du nez, deux oreilles courtes et épaisses, crinière en 5-6 grosses
  mèches avec un toupet sur le front). Fiche : art bible §6.2.
- **Toute la pièce change de matière** (socle, cou, tête, oreilles, crinière). **Les yeux et les
  narines restent noirs `#111111`**, avec un reflet blanc dans chaque œil, sur tous les skins
  (même Cristal et Galaxie).
- Les deux rendus Blender joints servent **pour la forme seulement** (leur fond sombre et leur
  éclairage ne doivent pas être copiés) ; l'icône capsule rouge jointe sert pour le **fond, la
  lumière et la qualité de rendu**.
- Ordre : le bleu par défaut (référence), puis les couleurs simples, puis les matières
  spéciales, et le Blanc nacré **en dernier** (changement de fond).
- **Une fois `couleur_bleu.png` validée**, c'est elle la référence du groupe (relance).

---

### Image 1 — `art/icones/couleur_bleu.png` (Bleu ciel, skin par défaut)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **À faire** : nouvelle conversation, **joindre les 3 images** `docs/captures/cheval_3_4.png`,
  `docs/captures/cheval_face.png` et `art/icones/_capsule/couleur_rouge.png`, puis coller :

```
Use the attached images as references. The first two images are 3D renders of our character on a dark background: copy its SHAPE faithfully (round pawn base, neck, head, long muzzle, ears, mane, big eyes, nostrils), but do NOT copy their dark background or their lighting. The third image is one of our existing shop icons: match its plain light lavender background, its soft warm lighting, its framing and its clean toy render quality.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single character, alone, perfectly centered, the whole piece visible from the bottom of its round base to the tips of its ears, filling about 75 percent of the image height with equal margins, nothing cropped. Three-quarter front view from slightly above (about 25 degrees), the piece turned about 20 degrees to the left, its face and muzzle toward the viewer so that both eyes are clearly visible. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a cute toy chess knight piece, like a chunky painted wooden board-game piece that has come to life. From bottom to top: a round flat pawn base with a stepped collar (two stacked rounded rings with beveled edges); a thick, round, smooth neck rising from the base, bulging forward at the chest and slightly thinner under the jaw; a horse head shaped like a softly rounded box, with a long, soft, rounded muzzle that sticks out toward the viewer and tilts slightly downward, for a gentle, friendly look; two small dark oval nostrils (#111111) side by side at the tip of the muzzle; two big glossy black oval eyes (#111111), slightly taller than wide, high on the front of the head just above the base of the muzzle, half sunk into the head, each with a small white highlight in the upper left, both eyes clearly visible; two short, thick, pointed ears on top of the head; a sculpted mane made of five or six chunky rounded tufts, starting with a small forelock tuft on the forehead between the ears and running down the back of the neck. The piece is about twice as tall as its base is wide. The whole piece (base, neck, head, muzzle, ears and mane) is made of ONE single material and color; only the eyes and nostrils are black. No mouth, no teeth, no legs, no tail, no saddle, no bridle, no rider, no horn, no hat, no accessories. Smooth surfaces with soft bevels, low-poly toy look, not a realistic horse.
BODY: sky blue (#4A90D9), matte like painted wood, with only a soft highlight. Not metallic, not purple, not navy.
```

**Critères de réussite** : on reconnaît **notre** cheval (socle de pion, museau qui avance,
crinière en grosses mèches, oreilles épaisses) ; **les deux yeux** bien visibles avec leur
reflet ; deux narines ; pas de bouche ; cheval entier, socle compris ; fond lavande uni ; aucune
ombre portée.

**Corrections fréquentes**
- *« Turn the horse a bit more toward the viewer: both eyes must be clearly visible. »*
- *« Show the whole piece including the round base: nothing cropped, a bit smaller in the frame. »*
- *« Remove the mouth and the legs: it is a chess piece, only eyes and nostrils on the face. »*
- *« Keep the plain light lavender background of the third reference image, not the dark one. »*

---

### Image 2 — `art/icones/couleur_rouge.png` (Rouge tomate, Commun)

- **Format** : carré 1:1, fond uni `#E6E1EE`. À la suite, même conversation (ainsi de suite
  jusqu'à l'image 16).

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: tomato red (#D64545), matte like painted wood. Not blood red, not orange, not pink.
```

**Critères de réussite** : même cheval, rouge tomate mat.

---

### Image 3 — `art/icones/couleur_vert.png` (Vert menthe, Commun)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: fresh mint green (#3CC48A), matte. Not dark green, not neon, not turquoise.
```

---

### Image 4 — `art/icones/couleur_jaune.png` (Jaune citron, Commun)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: lemon yellow (#F2D03B), matte like painted wood, with only a soft highlight. Not gold, not metallic, not orange.
```

**Critères de réussite** : jaune citron franc, **pas doré** (on doit le distinguer de l'Or massif).

---

### Image 5 — `art/icones/couleur_orange.png` (Orange mandarine, Commun)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: tangerine orange (#F28C38), matte. Not red, not brown, not metallic.
```

---

### Image 6 — `art/icones/couleur_rose.png` (Rose bonbon, Commun)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: candy pink (#F27BB0), matte. Not purple, not red, not pastel white.
```

**Critères de réussite** : rose soutenu, bien détaché du fond lavande.

---

### Image 7 — `art/icones/couleur_violet.png` (Violet magique, Peu commun)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: magical purple (#8E5BD6) with a slightly glossy satin finish and a soft highlight. Not metallic, not blue, no sparkles.
```

---

### Image 8 — `art/icones/couleur_turquoise.png` (Turquoise lagon, Peu commun)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: lagoon turquoise (#2BB8C4) with a slightly glossy satin finish and a soft highlight. Not metallic, not green, not dark teal.
```

---

### Image 9 — `art/icones/couleur_caramel.png` (Caramel, Peu commun)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: glossy caramel (#C98A4B), very shiny like a hard candy, with a bright glossy highlight in the upper left. Smooth and clean: not metallic, not wood, no grain, no drips, no sugar crystals.
```

**Critères de réussite** : effet « bonbon » très brillant, ni bois ni cuivre.

---

### Image 10 — `art/icones/motif_sucre_orge.png` (Sucre d'orge, Rare)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: candy cane stripes: 8 wide horizontal bands of equal height from the bottom of the base to the tips of the ears, alternating tomato red (#D64545) and off-white (#F4EEE6), satin finish. The stripes are perfectly level, as if the piece had been sliced by horizontal planes: they stay horizontal across the neck, the muzzle, the head and the mane; not spiral, not diagonal, not following the muzzle. Flat painted stripes, no relief. The black eyes and nostrils sit on top of the stripes.
```

**Critères de réussite** : rayures **horizontales** (comme le motif calculé par le code, qui
utilise une projection cylindrique verticale), larges, yeux toujours lisibles.

**Correction fréquente** : *« Make the stripes perfectly horizontal and level everywhere, even
on the muzzle, and fewer and wider: 8 bands in total. »*

---

### Image 11 — `art/icones/motif_pois.png` (Pois de fête, Rare)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: candy pink (#F27BB0) with round off-white polka dots (#F4EEE6), each dot about one eighth of the base width, evenly spaced in staggered rows all over the piece, satin finish. The dots are flat and painted, not bumps. No stripes, no dots on the eyes or nostrils.
```

---

### Image 12 — `art/icones/couleur_or.png` (Or massif, Super rare)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: polished shiny solid gold (#FFC83A), metallic, with warm golden reflections and a bright highlight from the upper left, like a golden trophy chess piece. Not orange copper, not flat yellow, no engravings, no jewels.
```

**Critères de réussite** : or chaud et brillant, bien différent du Jaune citron mat.

---

### Image 13 — `art/icones/matiere_argent.png` (Argent poli, Rare)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: polished silver (#C9CED8), metallic like a soft mirror, reflecting a simple studio with warm light and dark violet areas so that its shape reads clearly against the light background; bright highlight in the upper left. Not gold, not blue chrome, no engravings, no scratches.
```

**Correction fréquente** : *« Add darker reflections on the silver so it does not blend into
the background. »*

---

### Image 14 — `art/icones/matiere_cristal.png` (Cristal, Super rare)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: translucent ice-blue crystal glass (#9FE3FF), about half transparent: we can faintly see through the piece, and the black eyes and nostrils look like they float inside the glass. Very smooth and glossy, with crisp bright reflections along the edges of the base, the muzzle, the ears and the mane that outline the shape. Not frosted, no facets, no cracks, no bubbles, no rainbow colors.
```

**Critères de réussite** : bleu glace translucide, contour bien dessiné par les reflets (le
museau et les oreilles doivent rester lisibles), yeux visibles.

---

### Image 15 — `art/icones/matiere_arc_en_ciel.png` (Arc-en-ciel néon v3, Légendaire)

En v3, ce skin n'est plus une couleur qui change : c'est un **dégradé arc-en-ciel en bandes qui
montent le long du cheval**, avec des paillettes (`v3.md` §8.1). L'icône montre un instant de
ce défilé.

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: a rainbow gradient arranged in soft horizontal bands from the base to the ears, so that three or four clear colors are visible at once (for example pink on the base, orange and yellow on the neck, mint green and sky blue on the head and ears), medium saturation, smooth blends between the bands, satin finish. The piece glows softly from inside as if lit by its own light. Three or four tiny white four-pointed sparkles sit directly on the surface of the body. The glow and the sparkles stay on the piece: no halo, no light rays, no sparkles on the background.
```

**Critères de réussite** : 3 ou 4 couleurs franches en bandes horizontales (pas une seule teinte),
aspect lumineux, **fond resté uni** (sinon le détourage est sale).

**Correction fréquente** : *« Remove the glow and sparkles around the horse: the background must
stay plain flat lavender. »*

---

### Image 16 — `art/icones/matiere_galaxie.png` (Galaxie, Secret)

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the body material, on the whole piece (base, neck, head, ears and mane).
BODY: deep night blue-violet (#2E2366), glossy, sprinkled with small warm glowing stars (#FFF1D6): tiny round dots of different sizes, scattered randomly but sparsely, a few of them slightly brighter with a tiny four-pointed twinkle. The stars glow but the glow stays on the body. No nebula, no galaxy spiral, no planets, no rainbow; plain flat lavender background with no glow.
```

**Correction fréquente** : *« Fewer and smaller stars, and keep the eyes clearly visible. »*

---

### Image 17 — `art/icones/couleur_nacre.png` (Blanc nacré, Peu commun) — **fond bleu-gris**

- **Format** : carré 1:1, **fond uni `#5B6380`** (exception, §0 point 7). Toujours en dernier.

```
Same image as the previous one: same toy chess knight, same shape with its round pawn base, same head, muzzle, ears and mane, same black eyes and nostrils, same size in the frame, same angle and same lighting. Only change the body material, on the whole piece, AND the background color.
BODY: pearly white (#EDE6F2) with a soft pearl sheen: very subtle pink and pale blue iridescent reflections on the curves, slightly glossy. Not chrome, not plain grey, not pure white.
BACKGROUND: plain flat solid medium blue-grey (#5B6380), no gradient, no floor, no scenery, no cast shadow on the background.
```

**Critères de réussite** : blanc nacré bien détaché du fond bleu-gris ; cheval identique.

---

### Prompt de relance (conversation A)
Si les chevaux dérivent : nouvelle conversation, joindre **`art/icones/couleur_bleu.png`** (la
première icône cheval validée) **et** `docs/captures/cheval_3_4.png`, coller le prompt de
l'**image 1** en remplaçant sa première phrase par *« Use the first attached image as the exact
reference: same horse, same framing, same angle, same lighting and same background; the second
image only confirms the shape. Only the body material changes. »* et sa dernière ligne
`BODY: …` par la ligne `BODY: …` de l'image voulue. Pour le Blanc nacré, ajouter aussi sa
ligne `BACKGROUND: …`.

---

## 3. Conversation B — Icônes des panneaux, améliorations, masque (7 images)

### Règles communes au groupe
- Même style, même angle, même lumière et même fond que les icônes des lots 1 et 2.
- Ces icônes seront affichées **petites** (64 × 64 dans la colonne de boutons, 48 dans l'en-tête
  des panneaux, 96 × 96 dans les cartes d'amélioration) **sur un fond sombre `#2A2A3A`** : formes
  simples et épaisses, pas d'objet noir, pas de petits détails.
- **Aucun texte, aucun chiffre, aucun signe monétaire.** Les pièces portent le motif « tour +
  étoile » comme `piece.png`.
- Les étincelles demandées par la spec sont des **reflets posés sur l'objet**, jamais des
  étoiles qui flottent sur le fond (détourage).

---

### Image 18 — `art/icones/icone_boutique.png` (bouton « Boutique »)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **À faire** : nouvelle conversation, **joindre `art/icones/boite_skins_1.png` et
  `art/icones/chapeau_casquette.png`**, puis coller :

```
Use the attached images as the exact reference for the style, the size in the frame, the angle, the lighting and the plain light lavender background. The first image shows the gift box design to reuse.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

OBJECT: the same chunky magical toy gift box as in the first image, closed: a cube slightly wider than tall with soft beveled edges, magical violet body (#8E5BD6) with a satin finish, slightly overhanging lid with a darker violet edge (#6A4C9C), a wide brass ribbon (#C99A1E) crossing the box and a big chunky brass bow with two rounded loops on top. The lid is lifted very slightly on one side, letting out a thin line of soft warm light (#FFF1D6); the glow stays on the box. In the middle of the front face, on the ribbon, a round raised brass medallion with one simple chunky five-pointed star in relief (no paint drop). No text, no letters, no numbers, no question mark, no tag, no confetti, no sparkles on the background.
```

**Critères de réussite** : on reconnaît un **cadeau** et la même famille que `boite_skins_1` ;
médaillon à **étoile** (pour ne pas confondre l'icône du bouton avec la boîte de skins elle-même).

---

### Image 19 — `art/icones/icone_inventaire.png` (bouton « Inventaire »)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object.
OBJECT: a chunky cartoon travel trunk, open, clearly NOT a pirate treasure chest: a rectangular box with a FLAT lid (not domed) opened backward, the body painted in light caramel (#C98A4B) with a satin finish, brass corner caps (#C99A1E), two vertical dark brown straps (#6B4226) and a side handle. The inside of the lid is lined with off-white fabric (#F4EEE6). Sticking out of the open trunk: a pink and yellow striped party hat (#FF6FB5, #FFD54A) with a small white pompom, tilted, and the end of a mint green and off-white striped scarf (#3CC48A, #F4EEE6) hanging over the front edge. No coins, no gold, no jewels, no lock plate, no text, no labels, no stickers.
```

**Critères de réussite** : une **malle de voyage** (couvercle plat, sangles), pas un coffre au
trésor du jeu ; chapeau de fête et écharpe bien lisibles.

**Correction fréquente** : *« The lid must be flat, like a suitcase trunk, not a rounded treasure
chest lid. »*

---

### Image 20 — `art/icones/icone_index.png` (bouton « Index »)

Le « ? » est le seul signe autorisé : c'est le même symbole que l'interface affiche pour un
Secret non trouvé (art bible §10.2, règle 6).

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object.
OBJECT: a chunky cartoon collection album, open flat like a book, seen from slightly above. Thick cover in magical violet (#8E5BD6) with small brass corners (#C99A1E); the two thick pages are off-white (#F4EEE6). On each page, a grid of six rounded square slots (3 by 2). Most slots hold simple chunky colored shapes: a tiny red cap, a tiny gold crown, a small mint green circle, a small sky blue star, a small yellow heart, an orange flower. One slot on the right page is empty, dark violet (#2A2A3A), with one big chunky pink question mark (#FF4FD8) in it. No other symbols, no letters, no numbers, no text, no stickers with writing.
```

**Critères de réussite** : un album ouvert avec des cases colorées et **un seul** « ? » rose ;
aucun texte sur les pages.

**Correction fréquente** : *« Remove all writing from the pages: only the colored shapes and one
pink question mark. »*

---

### Image 21 — `art/icones/icone_ameliorations.png` (bouton « Améliorations »)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object.
OBJECT: a big chunky toy arrow pointing straight up, thick and rounded with soft bevels, in polished shiny gold (#FFC83A), metallic, with a bright highlight in the upper left. A chunky five-pointed gold star (#FFD54A), slightly glossy, sits just above the tip of the arrow, touching it. The arrow has a short wide shaft and a big triangular head. No numbers, no text, no plus sign, no bars, no graph, no sparkles on the background.
```

**Critères de réussite** : flèche épaisse et dorée, étoile au sommet ; silhouette lisible à 64 px.

---

### Image 22 — `art/icones/amelioration_gains.png` (« Bourse magique »)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object.
OBJECT: a chunky cartoon velvet pouch, round and plump, in magical violet (#8E5BD6) with a soft velvet sheen, its neck gathered and tied with a thick brass cord (#C99A1E) ending in a bow. The pouch is open at the top and overflowing with thick shiny gold coins (#FFC83A): a few coins pile out of the top and two or three lean against the front of the pouch. Each visible coin face shows the same simple raised symbol as our game coin: a small round tower top with a little five-pointed star above it. Two or three tiny bright four-pointed glints sit directly on the coins. No numbers, no letters, no currency sign, no dollar sign, no banknotes, no portraits; the glints stay on the coins, nothing floating on the background.
```

**Critères de réussite** : bourse violette, pièces de conte (tour + étoile), **aucun** `$` ni
chiffre, aucune étincelle détachée sur le fond.

**Correction fréquente** : *« Remove the dollar sign / the numbers from the pouch and the coins:
only the tower and star symbol. »*

---

### Image 23 — `art/icones/amelioration_pieges.png` (« Coffre en plus »)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the group fills about 75 percent of the image width. Only change the object.
OBJECT: four small identical chunky toy treasure chests standing side by side in a slightly curved row, all closed: warm brown wooden body (#8C5933), rounded domed lid in darker wood (#6B4226), two dark metal bands (#4A4E5A) and a shield-shaped brass lock plate (#C99A1E) on the front. The fourth chest, on the right, is slightly in front of the others and a little bigger, with one small bright four-pointed white-gold sparkle touching the top of its lid, as if it just appeared. No red at all, no gold coins, no open chest, no numbers, no text, no sparkles floating on the background.
```

**Critères de réussite** : 4 coffres identiques au coffre du jeu, le 4e mis en avant ; **aucun
rouge** (le rouge veut dire « piège ») ; aucun chiffre.

---

### Image 24 — `art/icones/masque_heros.png` (Masque de héros v3, Rare)

Nouvelles couleurs arbitrées par le DA (art bible §2.8) : l'ancien masque bleu disparaissait
sur le cheval bleu ciel.

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image width. Only change the object.
OBJECT: a chunky cartoon hero eye mask, alone, not worn by anyone, its front facing the viewer at the same three-quarter angle: a thick curved band shaped like a simple domino mask, bending around an invisible rounded face, in raspberry pink-red (#D63A6E) with a satin finish, with a thick bright yellow rim (#FFD54A) all around its outer edge AND around each of the two large oval eye holes. Through the eye holes the plain background is visible. Two short tied ribbon ends in raspberry stick out at the back on each side. No emblem, no letter, no symbol, no face, no eyes inside the holes. It must not look like the mask of any existing superhero.
```

**Critères de réussite** : bandeau framboise, **bord jaune épais autour du masque et des deux
trous**, trous vides (ils deviendront transparents), aucun emblème.

---

### Prompt de relance (conversation B)
Nouvelle conversation, joindre `boite_skins_1.png` et `chapeau_casquette.png`, coller le prompt
de l'**image 18** en remplaçant sa première phrase par *« Use the attached images as the exact
reference for the style, the size in the frame, the angle, the lighting and the plain light
lavender background. Draw a different object. »* et sa dernière ligne `OBJECT: …` par la ligne
`OBJECT: …` de l'image voulue.

---

## 4. Conversation C — Coffres de rareté (6 images + 1 facultative)

### Règles communes au groupe
- **Le même coffre sur toutes les images** : le coffre « jouet » du jeu (art bible §6.1) —
  corps trapu à 3 planches, couvercle **bombé** qui déborde, deux bandes verticales qui passent
  sur le devant et le couvercle, équerres aux coins, grosse serrure en écusson devant. **Fermé**
  (sauf `amelioration_coffres`).
- Seules changent les couleurs du corps et du métal, et un petit détail par rareté (art bible
  §2.10) : rivets (Bronze), étoile sur la serrure (Argent), frise et 2 pierres rouges (Or),
  5 gemmes (Diamant).
- Utilisation : petites icônes de l'étiquette d'étage (à côté d'un texte de taille 26) et du
  panneau Améliorations. **Silhouette identique, couleurs bien différentes.**
- Ordre : du Bois au Diamant ; `coffre_diamant` et `amelioration_coffres` en dernier (fond
  bleu-gris).

---

### Image 25 — `art/icones/coffre_bois.png` (Coffres en bois, rang 0)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **À faire** : nouvelle conversation, **joindre `art/concept/concept_etage.png` et
  `art/icones/chapeau_casquette.png`**, puis coller :

```
Use the attached images as references: the treasure chests in the first image show the exact chest design to reuse; the second image is one of our shop icons and shows the exact icon style, angle, lighting and plain light lavender background to match.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image width with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

OBJECT: one chunky toy pirate treasure chest, closed: a sturdy body slightly wider than deep, made of three horizontal planks separated by shallow grooves; a rounded domed lid that overhangs slightly; two vertical metal bands going down the front of the body and over the lid; small metal corner brackets on the front edges; a big shield-shaped lock plate with a keyhole in the center of the front, and a small clasp coming down from the lid onto it. Colors: warm brown wood body (#8C5933), darker wood lid (#6B4226), dark metal bands and corners (#4A4E5A), brass lock plate (#C99A1E). Soft bevels on every edge. No coins, no gold spilling out, no glow, no text.
```

**Critères de réussite** : le coffre du jeu, reconnaissable (couvercle bombé, deux bandes,
serrure écusson) ; fermé ; fond lavande uni.

---

### Image 26 — `art/icones/coffre_bronze.png` (Coffres de bronze, rang 1)

```
Same image as the previous one: same chest shape and size, same planks, same domed lid, same bands, corners and lock plate, same framing, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the colors and add one detail.
OBJECT: the body and lid are now varnished reddish mahogany wood (#7E3F28), and all the metal parts (bands, corners, lock plate, clasp) are warm polished bronze (#B0703A), metallic. Add a row of round raised rivets along each bronze band. No gems, no glow, no text.
```

**Critères de réussite** : bois **plus rouge** que le coffre en bois, métal bronze chaud, rivets
visibles.

---

### Image 27 — `art/icones/coffre_argent.png` (Coffres d'argent, rang 2)

```
Same image as the previous one: same chest shape and size, same planks, same domed lid, same bands, corners and lock plate, same framing, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the colors and the detail.
OBJECT: the body and lid are now painted night blue (#3E5C8A) with a satin finish, and all the metal parts are polished silver (#C9CED8), shiny, mirror-like with darker reflections so they read clearly. No rivets; instead, one chunky five-pointed star in relief in the center of the silver lock plate, above the keyhole. No gems, no glow, no text.
```

---

### Image 28 — `art/icones/coffre_or.png` (Coffres d'or, rang 3)

```
Same image as the previous one: same chest shape and size, same planks, same domed lid, same bands, corners and lock plate, same framing, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the colors and the detail.
OBJECT: the body and lid are now glossy lacquered royal violet (#5B2D82), and all the metal parts are polished gold (#E8B830), metallic. The edge of the lid is trimmed with a thick golden frieze of simple rounded scallops, and two round faceted ruby-red gems (#D62839), very glossy, are set on the front of the lid, one on each side of the clasp. The lock plate is plain gold, no star. No glow, no coins, no text.
```

**Critères de réussite** : violet royal + doré, frise et deux pierres rouges lisibles.

---

### Image 29 (facultative) — `art/icones/coffre_piege.png` (pastille « 4 coffres · 1 piège »)

La spec (§5.6) met une « icône coffre rouge » devant le risque de l'étage. Le programmeur peut
reteinter `coffre_bois` ; cette image donne un rendu plus net. À générer ici, avant le Diamant.

```
Same image as the previous one: same chest shape and size, same planks, same domed lid, same bands, corners and lock plate, same framing, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the colors and the details.
OBJECT: the body and lid are now bright trap red (#D91A1A), satin finish, and all the metal parts are dark metal (#4A4E5A) with a dark lock plate. No frieze, no gems, no rivets, no star. The chest stays closed and friendly-looking: no face, no teeth, no skull, no spikes, no smoke, no text.
```

**Critères de réussite** : coffre rouge vif bien lisible, **rien d'effrayant**.

---

### Image 30 — `art/icones/coffre_diamant.png` (Coffres de diamant, rang 4) — **fond bleu-gris**

- **Format** : carré 1:1, **fond uni `#5B6380`** (exception, §0 point 7).

```
Same image as the previous one: same chest shape and size, same planks, same domed lid, same bands, corners and lock plate, same framing, same angle and same lighting. Only change the colors, the details AND the background color.
OBJECT: the body and lid are now icy pearly white (#E6F0F7) with a soft pearl sheen, slightly glossy, and all the metal parts are polished silver (#C9CED8). Five chunky faceted ice-blue gems (#9FE3FF), very glossy and sparkling, are set on the chest: three on the front of the lid (one big in the center, one smaller on each side) and one on each front corner bracket. No frieze, no red gems, no glow around the chest, no sparkles on the background, no text.
BACKGROUND: plain flat solid medium blue-grey (#5B6380), no gradient, no floor, no scenery, no cast shadow on the background.
```

**Critères de réussite** : coffre nacré très clair bien détaché du fond bleu-gris, 5 gemmes
bleu glace, même silhouette que les autres.

---

### Image 31 — `art/icones/amelioration_coffres.png` (« Coffres précieux ») — **fond bleu-gris**

```
Same image as the previous one: same diamond chest design (icy pearly white body and lid, polished silver metal parts, five faceted ice-blue gems), same angle, same lighting and the same plain medium blue-grey background (#5B6380); the object fills about 70 percent of the image width. Only change the pose of the lid.
OBJECT: the diamond chest is now slightly ajar: the lid is lifted a little (about 20 degrees), and a warm golden light (#FFC83A) glows from inside through the gap, as if full of treasure, without showing any coins. The glow stays inside and on the chest: no light rays, no halo and no sparkles on the background. No text.
```

**Critères de réussite** : même coffre de diamant, **entrouvert**, lueur dorée dedans ; fond
resté uni.

---

### Prompt de relance (conversation C)
Nouvelle conversation, joindre **`art/icones/coffre_bois.png`** (validée) et
`art/icones/chapeau_casquette.png`, coller le prompt de l'**image 25** en remplaçant sa première
phrase par *« Use the first attached image as the exact reference: same chest shape, same
framing, same angle, same lighting and same background. Only the colors and small details
change. »* et sa dernière ligne `OBJECT: …` par la ligne `OBJECT: …` de l'image voulue (plus la
ligne `BACKGROUND: …` pour le Diamant et l'amélioration).

---

## 5. Conversation D — Ciels des paliers, du jour à la nuit (3 images)

### Règles communes au groupe
- **Format** : large 16:9 (Gemini sort du 1376 × 768). Une image de fond unique par palier,
  pas une texture tuilable.
- **Même monde, plus haut et plus tard** : palier 1 (étages 1-3) plein jour, nuages au-dessus ;
  palier 2 (4-6) dans les nuages au coucher du soleil ; palier 3 (7-9) au-dessus de la mer de
  nuages au crépuscule ; palier 4 (10+) la nuit (`ciel_palier4.png`, **gardé**).
- **La course du soleil** : hors champ en haut à gauche (palier 1) → bas à droite, à moitié dans
  les nuages (palier 2) → couché, lueur rose-orangé à l'horizon plus vive à droite, croissant de
  lune en haut à droite (palier 3).
- **Centre calme et sans détail** (le cheval et les coffres se découpent dessus), **bas le plus
  sombre** (boutons, messages). Le **côté gauche** reste calme aussi (colonne de boutons).
- Couleurs : art bible §2.9. Mêmes nuages « pouf » arrondis low-poly que l'image jointe.
- Pas de tour, de bâtiment au premier plan, de personnage, d'objet ni de texte.

---

### Image 32 — `art/fonds/ciel_palier1.png` (« En plein jour », étages 1 à 3) — **nouveau**

- **Format** : large 16:9.
- **À faire** : nouvelle conversation, **joindre `art/fonds/ciel_palier3_v2.png`**, puis coller :

```
Use the attached image only as the reference for the style, the format, the kind of puffy rounded low-poly clouds and the calm composition. The time of day and the colors are completely different: this is bright daytime.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. SKY MOOD: the same toy world, a magical fairground on top of a fairy-tale tower, seen at a different time of day; friendly, cozy, readable. Low contrast, soft gradients, calm empty center, darkest at the bottom. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

SUBJECT: a wide 16:9 background image, to be placed behind a 3D game scene: a bright, sunny, cheerful daytime sky seen from high up on a tower. No tower, no building in the foreground, no character, no object. A clear, clearly blue sky: a smooth vertical gradient from a vivid sky blue (#5FAEE8) at the top, to a soft light blue (#A6D4F4) in the middle, to a hazy blue-grey (#5A7598) along the bottom. Big puffy rounded low-poly cumulus clouds, bright white (#F4F8FC) with soft blue-grey shading underneath (#B8CDE6): a large cluster in the top right corner, a smaller one in the top left corner, and a few small distant clouds low on the sides near the horizon. The sun itself is out of frame, above the top left corner: only a soft warm light (#FFF4E0) touches the top of the clouds on the left. The center of the image is open, calm, soft light blue sky with no cloud edge and no detail. Along the very bottom, far away and very small, a tiny fairground in the distance: a small Ferris wheel, a carousel roof and a few striped tents in soft pastel colors, all veiled by blue haze, among low rolling hills in muted blue-grey (#5A7598, #6C8AA8); this bottom strip is the darkest part of the image. No moon, no stars, no birds, no airplane, no hot-air balloons, no rainbow, no visible sun disc, no lens flare, no saturated red or gold. Low contrast overall, so that warm objects placed in front stand out clearly.
```

**Critères de réussite** : on comprend tout de suite « il fait beau » : **bleu franc** en haut
(pas un bleu grisâtre) ; centre bleu clair calme, sans nuage ; nuages blancs en haut et sur les
côtés ; fête foraine minuscule et voilée en bas ; bas le plus sombre ; pas de soleil visible.

**Corrections fréquentes**
- *« Make the sky a clearer, more vivid sky blue at the top, but keep the center soft light
  blue and empty. »*
- *« Move the clouds away from the center: only in the top corners and low on the sides. »*
- *« Make the bottom strip darker and hazier, and the fairground smaller and farther away. »*

---

### Image 33 — `art/fonds/ciel_palier2.png` (« Dans les nuages ! », étages 4 à 6) — **refait**

```
Same style, same 16:9 format, same kind of puffy rounded low-poly clouds and same calm composition rules as the previous image, but higher up and later in the day: we are now inside the clouds at sunset.

SUBJECT: a wide 16:9 background image, to be placed behind a 3D game scene. No tower, no building, no character, no object, no text. A soft luminous sunset mist fills the image: a smooth gradient from warm peach (#F2B9A0) in the upper part, to dusty rose (#C98A96) in the middle, to the darkest deep plum (#4A3040) at the bottom and in the corners. A low setting sun on the right side, about one quarter from the right edge and a little below the middle height, a soft warm disc (#FFE2B0) half hidden behind a cloud bank, with a gentle glow but no rays. Big puffy rounded low-poly clouds all around the edges: on the left, on the right, in the top corners, and a thick soft bank along the bottom; their tops are lit in soft peach (#F7D3C2), their hollows in muted rose-mauve (#B07A88). The left side of the image stays calm and a little darker. The center is calm, soft, empty mist, with no cloud edge and no detail. No stars, no moon, no birds, no rainbow, no lens flare, no harsh orange. Low contrast overall, dreamy and cozy; nothing brighter than the soft peach of the cloud tops except the small sun disc.
```

**Critères de réussite** : on se sent **dans** les nuages, au coucher du soleil (pêche et rose),
soleil bas **à droite** sans rayons, centre vide, bas prune sombre.

**Correction fréquente** : *« Softer and calmer: lower the contrast, move the sun further to the
right, keep the center empty and the bottom darker. »*

---

### Image 34 — `art/fonds/ciel_palier3.png` (« Au-dessus des nuages ! », étages 7 à 9) — **refait**

```
Same style, same 16:9 format, same kind of puffy rounded low-poly clouds and same calm composition rules as the previous images, but higher up and a little later: we are now above the sea of clouds at twilight, just after sunset.

SUBJECT: a wide 16:9 background image, to be placed behind a 3D game scene. No tower, no building, no character, no object, no text. A twilight sky: a smooth gradient from deep violet-blue (#2E2850) at the top, through soft violet (#6A5A98), down to a thin glowing band of rose-orange (#E8A48C) just above the horizon, brighter on the right side where the sun has just set. A thin delicate crescent moon (#F0ECFF) high in the upper right area, about one fifth from the top and one fifth from the right edge, softly glowing, with no face. The first small soft low-poly stars (#FFF1D6), sparse, only in the upper part of the sky, a few with a tiny four-pointed twinkle. Across the bottom third, a wide calm sea of puffy rounded low-poly clouds seen from above, stretching to the horizon, their tops catching a little lavender light (#9A90D0) and a faint rose tint near the horizon on the right, their hollows in deep violet (#4A3F78); the bottom edge of the image is the darkest (#2E2850). The center is calm open sky with nothing in it. No sun disc, no full moon, no fairground, no birds, no airplane, no planets, no shooting star trails, no saturated colors. Low contrast overall.
```

**Critères de réussite** : mer de nuages **sous** nous, ciel violet-bleu avec une **fine** bande
rose-orangé à l'horizon (plus vive à droite), fin croissant de lune en haut à droite, premières
étoiles en haut seulement ; plus sombre que le palier 2, plus clair que le palier 4.

**Correction fréquente** : *« Make the moon a thin crescent, smaller, in the upper right; keep the
rose-orange glow as a thin band on the horizon only. »*

---

### Prompt de relance (conversation D)
Nouvelle conversation, joindre la dernière image de ciel **validée** de ce lot (ou
`ciel_palier3_v2.png` si aucune), coller le prompt de l'**image 32** en gardant sa première
phrase et ses blocs STYLE / SKY MOOD / RULES, et en remplaçant sa ligne `SUBJECT: …` par la
ligne `SUBJECT: …` de l'image voulue.

---

## 6. Récapitulatif — ordre de génération

| # | Conversation Gemini | Fichier | Format | Fond |
|---|---|---|---|---|
| 1 | A — Skins (joindre `cheval_3_4.png`, `cheval_face.png`, `_capsule/couleur_rouge.png`) | `art/icones/couleur_bleu.png` | carré 1:1 | `#E6E1EE` |
| 2 | A — Skins | `art/icones/couleur_rouge.png` | carré 1:1 | `#E6E1EE` |
| 3 | A — Skins | `art/icones/couleur_vert.png` | carré 1:1 | `#E6E1EE` |
| 4 | A — Skins | `art/icones/couleur_jaune.png` | carré 1:1 | `#E6E1EE` |
| 5 | A — Skins | `art/icones/couleur_orange.png` | carré 1:1 | `#E6E1EE` |
| 6 | A — Skins | `art/icones/couleur_rose.png` | carré 1:1 | `#E6E1EE` |
| 7 | A — Skins | `art/icones/couleur_violet.png` | carré 1:1 | `#E6E1EE` |
| 8 | A — Skins | `art/icones/couleur_turquoise.png` | carré 1:1 | `#E6E1EE` |
| 9 | A — Skins | `art/icones/couleur_caramel.png` | carré 1:1 | `#E6E1EE` |
| 10 | A — Skins | `art/icones/motif_sucre_orge.png` | carré 1:1 | `#E6E1EE` |
| 11 | A — Skins | `art/icones/motif_pois.png` | carré 1:1 | `#E6E1EE` |
| 12 | A — Skins | `art/icones/couleur_or.png` | carré 1:1 | `#E6E1EE` |
| 13 | A — Skins | `art/icones/matiere_argent.png` | carré 1:1 | `#E6E1EE` |
| 14 | A — Skins | `art/icones/matiere_cristal.png` | carré 1:1 | `#E6E1EE` |
| 15 | A — Skins | `art/icones/matiere_arc_en_ciel.png` | carré 1:1 | `#E6E1EE` |
| 16 | A — Skins | `art/icones/matiere_galaxie.png` | carré 1:1 | `#E6E1EE` |
| 17 | A — Skins (en dernier) | `art/icones/couleur_nacre.png` | carré 1:1 | **`#5B6380`** |
| 18 | B — Panneaux (joindre `boite_skins_1.png`, `chapeau_casquette.png`) | `art/icones/icone_boutique.png` | carré 1:1 | `#E6E1EE` |
| 19 | B — Panneaux | `art/icones/icone_inventaire.png` | carré 1:1 | `#E6E1EE` |
| 20 | B — Panneaux | `art/icones/icone_index.png` | carré 1:1 | `#E6E1EE` |
| 21 | B — Panneaux | `art/icones/icone_ameliorations.png` | carré 1:1 | `#E6E1EE` |
| 22 | B — Panneaux | `art/icones/amelioration_gains.png` | carré 1:1 | `#E6E1EE` |
| 23 | B — Panneaux | `art/icones/amelioration_pieges.png` | carré 1:1 | `#E6E1EE` |
| 24 | B — Panneaux | `art/icones/masque_heros.png` (v3, remplace l'ancien) | carré 1:1 | `#E6E1EE` |
| 25 | C — Coffres (joindre `concept_etage.png`, `chapeau_casquette.png`) | `art/icones/coffre_bois.png` | carré 1:1 | `#E6E1EE` |
| 26 | C — Coffres | `art/icones/coffre_bronze.png` | carré 1:1 | `#E6E1EE` |
| 27 | C — Coffres | `art/icones/coffre_argent.png` | carré 1:1 | `#E6E1EE` |
| 28 | C — Coffres | `art/icones/coffre_or.png` | carré 1:1 | `#E6E1EE` |
| 29 | C — Coffres (facultatif) | `art/icones/coffre_piege.png` | carré 1:1 | `#E6E1EE` |
| 30 | C — Coffres (avant-dernier) | `art/icones/coffre_diamant.png` | carré 1:1 | **`#5B6380`** |
| 31 | C — Coffres (en dernier) | `art/icones/amelioration_coffres.png` | carré 1:1 | **`#5B6380`** |
| 32 | D — Ciels (joindre `ciel_palier3_v2.png`) | `art/fonds/ciel_palier1.png` | 16:9 | — |
| 33 | D — Ciels | `art/fonds/ciel_palier2.png` | 16:9 | — |
| 34 | D — Ciels | `art/fonds/ciel_palier3.png` | 16:9 | — |

**Gardés tels quels** : `art/fonds/ciel_palier4.png` (palier 4), `art/fonds/ciel_nuit.png` (plus
utilisé par les paliers), le logo et l'emblème, les 11 autres icônes d'accessoires, les deux
boîtes, la pièce.

**Pas d'image** : halo, rayons, étincelles, effets des coffres précieux, cinématique du Secret,
pastilles de niveau (dessinés par le code).

**Après génération**
1. Détourer les 31 icônes (n° 1 à 31, la 29 si générée) → `art/icones_detourees/<même nom>.png`.
   Vérifier : trous du masque transparents, creux entre les oreilles du cheval, intérieur de la
   malle, bords clairs du nacre et du diamant intacts.
2. Copier dans Unity : icônes détourées → `TourDeLaChance-Unity/Assets/Art/icones_detourees/`
   (elles remplacent les icônes capsule du même nom) ; ciels → `TourDeLaChance-Unity/Assets/Art/fonds/`
   (`ciel_palier1.png` est nouveau, `ciel_palier2.png` et `ciel_palier3.png` remplacent les
   anciens).
3. Contrôle rapide dans le jeu : chaque icône réduite à 64 px et teintée `#15121F` (silhouette de
   l'Index) doit rester reconnaissable ; les icônes des panneaux doivent se lire sur le fond
   sombre `#2A2A3A` de la colonne de boutons ; « Étage N » doit rester lisible sur
   `ciel_palier1.png` (capture du critère 26 de la spec).
