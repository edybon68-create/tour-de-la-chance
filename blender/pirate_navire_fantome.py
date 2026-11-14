"""
pirate_navire_fantome.py — Navire fantôme (boîte d'accessoires Pirates : `navire_fantome`,
SECRET, Dos). Fiche complète : docs/specs/v5-catalogue.md §2.2 ; règles : art-bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/pirate_navire_fantome.py
Sortie : blender/exports/navire_fantome.fbx et docs/captures/v5_cheval_navire_fantome.png.

Un galion miniature (0,66 m : 0,55 × 1,2) translucide vert d'eau qui NAVIGUE EN ORBITE autour du cheval
sur un anneau de brume (rayon 1,0 m, centré à hauteur d'encolure, y = 1,15, penché de 12° :
haut devant). Devant le cheval, son grand mât culmine à y ≈ 1,85, sous la ligne de regard
caméra -> yeux (y ≈ 2,04 à cette distance) : il ne cache jamais les yeux. Même principe que
la Planète (planete.py) : un vide « Orbite » que le code fait tourner.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : la coque ventrue (proue relevée, château arrière haut) et 3 voiles
                 GONFLÉES au vent, au bas effiloché (dents irrégulières, jamais de crâne) ;
  - secondaire : 2 mâts et leurs vergues, le château arrière, le beaupré, la lanterne
                 jaune à la poupe (émission 1,4) ;
  - tertiaire  : 3 sabords lumineux de chaque côté (d'où sortent les confettis), une
                 flamme (petit fanion) en haut du grand mât, la hune.
L'anneau de brume (un tore translucide très léger) matérialise la route ; la traînée de
20 petits nuages est faite par le code (réserve réutilisée, naissance sur Fx_Emetteur).
Matières (4) : spectre vert d'eau (opacité 0,6, émission 0,7), voile (opacité 0,5,
émission 0,5), lanterne (émission 1,4), brume (opacité 0,3, émission 0,4).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    navire_fantome          vide, origine = PointDos (attachScale 1, attachOffset 0)
    └── Inclinaison         vide FIXE au centre de l'orbite, penché de 12° (devant en haut)
        ├── AnneauBrume     le tore de brume (fixe)
        └── Orbite          vide : LE CODE LE FAIT TOURNER autour de son axe Y local,
            │               +51,4°/s (un tour en 7 s ; positif = le navire avance proue
            │               devant)
            └── Anim_Tangage  vide au centre du navire : tangage ±8° (1,5 s) autour de son
                │             axe Z local (Unity : l'axe proue-poupe) ; tête basse : fort
                └── Navire    coque, mâts, voiles, lanterne ; origine = centre du pont
                    ├── Anim_VoileGrande, Anim_VoileHaute, Anim_VoileMisaine : les voiles ;
                    │       origine = leur vergue (gonflent : échelle Z 1 ↔ 1,08, 2 s)
                    ├── Anim_Flamme   le fanion ; origine = haut du mât (ondule)
                    ├── Fx_Sabord_1..3  vides : devant chaque sabord extérieur (salve de
                    │                   12 confettis dorés au saut de joie, son navire_canon)
                    ├── Fx_Emetteur     vide : à la poupe (traînée de brume, 20 nuages)
                    └── Eclat_1..3      vides : lanterne, proue, haut du mât (1 éclat / s)
"""

import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_pirates as P  # noqa: E402

NOM = "navire_fantome"
POINT = "PointDos"
MATS = ["M5_PirSpectre", "M5_PirVoile", "M5_PirLanterne", "M5_PirBrume"]   # 0..3
CENTRE_ORBITE = (0.0, 1.40, 0.02)   # V7 (retour programmeur) : orbite recentrée autour de la tête
RAYON = 0.68         # V7 : 1,0 -> 0,68, il tourne près de la tête (il sortait du cadre de la fiche)
INCLINAISON = 12.0
ECHELLE = 1.35       # V7 : un peu plus petit, l'orbite est plus serrée


# ---------------------------------------------------------------------------------------
# Le navire, construit à l'origine : proue vers +Y (Blender), pont à z = 0
# ---------------------------------------------------------------------------------------
STATIONS = [  # (y, demi-largeur, hauteur du bord, quille)
    (-0.27, 0.085, 0.14, -0.03), (-0.20, 0.115, 0.12, -0.07), (-0.10, 0.13, 0.06, -0.10),
    (0.02, 0.13, 0.05, -0.10), (0.13, 0.115, 0.06, -0.09), (0.22, 0.075, 0.09, -0.06),
    (0.28, 0.0, 0.13, -0.01)]


