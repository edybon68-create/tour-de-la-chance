"""
ailes_ange.py — Petites ailes d'ange (boîte d'accessoires n°1 : `ailes_ange`, Rare, Dos).
VERSION v5 « plus jeu moderne » (docs/art-bible.md §13.8 fiche 12), faites SUR le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/ailes_ange.py
Sortie : blender/exports/ailes_ange.fbx et docs/captures/v5_cheval_ailes_ange.png.

Défaut corrigé : les anciennes ailes étaient faites pour la capsule (charnières à ±0,32 sur
un corps rond) : sur le cheval, leurs racines flottaient à côté du cou. Les charnières sont
maintenant SUR LE DOS, de part et d'autre de la crinière (x = ±0,13, y = 1,05), posées sur la
peau mesurée par un rayon ; la racine de chaque aile rentre de 2 cm dans le dos.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : deux ailes de 0,70 × 0,48 (× 1,4), qui partent vers l'extérieur et un peu
                 vers l'arrière (30°) : elles dépassent de 0,45 m de chaque côté du cou ;
  - secondaire : 3 rangées de plumes en couches décalées de 2 cm : 5 grandes plumes rondes
                 (en bas, ombre #D9D2E4), 4 moyennes et 3 petites (blanc cassé) ;
  - tertiaire  : une broche dorée ronde (Ø 10 cm) à la racine de chaque aile.
Matières : plumes mates (0,30), broche dorée.

Hiérarchie (le code Unity retrouve les pièces par leur nom ; noms historiques gardés : le
script AccessoryMotion fait déjà battre AileGauche / AileDroite autour de leur axe Y local) :
    ailes_ange                vide, origine = PointDos (attachScale 1, attachOffset 0)
    ├── AileGauche            rangée du haut + broche ; origine = la charnière sur le dos
    │   ├── Anim_RangMilieuGauche  rangée du milieu ; même origine (charnière)
    │   ├── Anim_RangBasGauche     rangée du bas ; même origine — chaque rangée suit la
    │   │                          précédente avec 0,08 s de retard (l'aile « ondule ») ;
    │   │                          ressort sur la rangée du bas
    │   └── Eclat_1           vide : point d'éclat sur la broche
    ├── AileDroite            (symétrique) + Anim_RangMilieuDroite, Anim_RangBasDroite, Eclat_2
    └── Eclat_3               vide : point d'éclat en haut de l'aile gauche (dans AileGauche)
Battement (code, inchangé) : ±8° en 1,6 s, en miroir.
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "ailes_ange"
POINT = "PointDos"
MATS = ["M_Plume", "M_PlumeOmbre", "M5_DoreDeco"]  # 0, 1, 2
X_CH, Y_CH = 0.13, 1.05
RECUL = math.radians(30)      # l'aile part vers l'extérieur ET vers l'arrière
LEVE = math.radians(8)        # et se relève un peu


def arc_haut(u):
    """Bord du haut de l'aile (v en fonction de u) : monte de la racine vers la pointe."""
    return 0.08 + 0.34 * math.sin(min(u / 0.72, 1.0) * math.pi / 2)


def contour_rang(u_max, v_bas, lobes):
    """Contour d'une rangée : le bord haut suit l'arc de l'aile, le bord bas fait `lobes`
    plumes rondes. u = vers l'extérieur, v = vers le haut. Sens inverse des aiguilles."""
    pts = []
    # bas : de la racine vers la pointe, en festons ronds
    for k in range(lobes):
        u0, u1 = u_max * k / lobes, u_max * (k + 1) / lobes
        vb = v_bas(0.5 * (u0 + u1))
        for f in (0.0, 0.2, 0.5, 0.8):
            u = u0 + (u1 - u0) * f
            bombe = 0.065 * math.sin(math.pi * f) ** 0.6     # plume ronde
            pts.append((u, vb - bombe + 0.03))
    # bout de l'aile arrondi, qui remonte vers le haut
    for f in (0.35, 0.7):
        pts.append((u_max + 0.03 * math.sin(math.pi * f),
                    v_bas(u_max) + 0.03 + (arc_haut(u_max) - v_bas(u_max) - 0.05) * f))
    # haut : de la pointe vers la racine
    for k in range(6, -1, -1):
        u = u_max * k / 6
        pts.append((u, arc_haut(u) - (0.03 if k == 6 else 0.0)))
    # recentrer sur la racine (u = 0, v = 0 inchangés) : le contour doit tourner autour
    # d'un point intérieur pour bm_coussin, on renvoie aussi ce centre
    cu = sum(p[0] for p in pts) / len(pts)
    cv = sum(p[1] for p in pts) / len(pts)
    return [(u - cu, v - cv) for u, v in pts], (cu, cv)


