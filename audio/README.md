# Sons de Tour de la Chance — fabriqués par synthèse

Tous les sons du jeu sont **calculés** par des scripts Python : aucun enregistrement, aucun
son téléchargé, aucune imitation d'une musique ou d'un jingle existant. Relancer les scripts
redonne **exactement** les mêmes fichiers (les tirages au hasard utilisent des graines fixes).

Référence : `docs/specs/sons-animations.md` §5 (liste des sons) et §6 (mixage).

## Régénérer tous les sons

Les scripts ont besoin de **numpy**, absent du Python de macOS mais fourni avec Blender :

```bash
/Applications/Blender.app/Contents/Resources/4.5/python/bin/python3.11 audio/generer_tout.py
# ou, équivalent :
/Applications/Blender.app/Contents/MacOS/Blender --background --python audio/generer_tout.py
```

Cela écrit les WAV dans `TourDeLaChance-Unity/Assets/Audio/<categorie>/` puis affiche le
tableau de contrôle (voir « Vérification » plus bas). Chaque script peut aussi être lancé seul.

## Les fichiers de ce dossier

| Script | Rôle |
|---|---|
| `synth.py` | La boîte à outils commune : oscillateurs (sinus, carré, triangle, dents de scie, bruit), enveloppes (ADSR, percussive), glissandos, vibrato, filtres (passe-bas/haut/bande, passe-bas « qui s'ouvre »), timbres (boîte à musique, harpe, glockenspiel, clochette, pièce, orgue, cuivre), réverbération, fondus, normalisation, écriture WAV. |
| `sons_ui.py` | Les 7 sons `ui_*` + `vente`. |
| `sons_coffre.py` | Les 4 sons `coffre_*`. |
| `sons_resultat_pieces.py` | Les 3 sons `resultat_*` et les 3 sons `pieces_*`. |
| `sons_boite.py` | Les sons des boîtes à tirage aléatoire (secouer, ouvrir, 6 révélations). |
| `musique.py` | Les 2 musiques en boucle. |
| `analyse.py` | Le contrôle par la mesure (on ne peut pas écouter, alors on mesure). |
| `generer_tout.py` | Lance tout, puis l'analyse. |

## Règles appliquées à tous les sons

- **Format** : WAV 44,1 kHz, 16 bits ; mono pour les effets, stéréo pour les musiques.
- **Pas de clic** : chaque effet a un fondu d'entrée et de sortie (2 à 5 ms en entrée,
  5 ms à 0,3 s en sortie selon la longueur de la queue de réverbération). Un son qui
  démarre ou s'arrête brutalement fait un « clic » dans le haut-parleur.
- **Pic à −1 dBFS** (le maximum possible moins une petite marge contre la saturation).
- **Exception volontaire, pour des volumes cohérents** : le pic ne dit pas si un son *paraît*
  fort. `synth.py` mesure donc aussi un **volume ressenti** (moyenne la plus forte sur 0,4 s).
  Quelques sons sont baissés sous −1 dBFS pour atteindre un volume ressenti cible :
  - `resultat_piege` (cible −13 dB) : sinon il paraissait plus fort que le trésor ;
    le piège ne doit pas « gagner » contre le trésor ;
  - les révélations `commun` (−19), `peu_commun` (−17,5), `rare` (−16), `super_rare` (−14,5) :
    l'échelle monte ainsi vraiment jusqu'à `legendaire` (−12,4) et `secret` (−11,3).
    **Conséquence pour le code : jouer toutes les révélations au même volume** ; la
    progression est déjà dans les fichiers.
- **Musiques** : pas de fondu (il ferait un trou de volume à chaque tour de boucle). Le
  raccord est assuré autrement, voir `musique_ambiance`.

## Volumes de lecture conseillés (`AudioSource.volume`)

Pour les 19 sons de la spec : voir le tableau du §6.1 de la spec. Pour les nouveaux sons :

