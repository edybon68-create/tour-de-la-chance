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
1. [ ] **Packs de diamants** : les remettre dans la Boutique, visibles mais grisés « Bientôt disponible », dans une section belle et épurée. Aucun paiement branché.
2. [ ] **Fenêtre Inventaire un peu trop large** : elle touche les bulles des colonnes de menus. La réduire pour garder une marge nette à 1280 × 720 et à 1920 × 1080.
3. [ ] **Clic sur la carte d'étage** (grande carte du centre) : il doit rouvrir la fenêtre « toutes les infos » (récapitulatif des stats). C'est le **badge ÉTAGE** en haut à gauche qui ouvre L'Ascension.
4. [ ] **L'Ascension est « tristounette »** : la rendre vivante et festive. Idées :
   - lumière et rayons sur l'étage du joueur ;
   - petites animations (nuages qui dérivent, étoiles qui scintillent, drapeau) ;
   - dalles de palier plus spectaculaires ;
   - meilleure barre de progression ;
   - transition d'ouverture ;
   - corriger « Ma position » qui chevauche le « i » du bas ;
   - rayons de « Récupérer » trop discrets.
5. [ ] **Boutique épurée**, dans l'esprit de la Renaissance et du Casier :
   - moins de texte ;
   - cartes plus aérées ;
   - hiérarchie claire.
6. [ ] **Améliorations épurées**, même esprit.
7. [x] **Revue complète des détourages** : comparer chaque icône à son original, chercher les liserés, les trous, les ombres entourées de noir et les contours irréguliers. Corriger dans `blender/detourer_icones.py`, ou régénérer l'icône.
   - *Fait le 13/11* : nouvelle méthode de détourage (pente + ombre sous l'objet + matting), 27 icônes rendues dans Blender avec leur transparence exacte (`art/icones_alpha/`), 6 icônes de coffres refaites. Reste `kraken_bebe` à régénérer (lot 9, image 5). Planches : `docs/captures/qa_v7_detourage_00.png` à `_15.png`.
8. [ ] **Revue des modèles 3D** (accessoires, skins, coffres, tour, diamant, boîtes) : meilleur rendu possible, en corrigeant les formes grossières, les matériaux plats et les placements sur le cheval.
9. [ ] **Inspection QA complète**, puis ajout de ses trouvailles à cette liste et correction.
10. [ ] **Restes connus** :
    - particules des objets rares ;
    - réactions après un piège ;
    - révélation propre à chaque Secret ;
    - textures dessinées des skins ;
    - animation de l'échange (pièces volantes) ;
    - pictogramme « pas encore vu » de l'Index ;
    - mesure des performances au Profiler.
11. [ ] **Carte d'étage** (capture de l'auteur du 13/11) : elle est chargée. La simplifier, sans jamais cacher le gain du trésor (règle d'or).

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
10. [ ] *(à faire : l'éditeur était en mode Play lancé par quelqu'un d'autre ; les 6 icônes de coffres, désormais en 1024 px, sont encore limitées à 512 px à l'import)* **Réglages d'import des icônes incohérents.**
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
18. **Modèles 3D sur le cheval** (captures v5, donc peut-être anciennes, à vérifier en jeu) :
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
