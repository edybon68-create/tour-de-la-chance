# -*- coding: utf-8 -*-
"""
Régénère les sons du jeu, puis lance l'analyse de contrôle.

Lancer depuis n'importe où (python de Blender, qui contient numpy) :
    PY=/Applications/Blender.app/Contents/Resources/4.5/python/bin/python3.11

    $PY audio/generer_tout.py
        -> TOUS les sons (43 + 34 de la v5), écrits dans TourDeLaChance-Unity/Assets/Audio/

    $PY audio/generer_tout.py --sortie audio/exports
        -> tous les sons, mais dans audio/exports/ (on ne touche pas au projet Unity,
           par exemple pendant que quelqu'un joue dans l'éditeur)

    $PY audio/generer_tout.py --v3 --sortie audio/exports
        -> seulement les sons NOUVEAUX ou CORRIGÉS de la v3 (15 + 2), dans audio/exports/

    $PY audio/generer_tout.py --v5
        -> seulement les 34 sons NOUVEAUX de la v5 (sons_v5.py) ; aucun son existant
           n'est recalculé ni remplacé

Le dossier de sortie garde toujours la même arborescence que Assets/Audio
(<categorie>/<nom>.wav) : copier son contenu dans Assets/Audio suffit.
On peut aussi choisir le dossier avec la variable d'environnement TDLC_AUDIO_SORTIE.

Les graines aléatoires sont fixes : relancer donne exactement les mêmes fichiers.
"""
import argparse
import os
import sys

sys.dont_write_bytecode = True  # pas de dossier __pycache__ dans audio/
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import analyse
import synth

# Les sons retouchés en v3 (correctif B3 de docs/specs/v3.md §13)
CORRECTIFS_V3 = {"boite_ouverture", "vente"}


def main(arguments=None):
    parseur = argparse.ArgumentParser(description="Fabrique les sons de Tour de la Chance.")
    parseur.add_argument("--sortie", default=None,
                         help="dossier de sortie (relatif à la racine du projet), "
                              "ex. audio/exports ; par défaut Assets/Audio du projet Unity")
    parseur.add_argument("--v3", action="store_true",
                         help="ne produire que les 15 nouveaux sons v3 et les 2 sons corrigés")
    parseur.add_argument("--v5", action="store_true",
                         help="ne produire que les 34 nouveaux sons v5 (sons_v5.py)")
    parseur.add_argument("--sans-analyse", action="store_true",
                         help="ne pas afficher le tableau de contrôle à la fin")
    # Blender passe ses propres arguments : on ne garde que ceux placés après « -- ».
    if arguments is None:
        arguments = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
        if "--python" in arguments:  # lancé par Blender sans « -- »
            arguments = []
    options = parseur.parse_args(arguments)

    if options.sortie:
        synth.definir_sortie(options.sortie)
    print("Dossier de sortie :", os.path.relpath(synth.DOSSIER_AUDIO, synth.RACINE))

    # Import APRÈS le choix du dossier (les modules lisent synth.DOSSIER_AUDIO au moment
    # d'écrire, mais c'est plus clair ainsi).
    import musique
    import sons_boite
    import sons_coffre
    import sons_resultat_pieces
    import sons_secret
    import sons_ui
    import sons_v3
    import sons_v5

    if options.v5:
        sons_v5.genere()
    elif options.v3:
        sons_ui.genere(seulement=CORRECTIFS_V3)
        sons_boite.genere(seulement=CORRECTIFS_V3)
        sons_v3.genere()
        sons_secret.genere()
    else:
        for module in (sons_ui, sons_coffre, sons_resultat_pieces, sons_boite, musique,
                       sons_v3, sons_secret, sons_v5):
            module.genere()
    if not options.sans_analyse:
        analyse.main()


if __name__ == "__main__":
    main()
