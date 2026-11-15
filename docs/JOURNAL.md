# Journal du projet — Tour de la Chance

> **À quoi sert ce fichier ?** C'est le résumé de toutes nos conversations, version par version.
> Le chef de projet (Claude) le lit en début de session au lieu de relire tout l'historique : ça
> économise des crédits. **Règle : à la fin de chaque version, on ajoute une section ici.**
> Les détails sont dans `docs/GDD.md`, `docs/specs/*.md`, `docs/art-bible.md` et l'historique git.

## Qui fait quoi
- **L'auteur** (débutant en programmation, francophone) décide, teste lui-même le jeu et dicte les versions.
- **Claude** est le chef de projet. Il délègue aux sous-agents de `.claude/agents/` : game-designer, directeur-artistique, artiste-3d, sound-designer, unity-dev, qa-testeur.
- **Outils** :
  - Unity 6000.3 (URP, UI Toolkit, Input System), piloté par le CLI `unity` ;
  - Blender en script (`blender/`) ;
  - images générées par Gemini dans le navigateur de l'app, détourées par `blender/detourer_icones.py` ;
  - sons synthétisés en Python (`audio/`).

## Préférences et règles de l'auteur (à respecter)
- **Langue** : tout est en français et pédagogique.
- **Tests** : l'auteur teste lui-même. Pas de QA sauf s'il le demande (il l'a demandé pour préparer la V6).
- **Sauvegarde** : elle est réelle. La copier avant tout test, puis la restaurer et vérifier qu'elle est identique.
- **Comptes de génération** : ne **jamais** changer de compte Gemini ou ChatGPT pour contourner une limite (règles des services). On attend la réinitialisation, ou on rend les images dans Blender.
- **Argent réel** : jamais de paiement réel actif. Les packs de diamants sont grisés, « Bientôt disponible ».
- **Classement** : local pour l'instant, mais tout est déjà prêt pour Unity Gaming Services.
- **Style visuel** : façon Roblox. Pop-ups blancs à contour épais, icône qui déborde en haut à gauche, grand titre blanc contouré de noir, icônes à contour noir épais en style autocollant. On garde la croix de fermeture actuelle.

## Historique des versions
- **v1 (MVP)** : tour, 3 coffres dont 1 piégé, quitte ou double, cagnotte et porte-monnaie.
- **Boutique et boîtes** : 32 cosmétiques, raretés de Commun à Secret, boîtes à tirage.
- **v3** :
  - personnage cheval d'échecs, décor qui monte dans le ciel ;
  - améliorations (gains, rareté des coffres, coffre en plus, piège) ;
  - coffres de rareté, 4 panneaux sur le côté, textes aléatoires, cinématique Secret.
- **v4** :
  - interface façon Roblox avec pop-ups, Réglages, badge d'étage, Galaxie animée ;
  - bonus de pièces par rareté, améliorations jusqu'au niveau 10, classement local (prêt pour UGS).
- **v5** (commits f352986, 4207e0a…) :
  - diamants (1 à 5 % des trésors, jamais perdus), reprise d'une partie pour 1 diamant ;
  - Renaissance (10 000 pièces, puis ×2 pièces et ×2 diamants) ;
  - boîtes Pirates à 5 diamants (Kraken…) et boîtes Animaux à 500 pièces après la Renaissance ;
  - boutique à onglets, Index refait, carte d'étage, 2 accessoires au maximum ;
  - ouverture des boîtes en 3D, 16 accessoires modernisés, 28 nouveaux accessoires, 28 skins, 34 sons.
  - Spec : `docs/specs/v5.md` et `v5-catalogue.md`.
- **v5.1** (commits 1023eaa, 6fc8782) :
  - boutique sur une seule page (les onglets font défiler jusqu'à la section) ;
  - pop-ups façon Roblox ;
  - contour noir épais sur toutes les icônes ;
  - pictogrammes diamant et pièce dans les textes.
  - 49 icônes du lot 6 sur 60, dont 20 rendues dans Blender (`blender/rendu_icones.py`).
- **v6** (13-14/11/2026). Liste de l'auteur et du QA dans `docs/specs/v6-a-faire.md`, spec dans `docs/specs/v6.md`, design dans l'art bible §15.
  - Bugs du QA corrigés.
  - Route des trophées (clic sur l'étage), reprise à coût qui double (1, 2, 4, 8, 16 ; 5 au plus) avec confirmation sur Rejouer.
  - Échange diamants → pièces sans plafond (200 pièces le diamant), améliorations −25 / −35 %.
  - Bonus de diamants Pirates, récompenses de collection et de palier.
  - « Équiper le meilleur », « Mes skins », Index aux visuels cachés.
  - Fiche agrandie qu'on fait tourner, avec flèches.
  - Renaissance refaite, 25 bulles rendues dans Blender, graphismes Faible / Moyen / Élevé.
  - Nouvelles icônes Boutique (échoppe) et Réglages.

- **v6.5** (13/11/2026). Liste et décisions dans `docs/specs/v6.5.md`, design dans l'art bible §16.
  - La confirmation passe de Rejouer (direct) à « Continuer — X diamants ».
  - 6 aperçus sur les boîtes Pirates (2 Super rares, 2 Légendaires, 2 « ? »).
  - Index par catégorie : Skins et Accessoires seulement.
  - Inventaire refait façon « Casier » de Fortnite : menu à gauche, grille au milieu, cheval qu'on fait tourner à droite (`DragToRotate`).
  - « L'Ascension » remplace la route des trophées : 12 dalles Blender (`blender/rendu_dalles.py`), qui sont le vrai étage de la tour entouré du ciel de chaque palier (`TierForFloor` : 1-3, 4-6, 7-9, 10+). Presque pas de texte, détail dans le bouton « i ».
  - Petits textes de la Boutique allégés (ombre au lieu du contour épais), diamant 3D définitif (`blender/diamant.py`, `.fbx`, le projet n'a pas d'importeur glTF).
  - Leçons : chaque décor doit correspondre à ce qu'on voit en jeu au même étage ; l'auteur joue souvent dans l'éditeur, il ne faut jamais compiler pendant son mode Play.

- **v7** (13-14/11/2026, travail autonome pendant l'absence de l'auteur). Liste et journal détaillé : `docs/specs/v7-a-faire.md` ; échanges : `docs/specs/v7.md` ; design : art bible §17 ; textures : `docs/specs/v7-textures-skins.md`.
  - Écrans épurés : carte d'étage (gain en vedette), L'Ascension festive (ciels, nuages, projecteur, guirlandes, confettis), Boutique (prix sur le bouton, onglet Diamants, Contenu et taux façon Casier, icônes de catégories), Améliorations en 3 cartes, Inventaire moins large, packs de diamants grisés « Bientôt disponible » (aucun bouton).
  - Échanges dégressifs : 8 lots, bonus plafonné à +40 %. Clic sur le compteur de diamants → Boutique, section Diamants.
  - Détourage réécrit (127 icônes), icônes rendues dans Blender (coffres, catégories, échanges, lot 9), 6 icônes Animaux régénérées avec Gemini.
  - Qualité 3D : ~30 accessoires refaits pour ressembler à leur icône (règles de qualité dans `.claude/agents/artiste-3d.md`), 22 textures de skins dessinées, diamant 3D branché.
  - Spectacle : touche propre à chaque Secret, tremblement après un piège (après la révélation), pièces volantes de l'échange, particules des skins et accessoires rares, coffres de rareté qui gardent leur couleur.
  - QA en 3 phases ; performances : 2,4-2,7 ms en moyenne, pic de 47 ms (ramasse-miettes, surtout l'éditeur ; à mesurer sur un build).
  - Règles apprises : ne JAMAIS activer le son (Mute Audio de Unity toujours activé) ; ne jamais compiler pendant le mode Play de l'auteur ; restaurer la sauvegarde par Unity (effacer puis réécrire les clés), pas par `defaults import` ; copies de sauvegarde dans `/Users/edy/TourDeLaChance-sauvegardes/`.

## En cours / prochaine étape (v6, au 13/11/2026)
- **15/11 : pause de l'auteur** (quelques jours). Il a des retours de test à donner à son retour : les lui demander en premier.
- **Décision : objectif Roblox.** Le jeu sera porté sur Roblox Studio (Luau, DataStore, tirages côté serveur, sans doute via Rojo). La version Unity sert de **maquette vivante** : on y perfectionne écrans et design, **pensés pour le téléphone** (capture en plus au format 19,5:9, gros boutons tactiles, rien dans la zone réservée en haut par Roblox). Modèles, icônes, textures et sons seront réutilisés. Avant le portage : vérifier les règles Roblox sur le hasard et les objets aléatoires payants (jamais de quitte ou double en Robux), puis faire un prototype minimal.
- En attente : polissage V7.2 (4 points dans `docs/specs/v7-a-faire.md`, la Renaissance n'est pas touchée). Reprise automatique arrêtée. 1 000 diamants donnés à l'auteur à sa demande (copie de la sauvegarde avant dans `TourDeLaChance-sauvegardes/`).
- V7 livrée (14/11), en attente des tests de l'auteur à son retour. Restes : pictogramme « pas encore vu » de l'Index, mesure des perfs sur un build, effets « tête basse » de quelques accessoires.
- V6.5 livrée, en attente des tests de l'auteur. Petits restes : « Ma position » chevauche un peu le « i » du bas, rayons de « Récupérer » discrets sur le fond rose.
- V6 livrée, en attente des tests de l'auteur.
- À surveiller : retouches USS non recapturées (route, renaissance), mesure des performances au Profiler, animation de l'échange (pièces volantes) non faite.

- Lot 6 terminé (60/60 icônes, commit 4d60aac). Si on doit refaire des images : conversation Gemini des skins Animaux `7c4b360605bc4139`, ou ChatGPT avec une image de référence jointe.
- Inspection QA en cours, puis spec v6 par le game designer à partir de `docs/specs/v6-a-faire.md`.
