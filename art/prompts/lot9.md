# Lot 9 — Prompts d'images Gemini (V7 : Ascension festive, packs « Bientôt disponible »)

> **À quoi sert ce fichier ?** Chaque bloc ci-dessous = **une image**. On copie le prompt
> (le texte en anglais dans le cadre gris) et on le colle tel quel dans Gemini, dans le
> navigateur. On enregistre ensuite l'image sous le **nom de fichier indiqué**.
>
> Les prompts sont en anglais parce que les générateurs d'images le comprennent mieux.
> Les explications autour sont en français.
>
> Référence : `docs/art-bible.md` **§17** (où et à quelle taille chaque image s'affiche), §9
> (règles des images).
>
> **Pas dans ce lot** :
> - les **3 nuages** qui dérivent dans L'Ascension (`art/ascension/nuage_1..3.png`) : rendus dans
>   **Blender** par l'artiste 3D, pour qu'ils aient exactement le rendu des nuages des dalles
>   (art bible §17.2.11) ;
> - les rayons, projecteur, confettis, étincelles, rayures de la barre, médaillons, cadenas :
>   **dessinés par le code** (§17.6).

> **Fait le 13/11/2026 : les images 1 à 4 ont été RENDUES DANS BLENDER** (plus fiable que Gemini,
> même style « jouet verni », transparence exacte, aucun téléchargement) :
> `blender/objets_lot9.py` (les objets), `blender/rendu_lot9.py` (guirlande et ruban, de face),
> `blender/rendu_icones.py` (sablier et 4e pack), `blender/rendu_dalles.py` avec `NUAGES=1`
> (les 3 nuages). Les prompts ci-dessous restent en secours. Seule l'image 5 (kraken) reste à
> faire avec Gemini.

---

## 0. Mode d'emploi (à lire une fois)

1. **Deux conversations Gemini** :
   - **K — Décorations de fête** (images 1 et 2, fond lavande) : joindre au premier message
     (bouton « + ») `art/icones/drapeau_record.png` et `art/icones/icone_boutique.png` (le
     drapeau a les mêmes couleurs de fête ; l'échoppe montre le rendu « jouet verni »).
   - **L — Bientôt disponible** (images 3 et 4) : joindre au premier message
     `art/icones/pack_diamants_coffre.png` et `art/icones/pack_diamants_sac.png` (l'image 4 doit
     être de la **même famille** que ces packs).
2. **Le premier prompt de chaque conversation est long, le suivant est court** (*« Same style as
   the previous image… »*) : c'est ce qui garde le même rendu.
3. **Fonds** (pour un détourage propre, art bible §9.2) :
   - images 1, 2, 3 : **uni lavande très clair `#E6E1EE`** ;
   - image 4 : **uni bleu-gris moyen `#5B6380`**, comme les autres packs (les facettes claires
     des diamants disparaîtraient sur le lavande).
4. **Aucun texte, aucun chiffre, aucun logo**, aucun personnage.
5. **Détourage** : `blender/detourer_icones.py` (il ajoute le contour noir épais), résultat dans
   `art/icones_detourees/` (ou le dossier équivalent pour `art/ui/`). **Attention aux détourages**
   (mandat V7, point 7) : vérifier qu'il ne reste pas de lavande coincé entre les fanions et la
   corde (image 1), sous les queues du ruban (image 2), dans le creux du sablier (image 3).

**Ce qui doit être identique entre les images de ce lot et les lots précédents** : la lumière
(blanche chaude en haut à gauche, remplissage violet doux, liseré lavande `#B48CFF` derrière), le
rendu « jouet verni » à gros biseaux, les couleurs pleines sans texture, le fond uni sans ombre
portée. Les icônes (3, 4) : vue de 3/4 (~25° au-dessus, tourné ~20° vers la gauche), objet seul
centré à ~70 %. Les décorations (1, 2) : vues **de face**, pour se poser à plat dans l'interface.

---

## 1. Conversation K — Décorations de fête

### Image 1 — `art/ui/guirlande_fanions.png`

- **Format** : **large 4:1** (par exemple 1600 × 400), fond uni `#E6E1EE`.
- **Où** : accrochée au-dessus de chaque **dalle de palier** de L'Ascension, affichée
  **640 × 160** (§17.2.6). Elle se balance doucement (code). Elle doit se lire sur le bleu de
  jour, le corail, la lavande et la nuit.
- **Idée** : une guirlande de fanions de fête foraine, en arc doux, aux couleurs du jeu.

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

DECORATION FORMAT: a wide 4:1 game interface decoration, seen straight from the front, perfectly centered, filling the full width with a small equal margin on the left and right, nothing cropped. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a festive bunting garland: one thick rounded rope (#C99A1E brass-colored, as thick as a finger relative to the flags) hanging in a gentle shallow arc from the top left corner to the top right corner, its lowest point in the middle at about 45 percent of the image height. Nine chunky triangular pennant flags hang from the rope, evenly spaced, pointing down, each one thick like a small painted wooden toy with softly bevelled edges, all the same size, slightly turned at different small angles as if moving in a light breeze. Flag colors in this exact order from left to right: candy pink (#FF5FA2), sunny yellow (#FFD54A), sky blue (#3BC8FF), fresh green (#6BE34A), violet (#A35CE0), candy pink, sunny yellow, sky blue, fresh green. Each flag has one small round white dot near its top. Clear gaps between the flags so the background shows between them. Two small round brass knots at both ends of the rope. No stars, no lights, no text, no letters, no numbers, nothing else.
```

**Critères de réussite**
- 9 fanions épais, de la même taille, nettement séparés ; la corde est **épaisse** (une corde
  fine se casse au détourage et disparaît à 640 px).
- Les deux bouts de la corde touchent presque les coins hauts ; rien n'est coupé.
- Pas de rouge vif (le rouge est réservé à la croix) : le rose `#FF5FA2` doit rester rose.

**Corrections fréquentes**
- *« Make the rope much thicker and the flags chunkier, like painted wooden toys. »*
- *« Keep exactly nine flags, all the same size, with clear gaps between them. »*
- *« Seen straight from the front, flat in the image, no perspective. »*

---

### Image 2 — `art/ui/ruban_palier.png` (facultative)

- **Format** : **large 3:1** (par exemple 1200 × 400), fond uni `#E6E1EE`. À la suite, même
  conversation.
- **Où** : au premier étage de chaque palier d'altitude de L'Ascension (1, 4, 7, 10), avec le
  nom du palier **écrit par le code** dessus (« Dans les nuages ! », 30 px `#3A2200`). Affiché en
  **9-slice** (bords de 120 px gardés, milieu étiré) à **88 px** de haut (§17.2.6). La **bande du
  milieu doit donc être droite et unie**, sans pli ni motif : c'est elle qu'on étire.

```
Same style, same lighting and same plain very light lavender background (#E6E1EE) as the previous image. Same wide front-facing decoration format, this time 3:1, filling the full width with a small equal margin, nothing cropped. Only change the subject.
SUBJECT: a blank festive ribbon banner seen straight from the front, flat in the image: one long perfectly straight horizontal band in the middle, plain shiny gold-yellow (#FFC83A), with a slightly lighter top edge (#FFE38A) and a slightly darker bottom edge (#D99A1A), completely empty and smooth in its whole central part (no folds, no seams, no pattern, no text). At each end, the band folds back behind itself into a short darker tail (#D99A1A, shaded #B07A10) that ends in a forked swallow-tail cut, the tails slightly lower than the band. Chunky and thick like a painted wooden toy, soft bevels. No stars, no ornaments, no text, no letters, no numbers, nothing else.
```

**Critères de réussite**
- La bande du milieu est **droite, unie et vide** sur au moins 60 % de la largeur.
- Les deux queues sont symétriques et ne dépassent pas les 120 premiers / derniers pixels de
  l'image ramenée à 1200 px de large (sinon le 9-slice les étire).

**Correction fréquente** : *« The central band must be perfectly straight and plain, with no
folds and no curve: only the two ends fold back. »*

---

## 2. Conversation L — Bientôt disponible

### Image 3 — `art/icones/sablier_bientot.png`

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **Où** : sur le badge « Bientôt disponible » de la section des packs de diamants, à **72 px**,
  posé sur une pilule **gris-lavande `#8F89A6`** (§17.3.5). L'icône doit être **chaleureuse**
  (bois et laiton) pour réchauffer le gris, et lisible en petit.

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a chunky toy hourglass, slightly tilted to the right as if just turned over: two thick round caps of warm painted wood (#8C5933, darker edges #6B4226) on top and bottom, joined by three thick round brass pillars (#C99A1E). Between them, two plump rounded glass bulbs, glossy and very light lavender-white (#F4F0FF) with a soft white highlight, not fully transparent. The upper bulb is half full and the lower bulb is a quarter full of bright aqua sand (#4FD8F0, shaded #1E8FC0), with a thin aqua stream falling through the narrow neck. One tiny four-pointed white glint on the upper bulb. Big simple shapes readable at 60 pixels. No clock face, no numbers, no text, nothing floating on the background.
```

**Critères de réussite**
- Un sablier reconnaissable à 60 px ; le sable **aqua** (couleur du diamant) relie l'icône aux
  packs de diamants.
- Le verre n'est **pas** transparent au point de laisser voir le fond lavande (sinon le
  détourage le perce) : il est blanc-lavande opaque et brillant.

**Correction fréquente** : *« Make the glass bulbs opaque glossy lavender-white, not see-through. »*

---

### Image 4 — `art/icones/pack_diamants_tresor.png` (4e pack, le plus gros)

- **Format** : carré 1:1, fond uni **`#5B6380`** (comme les autres packs du lot 5). À la suite,
  même conversation, **en rappelant** les deux packs joints.
- **Où** : 4e carte de la vitrine « Bientôt disponible » (§17.3.5), à 140 px, à côté de
  `pack_diamants_petit`, `_sac` et `_coffre`. Aujourd'hui le 4e pack réutilise l'image du
  coffre : il lui faut la sienne, **nettement plus généreuse** que le coffre.
- Spec v5 : « un gros tas de diamants avec une couronne de lumière ». Pas de couronne de roi
  (ce serait un accessoire du jeu) : une **auréole de lumière** dorée au-dessus du tas.

```
Now a new icon in the exact same style, angle, lighting, glossy toy finish and gem design as the two attached diamond pack icons. Same square 1:1 format, the object fills about 70 percent of the image height, centered, nothing cropped. Plain flat solid medium blue-grey background (#5B6380), no gradient, no floor, no scenery, no cast shadow on the background.
SUBJECT: a big generous mountain of the same chunky aqua gemstones as in the attached icons (same brilliant cut with big simple facets, same colors #4FD8F0, lighter facets #C8F6FF, deeper facets #1E8FC0, same size for every gem), about twenty gems heaped into a wide rounded pile on a low round toy pedestal of light blue-grey stone (#8A90B0, darker edge #5B6380) with a thin brass rim (#C99A1E). Above the top of the pile floats a thick glowing golden ring of light (#FFC83A, bright center #FFE38A), like a halo, with four small four-pointed white glints around it. Every gem is clearly separated with soft contact shadows. Only aqua gems, no gold coins, no gems of other colors, no crown, no chest, no bag, no text, no numbers, no price tag, nothing else floating on the background.
```

**Critères de réussite**
- Visiblement **plus** que le coffre (`pack_diamants_coffre.png`), avec des gemmes **identiques**
  aux trois autres packs (même taille, même taille de facettes).
- L'auréole se détache du tas et ne se confond pas avec une couronne.

**Corrections fréquentes**
- *« Gems must be exactly the same size and cut as in the attached icons. »*
- *« The golden ring must float clearly above the pile, like a halo of light, not a crown. »*

---

## 3. Récapitulatif

| # | Conv. | Fichier | Format | Fond | Priorité |
|---|---|---|---|---|---|
| 1 | K | `art/ui/guirlande_fanions.png` | large 4:1, de face | `#E6E1EE` | haute (paliers festifs) |
| 2 | K | `art/ui/ruban_palier.png` | large 3:1, de face, milieu uni (9-slice) | `#E6E1EE` | facultative |
| 3 | L | `art/icones/sablier_bientot.png` | carré 1:1, 3/4 | `#E6E1EE` | haute (packs) |
| 4 | L | `art/icones/pack_diamants_tresor.png` | carré 1:1, 3/4 | `#5B6380` | moyenne |

**À rendre dans Blender (artiste 3D, art bible §17.2.11)** : `art/ascension/nuage_1.png`,
`nuage_2.png`, `nuage_3.png` (nuages blancs, contour encre, fond transparent).

**Après génération**
1. Ranger l'image avec `ranger.sh`, puis détourer → contour noir épais ajouté.
2. Comparer le détouré à l'original (liserés, trous, ombres entourées de noir).
3. Copier dans Unity : icônes → `Assets/Art/icones_detourees/` ; décorations →
   `Assets/UI/Ascension/` (import : *Alpha Is Transparency*, *Clamp*, *Bilinear* ; pour le ruban,
   régler les bords 9-slice à 120 px dans le *Sprite Editor* ou en USS).
4. Contrôle dans le jeu : la guirlande se lit sur les 4 ciels de L'Ascension ; le sablier se
   lit à 72 px sur `#8F89A6` ; le 4e pack est clairement le plus gros des quatre.

---

## 4. Conversation M — Icône à refaire après la revue des détourages (V7)

### Image 5 — `art/icones/kraken_bebe.png` (Petit Kraken, SECRET) — remplace l'image du lot 6

**Pourquoi la refaire ?** Le bébé kraken est **violet**, posé sur un fond lavande avec une **ombre
portée violette** : le détourage ne peut pas séparer l'objet de son ombre (mêmes couleurs, bords
doux). Il reste une tache d'ombre sous les tentacules (`docs/captures/qa_v7_detourage_10.png`).
On la régénère sur le **fond bleu-gris** des objets difficiles (bible §9.2) et **sans aucune ombre**.

- **Format** : carré 1:1, fond uni `#5B6380`.
- **À faire** : nouvelle conversation, coller le prompt complet de l'image 13 du lot 6
  (`art/prompts/lot6.md`), en remplaçant **seulement** la dernière phrase du bloc ICON FORMAT par :

```
Plain flat solid medium blue-grey background (#5B6380), perfectly uniform, no gradient, no vignette, no floor, no scenery. Absolutely no shadow of any kind on the background: no cast shadow, no contact shadow, no ambient occlusion under the tentacles; the kraken floats on the flat color.
```

**Critères de réussite** : ceux de l'image 13 du lot 6, plus : fond bleu-gris parfaitement uni,
**aucune ombre** sous l'objet, tentacules nettement séparés du fond.

**Après génération** : `ranger.sh`, puis
`ICONES="kraken_bebe" .../Blender --background --python blender/detourer_icones.py`, puis
**retirer** la ligne `"kraken_bebe.png"` de `REGLAGES` dans `blender/detourer_icones.py` (elle ne
sert qu'à l'ancienne image) et comparer avec `blender/planche_detourage.py`.
