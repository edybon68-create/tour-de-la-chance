"""
echarpe.py — Écharpe rayée (boîte d'accessoires n°1 : `echarpe`, Peu commun, Cou).
VERSION CHEVAL (v5) : elle épouse le vrai cou du cheval (blender/personnage_cheval.py).

Lancer (Blender fermé, ou en tout cas pas besoin de l'ouvrir) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/echarpe.py
Sortie : blender/exports/echarpe.fbx (dossier d'attente) et docs/captures/v5_cheval_echarpe.png.

Pourquoi refaite ?
    L'ancienne écharpe était un anneau ROND fait pour la capsule (rayon 0,535), réduit à
    0,62 pour le cheval. Or le cou du cheval n'est pas rond : il est plus profond que large,
    et la crinière dépasse derrière. Résultat : un jour visible devant et sur les côtés,
    l'anneau qui rentre dans la crinière derrière, et un pan caché DANS le poitrail.

Comment elle colle au cou (sans traverser) :
    pour chaque « tranche » de l'anneau (24 autour du cou), on lance des rayons depuis
    l'axe du cou vers l'extérieur sur toute la hauteur de l'écharpe (commun.sur_cheval_lisse)
    et on garde la surface la plus éloignée. L'écharpe passe donc PAR-DESSUS la crinière, en
    pente douce, au lieu de rentrer dedans. Le bord intérieur est 1 cm sous la « peau » :
    aucun jour visible, même de près.
    L'anneau penche comme un vrai foulard noué : plus bas devant (sur le poitrail), plus
    haut derrière (sur la nuque).

Refonte v5 (bible §13.8 fiche 8) :
  - primaire   : DEUX tours épais (16 et 15 cm de haut, 9 cm d'épaisseur), décalés et
                 inclinés l'un sur l'autre (le tour du haut penche de 8° de plus) ;
  - secondaire : le nœud devant à gauche de l'image et deux pans (gauche 0,40, droit 0,28 :
                 l'asymétrie de vie) qui suivent le poitrail ;
  - tertiaire  : 5 franges (petits cylindres arrondis Ø 6 cm) au bout de chaque pan ; une
                 rainure de tricot à chaque changement de rayure.
  Matière : laine mate (0,15), compensée par le contraste menthe / blanc cassé.

Réglage Unity : enfant de `PointCou`, décalage (0 ; 0 ; 0), échelle 1 (plus d'échelle 0,62).
Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    echarpe             vide, origine = PointCou
    └── Tours           les 2 tours + le nœud ; origine = PointCou
        ├── Anim_PanGauche  pan à gauche de l'image (le long) ; origine = le nœud (ressort,
        └── Anim_PanDroit   et balancement ±5° en 2,2 s, le pan droit décalé de 0,1 s)
2 matériaux : M_LaineMenthe / M_LaineBlanche (bible §2.8).
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "echarpe"
POINT = "PointCou"
MATS = ["M_LaineMenthe", "M_LaineBlanche"]  # indices 0, 1

SEGMENTS = 24            # tranches autour du cou (12 rayures de 2 tranches)
Y_MILIEU = 1.00          # hauteur moyenne du tour du BAS (Unity)
PENTE = 0.05             # le tour descend de 5 cm devant et monte de 5 cm derrière
# Tour du haut : 8 cm plus haut, penché de 8° de plus (0,04 m sur le rayon du cou), posé
# 2 cm plus à l'extérieur (il recouvre le tour du bas, comme une écharpe enroulée deux fois)
TOUR_HAUT = (0.08, 0.04, 0.02)
AXE_Z = 0.02             # l'axe du cou est 2 cm en arrière (Unity z)
# Section de l'anneau : (épaisseur vers l'extérieur, hauteur). Un boudin de laine
# un peu gonflé, 17 cm de haut, 9 cm d'épaisseur ; le premier et le dernier point sont
# 1,2 cm SOUS la surface du cou (pas de jour).
SECTION = [(-0.012, -0.07), (0.025, -0.085), (0.062, -0.068), (0.08, -0.025),
           (0.08, 0.025), (0.062, 0.068), (0.025, 0.085), (-0.012, 0.07)]
ANGLE_NOEUD = 0.55       # le nœud est devant, un peu à gauche de l'image (radians)
ARBRE = None


def centre_tranche(a, tour=0):
    """Hauteur (Unity) du milieu du tour (0 = bas, 1 = haut) dans la direction a (0 = devant)."""
    if tour == 1:
        return Y_MILIEU + TOUR_HAUT[0] - (PENTE + TOUR_HAUT[1]) * math.cos(a)
    return Y_MILIEU - PENTE * math.cos(a)


def rayon_tranche(a, demi_hauteur=0.085, tour=0):
    """Distance de l'axe du cou à la surface la plus éloignée sur toute la hauteur de
    l'écharpe dans la direction a (avec le lissage « tente » de sur_cheval_lisse)."""
    y0 = centre_tranche(a, tour)
    r = 0.0
    for dy in (-demi_hauteur, -demi_hauteur / 2, 0.0, demi_hauteur / 2, demi_hauteur):
        centre = (0.0, y0 + dy, AXE_Z)
        p, d = C.sur_cheval_lisse(ARBRE, centre, a, 0.0, 0.0, pente=0.6,
                                  fenetre=(0.42, 0.0), pas=0.14)
        r = max(r, (p - C.depuis_unity(*centre)).dot(d))
    return r


