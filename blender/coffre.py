"""
coffre.py — Modélise le coffre au trésor « jouet » de Tour de la Chance et l'exporte en FBX.

Fiche suivie : docs/art-bible.md §6.1. Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/coffre.py

Hiérarchie produite (le code Unity `Chest` retrouve les objets par leur nom) :
    Coffre              origine au sol, au centre (0 ; 0 ; 0)
    ├── Corps           bois du corps, UN SEUL matériau (le code le repeint en or / rouge)
    ├── Ferrures        bandes, coins, serrure, charnières fixes (métal + laiton)
    ├── Interieur       fond et parois intérieures (on les voit quand c'est ouvert)
    └── Charniere       objet vide sur l'axe de rotation, Unity (0 ; 0,8 ; 0,5)
        └── Couvercle   couvercle bombé complet, modélisé fermé

Rappel des axes : on modélise l'avant (serrure) vers +Y dans Blender ; il arrive vers -Z
dans Unity (voir commun.py). Toutes les cotes ci-dessous sont en mètres, repère Blender.
"""

import math
import os
import sys

import bmesh
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

# --- Dimensions (identiques au prototype) ------------------------------------------------
LARGEUR, PROFONDEUR, HAUTEUR = 1.40, 1.00, 0.80   # corps : x, y, z
AVANT = PROFONDEUR / 2                            # face avant en y = +0,5
PAROI = 0.06                                      # épaisseur des parois
FOND_INTERIEUR = 0.56                             # hauteur du fond intérieur (≈ 0,55)
RAINURE = 0.02                                    # largeur des rainures entre planches
PROF_RAINURE = 0.01                               # profondeur des rainures
CHARNIERE = C.depuis_unity(0, 0.8, 0.5)           # = (0 ; -0,5 ; 0,8) dans Blender

# Couvercle : 1,50 x 1,05 (débord de 5 cm sur les côtés et devant), 0,35 de haut maximum.
COUV_X = 0.75
COUV_ARRIERE, COUV_AVANT = -0.50, 0.55
COUV_BORD = 0.08          # partie verticale avant le bombé
COUV_DOME = 0.27          # hauteur du bombé (0,08 + 0,27 = 0,35)
SEGMENTS_DOME = 6         # 6 segments sur l'arrondi (fiche)

X_BANDES = 0.35           # bandes de métal à ~25 % des bords (±0,35)
L_BANDE, RELIEF = 0.08, 0.02


def profil_dome(decalage=0.0, z0=HAUTEUR):
    """Points (y, z) du dessus du couvercle, de l'avant vers l'arrière : bord vertical
    avant, demi-ellipse aplatie (6 segments), bord vertical arrière.
    `decalage` gonfle (>0) ou rentre (<0) le profil : sert aux bandes de métal."""
    yc = (COUV_AVANT + COUV_ARRIERE) / 2
    a = (COUV_AVANT - COUV_ARRIERE) / 2 + decalage
    b = COUV_DOME + decalage
    pts = [(yc + a, z0)]
    for k in range(SEGMENTS_DOME + 1):
        t = math.pi * k / SEGMENTS_DOME
        pts.append((yc + a * math.cos(t), z0 + COUV_BORD + b * math.sin(t)))
    pts.append((yc - a, z0))
    return pts


