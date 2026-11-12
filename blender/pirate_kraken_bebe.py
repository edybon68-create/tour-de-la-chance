"""
pirate_kraken_bebe.py — Petit Kraken (boîte d'accessoires Pirates : `kraken_bebe`, SECRET,
Tête). Fiche complète : docs/specs/v5-catalogue.md §2.1 ; règles : art-bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/pirate_kraken_bebe.py
Sortie : blender/exports/kraken_bebe.fbx et docs/captures/v5_cheval_kraken_bebe.png.

Un bébé kraken violet ASSIS sur le crâne du cheval, entre les oreilles, qui lui fait un
câlin : deux tentacules s'ENROULENT autour des oreilles (une spirale et demie), deux
pendent sur le front (de part et d'autre du toupet, à 12 cm des yeux : ils ne les cachent
jamais), et deux brandissent au-dessus de sa tête une PIÈCE D'OR comme un trophée.
Le front du cheval est mesuré par rayons : les tentacules du front se posent sur la peau.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : la grosse tête-manteau ronde (Ø 0,38 × 0,36 de haut) et ses 6 tentacules ;
  - secondaire : les GROS yeux brillants (Ø 14 cm, deux reflets), la pièce d'or (Ø 0,20) ;
  - tertiaire  : joues roses, petit sourire, 4 taches lumineuses cyan sur le front,
                 ventouses roses sous les tentacules libres, étoile en relief sur la pièce.
Matières (5, budget Secret de la bible §13.7) : violet verni (smoothness 0,70), cyan
lumineux (émission 0,9 : taches ET reflets des yeux), noir verni (yeux, sourire), rose
(joues, ventouses), or de la pièce (émission 0,6).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    kraken_bebe              vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Kraken               les 2 tentacules enroulés aux oreilles (fixes) ;
        │                    origine = sous le corps, entre les oreilles
        ├── Anim_Corps       manteau, joues, sourire, taches ; origine = bas du corps :
        │   │                respiration (échelle 1 ↔ 1,04, 2 s) ; taches émission 0,9
        │   ├── Anim_OeilGauche  yeux ; origine = centre de l'œil : clignent toutes les
        │   └── Anim_OeilDroit   3 à 5 s (échelle Y 1 -> 0,1 -> 1)
        ├── Anim_TentaculeAvantGauche  tentacules du front ; origine = leur base :
        ├── Anim_TentaculeAvantDroit   ondulent (période 1,8 s, décalés) ; tête basse :
        │                              ils remontent cacher les yeux DU KRAKEN (1 s)
        ├── Anim_BrasGauche  bras qui tiennent la pièce ; origine = leur base sur le corps
        ├── Anim_BrasDroit
        ├── Anim_Piece       la pièce ; origine = son centre (lancée et rattrapée au saut
        │   │                de joie, 0,6 s)
        │   └── Eclat_1..3   vides : sur la pièce et les taches (1 éclat / s, arc-en-ciel)
        └── Fx_Emetteur      vide : au-dessus de la tête (nuage d'encre en cœur, ≤ 16)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_pirates as P  # noqa: E402

NOM = "kraken_bebe"
POINT = "PointChapeau"
MATS = ["M5_PirKrakenViolet", "M5_PirKrakenLueur", "M5_PirOeilNoir", "M5_PirJoue",
        "M5_PirPiece"]   # 0..4
CORPS = (0.0, 2.24, 0.07)          # centre du manteau (Unity)
DEMI = (0.19, 0.17, 0.18)         # demi-tailles (x, profondeur, hauteur) en Blender x, y, z
BAS = (0.0, 2.05, 0.07)            # bas du corps (Unity) : origine de Kraken et Anim_Corps
OREILLE = 0.135                    # oreilles en Unity x = ±0,135, z = 0,075


def devant_tete(x_u, y_u):
    """Point de la peau du front vu de devant (rayon horizontal vers l'arrière)."""
    hit, n, _, _ = P.arbre().ray_cast(C.U(x_u, y_u, -2.0), Vector((0, -1, 0)))
    return (hit, n) if hit is not None else (C.U(x_u, y_u, -0.2), Vector((0, 1, 0)))


def ventouses(bm, chemin, cote_v, n=3, r=0.022):
    """Petites ventouses roses le long d'un tentacule (sur la face `cote_v`)."""
    for k in range(1, n + 1):
        i = int(k * (len(chemin) - 1) / (n + 1))
        t = (chemin[i + 1] - chemin[i - 1]).normalized()
        nrm = (cote_v - t * cote_v.dot(t)).normalized()
        C.bm_disque(bm, r * (1.0 - 0.15 * k), 0.012, C.repere(chemin[i] + nrm * 0.025, nrm),
                    cotes=6, mat=3)


def tentacule(bm, chemin, r0, mat=0):
    rayons = [max(0.35, 1.0 - 0.7 * k / (len(chemin) - 1)) for k in range(len(chemin))]
    C.bm_tube(bm, chemin, r0, cotes=6, mat=mat, rayons=rayons)


def spirale_oreille(s):
    """Tentacule qui part du bas du corps et s'enroule 1,5 fois autour de l'oreille."""
    cx, cz = s * OREILLE, 0.075
    pts = [C.U(s * 0.08, 2.08, 0.07), C.U(s * 0.11, 2.03, 0.07)]
    for k in range(13):
        t = k / 12
        a = math.pi / 2 + s * (2 * math.pi * 1.5 * t)
        rr = 0.075 - 0.01 * t
        pts.append(C.U(cx + s * rr * math.cos(a) * 0.9, 1.99 - 0.10 * t, cz + rr * math.sin(a)))
    return pts


def construire():
    C.nouvelle_scene()
    bas = C.U(*BAS)
    centre = C.U(*CORPS)

    # --- Kraken : les tentacules des oreilles (fixes)
    bm = bmesh.new()
    for s in (-1, 1):
        tentacule(bm, spirale_oreille(s), 0.05)
    kraken = C.piece("Kraken", bm, MATS, lissage="tout", pivot=bas)

    # --- Anim_Corps : le manteau (une goutte ronde), joues, bouche, taches lumineuses
    bm = bmesh.new()
    P.bm_ellipsoide(bm, centre, DEMI, segments=14, anneaux=9, mat=0)
    for v in bm.verts:                          # bas un peu aplati, haut un peu pointu
        dz = v.co.z - centre.z
        if dz < 0:
            v.co.z = centre.z + dz * 0.8
        else:
            v.co.z = centre.z + dz * (1 + 0.15 * (dz / DEMI[2]) ** 2)
    arbre = C.arbre_bm(bm)
    for s in (-1, 1):                           # joues roses
        p, n = C.poser_sur(arbre, centre, Vector((s * 0.85, 0.75, -0.45)), 0.0)
        C.bm_coussin(bm, C.contour_ellipse(0.035, 0.025, n=8), 0.01, 0.02, C.repere(p, n),
                     mat=3, anneaux=1, dos_plat=True)
    sourire = []                                                         # petit sourire
    for k in range(5):
        a = math.radians(-35 + 70 * k / 4)
        p, n = C.poser_sur(arbre, centre, Vector((math.sin(a) * 0.45, 1, -0.50 - 0.10 * math.cos(a))), 0.004)
        sourire.append(p)
    C.bm_tube(bm, sourire, 0.011, cotes=4, mat=2)
    eclats = []
    for k, (dx, dz) in enumerate(((0.0, 0.62), (0.32, 0.50), (-0.30, 0.52), (0.14, 0.82))):
        p, n = C.poser_sur(arbre, centre, Vector((dx, 0.75, dz)), 0.0)
        C.bm_coussin(bm, C.contour_ellipse(0.028, 0.028, n=8), 0.01, 0.022, C.repere(p, n),
                     mat=1, anneaux=1, dos_plat=True)
        if k < 2:
            eclats.append(p + n * 0.03)
    corps = C.piece("Anim_Corps", bm, MATS, lissage="tout", pivot=bas)
    C.parenter(corps, kraken)
    for s, nom in ((1, "Anim_OeilGauche"), (-1, "Anim_OeilDroit")):
        p, n = C.poser_sur(arbre, centre, Vector((s * 0.60, 1.0, -0.08)), -0.012)
        bm = bmesh.new()
        P.oeil_mignon(bm, p, n, 0.068, 2, 1)
        oeil = C.piece(nom, bm, MATS, lissage="tout", pivot=p)
        C.parenter(oeil, corps)

    # --- tentacules du front (de part et d'autre du toupet, loin des yeux)
    for s, nom in ((1, "Anim_TentaculeAvantGauche"), (-1, "Anim_TentaculeAvantDroit")):
        b = C.U(s * 0.06, 2.08, -0.04)
        chemin = [b]
        for k, y in enumerate((2.0, 1.94, 1.89, 1.85)):
            p, n = devant_tete(s * (0.065 + 0.012 * k), y)
            chemin.append(p + Vector(n) * 0.035)
        fin = chemin[-1]
        chemin.append(fin + Vector((s * 0.03, 0.045, -0.02)))           # bout recourbé
        chemin.append(fin + Vector((s * 0.06, 0.05, 0.01)))
        bm = bmesh.new()
        tentacule(bm, chemin, 0.05)
        ventouses(bm, chemin, Vector((0, 1, 0)))
        o = C.piece(nom, bm, MATS, lissage="tout", pivot=b)
        C.parenter(o, kraken)

    # --- la pièce, brandie au-dessus de la tête, et les deux bras qui la tiennent
    c_piece = C.U(0.0, 2.62, 0.05)
    bm = bmesh.new()
    m_p = C.repere(c_piece, Vector((0, 1, 0)))
    prof = [(0.0, 0.016), (0.085, 0.016), (0.10, 0.008), (0.10, -0.008), (0.085, -0.016),
            (0.0, -0.016)]
    av = set(bm.verts)
    C.bm_revolution(bm, prof, 14, mats=[4] * 5)
    v = [x for x in bm.verts if x not in av]
    bmesh.ops.transform(bm, matrix=m_p @ Matrix.Rotation(math.radians(90), 4, "X"), verts=v)
    C.bm_etoile(bm, 0.055, 0.012, m_p @ Matrix.Translation((0, 0.018, 0)), mat=4,
                bombe=0.006)
    C.bm_etoile(bm, 0.055, 0.012, m_p @ Matrix.Translation((0, -0.018, 0)), mat=4)
    piece = C.piece("Anim_Piece", bm, MATS, lissage="angle", pivot=c_piece)
    C.parenter(piece, kraken)
    eclats.append(c_piece + Vector((0.06, 0.03, 0.06)))
    for s, nom in ((1, "Anim_BrasGauche"), (-1, "Anim_BrasDroit")):
        b = C.U(s * 0.13, 2.30, 0.07)
        chemin = [b, C.U(s * 0.21, 2.40, 0.07), C.U(s * 0.20, 2.52, 0.06),
                  C.U(s * 0.14, 2.60, 0.05), C.U(s * 0.10, 2.645, 0.03),
                  C.U(s * 0.085, 2.66, -0.0)]
        bm = bmesh.new()
        tentacule(bm, chemin, 0.052)
        ventouses(bm, chemin, Vector((-s, 0.4, 0)), n=2)
        o = C.piece(nom, bm, MATS, lissage="tout", pivot=b)
        C.parenter(o, kraken)

    for e in C.reperes_effets(eclats, fx=C.U(0.0, 2.50, 0.0)):
        C.parenter(e, piece if e.name.startswith("Eclat") else kraken)
    return C.racine_v5(NOM, POINT, [kraken])


if __name__ == "__main__":
    P.finir_pirate(construire(), NOM, POINT)
