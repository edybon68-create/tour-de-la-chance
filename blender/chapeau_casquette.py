"""
chapeau_casquette.py — Casquette (boîte d'accessoires n°1 : `chapeau_casquette`, Commun, Tête).
VERSION v5 « plus jeu moderne » (docs/art-bible.md §13.8 fiche 1), faite SUR le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/chapeau_casquette.py
Sortie : blender/exports/chapeau_casquette.fbx et docs/captures/v5_cheval_chapeau_casquette.png.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : la calotte bombée en 6 panneaux (rainures de 2 cm entre eux) ;
  - secondaire : la visière longue (0,55), courbée et penchée de 12° vers le bas, portée
                 « de travers » (62° vers la droite de l'image) : elle dépasse de la
                 silhouette, donc on la voit de face, sans jamais cacher les yeux ; le
                 bourrelet blanc cassé tout autour de la base ;
  - tertiaire  : le bouton bombé Ø 0,14, 2 œillets, une couture blanche sur le bord de la
                 visière, une petite étoile « brodée » sur le panneau avant (pas de marque).
Matières : calotte mate (0,20), visière satinée (0,50), bouton / bourrelet vernis (0,85).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    chapeau_casquette   vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Anim_Casquette  toute la casquette ; origine = centre de sa base (le « ressort » :
                        petit sursaut de 0,03 m à l'atterrissage, bible §13.5 type A)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "chapeau_casquette"
POINT = "PointChapeau"
MATS = ["M5_CasquetteRouge", "M5_CasquetteVisiere", "M5_BlancVerni"]  # 0, 1, 2
BX, BY, BZ = C.CHAPEAU_BASE[0], C.CHAPEAU_BASE[1] + 0.04, C.CHAPEAU_BASE[2]
R0 = C.CHAPEAU_RAYON + 0.03     # rayon de la base de la calotte (elle coiffe la tête)
HAUT = 0.34                     # hauteur de la calotte
SEG = 24                        # 6 panneaux × 4 (V7 : plus rond)


ANGLE_VISIERE = 72.0            # degrés : visière tournée vers la droite de l'image
# V7 : 62° et penchée vers le BAS, la racine de la visière couvrait le haut de l'œil droit
# vu du jeu. Plus tournée et relevée de 6° (comme une casquette portée « cool »), elle
# dégage les deux yeux.


def base_blender():
    return C.U(BX, BY, BZ)


def calotte(bm):
    """Dôme « poire » : il gonfle un peu au-dessus de la base, puis s'arrondit."""
    profil = [(0.0, HAUT), (0.17, HAUT - 0.025), (0.29, HAUT - 0.10), (R0 + 0.025, 0.12),
              (R0 + 0.01, 0.03), (R0 - 0.03, -0.02)]

    def rainure(a, i, r, z):
        # 6 rainures : sur 1 méridien sur 3, la calotte rentre de 2 cm (sauf sommet et bord).
        k = round(a / (2 * math.pi / SEG)) % 4
        if k == 0 and 0 < i < len(profil) - 2:
            return r - 0.012, z      # V7 : rainures plus discrètes (1,2 cm)
        return r, z

    v_avant = set(bm.verts)
    C.bm_revolution(bm, profil, SEG, mats=[0] * (len(profil) - 1), rayon_fn=rainure,
                    fermer_bas=True)
    v = [x for x in bm.verts if x not in v_avant]
    bmesh.ops.translate(bm, vec=base_blender(), verts=v)


def bourrelet(bm):
    # V7 : bourrelet ROUGE foncé (l'icône n'a pas de bande blanche), plus fin
    v = C.bm_tore(bm, R0 + 0.006, 0.030, segments=SEG, segments_tube=6, mat=1)
    bmesh.ops.translate(bm, vec=base_blender() + Vector((0, 0, 0.012)), verts=v)


