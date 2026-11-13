# Lot 7 — Prompts d'images Gemini (v6 : icônes, Renaissance, plan B des bulles)

> **À quoi sert ce fichier ?** Chaque bloc ci-dessous = **une image**. On copie le prompt
> (le texte en anglais dans le cadre gris) et on le colle tel quel dans Gemini, dans le
> navigateur. On enregistre ensuite l'image sous le **nom de fichier indiqué**.
>
> Les prompts sont en anglais parce que les générateurs d'images le comprennent mieux.
> Les explications autour sont en français.
>
> Références : `docs/art-bible.md` **§15** (interface v6 : où et à quelle taille chaque image
> s'affiche), §9 (règles des images), §12.18 (style des pop-ups).
>
> **Les bulles des boutons ne sont PAS dans ce lot** : elles sont rendues dans Blender par
> l'artiste 3D (`blender/rendu_bulles.py`, art bible §15.1). La section 5 ci-dessous n'est
> qu'un **plan B**, à utiliser seulement si l'auteur n'aime pas le rendu Blender.

---

## 0. Mode d'emploi (à lire une fois)

1. **Deux conversations Gemini** (plus une troisième pour le plan B, si besoin) :
   - **H — Icônes de l'interface** (7 images, fond lavande) : joindre au **premier** message
     (bouton « + ») `art/icones/icone_inventaire.png` et `art/icones/icone_classement.png`.
   - **I — Renaissance** (2 images, fond lavande) : joindre au premier message
     `docs/captures/cheval_3_4.png`, `art/icones/couleur_or.png` et `art/icones/icone_rebirth.png`.
2. **Le premier prompt de chaque conversation est long, les suivants sont courts** (*« Same
   image as the previous one… only change… »*) : c'est la méthode qui garde le même rendu.
   Si une image dérive (autre angle, autre fond), nouvelle conversation et prompt de relance (§4).
3. **Fond** : **uni lavande très clair `#E6E1EE`** pour tout le lot (aucun objet très clair ici).
4. **Où ces images s'affichent** : les icônes sont posées **sur des bulles colorées** (art bible
   §15.1.4) : Boutique sur du **rose** `#FF4F8B`, roue dentée sur de l'**ardoise** `#5A5488`.
   L'icône ne doit donc pas reprendre la couleur de sa bulle.
