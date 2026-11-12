# Sons de Tour de la Chance — fabriqués par synthèse

Tous les sons du jeu sont **calculés** par des scripts Python : aucun enregistrement, aucun
son téléchargé, aucune imitation d'une musique ou d'un jingle existant. Relancer les scripts
redonne **exactement** les mêmes fichiers (les tirages au hasard utilisent des graines fixes).

Références : `docs/specs/sons-animations.md` §5 (liste des sons) et §6 (mixage) ;
`docs/specs/v3.md` §14 (les 15 sons de la v3) et §13 (correctif B3 : deux sons raccourcis) ;
`docs/specs/v5.md` §17.4 et `docs/art-bible.md` §14.4 (les 34 sons de la v5, voir « Sons v5 »).

## Régénérer tous les sons

Les scripts ont besoin de **numpy**, absent du Python de macOS mais fourni avec Blender :

```bash
PY=/Applications/Blender.app/Contents/Resources/4.5/python/bin/python3.11

$PY audio/generer_tout.py                               # les 43 sons -> Assets/Audio du projet Unity
$PY audio/generer_tout.py --sortie audio/exports        # les 43 sons -> audio/exports (Unity intact)
$PY audio/generer_tout.py --v3 --sortie audio/exports   # seulement les 15 nouveaux + 2 corrigés
$PY audio/generer_tout.py --v5                          # seulement les 34 sons v5 (aucun ancien son touché)
$PY audio/analyse.py audio/exports                      # re-mesurer un dossier sans rien régénérer
# avec Blender, les options se placent après « -- » :
/Applications/Blender.app/Contents/MacOS/Blender --background --python audio/generer_tout.py -- --v3 --sortie audio/exports
```

