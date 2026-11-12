"""
pirate_foulard_marin.py — Foulard marin (boîte d'accessoires Pirates : `foulard_marin`,
Super rare, Cou). Fiche : docs/specs/v5-catalogue.md §2 n° 04 ; règles : art-bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/pirate_foulard_marin.py
Sortie : blender/exports/foulard_marin.fbx et docs/captures/v5_cheval_foulard_marin.png.

Comment il épouse l'encolure (même méthode que l'Écharpe corrigée, v5 §4.3) :
    pour chacune des 24 tranches autour du cou, des rayons partent de l'axe du cou sur
    toute la hauteur de la bande (commun_pirates.tour_cou) : la bande se pose sur la
    surface la plus éloignée, lissée par-dessus la crinière, 1 cm sous la « peau » sur ses
    bords (aucun jour). Elle penche comme un foulard noué : plus basse devant (0,94),
    plus haute derrière (1,06). La pointe se pose sur le poitrail par des rayons.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : la bande bleu marine (14 cm de haut, 7 cm d'épaisseur) autour du cou ;
  - secondaire : le gros nœud devant (un nœud et deux « oreilles ») et LA POINTE en
                 triangle qui tombe sur le poitrail (0,30 de large, 0,30 de long) ;
  - tertiaire  : le liseré blanc verni (bandes de 2,5 cm en haut et en bas de la bande, et
                 tout autour de la pointe).
Matières (2) : marine satiné (smoothness 0,45) / blanc verni (0,85).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    foulard_marin       vide, origine = PointCou (attachScale 1, attachOffset 0)
    └── Foulard         bande + nœud ; origine = PointCou
        ├── Anim_Pointe la pointe ; origine = sous le nœud (ressort A, et balancement
        │               ±4° en 2 s : bible §13.5 A et B)
        └── Eclat_1..2  vides : sur le liseré (1 éclat / 2,5 s, émission 0,5)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_pirates as P  # noqa: E402

NOM = "foulard_marin"
POINT = "PointCou"
MATS = ["M5_PirMarine", "M5_BlancVerni"]   # 0, 1
N = 24
Y_MILIEU, PENTE = 1.00, 0.06
# Section de la bande (vers l'extérieur, hauteur) ; matériau de chaque côté de la section
SECTION = [(-0.012, -0.065), (0.03, -0.072), (0.06, -0.05), (0.068, -0.02),
           (0.068, 0.02), (0.06, 0.05), (0.03, 0.072), (-0.012, 0.065)]
MAT_SECTION = [1, 1, 0, 0, 0, 1, 1, 0]       # liseré blanc en haut et en bas


def hauteur(a):
    return Y_MILIEU - PENTE * math.cos(a)


def bande(bm):
    tour = P.tour_cou(hauteur, 0.0, n=N, demi_hauteur=0.065)
    rangs = []
    for (p, d) in tour:
        rangs.append([bm.verts.new(p + d * dr + Vector((0, 0, dz))) for dr, dz in SECTION])
    m = len(SECTION)
    for i in range(N):
        A, B = rangs[i], rangs[(i + 1) % N]
        for s in range(m):
            u = (s + 1) % m
            f = bm.faces.new((A[s], B[s], B[u], A[u]))
            f.material_index = MAT_SECTION[s]
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return tour


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    tour = bande(bm)
    p0, d0 = tour[0]                      # devant
    noeud = p0 + d0 * 0.12 + Vector((0, 0, -0.01))
    rot = d0.to_track_quat("Y", "Z").to_matrix()
    P.bm_ellipsoide(bm, noeud, (0.10, 0.07, 0.09), segments=8, anneaux=6, mat=0, rot=rot)
    for cote in (-1, 1):                  # les deux « oreilles » du nœud
        lat = d0.cross(Vector((0, 0, 1))).normalized() * cote
        q = noeud + lat * 0.14 + Vector((0, 0, 0.02)) - d0 * 0.02
        r2 = Matrix.Rotation(math.radians(-20 * cote), 3, d0) @ rot
        P.bm_ellipsoide(bm, q, (0.10, 0.045, 0.065), segments=8, anneaux=5, mat=0, rot=r2)
    foulard = C.piece("Foulard", bm, MATS, lissage="tout",
                      pivot=C.U(*C.CHEVAL_ACCROCHES[POINT]))

    # La pointe : un triangle qui suit le poitrail (posé par des rayons), bord blanc
    bm = bmesh.new()
    chemin, hauts, largeurs = [], [], []
    n = 6
    y_haut = noeud.z - 0.05
    for k in range(n + 1):
        t = k / n
        y = y_haut - 0.30 * t
        p, d = C.sur_cheval_lisse(P.arbre(), (0.0, y, P.AXE_COU_Z), 0.0, 0.0, 0.03,
                                  pente=0.6, fenetre=(0.3, 0.0), pas=0.15)
        if k == 0:
            p = p.lerp(noeud, 0.4)
        chemin.append(p)
        hauts.append(d)
        largeurs.append(0.15 * (1 - t) + 0.012)
    for couche, (mat, marge, dz) in enumerate(((1, 0.022, 0.0), (0, 0.0, 0.012))):
        an = C.bm_balayage_surface(bm, chemin, hauts,
                                   [(-1, 0), (1, 0), (1, 0.025), (-1, 0.025)], mat=mat)
        for i, anneau in enumerate(an):
            w = largeurs[i] + marge - (0.0 if i < n else 0.0)
            c = chemin[i]
            for v in anneau:
                off = v.co - c
                cote = (chemin[min(i + 1, n)] - chemin[max(i - 1, 0)]).cross(hauts[i]).normalized()
                v.co = c + cote * off.dot(cote) * w + hauts[i] * (off.dot(hauts[i]) + dz)
    pointe = C.piece("Anim_Pointe", bm, MATS, lissage="tout", pivot=chemin[0])
    C.parenter(pointe, foulard)
    eclats = [tour[3][0] + tour[3][1] * 0.07 + Vector((0, 0, 0.06)),
              tour[-3][0] + tour[-3][1] * 0.07 + Vector((0, 0, 0.06))]
    for e in C.reperes_effets(eclats):
        C.parenter(e, foulard)
    return C.racine_v5(NOM, POINT, [foulard])


if __name__ == "__main__":
    P.finir_pirate(construire(), NOM, POINT, y_min=0.55)
