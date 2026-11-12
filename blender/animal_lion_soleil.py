"""
animal_lion_soleil.py — Extras 3D du skin Lion solaire (boîte « Animaux : skins » :
`animal_lion_soleil`, SECRET) : la CRINIÈRE DE FLAMMES et le DISQUE SOLAIRE.
docs/specs/v5-catalogue.md §3.2 et §0.4, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_lion_soleil.py
Sortie : blender/exports/animal_lion_soleil_extras.fbx et
docs/captures/v5_cheval_animal_lion_soleil_extras.png.

Prefab `skinExtrasPrefab` du skin (catalogue §0.4) : enfant direct de `Joueur` (position et
rotation locales nulles, échelle 1), sans collider, retiré quand on change de skin. Le corps
or → orange et la chaleur qui ondule sont une matière du code, pas ce fichier.

Ce qu'il y a :
  - 14 mèches de flamme (langues de feu renflées et aplaties, 0,28 à 0,46 m, dont le bout
    fait un S) en 2 couronnes :
      • 8 autour de la tête, sur le pourtour du visage vu de face (de la joue gauche à la
        joue droite par le dessus), posées EN ARRIÈRE du plan des yeux : elles encadrent le
        visage sans jamais le cacher ;
      • 6 autour du haut du cou (côtés et arrière), qui partent vers l'extérieur et le haut ;
    chaque mèche est jaune pâle à la base (émission 1,2) et orange au bout (0,8), et son
    bout s'enroule un peu (sens alterné) : un feu « vivant » même immobile ;
  - le disque solaire : 12 rayons pointus autour d'un anneau (Ø 1,6), derrière la tête,
    jaune pâle transparent (opacité 0,25, émission 0,5), toujours derrière le cheval.
3 matières (budget des extras) : M5_FlammeBase, M5_FlammeOrange, M5_DisqueSolaire.

Pour le programmeur :
  - Anim_Flamme_NN : rotation locale = l'orientation de la mèche ; son axe Y LOCAL (Unity)
    est la longueur : la « danse » = échelle Y 0,9 ↔ 1,15 + rotation ±12° autour de Y,
    période propre 0,6 à 1,1 s (une par mèche). Saut de joie : ×1,3 et émission 1,3 pendant
    0,5 s ; tête basse : ×0,7 et émission 0,4 pendant 1 s.
  - Anim_DisqueSolaire : tourne autour de son axe Z local (Unity, l'axe avant-arrière) à
    10°/s ; un tour rapide au saut de joie.
  - Fx_Emetteur : en haut de la crinière ; les escarbilles (réserve de 30, une toutes les
    0,12 s) peuvent aussi naître au bout d'une mèche au hasard. Particules ≤ 60 au total.
  - Eclat_1..3 : sur 3 mèches (Secret : 1 éclat / s).

Hiérarchie :
    animal_lion_soleil_extras   vide, origine = la base du cheval (enfant de Joueur)
    ├── Criniere                vide au centre de la tête
    │   └── Anim_Flamme_01..14  une mèche ; origine = sa base
    ├── Anim_DisqueSolaire      le disque ; origine = son centre (y 1,62, z 0,48)
    ├── Fx_Emetteur
    └── Eclat_1..3
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "animal_lion_soleil_extras"
MATS = ["M5_FlammeBase", "M5_FlammeOrange", "M5_DisqueSolaire"]  # 0..2
TETE = (0.0, 1.62, 0.08)            # Unity : centre de la couronne de la tête
DISQUE = (0.0, 1.62, 0.48)


PAS = (0.0, 0.15, 0.35, 0.55, 0.75, 0.9)


def langue(bm, longueur, rayon, sens, segments):
    """Une langue de feu : goutte renflée, aplatie, dont le bout fait un S."""
    av = set(bm.verts)
    A.bm_goutte(bm, longueur, rayon, segments=segments, bandes=((0.42, 0), (1.0, 1)),
                aplati=0.6, courbe=0.6, pas=PAS)
    for v in bm.verts:
        if v in av:
            continue
        t = max(0.0, v.co.z / longueur)
        v.co.x += sens * longueur * (0.24 * t * t - 0.09 * math.sin(math.pi * t))
    return [v for v in bm.verts if v not in av]


def meche(nom, base, direction, longueur, sens, double=True):
    """Une mèche : une grande langue de feu (+ une petite sur le côté, qui s'enroule dans
    l'autre sens, pour les mèches de la tête)."""
    bm = bmesh.new()
    langue(bm, longueur, longueur * 0.42, sens, 6)
    if double:
        v = langue(bm, longueur * 0.55, longueur * 0.3, -sens, 5)
        bmesh.ops.transform(bm, matrix=Matrix.Translation((-sens * longueur * 0.16, -0.01, 0.0))
                            @ Matrix.Rotation(math.radians(sens * 28), 4, "Y"), verts=v)
    o = C.objet_depuis_bmesh(nom, bm, MATS)
    C.lisser_tout(o)
    o.matrix_world = A.matrice_vers(base, direction, Vector((0, -1, 0)))
    bpy.context.view_layer.update()
    return o


def couronne_tete():
    """8 mèches sur le pourtour de la tête vue de face, en arrière du plan des yeux."""
    c = C.U(*TETE)
    res = []
    angles = [-112, -80, -50, -18, 18, 50, 80, 112]           # 0 = le haut, + = gauche écran
    longueurs = [0.30, 0.38, 0.44, 0.46, 0.46, 0.44, 0.38, 0.30]
    for k, (psi, L) in enumerate(zip(angles, longueurs)):
        p = math.radians(psi)
        radial = Vector((math.sin(p), 0.0, math.cos(p)))       # dans le plan de face (Blender)
        dirn = (radial + Vector((0, -0.45, 0))).normalized()   # et vers l'arrière
        # base : sur la surface, en lançant un rayon depuis l'extérieur vers le centre
        hit, _, _, _ = A.arbre().ray_cast(c + dirn * 2.0, -dirn)
        base = (hit if hit is not None else c) + dirn * 0.01
        res.append(meche(f"Anim_Flamme_{k + 1:02d}", base, (radial * 1.0 + Vector((0, -0.3, 0.15))),
                         L, 1 if k % 2 else -1))
    return res


def couronne_cou():
    """6 mèches autour du haut du cou (côtés et arrière), vers l'extérieur et le haut."""
    res = []
    for k, (az, y, L) in enumerate(((112, 1.30, 0.32), (150, 1.22, 0.30), (172, 1.40, 0.28),
                                    (-112, 1.30, 0.32), (-150, 1.22, 0.30), (-172, 1.40, 0.28))):
        a = math.radians(az)
        p, d = A.surface((0.0, y, 0.02), a, 0.0, -0.04, fenetre=(0.2, 0.0), pas=0.1)
        dirn = (d + Vector((0, 0, 0.75))).normalized()
        res.append(meche(f"Anim_Flamme_{k + 9:02d}", p, dirn, L, 1 if k < 3 else -1,
                         double=False))
    return res


def disque():
    c = C.U(*DISQUE)
    bm = bmesh.new()
    m = C.repere(c, Vector((0, 1, 0)))           # plaque face caméra
    # anneau (couronne plate) entre 0,30 et 0,42
    n = 24
    ext = [(0.42 * math.cos(2 * math.pi * k / n), 0.42 * math.sin(2 * math.pi * k / n)) for k in range(n)]
    inn = [(0.30 * math.cos(2 * math.pi * k / n), 0.30 * math.sin(2 * math.pi * k / n)) for k in range(n)]
    av = set(bm.verts)
    rangs = C.bm_anneaux(bm, [[Vector((x, -0.01, z)) for x, z in ext],
                              [Vector((x, -0.01, z)) for x, z in inn],
                              [Vector((x, 0.01, z)) for x, z in inn],
                              [Vector((x, 0.01, z)) for x, z in ext]], mats=[2, 2, 2])
    r0, r3 = rangs[0], rangs[3]
    for s in range(n):                            # referme la tranche extérieure
        t = (s + 1) % n
        f = bm.faces.new((r3[s], r0[s], r0[t], r3[t]))
        f.material_index = 2
    nv = [v for v in bm.verts if v not in av]
    bmesh.ops.transform(bm, matrix=m, verts=nv)
    # 12 rayons pointus, longs et courts en alternance (Ø 1,6 au bout des longs)
    for k in range(12):
        a = 2 * math.pi * k / 12
        L = 0.80 if k % 2 == 0 else 0.66
        w = 0.12
        tri = [(0.40, -w), (L, 0.0), (0.40, w)]
        cont = [(r * math.cos(a) - q * math.sin(a), r * math.sin(a) + q * math.cos(a)) for r, q in tri]
        C.bm_plaque(bm, cont, 0.02, m, mat=2)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return C.piece("Anim_DisqueSolaire", bm, MATS, lissage="angle", pivot=c)


def construire():
    C.nouvelle_scene()
    crin = C.vide("Criniere", C.U(*TETE))
    flammes = couronne_tete() + couronne_cou()
    for f in flammes:
        C.parenter(f, crin)
    d = disque()
    haut = flammes[3].matrix_world @ Vector((0, 0, 0.46))
    eff = C.reperes_effets([flammes[i].matrix_world @ Vector((0, 0, 0.3)) for i in (2, 4, 6)],
                           fx=haut)
    return A.racine_joueur(NOM, [crin, d] + eff)


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, "Joueur", y_min=0.3)
