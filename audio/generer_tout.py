# -*- coding: utf-8 -*-
"""
Régénère TOUS les sons du jeu, puis lance l'analyse de contrôle.
Lancer depuis n'importe où :
    /Applications/Blender.app/Contents/Resources/4.5/python/bin/python3.11 audio/generer_tout.py
(ou : /Applications/Blender.app/Contents/MacOS/Blender --background --python audio/generer_tout.py)
Les graines aléatoires sont fixes : relancer donne exactement les mêmes fichiers.
"""
import os
import sys

sys.dont_write_bytecode = True  # pas de dossier __pycache__ dans audio/
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import analyse
import musique
import sons_boite
import sons_coffre
import sons_resultat_pieces
import sons_ui

for module in (sons_ui, sons_coffre, sons_resultat_pieces, sons_boite, musique):
    module.genere()
analyse.main()
