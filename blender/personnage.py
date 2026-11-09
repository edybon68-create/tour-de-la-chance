"""
personnage.py — Modélise le personnage (capsule-jouet avec des yeux) et l'exporte en FBX.

Fiche suivie : docs/art-bible.md §6.2. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/personnage.py

Hiérarchie produite :
    Joueur              origine à la base, au centre (0 ; 0 ; 0)
    ├── Corps           la capsule, UN SEUL matériau (M_PersoCorps, recoloré par le shop)
    ├── OeilGauche      Unity (-0,18 ; 1,6 ; avant)  = à gauche de l'image vue de la caméra
    ├── OeilDroit       Unity (+0,18 ; 1,6 ; avant)
    ├── RefletGauche / RefletDroit   petits points blancs dans les yeux
    └── PointChapeau    objet vide au sommet de la tête, Unity (0 ; 2,0 ; 0)

Les yeux et reflets sont des objets séparés pour ne JAMAIS changer de couleur quand le
shop repeint le Corps. Avant = +Y dans Blender = -Z dans Unity (voir commun.py).
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

RAYON = 0.50            # capsule de 1,00 de diamètre
RAYON_BASE = 0.525      # base légèrement élargie (Ø 1,05) : air stable de pion
HAUTEUR = 2.00
SEGMENTS = 24           # autour de l'axe
ANNEAUX_DEMI_SPHERE = 7 # par demi-sphère (≈ 16 anneaux au total avec le tronc)


def profil_capsule():
    """Profil (rayon, hauteur) du haut vers le bas : demi-sphère du haut (centre z=1,5),
    tronc qui s'évase un peu vers le bas, demi-sphère du bas (centre z=0,525)."""
    pts = []
    c_haut = HAUTEUR - RAYON
    for k in range(ANNEAUX_DEMI_SPHERE + 1):          # du pôle (0°) à l'équateur (90°)
        a = (math.pi / 2) * k / ANNEAUX_DEMI_SPHERE
        pts.append((RAYON * math.sin(a), c_haut + RAYON * math.cos(a)))
    c_bas = RAYON_BASE                                 # le bas touche le sol en z = 0
    pts.append((RAYON_BASE, c_bas + 0.25))             # milieu du tronc (aide le lissage)
    for k in range(ANNEAUX_DEMI_SPHERE + 1):          # de l'équateur au pôle du bas
        a = (math.pi / 2) * k / ANNEAUX_DEMI_SPHERE
        pts.append((RAYON_BASE * math.cos(a), c_bas - RAYON_BASE * math.sin(a)))
    pts[-1] = (0.0, 0.0)
    return pts


def surface_avant(x, z):
    """Point de la surface de la tête (demi-sphère du haut) à la hauteur z et au côté x,
    sur la face avant (+Y), et la normale de la surface en ce point."""
    c = Vector((0, 0, HAUTEUR - RAYON))
    y = math.sqrt(max(RAYON ** 2 - x ** 2 - (z - c.z) ** 2, 0.0))
    p = Vector((x, y, z))
    return p, (p - c).normalized()


def oeil(nom, x_blender):
    """Œil ovale (15 x 19 cm, 6 cm d'épaisseur), à moitié enfoncé dans la tête,
    orienté selon la surface. Renvoie (oeil, reflet)."""
    p, n = surface_avant(x_blender, 1.6)
    # Repère local de l'œil : son axe « épaisseur » suit la normale de la tête.
    rot = n.to_track_quat("Y", "Z").to_matrix().to_4x4()
    bm = bmesh.new()
    C.bm_sphere(bm, 1.0, (0, 0, 0), segments=10, anneaux=6, echelle=(0.075, 0.03, 0.095))
    bmesh.ops.transform(bm, matrix=Matrix.Translation(p) @ rot, verts=bm.verts)
    o = C.objet_depuis_bmesh(nom, bm, ["M_Yeux"])
    C.deplacer_origine(o, p)
    C.lisser_tout(o)

    # Reflet : point blanc de 4 cm en haut à gauche de l'œil (gauche de l'image = +X
    # Blender). Les deux reflets du même côté, comme la lumière principale.
    local = Vector((0.03, 0.026, 0.04))  # (côté, devant, haut) dans le repère de l'œil
    centre = Matrix.Translation(p) @ rot @ local
    bm = bmesh.new()
    C.bm_sphere(bm, 1.0, (0, 0, 0), segments=6, anneaux=4, echelle=(0.02, 0.01, 0.02))
    bmesh.ops.transform(bm, matrix=Matrix.Translation(centre) @ rot, verts=bm.verts)
    nom_reflet = nom.replace("Oeil", "Reflet")
    r = C.objet_depuis_bmesh(nom_reflet, bm, ["M_Reflet"])
    C.deplacer_origine(r, centre)
    C.lisser_tout(r)
    return o, r


def construire():
    C.nouvelle_scene()
    racine = C.vide("Joueur")

    bm = bmesh.new()
    C.bm_revolution(bm, profil_capsule(), SEGMENTS)
    corps = C.objet_depuis_bmesh("Corps", bm, ["M_PersoCorps"])
    C.lisser_tout(corps)  # objet rond : lissage complet, aucun biseau

    # « Gauche » = côté x négatif dans Unity = x positif dans Blender (voir commun.py).
    oeil_g, reflet_g = oeil("OeilGauche", +0.18)
    oeil_d, reflet_d = oeil("OeilDroit", -0.18)
    point = C.vide("PointChapeau", (0, 0, HAUTEUR))

    for o in (corps, oeil_g, oeil_d, reflet_g, reflet_d, point):
        C.parenter(o, racine)
    return racine


if __name__ == "__main__":
    racine = construire()
    C.rapport(racine)
    C.rendre_miniature(racine, "blender_personnage.png")
    chemin = C.exporter_fbx(racine, "personnage.fbx")
    C.verifier_fbx(chemin)
