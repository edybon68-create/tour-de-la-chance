"""
coffre_bronze.py — Coffre de rareté BRONZE (spec v3 §5.3.1 et §16).

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/coffre_bronze.py
Export : blender/exports/coffre_bronze.fbx (rien n'est écrit dans le projet Unity).
Miniatures : docs/captures/blender_coffre_bronze.png (et _ouvert.png).

Ce qui le distingue du coffre en bois (rang 1, « un cran au-dessus ») :
    - bois verni roux #7A4A2E (corps) et plus foncé sur le couvercle ;
    - toutes les ferrures en bronze #B0703A (métal, smoothness 0,55), serrure comprise ;
    - de gros RIVETS ronds sur les bandes (corps et couvercle) et sur les coins : de loin,
      les bandes deviennent « pointillées », c'est le détail qui se lit ;
    - un SOCLE de bronze tout autour du bas : le coffre a l'air posé sur un piédestal.
Même gabarit et mêmes pivots que coffre.fbx (le moule commun est coffre_rarete.py).
"""

import math
import os
import sys

import bmesh
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import coffre_rarete as R  # noqa: E402

STYLE = {
    "corps": "M_CorpsBronze",
    "couvercle": "M_CouvercleBronze",
    "metal": "M_Bronze",
    "interieur": "M_Interieur",
    "segments_dome": 6,
    "plaque": "ecusson",
    "coins": "equerres",
    "socle": True,
}


def construire():
    pieces = R.construire(STYLE)
    metal = [STYLE["metal"]]

    # Rivets du corps : 3 par bande, et 1 au milieu de chaque coin (aile avant).
    bm = bmesh.new()
    devant = Vector((0, 1, 0))
    for sx in (-1, 1):
        for z in (0.16, 0.40, 0.64):
            R.bm_rivet(bm, (sx * R.X_BANDES, R.FACE_BANDE, z), devant)
        for z in (0.075, R.HAUTEUR - 0.075):
            R.bm_rivet(bm, (sx * (R.LARGEUR / 2 - 0.05), R.AVANT + 0.02, z), devant, rayon=0.02)
    pieces["ferrures"].append(C.objet_depuis_bmesh("RivetsCorps", bm, metal))

    # Rivets du couvercle : 3 par bande, posés sur le bombé (perpendiculaires à la surface).
    bm = bmesh.new()
    for sx in (-1, 1):
        for t in (0.25, 0.5, 0.75):
            p, n = R.point_dome(math.pi * t, 6, decalage=R.RELIEF)
            p.x = sx * R.X_BANDES
            R.bm_rivet(bm, p, n)
    pieces["couvercle"].append(C.objet_depuis_bmesh("RivetsCouvercle", bm, metal))
    return R.assembler(pieces)


if __name__ == "__main__":
    R.finir(construire(), "bronze")
