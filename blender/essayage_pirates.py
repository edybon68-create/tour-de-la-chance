"""
essayage_pirates.py — Contrôle des objets des Boîtes Pirates portés par le cheval :
  1. une bande de 5 vues par objet (face, 3/4, profil, dessus, 3/4 arrière), faite par
     blender/essayage_angles.py → docs/captures/v5_essai_<id>.png ;
  2. la planche vue de la caméra du jeu → docs/captures/v5_planche_pirates.png
     (12 accessoires, 2 extras de skins, et 2 essais « deux emplacements ensemble »).

Lancer (après les scripts pirate_*.py, qui exportent dans blender/exports/) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/essayage_pirates.py
Quelques objets seulement (pas de planche) :
    ACCESSOIRES="tricorne perroquet" /Applications/Blender.app/.../Blender --background --python ...
Seulement la planche :
    VUES=0 /Applications/Blender.app/.../Blender --background --python ...

Ce script ne modifie aucun modèle et ne touche pas aux fichiers partagés : il réutilise les
fonctions d'essayage_angles.py et de cheval_accessoires.py en leur passant NOTRE tableau.

REGLAGES est la référence pour le programmeur : point d'accroche, attachScale (échelle
uniforme), attachOffset (décalage local Unity). Tous les objets sont modélisés SUR le cheval,
à leur place : échelle 1 et décalage nul partout. Les extras de skin (Kraken, Abysses) sont
posés comme enfants directs de `Joueur` (catalogue §0.4).
"""

import os
import sys

import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import essayage_angles as E  # noqa: E402
import cheval_accessoires as A  # noqa: E402

# id : (point d'accroche, attachScale, attachOffset Unity (x ; y ; z), source)
REGLAGES = {
    "tricorne":             ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "bandana_pirate":       ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "cache_oeil":           ("PointVisage", 1.0, (0.0, 0.0, 0.0), "exports"),
    "foulard_marin":        ("PointCou", 1.0, (0.0, 0.0, 0.0), "exports"),
    "boussole":             ("PointCou", 1.0, (0.0, 0.0, 0.0), "exports"),
    "longue_vue":           ("PointDos", 1.0, (0.0, 0.0, 0.0), "exports"),
    "chapeau_capitaine":    ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "loup_corsaire":        ("PointVisage", 1.0, (0.0, 0.0, 0.0), "exports"),
    "perroquet":            ("PointDos", 1.0, (0.0, 0.0, 0.0), "exports"),
    "ancre_doree":          ("PointDos", 1.0, (0.0, 0.0, 0.0), "exports"),
    "kraken_bebe":          ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "navire_fantome":       ("PointDos", 1.0, (0.0, 0.0, 0.0), "exports"),
    "pirate_kraken_extras": ("Joueur", 1.0, (0.0, 0.0, 0.0), "exports"),
    "pirate_abysses_extras": ("Joueur", 1.0, (0.0, 0.0, 0.0), "exports"),
}
# Essais de cohabitation : deux emplacements différents portés ensemble.
DUOS = [("bandana_pirate", "cache_oeil", "foulard_marin", "longue_vue"),
        ("tricorne", "loup_corsaire", "boussole", "ancre_doree")]
Y_MIN = {"PointChapeau": 1.0, "PointVisage": 1.0, "PointCou": 0.45, "PointDos": 0.3,
         "Joueur": -0.3}


def poser(id_, cheval):
    point, echelle, decalage, source = REGLAGES[id_]
    objs = C.importer_fbx(A.fichier(id_, source))
    racines = [o for o in objs if o.parent is None]
    C.poser_sur_point(racines, cheval[point], echelle, decalage)
    return objs


def vue_jeu(ids, cheval, nom):
    objs = []
    for id_ in ids:
        objs += poser(id_, cheval)
    import commun_pirates as P
    P.appliquer_transparence()
    y_min = min(Y_MIN[REGLAGES[i][0]] for i in ids)
    chemin = C.rendre_camera_jeu(list(cheval.values()) + objs, nom,
                                 y_min=C.CHEVAL_POSITION[1] + y_min)
    for o in objs:
        bpy.data.objects.remove(o, do_unlink=True)
    return chemin


def main():
    C.MONDE_DEGRADE = True
    choix = os.environ.get("ACCESSOIRES", "").split() or list(REGLAGES)
    if os.environ.get("VUES", "1") != "0":
        C.nouvelle_scene()
        _, cheval = C.importer_cheval(position_unity=(0, 0, 0))
        for id_ in choix:
            E.essayer(id_, cheval, reglages=REGLAGES)
    if os.environ.get("ACCESSOIRES"):
        return
    # La planche : vue de la caméra du jeu, cheval sur son estrade.
    C.nouvelle_scene()
    _, cheval = C.importer_cheval()
    C.estrade()
    chemins = []
    for id_ in REGLAGES:
        chemins.append(vue_jeu([id_], cheval, f"_tmp_pir_{id_}.png"))
    for k, duo in enumerate(DUOS):
        chemins.append(vue_jeu(duo, cheval, f"_tmp_pir_duo{k}.png"))
    A.planche(chemins, nom="v5_planche_pirates.png")
    for ch in chemins:
        os.remove(ch)


if __name__ == "__main__":
    main()
