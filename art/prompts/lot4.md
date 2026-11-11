# Lot 4 — Prompts d'images Gemini (v4 : interface « jouet de fête foraine »)

> **À quoi sert ce fichier ?** Chaque bloc ci-dessous = **une image**. On copie le prompt
> (le texte en anglais dans le cadre gris) et on le colle tel quel dans Gemini, dans le
> navigateur. On enregistre ensuite l'image sous le **nom de fichier indiqué**.
>
> Les prompts sont en anglais parce que les générateurs d'images le comprennent mieux.
> Les explications autour sont en français.
>
> Références : `docs/art-bible.md` §12 (interface v4 : où et comment ces images sont affichées),
> §9 (règles des images), §2.10 (couleurs or / argent / bronze).

---

## 0. Mode d'emploi (à lire une fois)

1. **Une seule conversation Gemini** (conversation E), 7 images, dans l'ordre du tableau final.
   On joint au **premier** message (bouton « + ») les deux icônes déjà validées du lot 3 :
   `art/icones/icone_boutique.png` et `art/icones/icone_ameliorations.png`. Elles donnent le
   style, l'angle, la lumière, le cadrage et le fond à recopier.
2. **Le premier prompt est long, les suivants sont courts** (*« Same image as the previous
   one… only change… »*) : c'est la méthode qui garde le même rendu d'une image à l'autre.