| Sons | Volume conseillé |
|---|---|
| `boite_secoue`, `boite_ouverture` | 0,7 (comme les coffres) |
| `revelation_*` (les six) | 0,9 (comme les résultats ; tous au même volume) |
| `vente` | 0,6 |

Réglages d'import Unity (spec §5.1) : effets en *Decompress On Load* ; musiques en
*Streaming* + *Load In Background*. Sons « variés » (hauteur 0,95–1,05 au hasard) :
`ui_survol`, `ui_clic`, `coffre_survol`, `pieces_tic`.

---

## Fiche de chaque son

### Interface (`ui/`)

| Son | Durée | Rôle | Fabrication |
|---|---|---|---|
| `ui_survol` | 0,05 s | Survol d'un bouton : un tic léger, discret. | Sinus 1 800 Hz, attaque 2 ms, chute 40 ms. |
| `ui_clic` | 0,08 s | Clic de bouton / coffre / aperçu : « toc » de bouton en bois. | Sinus qui glisse de 900 à 600 Hz + 10 ms de bruit filtré passe-bas 2 kHz (l'impact). |
| `ui_jouer` | 0,6 s | Bouton « Jouer » : « C'est parti ! ». | Arpège do5-mi5-sol5 puis do6 tenu, timbre boîte à musique (sinus + harmonique ×4 qui meurt en 0,1 s), écho léger (110 ms). |
| `ui_etage_monte` | 1,0 s | « Continuer » : on s'envole vers l'étage suivant. | Glissando de harpe sur 2 octaves de fa majeur (fa4 → fa6, 15 notes en 0,6 s, crescendo) + souffle de bruit dont le passe-bas s'ouvre de 300 à 3 000 Hz ; petite réverb. |
| `ui_nouvelle_partie` | 0,5 s | « Rejouer » : on redescend, sans tristesse. | Harpe plus douce (moins d'harmoniques), une octave de fa majeur vers le bas en 0,3 s. |
| `ui_boutique_ouvre` | 0,3 s | Ouverture du shop : clochette d'entrée. | Deux notes de clochette sol5 → do6 (sinus + harmonique ×3). |
| `ui_boutique_ferme` | 0,25 s | Fermeture du shop : la même, à l'envers. | do6 → sol5. |
| `vente` | 0,5 s | Vente d'un doublon (boîtes) : pièces qui tombent dans la bourse. | 6 « ting » de pièce en cascade accélérée, de plus en plus graves et doux, puis un « pouf » de tissu (bruit passe-bas 600 Hz + petit sinus 160 → 90 Hz). Différent de `pieces_achat` (pas de clochette) : on *reçoit* des pièces. |

### Coffres (`coffre/`)

| Son | Durée | Rôle | Fabrication |
|---|---|---|---|
| `coffre_survol` | 0,12 s | Le coffre frétille au survol, petit grincement de bois. | Deux impulsions de bruit dans un filtre passe-bande résonant vers 1 200 Hz (le « corps » du bois), à 45 ms d'écart. |
| `coffre_tremble` | 1,0 s | Suspense : le coffre claque, le cœur bat. À couper à l'ouverture. | 12 cliquetis de bois par seconde (passe-bande 800 Hz) de plus en plus forts, chacun doublé d'un petit « toc » grave ; dessous, un sinus qui monte de 110 à 220 Hz (la tension). Il continue jusqu'au bout (le code le coupe). |
| `coffre_ouvre` | 0,35 s | Le couvercle s'ouvre d'un coup. | Grincement 0,15 s (triangle 400 → 700 Hz, vibrato rapide 38 Hz) puis « boum » sourd (sinus 135 → 105 Hz, chute 0,15 s) + choc de bruit passe-bas. |
| `coffre_ferme` | 0,2 s | Les couvercles retombent (shop). | « Boum » seul : sinus 140 → 90 Hz + bruit passe-bas 700 Hz. |

### Résultats (`resultat/`)

| Son | Durée | Rôle | Fabrication |
|---|---|---|---|
| `resultat_tresor` | 0,9 s | Trésor : joie, « Ta-daa ! » scintillant. | Glockenspiel (partiels ×2,76 et ×5,4) en arpège fa5-la5-do6-fa6, 0,07 s entre les notes, + 16 étincelles (sinus 4–6 kHz très courts) ; réverb. |
| `resultat_piege` | 1,0 s | Piège : « boïng… wah-wah » drôle, jamais effrayant. | Ressort (sinus 300 → 150 Hz, vibrato large 12 Hz) puis deux notes de trombone cartoon (carré filtré mi4 → ré#4, vibrato lent) dont le filtre s'ouvre et se ferme (« wah »), la dernière glisse d'une tierce vers le bas. Notes proches, pas de grave menaçant. Baissé à −5,4 dBFS de pic pour ne pas paraître plus fort que le trésor. |
| `resultat_encaisse` | 1,4 s | Encaisser : petite fanfare de fête foraine. | Orgue de barbarie (carré doux filtré + trémolo 5 Hz) : accords fa – si♭ – do – fa (le dernier tenu 0,6 s), avec basse ; puis glockenspiel do6-fa6-la6-do7 et étincelles ; réverb. C'est le son le plus fort ressenti des effets : la victoire. |

### Pièces (`pieces/`)

| Son | Durée | Rôle | Fabrication |
|---|---|---|---|
| `pieces_jaillissement` | 0,8 s | Gerbe de pièces avec le trésor. | 14 « ting » (sinus 2–5 kHz + partiel non entier ×2,7, chute 60 ms) sur 0,6 s, de plus en plus espacés et doux (les pièces retombent). |
| `pieces_tic` | 0,04 s | Une pièce dans la bourse (compteurs, pièces volantes). | Un « ting » 3 200 Hz + ×2,7, chute 30 ms. |
| `pieces_achat` | 0,5 s | Achat au shop : « kaching » de conte. | Deux « ting » à 60 ms d'écart puis une clochette do6 (sinus + ×4). |

### Boîtes à tirage (`boite/`)

| Son | Durée | Rôle | Fabrication |
|---|---|---|---|
| `boite_secoue` | 1,0 s | La boîte tremble avant l'ouverture : suspense. | Secousses par paires (comme une boîte qu'on agite) qui accélèrent ; à chaque secousse, la paroi en bois cogne (passe-bande 450–600 Hz + « toc » sinus) et 2 ou 3 objets cliquettent dedans (bruit passe-bande 2,5–4,5 kHz, au hasard) ; un souffle qui s'éclaircit (400 → 2 200 Hz) monte dessous. |
| `boite_ouverture` | 0,6 s | Le couvercle saute. | « Pop » feutré (sinus 420 → 110 Hz en 80 ms + bruit sourd), puis souffle magique dont le filtre monte de 900 à 7 000 Hz, trois clochettes do6-fa6-la6 et des étincelles. |
| `revelation_commun` | 0,5 s | Objet commun. | Un seul « ding » de clochette (do6). Simple et propre. |
| `revelation_peu_commun` | 0,8 s | Objet peu commun. | Deux notes de glockenspiel qui montent (la5 → do6) + 3 étincelles. |
| `revelation_rare` | 1,1 s | Objet rare. | Arpège de 3 notes fa5-la5-do6, glockenspiel doublé d'une harpe à l'octave inférieure, 8 étincelles, réverb plus grande. |
| `revelation_super_rare` | 1,5 s | Objet super rare. | Arpège rapide de 5 notes jusqu'au la6, **nappe** chatoyante (scies désaccordées) qui gonfle dessous, souffle montant, 20 étincelles. |
| `revelation_legendaire` | 2,0 s | Objet légendaire : fanfare scintillante. | Appel de **cuivres** synthétiques (do5-fa5-la5 en triolet) puis accord de fa majeur tenu par les cuivres, **timbale** grave, glockenspiel qui grimpe jusqu'au do7, nappe aiguë, 40 étincelles, grande réverb. |
| `revelation_secret` | 2,5 s | Objet secret : mystérieux et unique. | Souffle « à l'envers » qui gonfle avec un glissando aigu, puis un accord **hors de la tonalité** du jeu : ré♭ majeur 7e majeure avec quarte augmentée (ré♭-la♭-fa-do-sol, couleur « lydienne », féerique et suspendue, qui ne se résout pas) ; grave doux en ré♭, cloches de verre (partiels ×3,01 et ×5,43) qui descendent en cascade, étincelles qui *glissent* vers l'aigu (aucun autre son ne fait ça), très grande réverb. L'accord est majeur et lumineux : mystère, pas peur. |

**Comment l'échelle monte** : nombre de notes (1 → 2 → 3 → 5 → fanfare), durée
(0,5 → 2,0 s), hauteur maximale (do6 → do7), étincelles (0 → 3 → 8 → 20 → 40), taille de
la réverb, nouveaux instruments (nappe au super rare, cuivres et timbale au légendaire),
et volume ressenti (−19 → −12,4 dB). Toutes en fa majeur (la tonalité de la musique),
sauf le secret, qui sort de la tonalité : c'est ce qui le rend « à part ».

### Musiques (`musique/`, stéréo)

**`musique_ambiance`** — 48,0 s en boucle. Fête foraine la nuit, douce, magique.
- Valse lente : 3 temps, 120 battements/minute, 32 mesures de 1,5 s = 48 s pile.
- Fa majeur. Forme A – A' – B – A. Grille A : fa – fa – si♭ – fa – do – do – fa – do ;
  B : ré m – ré m – si♭ – si♭ – sol m – sol m – do – do. Le morceau finit sur *do*, l'accord
  qui « appelle » le *fa* du début : la boucle enchaîne naturellement.
- Mélodie originale en boîte à musique (do5 → do6), légèrement à droite ; au dernier A, un
  glockenspiel très doux la double à l'octave.
- Orgue de barbarie (carré doux filtré 1 500 Hz, trémolo 5 Hz) : basse sur le temps 1,
  accord sur les temps 2 et 3 (« boum – tchak – tchak »), légèrement à gauche.
- Pas de batterie ; réverbération stéréo légère (1,8 s) pour le plein air nocturne.
- **Boucle sans coupure** : la musique est calculée sur 52 s ; tout ce qui dépasse 48 s
  (notes qui résonnent, queue de réverb) est **recopié sur le début**, puis on coupe à 48 s.
  Le fichier est donc « circulaire » : le raccord fin → début se comporte comme n'importe
  quelle barre de mesure.

**`musique_tension`** — 48,0 s en boucle. Couche qui se **superpose** à l'ambiance
(même tempo, même grille, à démarrer au même instant avec `PlayScheduled`).
- Cœur « boum-boum » (sinus ~60 Hz qui chute, + octave) sur les temps 1 et 2 ;
- basse pizzicato (triangle, note courte) sur le temps 1 ;
- nappe aiguë (deux scies désaccordées, filtrées) qui tient les notes de l'accord, avec un
  trémolo rapide à 8 Hz **calé sur le tempo** (4 ondulations par temps, calculé sur le temps
  absolu pour ne jamais se décaler) ;
- même technique de boucle. Seule, elle est un peu inquiétante mais jamais effrayante
  (accords majeurs/mineurs simples, aucune dissonance).

---

## Vérification (puisqu'on ne peut pas écouter)

`analyse.py` mesure chaque fichier :
- **durée**, **pic** (dBFS), **RMS** (volume moyen), **volume ressenti** (0,4 s) ;
- **saturation** : nombre d'échantillons au maximum (doit être 0) ;
- **premier et dernier échantillon** d'un effet : doivent valoir 0 (fondus = pas de clic) ;
- **raccord de boucle** des musiques : le saut entre le dernier et le premier échantillon doit
  être plus petit que 99,9 % des sauts entre échantillons voisins du morceau, et le volume
  juste avant le raccord doit ressembler à celui juste avant les autres barres de mesure.

Le bilan final affiche « tout est conforme » ou signale les fichiers à vérifier.