5. **Aucun texte, aucun chiffre, aucun logo**, aucun symbole d'argent réel (`$`, `€`, billets,
   cartes bancaires). Ne rien copier : **pas de panier rouge** (c'est l'icône du jeu de référence
   de l'auteur), pas de personnage connu, pas de gant blanc de dessin animé.
6. **Enregistrement** : PNG, nom exact (minuscules, sans accent, tirets bas) dans le dossier
   indiqué (`art/icones/`, `art/illustrations/` ou `art/ui/`).
7. **Détourage** : `blender/detourer_icones.py` (il ajoute aussi le contour noir épais de la
   v5.1), résultat dans `art/icones_detourees/` avec le même nom. Points à vérifier pour chaque
   image : voir ses « critères de réussite ».

---

## 1. Blocs communs

**Bloc de style commun** (inchangé depuis le lot 1, art bible §9.1) — déjà inclus dans les
premiers prompts :

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.
```

**Bloc « icône »** (identique aux lots 1 à 6) :

```
ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.
```

**Ce qui doit être identique dans tout le lot** : la lumière (chaude en haut à gauche, liseré
lavande derrière), l'angle (3/4, ~25° au-dessus, tourné ~20° vers la gauche), l'objet seul et
centré à ~70 % du cadre, le fond uni lavande sans ombre portée, le rendu « jouet verni » des
icônes jointes, des formes **épaisses** lisibles à 64-92 px (rien de fin, pas de petits détails).

---

## 2. Conversation H — Icônes de l'interface (fond lavande)

### Image 1 — `art/icones/icone_boutique.png` (nouvelle icône Boutique)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **Utilisation** : bulle rose de 160 px (icône à 92 px), et titre de la pop-up Boutique (120 px).
  On doit comprendre **« magasin »** en une demi-seconde : c'est l'**auvent rayé** qui le dit.
- **À faire** : nouvelle conversation, **joindre `art/icones/icone_inventaire.png` et
  `art/icones/icone_classement.png`**, puis coller :

```
Use the attached images as the exact reference for the style, the size in the frame, the angle, the lighting and the plain light lavender background. They are two of our existing game icons; draw a new object that clearly belongs to the same set.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

OBJECT: a small chunky cartoon toy fairground market stall, like a wooden toy shop booth. The top half of the image is a big, thick, puffy awning with wide vertical stripes alternating turquoise (#2EC4C4) and cream (#FFF4DC), its front edge cut into large rounded scallops, slightly bulging like a soft cushion, with a small shiny gold ball (#FFC83A) on each top corner. Below it, a sturdy wooden counter in warm wood (#8C5933) with a darker wood base (#6B4226) and two thick rounded wooden posts holding the awning. On the counter, three big chunky goods side by side: a small violet gift box (#8E5BD6) with a shiny gold ribbon and bow, a tiny black top hat with a violet band, and a big gold star lollipop on a short stick. A blank rounded cream price tag hangs from the front of the counter on a short string, with nothing written on it. Thick and solid like a toy, soft bevels on every edge, no thin parts. No basket, no shopping cart, no bag, no shopkeeper, no character, no sign board, no text, no numbers, no coins, nothing floating on the background.
```

**Critères de réussite** : un **stand de foire** reconnaissable même réduit à 64 px (vérifier en
réduisant l'image) ; auvent rayé turquoise et crème bien visible en haut ; trois objets épais
sur le comptoir ; étiquette **vierge** ; même rendu que les icônes jointes. Détourage : vérifier
l'espace entre l'auvent et le comptoir (le fond lavande ne doit pas y rester).

**Corrections fréquentes**
- *« Make the striped awning bigger and puffier: it must take the whole top half of the image. »*
- *« Fewer and bigger goods on the counter: only the gift box, the top hat and the star lollipop. »*
- *« Remove any writing from the price tag and from the stall. »*

---

### Image 2 (facultative) — `art/icones/icone_boutique_b.png` (variante « sac de courses »)

- **Format** : carré 1:1, fond uni `#E6E1EE`. À la suite, même conversation.
- **Pourquoi ?** Si le stand est trop chargé à petite taille, le chef de projet peut choisir
  ce sac. On ne garde **qu'une** des deux (le fichier choisi est renommé `icone_boutique.png`).
  Attention : sac en **turquoise** pour ne pas disparaître sur la bulle rose.

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object.
OBJECT: a chunky cartoon toy paper shopping bag, plump and slightly rounded, in turquoise (#2EC4C4) with a cream (#FFF4DC) folded top rim and two thick rounded cream rope handles. A small gold star sticker (#FFC83A) on the front, no other decoration. Sticking out of the top: a small violet gift box (#8E5BD6) with a gold bow, the brim of a tiny black top hat, and a gold star lollipop. Thick and solid like a toy, soft bevels. No logo, no brand, no text, no price tag, no basket, no cart, nothing floating on the background.
```

**Critères de réussite** : un sac de courses jouet, turquoise, **sans aucun logo** ; on voit les
objets dépasser.

---

### Image 3 — `art/icones/icone_reglages.png` (nouvelle roue dentée)

- **Format** : carré 1:1, fond uni `#E6E1EE`. À la suite, même conversation.
- **Utilisation** : petite bulle ardoise de 96 px (roue à 64 px), qui tourne de 60° au survol.
- **Pourquoi on la refait** : l'ancienne avait un **trou au centre** et des dents fines ; le
  détourage laissait du lavande coincé dedans. Ici : **moyeu plein**, 8 grosses dents.

```
Same image as the previous one: same style, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object, and show it almost from the front (tilted only about 15 degrees) so its round shape reads clearly.
OBJECT: one single chunky cartoon toy cog wheel, very thick like a toy, with exactly 8 big wide teeth with softly rounded corners, evenly spaced, in smooth satin silver-lavender metal (#C9CDE0 on the lit side, #8A90B0 in the shade), with a thick soft bevel on all edges. In the center, a big solid domed hub cap in polished shiny gold (#FFC83A) that completely fills the middle: there is NO hole in the wheel, nothing can be seen through it. One tiny four-pointed glint sits on the gold hub. No second gear, no screws, no wrench, no text, no numbers, nothing floating on the background.
```

**Critères de réussite** : une roue dentée **pleine** (aucun trou), 8 dents épaisses bien
séparées, moyeu doré ; lisible à 48 px. Détourage : les creux **entre** les dents doivent être
nets et entièrement transparents.

**Corrections fréquentes**
- *« Fill the center completely with the gold hub: no hole at all. »*
- *« Make the teeth wider and fewer: exactly 8 big rounded teeth. »*

---

### Image 4 — `art/icones/icone_graphismes.png` (rangée « Graphismes » des Paramètres)

- **Format** : carré 1:1, fond uni `#E6E1EE`. À la suite, même conversation.
- **Utilisation** : 64 px, à gauche du mot « Graphismes » (comme les icônes Musique et Effets).
  Idée : **« la qualité de l'image »** → un petit cadre-tableau jouet avec un paysage brillant.

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object.
OBJECT: a chunky cartoon toy picture frame standing upright on a small foot, with a thick rounded frame in shiny gold (#FFC83A) with a brass inner edge (#C99A1E). Inside the frame, a very simple bright toy landscape made of thick rounded shapes: a sky blue background (#6EC3FF), one big rounded green hill (#4CC27A), a small rounded violet hill behind it (#8E5BD6) and a big round yellow sun (#FFE27A) in the upper corner. Two tiny four-pointed glints sit on the gold frame. Thick and solid like a toy, soft bevels. No people, no text, no numbers, no screen, no buttons, nothing floating on the background.
```

**Critères de réussite** : un **cadre doré avec un paysage** simple, lisible à 64 px ; pas d'écran
d'ordinateur.

---

### Image 5 — `art/icones/icone_rotation.png` (indice « glisse pour tourner »)

- **Format** : carré 1:1, fond uni `#E6E1EE`. À la suite, même conversation.
- **Utilisation** : 40 px, dans une pilule sombre sous le modèle de la fiche agrandie (art bible
  §15.6). Il doit se lire **sur du sombre** : flèche dorée bien claire.

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object.
OBJECT: a small chunky round toy turntable platform, a thick flat disc in light lavender (#D9D2F0) with a darker violet rim (#8E5BD6), seen from slightly above. Around it, one very thick curved double-headed arrow in shiny gold (#FFC83A) with a brass edge (#C99A1E), curving horizontally in front of the platform in a wide smile-shaped arc, with one big rounded arrowhead at each end pointing left and right, as if the platform can be spun both ways. The arrow is thick like a toy and much more visible than the platform. No hand, no glove, no finger, no character, no text, nothing floating on the background.
```

**Critères de réussite** : on lit **« tourner dans les deux sens »** ; flèche épaisse dorée à deux
pointes ; aucune main.

---

### Image 6 — `art/icones/icone_route.png` (titre de la Route des trophées)

- **Format** : carré 1:1, fond uni `#E6E1EE`. À la suite, même conversation.
- **Utilisation** : icône qui déborde du titre de la pop-up « Route des trophées » (120 px).
  Idée : **un chemin qui monte vers un drapeau**, comme la tour.

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object.
OBJECT: a small chunky toy spiral staircase of five thick rounded stone steps in cool blue-grey stone (#8A90B0, darker #5B6380 on the sides), winding up around a short round stone pillar, like a tiny piece of a fairy-tale tower. On the top step stands a short brass pole (#C99A1E) with a round gold knob, carrying a big chunky triangular pennant flag in shiny gold (#FFC83A) with a small rounded white star on it, waving to the right. A thin glowing gold trail (#FFE27A) runs along the edge of the steps, as if marking the path already climbed. Thick and solid like a toy, soft bevels. No character, no trophy cup, no text, no numbers, nothing floating on the background.
```

**Critères de réussite** : un **escalier qui monte vers un drapeau doré**, lisible à 120 px ;
pas de coupe (la coupe est l'icône du Classement).

---

### Image 7 — `art/icones/drapeau_record.png` (marqueur du record)

- **Format** : carré 1:1, fond uni `#E6E1EE`. À la suite, même conversation.
- **Utilisation** : 40 à 64 px, planté au bord du chemin de la Route, à l'étage du record, et
  dans la pilule « Record ».

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object.
OBJECT: the same gold pennant flag as in the previous image, alone and bigger: a short thick brass pole (#C99A1E) with a round gold knob on top, planted in a small round stone base (#8A90B0), carrying a big chunky triangular pennant flag in shiny gold (#FFC83A) with a small rounded white star in its center, waving to the right with one soft fold. Thick and solid like a toy, soft bevels. No stairs, no text, no numbers, nothing floating on the background.
```

**Critères de réussite** : le **même drapeau** que sur l'icône 6, seul ; lisible à 40 px.

---

## 3. Conversation I — Renaissance (fond lavande)

### Image 8 — `art/illustrations/renaissance_heros.png` (grande illustration de la Renaissance)

- **Format** : carré 1:1, fond uni `#E6E1EE`. **Exception au cadrage des icônes** : le sujet
  remplit ~**85 %** de la hauteur (c'est une illustration, affichée en 300 × 300).
- **Utilisation** : centre de la pop-up Renaissance, devant des rayons dorés qui tournent (art
  bible §15.4). Idée : **le cheval renaît en plus fort**, porté par des ailes de flammes.
- **Important pour le détourage** : flammes et ailes en **formes pleines et épaisses**, sans
  lueur floue ni particules (le halo et les étincelles sont dessinés par le code).
- **À faire** : nouvelle conversation, **joindre `docs/captures/cheval_3_4.png`,
  `art/icones/couleur_or.png` et `art/icones/icone_rebirth.png`**, puis coller :

```
Use the attached images as the exact reference: the horse is our game character, a toy chess knight piece, and must keep exactly the same shape (pawn base, neck, head, muzzle, ears, mane, two big glossy black eyes with a white highlight, two dark nostrils, no mouth, no legs); the gold icon shows its gold material; the orange ring icon shows the style and colors of our "rebirth" symbol. Keep the same rendering style, lighting and plain light lavender background.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

FORMAT: a square 1:1 game illustration. One single group, perfectly centered, filling about 85 percent of the image height, nothing cropped. Three-quarter front view from slightly above (about 20 degrees), the horse turned about 20 degrees to the left, its face and both eyes toward the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: our toy chess knight horse, entirely in polished shiny gold (#FFC83A, like the attached gold icon), rising upward triumphantly out of a thick round ring of chunky toy flames. The ring of flames is made of big solid rounded flame shapes, glossy candy flame orange (#EF6A32) at the bottom turning to warm orange (#FF9A66) and shiny gold (#FFC83A) at the tips, like the orange rebirth ring icon. Behind the horse, two big wings spread wide and raised in a V, each made of four thick rounded flame-shaped feathers, orange (#EF6A32) at the root and gold (#FFC83A) at the tips, like a phoenix. Above the horse's head, a chunky five-pointed gold star, slightly tilted. Two or three tiny four-pointed glints sit directly on the horse and the star. Everything thick and solid like a toy, soft bevels, joyful and powerful. No blurry glow, no smoke, no sparks or particles on the background, no fire realism, no rider, no legs, no text, nothing floating on the background except the star.
```

**Critères de réussite** : **notre cheval** (même forme que `cheval_3_4.png`, deux yeux, pas de
bouche ni de pattes), doré, qui monte d'un anneau de flammes **pleines**, ailes de phénix en V,
étoile au-dessus ; lisible à 300 px ; détourage propre (pas de halo flou autour des flammes).

**Corrections fréquentes**
- *« Keep exactly the same horse shape as the attached render: chess piece base, no legs, two big eyes. »*
- *« Make the flames solid chunky toy shapes, with no glow and no particles around them. »*
- *« Make the wings bigger and the feathers fewer and thicker. »*

---

### Image 9 — `art/ui/macaron_or.png` (macaron du « ×2 »)

- **Format** : carré 1:1, fond uni `#E6E1EE`. À la suite, même conversation.
- **Utilisation** : 170 × 170 px, collé en haut à droite de l'illustration ; l'interface écrit
  « ×2 » **par-dessus** (le centre doit donc être **vide et plat**).

```
Same style, same lighting and same plain light lavender background (#E6E1EE) as the previous image; the object fills about 80 percent of the image height. Only change the subject, and show it straight from the front, perfectly flat-on, like a badge.
SUBJECT: one chunky cartoon toy award seal badge, seen from the front: a thick round starburst with 16 big rounded points, in polished shiny gold (#FFC83A) with a darker gold edge (#F0A820) and a soft bevel, and in its center a large smooth flat round area in lighter gold (#FFE27A), completely empty, surrounded by a thin raised brass ring (#C99A1E). One tiny four-pointed glint on the upper left point. No ribbon tails, no text, no numbers, no symbol, no star in the center, nothing floating on the background.
```

**Critères de réussite** : macaron doré **de face**, centre **vide** et plat, 16 pointes arrondies ;
rien d'écrit.

---

## 4. Prompts de relance

- **Conversation H** : nouvelle conversation, joindre `art/icones/icone_inventaire.png` et
  `art/icones/icone_classement.png` (ou, une fois validée, `art/icones/icone_boutique.png`),
  coller le prompt complet de l'**image 1** en remplaçant sa dernière ligne `OBJECT: …` par celle
  de l'image voulue (pour la roue dentée, ajouter la phrase « show it almost from the front »).
- **Conversation I** : nouvelle conversation, mêmes trois images jointes, prompt complet de
  l'**image 8** ; pour le macaron, remplacer `FORMAT` et `SUBJECT` par ceux de l'image 9.

---

## 5. Plan B — Les bulles des boutons (seulement si le rendu Blender est refusé)

> La méthode retenue est le rendu Blender (art bible §15.1.2). Ces prompts ne servent que si
> l'auteur préfère l'aspect Gemini. Dans ce cas : **pas de variantes « appuyé »** (l'appui est
> fait en USS, §15.1.7), et les bords du 9-slice sont **mesurés sur l'image obtenue**.

**Conversation J — Bulles** : nouvelle conversation, joindre `docs/references/v5_1_ref_popup_rebirth.png`
(pour le contour épais et le brillant « jeu mobile » seulement), fond **lavande** `#E6E1EE`.

### Image B1 — `art/bulles/bulle_rond_boutique.png` (bulle ronde de référence)

```
Use the attached image only as a reference for the thick dark outline and the glossy candy look of mobile game buttons; do not copy anything else from it.

STYLE: a glossy cartoon game button bubble, clean vector-like 3D render, smooth and shiny like hard candy. LIGHTING: soft light from the upper left. RULES: no text, no letters, no numbers, no icon, no symbol, no logo, no watermark.

FORMAT: a square 1:1 image, seen perfectly from the front (orthographic, no perspective). The button is centered and fills about 80 percent of the image width. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor; only a soft dark shadow directly under the button.

OBJECT: one round toy button seen from the front, like a thick candy coin. A very thick, perfectly even, solid near-black outline (#110D1F) around the whole shape. The face is a perfect circle in glossy candy pink, light pink (#FF8FB8) at the top, pink (#FF4F8B) in the middle and deeper pink (#E0336F) at the bottom, slightly domed. Under the face, a darker pink band (#A3214F) shows the thickness of the button, visible only along the bottom edge, following the round shape exactly. A thin lighter bevel line along the inner top edge of the face. A large soft white oval highlight in the upper part of the face, fading from 60 percent white at the top to transparent, never touching the outline, plus one small bright white dot at its upper left. The center of the face is empty and smooth: absolutely nothing drawn in the middle, because an icon will be placed on top later.
```

**Critères de réussite** : un rond parfait, contour noir épais et régulier, épaisseur visible
seulement en bas, reflet doux dans la face, **centre vide**.

### Images B2 à B9 — les autres couleurs (à la suite, même conversation)

Coller, pour chaque fichier, la phrase suivante en remplaçant les couleurs :

```
Same image as the previous one: exactly the same button shape, size, outline, highlight, shadow, angle and background. Only change the colors: face from [CLAIR] at the top to [FACE] in the middle and [MI] at the bottom, thickness band [ÉPAISSEUR].
```

| # | Fichier | [CLAIR] | [FACE] | [MI] | [ÉPAISSEUR] | Ajout |
|---|---|---|---|---|---|---|
| B2 | `art/bulles/bulle_rond_inventaire.png` | `#7DB5FF` | `#3B7DD8` | `#2F68BE` | `#1F4A8C` | — |
| B3 | `art/bulles/bulle_rond_index.png` | `#5FE0CF` | `#1FB5A3` | `#17998A` | `#0F6B61` | — |
| B4 | `art/bulles/bulle_rond_ameliorations.png` | `#8BEA6B` | `#3DBE4F` | `#2EA040` | `#1E7030` | — |
| B5 | `art/bulles/bulle_rond_classement.png` | `#B49CFF` | `#7B5BF0` | `#6545D6` | `#4530A8` | — |
| B6 | `art/bulles/bulle_rond_renaissance.png` | `#6A5AD8` | `#3A2C9A` | `#2A1F78` | `#17104A` | *« Add six tiny cream four-pointed stars (#FFF1D6) near the edge of the face, none in the center. »* |
| B7 | `art/bulles/bulle_petit_ardoise.png` | `#8A84B8` | `#5A5488` | `#4A4472` | `#2E2A4E` | — |
| B8 | `art/bulles/bulle_petit_rouge.png` | `#FF8A8A` | `#F0484E` | `#D83A40` | `#9A2228` | — |
| B9 | `art/bulles/bulle_petit_or.png` | `#FFE66B` | `#FFC83A` | `#F0A820` | `#B87410` | — |

(B7 à B9 : même image que les grandes, l'interface les affiche plus petites.)

### Images B10 à B13 — bulles rectangulaires (9-slice)

Première (à la suite) :

```
Same style, outline, highlight, shadow and background as the previous images, seen perfectly from the front. Only change the shape: a wide rounded-rectangle button, about 3 times wider than tall, with round corners, perfectly straight top and bottom edges, filling about 85 percent of the image width, in a 16:9 image. Face from light green (#7BE38F) at the top to green (#2FB85A) in the middle and deeper green (#26A04D) at the bottom, thickness band (#1A6E36) along the bottom edge only. The highlight is a long soft white band along the top of the face, with rounded ends, never touching the outline. The middle of the face is empty: nothing written.
```

Puis la phrase « Same image as the previous one… only change the colors » avec :

| # | Fichier | [CLAIR] | [FACE] | [MI] | [ÉPAISSEUR] |
|---|---|---|---|---|---|
| B10 | `art/bulles/bulle_rect_vert.png` | (prompt ci-dessus) | | | |
| B11 | `art/bulles/bulle_rect_violet.png` | `#A08CFF` | `#6C4FE0` | `#5A3FCC` | `#3F2A9E` |
| B12 | `art/bulles/bulle_rect_aqua.png` | `#8FEAFF` | `#29B6E8` | `#1E9BD0` | `#12689A` |
| B13 | `art/bulles/bulle_rect_gris.png` | `#A8A4BC` | `#7E7A94` | `#6E6A84` | `#4E4A64` |

**Après génération (plan B)** : détourer **sans** ajouter de contour (il est déjà dans l'image :
ne pas laisser `blender/detourer_icones.py` en ajouter un second, prévoir une option si besoin), recadrer les rondes en carré serré,
puis mesurer les bords du 9-slice (largeur du coin arrondi + 8 px) et les noter dans le USS.

---

## 6. Récapitulatif — ordre de génération

| # | Conversation Gemini | Fichier | Format | Fond |
|---|---|---|---|---|
| 1 | H — Icônes (joindre `icone_inventaire.png`, `icone_classement.png`) | `art/icones/icone_boutique.png` | carré 1:1 | `#E6E1EE` |
| 2 | H (facultative) | `art/icones/icone_boutique_b.png` | carré 1:1 | `#E6E1EE` |
| 3 | H | `art/icones/icone_reglages.png` | carré 1:1 | `#E6E1EE` |
| 4 | H | `art/icones/icone_graphismes.png` | carré 1:1 | `#E6E1EE` |
| 5 | H | `art/icones/icone_rotation.png` | carré 1:1 | `#E6E1EE` |
| 6 | H | `art/icones/icone_route.png` | carré 1:1 | `#E6E1EE` |
| 7 | H | `art/icones/drapeau_record.png` | carré 1:1 | `#E6E1EE` |
| 8 | I — Renaissance (joindre `cheval_3_4.png`, `couleur_or.png`, `icone_rebirth.png`) | `art/illustrations/renaissance_heros.png` | carré 1:1 (sujet à 85 %) | `#E6E1EE` |
| 9 | I | `art/ui/macaron_or.png` | carré 1:1, de face | `#E6E1EE` |
| B1–B13 | J — **plan B seulement** | `art/bulles/bulle_*.png` | carré / 16:9 | `#E6E1EE` |

**Priorité** : 1 et 3 (demandés par l'auteur), puis 8 et 9 (Renaissance), puis 5, 6, 7, 4 (à
mesure que les fonctions de la spec v6 sont codées). 2 seulement si 1 ne convient pas.

**Pas dans Gemini** : les 25 bulles (`blender/rendu_bulles.py`, artiste 3D) ; rayons, lueurs,
chemin de la Route, chevrons, croix, coches, jauges (code ou USS).

**Après génération**
1. Détourer → `art/icones_detourees/<même nom>.png` (illustration et macaron aussi).
2. Copier dans `TourDeLaChance-Unity/Assets/Art/icones_detourees/`.
3. Contrôle dans le jeu : icône Boutique à 92 px sur la bulle rose et roue dentée à 64 px sur la
   bulle ardoise se reconnaissent **sans texte** ; l'illustration de la Renaissance se détache
   des rayons dorés ; « ×2 » se lit bien au centre du macaron.
