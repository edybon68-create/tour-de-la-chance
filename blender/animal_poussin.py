"""
animal_poussin.py — Poussin (boîte « Animaux : accessoires » : `poussin`, Rare, Tête).
docs/specs/v5-catalogue.md §4 n°12, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_poussin.py
Sortie : blender/exports/poussin.fbx et docs/captures/v5_cheval_poussin.png.

Un poussin tout rond assis sur le crâne du cheval, entre les oreilles, une coquille d'œuf
cassée sur la tête.

Trois niveaux de forme :
  - primaire   : la boule jaune (Ø 0,32 : la fiche dit 0,22, exagérée à 145 % pour être lue
                 depuis la caméra du jeu) un peu en poire ;
  - secondaire : la coquille d'œuf au bord en zigzag, penchée sur le côté (asymétrie de
                 vie) ; deux petites ailes ; le bec ;
  - tertiaire  : yeux vernis avec reflet, deux pattes orange à 3 doigts, une
                 petite mèche de duvet qui sort de la coquille.
Matières : duvet mat 0,20, bec et pattes vernis 0,85, coquille vernie 0,80, reflet.

Hiérarchie :
    poussin              vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Anim_Poussin     le corps (+ coquille, bec, yeux, pattes) ; origine = sous lui,
        │                sur le crâne (il sautille : translation Y ; il « regarde le coffre
        │                survolé » : rotation Y ±35°)
        ├── Anim_AileGauche   origine = l'épaule (battent au saut de joie)
        ├── Anim_AileDroite
        └── Eclat_1..3        vides sur la coquille et le bec (Rare : 1 éclat / 4 s)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "poussin"
POINT = "PointChapeau"
MATS = ["M5_DuvetPoussin", "M5_BecOrange", "M5_CoquilleOeuf", "M5_AnimNoirVerni",
        "M5_Reflet"]  # 0..4
R = 0.16


def coquille(bm, c):
    """Demi-coquille d'œuf posée sur le haut de la tête, bord en zigzag, penchée."""
    n = 16
    anneaux = []
    for j in range(5):
        t = j / 4                        # 0 = le bord, 1 = le sommet
        el = math.radians(38 + 50 * t)
        rang = []
        for k in range(n):
            a = 2 * math.pi * k / n
            zig = (0.035 if k % 2 == 0 else -0.01) if j == 0 else 0.0
            r = R * 1.08 * math.cos(el)
            z = R * 1.22 * math.sin(el) - zig
            rang.append(Vector((r * math.cos(a), r * math.sin(a), z)))
        anneaux.append(rang)
    sommet = Vector((0, 0, R * 1.24))
    vs = [[bm.verts.new(p) for p in rang] for rang in anneaux]
    vt = bm.verts.new(sommet)
    # coque épaisse : on double vers l'intérieur
    vi = [bm.verts.new(p * 0.9) for p in anneaux[0]]
    faces = []
    for j in range(4):
        for k in range(n):
            u = (k + 1) % n
            faces.append(bm.faces.new((vs[j][k], vs[j][u], vs[j + 1][u], vs[j + 1][k])))
    for k in range(n):
        faces.append(bm.faces.new((vs[4][k], vs[4][(k + 1) % n], vt)))
        faces.append(bm.faces.new((vs[0][k], vi[k], vi[(k + 1) % n], vs[0][(k + 1) % n])))
    for f in faces:
        f.material_index = 2
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    verts = [v for r in vs for v in r] + [vt] + vi
    m = Matrix.Translation(c + Vector((-0.01, -0.02, 0.03))) @ Matrix.Rotation(
        math.radians(-16), 4, "Y") @ Matrix.Rotation(math.radians(-8), 4, "X")
    bmesh.ops.transform(bm, matrix=m, verts=verts)
    return m @ Vector((0.05, 0.06, R * 1.2))


def construire():
    C.nouvelle_scene()
    assise = C.U(0.0, 1.875, 0.04)
    c = assise + Vector((0, 0, R * 0.92))
    bm = bmesh.new()
    A.bm_ellipsoide(bm, c, (R, R * 0.95, R * 0.95), segments=14, anneaux=10, mat=0)
    # le bas un peu plus large (poire)
    for v in bm.verts:
        if v.co.z < c.z:
            f = 1.0 + 0.08 * (c.z - v.co.z) / R
            v.co.x = c.x + (v.co.x - c.x) * f
            v.co.y = c.y + (v.co.y - c.y) * f
    eclat_coq = coquille(bm, c)
    # bec : deux petits cônes (haut plus long)
    for z, L, r in ((0.012, 0.085, 0.045), (-0.022, 0.06, 0.035)):
        A.bm_goutte(bm, L, r, segments=6, bandes=((1.0, 1),), aplati=0.7,
                    matrice=A.matrice_vers(c + Vector((0, R * 0.86, z - 0.01)),
                                           Vector((0, 1, -0.15 if z > 0 else -0.5))))
    for s in (-1, 1):
        regard = Vector((s * 0.35, 1.0, 0.1)).normalized()
        A.oeil_mignon(bm, c + Vector((s * 0.065, R * 0.86, 0.05)), regard, 0.03, 3, 4)
        # pattes : 3 doigts orange devant, sur le crâne
        pied = assise + Vector((s * 0.06, 0.12, 0.02))
        for dx in (-0.03, 0.0, 0.03):
            C.bm_tube(bm, [pied, pied + Vector((dx, 0.05, -0.012))], 0.013, cotes=5, mat=1)
    # mèche de duvet qui sort de la coquille (3 petites gouttes)
    for k, (dx, inc) in enumerate(((0.0, 0), (0.03, 25), (-0.03, -25))):
        A.bm_goutte(bm, 0.08, 0.025, segments=5, bandes=((1.0, 0),),
                    matrice=A.matrice_vers(c + Vector((dx - 0.02, -0.02, R * 1.28)),
                                           Vector((math.sin(math.radians(inc)) - 0.2, 0, 1))))
    corps = C.piece("Anim_Poussin", bm, MATS, lissage="tout", pivot=assise)
    for s, nom in ((1, "Anim_AileGauche"), (-1, "Anim_AileDroite")):
        bm = bmesh.new()
        epaule = c + Vector((s * R * 0.9, 0.0, 0.03))
        A.bm_ellipsoide(bm, epaule + Vector((s * 0.03, 0.01, -0.05)), (0.035, 0.06, 0.08),
                        segments=8, anneaux=6, mat=0, rot=Matrix.Rotation(math.radians(s * 20), 4, "Y"))
        C.parenter(C.piece(nom, bm, MATS, lissage="tout", pivot=epaule), corps)
    for e in C.reperes_effets([eclat_coq, c + Vector((0, R + 0.06, 0.0)),
                               c + Vector((-0.1, 0.05, R * 1.25))]):
        C.parenter(e, corps)
    return C.racine_v5(NOM, POINT, [corps])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT)
