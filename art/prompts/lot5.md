# Lot 5 — Prompts d'images Gemini (v5 : Rebirth, Diamant, packs de diamants)

> **À quoi sert ce fichier ?** Chaque bloc ci-dessous = **une image**. On copie le prompt
> (le texte en anglais dans le cadre gris) et on le colle tel quel dans Gemini, dans le
> navigateur. On enregistre ensuite l'image sous le **nom de fichier indiqué**.
>
> Les prompts sont en anglais parce que les générateurs d'images le comprennent mieux.
> Les explications autour sont en français.
>
> Références : `docs/art-bible.md` §12.17 (boutons ronds v5 : où l'icône Rebirth est affichée),
> §9 (règles des images), §2 (palette).
>
> **Ce lot est partiel, et c'est voulu.** Les icônes des nouveaux objets (boîtes Pirates et
> Animaux) et celles des accessoires refaits (art bible §13.9, point 5) viendront dans un lot
> suivant, quand le catalogue du game designer et les modèles Blender v5 seront prêts. Ici : ce
> qui ne dépend pas de ce catalogue.

---

## 0. Mode d'emploi (à lire une fois)

1. **Deux conversations Gemini** :
   - **F — Rebirth** (2 images, fond lavande) : joindre au **premier** message (bouton « + »)
     `art/icones/icone_boutique.png` et `art/icones/icone_classement.png`.
   - **G — Diamants** (4 images, fond bleu-gris) : joindre au premier message
     `art/icones/piece.png` (la pièce : la monnaie « sœur » du diamant) et
     `art/icones/icone_boutique.png`.
2. **Le premier prompt de chaque conversation est long, les suivants sont courts** (*« Same
   image as the previous one… only change… »*) : c'est la méthode qui garde le même rendu.
3. **Fonds** (pour un détourage propre, art bible §9.2) :
   - icônes Rebirth : **uni lavande très clair `#E6E1EE`** (comme Boutique et Classement) ;
   - diamant et packs : **uni bleu-gris moyen `#5B6380`**. Pourquoi : les facettes claires du
     diamant (`#C8F6FF`) sont presque aussi claires que le lavande ; le détourage mangerait leurs
     bords. Sur le bleu-gris, elles se détachent nettement (même règle que pour `coffre_diamant`).
4. **Où ces images s'affichent** :
   - `icone_rebirth` : **seule, en grand** (~80 px visibles) dans un bouton rond à face violet
     sombre `#3A3358` cerclé d'un anneau « flamme » `#EF6A32` (art bible §12.17). Elle doit donc
     se lire **sur du sombre**, sans texte à côté : silhouette simple, pas d'objet noir.
   - `diamant` : à côté des chiffres (compteur de diamants, prix), parfois **petite (32 à
     40 px)** : silhouette de gemme reconnaissable même minuscule.
   - packs : cartes de la future boutique (~112 à 160 px).
5. **Aucun texte, aucun chiffre, aucun prix** sur les images. Rappel de la **règle d'or** : les
   diamants sont une monnaie **du jeu**, gagnée en jouant ; jamais de symbole d'argent réel
   (`$`, `€`, billets, cartes bancaires), jamais de bandeau « promo » ou « meilleure offre ».
6. **Ne rien copier** : pas la monnaie d'un jeu connu (ni la monnaie hexagonale de Roblox, ni une
   gemme verte ou rouge emblématique d'un autre jeu), pas de logo de phénix d'une marque ou d'une
   équipe.
7. **Enregistrement** : PNG, nom exact (minuscules, sans accent, tirets bas), dans `art/icones/`.
8. **Détourage** : comme aux lots précédents (Aperçu sur Mac → « Supprimer l'arrière-plan » ou
   `blender/detourer_icones.py`), résultat dans `art/icones_detourees/` avec le même nom.
   Vérifier : le **creux au centre** de la flèche circulaire (Rebirth), les **pointes** des
   diamants, l'ouverture du **sac** et l'espace sous le **couvercle** du coffre.

---

## 1. Blocs communs

