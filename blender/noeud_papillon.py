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
    for k in range(1, 6):                     # bas : s'évase (V7 : 5 points, plus rond)
        t = k / 6
        pts.append((L_AILE * t, -H_PINCE / 2 - (H_AILE - H_PINCE) / 2 * math.sin(t * math.pi / 2)))
    # bout : arrondi avec 2 encoches
    # V7 : bout simplement ARRONDI (l'icône n'a pas d'encoches), 11 points
    bout = [(d, 1.0) for d in range(-85, 86, 17)]
    for deg, r in bout:
        a = math.radians(deg)
        pts.append((L_AILE * 0.80 + 0.06 * r * math.cos(a) * 1.5, H_AILE / 2 * r * math.sin(a)))
    for k in range(5, 0, -1):                 # haut
        t = k / 6
        pts.append((L_AILE * t, H_PINCE / 2 + (H_AILE - H_PINCE) / 2 * math.sin(t * math.pi / 2)))
    pts.append((0.0, H_PINCE / 2))
    cx = L_AILE / 2
    return [(x - cx, z) for x, z in pts]


def lisse_aile(t):
    return t * t * (3 - 2 * t)


def aile(nom, cote, origine, n):
    # V7 (retour 2) : chaque aile est une BOUCLE GONFLÉE lisse (un ballon ovale), pincée vers
    # le nœud, comme sur l'icône. Plus de coussin plat à plis (il paraissait froissé).
    bm = bmesh.new()
    v = C.bm_sphere(bm, 1.0, (0, 0, 0), segments=20, anneaux=14,
                    echelle=(L_AILE / 2, 0.075, H_AILE / 2), mat=0)
    for v_ in v:
        t = (cote * v_.co.x + L_AILE / 2) / L_AILE          # 0 côté nœud, 1 au bout
        k = 0.42 + 0.58 * lisse_aile(min(1.0, max(0.0, t * 1.6)))
        v_.co.z *= k                                        # pincée en hauteur près du nœud
        v_.co.y *= 0.6 + 0.4 * k                            # et un peu moins épaisse
        v_.co.z *= 1.0 + 0.12 * math.sin(math.pi * t)       # bout bien rond et plein
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
    # V7 : le nœud central est un seul « coussin » rond et bombé, rouge plus foncé (deux
    # tons), comme sur l'icône (au lieu de 3 boudins).
    bm = bmesh.new()
    v = C.bm_sphere(bm, 1.0, (0, 0, 0), segments=16, anneaux=10, echelle=(0.07, 0.07, 0.08), mat=1)
    rot = n.to_track_quat("Y", "Z").to_matrix().to_4x4()
    bmesh.ops.transform(bm, matrix=Matrix.Translation(centre + n * 0.02) @ rot, verts=v)
    noeud = C.piece("Noeud", bm, MATS, lissage="tout", pivot=centre)
    for nom, cote in (("Anim_AileGauche", +1), ("Anim_AileDroite", -1)):
        C.parenter(aile(nom, cote, centre, n), noeud)
    return C.racine_v5(NOM, POINT, [noeud])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT, y_min=0.55)