def anneau(bm, tour=0):
    """Un tour de l'écharpe. Les tranches de bord de rayure (une sur deux) rentrent de
    1,2 cm : c'est la rainure de tricot entre deux rayures."""
    rangs = []
    echelle_h = 0.94 if tour == 1 else 1.0
    for i in range(SEGMENTS):
        a = 2 * math.pi * i / SEGMENTS + (math.pi / SEGMENTS if tour else 0.0)
        d = C.direction_angles(a)
        r = rayon_tranche(a, tour=tour)
        if tour == 1:      # le tour du haut passe par-dessus le tour du bas
            r = max(r, rayon_tranche(a, tour=0) + TOUR_HAUT[2])
        axe = C.depuis_unity(0.0, centre_tranche(a, tour), AXE_Z)
        rainure = 0.012 if i % 2 == 0 else 0.0
        rangs.append([bm.verts.new(axe + d * (r + dr - (rainure if dr > 0.03 else 0.0))
                                   + Vector((0, 0, dz * echelle_h)))
                      for dr, dz in SECTION])
    m = len(SECTION)
    for i in range(SEGMENTS):
        A, B = rangs[i], rangs[(i + 1) % SEGMENTS]
        for s in range(m):
            u = (s + 1) % m
            f = bm.faces.new((A[s], B[s], B[u], A[u]))
            f.material_index = ((i // 2) + tour) % 2          # 12 rayures
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])


def noeud(bm):
    """Le nœud : une boule de laine aplatie, devant à gauche de l'image, sur les 2 tours.
    Renvoie son centre (l'origine des pans)."""
    a = ANGLE_NOEUD
    d = C.direction_angles(a)
    y_n = centre_tranche(a, 1)
    r = max(rayon_tranche(a, tour=1), rayon_tranche(a) + TOUR_HAUT[2])
    centre = C.depuis_unity(0.0, y_n - 0.02, AXE_Z) + d * (r + 0.09)
    rot = d.to_track_quat("Y", "Z").to_matrix().to_4x4()
    v = C.bm_sphere(bm, 1.0, (0, 0, 0), segments=8, anneaux=6, echelle=(0.085, 0.06, 0.09))
    bmesh.ops.transform(bm, matrix=Matrix.Translation(centre) @ rot, verts=v)
    return centre, y_n - 0.02


def pan(nom, centre_noeud, y_n, a0, derive, longueur):
    """Un pan qui pend du nœud sur le poitrail en suivant sa courbe (chaque point est
    reposé sur la surface par un rayon), rayé en 4 bandes, avec 5 franges au bout."""
    bm = bmesh.new()
    section = C.contour_rectangle_arrondi(0.15, 0.04, 0.016, par_coin=1)
    chemin, hauts = [], []
    n = 7
    for k in range(n + 1):
        t = k / n
        ak = a0 + derive * t
        yk = y_n - 0.03 - longueur * t
        p, dk = C.sur_cheval_lisse(ARBRE, (0.0, yk, AXE_Z), ak, 0.0, 0.035, pente=0.6,
                                   fenetre=(0.28, 0.0), pas=0.14)
        if k == 0:
            p = p.lerp(centre_noeud, 0.5)
        chemin.append(p)
        hauts.append(dk)
    anneaux = C.bm_balayage_surface(bm, chemin, hauts, section, mat=0)
    haut_z, bas_z = chemin[0].z, chemin[-1].z
    for f in {f for r_ in anneaux for vv in r_ for f in vv.link_faces}:
        t = (haut_z - f.calc_center_median().z) / (haut_z - bas_z)
        f.material_index = min(int(t * 4), 3) % 2
    # franges : 5 petits cylindres arrondis, blanc cassé, sous le bout du pan
    bout = chemin[-1]
    tdir = (chemin[-1] - chemin[-2]).normalized()
    cote = tdir.cross(hauts[-1]).normalized()
    for j in range(5):
        x = (j - 2) * 0.031
        p0 = bout + cote * x + tdir * 0.01
        C.bm_tube(bm, [p0, p0 + tdir * 0.035, p0 + tdir * 0.06], 0.03, cotes=6, mat=1,
                  rayons=[1.0, 1.0, 0.55])
    return C.piece(nom, bm, MATS, lissage="tout", pivot=centre_noeud)


def construire():
    global ARBRE
    C.nouvelle_scene()
    ARBRE = C.arbre_cheval()
    bm = bmesh.new()
    anneau(bm, 0)
    anneau(bm, 1)
    centre_n, y_n = noeud(bm)
    tours = C.piece("Tours", bm, MATS, lissage="tout", pivot=C.U(*C.CHEVAL_ACCROCHES[POINT]))
    a = ANGLE_NOEUD
    for nom, a0, derive, longueur in (("Anim_PanGauche", a - 0.06, 0.10, 0.40),
                                      ("Anim_PanDroit", a + 0.14, 0.26, 0.28)):
        C.parenter(pan(nom, centre_n, y_n, a0, derive, longueur), tours)
    return C.racine_v5(NOM, POINT, [tours])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT, y_min=0.55)
