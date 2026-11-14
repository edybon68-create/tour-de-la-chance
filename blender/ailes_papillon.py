"""
ailes_papillon.py — Ailes de papillon (boîte d'accessoires n°1 : `ailes_papillon`, Super rare, Dos).
VERSION v5 « plus jeu moderne » (docs/art-bible.md §13.8 fiche 14), faites SUR le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/ailes_papillon.py
Sortie : blender/exports/ailes_papillon.fbx et docs/captures/v5_cheval_ailes_papillon.png.

Défaut corrigé : faites pour l'ancienne capsule, leurs racines flottaient à côté du cou.
Les charnières sont maintenant SUR LE DOS, de part et d'autre de la crinière (x = ±0,12,
y = 1,02), posées sur la peau mesurée par un rayon ; un petit corps de papillon est posé
sur la crinière, entre les deux ailes : on comprend d'où elles partent.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : 2 lobes turquoise par aile (le grand en haut, le petit en bas), au bord
                 festonné (vagues de 5 cm), cernés de violet nuit ;
  - secondaire : 3 nervures en relief violet nuit par aile, qui partent de la racine ; le
                 corps central (capsule 0,25) et ses 2 antennes à boule ;
  - tertiaire  : les taches orange BOMBÉES et émissives (0,4, moins de 10 % de la surface).
Matières : lobes brillants (0,65), nervures / bord / corps mats (0,30), taches émissives.

Hiérarchie (le code Unity retrouve les pièces par leur nom ; AileGauche / AileDroite sont les
noms historiques que AccessoryMotion fait déjà battre, ±20° en 0,9 s autour de leur axe Y) :
    ailes_papillon             vide, origine = PointDos (attachScale 1, attachOffset 0)
    ├── AileGauche             aile à gauche de l'image ; origine = charnière sur le dos
    │   └── Eclat_1            vide : point d'éclat sur le grand lobe
    ├── AileDroite             (symétrique) ; └── Eclat_2
    ├── Corps                  le corps sur la crinière ; origine = sa base
    │   ├── Anim_AntenneGauche antenne + boule ; origine = sa base (ressort)
    │   ├── Anim_AntenneDroite
    │   └── Eclat_3            vide : en haut du corps
    └── Fx_Emetteur            vide : où naissent les 4 écailles qui tombent en tournoyant
Effets (code) : battement en courbe 40 % / 60 % ; émission des taches (matériau
M5_TachePapillon) de 0,25 à 0,5 sur chaque battement ; éclat toutes les 2,5 s ; ≤ 4 écailles.
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "ailes_papillon"
POINT = "PointDos"
MATS = ["M5_AilePapillon", "M5_BordPapillon", "M5_TachePapillon"]  # 0, 1, 2
X_CH, Y_CH = 0.12, 1.02
RECUL = math.radians(14)      # V7 : plus ouvertes vers les côtés (vues de face)
LOBES = [  # centre (u, v), rayons, tache (décalage, rayons)
    # V7 : lobes plus ronds et plus grands, GROSSES taches orange rondes (comme l'icône)
    ((0.36, 0.22), (0.32, 0.27), ((0.07, 0.05), (0.11, 0.10))),
    ((0.25, -0.16), (0.21, 0.17), ((0.03, -0.02), (0.075, 0.07))),
]


def contour_festonne(ru, rv, n=24, vagues=8):
    pts = []
    for k in range(n):
        a = 2 * math.pi * k / n
        f = 1.0 - 0.03 * abs(math.sin(vagues * a / 2))      # V7 : bord presque lisse (icône)
        pts.append((ru * f * math.cos(a), rv * f * math.sin(a)))
    return pts


def charniere(arbre, cote):
    depart = C.U(-cote * X_CH, Y_CH, 3.0)
    hit, _, _, _ = arbre.ray_cast(depart, Vector((0, 1, 0)))
    return hit + Vector((0, 0.02, 0))


def repere_aile():
    u = Vector((math.cos(RECUL), -math.sin(RECUL), 0.0))
    z = Vector((0, 0, 1))
    y = z.cross(u).normalized()
    return Matrix((u, y, z)).transposed().to_4x4()


def aile(cote, arbre):
    h = charniere(arbre, cote)
    m = Matrix.Translation(h) @ Matrix.Diagonal((cote, 1, 1, 1)) @ repere_aile()
    bm = bmesh.new()
    for (cu, cv), (ru, rv), ((tu, tv), (tru, trv)) in LOBES:
        loc = Matrix.Translation((cu, 0, cv))
        C.bm_coussin(bm, contour_festonne(ru, rv, n=28), 0.045, 0.08, loc, mat=0, anneaux=2)
        # le cerne violet : une plaque un peu plus grande, 1,5 cm derrière
        C.bm_coussin(bm, contour_festonne(ru + 0.035, rv + 0.035, n=24), 0.04, 0.05,
                     loc @ Matrix.Translation((0, -0.018, 0)), mat=1, anneaux=1)
        C.bm_coussin(bm, C.contour_ellipse(tru, trv, n=20), 0.02, 0.04,
                     Matrix.Translation((cu + tu, 0.045, cv + tv)), mat=2, anneaux=1,
                     dos_plat=True)
    # 3 nervures en relief depuis la racine
    for fin in ():   # V7 : nervures retirées (fines baguettes absentes de l'icône)
        chemin = [Vector((0.05, 0.03, fin[1] * 0.15)), Vector((fin[0] * 0.5, 0.036, fin[1] * 0.5)),
                  Vector((fin[0] * 0.85, 0.032, fin[1] * 0.85))]
        C.bm_tube(bm, chemin, 0.03, cotes=4, mat=1, rayons=[0.9, 0.7, 0.4])
    bmesh.ops.transform(bm, matrix=m, verts=bm.verts)
    if cote < 0:
        bmesh.ops.reverse_faces(bm, faces=bm.faces[:])
    nom = "AileGauche" if cote > 0 else "AileDroite"
    o = C.piece(nom, bm, MATS, lissage="tout", pivot=h)
    return o, m @ Vector((0.40, 0.05, 0.30))


def corps(arbre):
    """Le corps : une capsule violet nuit posée sur la crinière, avec 2 antennes."""
    depart = C.U(0.0, Y_CH - 0.10, 3.0)
    hit, _, _, _ = arbre.ray_cast(depart, Vector((0, 1, 0)))
    base = hit + Vector((0, -0.04, 0))
    bm = bmesh.new()
    prof = [(0.0, 0.0), (0.045, 0.02), (0.06, 0.08), (0.06, 0.17), (0.045, 0.23), (0.0, 0.26)]
    v = C.bm_revolution(bm, prof, 8, mats=[1] * 5)
    bmesh.ops.translate(bm, vec=base, verts=[x for r in v for x in r])
    corps_o = C.piece("Corps", bm, MATS, lissage="tout", pivot=base)
    sommet = base + Vector((0, 0, 0.24))
    antennes = []
    for cote, nom in ((1, "Anim_AntenneGauche"), (-1, "Anim_AntenneDroite")):
        bm = bmesh.new()
        chemin = [sommet, sommet + Vector((cote * 0.05, -0.02, 0.10)),
                  sommet + Vector((cote * 0.12, -0.03, 0.17)),
                  sommet + Vector((cote * 0.19, -0.02, 0.20))]
        C.bm_tube(bm, chemin, 0.015, cotes=5, mat=1)
        C.bm_sphere(bm, 0.035, chemin[-1], segments=8, anneaux=5, mat=1)
        antennes.append(C.piece(nom, bm, MATS, lissage="tout", pivot=sommet))
    for a in antennes:
        C.parenter(a, corps_o)
    return corps_o, sommet + Vector((0, 0, 0.02))


def construire():
    C.nouvelle_scene()
    arbre = C.arbre_cheval()
    gauche, eg = aile(+1, arbre)
    droite, ed = aile(-1, arbre)
    corps_o, haut = corps(arbre)
    fx = (gauche.location + droite.location) / 2 + Vector((0, -0.05, 0.15))
    e1, e2, e3, emetteur = C.reperes_effets([eg, ed, haut], fx=fx)
    C.parenter(e1, gauche)
    C.parenter(e2, droite)
    C.parenter(e3, corps_o)
    return C.racine_v5(NOM, POINT, [gauche, droite, corps_o, emetteur])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT, y_min=0.45)