# =========================================================================================
def doublure_couvercle(mat_cadre, mat_fond):
    """Dessous du couvercle, vu à chaque ouverture.
    V7 (retour de l'auteur du 14/11) : le coussin capitonné faisait « faux » (en jeu on voyait
    surtout un grand panneau jaune). On revient à un intérieur SOMBRE et PROPRE, assorti à la
    caisse : le fond reste la doublure foncée (`mat_fond`, celle de l'intérieur du corps) et un
    simple cadre biseauté dans le bois du couvercle (`mat_cadre`) en fait le tour, comme le
    rebord de la caisse vu d'en haut. Deux tons, aucune couleur vive.
    Le cadre reste DANS l'ouverture du corps quand c'est fermé (marge de 13 cm) : rien ne
    traverse les parois. Ce sont des boîtes fermées : leurs normales sont fiables (le coussin
    ouvert avait des normales retournées, donc invisible dans Unity)."""
    bm = bmesh.new()
    y0, y1 = COUV_ARRIERE + 0.13, COUV_AVANT - 0.13
    lx, ly = 2 * (COUV_X - 0.13), y1 - y0
    cy = (y0 + y1) / 2
    lb, ep = 0.06, 0.025          # largeur et épaisseur du cadre
    z = HAUTEUR - ep / 2 + 0.002  # collé sous le couvercle (2 mm dedans : pas de jour)
    # deux longs côtés (avant / arrière) puis deux petits côtés entre eux
    for sy in (-1, 1):
        C.bm_boite(bm, (lx, lb, ep), (0, cy + sy * (ly - lb) / 2, z), mat=0)
    for sx in (-1, 1):
        C.bm_boite(bm, (lb, ly - 2 * lb, ep), (sx * (lx - lb) / 2, cy, z), mat=0)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    o = C.objet_depuis_bmesh("DoublureCouvercle", bm, [mat_cadre, mat_fond])
    C.biseau(o, 0.008, 2, 30)
    C.appliquer(o)
    return o


