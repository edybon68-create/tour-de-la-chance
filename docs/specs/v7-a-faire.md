# V7 — Travail autonome jusqu'au dimanche 15/11/2026

> Consignes de l'auteur, données le 13/11/2026 avant son départ. **À relire à chaque reprise de session**,
> avec `docs/JOURNAL.md`. On coche les points au fur et à mesure (`[x]`) et on commite après chaque
> amélioration terminée et testée (accord de l'auteur).

## Mandat de l'auteur
- **Améliorer le jeu en continu jusqu'au dimanche 15/11**, sans jamais rester sans rien faire. Claude prend les
  décisions seul, pour que la V7 soit parfaite au retour de l'auteur.
- **Interdits** : aucun nouveau coffre ou boîte, aucune nouvelle monnaie. Aucun paiement réel actif.
- **Autorisé** :
  - générer des images avec Gemini et les enregistrer sur le PC (dossier `art/`, via `ranger.sh`) ;
  - lancer le QA testeur pour trouver d'autres problèmes.
- **Goûts de l'auteur** :
  - il adore la DA de l'onglet Renaissance et du nouvel Inventaire Casier ;
  - il veut des écrans **plus simples et plus épurés**, surtout la Boutique et les Améliorations ;
  - le style reste Roblox, avec les couleurs du jeu.
- **Qualité** : faire très attention aux **détourages** de toutes les icônes et aux **modèles 3D**. On cherche le meilleur rendu possible.

## Demandes précises
1. [x] *(14/11 : vitrine fermée sans aucun bouton, captures `v7_boutique_packs.png`)* **Packs de diamants** : les remettre dans la Boutique, visibles mais grisés « Bientôt disponible », dans une section belle et épurée. Aucun paiement branché.
2. [x] *(14/11 : 1600 → 1510 px, marge mesurée de 49 unités, soit 33 px en 1280 × 720 et 49 px en 1920 × 1080 ; `v7_inventaire_1280.png`)* **Fenêtre Inventaire un peu trop large** : elle touche les bulles des colonnes de menus. La réduire pour garder une marge nette à 1280 × 720 et à 1920 × 1080.
3. [x] *(14/11 : clic sur la barre = carte rouverte 2 s avec toutes les infos ; badge = L'Ascension ; `v7_carte_etage_infos.png`)* **Clic sur la carte d'étage** (grande carte du centre) : il doit rouvrir la fenêtre « toutes les infos » (récapitulatif des stats). C'est le **badge ÉTAGE** en haut à gauche qui ouvre L'Ascension.
4. [x] *(14/11 : art bible §17.2 codée ; `v7_ascension_*.png`)* **L'Ascension est « tristounette »** : la rendre vivante et festive. Idées :
   - lumière et rayons sur l'étage du joueur ;
   - petites animations (nuages qui dérivent, étoiles qui scintillent, drapeau) ;
   - dalles de palier plus spectaculaires ;
   - meilleure barre de progression ;
   - transition d'ouverture ;
   - corriger « Ma position » qui chevauche le « i » du bas ;
   - rayons de « Récupérer » trop discrets.
5. [x] *(14/11 : §17.3 ; `v7_boutique_*.png`)* **Boutique épurée**, dans l'esprit de la Renaissance et du Casier :
   - moins de texte ;
   - cartes plus aérées ;
   - hiérarchie claire.
6. [x] *(14/11 : §17.4 ; `v7_ameliorations*.png`)* **Améliorations épurées**, même esprit.
7. [x] **Revue complète des détourages** : comparer chaque icône à son original, chercher les liserés, les trous, les ombres entourées de noir et les contours irréguliers. Corriger dans `blender/detourer_icones.py`, ou régénérer l'icône.
   - *Fait le 13/11* : nouvelle méthode de détourage (pente + ombre sous l'objet + matting), 27 icônes rendues dans Blender avec leur transparence exacte (`art/icones_alpha/`), 6 icônes de coffres refaites. Reste `kraken_bebe` à régénérer (lot 9, image 5). Planches : `docs/captures/qa_v7_detourage_00.png` à `_15.png`.
8. [x] **Revue des modèles 3D** (accessoires, skins, coffres, tour, diamant, boîtes) : meilleur rendu possible, en corrigeant les formes grossières, les matériaux plats et les placements sur le cheval.
   - *Fait le 14/11* : les 60 modèles ont été rendus sur le cheval (vues jeu, 3/4 et profil) avec `blender/revue_modeles.py`. 12 ont été corrigés : longue-vue, ancre, foulard, navire, tricorne, carapace, baleine, casquette, auréole, planète, plus la doublure des 5 coffres. Planches : `docs/captures/v7_modeles_*.png`. Aucun `attachOffset` à changer.
9. [ ] **Inspection QA complète**, puis ajout de ses trouvailles à cette liste et correction.
10. [ ] **Restes connus** :
    - particules des objets rares ;
    - réactions après un piège ;
    - révélation propre à chaque Secret ;
    - textures dessinées des skins ; *(14/11 : 17 textures branchées, carte de lumière comprise ; `v7_fiche_animal_tigre.png`, `v7_fiche_pirate_or_maudit.png`)*
    - animation de l'échange (pièces volantes) ; *(14/11 : faite, 10 pièces vers le solde)*
    - pictogramme « pas encore vu » de l'Index ;
    - mesure des performances au Profiler.
11. [x] *(14/11 : §17.5, puis retour 13)* **Carte d'étage** (capture de l'auteur du 13/11) : elle est chargée. La simplifier, sans jamais cacher le gain du trésor (règle d'or).

## Règles de sécurité (toujours)
- **Ne JAMAIS activer le son de l'ordinateur** (demande expresse de l'auteur) : ne pas toucher au volume du Mac (`osascript`, `afplay`, etc.), ne pas désactiver le « Mute Audio » de Unity, ne jouer aucun son à voix haute. Les sons se vérifient par le code ou les fichiers, pas en les écoutant.
- Sauvegarde de l'auteur : la copier avant tout test, la restaurer après, et vérifier qu'elle est identique.
- Ne jamais compiler, ni modifier un script, pendant un mode Play que nous n'avons pas lancé.
- Ne jamais changer de compte Gemini ou ChatGPT, ne jamais saisir de mot de passe.
- Ne jamais toucher aux fichiers personnels de Téléchargements : seulement `Gemini_Generated_Image*` et `tdlc_*` récents.
- Refuser les cookies non essentiels. Rien de public, aucun envoi de message.
- Après chaque version : mettre à jour `docs/JOURNAL.md`.

## Journal de travail autonome
<!-- Une ligne par session : date, ce qui est fait, commit. -->
- 14/11 : artiste 3D, points 12 (intérieur des coffres, f6237b2), 15 (3 icônes de catégories + 8 images d'échange) et 17 (planches de comparaison, 5 lots, 30 accessoires refaits ou repris, notes skins, lot 10 de prompts) ; commits f6237b2, 1209966, b01ab97, 13ba52d, 7da8f23, d5f6281 et lot 5.
- 14/11 : 17 textures de skins dessinées (point 10), branchement à faire par le programmeur.
- 14/11 : revue des modèles 3D (point 8 et QA 18) : 12 modèles corrigés, icône kraken rendue dans Blender ; commits 7a188b4, a9bc6ee, a71dff4, be13f17.
- 13/11 : détourages refaits (QA 1 à 5, 8, 9) et icônes des coffres rendues dans Blender ; mipmaps à faire (Unity en mode Play).

## Trouvé par le QA (V7)

> Inspection du 13/11/2026, **phase 1 seulement (lecture seule)** : un mode Play était déjà en cours
> dans l'éditeur (lancé par quelqu'un d'autre), donc pas de test en jeu. Sources : relecture du code,
> captures `v65_*` et `v6*`, comparaison automatique et visuelle des 127 icônes avec leurs originaux.
> Planches de comparaison (original | détourée sur fond sombre | sur fond rose) :
> `docs/captures/qa_v7_icones_planche_00.png` à `_12.png`, gros plans `qa_v7_icones_zoom_a.png` / `_b.png`,
> accessoires sur le cheval `qa_v7_modeles_cheval_1.png` à `_3.png`.
> Rien n'a été corrigé. Les points marqués « à vérifier en jeu » demandent la phase 2 (mode Play).

### Important

1. [x] *(corrigé : nouvelle méthode de détourage ; poussin, ruche, bonnet grenouille et chat du sac utilisent la transparence du rendu Blender)* **Icônes détourées abîmées : des parties blanches ou grises de l'objet sont devenues transparentes.**
   Le zèbre, la marinière, le poussin, la ruche et la médaille d'argent sont méconnaissables. Le bonnet grenouille a perdu le blanc des yeux, et le chat du sac a la tête trouée.
   - *Cause probable* : la détection des « ombres portées » (`blender/detourer_icones.py:86`, puis `:89`) prend pour de l'ombre tout pixel gris ou blanc un peu sombre dont la teinte ressemble au fond lavande. S'il touche le fond, le remplissage depuis les bords le fait disparaître.
   - *Piste* : mettre ces icônes dans `SANS_OMBRE` (l. 48), ou ne chercher l'ombre que sous l'objet.
   - *Capture* : planches 02 (zèbre), 03 (bonnet grenouille), 04 (chat du sac), 09 (médaille d'argent), 11 (marinière), 12 (poussin, ruche).
2. [x] *(corrigé avec le point 1)* **Médaille d'argent cassée dans le Classement** (2e place). L'icône `medaille_argent.png` est presque noire et trouée (point 1).
   - *Fichier* : `Assets/UI/Popups.uss:504`.
   - *Capture* : `v6fix_classement_record.png` (médaille n° 2 terne) et planche 09.
3. [x] *(corrigé, sauf `kraken_bebe` : corps violet comme son ombre, à régénérer, lot 9 image 5)* **Ombres portées gardées et entourées de contour noir.** On voit une tache violette sous l'objet, cernée de noir, qui fait « autocollant raté » : bandana, tricorne, bébé kraken, perroquet, boussole, cache-œil, longue-vue, foulard marin, ancre, chapeau de capitaine et loup corsaire (plus légèrement : drapeau du record, icône Renaissance).
   - *Cause probable* : ces ombres sont plus sombres ou plus saturées que ce qu'accepte `SEUIL_OMBRE` (l. 45) et le test `lum > 0.55 * lum_fond` (l. 86). Elles restent donc opaques et reçoivent le contour.
   - *Capture* : planches 02, 03, 06, 08, 10, 12.
4. [x] *(corrigé : `OBJETS_TROUES` + trous qui suivent aussi l'ombre ; collier et écharpe renard : transparence Blender)* **Fond lavande enfermé dans les boucles.** L'intérieur de la chaîne (ancre, boussole), de la lanière (cache-œil, longue-vue), du collier à médaille, de l'écharpe renard et les trous des yeux du loup corsaire sont remplis de lavande opaque au lieu d'être transparents.
   - *Cause* : `detourer_icones.py:135` rend opaque toute zone transparente entourée par l'objet. C'est voulu pour le bocal et le fantôme, mais faux pour les objets en anneau.
   - *Piste* : ajouter ces icônes à `OBJETS_TROUES` (l. 37).
   - *Capture* : planches 00, 03, 05, 06, 08.
5. [x] *(corrigé : ombre retirée et marge automatique avant le contour ; aucune icône ne touche plus le bord)* **Icônes coupées au bord** : `chapeau_capitaine` (6 % du bord opaque) et `kraken_bebe` (2 %). L'ombre ou l'objet touche le bord de l'image, et le contour s'arrête net.
   - *Capture* : planches 03 et 08.
6. **L'Ascension : les boutons « i » sont à moitié cachés** derrière la barre de défilement, sur toutes les dalles.
   - *Cause* : le contenu a une largeur fixe (`.asc-contenu { width: 1032px; }`, `Assets/UI/V65.uss:351`, et `ContentWidth = 1032f`, `TrophyRoadPage.cs:33`), alors que la zone visible, moins la barre de défilement, est plus étroite. Le « i » est placé à `left: 924px` (`V65.uss:447`), donc son bord droit (988 px) passe sous la barre.
   - *Capture* : `v65_ascension_info.png`, `v65_ascension_palier.png`, `v65_ascension_recuperer.png` (les « i » sont coupés à droite).
7. **« Ma position » recouvre le « i » de l'étage du bas** (reste connu, cause confirmée) : `.asc-ma-position { right: 40px; bottom: 16px; }` (`V65.uss:454`) est posé dans la même colonne que les « i ».
   - *Capture* : `v65_ascension_1280.png`.
8. [x] *(corrigé : rendues dans Blender depuis les vrais modèles, 1024 px, contour noir ; `amelioration_coffres` = coffre de diamant entrouvert plein de pièces)* **Améliorations : icônes des coffres sans contour noir et de mauvaise qualité.** `coffre_bois/bronze/argent/or/diamant.png` et `amelioration_coffres.png` sont en 512 px, sans contour « autocollant », et n'ont pas d'original dans `art/icones/` (rendus de la v3). Elles jurent à côté des autres icônes (le coffre blanc de « Coffres précieux »).
   - *Fichier* : `Assets/Resources/UpgradeCatalog.asset`.
   - *Capture* : `v6fix_ameliorations.png`, planches 00 et 04.

### Mineur

9. [x] *(corrigé, vérifié en gros plan)* **Contours irréguliers et petites encoches noires** là où l'objet a des reflets blancs :
   - pompon et côté droit du chapeau de fête ;
   - bouton de la casquette ;
   - museau du tigre, du chat roux et du caméléon ;
   - nez du cristal ;
   - rayures du sucre d'orge ;
   - reflet en haut à gauche de la **pièce** (visible partout dans le jeu) ;
   - lanterne des abysses, brume de l'or maudit ;
   - oreilles de lapin, revers du bonnet, monture des lunettes cœur, socle du drapeau.

   *Cause* : même mécanisme que le point 1, en moins grave. *Capture* : `qa_v7_icones_zoom_a.png`, `_b.png`.
10. [x] *(14/11 : mipmaps activées sur toutes les icônes, `IconAutoBinder` les active aussi pour les nouvelles ; coffres à 1024 px ; ruban en 9-slice 120 px)* **Réglages d'import des icônes incohérents.**
    - 79 icônes n'ont pas de mipmaps (`enableMipMap: 0`) : une image de 1024 px affichée en 60-100 px devient crénelée et scintille.
    - 48 autres sont limitées à 512 px avec mipmaps.
    - *Piste* : harmoniser, avec mipmaps activés et 1024 px partout.
    - *Fichiers* : `Assets/Art/icones_detourees/*.png.meta`.
11. **Boutique trop chargée** (rejoint le point 5 de la liste) :
    - petits textes blancs sur dégradé orange ou rose peu lisibles (« 16 objets à collectionner », « Tout collectionner ») ;
    - « Il te manque 2 » blanc sur jaune ;
    - porte-monnaie affiché en double (HUD et haut de la Boutique, idem Index et Améliorations) ;
    - long paragraphe dans « Contenu et taux ».

    *Capture* : `v65_boutique_classiques.png`, `v65_boutique_echanges.png`, `v65_boutique_contenu.png`.
12. **Améliorations : redite.** « Maintenant ×1,4 → Prochain ×1,6 » puis « Au prochain niveau : trésors +60 % » disent la même chose (`Scripts/UI/UpgradesPage.cs:151-161`). À épurer (point 6 de la liste).
13. **Inventaire : le compteur « 11 » dépasse de l'entrée active « Accessoires »** quand l'icône grossit à 64 px.
    - *Calcul* : 12 + 10 de marges, 64 + 2 d'icône, environ 150 de texte et environ 40 de compteur, soit plus que les 256 px de l'entrée (`V65.uss:41-42`, `:62`, `:74`).
    - *Capture* : `v65_inventaire_accessoires.png`. À vérifier en jeu (la police est passée à 26 px depuis).
14. **Inventaire : bouton « Aucun doublon » gris clair sur gris**, texte presque illisible à 1280 × 720.
    - *Capture* : `v65_inventaire_croix_1280.png`.
15. **Incohérence de compte** : l'Inventaire affiche « Skins 6 / 45 » et l'Index « Skins 5 / 44 » (le skin Bleu gratuit n'est compté que d'un côté).
    - *Capture* : `v65_inventaire_croix_1280.png` et `v65_index_categories.png`.
16. **L'Ascension, fenêtre « i » trop bavarde** : 3 lignes en petit gris, dont « risque 29,17 % » coupé à la ligne. Le titre « Étage 7 » est décalé à droite (marge de 90 px pour la vignette, `V65.uss:468-469`).
    - *Capture* : `v65_ascension_info.png`.
17. **Index : textes des cartes trop petits** (« 10 % », « +1 % », « Pas encore »), illisibles à 1280 × 720.
    - *Capture* : `v65_index_categories.png`.
18. [x] *(corrigé le 14/11, sauf la plume : refaite plus large, c'est désormais une vraie plume)* **Modèles 3D sur le cheval** (captures v5, donc peut-être anciennes, à vérifier en jeu) :
    - la longue-vue traverse la tête comme une broche ;
    - l'ancre pend loin sur le côté ;
    - la carapace ressemble à un tambourin jaune (rien à voir avec son icône verte) ;
    - le foulard marin est surtout un anneau blanc (l'icône est bleu marine) ;
    - le navire fantôme est minuscule, avec un anneau autour de la taille ;
    - la baleine et la planète flottent loin et petites ;
    - la plume du tricorne est une simple pointe rouge ;
    - les coffres en jeu ont un intérieur de couvercle brun uni (`v65_diamant_4.png`) ;
    - l'auréole flotte haut au-dessus de la tête (`v65_inventaire_croix_1280.png`).

    *Capture* : `qa_v7_modeles_cheval_1.png` à `_3.png`.

### Icônes à refaire (détourage), et pourquoi
- **À régénérer ou redétourer en priorité** (objet abîmé) :
  - `animal_zebre`, `pirate_mariniere` : rayures blanches effacées ;
  - `poussin` : coquille effacée ;
  - `ruche` : pot blanc et ailes des abeilles effacés, îlot noir flottant ;
  - `medaille_argent` : pièce presque entièrement noire ;
  - `bonnet_grenouille` : blanc des yeux transparent ;
  - `chaton_sac` : tête du chat trouée ;
  - `perroquet` : contour de l'œil effacé, et ombre gardée.
- **Ombre portée à retirer** :
  - `bandana_pirate`, `tricorne`, `kraken_bebe` (et coupé au bord) ;
  - `chapeau_capitaine` (et coupé au bord) ;
  - `foulard_marin` (et liseré blanc perdu) ;
  - `boussole`, `cache_oeil`, `longue_vue`, `ancre_doree`, `loup_corsaire` ;
  - plus légers : `icone_rebirth`, `drapeau_record`.
- **Intérieur des boucles à rendre transparent** (à ajouter à `OBJETS_TROUES`) :
  - `ancre_doree`, `boussole`, `cache_oeil`, `longue_vue` ;
  - `collier_medaille`, `echarpe_renard`, `loup_corsaire`.
- **Retouche du contour** (encoches noires sur les reflets) :
  - `chapeau_fete`, `chapeau_casquette`, `chapeau_bonnet` ;
  - `piece` (prioritaire, visible partout) ;
  - `animal_tigre`, `animal_chat_roux`, `animal_cameleon`, `animal_flamant` ;
  - `matiere_cristal`, `motif_sucre_orge` ;
  - `pirate_abysses`, `pirate_or_maudit` ;
  - `oreilles_lapin`, `lunettes_coeur`, `masque_heros`.
- **À refaire au style du jeu** (contour noir, 1024 px, original dans `art/icones/`) :
  - `coffre_bois`, `coffre_bronze`, `coffre_argent`, `coffre_or`, `coffre_diamant` ;
  - `amelioration_coffres`.
- Aucune icône franchement floue. `boite_skins_1` n'est qu'en 768 px, ce qui reste acceptable.
- 14/11 02h : le code des points 1-3, 4, 5, 6 et 11 (art bible §17) est ÉCRIT par unity-dev mais NON compilé ni testé, car un mode Play étranger tourne depuis le 13/11 vers 18 h (question posée à l'auteur, ne pas l'arrêter sans son accord). Vérif hors éditeur OK (`scratchpad/check.sh`, 0 erreur). Quand le Play s'arrête : suivre le « Reste à faire » du compte rendu unity-dev (sauvegarde, recompile, showDiamondPacks + rebirthIcon par SerializedObject, captures, restauration, commits par écran). Mute Audio Unity activé, à garder.
- 14/11 : tout ce qui pouvait se faire sans Unity est fait (détourages, lot 9, revue 3D, 17 textures de skins). À CHAQUE REPRISE : vérifier `EditorApplication.isPlaying` par `script eval`. Tant qu'il vaut vrai et que l'auteur n'a pas répondu, ne rien relancer de coûteux. Dès qu'il vaut faux : relancer unity-dev (agent du 13/11) pour compiler, brancher (showDiamondPacks, rebirthIcon, textures de skins selon `v7-textures-skins.md`, mipmaps : IconAutoBinder force mipmapEnabled=false, à changer), tester, puis commiter par écran. Ensuite le QA phase 2.

## Retours de l'auteur du 14/11 matin (captures `docs/references/v7_retour_1..8.png`)
12. [x] **Coffre ouvert** : l'intérieur du couvercle est jaune (coussin capitonné de la revue 3D), ça fait faux. Revenir à un intérieur sombre et propre, cohérent avec la caisse.
    - *Fait le 14/11* : coussin retiré (ses normales étaient retournées : Unity montrait le cadre en laiton derrière). Dessous = doublure foncée de la caisse + fin cadre biseauté dans le bois du couvercle, sur les 5 coffres. Planche : `docs/captures/blender_coffres_planche_ouverts.png`.
13. [x] *(14/11 : gain en grand chiffre or sur une lueur, plus de pilule ; 3 niveaux de texte : titre 44, gain 76, le reste 22 ; `v7_carte_etage_arrivee.png`)* **Carte d'étage (clic sur la barre du haut)** : plus intuitive qu'avant, MAIS il n'accroche pas au « bouton pièce » (la pilule Trésor avec la pièce) ; et revoir la **hiérarchie** : les infos du bas sont de tailles toutes différentes, c'est incohérent → tailles harmonisées, 2-3 niveaux clairs.
14. [x] *(14/11 : phrase sous chaque nom, aucun défilement ; `v7_ameliorations.png`)* **Améliorations** (DA parfaite) : on ne comprend pas ce que font « Coffres précieux » et « Coffre en plus » sans deviner → une phrase courte et claire visible sur la carte (ex. « Coffres plus rares = trésors plus gros », « Plus souvent un coffre de plus »). **Retirer le défilement** : tout tient sur un écran.
15. [x] *(14/11 : prix sur le bouton (gris s'il manque), 8 échanges dégressifs de `v7.md` §1, clic sur le compteur de diamants, « Contenu et taux » refait ; icônes de catégories branchées dès leur arrivée dans `icones_detourees/`, repli = image de la 1re boîte)* **Boutique** :
    - afficher le **prix** directement sur le bouton (« Ouvrir ◆ 5 », grisé si pas assez) au lieu de « Encore ◆ 1 » ;
    - **Échanges** : plus de packs, avec **réduction sur les gros** (plus de pièces par diamant), en restant raisonnable (pas « broken ») ; remplir l'espace ;
    - **raccourci** : cliquer le compteur de diamants en haut (HUD) doit ouvrir la Boutique sur la section Diamants (packs + échanges) ;
    - **icônes des 3 catégories** de boîtes (Classiques, Pirates, Animaux) : en générer de **neutres** (ni skin ni accessoire), une par catégorie ;
      *(14/11 : faites, rendues dans Blender : `categorie_classiques` (étoile de fête dorée), `categorie_pirates` (carte au trésor roulée), `categorie_animaux` (empreinte de patte sur une feuille), dans `Assets/Art/icones_detourees/`. Reste au programmeur à les brancher sur les onglets. Aussi : les 8 images des lots d'échange `echange_poignee` … `echange_montagne`, même famille, IconAutoBinder les relie par leur nom.)*
    - **refaire l'interface « i » / Contenu et taux** des boîtes (jugée « dégueulasse ») dans le style Casier/Renaissance.
16. [x] *(14/11 : retirés des petits textes de la carte d'étage, des Améliorations, de la Boutique et du ×2 de L'Ascension ; gardés sur les titres et boutons ; « Aucun doublon » lisible)* **Contours noirs** : en enlever là où ils rendent le texte illisible (garder où c'est beau).
17. [ ] **Qualité 3D (demande personnelle forte)** : beaucoup de skins/accessoires 3D sont laids et ne ressemblent pas à leur icône. Refaire les modèles au niveau de l'icône (règles ajoutées à `.claude/agents/artiste-3d.md`), et régénérer avec Gemini les icônes qui ne sont pas incroyables. Le jeu doit être parfait.
    - *14/11, en cours* : planches icône | 3/4 | sur le cheval pour les 44 accessoires (`docs/captures/v7_qualite_accessoires_1..6.png`) et les 45 skins (`v7_qualite_skins_1..6.png`), script `blender/planche_qualite.py`.
    - **Classement des accessoires, du pire au meilleur** : masque héros, loup corsaire, auréole, nœud papillon, perroquet, longue-vue, casquette, couronne, ailes d'ange, ancre, bandana, navire fantôme, ailes de papillon, ruche, chaton du sac, planète, cache-œil, moustaches, carapace, baleine, chapeau de fête, boussole, foulard, lunettes de soleil, bonnet, ailes de chauve-souris, truffe, écharpe renard, collier médaille, bocal, poussin, bois de cerf, sorcier, oreilles de lapin, lunettes hibou, tricorne, capitaine, écharpe, collier de fleurs, haut-de-forme, bonnet grenouille, oreilles de chat, lunettes cœur, bébé kraken.
    - **Refaits** : lot 1 (masque héros, loup corsaire, auréole, nœud papillon, perroquet, longue-vue, casquette, couronne : `v7_qualite_lot1_avant_apres.png`), lot 2 (ailes d'ange, ailes de papillon, planète, chapeau de fête, ancre : `v7_qualite_lot2_avant_apres.png`).
    - Lot 3 (7da8f23) : retours du chef de projet sur le lot 1 (masques en tissu plein, nœud gonflé, perroquet ×1,7, longue-vue laiton clair et lentille bleue), navire fantôme sans cerceau, bandana à pois peints. Lot 4 (d5f6281) : carapace, cache-œil, baleine, lunettes de soleil, bonnet, tricorne. Lot 5 : poussin ×1,4, collier de fleurs, écharpe plus grosse, boussole. Planches `v7_qualite_lot3..5_avant_apres.png`.
    - Lot 6 : les 6 modèles du lot 10 refaits d'après les nouvelles icônes Gemini (moustaches : nez en cœur, grosses moustaches arrondies ; truffe : bombée, narines ovales, langue ; bois de cerf caramel à bouts crème sur serre-tête ; ailes de chauve-souris lilas, os prune, fermoir étoilé ; chaton plus grand dans un panier caramel à bord crème ; ruche en paille dorée à 4 anneaux, entrée sombre, miel qui coule). Planche `v7_qualite_lot6_avant_apres.png`. À reprendre si besoin : les coussinets blancs des moustaches restent petits vus du jeu.
    - **Reste** : les autres accessoires du classement sont jugés fidèles à leur icône (vérifiés sur planche). À surveiller : la baleine, plus grosse, passe par le centre de la tête sur sa trajectoire en 8 (`AccessoryRig.AnimateWhale`, x = 0 et z = 0,05) : déjà vrai avant, plus visible maintenant (au programmeur). Ruche, chaton du sac, moustaches, truffe, bois de cerf et ailes de chauve-souris attendent leurs nouvelles icônes (`art/prompts/lot10.md`, à générer par le chef de projet), puis on refera le modèle d'après elles.
    - **Skins** : corrections précises pour le programmeur dans `docs/specs/v7-textures-skins.md` (caméléon gris, métaux trop sombres, lagon, lion, grenouille, coccinelle, sucre d'orge, galaxie, tigre).
    - Nouveaux matériaux `M7_*` (or clair, plumes) : Unity créera leurs `.mat` à l'import.

> 14/11 : l'auteur adore L'Ascension et le reste de la V7 (« continue comme ça, on vise la perfection »). Il ne touchera plus à son ordinateur jusqu'à son retour : **ne lui poser aucune question**, tout décider seul, ne jamais attendre une réponse. Son mode Play ne tournera plus : l'éditeur est à nous.
- 14/11 matin (unity-dev) : points 1-6, 10 (textures, échange, import des icônes), 11, 13-16 compilés, testés en mode Play avec captures `docs/captures/v7_*.png`, sauvegarde de l'auteur restaurée et vérifiée identique.

## Trouvé par le QA (V7, phase 2)

> Inspection en mode Play le 14/11/2026, sur les commits jusqu'à b01ab97, en 1280 × 720 puis en 1920 × 1080.
> Mute Audio resté activé, aucun son joué. Sauvegarde copiée dans `TourDeLaChance-sauvegardes/qa_20261114_0740/`,
> puis restaurée par Unity : elle est vérifiée identique (`diff` vide). Play arrêté.
> Captures : `docs/captures/qa_v7p2_*.png`. La console ne montre aucune erreur venant de nos scripts.
>
> **Déjà corrigé depuis la phase 1** :
> - les « i » de L'Ascension sont entiers ;
> - « Aucun doublon » est lisible ;
> - l'Inventaire laisse une marge avec les bulles ;
> - l'auréole est mieux posée ;
> - les packs de diamants sont là, grisés ;
> - la carte d'étage est simplifiée, et son clic ouvre bien les détails.

### Important

1. **Renaissance : le mot « Améliorations » est coupé en « Amélioratio / ns »** sous l'icône de « Tu perds ». « Nouvelles boîtes » tient aussi sur deux lignes, en tout petit.
   - *Fichier* : `Scripts/UI/RebirthPage.cs:93` (libellé trop long pour la largeur de l'icône).
   - *Capture* : `qa_v7p2_renaissance_1280.png`, `qa_v7p2_renaissance_1920.png`.
2. **Boutique : titres de boîtes sur 2 ou 3 lignes, et barre de collection écrasée.**
   - « Boîte d'accessoires n°1 » occupe 3 lignes, « Pirates : accessoires » et « Animaux : accessoires » 2 lignes. Les deux cartes d'une même rangée n'ont donc plus la même mise en page.
   - En 1920 × 1080, la barre « 11 / 16 » est aplatie et son texte coupé.
   - Le texte de la barre, blanc 20 px sur orange ou jaune, est illisible.
   - *Fichiers* : `UI/V7.uss:334-335` (`.bq7-titre-carte`, 32 px, `white-space: normal`) et `:345` (texte de la barre).
   - *Piste* : titre court (« Skins n°1 », « Accessoires n°1 ») ou police plus petite sur une seule ligne.
   - *Capture* : `qa_v7p2_boutique_1920.png`, `qa_v7p2_boutique_classiques_1280.png`, `qa_v7p2_boutique_pirates_1280.png`.
3. **Classement : « Ton record : étage 8 », alors que la meilleure ligne de la liste est « étage 7 ».** Le record (`record_etage` = 8) n'a jamais été envoyé au classement local, qui plafonne à 7. C'était déjà signalé en v6 (`qa_v6_classement_record_8_liste_7.png`) et ce n'est toujours pas corrigé.
   - *Fichier* : `Scripts/UI/LeaderboardPopup.cs:115` (titre lu ailleurs que la liste). Il faudrait aussi rattraper le record manquant dans le classement local.
   - *Capture* : `qa_v7p2_classement_1280.png`.
4. **Skins métalliques salis** : sur « Argent » et « Or massif », de grosses taches brunes apparaissent au museau, au poitrail et au socle (reflet d'un environnement brun). « Blanc nacré » est blanc uni, sans aucun reflet nacré. Dans le faisceau bleu d'ouverture, « Argent » paraît en verre bleu.
   - *Cause probable* : la réflexion ou la sonde d'environnement des matériaux métalliques, dans la vitrine comme dans l'ouverture.
   - *Capture* : `qa_v7p2_vitrine_skins_1280.png` (5e et 6e), `qa_v7p2_ouverture_3_1280.png`.
5. **Images de catégories et d'échange non branchées.**
   - Les 3 `categorie_*.png` et les 8 `echange_*.png` sont dans `Art/icones_detourees/`, mais l'en-tête des sections montre encore la 1re boîte, et les échanges des piles de pièces dessinées.
   - *Cause* : `Resources/EconomyCatalog.asset:31-66` (`icon: {fileID: 0}` pour les 8 lots) et `Resources/UiImages.asset` (aucune entrée `categorie*`). `IconAutoBinder` n'a pas été relancé.
   - *Capture* : `qa_v7p2_boutique_diamants_1280.png`, `qa_v7p2_boutique_classiques_1280.png`.

### Mineur

6. **L'Ascension** :
   - la carte du palier 30, tout en haut, est coupée par le bord de la zone (`TopPadding = 40f`, `TrophyRoadPage.cs:52`, trop petit pour une carte de palier) ;
   - la lueur sous le cheval du joueur ressemble à un cadre jaune vide (`asc-toi__lueur`, `V65.uss:415`) ;
   - les numéros d'étage normaux (« 3 », « 29 ») sont minuscules à côté de la pastille de l'étage actuel ;
   - « Ma position » est posée à cheval sur la bordure du haut de la fenêtre.

   *Capture* : `qa_v7p2_ascension_haut_1280.png`, `qa_v7p2_ascension_1280.png`.
7. **Fenêtre « i » de L'Ascension inchangée** : trois lignes de petit gris, « risque 29,17 % » coupé à la ligne, titre décalé (point 16 de la phase 1).
   - *Capture* : `qa_v7p2_ascension_info_1280.png`.
8. **Inventaire, vitrine** : quand la pastille de rareté et celle du bonus ne tiennent pas sur une ligne (« Peu commun » + « +2 % de pièces »), elles passent sur deux lignes et la scène du cheval rétrécit. La vitrine « saute » d'un objet à l'autre.
   - « Avec cet objet : +16 % » s'affiche en vert alors que c'est une baisse (+29 % → +16 %) : `InventoryPage.cs:389`.
   - En 1920 × 1080, « Accessoires » et son compteur « 11 » se touchent.
   - Les deux points bleus à droite du titre « Accessoires » n'ont pas de légende.
   - *Capture* : `qa_v7p2_vitrine_skins_1280.png`, `qa_v7p2_inventaire_accessoires_1280.png`, `qa_v7p2_inventaire_1920.png`.
9. **Boutique, petits textes** :
   - « Ta cagnotte en jeu (34) ne se dépense pas… » en gris minuscule ;
   - porte-monnaie en double avec le HUD ;
   - pourcentages sous les aperçus et rubans « +5 % » des échanges en environ 9 px ;
   - « Bientôt disponible » posé en travers des packs 2 et 3, qui cache leurs images ;
   - grand vide au-dessus de « Packs de diamants ».

   *Capture* : `qa_v7p2_boutique_*_1280.png`.
10. **Contenu et taux** :
    - bouton « Retour » minuscule ;
    - solde affiché sur une ligne à part ;
    - pastilles de bonus (« +1 % ») illisibles ;
    - grille de 4 colonnes avec une grande marge vide à droite.

    *Capture* : `qa_v7p2_contenu_taux_1280.png`.
11. **Ouverture d'une boîte** : le panneau du résultat recouvre le socle, et « Rare ! » est bleu sur bleu dans le faisceau.
    - *Capture* : `qa_v7p2_ouverture_3_1280.png`.
12. **Améliorations** (bien plus épurées) :
    - la note du bas « Chaque amélioration compte à partir du prochain étage » est quasi invisible (gris très clair sur blanc) ;
    - les barres « Avant / Après » de Coffres précieux n'ont pas de légende des couleurs ;
    - l'image de « Coffre en plus » (3 coffres) est plate et sans contour ;
    - « Risque : 29 % > 27 % » est minuscule.

    *Capture* : `qa_v7p2_ameliorations_1280.png`.
13. **Partie** :
    - « Reprise 1 sur 5 · Tu as 4 » en tout petit, peu contrasté (`qa_v7p2_piege_1280.png`) ;
    - les boutons « Oui, continuer » et « Annuler » ont un texte petit (`qa_v7p2_continuer_confirm_1280.png`) ;
    - la carte « Reprise réussie » cache entièrement le cheval pendant quelques secondes (`qa_v7p2_apres_reprise_1280.png`), et la carte des détails aussi (`qa_v7p2_carte_details_1280.png`). Cette carte répète « 2 % » et « +29 % » (pastilles, puis ligne du bas) ;
    - les coffres d'argent fermés sont bleus, mais une fois ouverts, ceux qui contenaient un trésor deviennent jaune or (`qa_v7p2_suspense_1280.png`), ce qui peut faire croire à une autre rareté.
14. **Réglages : « Tour de la Chance · version 6 »** alors qu'on est en V7 (`UI/Panels.uxml:109`).
    - *Capture* : `qa_v7p2_reglages_1280.png`.
15. **Renaissance** : « Tu gardes tout le reste ! » vert contouré de noir sur blanc, peu lisible ; les deux lignes du bas sont en petit gris.
    - *Capture* : `qa_v7p2_renaissance_1280.png`.
16. **Index** : textes des cartes toujours minuscules (« 10 % », « Pas encore », « +1 % ») ; « Tout collectionner : 5 » en marron sur fond clair.
    - *Capture* : `qa_v7p2_index_1280.png`.
17. **Classement** : « Classement local » écrit deux fois (bandeau du haut et bas de la fenêtre).
    - *Capture* : `qa_v7p2_classement_1280.png`.
18. **Avertissement USS** : `:last-child` n'est pas reconnu par UI Toolkit, donc la règle ne fait rien (`UI/V65.uss:48`, « Unknown pseudo class "last-child" » dans la console).
