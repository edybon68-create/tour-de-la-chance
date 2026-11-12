"""
animal_carapace.py — Carapace de tortue (boîte « Animaux : accessoires » : `carapace`,
Peu commun, Dos). docs/specs/v5-catalogue.md §4 n°09, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_carapace.py
Sortie : blender/exports/carapace.fbx et docs/captures/v5_cheval_carapace.png.

Trois niveaux de forme :
  - primaire   : une grande carapace bombée (1,24 de large, 1,0 de haut) portée comme un
                 sac à dos sur l'arrière du cou : elle dépasse de 0,3 de chaque côté vu de
                 face (bible §13.2 : un objet de dos doit se voir) ;
  - secondaire : le bourrelet jaune du bord, 7 écailles hexagonales en relief bordées de
                 jaune, deux bretelles brunes ;
  - tertiaire  : les boucles dorées des bretelles, 10 petites écailles de bord (bosses).
Les bretelles font une boucle sur les CÔTÉS du cou, sous le niveau des colliers et
écharpes (y < 0,88) et jamais sur le poitrail : on peut porter un accessoire de cou avec.
Matières : écaille vernie 0,75, bord satiné 0,45, sangles mates 0,40, doré déco.

Hiérarchie :
    carapace         vide, origine = PointDos (attachScale 1, attachOffset 0)
    └── Carapace     tout l'objet (aucune animation propre ; ressort léger sur l'ensemble
                     possible : elle « tressaute » à l'atterrissage)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "carapace"
POINT = "PointDos"
MATS = ["M5_Carapace", "M5_BordCarapace", "M5_SangleBrune", "M5_DoreDeco"]  # 0..3
CENTRE = (0.0, 0.90, 0.37)          # Unity : centre de la face plate (contre le cou)
RX, RY, RZ = 0.62, 0.50, 0.30      # demi-largeur, demi-hauteur, épaisseur du dôme


def dome(bm):
    """Dôme : demi-ellipsoïde dont la face plate regarde le cheval (vers l'avant, +Y)."""
    c = C.U(*CENTRE)
    n_a, n_e = 18, 5
    rangs = []
    for j in range(n_e + 1):
        e = (math.pi / 2) * j / n_e         # 0 = le bord, π/2 = le sommet (vers l'arrière)
        rang = []
        for k in range(n_a):
            a = 2 * math.pi * k / n_a
            r = math.cos(e) ** 0.8
            p = c + Vector((RX * r * math.cos(a), -RZ * math.sin(e) - 0.02, RY * r * math.sin(a)))
            rang.append(p)
        rangs.append(rang)
    vs = [[bm.verts.new(p) for p in r] for r in rangs[:-1]]
    sommet = bm.verts.new(c + Vector((0, -RZ - 0.02, 0)))
    devant = [bm.verts.new(p + Vector((0, 0.02, 0))) for p in rangs[0]]
    faces = []
    for j in range(n_e - 1):
        for k in range(n_a):
            u = (k + 1) % n_a
            faces.append(bm.faces.new((vs[j][k], vs[j][u], vs[j + 1][u], vs[j + 1][k])))
    for k in range(n_a):
        faces.append(bm.faces.new((vs[-1][k], vs[-1][(k + 1) % n_a], sommet)))
        faces.append(bm.faces.new((vs[0][k], devant[k], devant[(k + 1) % n_a], vs[0][(k + 1) % n_a])))
    plastron = bm.faces.new(devant)
    for f in faces:
        f.material_index = 0
    plastron.material_index = 1        # le « plastron » clair, vu sur les côtés
    faces.append(plastron)
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    # bourrelet jaune sur le bord
    bord = []
    for k in range(n_a):
        a = 2 * math.pi * k / n_a
        bord.append(c + Vector(((RX + 0.01) * math.cos(a), -0.015, (RY + 0.01) * math.sin(a))))
    hauts = [(p - c).normalized() for p in bord]
    C.bm_balayage_surface(bm, bord, hauts, [(-0.05, -0.035), (0.05, -0.035), (0.06, 0.0),
                                            (0.05, 0.035), (-0.05, 0.035), (-0.06, 0.0)],
                          mat=1, ferme=True)
    # 12 bosses de bord (petites écailles marginales)
    for k in range(10):
        a = 2 * math.pi * (k + 0.5) / 10
        p = c + Vector(((RX + 0.05) * math.cos(a), -0.05, (RY + 0.05) * math.sin(a)))
        A.bm_ellipsoide(bm, p, (0.05, 0.035, 0.04), segments=6, anneaux=3, mat=1)
    return c


def ecailles(bm, c):
    """7 écailles hexagonales en relief (une au centre, 6 autour), bordées de jaune,
    posées sur le dôme par des rayons."""
    arbre = C.arbre_bm(bm)
    hexa = C.contour_ellipse(1.0, 1.0, n=6, depart=math.pi / 6)
    positions = [(0.0, 0.0, 0.17)] + [(0.36 * math.cos(math.radians(60 * k + 30)),
                                       0.30 * math.sin(math.radians(60 * k + 30)), 0.14)
                                      for k in range(6)]
    for x, z, r in positions:
        p, n = C.poser_sur(arbre, c + Vector((x, 0.0, z)), Vector((x * 0.9, -1.0, z * 0.9)), 0.0)
        m = C.repere(p, n)
        C.bm_plaque(bm, [(u * r, v * r) for u, v in hexa], 0.03, m, mat=1)
        C.bm_coussin(bm, [(u * r * 0.8, v * r * 0.8) for u, v in hexa], 0.03, 0.06,
                     m @ Matrix.Translation((0, 0.02, 0)), mat=0, anneaux=1)


def bretelles(bm):
    """Une boucle de sangle sur chaque côté du cou (sous les colliers, jamais devant)."""
    for s in (+1, -1):
        pts = []
        hauts = []
        for ang, y in ((150, 0.86), (118, 0.85), (92, 0.80), (78, 0.72), (88, 0.64),
                       (115, 0.61), (150, 0.62)):
            p, d = A.surface((0.0, y, 0.02), math.radians(s * ang), 0.0, 0.025,
                             fenetre=(0.2, 0.0), pas=0.1)
            pts.append(p)
            hauts.append(d)
        sec = [(-0.04, -0.012), (0.04, -0.012), (0.04, 0.012), (-0.04, 0.012)]
        C.bm_balayage_surface(bm, pts, hauts, sec, mat=2)
        # boucle dorée au milieu
        m = C.repere(pts[3] + hauts[3] * 0.015, hauts[3])
        C.bm_plaque(bm, C.contour_rectangle_arrondi(0.1, 0.07, 0.02, par_coin=1), 0.02, m, mat=3)


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    c = dome(bm)
    ecailles(bm, c)
    bretelles(bm)
    obj = C.piece("Carapace", bm, MATS, lissage="tout", pivot=C.U(*C.CHEVAL_ACCROCHES[POINT]))
    return C.racine_v5(NOM, POINT, [obj])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT, y_min=0.45)
