"""
noeud_papillon.py — Nœud papillon (boîte d'accessoires n°1 : `noeud_papillon`, Commun, Cou).

Fiche suivie : docs/specs/boites.md §5.6. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/noeud_papillon.py

Un seul objet, nommé comme l'identifiant. Origine = point d'accroche Cou = `PointCou`,
Unity (0 ; 1,3 ; 0) local à Joueur (centre du corps à hauteur du « col ») : position et
rotation locales à zéro. Le nœud est donc 0,52 DEVANT l'origine (Unity z = −0,52), contre
le corps. Ses deux ailes sont tournées de 10° vers l'arrière pour suivre la courbe du corps.
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "noeud_papillon"
EMPLACEMENT = "cou"
MATS = ["M_Satin", "M_SatinFonce"]  # indices 0, 1
CENTRE = C.perso(0, 1.3, -0.525)
EPAISSEUR = 0.06
ORIENTATION = 10.0       # degrés vers l'arrière pour chaque aile

# Une aile, dessinée vers +X Blender (gauche de l'image) : (x vers l'extérieur, z en haut).
# Étroite contre le nœud (7 cm), large au bout (≈ 16 cm), petite encoche au bout.
AILE = [(0.03, -0.035), (0.10, -0.055), (0.17, -0.078), (0.205, -0.07), (0.215, -0.035),
        (0.203, 0.0), (0.215, 0.035), (0.205, 0.07), (0.17, 0.078), (0.10, 0.055), (0.03, 0.035)]


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    for cote in (1, -1):
        contour = [(cote * x, z) for x, z in AILE]
        if cote < 0:
            contour.reverse()
        # Rotation autour d'un axe vertical passant contre le nœud : le bout part vers l'arrière.
        pivot = Matrix.Translation((cote * 0.03, 0, 0))
        rot = Matrix.Rotation(math.radians(-cote * ORIENTATION), 4, "Z")
        m = Matrix.Translation(CENTRE) @ pivot @ rot @ pivot.inverted()
        C.bm_plaque(bm, contour, EPAISSEUR, m, mat=0)
    ailes = C.objet_depuis_bmesh("Ailes", bm, MATS)
    C.biseau(ailes, largeur=0.015, segments=1, angle=40)
    C.appliquer(ailes)

    bm = bmesh.new()
    C.bm_boite(bm, (0.08, 0.08, 0.08), CENTRE + C.Vector((0, 0.012, 0)), mat=1)
    noeud = C.objet_depuis_bmesh("Noeud", bm, MATS)
    C.biseau(noeud, largeur=0.022, segments=2, angle=40)
    C.appliquer(noeud)

    obj = C.joindre([ailes, noeud], NOM)
    C.lisser_angle(obj, 40)
    return C.vers_accroche(obj, EMPLACEMENT)


if __name__ == "__main__":
    C.finir_accessoire(construire(), NOM, EMPLACEMENT)