def coque(bm):
    rangs = []
    for y, w, h, q in STATIONS:
        w = max(w, 0.012)
        sec = [(-w, h), (-w * 1.04, h * 0.3), (-w * 0.85, q * 0.55), (-w * 0.4, q * 0.95),
               (0.0, q), (w * 0.4, q * 0.95), (w * 0.85, q * 0.55), (w * 1.04, h * 0.3),
               (w, h)]
        rangs.append([bm.verts.new((x, y, z)) for x, z in sec])
    m = len(rangs[0])
    faces = []
    for i in range(len(rangs) - 1):
        A, B = rangs[i], rangs[i + 1]
        for s in range(m - 1):
            faces.append(bm.faces.new((A[s], B[s], B[s + 1], A[s + 1])))
    # pont (à hauteur du bord, un peu en creux) et tableau arrière
    for i in range(len(rangs) - 1):
        A, B = rangs[i], rangs[i + 1]
        faces.append(bm.faces.new((A[-1], B[-1], B[0], A[0])))
    faces.append(bm.faces.new(rangs[0]))
    for f in faces:
        f.material_index = 0
    bmesh.ops.recalc_face_normals(bm, faces=faces)


def voile(nom, centre, larg, haut, gonfle, graine_, mat=1):
    """Voile gonflée (vers l'avant +Y), au bas effiloché ; origine = la vergue (en haut)."""
    rnd = random.Random(graine_)
    bm = bmesh.new()
    nx, nz = 4, 3
    grille_av, grille_ar = [], []
    for j in range(nz + 1):
        t = j / nz                     # 0 en haut, 1 en bas
        ra, rr = [], []
        for i in range(nx + 1):
            u = i / nx - 0.5
            x = u * larg * (1 + 0.15 * t)
            z = -t * haut
            if j == nz:                # bas effiloché : dents irrégulières
                z -= (0.035 if i % 2 else -0.01) + rnd.uniform(-0.01, 0.015)
            y = gonfle * math.sin(math.pi * (u + 0.5)) * math.sin(math.pi * min(1, t * 0.9 + 0.1))
            ra.append(bm.verts.new((x, y + 0.006, z)))
            rr.append(bm.verts.new((x, y - 0.006, z)))
        grille_av.append(ra)
        grille_ar.append(rr)
    faces = []
    for g in (grille_av, grille_ar):
        for j in range(nz):
            for i in range(nx):
                faces.append(bm.faces.new((g[j][i], g[j][i + 1], g[j + 1][i + 1], g[j + 1][i])))
    for f in faces:
        f.material_index = mat
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    bmesh.ops.translate(bm, vec=centre, verts=bm.verts)
    return C.piece(nom, bm, MATS, lissage="tout", pivot=centre)


def navire():
    """Renvoie (objet Navire, liste des points des effets : sabords, poupe, éclats)."""
    bm = bmesh.new()
    coque(bm)
    # V7 : nuage de brume sous la coque, qui traîne un peu derrière (suit le navire)
    for x, y, z, r in ((0.0, 0.10, -0.04, 0.09), (0.06, -0.05, -0.05, 0.08), (-0.06, -0.08, -0.05, 0.08),
                       (0.0, -0.22, -0.04, 0.085), (0.03, -0.36, -0.02, 0.06), (-0.02, -0.46, 0.0, 0.045)):
        C.bm_sphere(bm, r, (x, y, z), segments=10, anneaux=6, echelle=(1.3, 1.0, 0.7), mat=3)
    # château arrière
    C.bm_boite(bm, (0.17, 0.10, 0.07), (0, -0.21, 0.155), mat=0)
    # mâts (grand mât et misaine), vergues, beaupré, hune
    mats_pos = [(-0.03, 0.46), (0.13, 0.36)]
    for y, h in mats_pos:
        C.bm_tube(bm, [Vector((0, y, 0.02)), Vector((0, y, h))], 0.014, cotes=5, mat=0)
    C.bm_tube(bm, [Vector((0, 0.24, 0.10)), Vector((0, 0.38, 0.19))], 0.011, cotes=4, mat=0)
    C.bm_tore(bm, 0.035, 0.012, segments=8, segments_tube=3, mat=0,
              matrice=Matrix.Translation((0, -0.03, 0.36)))
    vergues = [(-0.03, 0.34, 0.30), (-0.03, 0.44, 0.20), (0.13, 0.31, 0.24)]
    for y, z, l in vergues:
        C.bm_tube(bm, [Vector((-l / 2, y, z)), Vector((l / 2, y, z))], 0.009, cotes=4, mat=0)
    # lanterne à la poupe (sur une potence)
    C.bm_tube(bm, [Vector((0, -0.25, 0.19)), Vector((0, -0.30, 0.24)), Vector((0, -0.33, 0.22))],
              0.007, cotes=4, mat=0)
    lanterne = Vector((0, -0.33, 0.185))
    C.bm_boite(bm, (0.045, 0.045, 0.05), lanterne, mat=2)
    C.bm_boite(bm, (0.055, 0.055, 0.012), lanterne + Vector((0, 0, 0.03)), mat=0)
    # 3 sabords lumineux de chaque côté
    sabords = []
    for s in (-1, 1):
        for y in (-0.10, 0.0, 0.10):
            w = 0.13
            p = Vector((s * (w * 1.04 + 0.002), y, 0.0))
            C.bm_boite(bm, (0.01, 0.032, 0.03), p, mat=2)
            if s < 0:                  # côté −X local = extérieur de l'orbite (Unity +x)
                sabords.append(p + Vector((-0.04, 0, 0)))
    nav = C.piece("Navire", bm, MATS, lissage="angle", pivot=Vector((0, 0, 0)))
    voiles = [voile("Anim_VoileGrande", Vector((-0.03, 0.0, 0.34)), 0.30, 0.15, 0.06, 1),
              voile("Anim_VoileHaute", Vector((-0.03, 0.0, 0.44)), 0.20, 0.09, 0.04, 2),
              voile("Anim_VoileMisaine", Vector((0.13, 0.0, 0.31)), 0.24, 0.13, 0.05, 3)]
    for v in voiles:
        v.location.y += 0.0
        C.parenter(v, nav)
    bm = bmesh.new()                   # fanion en haut du grand mât
    haut_mat = Vector((0, -0.03, 0.46))
    C.bm_plaque(bm, [(0, 0.012), (0.0, -0.012), (-0.12, -0.002)], 0.006,
                Matrix.Translation(haut_mat) @ Matrix.Rotation(math.radians(90), 4, "Z")
                @ Matrix.Rotation(math.radians(0), 4, "X"), mat=1)
    flamme = C.piece("Anim_Flamme", bm, MATS, lissage="angle", pivot=haut_mat)
    C.parenter(flamme, nav)
    effets = {"sabords": sabords, "poupe": Vector((0, -0.36, 0.06)),
              "eclats": [lanterne + Vector((0, -0.03, 0)), Vector((0, 0.30, 0.13)),
                         haut_mat + Vector((0, 0, 0.02))]}
    return nav, effets