Par défaut, les WAV vont dans `TourDeLaChance-Unity/Assets/Audio/<categorie>/`. Avec
`--sortie <dossier>` (ou la variable d'environnement `TDLC_AUDIO_SORTIE`), ils vont ailleurs,
**avec la même arborescence** (`<categorie>/<nom>.wav`) : on s'en sert quand on ne doit pas
toucher au projet Unity (par exemple pendant que quelqu'un joue dans l'éditeur). Pour les
installer ensuite, il suffit de copier le contenu de `audio/exports/` dans `Assets/Audio/`
(Unity crée les `.meta` tout seul ; réglages d'import : voir plus bas).

À la fin, le tableau de contrôle s'affiche (voir « Vérification » plus bas). Chaque script
peut aussi être lancé seul.

**Reproductibilité vérifiée** : régénérer les 43 sons redonne au bit près les 26 fichiers
déjà présents dans Unity qui n'ont pas changé (les révélations comprises, voir
`_consomme_comme_boite_ouverture_v2` dans `sons_boite.py`), et le mode `--v3` produit
exactement les mêmes fichiers que le mode complet.

## Les fichiers de ce dossier

| Script | Rôle |
|---|---|
| `synth.py` | La boîte à outils commune : oscillateurs (sinus, carré, triangle, dents de scie, bruit), enveloppes (ADSR, percussive), glissandos, vibrato, filtres (passe-bas/haut/bande, passe-bas « qui s'ouvre »), timbres (boîte à musique, harpe, glockenspiel, clochette, pièce, orgue, cuivre), réverbération, fondus, normalisation, **limiteur doux**, mesures de volume (ressenti, « à l'oreille »), écriture WAV, choix du dossier de sortie. |
| `sons_ui.py` | Les 7 sons `ui_*` + `vente`. |
| `sons_coffre.py` | Les 4 sons `coffre_*`. |
| `sons_resultat_pieces.py` | Les 3 sons `resultat_*` et les 3 sons `pieces_*`. |
| `sons_boite.py` | Les sons des boîtes à tirage aléatoire (secouer, ouvrir, 6 révélations). |
| `sons_v3.py` | v3 : `amelioration_achat`, `amelioration_max`, `record` (ui) et les 7 nouveaux sons de coffres. |
| `sons_secret.py` | v3 : les 5 sons de la cinématique du Secret (`secret_*`, catégorie boite). |
| `sons_v5.py` | v5 : les 34 nouveaux sons (diamants, reprise, renaissance, interface, couches de l'ouverture 3D, touches Pirates / Animaux, objets animés). Nouveau dossier `jeu/`. |
| `musique.py` | Les 2 musiques en boucle. |
| `analyse.py` | Le contrôle par la mesure (on ne peut pas écouter, alors on mesure). |
| `generer_tout.py` | Lance tout (ou seulement la v3 avec `--v3`, ou la v5 avec `--v5`), dans le dossier choisi (`--sortie`), puis l'analyse. |
| `exports/` | Sons en attente d'être copiés dans `Assets/Audio/` (même arborescence). |

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
- **Volume « à l'oreille » (v3)** : le volume ressenti ci-dessus est un RMS brut ; il
  surestime les sons très graves (battement de cœur) et sous-estime les sons très aigus
  (crépitements, éclats de verre), que l'oreille entend plus fort. Pour les sons v3, on
  vise donc une valeur **pondérée K** (la pondération de la norme ITU-R BS.1770, celle des
  mesures LUFS) : `synth.volume_pondere`, colonne « oreille » de l'analyse. Pour les sons
  sans grave ni aigu extrêmes, les deux mesures sont proches (à 1 dB près), donc les sons
  v3 restent cohérents avec les anciens.
- **Limiteur doux (v3)** : un son fait de clics très pointus atteint -1 dBFS en paraissant
  minuscule. `finalise(..., cible_oreille=...)` rabote alors ses pointes (au plus 9 dB) avec
  `synth.limiteur` (qui baisse le volume 1,5 ms avant chaque pointe puis le relâche en
  40 ms, sans écrêter l'onde) avant de renormaliser à -1 dBFS. Utilisé par les crépitements,
  éclats de verre, fissures, bulles. À l'inverse, un son trop fort est simplement baissé
  (pic sous -1 dBFS : `coffres_precieux`, `secret_coupure`, `amelioration_max`,
  `coffre_eclat_argent`, `coffres_remelange`).
- **Musiques** : pas de fondu (il ferait un trou de volume à chaque tour de boucle). Le
  raccord est assuré autrement, voir `musique_ambiance`.

## Volumes de lecture conseillés (`AudioSource.volume`)

Pour les 19 sons de la spec : voir le tableau du §6.1 de la spec. Pour les sons des boîtes (lot 2) :

| Sons | Volume conseillé |
|---|---|
| `boite_secoue`, `boite_ouverture` | 0,7 (comme les coffres) |
| `revelation_*` (les six) | 0,9 (comme les résultats ; tous au même volume) |
| `vente` | 0,6 |

Sons v3 (spec `v3.md` §14, valeurs reprises telles quelles : les fichiers sont déjà
équilibrés entre eux, ces volumes ajoutent la hiérarchie voulue par le game-designer) :

| Sons | Volume conseillé |
|---|---|
| `amelioration_achat` | 0,7 |
| `amelioration_max` | 0,8 |
| `record` | 0,7 |
| `coffres_precieux`, `coffre_en_plus` | 0,5 |
| `coffre_eclat_bronze` / `argent` / `or` / `diamant` | 0,6 / 0,65 / 0,7 / 0,75 |
| `coffres_remelange` | 0,6 |
| `secret_coupure`, `secret_battement`, `secret_fissure` | 0,8 |
| `secret_eclat` | 0,9 |
| `secret_confettis` | 0,6 |

Atténuation de la musique (*ducking*, spec §14) : `secret_*`, `coffre_eclat_or` et
`coffre_eclat_diamant` déclenchent la même atténuation que `resultat_*` ; pendant
`secret_coupure`, la musique descend à **0**. `coffres_precieux` contient seulement des
aigus : le code peut monter sa hauteur selon le rang (`AudioSource.pitch` 1,0 / 1,06 /
1,12 / 1,19 par exemple, soit 0 à 3 demi-tons) sans qu'il devienne criard.

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
| `vente` | 0,4 s | Vente d'un doublon (boîtes) : pièces qui tombent dans la bourse. | 6 « ting » de pièce en cascade accélérée (en 0,15 s), de plus en plus graves et doux, puis un « pouf » de tissu à 0,18 s (bruit passe-bas 600 Hz + petit sinus 160 → 90 Hz). Différent de `pieces_achat` (pas de clochette) : on *reçoit* des pièces. *v3, correctif B3 : 0,5 → 0,4 s, tout resserré, même volume « à l'oreille » (-14,5).* |
| `amelioration_achat` | 0,6 s | Achat d'un niveau d'amélioration (1 à 4) : « tu deviens plus fort ». | « Ding-ding-ding » montant la5-do6-fa6 (80 ms d'écart, crescendo), clochette (sinus + ×4) doublée d'un glockenspiel discret ; puis une étincelle : sinus qui file de 2 500 à 5 200 Hz + 5 « tin » ; réverb légère. |
| `amelioration_max` | 1,2 s | Achat du niveau 5 : le triomphe de l'amélioration. | La même montée de 3 notes, puis fanfare d'orgue de barbarie « ta-ta-taaa » si♭ – do – fa (le fa tenu 0,55 s), glockenspiel fa6-la6-do7 et 14 étincelles. Même famille que `resultat_encaisse` (l'orgue de manège) mais avec la montée en tête : on reconnaît « amélioration ». |
| `record` | 1,0 s | Bannière « Nouveau record ! » : fierté, « Ta-da ! ». | Trompette synthétique (`cuivre`) : do5 bref (« ta ») puis fa5 tenu avec vibrato (« daaa »), harmonisé par trois cuivres plus doux (do5, la4, fa3) ; cymbale légère (bruit passe-haut 6 kHz), glockenspiel la5-do6-fa6, étincelles. Deux notes seulement : différent de l'appel en 3 notes du légendaire. |

### Coffres (`coffre/`)

| Son | Durée | Rôle | Fabrication |
|---|---|---|---|
| `coffre_survol` | 0,12 s | Le coffre frétille au survol, petit grincement de bois. | Deux impulsions de bruit dans un filtre passe-bande résonant vers 1 200 Hz (le « corps » du bois), à 45 ms d'écart. |
| `coffre_tremble` | 1,0 s | Suspense : le coffre claque, le cœur bat. À couper à l'ouverture. | 12 cliquetis de bois par seconde (passe-bande 800 Hz) de plus en plus forts, chacun doublé d'un petit « toc » grave ; dessous, un sinus qui monte de 110 à 220 Hz (la tension). Il continue jusqu'au bout (le code le coupe). |
| `coffre_ouvre` | 0,35 s | Le couvercle s'ouvre d'un coup. | Grincement 0,15 s (triangle 400 → 700 Hz, vibrato rapide 38 Hz) puis « boum » sourd (sinus 135 → 105 Hz, chute 0,15 s) + choc de bruit passe-bas. |
| `coffre_ferme` | 0,2 s | Les couvercles retombent (shop). | « Boum » seul : sinus 140 → 90 Hz + bruit passe-bas 700 Hz. |
| `coffres_precieux` | 0,5 s | Un étage à coffres précieux arrive : « ooh, c'est joli ». | 8 clochettes aiguës (fa6 à fa7, notes de fa majeur) en pluie de plus en plus légère sur 0,3 s + léger souffle aigu, réverb. Aucun grave : on peut le transposer selon le rang. Baissé à -5,5 dBFS de pic (sinon trop présent pour un son d'annonce). |
| `coffre_en_plus` | 0,3 s | Un 4e / 5e coffre apparaît : « pop ! » de bulle magique. | Bulle : sinus qui **monte** de 350 à 1 400 Hz en 70 ms (c'est ce qui fait « bulle ») + claquement humide (bruit passe-bande 2,5 kHz, 8 ms) ; puis deux clochettes fa6 et do7 et 3 étincelles. |
| `coffre_eclat_bronze` | 0,4 s | Ouverture d'un coffre de bronze : métal chaud. | Petite cloche la5 (880 Hz) aux partiels de cloche ×2 / ×2,92 / ×4,1 + un peu de corps à l'octave basse, adoucie par un passe-bas 4 kHz (c'est la douceur qui la rend « chaude »). Le plus discret des éclats. |
| `coffre_eclat_argent` | 0,6 s | Coffre d'argent : clair, net. | Carillon de verre pur 2 notes do6 → fa6 (90 ms d'écart, partiels ×2 et ×4,16) + souffle aigu dont le filtre s'ouvre de 3 à 9 kHz. |
| `coffre_eclat_or` | 0,8 s | Coffre d'or : riche, éclatant. | Accord de harpe fa majeur égrené de bas en haut en 0,1 s (fa4 → do6), glockenspiel fa6 au sommet, 12 étincelles, réverb. |
| `coffre_eclat_diamant` | 1,1 s | Coffre de diamant : le cristal chante, le plus « wow ». | Glissando de 10 verres de cristal en 0,33 s (pentatonique de fa, fa5 → fa7), puis éclat : accord de verre fa6-la6-do7, bouffée de bruit très aigu (> 7 kHz), gerbe de 25 étincelles ; enfin un verre qui « chante » (fa6 tenu, vibrato lent 5 Hz) ; réverb brillante 1 s. |
| `coffres_remelange` | 0,5 s | Remélange de l'étage 1 : on rebat les cartes. | Trois couvercles qui claquent à 55 ms d'écart, du grave à l'aigu (sinus 150 → 100, 170 → 115, 190 → 130 Hz + choc de bruit étouffé), puis deux « pouf » de magicien à 0,2 et 0,32 s (souffle dont le filtre s'ouvre puis se ferme + petit sinus 320 → 180 Hz). |

**Échelle des éclats** (volume « à l'oreille ») : bronze -17 < argent -15 < or -13,5 <
diamant -12,5, en plus des volumes de lecture 0,6 → 0,75. Durée, hauteur, nombre de notes
et d'étincelles montent aussi, comme pour les révélations.

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
| `boite_ouverture` | 0,3 s | Le couvercle saute. | « Pop » feutré (sinus 420 → 110 Hz en 70 ms + bruit sourd), puis souffle magique de 0,24 s dont le filtre monte de 900 à 7 000 Hz, trois clochettes do6-fa6-la6 à 50 ms d'écart et 7 étincelles. *v3, correctif B3 : 0,6 → 0,3 s, même recette resserrée, s'éteint d'elle-même (-46 dB sous le pic juste avant le fondu de 5 ms), même volume « à l'oreille » qu'avant (-16,5).* |
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

#### La cinématique du Secret (v3, `sons_secret.py`)

Cinq sons qui s'enchaînent bout à bout, chacun calé sur sa phase (spec `v3.md` §8.3) :
Coupure (R + 0) → Battement (R + 0,5) → Fissures (R + 1,3) → Éclat (R + 2,2) →
`revelation_secret` (R + 2,4) → Confettis (R + 3,4). Du mystère qui finit en fête, jamais
de peur : pas de grondement, pas de dissonance ; les notes tenues sont en ré♭ majeur, la
tonalité « à part » de `revelation_secret`, pour que tout sonne comme un seul moment.

| Son | Durée | Rôle | Fabrication |
|---|---|---|---|
| `secret_coupure` | 0,6 s | Panne de courant : « vwoum » qui descend, puis le noir. | Petit « clac » d'interrupteur (bruit sourd 30 ms), puis note électrique (dents de scie + octave) qui glisse de 220 à 35 Hz en 0,48 s pendant que son filtre se referme (3 000 → 120 Hz) ; le volume retombe à zéro, les 0,1 dernières secondes sont silencieuses. Cartoon : une seule descente courte. |
| `secret_battement` | 0,8 s | Deux battements de cœur graves, doux, mystérieux. | Deux « poum-poum » à 0,4 s d'écart (calés sur le point rose qui bat) : sinus 72 → 48 Hz + harmoniques 2 et 3 (pour les petits haut-parleurs) + feutre de bruit grave, second coup plus doux 120 ms après ; dessous, un voile très discret ré♭6 + la♭6 qui ondule. |
| `secret_fissure` | 0,9 s | Le cristal se fissure, la tension monte. | Craquements (3 à 6 micro-clics de bruit dans un filtre résonant) de plus en plus serrés (6 → 35 par seconde), aigus (1,5 → 7 kHz) et forts, avec de petits éclats de verre ; dessous, note pure 400 → 1 600 Hz et souffle qui s'éclaircit. Finit « au sommet » (fondu de sortie de 50 ms) : l'éclat prend le relais. |
| `secret_eclat` | 0,6 s | La vitre magique vole en éclats + « whoosh », très brillant, sans violence. | Attaque douce de bruit aigu (aucun grave, pas de « boum »), 40 morceaux de verre qui tintent, très denses au début puis de plus en plus rares, « whoosh » (filtre qui balaie de 7 000 à 1 200 Hz), accord de verre ré♭6-fa6-la♭6 qui fleurit (le pont vers la fanfare). |
| `secret_confettis` | 1,5 s | Impact : petits pétards de fête et crépitements joyeux. | Trois « pan ! » de cotillon (bruit passe-bande + petit sinus « bouchon » 900 → 400 Hz) à 0 / 0,1 / 0,28 s, chacun suivi d'un sifflet de papier qui monte ; 140 crépitements aigus au hasard qui se raréfient (les confettis retombent) ; 14 étincelles. |

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

## Sons v5 (`sons_v5.py`, spec `v5.md` §17.4)

34 nouveaux fichiers, **aucun son existant n'est remplacé** (`--v5` ne recalcule que ceux-ci).
Tous en fa majeur ; animaux et pirates toujours **mignons** (voix aiguës, glissandos ronds,
pas de grave menaçant). Colonne « Volume » = `AudioSource.volume` conseillé ; les fichiers sont
déjà équilibrés entre eux (« oreille » = volume pondéré K mesuré par `analyse.py`).

### Jeu (`jeu/`)

| Son | Durée | Quand il joue | Volume | Fabrication |
|---|---|---|---|---|
| `diamant_gain_1` / `_2` / `_3` | 0,9 s | Trésor en diamants (§10.5), à la place de `pieces_jaillissement`. **Choisir selon le palier d'étage** : 1-5 → `_1`, 6-9 → `_2`, 10+ → `_3` (donc 1 ou 2, 2 ou 4, 3 ou 6 diamants). | 0,8 | Arpège montant de « cristal » (partiels ×2 / ×3 / ×5,04, différents de la pièce ×2,7) : 3, 4 puis 5 notes (do6 → la6, do7, fa7), éclat doublé à l'octave grave, souffle très aigu qui brille, 14 à 26 étincelles. Oreille −13,5 / −13 / −12,5. |
| `diamant_compteur` | 0,12 s | Le compteur de diamants qui défile (§10.4) : une fois par diamant ajouté (≤ 6), pendant les 0,4 s. | 0,5 | Un « tink » de cristal la7 + mi7, chute 90 ms (le pendant de `pieces_tic`). |
| `reprise_ok` | 0,8 s | « Continuer — 1 ◆ » accepté (§11.2), avec la spirale bleutée. | 0,7 | « Rembobinage » : sifflet tremblotant (vibrato 22 Hz) qui descend 1 400 → 350 Hz puis remonte à 1 100 Hz, souffle qui fait le même aller-retour, puis carillon fa6-la6-do7 (0,46 s). |
| `renaissance` | 4,0 s | Début de l'animation de renaissance (§13.4, t = 0). | 0,9 | 0 → 1,5 s : 3 glissandos de harpe de plus en plus hauts, nappe de cordes (scies désaccordées) qui s'ouvre, carillons qui accélèrent, souffle montant ; **1,5 s (flash)** : accord de fa majeur cuivres doux + glockenspiel + cymbale légère (sommet du fichier) ; **2,1 s (atterrissage)** : « poum » feutré + 40 étincelles ; puis boîte à musique qui redescend et longue retombée. Oreille −11,5 (le plus grand moment du jeu). |
| `renaissance_tampon` | 0,4 s | Le tampon « ×2 » s'écrase sur le compteur (§13.4, t = 2,4 s). | 0,8 | « Boum » feutré (sinus 150 → 70 Hz + bruit sourd), « tchac » de papier, clochettes fa6 et do7. |
| `pirate_eclair` | 0,3 s | Skin Tempête : à chaque éclair (toutes les 2 à 4 s). | 0,25 | Crépitement de bruit aigu (0,15 s) + « zip » 2 400 → 600 Hz. Aucun tonnerre. Oreille −20 (il revient souvent). |
| `perroquet_cri` | 0,4 s | Perroquet, saut de joie. | 0,3 | « Rouah ! » : voix nasillarde (scie + filtre résonant) 900 → 1 400 → 1 000 Hz, un peu de grain, deux froufrous d'ailes. |
| `kraken_bebe` | 0,4 s | Petit Kraken, saut de joie. | 0,3 | « Blop-blop » : deux sinus qui montent (400 → 1 000, 550 → 1 400 Hz) + une goutte. |
| `navire_canon` | 0,5 s | Navire fantôme, salve de confettis. | 0,3 | « Pouf » de cotillon (bruit sourd + sinus 220 → 90 Hz), 14 crépitements de papier, clochette. |
| `paon_roue` | 0,8 s | Skin Paon, la roue se déploie (saut de joie). | 0,35 | Froufrou de plumes (bruit haché à 28 Hz, filtre qui monte, 0,36 s) puis « ta-da » glockenspiel do6 → fa6 + petite harpe. |
| `lion_rugit` | 0,6 s | Skin Lion solaire, saut de joie. | 0,4 | Rugissement de lionceau : voix aiguë qui monte et retombe, filtre « a → ou », grain à 38 Hz, étincelles. |
| `poussin_piou` | 0,3 s | Poussin, saut de joie. | 0,25 | Deux pépiements sinus 2,4 → 3,4-3,7 kHz → 2,1 kHz. |
| `chaton_miaou` | 0,5 s | Chaton, saut de joie. | 0,25 | « Miaou » : scie douce 600 → 950 → 550 Hz, filtre « i-a-ou », vibrato léger. |
| `baleine_chant` | 1,2 s | Baleine céleste, saut de joie. | 0,35 | Chant doux do5 → fa5 → ré5 (sinus + octave, vibrato lent), étoiles, grande réverb. |

### Interface (`ui/`)

| Son | Durée | Quand il joue | Volume | Fabrication |
|---|---|---|---|---|
| `reprise_offre` | 0,5 s | Ouverture de la fenêtre de reprise (§11.2, 0,8 s après la révélation du piège). | 0,6 | Deux notes de boîte à musique do6 → ré6, la seconde remonte un peu à la fin (intonation de question), grave doux fa4. Rien d'alarmant. |
| `recap_etage` | 0,3 s | Arrivée de la carte d'étage (§9.3, à `FloorShown`). | 0,4 | « Swoosh » de papier (bruit, filtre 1,5 → 5 → 2,5 kHz), 7 froissements, mini clochette quand elle se pose. |
| `ui_bouton_rond` | 0,1 s | Clic sur un bouton rond de colonne (§7.1). | 0,5 | « Bop » de bulle : sinus qui monte 500 → 850 Hz + éclat fa6. |
| `ui_infobulle` | 0,08 s | Apparition d'une info-bulle (après 0,4 s de survol). | 0,3 | « Blip » sinus 1 300 → 1 750 Hz. Le plus discret du jeu (oreille −21). |
| `ui_onglet` | 0,15 s | Changement d'onglet de la Boutique (§15.2), et « ← Retour » de la sous-page. | 0,5 | « Tic-tic » boîte à musique do6 → fa6 + mini souffle. |
| `ui_refus` | 0,28 s | Bouton grisé cliqué (pas assez, « Bientôt disponible », cadenas). Cité par la spec, n'existait pas encore. | 0,5 | « Bonk-bonk » : triangle étouffé la4 puis fa4. Gentil, pas une alarme. |

### Ouverture 3D (`boite/`) — couches calées sur `art-bible.md` §14.4

Les temps sont comptés depuis le clic sur « Ouvrir ». Les sons existants restent à leur place
(`pieces_achat` 0,00 ; `boite_secoue` 0,30 ; `boite_ouverture` 1,50 ; `revelation_<rareté>`
1,75 = R). Les nouvelles couches **s'ajoutent** ; elles sont **identiques pour toutes les raretés**
(le faisceau est une quinte à vide fa-do : ni joyeux ni triste, il ne trahit rien).

| Son | Durée | Quand il joue | Volume | Fabrication |
|---|---|---|---|---|
| `boite3d_atterrit` | 0,35 s | **0,24** (la boîte touche le socle ; le choc est au début du fichier). | 0,7 | « Bom » de bois sourd (sinus 147 → 105 Hz + modes ×2 / ×3,7 / ×6,3 + bruit étouffé), deux petits rebonds à +0,11 et +0,18 s. |
| `boite3d_saut` | 0,3 s | **0,55 / 0,95 / 1,35** (début de chacun des 3 petits sauts). | 0,5 | « Tom » de bois à +0,10 s (la boîte retombe), « clac » du couvercle à +0,15 s. |
| `boite3d_craque` | 0,4 s | **1,20** (spec v5 §6.3). | 0,6 | Craquement de bois (micro-clics dans un filtre résonant 600 → 1 500 Hz, de plus en plus serrés) + la lumière qui grésille (sinus 4 200 / 4 237 Hz qui battent, trémolo 31 Hz, souffle). |
| `boite3d_noeud` | 0,5 s | **1,58** (le nœud s'envole). | 0,6 | « Pwing » élastique (sinus 300 → 900 Hz, vibrato 18 Hz) + froufrou de ruban haché à 15 Hz qui s'éloigne. |
| `boite3d_couvercle` | 0,6 s | **1,62** (le couvercle part en arrière en basculant). | 0,6 | Petit « fwoump » de départ + souffle haché au rythme de la bascule (7 → 11 Hz), de plus en plus sourd et faible. Le « pop » reste celui de `boite_ouverture`. |
| `boite3d_faisceau` | 0,5 s | **1,62** (le faisceau blanc jaillit), en même temps que le couvercle. | 0,6 | « Shwiiing » (souffle 2 → 11 kHz en 0,16 s) puis quinte à vide fa5-do6-fa6 scintillante (trémolo 14 Hz) qui s'éteint vers R + 0,1. |

### Touches thématiques des Secrets (`boite/`, spec §6.6)

Jouées à **R + 3,4 s** (impact de la carte), par-dessus `secret_confettis`. Toutes les boîtes
Pirates : `pirate_vague` ; toutes les boîtes Animaux : `animal_pattes`. Si l'objet gagné est un
des 4 Secrets, ajouter **en même temps** sa signature (1,2 s).

| Son | Durée | Quand il joue | Volume | Fabrication |
|---|---|---|---|---|
| `pirate_vague` | 1,0 s | Secret d'une boîte Pirates. | 0,7 | Bruit qui gonfle (filtre 300 → 3 000 Hz), se brise à 0,4 s, écume qui pétille, 5 gouttes « plic ». |
| `animal_pattes` | 1,0 s | Secret d'une boîte Animaux (8 empreintes, 0,1 s d'écart). | 0,7 | 8 « pat » rebondis (sinus + coussinet de bruit doux) qui montent la gamme de fa5 à fa6, une clochette à la fin. |
| `pirate_secret_kraken` | 1,2 s | Secret = Kraken. | 0,7 | Grondement **doux** (55 Hz + bruit grave, discret) qui gonfle, puis gros « BLOP » rigolo (150 → 700 Hz) et petit « blop », trois bulles. |
| `pirate_secret_abysses` | 1,2 s | Secret = Abysses. | 0,7 | Chant grave et doux « ouuu » (voix filtrée do3 → fa3 → la3 + quinte), écho ×2, lueur aiguë do6. |
| `animal_secret_lion` | 1,2 s | Secret = Lion solaire. | 0,7 | Rugissement de lionceau puis accord de cuivres chaud en fa majeur (« lever de soleil »), glockenspiel, étincelles. |
| `animal_secret_baleine` | 1,2 s | Secret = Baleine céleste. | 0,7 | Chant de baleine la4 → do5 → fa5 → do5, 8 bulles d'étoiles qui montent, réverb « ciel ». |

Ducking de la musique : comme `resultat_*` pour `diamant_gain_*`, `renaissance` (musique à 0,3
pendant 4 s) et les touches de Secret.

---

## Vérification (puisqu'on ne peut pas écouter)

`analyse.py` mesure chaque fichier (`$PY audio/analyse.py [dossier]`) :
- **durée**, **pic** (dBFS), **RMS** (volume moyen), **volume ressenti** (0,4 s) et
  volume **« à l'oreille »** (le même après pondération K, proche des LUFS) ;
- **saturation** : nombre d'échantillons au maximum (doit être 0) ;
- **premier et dernier échantillon** d'un effet : doivent valoir 0 (fondus = pas de clic) ;
- **raccord de boucle** des musiques : le saut entre le dernier et le premier échantillon doit
  être plus petit que 99,9 % des sauts entre échantillons voisins du morceau, et le volume
  juste avant le raccord doit ressembler à celui juste avant les autres barres de mesure.

Le bilan final affiche « tout est conforme » ou signale les fichiers à vérifier.
