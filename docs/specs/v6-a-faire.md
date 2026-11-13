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

## Trouvé par le QA

_(à compléter après l'inspection)_
