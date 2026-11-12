"""
pirate_chapeau_capitaine.py — Chapeau de capitaine (boîte d'accessoires Pirates :
`chapeau_capitaine`, LÉGENDAIRE, Tête). Fiche : docs/specs/v5-catalogue.md §2 n° 07.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/pirate_chapeau_capitaine.py
Sortie : blender/exports/chapeau_capitaine.fbx et docs/captures/v5_cheval_chapeau_capitaine.png.

Un grand BICORNE porté en travers (les deux pointes à gauche et à droite) : vu de face, une
demi-lune noire-violette de 1,2 m de large, bien plus imposante que le tricorne.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : deux grands pans de feutre bombés (devant et derrière) en demi-lune, qui
                 se rejoignent presque en haut ; une calotte les ferme vus de dessus ;
  - secondaire : la bordure dorée épaisse (jonc de 7 cm) sur le haut des deux pans, la
                 grande ANCRE dorée (0,34 m) en relief au milieu du pan avant (elle luit : émission
                 0,8), et le panache de 3 plumes blanches (le détail-signature, 0,6 m) ;
  - tertiaire  : 3 gemmes bleu glacier : une au bout de chaque pointe, une au-dessus de
                 l'ancre.
Matières (5, budget Légendaire de la bible §13.7) : feutre (smoothness 0,40), doré déco,
or de l'ancre émissif (M5_PirAncreLuit), gemme (0,95), plume blanche mate (M5_BlancMat).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    chapeau_capitaine   vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Chapeau         pans + calotte + bordures + gemmes ; origine = centre de la base
        ├── Anim_Ancre  l'ancre ; origine = son centre. Matériau M5_PirAncreLuit (émission
        │               0,8) : une étincelle la traverse toutes les 2 s (code)
        ├── Anim_Plume  le panache ; origine = sa base (ressort A et balancement ±5°, 2,4 s)
        ├── Eclat_1..3  vides : bouts des pointes et haut de l'ancre (1 éclat / 1,5 s,
        │               émission 1,2)
        └── Fx_Emetteur vide : au-dessus de l'ancre (≤ 10 petites étoiles dorées)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_pirates as P  # noqa: E402

NOM = "chapeau_capitaine"
POINT = "PointChapeau"
MATS = ["M5_PirFeutreCapitaine", "M5_DoreDeco", "M5_PirAncreLuit", "M5_PirGemme",
        "M5_BlancMat"]   # 0..4
BASE = (0.0, 1.83, 0.06)
X0 = 0.60                # demi-largeur (pointes)
NX, NT = 15, 4


def bas(x):
    return 0.10 * (x / X0) ** 4


def haut(x):
    return 0.12 + 0.34 * max(0.0, 1 - (x / X0) ** 2) ** 0.75


def pan_point(x, t, cote, interieur=False):
    """Point du pan (cote +1 devant, −1 derrière) ; t = 0 en bas, 1 en haut."""
    yb = (0.29 if cote > 0 else 0.33) * max(0.0, 1 - (x / X0) ** 2) ** 0.5 + 0.03
    y = yb * (1 - 0.62 * t ** 1.3)
    if interieur:
        y -= 0.035 * (1 - 0.3 * t)
    z = bas(x) + (haut(x) - bas(x)) * t
    return Vector((x, cote * y, z))


def pan(bm, cote):
    xs = [X0 * (-1 + 2 * k / (NX - 1)) * 0.995 for k in range(NX)]
    ts = [k / (NT - 1) for k in range(NT)]
    ext = [[bm.verts.new(pan_point(x, t, cote)) for x in xs] for t in ts]
    inn = [[bm.verts.new(pan_point(x, t, cote, True)) for x in xs] for t in ts]
    faces = []
    for g in (ext, inn):
        for j in range(NT - 1):
            for i in range(NX - 1):
                faces.append(bm.faces.new((g[j][i], g[j][i + 1], g[j + 1][i + 1], g[j + 1][i])))
    # tranche : tout le tour du pan
    bord = [(0, i) for i in range(NX)] + [(j, NX - 1) for j in range(1, NT)] + \
           [(NT - 1, i) for i in range(NX - 2, -1, -1)] + [(j, 0) for j in range(NT - 2, 0, -1)]
    for k in range(len(bord)):
        a, b = bord[k], bord[(k + 1) % len(bord)]
        faces.append(bm.faces.new((ext[a[0]][a[1]], ext[b[0]][b[1]], inn[b[0]][b[1]],
                                   inn[a[0]][a[1]])))
    for f in faces:
        f.material_index = 0
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    # jonc doré sur le haut du pan
    chemin = [pan_point(x, 1.0, cote) + Vector((0, -cote * 0.015, 0.012)) for x in xs]
    C.bm_tube(bm, chemin, 0.036, cotes=5, mat=1, rayons=[0.6] + [1.0] * (NX - 2) + [0.6])


def calotte(bm):
    prof = [(0.30, -0.03), (0.31, 0.04), (0.28, 0.16), (0.18, 0.26), (0.0, 0.29)]
    C.bm_revolution(bm, prof, 12, mats=[0] * 4)


def ancre(bm, m, mat):
    """Ancre de dessin animé (≈ 0,26 de haut avant l'échelle) construite dans un repère
    local (devant = +Y, haut = +Z), puis posée par la matrice m (position, pente, échelle)."""
    avant = set(bm.verts)
    e = 0.035
    I = Matrix.Identity(4)
    C.bm_boite(bm, (0.032, e, 0.20), (0, 0, 0), mat=mat)                          # verge
    C.bm_boite(bm, (0.15, e, 0.032), (0, 0, 0.065), mat=mat)                      # jas
    C.bm_tore(bm, 0.03, 0.012, segments=10, segments_tube=4, mat=mat,
              matrice=Matrix.Translation((0, 0, 0.125)) @ Matrix.Rotation(math.radians(90), 4, "X"))
    arc = [Vector((0.11 * math.sin(a), 0, -0.02 - 0.08 * math.cos(a)))
           for a in [math.radians(d) for d in range(-75, 76, 25)]]
    C.bm_tube(bm, arc, 0.018, cotes=5, mat=mat)
    for s in (-1, 1):                                                               # pattes
        a = math.radians(75 * s)
        c = Vector((0.11 * math.sin(a), 0, -0.02 - 0.08 * math.cos(a)))
        mm = Matrix.Translation(c) @ Matrix.Rotation(math.radians(-40 * s), 4, "Y")
        C.bm_plaque(bm, [(0, 0.055), (-0.035, -0.01), (0.035, -0.01)], e, mm, mat=mat)
    C.bm_sphere(bm, 0.022, (0, 0, -0.10), segments=6, anneaux=4, mat=mat)
    nouveaux = [v for v in bm.verts if v not in avant]
    bmesh.ops.transform(bm, matrix=m @ I, verts=nouveaux)


def construire():
    C.nouvelle_scene()
    base = C.U(*BASE)
    bm = bmesh.new()
    pan(bm, +1)
    pan(bm, -1)
    calotte(bm)
    eclats = []
    for s in (-1, 1):                       # gemmes au bout des pointes
        p = Vector((s * X0 * 0.99, 0, bas(X0) + 0.06))
        C.bm_sphere(bm, 0.04, p, segments=6, anneaux=4, mat=3, echelle=(1, 1, 1.25))
        eclats.append(base + p + Vector((0, 0, 0.05)))
    # repère du pan avant, au milieu (normale vers l'avant, un peu vers le haut)
    pa = pan_point(0.0, 0.42, +1)
    pb = pan_point(0.0, 0.62, +1)
    pente = (pb - pa).normalized()
    n = Vector((1, 0, 0)).cross(pente).normalized()
    if n.y < 0:
        n = -n
    m = C.repere(pan_point(0.0, 0.40, +1) + n * 0.03, n, pente)
    p_gem = m @ Vector((0, 0.0, 0.25))
    C.bm_sphere(bm, 0.035, p_gem, segments=6, anneaux=4, mat=3)
    bmesh.ops.translate(bm, vec=base, verts=bm.verts)
    chapeau = C.piece("Chapeau", bm, MATS, lissage="angle", pivot=base)

    bm = bmesh.new()
    ancre(bm, Matrix.Translation(base) @ m @ Matrix.Scale(1.3, 4), 2)
    a = C.piece("Anim_Ancre", bm, MATS, lissage="angle", pivot=base + m.translation)
    C.parenter(a, chapeau)
    eclats.append(base + p_gem + n * 0.04)

    # panache : 3 plumes depuis le haut, derrière la bordure avant, inclinées en éventail
    bm = bmesh.new()
    p0 = base + Vector((0.0, 0.0, haut(0.0) - 0.04))
    for dx, dy, lg, w in ((0.10, -0.55, 0.62, 0.27), (0.45, -0.45, 0.50, 0.24),
                          (-0.30, -0.60, 0.46, 0.22)):
        P.bm_plume(bm, p0, Vector((dx, dy, 1.0)), lg, w,
                   courbe=Vector((dx * 0.35, -0.30, -0.30)), mat=4, cotes=6, n=6,
                   haut=Vector((0, 1, 0.6)), plat=0.45)
    plume = C.piece("Anim_Plume", bm, MATS, lissage="tout", pivot=p0)
    C.parenter(plume, chapeau)
    for e in C.reperes_effets(eclats, fx=base + p_gem + n * 0.06):
        C.parenter(e, chapeau)
    return C.racine_v5(NOM, POINT, [chapeau])


if __name__ == "__main__":
    P.finir_pirate(construire(), NOM, POINT)