**Bloc de style commun** (inchangé depuis le lot 1, art bible §9.1) — déjà inclus dans les
premiers prompts :

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.
```

**Bloc « icône »** (identique aux lots 1 à 4 ; pour la conversation G, la dernière phrase est
remplacée par le fond bleu-gris, voir le prompt) :

```
ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.
```

**Ce qui doit être identique dans tout le lot** : la lumière (chaude en haut à gauche, liseré
lavande derrière), l'angle (3/4, ~25° au-dessus, tourné ~20° vers la gauche), l'objet seul et
centré à ~70 % du cadre, le fond uni sans ombre portée, le rendu « jouet verni » des icônes
jointes. Les étincelles éventuelles sont **posées sur l'objet**, jamais flottantes sur le fond.
**Dans la conversation G** : le **même diamant** (forme, taille des facettes, couleurs) dans
les 4 images ; seuls la quantité et le contenant changent.

---

## 2. Conversation F — Icône Rebirth (fond lavande)

### Image 1 — `art/icones/icone_rebirth.png` (bouton rond « Rebirth », colonne droite)

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **Utilisation** : bouton rond de 120 px sous le Classement, icône seule, sur face sombre
  `#3A3358` avec anneau `#EF6A32` (art bible §12.17.2). Idée : **« on recommence, en plus fort »**
  → une flèche circulaire (on refait un tour) et une étoile qui renaît au centre.
- **À faire** : nouvelle conversation, **joindre `art/icones/icone_boutique.png` et
  `art/icones/icone_classement.png`**, puis coller :

```
Use the attached images as the exact reference for the style, the size in the frame, the angle, the lighting and the plain light lavender background. They are two of our existing game icons; draw a new object that clearly belongs to the same set.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

OBJECT: a chunky cartoon toy "rebirth" symbol standing upright: one very thick circular arrow that makes almost a full loop (about 300 degrees, clockwise), with a big rounded arrowhead, like a thick rounded toy ring, in glossy candy flame orange (#EF6A32) with a lighter warm orange (#FF9A66) highlight on its upper left edge and a thin brass trim (#C99A1E) along its outer edge. In the middle of the loop, a big chunky five-pointed star in polished shiny gold (#FFC83A), slightly tilted and rising upward, with a short rounded flame-shaped tail under it in warm orange (#F28C38), as if the star is being reborn and flying up. The star is large, filling most of the inside of the loop, and overlaps the top of the ring a little. One or two tiny bright four-pointed glints sit directly on the gold star. Thick and solid like a toy, soft bevels on every edge. No text, no numbers, no letters, no recycling symbol, no clock hands, no characters, nothing floating on the background.
```

**Critères de réussite** : on lit « recommencer / renaître » en une seconde ; flèche circulaire
épaisse orange-flamme + grosse étoile dorée au centre ; formes épaisses, rien de fin ; même rendu
que l'icône Boutique ; lisible réduite à 80 px sur du violet sombre (vérifier en posant l'image
détourée sur un carré `#3A3358`).

**Corrections fréquentes**
- *« Make the arrow thicker and rounder, like a chunky toy ring, and the star bigger. »*
- *« Remove the recycling look: one single circular arrow only, no second arrow. »*
- *« Keep the plain flat light lavender background, no floor and no shadow. »*

---

### Image 2 (facultative) — `art/icones/icone_rebirth_b.png` (variante « phénix »)

- **Format** : carré 1:1, fond uni `#E6E1EE`. À la suite, même conversation.
- **Pourquoi une variante ?** Si la flèche circulaire fait trop « bouton actualiser », le chef de
  projet peut choisir ce petit phénix jouet. On garde **une seule** des deux dans le jeu (le
  fichier choisi est renommé `icone_rebirth.png`).

```
Same image as the previous one: same style, same angle, same lighting and same plain light lavender background (#E6E1EE); the object fills about 70 percent of the image height. Only change the object.
OBJECT: a chunky cartoon toy phoenix, cute and round, rising upward with its two wings spread wide and raised in a V shape. Its body is a plump rounded teardrop in glossy flame orange (#EF6A32); each wing is made of three thick rounded flame-shaped feathers, orange (#EF6A32) at the root and shiny gold (#FFC83A) at the tips; its tail is three short thick rounded flame curls in warm orange (#F28C38) curling downward. Small round head with a tiny rounded gold beak and two small glossy black eyes with a white highlight, friendly expression. A round brass gem setting (#C99A1E) with a small gold star in its center on its chest. Thick and solid like a toy, soft bevels, no thin feathers, no sharp points. No text, no letters, no fire particles on the background, nothing floating on the background, no resemblance to any existing logo or mascot.
```

**Critères de réussite** : un oiseau de flammes **mignon et épais** (pas un aigle héraldique, pas
un logo), ailes en V bien ouvertes ; lisible à 80 px.

**Correction fréquente** : *« Make it rounder, cuter and chunkier, like a toy, with fewer and
thicker feathers. »*

---

## 3. Conversation G — Le Diamant et les packs (fond bleu-gris)

### Image 3 — `art/icones/diamant.png` (icône de la monnaie Diamant)

