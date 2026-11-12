"""
pirate_abysses_extras.py — Les « extras » 3D du skin Abysses (`pirate_abysses`, SECRET) :
la lanterne de baudroie et 3 petits poissons lumineux. Fiche : docs/specs/v5-catalogue.md
§1.2 et §0.4.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/pirate_abysses_extras.py
Sortie : blender/exports/pirate_abysses_extras.fbx et docs/captures/v5_cheval_pirate_abysses_extras.png.

Ce n'est PAS un accessoire : c'est le `skinExtrasPrefab` du skin Abysses, enfant direct de
`Joueur` (origine = la base du cheval, échelle 1, décalage nul).

La lanterne : une TIGE courbée (Ø 4 cm à la base -> 2,4 cm au bout, 0,75 m) part du front,
entre les oreilles (sur une petite bosse charnue), monte et se penche vers l'avant ; au bout
pend une LANTERNE sphérique Ø 14 cm jaune pâle (émission 1,5, le maximum de la bible), avec
une petite coiffe et 3 nageoires-collerette cyan. Elle reste AU-DESSUS de la ligne de regard
caméra -> yeux (y ≈ 1,83 à cet endroit, la lanterne est à y ≈ 2,0) : elle ne cache jamais
les yeux ; la tige (fine) passe au milieu, entre eux.
Les poissons : 3 losanges plats cyan émissifs (24 cm) avec une queue, couchés à plat (lisibles d'en haut), qui tournent autour du cheval
sur des cercles de rayon 0,9 / 1,0 / 1,1 à des hauteurs différentes (0,7 / 1,25 / 1,85).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    pirate_abysses_extras     vide, origine = base du cheval (enfant de Joueur)
    ├── Anim_Tige_1           base de la tige + bosse ; origine = sur le front
    │   └── Anim_Tige_2       milieu ; origine = son point de départ
    │       └── Anim_Tige_3   bout ; (ressort A en chaîne : chaque morceau suit le
    │           │             précédent avec du retard, comme l'Auréole v3)
    │           └── Anim_Lanterne  la lanterne ; origine = son point d'accroche (se balance)
    │               └── PointLumiere  vide au centre de la lanterne : y poser la Point Light
    │                                 URP (portée 1,5 m, intensité 1,2, #FFF1A8, sans ombre)
    ├── Anim_OrbitePoisson_1  vide au centre du cercle (y = 0,70) : le code le fait tourner
    │   └── Poisson_1         autour de Y (+40°/s) ; poisson à 0,9 m, nez dans le sens du tour
    ├── Anim_OrbitePoisson_2  (y = 1,25, −55°/s : sens inverse) └── Poisson_2 à 1,0 m
    ├── Anim_OrbitePoisson_3  (y = 1,85, +70°/s)                └── Poisson_3 à 1,1 m
    └── Fx_Emetteur           vide : bas du cou (bulles lumineuses, réserve de 15)
Matières (3) : noir bleuté des abysses (smoothness 0,90), lanterne (émission 1,5), cyan des
poissons et des anneaux de la tige (émission 1,2).
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_pirates as P  # noqa: E402

NOM = "pirate_abysses_extras"
MATS = ["M5_PirAbysses", "M5_PirLanterneAbysses", "M5_PirPoissonCyan"]   # 0, 1, 2
TIGE = [(0.0, 1.89, -0.03), (0.0, 2.03, -0.06), (0.0, 2.16, -0.13), (0.0, 2.25, -0.24),
        (0.0, 2.29, -0.37), (0.0, 2.26, -0.50), (0.0, 2.19, -0.60), (0.0, 2.12, -0.65)]
COUPES = [0, 3, 5, 7]       # la tige est coupée en 3 morceaux : points 0-3, 3-5, 5-7
R_LANTERNE = 0.07
POISSONS = [(0.70, 0.90), (1.25, 1.00), (1.85, 1.10)]   # (hauteur, rayon)


def morceau(i, parent):
    a, b = COUPES[i], COUPES[i + 1]
    pts = [C.U(*TIGE[k]) for k in range(a, b + 1)]
    n = len(TIGE) - 1
    rayons = [1.0 - 0.4 * (k / n) for k in range(a, b + 1)]
    if i > 0:                                   # recouvre un peu le morceau précédent
        pts.insert(0, pts[0] - (pts[1] - pts[0]).normalized() * 0.02)
        rayons.insert(0, rayons[0])
    bm = bmesh.new()
    C.bm_tube(bm, pts, 0.02, cotes=6, mat=0, rayons=rayons)
    # anneaux bioluminescents sur la tige
    for k in range(1, len(pts) - 1):
        if (a + k) % 2 == 0:
            d = (pts[k + 1] - pts[k - 1]).normalized()
            C.bm_tore(bm, 0.02 * rayons[k] + 0.004, 0.007, segments=8, segments_tube=3,
                      mat=2, matrice=P.matrice_vers(pts[k], d))
    if i == 0:                                  # bosse charnue sur le front
        b0 = C.U(*TIGE[0])
        P.bm_ellipsoide(bm, b0, (0.06, 0.06, 0.04), segments=8, anneaux=5, mat=0)
        for k in range(5):
            ang = 2 * math.pi * k / 5
            q = b0 + Vector((0.055 * math.cos(ang), 0.055 * math.sin(ang), 0.012))
            C.bm_sphere(bm, 0.012, q, segments=5, anneaux=3, mat=2)
    o = C.piece(f"Anim_Tige_{i + 1}", bm, MATS, lissage="tout", pivot=C.U(*TIGE[a]))
    C.parenter(o, parent)
    return o


def lanterne(parent):
    bout = C.U(*TIGE[-1])
    centre = bout + Vector((0, 0.01, -R_LANTERNE - 0.02))
    bm = bmesh.new()
    C.bm_sphere(bm, R_LANTERNE, centre, segments=10, anneaux=7, mat=1)
    # coiffe sombre et petit crochet
    avant = set(bm.verts)
    C.bm_revolution(bm, [(0.0, R_LANTERNE + 0.02), (0.035, R_LANTERNE + 0.012),
                         (0.05, R_LANTERNE - 0.012), (0.042, R_LANTERNE - 0.02)], 8,
                    mats=[0, 0, 0])
    bmesh.ops.translate(bm, vec=centre, verts=[v for v in bm.verts if v not in avant])
    C.bm_tube(bm, [bout, centre + Vector((0, 0, R_LANTERNE + 0.02))], 0.008, cotes=4, mat=0)
    # 3 nageoires-collerette cyan autour de la coiffe
    for k in range(3):
        ang = 2 * math.pi * k / 3 + 0.3
        d = Vector((math.cos(ang), math.sin(ang), 0.5)).normalized()
        m = C.repere(centre + d * (R_LANTERNE + 0.02), Vector((0, 0, 1)).cross(d).normalized())
        C.bm_coussin(bm, [(0.0, 0.0), (0.05, 0.03), (0.06, -0.01), (0.02, -0.025)], 0.008,
                     0.016, m, mat=2, anneaux=1)
    o = C.piece("Anim_Lanterne", bm, MATS, lissage="tout", pivot=bout)
    C.parenter(o, parent)
    pl = C.vide("PointLumiere", centre)
    pl.empty_display_size = 0.08
    C.parenter(pl, o)
    return o


def poisson(i, h, r, racine):
    piv = C.vide(f"Anim_OrbitePoisson_{i + 1}", C.U(0.0, h, 0.0))
    C.parenter(piv, racine)
    # à droite de l'image (Unity x = +r), nez vers l'avant (Unity −z) pour un tour positif
    sens = 1 if i != 1 else -1
    centre = C.U(r, h, 0.0)
    avant = Vector((0, sens, 0))                 # +Y Blender = Unity −z
    bm = bmesh.new()
    K = 1.6                                      # poissons agrandis (lisibles de loin)
    corps = [(0.0, 0.09 * K), (0.045 * K, 0.0), (0.0, -0.06 * K), (-0.045 * K, 0.0)]
    # repère du poisson : x local = le côté (largeur du losange), y local = vertical
    # (épaisseur), z local = l'avant (longueur) : un losange plat COUCHÉ, comme une petite
    # raie : la caméra du jeu, qui regarde d'en haut, le voit en entier à tout moment du tour
    haut = Vector((0, 0, 1))
    cote = avant.cross(haut).normalized()
    m = Matrix.Translation(centre) @ Matrix((cote, haut, avant)).transposed().to_4x4()
    C.bm_coussin(bm, corps, 0.014, 0.04, m, mat=2, anneaux=1)
    C.bm_coussin(bm, [(0.0, -0.05 * K), (0.035 * K, -0.10 * K), (-0.035 * K, -0.10 * K)],
                 0.01, 0.016, m, mat=2, anneaux=1)
    for s in (-1, 1):                            # deux petits yeux sombres
        q = centre + avant * 0.06 * K + cote * s * 0.025 + haut * 0.018
        C.bm_sphere(bm, 0.012, q, segments=5, anneaux=3, mat=0)
    o = C.piece(f"Poisson_{i + 1}", bm, MATS, lissage="angle", pivot=centre)
    C.parenter(o, piv)


def construire():
    C.nouvelle_scene()
    racine = C.vide(NOM, (0, 0, 0))
    parent = racine
    for i in range(3):
        parent = morceau(i, parent)
    lanterne(parent)
    for i, (h, r) in enumerate(POISSONS):
        poisson(i, h, r, racine)
    fx = C.vide("Fx_Emetteur", C.U(0.0, 0.55, 0.0))
    fx.empty_display_size = 0.05
    C.parenter(fx, racine)
    bpy.context.view_layer.update()
    return racine


if __name__ == "__main__":
    P.finir_pirate(construire(), NOM, "Joueur", y_min=-0.3)
