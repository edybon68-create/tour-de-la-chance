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
7. [ ] **Revue complète des détourages** : comparer chaque icône à son original, chercher les liserés, les trous, les ombres entourées de noir et les contours irréguliers. Corriger dans `blender/detourer_icones.py`, ou régénérer l'icône.
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
- Sauvegarde de l'auteur : la copier avant tout test, la restaurer après, et vérifier qu'elle est identique.
- Ne jamais compiler, ni modifier un script, pendant un mode Play que nous n'avons pas lancé.
- Ne jamais changer de compte Gemini ou ChatGPT, ne jamais saisir de mot de passe.
- Ne jamais toucher aux fichiers personnels de Téléchargements : seulement `Gemini_Generated_Image*` et `tdlc_*` récents.
- Refuser les cookies non essentiels. Rien de public, aucun envoi de message.
- Après chaque version : mettre à jour `docs/JOURNAL.md`.

## Journal de travail autonome
<!-- Une ligne par session : date, ce qui est fait, commit. -->
