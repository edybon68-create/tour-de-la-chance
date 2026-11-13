# Lot 8 — Prompts d'images Gemini (v6.5 : icône « Mes skins »)

> **À quoi sert ce fichier ?** Chaque bloc ci-dessous = **une image**. On copie le prompt
> (le texte en anglais dans le cadre gris) et on le colle tel quel dans Gemini, dans le
> navigateur. On enregistre ensuite l'image sous le **nom de fichier indiqué**.
>
> Les prompts sont en anglais parce que les générateurs d'images le comprennent mieux.
> Les explications autour sont en français.
>
> Référence : `docs/art-bible.md` **§16.1.3** (menu de l'Inventaire : où et à quelle taille
> l'icône s'affiche), §9 (règles des images).
>
> **Les 12 dalles de L'Ascension ne sont PAS dans ce lot** : elles sont rendues dans Blender par
> l'artiste 3D (`blender/rendu_dalles.py`, art bible §16.3).

---

## 0. Mode d'emploi

1. **Une conversation Gemini** (J). Joindre au premier message (bouton « + »)
   `art/icones/icone_inventaire.png` et `art/icones/icone_boutique.png` : ce sont les références
   de rendu (jouet verni, angle, fond, lumière).
2. **Fond** : uni lavande très clair `#E6E1EE`.
3. **Où l'image s'affiche** : à **60 px** dans le menu de l'Inventaire, sur un fond beige
   `#E9E3D6`, et à **68 px** sur la face **bleue** `#3B7DD8` quand l'entrée est active. L'icône ne
   doit donc être ni beige ni bleue, et elle doit se lire en tout petit (formes épaisses).
4. **Aucun texte, aucun chiffre, aucun logo.** Ne rien copier : pas de cintre avec tee-shirt
   (c'est l'icône du jeu de référence de l'auteur).
5. **Enregistrement** : PNG, `art/icones/icone_mes_skins.png`, puis détourage par
   `blender/detourer_icones.py` (il ajoute le contour noir épais), résultat dans
   `art/icones_detourees/`.

**Ce qui doit être identique aux icônes des lots précédents** : la lumière (chaude en haut à
gauche, liseré lavande derrière), l'angle (3/4, ~25° au-dessus, tourné ~20° vers la gauche),
l'objet seul et centré à ~70 % du cadre, le fond uni lavande sans ombre portée, le rendu « jouet
verni ».

---

## 1. Conversation J — Icône « Mes skins »

### Image 1 — `art/icones/icone_mes_skins.png`

- **Format** : carré 1:1, fond uni `#E6E1EE`.
- **Idée** : une petite **armoire jouet** en bois aux portes grandes ouvertes, où pendent trois
  petites capes de couleurs vives : « mes tenues rangées ». Le bois chaud et les capes colorées
  ressortent sur le beige et sur le bleu.

```
STYLE: stylized low-poly 3D render in a cheerful toy style, like painted wooden board-game pieces and tabletop toys. Chunky, thick, rounded shapes with slightly exaggerated proportions; soft bevels on every hard edge that catch the light; perfectly smooth shading on round objects; clean flat colors with no textures, no wood grain, no scratches, no dirt, no wear. LIGHTING: warm white key light (#FFF1D6) from the upper left and slightly in front, soft shadows, gentle violet ambient fill so shadows are never black, subtle lavender rim light (#B48CFF) from behind. PALETTE: night violet (#1F1730, #3A2850), cool blue-grey stone (#5B6380, #8A90B0), warm wood (#8C5933, #6B4226), dark metal (#4A4E5A), brass (#C99A1E), shiny gold (#FFC83A). MOOD: a magical fairground at night on top of a fairy-tale tower; friendly, cozy, readable. RULES: no text, no letters, no numbers, no logos, no brand names, no watermark, no signature, no money symbols, no banknotes, no casino chips.

ICON FORMAT: a square 1:1 game shop icon. One single object, alone, perfectly centered, filling about 70 percent of the image height with equal margins, nothing cropped. Three-quarter view from slightly above (about 25 degrees), object turned about 20 degrees to the left, its front facing the viewer. Plain flat solid very light lavender background (#E6E1EE), no gradient, no floor, no scenery, no cast shadow on the background.

SUBJECT: a small chunky toy wardrobe made of warm painted wood (#8C5933, darker edges #6B4226), slightly taller than wide, with a rounded arched top and short round feet. Both doors are wide open toward the viewer, their inner panels painted cream (#FFF4DC), each with a round brass knob (#C99A1E). Inside, on a thick brass rod, hang three small capes side by side on chunky rounded hangers: a pink cape (#FF6FB5), a lemon yellow cape (#F2D03B) and a turquoise cape (#2BB8C4), each a simple thick rounded shape clearly separated from the others. The interior back wall is a soft violet (#3A2850) so the three capes pop out. Big simple shapes readable at 60 pixels, no small details, no clothes with prints, no shirt, no person, no mirror.
```

**Critères de réussite**
- On reconnaît « une armoire ouverte avec des tenues » même réduite à 60 px.
- Les trois capes sont nettement séparées, de trois couleurs franches.
- Aucun tee-shirt sur cintre seul, aucun texte, fond uni lavande sans ombre.

**Prompt de relance** (si l'image dérive, nouvelle conversation avec les deux images jointes) :
recoller le prompt complet ci-dessus, en ajoutant à la fin : *« Match exactly the lighting,
angle, glossy toy finish and plain lavender background of the attached reference icons. »*
