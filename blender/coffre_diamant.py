"""
coffre_diamant.py — Coffre de rareté DIAMANT (spec v3 §5.3.1 et §16).

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/coffre_diamant.py
Export : blender/exports/coffre_diamant.fbx (rien n'est écrit dans le projet Unity).
Miniatures : docs/captures/blender_coffre_diamant.png (et _ouvert.png).

Ce qui le distingue (rang 4, le plus spectaculaire) :
    - nacre glacée #E6F0F7 (smoothness 0,7) : un coffre presque blanc, unique au milieu
      des bois sombres ;
    - un couvercle TAILLÉ EN FACETTES (4 pans nets au lieu du bombé lisse) : la silhouette
      elle-même rappelle une pierre précieuse ;
    - ferrures en argent #C9CED8, serrure en LOSANGE (une pierre vue de face) ;
    - 5 GEMMES taillées bleu glace #9FE3FF (smoothness 0,95), serties dans des chatons
      d'argent : une grosse sur le pan avant du couvercle, 4 sur les cornières avant ;
    - liseré d'argent autour du couvercle, socle d'argent.
Doublure intérieure : velours bleu profond #1E2A4A.

Gemmes = OBJETS SÉPARÉS, pivot au centre de chaque pierre, aucune rotation :
    Charniere/Gemme_0   grosse gemme du couvercle (s'ouvre avec lui) ; table vers le haut
                        et l'avant -> on peut la faire tourner sur son axe Y local
    Coffre/Gemme_1..4   gemmes des cornières ; table vers l'avant (-Z Unity)
                        -> rotation sur l'axe Z local, ou petite pulsation d'échelle
Le code n'est pas obligé de les animer : immobiles, elles sont déjà à leur place.
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
    "corps": "M_CorpsDiamant",
    "couvercle": "M_CouvercleDiamant",
    "metal": "M_Argent",
    "interieur": "M_VeloursBleu",
    "segments_dome": 4,          # couvercle à facettes
    "plaque": "losange",
    "coins": "cornieres",
    "socle": True,
}
MAT_GEMME = ["M_Gemme"]


def construire():
    pieces = R.construire(STYLE)
    metal = [STYLE["metal"]]

    # Grosse gemme : au milieu du pan avant-haut du couvercle (le pan le mieux vu par la
    # caméra du jeu), entre les deux bandes.
    p, n = R.point_dome(math.pi * 0.375, 4, decalage=0.0)
    bm = bmesh.new()
    R.bm_chaton(bm, p, n, 0.115, hauteur=0.02)
    R.bm_lisere(bm, R.HAUTEUR, R.HAUTEUR + 0.05, 0.012)
    pieces["couvercle"].append(C.objet_depuis_bmesh("DecorCouvercle", bm, metal))
    pieces["gemmes_couvercle"].append(R.gemme("Gemme_0", 0.10, p + n * 0.035, n, MAT_GEMME))

    # 4 gemmes sur les cornières avant (en haut et en bas), avec leur chaton.
    devant = Vector((0, 1, 0))
    bm = bmesh.new()
    i = 1
    for sx in (1, -1):              # +X Blender = gauche de l'image : Gemme_1/2 à gauche
        for z in (R.HAUTEUR - 0.14, 0.17):
            c = Vector((sx * (R.LARGEUR / 2 - 0.055), R.AVANT + 0.02, z))
            R.bm_chaton(bm, c, devant, 0.058, hauteur=0.012)
            pieces["gemmes_corps"].append(R.gemme(f"Gemme_{i}", 0.05, c + devant * 0.022, devant,
                                                  MAT_GEMME))
            i += 1
    pieces["ferrures"].append(C.objet_depuis_bmesh("Chatons", bm, metal))
    return R.assembler(pieces)


if __name__ == "__main__":
    R.finir(construire(), "diamant")