def visiere(bm, bm_couture):
    """Visière : une plaque courbe (bords 5 cm plus bas que le centre), longue de 0,55,
    penchée de 12° vers le bas. Construite en grille (u = travers, v = longueur) dans un
    repère local (base de la calotte à l'origine, avant = +Y), puis tournée de
    ANGLE_VISIERE vers le côté : portée « de travers », elle dépasse nettement de la
    silhouette (on la voit de face) et ne passe JAMAIS devant les yeux. Une visière droit
    devant, à cette hauteur, cacherait les deux yeux depuis la caméra du jeu (qui est
    au-dessus) : nous l'avons vérifié en rendu."""
    nu, nv = 10, 5                # V7 : grille plus fine (courbe douce)
    larg, long_ = 0.54, 0.40      # V7 : visière plus COURTE et ronde, comme l'icône
    epais = 0.04
    inc = math.radians(-6)        # V7 : relevée (avant : 12° vers le bas)
    hs, bs = [], []
    for j in range(nv + 1):
        t = j / nv
        demi = max((larg / 2) * math.sqrt(max(1 - (t * 0.93) ** 2, 0.0)), 0.07)
        rh, rb = [], []
        for i in range(nu + 1):
            s = -1 + 2 * i / nu
            x = s * demi
            y0 = math.sqrt(max((R0 - 0.02) ** 2 - x ** 2, 0.0))     # sur le bord de la calotte
            y = y0 + long_ * t * math.cos(inc)
            z = 0.03 - long_ * t * math.sin(inc) - 0.07 * s * s * (0.3 + 0.7 * t)   # bien bombée
            rh.append(bm.verts.new((x, y, z + epais / 2)))
            rb.append(bm.verts.new((x, y, z - epais / 2)))
        hs.append(rh)
        bs.append(rb)
    faces = []
    for j in range(nv):
        for i in range(nu):
            faces.append(bm.faces.new((hs[j][i], hs[j][i + 1], hs[j + 1][i + 1], hs[j + 1][i])))
            faces.append(bm.faces.new((bs[j][i], bs[j + 1][i], bs[j + 1][i + 1], bs[j][i + 1])))
    tour = lambda g: ([g[j][0] for j in range(nv + 1)] + g[nv][1:] +
                      [g[j][nu] for j in range(nv - 1, -1, -1)] + g[0][nu - 1:0:-1])
    ch, cb = tour(hs), tour(bs)
    for k in range(len(ch)):
        q = (k + 1) % len(ch)
        faces.append(bm.faces.new((ch[k], cb[k], cb[q], ch[q])))
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    # V7 : dessus de la visière du même rouge que la calotte, dessous et tranche plus foncés
    for f in faces:
        f.material_index = 0 if f.normal.z > 0.5 else 1
    if True:   # V7 : plus de couture blanche (elle faisait « plaque en plastique »)
        bmesh.ops.transform(bm, matrix=Matrix.Translation(base_blender()) @ Matrix.Rotation(
            math.radians(ANGLE_VISIERE), 4, "Z"), verts=bm.verts)
        return
    # Couture blanche en relief (2 cm), 4 cm en retrait du bord, sur le dessus.
    chemin = []
    for j, i in [(1, 0), (2, 0)] + [(nv, i) for i in range(1, nu)] + [(2, nu), (1, nu)]:
        co = hs[j][i].co
        vers_centre = Vector((-co.x, 0, 0)).normalized() if j < nv else Vector((0, -1, 0))
        chemin.append(co + vers_centre * 0.04 + Vector((0, 0, 0.012)))
    v = C.bm_tube(bm_couture, chemin, 0.013, cotes=4, mat=2)
    rot = Matrix.Translation(base_blender()) @ Matrix.Rotation(math.radians(ANGLE_VISIERE), 4, "Z")
    bmesh.ops.transform(bm, matrix=rot, verts=bm.verts)
    bmesh.ops.transform(bm_couture, matrix=rot, verts=v)


def details(bm, arbre):
    """Bouton du sommet, 2 œillets et l'étoile brodée du panneau avant (face à la caméra),
    posés pile sur la calotte par des rayons."""
    b = base_blender()
    C.bm_disque(bm, 0.06, 0.03, Matrix.Translation(b + Vector((0, 0, HAUT + 0.005)))
                @ Matrix.Rotation(math.radians(-90), 4, "X"), cotes=16, mat=2, bombe=0.025)
    centre = b + Vector((0, 0, 0.05))
    for cote in (-1, 1):
        p, n = C.poser_sur(arbre, centre, C.direction_angles(cote * 0.62, math.radians(30)), 0.004)
        C.bm_disque(bm, 0.026, 0.02, C.repere(p, n), cotes=10, mat=1)
    # V7 : plus d'étoile brodée (absente de l'icône : une casquette rouge toute simple)


def construire():
    C.nouvelle_scene()
    bm = bmesh.new()
    calotte(bm)
    arbre = C.arbre_bm(bm)
    bourrelet(bm)
    details(bm, arbre)
    bm_v = bmesh.new()
    visiere(bm_v, bm)
    tissu = C.piece("Calotte", bm, MATS, lissage="tout")
    bm = bm_v
    vis = C.piece("Visiere", bm, MATS, biseau=0.02, segments=2)
    casquette = C.joindre([tissu, vis], "Anim_Casquette")
    C.deplacer_origine(casquette, base_blender())
    return C.racine_v5(NOM, POINT, [casquette])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT)
