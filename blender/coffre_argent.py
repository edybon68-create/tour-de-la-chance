"""
coffre_argent.py — Coffre de rareté ARGENT (spec v3 §5.3.1 et §16).

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/coffre_argent.py
Export : blender/exports/coffre_argent.fbx (rien n'est écrit dans le projet Unity).
Miniatures : docs/captures/blender_coffre_argent.png (et _ouvert.png).

Ce qui le distingue (rang 2) :
    - bois peint bleu nuit #3E5C8A : la seule rareté « froide », reconnaissable de loin ;
    - ferrures en argent poli #C9CED8 (métal, smoothness 0,85) ;
    - la serrure devient une GRANDE ÉTOILE d'argent (38 cm de pointe à pointe), trou au centre ;
    - cornières pleine hauteur sur les arêtes avant (au lieu des petites équerres), chacune
      ornée d'une petite étoile ; socle d'argent ;
    - un liseré d'argent tout autour du bord du couvercle et 2 étoiles sur chaque bande
      du couvercle (vues d'en haut par la caméra du jeu).
Doublure intérieure : velours bleu nuit #1E2438 (on la voit quand le couvercle s'ouvre).
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
    "corps": "M_CorpsArgent",
    "couvercle": "M_CouvercleArgent",
    "metal": "M_Argent",
    "interieur": "M_VeloursNuit",
    "segments_dome": 6,
    "plaque": "etoile",
    "coins": "cornieres",
    "socle": True,
}


def construire():
    pieces = R.construire(STYLE)
    metal = [STYLE["metal"]]

    # Petites étoiles au milieu des cornières (face avant, 2 cm de relief au total).
    bm = bmesh.new()
    for sx in (-1, 1):
        R.bm_etoile(bm, (sx * (R.LARGEUR / 2 - 0.055), R.AVANT + 0.02, 0.40), Vector((0, 1, 0)),
                    0.065, 0.028, 0.022)
    pieces["ferrures"].append(C.objet_depuis_bmesh("EtoilesCoins", bm, metal))

    # Couvercle : liseré + 2 étoiles par bande, couchées sur le bombé.
    bm = bmesh.new()
    R.bm_lisere(bm, R.HAUTEUR, R.HAUTEUR + 0.05, 0.012)
    for sx in (-1, 1):
        for t in (math.pi * 0.30, math.pi * 0.55):
            p, n = R.point_dome(t, 6, decalage=R.RELIEF)
            p.x = sx * R.X_BANDES
            R.bm_etoile(bm, p, n, 0.075, 0.032, 0.02)
    # Pas de biseau sur ces petites étoiles : à cette taille il ne se verrait pas.
    pieces["couvercle"].append(C.objet_depuis_bmesh("DecorCouvercle", bm, metal))
    return R.assembler(pieces)


if __name__ == "__main__":
    R.finir(construire(), "argent")