RANGS = [  # nom, u_max, bas(u), lobes, matériau, avance (couches de 2 cm)
    ("Bas", 0.70, lambda u: -0.08 - 0.04 * (u / 0.7), 5, 1, 0.00),
    ("Milieu", 0.56, lambda u: 0.06 + 0.02 * (u / 0.56), 4, 0, 0.022),
    ("Haut", 0.40, lambda u: 0.17 + 0.03 * (u / 0.40), 3, 0, 0.044),
]


def charniere(arbre, cote):
    """Point de la charnière sur le dos (la peau, mesurée par un rayon venu de derrière)."""
    depart = C.U(-cote * X_CH, Y_CH, 3.0)      # cote +1 = +X Blender = Unity x négatif
    hit, _, _, _ = arbre.ray_cast(depart, Vector((0, 1, 0)))
    return hit + Vector((0, 0.02, 0))        # 2 cm DANS le dos : pas de jour à la racine


def repere_aile():
    """Repère de l'aile GAUCHE (+X Blender) : X = vers l'extérieur et l'arrière, Z = haut,
    Y = la face avant de l'aile."""
    u = Vector((math.cos(RECUL), -math.sin(RECUL), 0.0))
    rot = Matrix.Rotation(LEVE, 4, u.cross(Vector((0, 0, 1))).normalized())
    u = (rot.to_3x3() @ u).normalized()
    z = Vector((0, 0, 1))
    y = z.cross(u).normalized()
    z = u.cross(y)
    return Matrix((u, y, z)).transposed().to_4x4()


def aile(cote, arbre):
    suffixe = "Gauche" if cote > 0 else "Droite"
    h = charniere(arbre, cote)
    rep = repere_aile()
    miroir = Matrix.Diagonal((cote, 1, 1, 1))
    pieces = {}
    for nom, u_max, bas, lobes, mat, avance in RANGS:
        bm = bmesh.new()
        contour, (cu, cv) = contour_rang(u_max, bas, lobes)
        local = Matrix.Translation((cu, avance, cv))
        C.bm_coussin(bm, contour, 0.05, 0.075, local, mat=mat, anneaux=2)
        if nom == "Haut":
            # broche dorée bombée à la racine, devant la rangée du haut
            C.bm_disque(bm, 0.05, 0.03, Matrix.Translation((0.07, 0.08, 0.14)), cotes=10,
                        mat=2, bombe=0.02)
        m = Matrix.Translation(h) @ miroir @ rep
        bmesh.ops.transform(bm, matrix=m, verts=bm.verts)
        if cote < 0:
            bmesh.ops.reverse_faces(bm, faces=bm.faces[:])
        nom_obj = f"Aile{suffixe}" if nom == "Haut" else f"Anim_Rang{nom}{suffixe}"
        pieces[nom] = C.piece(nom_obj, bm, MATS, lissage="tout", pivot=h)
    racine = pieces["Haut"]
    for nom in ("Milieu", "Bas"):
        C.parenter(pieces[nom], racine)
    m = Matrix.Translation(h) @ miroir @ rep
    eclats = [m @ Vector((0.07, 0.11, 0.14))]
    if cote > 0:
        eclats.append(m @ Vector((0.38, 0.06, arc_haut(0.38))))
    return racine, eclats


def construire():
    C.nouvelle_scene()
    arbre = C.arbre_cheval()
    gauche, eg = aile(+1, arbre)
    droite, ed = aile(-1, arbre)
    e1, e2, e3 = C.reperes_effets([eg[0], ed[0], eg[1]])
    C.parenter(e1, gauche)
    C.parenter(e3, gauche)
    C.parenter(e2, droite)
    return C.racine_v5(NOM, POINT, [gauche, droite])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT, y_min=0.45)