3. **Ce qu'on NE refait PAS** : les icônes Boutique, Inventaire, Index et Améliorations sont
   **gardées** telles quelles (demande de l'auteur). Les boutons, cadres, croix de fermeture,
   barres de défilement, interrupteurs, podium et badge d'étage sont **dessinés en USS**
   (art bible §12) : aucune image pour eux.
4. **Pourquoi la roue dentée est une image et pas de l'USS ?** L'USS sait faire des rectangles
   arrondis, des cercles et des bordures, mais pas une forme dentée propre. Une image dans le
   même style que les autres icônes est plus jolie et plus simple.
5. **Deux fonds, pour un détourage propre** (comme aux lots précédents) :
   - toutes les icônes : **uni lavande très clair `#E6E1EE`** ;
   - sauf `icone_parametres` (roue en argent clair) : **uni bleu-gris moyen `#5B6380`**. Elle
     passe donc **en dernier** pour que le changement de fond ne « contamine » pas les autres.
6. **Les icônes s'affichent sur du sombre** : dans un disque `#3B2F66` (boutons ronds, §12.5),
   dans l'en-tête coloré des pop-ups (120 px) et sur le podium. Formes épaisses, pas d'objet
   noir, pas de petits détails, **silhouette lisible à 52 px**.
7. **Aucun texte, aucun chiffre** : même sur les médailles. Le rang « 1, 2, 3 » est écrit par
   l'interface, à côté (art bible §12.14).
8. **Ne rien copier de Roblox** : pas de logo, pas de carré penché, pas de monnaie hexagonale,
   pas de personnage « bloc ». Pas de trophée copié d'une compétition réelle.
9. **Enregistrement** : PNG, nom exact (minuscules, sans accent, tirets bas), dans `art/icones/`.
10. **Détourage** : comme aux lots précédents (Aperçu sur Mac → « Supprimer l'arrière-plan »),
    résultat dans `art/icones_detourees/` avec le même nom. Vérifier : le **trou** au centre de
    la roue dentée et les **creux entre ses dents**, l'espace entre les **anses** du trophée et
    la coupe, le creux du **ruban** des médailles, l'espace entre les deux **notes** de musique.

---

## 1. Blocs communs

**Bloc de style commun** (inchangé depuis le lot 1, art bible §9.1) — déjà inclus dans le
premier prompt :

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.
```

**Bloc « icône »** (identique aux lots 1 à 3) :

```
ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.
```

**Ce qui doit être identique dans tout le lot** : la lumière (chaude en haut à gauche, liseré
lavande derrière), l'angle (3/4, ~25° au-dessus, tourné ~20° vers la gauche), l'objet seul et
centré à ~70 % du cadre, le fond uni sans ombre portée, le rendu « jouet verni » des icônes
`icone_boutique.png` et `icone_ameliorations.png`. Les étincelles éventuelles sont **posées sur
l'objet**, jamais flottantes sur le fond.

---

## 2. Conversation E — Icônes de l'interface v4 (7 images)

### Image 1 — `art/icones/icone_classement.png` (bouton et pop-up « Classement »)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **Utilisation** : bouton rond de la colonne droite (92 px, sur `#3B2F66`) et icône de l'en-tête
  **or** de la pop-up Classement (120 px). Le trophée doit donc rester lisible sur du sombre
  **et** sur du jaune : d'où le podium violet sous la coupe dorée.
- **À faire** : nouvelle conversation, **joindre `art/icones/icone_boutique.png` et
  `art/icones/icone_ameliorations.png`**, puis coller :

```
Use the attached images as the exact reference for the style, the size in the frame, the angle, the lighting and the plain light lavender background. They are two of our existing game icons; draw a new object that clearly belongs to the same set.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

OBJECT: a chunky cartoon toy trophy standing on a small toy podium. The trophy is a big rounded cup with two thick looped handles, a short stem and a round foot, in polished shiny gold (#FFC83A), metallic, with a bright highlight in the upper left; one chunky five-pointed star in relief in the middle of the front of the cup. The trophy stands on the top step of a small three-step podium seen from the front: the middle step is the highest, the left step a bit lower, the right step the lowest; the three blocks are thick rounded toy blocks in magical violet (#8E5BD6) with a satin finish, with a thin brass trim (#C99A1E) along their top edges. The trophy is clearly the main element and takes most of the height; the podium is short and wide under it. One or two tiny bright four-pointed glints sit directly on the gold. No numbers on the podium steps, no text, no ribbons with writing, no laurel wreath, no figures, no characters, nothing floating on the background.
```

**Critères de réussite** : on lit « trophée » en une seconde ; coupe dorée épaisse avec deux
anses et une étoile ; podium violet à trois marches **sans chiffres** ; fond lavande uni ; même
rendu que l'icône Boutique.

**Corrections fréquentes**
- *« Remove the numbers from the podium steps: plain violet blocks only. »*
- *« Make the trophy bigger and the podium shorter: the cup must be the main shape. »*
- *« Keep the plain flat light lavender background, no floor and no shadow. »*

---

### Image 2 — `art/icones/medaille_or.png` (podium du Classement, 1re place)

- **Format** : carré 1:1, fond uni `#E6E1EE`. À la suite, même conversation.
- **Utilisation** : 88 px au-dessus de la plus haute marche du podium (USS). Les 3 médailles
  doivent être **identiques**, seul le métal change.

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object.
OBJECT: a chunky cartoon toy medal: a thick round disc with a raised rounded rim, in polished shiny gold (#FFC83A), metallic, with a bright highlight in the upper left, and one big chunky five-pointed star in relief in the center of the disc. The medal hangs from a short, wide, folded ribbon above it, shaped like a V with two thick tails, in magical violet (#8E5BD6) with one off-white (#F4EEE6) stripe down the middle of each tail, satin finish. The medal faces the viewer at the same three-quarter angle. No number, no letter, no text, no laurel, no face, no portrait, no engraving other than the star, nothing floating on the background.
```

**Critères de réussite** : médaille dorée épaisse, étoile en relief, ruban violet en V à rayure
blanc cassé ; **aucun chiffre**.

**Correction fréquente** : *« Remove the number 1 from the medal: only the star. »*

---

### Image 3 — `art/icones/medaille_argent.png` (2e place)

```
Same image as the previous one: same medal shape and size, same star, same violet and off-white ribbon, same framing, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the metal of the disc.
OBJECT: the disc and its star are now polished silver (#C9CED8), shiny and mirror-like, with darker violet-grey reflections on the rim and around the star so that it reads clearly against the light background; bright highlight in the upper left. Not white, not blue chrome, no number, no text.
```

**Critères de réussite** : argent bien détaché du fond (reflets sombres), même médaille.

**Correction fréquente** : *« Add darker reflections on the silver so it does not blend into
the background. »*

---

### Image 4 — `art/icones/medaille_bronze.png` (3e place)

```
Same image as the previous one: same medal shape and size, same star, same violet and off-white ribbon, same framing, same angle, same lighting and same plain light lavender background (#E6E1EE). Only change the metal of the disc.
OBJECT: the disc and its star are now warm polished bronze (#B0703A), metallic, with a soft warm highlight in the upper left. Clearly darker and more orange-brown than gold, not copper pink, not brown wood, no number, no text.
```

**Critères de réussite** : bronze chaud, nettement différent de l'or ; les trois médailles
côte à côte se distinguent au premier coup d'œil.

---

### Image 5 — `art/icones/icone_musique.png` (ligne « Musique » de la pop-up Paramètres)

- **Utilisation** : 64 px dans une carte `#3D3270`, à gauche du libellé « Musique ».

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object.
OBJECT: a chunky cartoon toy music symbol: two joined eighth notes (two round note heads tilted slightly, two thick stems and one thick bar joining their tops), as a thick solid 3D object with rounded edges, in glossy candy pink (#FF6FB5) with a soft white highlight in the upper left and a thin brass trim (#C99A1E) along the joining bar. Standing upright, its front facing the viewer at the same three-quarter angle. No staff lines, no other notes, no text, no sparkles on the background.
```

**Critères de réussite** : double croche épaisse, rose bonbon brillant ; l'espace entre les deux
notes reste du fond (détourage).

---

### Image 6 — `art/icones/icone_effets.png` (ligne « Effets » de la pop-up Paramètres)

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object.
OBJECT: a chunky cartoon toy loudspeaker: a small rounded box in fresh mint green (#3CC48A) with a satin finish, with a big round brass speaker cone (#C99A1E) on its front facing the viewer at the same three-quarter angle, a darker round center cap on the cone, and two thick curved off-white (#F4EEE6) sound-wave arcs on the right side of the cone, very close to it, as solid rounded 3D shapes. No buttons with symbols, no text, no notes, no cables, no sparkles on the background.
```

**Critères de réussite** : on lit « son / haut-parleur » ; vert menthe + laiton ; les deux arcs
épais et proches du haut-parleur.

**Correction fréquente** : *« Make the two sound-wave arcs thicker and closer to the speaker. »*

---

### Image 7 — `art/icones/icone_parametres.png` (bouton « Paramètres », roue dentée) — **fond bleu-gris**

- **Format** : carré 1:1, **fond uni `#5B6380`** (exception, §0 point 5). Toujours en dernier.
- **Utilisation** : 52 px dans le petit bouton rond ardoise à côté du badge d'étage (elle tourne
  de 90° au survol) et 120 px dans l'en-tête de la pop-up Paramètres. **Vue presque de face**
  (pour qu'elle tourne joliment sur elle-même).

```
Same image as the previous one: same style, same lighting and same render quality. Only change the object, the angle AND the background color.
OBJECT: a chunky cartoon toy cogwheel (settings gear), seen almost from the front, tilted only very slightly (about 10 degrees) so that its thickness is visible: a thick round wheel with eight wide, short, rounded teeth evenly spaced around it, and a round hole in the center surrounded by a raised brass hub ring (#C99A1E). The wheel is polished silver (#C9CED8), shiny, with soft darker violet-grey reflections on its faces and teeth so its shape reads clearly; bright highlight in the upper left. Thick and solid like a toy, soft bevels on every edge. Through the center hole the plain background is visible. No text, no numbers, no screws, no second gear, no sparkles on the background.
BACKGROUND: plain flat solid medium blue-grey (#5B6380), no gradient, no floor, no scenery, no cast shadow on the background.
```

**Critères de réussite** : roue dentée épaisse à **8 dents arrondies**, argent clair bien
détaché du fond bleu-gris, moyeu laiton, **trou central** vide ; presque de face.

**Corrections fréquentes**
- *« Fewer, wider and rounder teeth: eight teeth in total. »*
- *« Show it more from the front, only slightly tilted. »*

---

### Prompt de relance (conversation E)

Si les images dérivent (autre angle, autre fond, style qui change) : nouvelle conversation,
joindre **`art/icones/icone_boutique.png`** et **`art/icones/icone_classement.png`** (si elle
est validée ; sinon `icone_ameliorations.png`), coller le prompt de l'**image 1** en remplaçant
sa première phrase par *« Use the attached images as the exact reference for the style, the
size in the frame, the angle, the lighting and the plain light lavender background. Draw a
different object. »* et sa dernière ligne `OBJECT: …` par la ligne `OBJECT: …` de l'image
voulue. Pour une médaille d'argent ou de bronze, recopier la ligne `OBJECT: …` de la
**médaille d'or** en remplaçant seulement le métal. Pour la roue dentée, ajouter aussi sa ligne
`BACKGROUND: …` et la phrase « seen almost from the front ».

---

## 3. Récapitulatif — ordre de génération

| # | Conversation Gemini | Fichier | Format | Fond |
|---|---|---|---|---|
| 1 | E — Interface v4 (joindre `icone_boutique.png`, `icone_ameliorations.png`) | `art/icones/icone_classement.png` | carré 1:1 | `#E6E1EE` |
| 2 | E | `art/icones/medaille_or.png` | carré 1:1 | `#E6E1EE` |
| 3 | E | `art/icones/medaille_argent.png` | carré 1:1 | `#E6E1EE` |
| 4 | E | `art/icones/medaille_bronze.png` | carré 1:1 | `#E6E1EE` |
| 5 | E | `art/icones/icone_musique.png` | carré 1:1 | `#E6E1EE` |
| 6 | E | `art/icones/icone_effets.png` | carré 1:1 | `#E6E1EE` |
| 7 | E (en dernier) | `art/icones/icone_parametres.png` | carré 1:1 | **`#5B6380`** |

**Priorité** si le temps manque : 1 (Classement) et 7 (Paramètres) sont **indispensables**
(boutons du HUD) ; 2 à 4 servent au podium (sans elles, le programmeur peut afficher des disques
USS or / argent / bronze) ; 5 et 6 sont du confort (la pop-up Paramètres marche sans).

**Gardés tels quels** : `icone_boutique.png`, `icone_inventaire.png`, `icone_index.png`,
`icone_ameliorations.png`, la pièce, le logo, toutes les autres icônes.

**Pas d'image** (USS, art bible §12) : boutons, cadres et en-têtes de pop-ups, croix de
fermeture, onglets, barre de défilement, interrupteurs, badge d'étage, compteurs, toasts,
marches du podium.

**Après génération**
1. Détourer les 7 icônes → `art/icones_detourees/<même nom>.png`. Vérifier le trou et les creux
   de la roue, les anses du trophée, le ruban des médailles, l'espace entre les deux notes.
2. Copier les icônes détourées dans `TourDeLaChance-Unity/Assets/Art/icones_detourees/`.
3. Contrôle rapide dans le jeu : chaque icône réduite à 52 px sur le disque `#3B2F66` doit
   rester reconnaissable ; le trophée doit se lire sur l'en-tête or `#FFD54A` de la pop-up
   Classement ; la roue dentée doit rester propre quand elle tourne (pas de reste de fond).