- **Format** : carré 1:1, **fond uni bleu-gris `#5B6380`**.
- **Utilisation** : compteur de diamants, prix, récompenses ; de 32 px (à côté d'un chiffre) à
  120 px. **Silhouette ultra simple** : une gemme taillée « brillant », large table sur le dessus,
  pointe en bas.
- **Couleur** : bleu aqua `#4FD8F0`, facettes claires `#C8F6FF`, facettes sombres `#1E8FC0`.
  Assez différent de l'or des pièces (on ne confond jamais les deux monnaies) et du bleu
  « glacier » des gemmes du coffre Diamant `#9FE3FF` (plus pâle).
- **À faire** : nouvelle conversation, **joindre `art/icones/piece.png` et
  `art/icones/icone_boutique.png`**, puis coller :

```
Use the attached images as the exact reference for the style, the size in the frame, the angle and the lighting. The gold coin is our existing in-game currency; draw its sister currency, a gem, that clearly belongs to the same set. Use a different background color, given below.

STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid medium blue-grey background (#5B6380), no gradient, no floor, no scenery, no cast shadow on the background.

OBJECT: one chunky cartoon toy gemstone in a classic brilliant cut, seen from slightly above: a wide flat top facet, a short crown of a few large facets, and a deeper pointed bottom; only big, simple facets (about 8 around), like a hard candy gem toy, thick and solid with softly rounded facet edges. Bright aqua blue (#4FD8F0), with lighter facets (#C8F6FF) on the upper left facing the light and deeper facets (#1E8FC0) on the lower right, glossy and clear-looking but not transparent, one bright white glint on the top facet and one tiny four-pointed glint sitting on its upper left edge. No setting, no ring, no chain, no second gem, no text, no numbers, nothing floating on the background.
```

**Critères de réussite** : une gemme **épaisse à grosses facettes**, bleu aqua, lisible réduite à
32 px (vérifier en réduisant l'image) ; bien détachée du fond bleu-gris ; même rendu « jouet
verni » que la pièce.

**Corrections fréquentes**
- *« Fewer and bigger facets: it must read as a gem even when very small. »*
- *« Make the darker facets deeper blue so the gem stands out from the background. »*
- *« Keep the plain flat medium blue-grey background (#5B6380), no floor and no shadow. »*

---

### Image 4 — `art/icones/pack_diamants_petit.png` (petit tas)

- **Format** : carré 1:1, fond uni `#5B6380`. À la suite, même conversation.
- **Utilisation** : carte de la future boutique (le plus petit pack).

```
Same image as the previous one: same style, same angle, same lighting and same plain medium blue-grey background (#5B6380); the group fills about 70 percent of the image height. Only change the object.
OBJECT: a small heap of exactly five of the same chunky aqua gems as in the previous image (same cut, same size, same colors #4FD8F0, #C8F6FF, #1E8FC0): three gems lying side by side at the bottom, tilted at different angles, one resting on top of them, and one leaning against the heap in front. All gems the same size and clearly separated, with soft contact shadows between them only. One or two tiny four-pointed glints sitting on the gems. No container, no coins, no price tag, no ribbon, no text, no numbers, nothing floating on the background.
```

**Critères de réussite** : 5 gemmes identiques au diamant de l'image 3, en petit tas.

---

### Image 5 — `art/icones/pack_diamants_sac.png` (sac de diamants)

- **Format** : carré 1:1, fond uni `#5B6380`. À la suite, même conversation.
- **Utilisation** : pack moyen. Le sac est en **violet magique** (couleur de la boîte de skins),
  qui se détache bien du fond bleu-gris et met en valeur le bleu aqua.

```
Same image as the previous one: same style, same angle, same lighting and same plain medium blue-grey background (#5B6380); the object fills about 70 percent of the image height. Only change the object.
OBJECT: a chunky round toy drawstring pouch, plump and soft, in magical violet (#8E5BD6) with a satin finish, its neck gathered and tied with a thick rounded brass cord (#C99A1E) ending in two round brass beads. The top of the pouch is open and overflowing with many of the same chunky aqua gems as before (same cut and colors #4FD8F0, #C8F6FF, #1E8FC0), piled above the opening, and three gems have tumbled out and rest on the ground in front of the pouch. Same gem size as in the previous images, so the pouch looks bigger than the small heap. No symbol, no emblem, no dollar sign, no text, no numbers, no price tag, nothing floating on the background.
```

**Critères de réussite** : sac violet dodu, cordon laiton, gemmes identiques qui débordent ;
**aucun symbole** sur le sac (surtout pas de `$`).

**Correction fréquente** : *« Remove any symbol from the pouch: plain violet fabric only. »*

---

### Image 6 — `art/icones/pack_diamants_coffre.png` (coffre de diamants)

- **Format** : carré 1:1, fond uni `#5B6380`. À la suite, même conversation.
- **Utilisation** : le plus gros pack. Coffre **dans le style des coffres du jeu** (bois, ferrures
  sombres, serrure laiton), couvercle ouvert, rempli de diamants (pas de pièces d'or : on ne mélange
  pas les deux monnaies).

```
Same image as the previous one: same style, same angle, same lighting and same plain medium blue-grey background (#5B6380); the object fills about 70 percent of the image height. Only change the object.
OBJECT: a small chunky toy treasure chest with its domed lid wide open, tilted back: body in warm wood (#8C5933) made of thick planks separated by grooves, darker wood lid (#6B4226), two dark metal bands (#4A4E5A) across the front and the lid, and a big shield-shaped brass lock plate (#C99A1E) on the front. The chest is overflowing with a big mound of the same chunky aqua gems as before (same cut and colors #4FD8F0, #C8F6FF, #1E8FC0), heaped high above the rim, and four gems have spilled onto the ground in front of the chest. Two or three tiny four-pointed glints sit on the gems. Only gems inside, no gold coins, no jewels of other colors, no skull, no text, no numbers, no price tag, nothing floating on the background.
```

**Critères de réussite** : coffre jouet reconnaissable (même famille que les coffres du jeu),
débordant de gemmes aqua identiques ; nettement « plus » que le sac.

**Correction fréquente** : *« Only aqua gems inside the chest, no gold coins. »*

---

### Prompts de relance

- **Conversation F** : nouvelle conversation, joindre `art/icones/icone_boutique.png` et
  `art/icones/icone_classement.png`, coller le prompt complet de l'**image 1** ; pour la variante,
  remplacer sa dernière ligne `OBJECT: …` par celle de l'image 2.
- **Conversation G** : nouvelle conversation, joindre `art/icones/piece.png` **et
  `art/icones/diamant.png`** (s'il est validé), coller le prompt complet de l'**image 3** en
  remplaçant sa première phrase par *« Use the attached images as the exact reference for the
  style, the angle and the lighting; the gem is our existing diamond: reuse exactly the same gem. »*
  et sa dernière ligne `OBJECT: …` par celle de l'image voulue.

---

## 4. Récapitulatif — ordre de génération

| # | Conversation Gemini | Fichier | Format | Fond |
|---|---|---|---|---|
| 1 | F — Rebirth (joindre `icone_boutique.png`, `icone_classement.png`) | `art/icones/icone_rebirth.png` | carré 1:1 | `#E6E1EE` |
| 2 | F (facultative) | `art/icones/icone_rebirth_b.png` | carré 1:1 | `#E6E1EE` |
| 3 | G — Diamants (joindre `piece.png`, `icone_boutique.png`) | `art/icones/diamant.png` | carré 1:1 | **`#5B6380`** |
| 4 | G | `art/icones/pack_diamants_petit.png` | carré 1:1 | `#5B6380` |
| 5 | G | `art/icones/pack_diamants_sac.png` | carré 1:1 | `#5B6380` |
| 6 | G | `art/icones/pack_diamants_coffre.png` | carré 1:1 | `#5B6380` |

**Priorité** : 1 (Rebirth, bouton du HUD v5) et 3 (Diamant, monnaie) sont **indispensables** dès
que le game designer valide ces fonctions ; 4 à 6 attendent la spec de la boutique de diamants ;
2 n'est utile que si l'image 1 ne convient pas.

**Pas d'image** (USS ou code, art bible §12.17 et §14) : boutons ronds, anneaux, info-bulle,
pastille « ! », faisceau, halo, rayons et étincelles de l'ouverture des boîtes.

**Après génération**
1. Détourer → `art/icones_detourees/<même nom>.png`. Vérifier le creux de la flèche circulaire,
   les pointes des gemmes, l'ouverture du sac, l'espace sous le couvercle du coffre.
2. Copier les icônes détourées dans `TourDeLaChance-Unity/Assets/Art/icones_detourees/`.
3. Contrôle dans le jeu : `icone_rebirth` à 112 px dans le bouton rond `#3A3358` / anneau
   `#EF6A32` se reconnaît **sans texte** ; `diamant` à 32 px à côté d'un chiffre reste une gemme ;
   le diamant et la pièce côte à côte ne se confondent pas.
