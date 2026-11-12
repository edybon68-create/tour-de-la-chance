"""
noeud_papillon.py — Nœud papillon (boîte d'accessoires n°1 : `noeud_papillon`, Commun, Cou).
VERSION v5 « plus jeu moderne » (docs/art-bible.md §13.8 fiche 5), fait SUR le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/noeud_papillon.py
Sortie : blender/exports/noeud_papillon.fbx et docs/captures/v5_cheval_noeud_papillon.png.

Défaut corrigé : l'ancien nœud flottait ~10 cm devant le cou. Celui-ci est posé SUR le
poitrail : on mesure la surface du cheval par un rayon (point + direction de la surface) et
le dos du nœud la touche. Il est placé un peu sous la gorge (y = 0,94) : plus haut, le
museau le cacherait depuis la caméra du jeu, qui regarde d'au-dessus.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : deux ailes BOMBÉES (forme de coussin, 10 cm d'épaisseur au milieu), pincées
                 vers le centre, 0,30 × 0,26 chacune (× 1,6 par rapport à l'ancien) ;
  - secondaire : 3 plis par aile (rainures qui rayonnent depuis le nœud, et le bord
                 extérieur festonné) ; le nœud central en 3 boudins (2 rainures horizontales) ;
  - tertiaire  : 5 pois blanc cassé vernis par aile (Ø 6 cm, en relief).
Matières : ailes satin 0,55, nœud mat 0,30, pois vernis 0,85.

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    noeud_papillon      vide, origine = PointCou (attachScale 1, attachOffset 0)
    └── Noeud           le nœud central ; origine = son centre
        ├── Anim_AileGauche  aile à gauche de l'image ; origine = centre du nœud (ressort :
        └── Anim_AileDroite  elles pivotent de ±6° au saut, bible §13.5 type A)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "noeud_papillon"
POINT = "PointCou"
MATS = ["M5_SatinRouge", "M5_SatinFonce", "M5_BlancVerni"]  # 0, 1, 2
Y_NOEUD = 0.94
L_AILE, H_AILE, H_PINCE = 0.30, 0.26, 0.11


def repere_poitrail(arbre):
    """Repère posé sur le poitrail à Y_NOEUD : origine sur la peau, Y local = direction qui
    sort de la surface (vers l'avant et un peu vers le haut), Z local ≈ vers le haut."""
    depart = C.U(0.0, Y_NOEUD, -2.0)
    hit, _, _, _ = arbre.ray_cast(depart, Vector((0, -1, 0)))
    # Le poitrail est bombé : sous la gorge, sa surface regarde vers le BAS. Un nœud collé à
    # cette pente regarderait le sol ; on le tourne vers l'avant et un peu vers le haut (vers
    # la caméra du jeu), et on recule son centre pour que le bas des ailes touche la peau.
    n = Vector((0.0, 1.0, 0.3)).normalized()
    return hit, n


def contour_aile():
    """Aile qui part vers +X local : pincée au nœud (11 cm), large au bout (26 cm), bout
    arrondi avec 2 encoches (les plis du tissu). Points autour de (L/2, 0)."""
    pts = []
    pts.append((0.0, -H_PINCE / 2))
    for k in range(1, 4):                     # bas : s'évase
        t = k / 4
        pts.append((L_AILE * t, -H_PINCE / 2 - (H_AILE - H_PINCE) / 2 * math.sin(t * math.pi / 2)))
    # bout : arrondi avec 2 encoches
    bout = [(-80, 1.0), (-50, 0.98), (-25, 0.86), (0, 0.97), (25, 0.86), (50, 0.98), (80, 1.0)]
    for deg, r in bout:
        a = math.radians(deg)
        pts.append((L_AILE * 0.80 + 0.06 * r * math.cos(a) * 1.5, H_AILE / 2 * r * math.sin(a)))
    for k in range(3, 0, -1):                 # haut
        t = k / 4
        pts.append((L_AILE * t, H_PINCE / 2 + (H_AILE - H_PINCE) / 2 * math.sin(t * math.pi / 2)))
    pts.append((0.0, H_PINCE / 2))
    cx = L_AILE / 2
    return [(x - cx, z) for x, z in pts]


def aile(nom, cote, origine, n):
    bm = bmesh.new()
    contour = contour_aile()
    if cote < 0:
        contour = [(-x, z) for x, z in reversed(contour)]
    v = C.bm_coussin(bm, contour, 0.06, 0.11, None, mat=0, anneaux=2, dos_plat=True)
    # 3 plis : on creuse 3 « rayons » du devant (de l'intérieur vers le bout)
    for v_ in v:
        if v_.co.y > 0.035:
            ang = math.atan2(v_.co.z, cote * v_.co.x + L_AILE / 2)
            for cible in (-0.32, 0.0, 0.32):
                if abs(ang - cible) < 0.05 and abs(v_.co.x) > 0.02:
                    v_.co.y -= 0.022
    # pincé vers le nœud : l'aile se resserre et se recule près du centre
    for v_ in v:
        t = (cote * v_.co.x + L_AILE / 2) / L_AILE        # 0 au nœud, 1 au bout
        v_.co.y *= 0.55 + 0.45 * min(1.0, t * 2.2)
    arbre = C.arbre_bm(bm)
    # 5 pois sur le devant
    for px, pz in ((-0.07, 0.05), (0.02, -0.06), (0.05, 0.06), (0.10, -0.01), (-0.04, -0.02)):
        x = cote * px
        p, nn = C.poser_sur(arbre, Vector((x, 0.0, pz)), Vector((0, 1, 0)), 0.0)
        C.bm_disque(bm, 0.03, 0.02, C.repere(p, nn), cotes=6, mat=2, bombe=0.008)
    # placement : l'aile recule de 14° pour suivre le poitrail arrondi, dans le repère
    rot = Matrix.Rotation(math.radians(cote * -14), 4, "Z")
    cote_n = Vector((1, 0, 0))
    haut = n.cross(cote_n).normalized() * -1
    m = (Matrix.Translation(origine) @ Matrix((cote_n, n, haut)).transposed().to_4x4()
         @ rot @ Matrix.Translation((cote * (L_AILE / 2 + 0.035), -0.03, 0)))
    bmesh.ops.transform(bm, matrix=m, verts=bm.verts)
    return C.piece(nom, bm, MATS, lissage="tout", pivot=origine)


def construire():
    C.nouvelle_scene()
    arbre = C.arbre_cheval()
    peau, n = repere_poitrail(arbre)
    centre = peau + n * 0.035
    # Le nœud central : 3 boudins horizontaux empilés (2 rainures)
    bm = bmesh.new()
    haut = Vector((1, 0, 0)).cross(n).normalized()
    for k, dz in enumerate((-0.042, 0.0, 0.042)):
        c = centre + haut * dz + n * (0.008 if k == 1 else 0.0)
        C.bm_tube(bm, [c - Vector((0.06, 0, 0)), c - Vector((0.035, 0, 0)),
                       c + Vector((0.035, 0, 0)), c + Vector((0.06, 0, 0))],
                  0.03, cotes=8, mat=1, rayons=[0.6, 1.0, 1.0, 0.6])
    noeud = C.piece("Noeud", bm, MATS, lissage="tout", pivot=centre)
    for nom, cote in (("Anim_AileGauche", +1), ("Anim_AileDroite", -1)):
        C.parenter(aile(nom, cote, centre, n), noeud)
    return C.racine_v5(NOM, POINT, [noeud])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT, y_min=0.55)
