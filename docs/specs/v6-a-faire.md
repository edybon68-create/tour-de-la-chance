# V6 — Liste des choses à faire

> Liste dictée par l'auteur le 13/11/2026, après ses tests de la v5.1.
> Une section « Trouvé par le QA » sera ajoutée à la fin quand l'inspection complète sera faite.
> Le game designer la transformera ensuite en spec détaillée (`docs/specs/v6.md`).
> Captures fournies par l'auteur : `docs/references/v6_ref_bulles_colonne.png`, `docs/references/v6_ref_barre_haut.png`.

## Interface

1. **Refonte complète de l'onglet Renaissance** : la version actuelle est jugée vraiment moche.
2. **Bulles des boutons** : celles derrière les icônes des menus sont « dégueu » (voir `v6_ref_bulles_colonne.png`). Il faut créer ou générer des bulles aussi belles que possible, pour les boutons de colonne, la croix de fermeture, et les boutons Rejouer, Continuer et Encaisser.
3. **Colonne de droite** : les bulles touchent presque le compteur de diamants. Les descendre et les aligner sur la même hauteur que la colonne de gauche.
4. **Barre du haut** (carte d'étage compacte, `v6_ref_barre_haut.png`) : la décoller du bord haut de l'écran.
5. **Icône de la Boutique** à changer. Le cadeau date de l'époque où elle ne servait qu'à acheter des boîtes. Maintenant que c'est un vrai magasin, il faut une icône qu'on comprend tout de suite.
6. **Icône des Paramètres** : le détourage de la roue dentée est très mauvais, à refaire.
7. **Barres de défilement** : la barre doit prendre toute la hauteur prévue. Pour l'instant, elle ne prend pas tout l'espace (Inventaire, etc.).

## Boutique

8. **Onglets** : les catégories doivent occuper toute la largeur, jusqu'à l'affichage des monnaies.
9. **Texte « Collection : X / N »** sur les cartes de boîtes : beaucoup trop gras et peu agréable à lire.
10. **Voir les objets qu'on n'a pas** depuis la boîte, avant d'acheter, sauf les Secrets (comme maintenant, c'est parfait).
11. **Acheter des pièces avec des diamants** dans la boutique. Le taux doit rester raisonnable pour qu'on ne puisse pas en abuser.
12. **Ordre des raretés croissant partout** : Commun en haut, Secret en bas.

## Index

13. L'Index sert à voir ce qu'on a **déjà eu**. Les objets pas encore obtenus montrent leur **nom mais pas leur visuel** : on doit passer par la Boutique pour les voir, ce qui pousse à dépenser.
14. **Fiche agrandie** (elle est parfaite) avec deux ajouts : pouvoir **faire tourner** le skin soi-même (glisser à gauche ou à droite), et ajouter la **flèche droite** qui manque pour passer à l'objet suivant.
15. **Récompense de collection** : une petite récompense en pièces ou en diamants quand on a fini l'index complet d'une boîte.

## Inventaire

16. Bouton **« Équiper le meilleur »** : il équipe le meilleur combo du point de vue du bonus (skin + 2 accessoires).
17. Section **« Mes skins »** : on y sauvegarde ses combos préférés (skin + accessoires) en leur donnant un nom, puis on les remet en un clic.

## Étage et progression

18. **Clic sur l'étage** : il ouvre une section qui montre où on en est, les changements par étage et notre record, comme une « route des trophées » qu'on fait défiler.

## Reprise et partie perdue

19. **Reprise** : plus de pop-up. Un bouton **« Continuer — 1 diamant »** apparaît en bas, à côté de **Rejouer**.
20. **Coût croissant de la reprise** dans une même partie : 1, puis 2, puis 4, puis 8, puis 16 diamants… (le prix double à chaque fois).
21. **Confirmation avant Rejouer** : un clic sur Rejouer ouvre une pop-up « Tu es sûr ? », pour éviter de perdre une partie par étourderie.

## Économie

22. **Améliorations** : baisser un peu leurs prix en pièces, car elles sont trop longues à farmer et le joueur a l'impression de stagner. Ni trop lent au début, ni trop rapide.
23. **Skins achetés avec des diamants** (boîtes Pirates) : les Légendaires et les Secrets donnent un petit **multiplicateur de diamants**, pour donner une raison de les acheter.

## Paramètres

24. **Niveau de graphismes** :
    - **Faible** : pour les appareils peu puissants ;
    - **Moyen** : résolution classique ;
    - **Élevé** : tous les détails et les graphismes au maximum.

## Restes de la v5 (déjà connus)

25. 11 icônes de skins Animaux à générer (tigre, girafe, léopard, grenouille, abeille, coccinelle, zèbre, méduse, panda, dalmatien, vache).
26. Particules des objets rares (fumée, bulles, confettis, traînée de la baleine…), réactions après un piège, révélation propre à chaque Secret, textures dessinées des skins, modèle 3D définitif du diamant.
27. Petites traînées violettes encore entourées de noir sous quelques icônes (Perroquet, Capitaine, Petit Kraken, Renaissance).
28. Paiement réel des diamants : **toujours inactif** (voir la v5 §16 avant toute activation).