def construire():
    C.nouvelle_scene()
    racine = C.vide("Coffre")

    # ---------------------------------------------------------------- Corps (bois seul)
    # 3 planches horizontales séparées par des rainures : chaque planche est une boîte
    # biseautée, et un « noyau » un peu rentré (1 cm) bouche les rainures.
    h_planche = (HAUTEUR - 2 * RAINURE) / 3
    pieces = []
    for i in range(3):
        z0 = i * (h_planche + RAINURE)
        bm = bmesh.new()
        if i < 2:
            C.bm_boite(bm, (LARGEUR, PROFONDEUR, h_planche), (0, 0, z0 + h_planche / 2))
        else:
            # La planche du haut est un cadre creux : on voit l'intérieur du coffre.
            ex, ey = LARGEUR / 2, PROFONDEUR / 2
            ix, iy = ex - PAROI, ey - PAROI
            z1 = HAUTEUR

            def rect(x, y, z):
                return [(-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
            C.bm_anneaux(bm, [rect(ex, ey, z0), rect(ex, ey, z1), rect(ix, iy, z1),
                              rect(ix, iy, z0), rect(ex, ey, z0)])
            bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
            bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        o = C.objet_depuis_bmesh(f"Planche{i}", bm, ["M_BoisCorps"])
        C.biseau(o, largeur=0.02, segments=1, angle=30)
        C.appliquer(o)
        pieces.append(o)
    noyau = C.boite("Noyau", (LARGEUR - 2 * PROF_RAINURE, PROFONDEUR - 2 * PROF_RAINURE, 0.54),
                    (0, 0, 0.01 + 0.27), "M_BoisCorps")
    pieces.append(noyau)
    corps = C.joindre(pieces, "Corps")
    C.lisser_angle(corps, 30)

    # ---------------------------------------------------------------- Interieur
    # Une « doublure » : boîte sans couvercle dont les faces regardent vers l'intérieur
    # (normales retournées). Vue d'en haut, la caméra voit le fond et les parois sombres.
    bm = bmesh.new()
    ix, iy = LARGEUR / 2 - PAROI - 0.002, PROFONDEUR / 2 - PAROI - 0.002
    h = HAUTEUR - 0.004 - FOND_INTERIEUR
    C.bm_boite(bm, (2 * ix, 2 * iy, h), (0, 0, FOND_INTERIEUR + h / 2))
    dessus = [f for f in bm.faces if f.normal.z > 0.9]
    bmesh.ops.delete(bm, geom=dessus, context="FACES")
    bmesh.ops.reverse_faces(bm, faces=bm.faces)
    interieur = C.objet_depuis_bmesh("Interieur", bm, ["M_Interieur"])

    # ---------------------------------------------------------------- Ferrures
    # Matériaux : 0 = métal sombre, 1 = laiton, 2 = trou de serrure.
    MAT_F = ["M_MetalSombre", "M_Laiton", "M_TrouSerrure"]
    ferr = []
    # Deux bandes verticales sur la face avant (2 cm de relief).
    bm = bmesh.new()
    for sx in (-1, 1):
        C.bm_boite(bm, (L_BANDE, RELIEF + 0.01, HAUTEUR),
                   (sx * X_BANDES, AVANT + (RELIEF - 0.01) / 2, HAUTEUR / 2), mat=0)
    o = C.objet_depuis_bmesh("Bandes", bm, MAT_F)
    C.biseau(o, 0.008, 1, 30)
    C.appliquer(o)
    ferr.append(o)

    # Coins en équerre sur les deux arêtes verticales avant, en bas et en haut.
    bm = bmesh.new()
    for sx in (-1, 1):
        for zc in (0.075, HAUTEUR - 0.075):
            # aile sur la face avant
            C.bm_boite(bm, (0.12, 0.03, 0.15), (sx * (LARGEUR / 2 - 0.05), AVANT + 0.005, zc))
            # aile sur le côté
            C.bm_boite(bm, (0.03, 0.12, 0.15), (sx * (LARGEUR / 2 + 0.005), AVANT - 0.05, zc))
    ferr.append(C.objet_depuis_bmesh("Coins", bm, MAT_F))

    # Plaque de serrure en forme d'écusson (26 x 30 cm, 3 cm de relief), en laiton.
    demi, haut_plaque, rayon = 0.13, HAUTEUR - 0.02, 0.13
    centre_arc = haut_plaque - 0.30 + rayon
    contour = [(demi, haut_plaque), (-demi, haut_plaque)]
    for k in range(7):  # demi-cercle du bas, de gauche à droite
        t = math.pi + math.pi * k / 6
        contour.append((rayon * math.cos(t), centre_arc + rayon * math.sin(t)))
    bm = bmesh.new()
    C.bm_prisme(bm, contour, 0.035, axe="Y", decalage=AVANT - 0.005, mat=1)
    o = C.objet_depuis_bmesh("Plaque", bm, MAT_F)
    C.biseau(o, 0.01, 1, 30)
    C.appliquer(o)
    ferr.append(o)

    # Trou de serrure : un rond + un trapèze, très sombres, à peine en relief sur la plaque
    # (vu de loin, une forme presque noire se lit comme un creux).
    bm = bmesh.new()
    y_plaque = AVANT + 0.03
    z_trou = centre_arc + 0.01
    rond = [(0.032 * math.cos(2 * math.pi * k / 8), z_trou + 0.032 * math.sin(2 * math.pi * k / 8))
            for k in range(8)]
    C.bm_prisme(bm, rond, 0.006, axe="Y", decalage=y_plaque, mat=2)
    trapeze = [(-0.014, z_trou), (-0.026, z_trou - 0.075), (0.026, z_trou - 0.075), (0.014, z_trou)]
    C.bm_prisme(bm, trapeze, 0.006, axe="Y", decalage=y_plaque, mat=2)
    ferr.append(C.objet_depuis_bmesh("Trou", bm, MAT_F))

    # Charnières fixes : deux cylindres couchés (axe X) à l'arrière, sur l'axe de rotation.
    bm = bmesh.new()
    for sx in (-1, 1):
        cercle = [(CHARNIERE.y - 0.015 + 0.04 * math.cos(2 * math.pi * k / 8),
                   CHARNIERE.z + 0.01 + 0.04 * math.sin(2 * math.pi * k / 8)) for k in range(8)]
        C.bm_prisme(bm, cercle, 0.16, axe="X", decalage=sx * 0.45 - 0.08, mat=0)
    ferr.append(C.objet_depuis_bmesh("Gonds", bm, MAT_F))
    ferrures = C.joindre(ferr, "Ferrures")
    C.lisser_angle(ferrures, 30)

    # ---------------------------------------------------------------- Couvercle
    # Matériaux : 0 = bois foncé, 1 = métal sombre, 2 = laiton, 3 = intérieur (dessous).
    MAT_C = ["M_BoisCouvercle", "M_MetalSombre", "M_Laiton", "M_Interieur"]
    couv = []
    bm = bmesh.new()
    C.bm_prisme(bm, profil_dome(), 2 * COUV_X, axe="X", decalage=-COUV_X, mat=0)
    for f in bm.faces:  # le dessous plat (vu quand le couvercle est ouvert) en sombre
        if f.normal.z < -0.9:
            f.material_index = 3
    o = C.objet_depuis_bmesh("Dome", bm, MAT_C)
    # Biseau sur les arêtes vives seulement (pas entre les segments du bombé).
    C.biseau(o, 0.03, 1, 50)
    C.appliquer(o)
    couv.append(o)

    # Bandes de métal qui passent sur le bombé (suite des bandes du corps).
    bm = bmesh.new()
    ext = profil_dome(decalage=RELIEF)[1:-1]
    inte = profil_dome(decalage=-0.005)[1:-1]
    # on prolonge les bandes jusqu'en bas du bord avant et arrière
    ext = [(ext[0][0], HAUTEUR + 0.005)] + ext + [(ext[-1][0], HAUTEUR + 0.005)]
    inte = [(inte[0][0], HAUTEUR + 0.005)] + inte + [(inte[-1][0], HAUTEUR + 0.005)]
    for sx in (-1, 1):
        x0, x1 = sx * X_BANDES - L_BANDE / 2, sx * X_BANDES + L_BANDE / 2
        sections = [[(x0, e[0], e[1]), (x1, e[0], e[1]), (x1, i[0], i[1]), (x0, i[0], i[1])]
                    for e, i in zip(ext, inte)]
        rangs = C.bm_anneaux(bm, sections, mats=[1] * len(sections))
        for bout in (rangs[0], rangs[-1]):
            f = bm.faces.new(bout)
            f.material_index = 1
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    o = C.objet_depuis_bmesh("BandesCouvercle", bm, MAT_C)
    C.biseau(o, 0.006, 1, 40)
    C.appliquer(o)
    couv.append(o)

    # Fermoir en laiton qui descend du couvercle sur la plaque de serrure.
    bm = bmesh.new()
    C.bm_boite(bm, (0.10, 0.035, 0.16), (0, COUV_AVANT + 0.012, HAUTEUR - 0.02), mat=2)
    o = C.objet_depuis_bmesh("Fermoir", bm, MAT_C)
    C.biseau(o, 0.01, 1, 30)
    C.appliquer(o)
    couv.append(o)

    couv.append(doublure_couvercle("M_BoisCouvercle", "M_Interieur"))   # V7 : cadre sombre
    couvercle = C.joindre(couv, "Couvercle")
    C.lisser_angle(couvercle, 40)

    # ---------------------------------------------------------------- Hiérarchie et pivots
    charniere = C.vide("Charniere", CHARNIERE)
    C.deplacer_origine(couvercle, CHARNIERE)  # le pivot du couvercle = l'axe des charnières
    for o in (corps, ferrures, interieur, charniere):
        C.parenter(o, racine)
    C.parenter(couvercle, charniere)
    return racine


if __name__ == "__main__":
    racine = construire()
    C.rapport(racine)
    C.rendre_miniature(racine, "blender_coffre.png")
    chemin = C.exporter_fbx(racine, "coffre.fbx")
    C.verifier_fbx(chemin)
