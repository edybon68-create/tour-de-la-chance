"""
animal_chaton_sac.py — Chaton dans son panier (boîte « Animaux : accessoires » :
`chaton_sac`, Super rare, Dos). docs/specs/v5-catalogue.md §4 n°13, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_chaton_sac.py
Sortie : blender/exports/chaton_sac.fbx et docs/captures/v5_cheval_chaton_sac.png.

Un panier d'osier porté sur le dos ; un chaton gris en sort la tête et les pattes avant,
bien visible AU-DESSUS DE L'ÉPAULE DROITE de l'écran (Unity x positif).

Trois niveaux de forme :
  - primaire   : le panier rond tressé (Ø 0,70) décalé vers la droite, et la tête du chaton
                 (0,34 de large : 150 %) penchée hors du panier ;
  - secondaire : grandes oreilles, deux pattes posées sur le bord, le bourrelet du bord,
                 deux bretelles (boucles sur les côtés du cou, sous les colliers) ;
  - tertiaire  : tressage (rangs décalés en relief), yeux verts brillants avec pupille et
                 reflet, petite truffe vernie, joues rebondies, collier et CLOCHETTE DORÉS
                 (la matière précieuse du Super rare).
Matières (5 + reflet) : osier mat 0,30, pelage mat 0,15, yeux verts vernis émissifs 0,4
(≤ 10 % de la surface, autorisé au Super rare), noir verni, doré déco.

Hiérarchie :
    chaton_sac              vide, origine = PointDos (attachScale 1, attachOffset 0)
    └── Panier              panier + bretelles
        ├── Fx_Emetteur     vide au-dessus du chaton (≤ 4 petits cœurs au saut de joie)
        └── Anim_Chaton     corps du chaton ; origine = le bord du panier (il se hausse)
            ├── Anim_TeteChaton   tête ; origine = le cou (suit le curseur des yeux : ±25°)
            │   ├── Anim_YeuxChaton  les 2 yeux ; origine = entre les yeux (clignement :
            │   │                    échelle verticale 1 → 0,1 → 1)
            │   └── Clochette        (Eclat_1..3 : sur la clochette et les yeux, 1 / 2,5 s)
            └── Anim_PatteChaton  patte gauche de l'image ; origine = l'épaule (elle se
                                  lève au saut de joie, son `chaton_miaou`)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "chaton_sac"
POINT = "PointDos"
# Bible §13.7 (Super rare) : 5 matières au plus (+ reflet). Les indices 1, 2, 4 et 9 sont
# des « rôles » qui réutilisent une matière : rangs foncés et bretelles en osier, intérieur
# des oreilles et joues dans le pelage.
MATS = ["M5_Osier", "M5_Osier", "M5_Osier", "M5_PelageChaton", "M5_PelageChaton",
        "M5_YeuxChaton", "M5_AnimNoirVerni", "M5_Reflet", "M5_DoreDeco", "M5_PelageChaton"]
BASE = (0.10, 0.70, 0.62)          # Unity : centre du fond du panier
H_PANIER = 0.42
R_PANIER = 0.35


def panier(bm):
    b = C.U(*BASE)
    profil = [(0.0, 0.0), (0.27, 0.0), (0.31, 0.04), (0.33, 0.12), (0.345, 0.2),
              (0.35, 0.28), (0.35, 0.36), (0.345, H_PANIER), (0.31, H_PANIER), (0.31, 0.05),
              (0.0, 0.05)]
    mats = [0, 0, 1, 0, 1, 0, 1, 0, 1, 1]

    def tresse(a, i, r, z):
        if 2 <= i <= 6:
            return r + 0.014 * math.cos(11 * a + (math.pi if i % 2 else 0.0)), z
        return r, z
    av = set(bm.verts)
    C.bm_revolution(bm, profil, 22, mats=mats, rayon_fn=tresse)
    nv = [v for v in bm.verts if v not in av]
    # bourrelet du bord (tore), foncé
    nv += C.bm_tore(bm, 0.345, 0.035, segments=22, segments_tube=5, mat=1,
                    matrice=Matrix.Translation((0, 0, H_PANIER)))
    incl = Matrix.Rotation(math.radians(-10), 4, "X") @ Matrix.Rotation(math.radians(-8), 4, "Y")
    bmesh.ops.transform(bm, matrix=Matrix.Translation(b) @ incl, verts=nv)
    return Matrix.Translation(b) @ incl


def bretelles(bm):
    for s in (+1, -1):
        pts, hauts = [], []
        for ang, y in ((150, 0.86), (118, 0.85), (92, 0.80), (78, 0.72), (88, 0.64),
                       (115, 0.61), (150, 0.62)):
            p, d = A.surface((0.0, y, 0.02), math.radians(s * ang), 0.0, 0.025,
                             fenetre=(0.2, 0.0), pas=0.1)
            pts.append(p)
            hauts.append(d)
        C.bm_balayage_surface(bm, pts, hauts,
                              [(-0.04, -0.012), (0.04, -0.012), (0.04, 0.012), (-0.04, 0.012)],
                              mat=2)


def chaton(mp):
    """Le chaton, construit dans le repère du panier mp (z = axe du panier)."""
    bord = mp @ Vector((-0.17, 0.10, H_PANIER))            # côté droit de l'écran (−X)
    # corps : une boule qui sort du panier
    bm = bmesh.new()
    A.bm_ellipsoide(bm, mp @ Vector((-0.12, 0.08, H_PANIER - 0.02)), (0.17, 0.15, 0.16),
                    segments=10, anneaux=7, mat=3)
    # patte droite (fixe) posée sur le bord
    A.bm_ellipsoide(bm, mp @ Vector((-0.27, 0.22, H_PANIER + 0.03)), (0.05, 0.075, 0.045),
                    segments=8, anneaux=5, mat=3)
    corps = C.piece("Anim_Chaton", bm, MATS, lissage="tout", pivot=bord)

    cou = mp @ Vector((-0.16, 0.12, H_PANIER + 0.10))
    tc = cou + Vector((-0.12, 0.10, 0.18))                # la tête penche vers la droite
    bm = bmesh.new()
    A.bm_ellipsoide(bm, tc, (0.17, 0.14, 0.145), segments=10, anneaux=7, mat=3)
    for s in (-1, 1):                                     # joues claires
        A.bm_ellipsoide(bm, tc + Vector((s * 0.06, 0.11, -0.05)), (0.06, 0.04, 0.045),
                        segments=8, anneaux=5, mat=9)
    C.bm_sphere(bm, 0.024, tc + Vector((0, 0.15, -0.02)), segments=6, anneaux=4, mat=6)
    for s in (-1, 1):                                     # oreilles
        base = tc + Vector((s * 0.09, -0.01, 0.10))
        d = Vector((s * 0.45, 0.05, 1.0)).normalized()
        A.bm_goutte(bm, 0.14, 0.065, segments=6, bandes=((1.0, 3),), aplati=0.55,
                    matrice=A.matrice_vers(base, d, Vector((0, 1, 0))))
        A.bm_goutte(bm, 0.09, 0.04, segments=5, bandes=((1.0, 4),), aplati=0.4,
                    matrice=A.matrice_vers(base + Vector((0, 0.03, 0.01)), d, Vector((0, 1, 0))))
    # ruban rose + clochette sous le menton
    rub = C.bm_tore(bm, 0.11, 0.02, segments=12, segments_tube=4, mat=8)
    bmesh.ops.transform(bm, matrix=Matrix.Translation(tc + Vector((0, 0.02, -0.12)))
                        @ Matrix.Rotation(math.radians(15), 4, "X"), verts=rub)
    tete = C.piece("Anim_TeteChaton", bm, MATS, lissage="tout", pivot=cou)
    C.parenter(tete, corps)

    bm = bmesh.new()
    cloche = tc + Vector((0, 0.12, -0.16))
    C.bm_revolution(bm, [(0.0, 0.035), (0.025, 0.03), (0.035, 0.0), (0.042, -0.03),
                         (0.0, -0.03)], 8, mats=[8] * 4)
    bmesh.ops.translate(bm, vec=cloche, verts=bm.verts)
    cl = C.piece("Clochette", bm, MATS, lissage="angle", pivot=cloche)
    C.parenter(cl, tete)

    bm = bmesh.new()
    milieu = tc + Vector((0, 0.13, 0.02))
    for s in (-1, 1):
        regard = Vector((s * 0.15, 1.0, 0.0)).normalized()
        c = tc + Vector((s * 0.065, 0.12, 0.025))
        A.bm_ellipsoide(bm, c, (0.042, 0.022, 0.05), segments=8, anneaux=5, mat=5)
        A.oeil_mignon(bm, c + regard * 0.02, regard, 0.022, 6, 7, hauteur=1.5)
    yeux = C.piece("Anim_YeuxChaton", bm, MATS, lissage="tout", pivot=milieu)
    C.parenter(yeux, tete)

    bm = bmesh.new()
    epaule = mp @ Vector((-0.02, 0.18, H_PANIER))
    A.bm_ellipsoide(bm, epaule + Vector((-0.02, 0.06, 0.03)), (0.05, 0.075, 0.045),
                    segments=8, anneaux=5, mat=3)
    patte = C.piece("Anim_PatteChaton", bm, MATS, lissage="tout", pivot=epaule)
    C.parenter(patte, corps)
    return corps, [cloche + Vector((0.03, 0.04, 0.0)), milieu + Vector((0.07, 0.03, 0.03)),
                   milieu + Vector((-0.07, 0.03, 0.03))], tc + Vector((0, 0, 0.3))


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    mp = panier(bm)
    bretelles(bm)
    pan = C.piece("Panier", bm, MATS, lissage="tout", pivot=C.U(*C.CHEVAL_ACCROCHES[POINT]))
    corps, eclats, fx = chaton(mp)
    C.parenter(corps, pan)
    e = C.reperes_effets(eclats, fx=fx)
    tete = next(o for o in corps.children if o.name == "Anim_TeteChaton")
    for o in e[:3]:
        C.parenter(o, tete)
    C.parenter(e[3], pan)
    return C.racine_v5(NOM, POINT, [pan])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT, y_min=0.45)