## Trouvé par le QA (inspection du 13/11/2026)

Verdict : aucun problème bloquant. Les captures sont dans `docs/captures/qa_v6_*.png`.
Contrôlés et conformes :
- équité des tirages ;
- taux affichés égaux aux taux réels ;
- 2 accessoires au maximum ;
- reprise protégée contre le double-clic ;
- Renaissance ;
- aucun paiement réel actif.

### Important
29. **Première ouverture d'une boîte de skins dans une session** : le mini-cheval s'affiche en plâtre blanc au lieu du skin gagné. Cause : `BoxStudio.cs:373` appelle `ShowOnly` alors que le cheval est encore désactivé, puis `PlayerAvatar.Start()` écrase le skin (`qa_v6_ouverture_skin_platre.png`).
30. **Carte d'étage trop chargée** : la pastille « Trésor : +X » chevauche les petites mentions (bannière de record, tenue, Renaissance), et le gain devient illisible. La règle d'or est touchée : le gain doit être lisible avant le choix (`qa_v6_carte_etage_chevauchement.png`).
31. ~~10 icônes d'accessoires Pirates non branchées~~ : **corrigé le 13/11** (branchement automatique relancé).
32. **Console** : des milliers d'erreurs NaN dans `AccessoryRig.cs:235` (antennes des Ailes de papillon). Le ressort devient instable quand des images sont longues. Il faut limiter `dt` et ajouter une protection anti-NaN.
33. **Bouton « Encaisser 0 »** affiché quand la cagnotte est vide (après un trésor en diamants à l'étage 1). Il ne doit pas apparaître sans cagnotte (`HudController.cs:183` et `:224`).
34. **« Voir dans l'inventaire »** ne fait rien depuis la Boutique (« Contenu et taux » › fiche). Le gestionnaire est vide dans `BoutiquePage.cs:96`.
35. **Résultat d'ouverture mal cadré** :
    - le socle de la boîte est coupé en bas ;
    - le panneau de résultat recouvre la boîte ;
    - la rareté est illisible dans le faisceau ;
    - certaines lignes sont alignées à gauche ;
    - le HUD reste visible.

    Attendu : les textes sous l'objet et le HUD caché (`qa_v6_ouverture_*.png`).

### Mineur
36. **Améliorations** : les points de la jauge or sont invisibles sur la carte or. La ligne « Trésors +60 % » décrit le niveau suivant sans le dire.
37. **Boîtes Animaux verrouillées** : le cadenas cache « Collection : 0 / 16 ».
38. **Inventaire** :
    - une seule rangée visible, et les cartes coupées (« Remplace : … », rareté, badge de bonus) ;
    - les grilles n'utilisent que 3 à 4 colonnes et laissent un grand vide à droite.
39. **Voiles sombres** (fiche agrandie, « Lequel enlever ? », confirmations) : ils ne couvrent pas le haut de la fenêtre.
40. **Texte clair sur fond blanc** : titres de l'Index, Contenu et taux, Réglages. « Il te manque X » est en rouge, peu lisible sur les cartes colorées : à mettre en blanc contouré.
41. **Retour à la ligne avant « % »** (il faut une espace insécable). La puce de « Peu commun » est décalée dans le tableau des taux.
42. **Phrase de la carte d'étage** après un trésor en diamants ou une reprise : elle affiche « +0 ! » ou une phrase de montée alors qu'on n'est pas monté.
43. **L'Index range Animaux avant Pirates** : il doit suivre l'ordre de la Boutique (Pirates puis Animaux).
44. **Les 32 objets des boîtes n°1 n'ont pas de phrase de fiche** dans l'Index.
45. **Casquette mal placée sur le cheval vierge** de la fiche : elle cache un œil.
46. **Classement** : « Ton record : étage 8 » alors que la meilleure ligne est l'étage 7 (le record est enregistré avant que la ligne de la partie soit ajoutée).
47. **Porte-monnaie du HUD** sans séparateur de milliers (« 11992 » au lieu de « 11 992 »).
48. **Réglages** : la fenêtre affiche encore « version 5 » et elle est trop haute.
49. **Skins sans icône** : la vignette de secours est un carré uni. Mettre un vrai substitut.
50. **Rayons 3D de la révélation** : on voit des marches (blocs carrés).
51. **Vitrine de l'Inventaire** : l'Auréole est coupée en haut.
52. **Moment de la révélation** : la v5 §21 dit 1,90 s, l'art bible dit 1,75 s, et le code suit 1,75 s. À trancher dans la spec v6.
53. **Performances** : à mesurer au Profiler, fenêtre au premier plan (aucune image de plus de 50 ms au premier étage à 6 coffres).