def anneau_brume():
    """V7 (retour 2) : l'anneau de brume se lisait toujours comme un cerceau autour du cheval.
    On garde l'objet `AnneauBrume` (le code peut le chercher), mais réduit à un minuscule
    noyau caché au centre de l'orbite, DANS le cou. La brume est maintenant un petit nuage
    sous la coque, qui suit le navire (voir navire())."""
    bm = bmesh.new()
    C.bm_sphere(bm, 0.02, (0, 0, 0), segments=4, anneaux=2, mat=3)
    return bm


def construire():
    C.nouvelle_scene()
    incl = C.vide("Inclinaison", C.U(*CENTRE_ORBITE))
    incl.rotation_euler = (math.radians(INCLINAISON), 0, 0)     # devant (+Y) en haut
    bpy.context.view_layer.update()
    bm = anneau_brume()
    bmesh.ops.transform(bm, matrix=incl.matrix_world, verts=bm.verts)
    brume = C.piece("AnneauBrume", bm, MATS, lissage="tout", pivot=incl.matrix_world.translation)
    C.parenter(brume, incl)
    orbite = C.vide("Orbite")
    orbite.matrix_world = incl.matrix_world.copy()
    C.parenter(orbite, incl)
    # le navire à droite de l'image (Unity x = +1,0 = Blender x = −1,0), proue vers l'avant,
    # coque posée sur l'anneau (la quille trempe dans la brume)
    m_nav = orbite.matrix_world @ Matrix.Translation((-RAYON, 0, 0.08)) @ Matrix.Scale(ECHELLE, 4)
    tangage = C.vide("Anim_Tangage")
    tangage.matrix_world = m_nav
    C.parenter(tangage, orbite)
    nav, eff = navire()
    objs = C.descendants(nav)
    bpy.context.view_layer.update()
    # on place tout le navire (pièces + pivots) dans le repère du navire sur l'orbite ;
    # les cibles sont calculées AVANT de bouger quoi que ce soit (sinon déplacer le parent
    # déplace déjà les enfants)
    cibles = {o.name: m_nav @ o.matrix_world.translation for o in objs}
    for o in objs:
        if o.type == "MESH":
            o.data.transform(m_nav.to_3x3().to_4x4())
    for o in objs:
        o.matrix_world = Matrix.Translation(cibles[o.name])
        bpy.context.view_layer.update()
    C.parenter(nav, tangage)
    vides = []
    m_eff = m_nav
    for i, p in enumerate(eff["sabords"]):
        vides.append(C.vide(f"Fx_Sabord_{i + 1}", m_nav @ p))
    vides += C.reperes_effets([m_nav @ e for e in eff["eclats"]], fx=m_nav @ eff["poupe"])
    for v in vides:
        v.empty_display_size = 0.04
        C.parenter(v, nav)
    return C.racine_v5(NOM, POINT, [incl])


if __name__ == "__main__":
    P.finir_pirate(construire(), NOM, POINT, y_min=0.3)
