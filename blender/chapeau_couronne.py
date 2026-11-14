"""
chapeau_couronne.py — Couronne royale (boîte d'accessoires n°1 : `chapeau_couronne`,
Super rare, Tête). VERSION v5 « plus jeu moderne » (docs/art-bible.md §13.8 fiche 13), faite
SUR le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/chapeau_couronne.py
Sortie : blender/exports/chapeau_couronne.fbx et docs/captures/v5_cheval_chapeau_couronne.png.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : l'anneau d'or et ses 5 pointes HAUTES (0,22), arrondies, évasées vers
                 l'extérieur (couronne de conte, sans copier un modèle connu) ;
  - secondaire : un bandeau en relief autour de l'anneau, avec une frise de losanges ; un
                 bonnet de velours violet à l'intérieur ; le gros rubis avant Ø 0,16 taillé ;
  - tertiaire  : une perle de nacre au bout de chaque pointe, 4 petits rubis sur le bandeau.
Matières : or trésor (M_Or : le seul objet porté en or), nacre, pierre (rubis), velours.
Pourquoi l'or brille enfin : tous ses bords sont ARRONDIS (le haut de l'anneau est un
boudin, le bandeau un demi-jonc) : une partie de chaque bord regarde le ciel et le reflète.
Une bande d'or verticale et plate, vue d'en haut, ne refléterait que le sol sombre (marron).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    chapeau_couronne    vide, origine = PointChapeau (attachScale 1, attachOffset 0)
    └── Anim_Couronne   toute la couronne ; origine = centre de la base (légère rotation
        │               ±2° : elle tient « royalement »)
        ├── Rubis       le gros rubis avant (matériau M5_Rubis : le code fait « respirer »
        │               son émission 0,2 ↔ 0,5 en 2 s) ; origine = son centre
        ├── Eclat_1..3  vides : points d'éclat au bout de 3 pointes (1 toutes les 2,5 s)
        └── Fx_Emetteur vide : au centre, au-dessus : 3 étincelles dorées qui montent
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "chapeau_couronne"
POINT = "PointChapeau"
MATS = ["M7_OrVif", "M5_Nacre", "M5_Rubis", "M5_Velours"]  # 0..3 (V7 : or clair, voir commun.py)
BX, BY, BZ = C.CHAPEAU_BASE[0], C.CHAPEAU_BASE[1] + 0.06, C.CHAPEAU_BASE[2]
R0 = 0.31
BANDE = 0.13
POINTE = 0.22
N = 35                     # segments autour (5 pointes × 7, V7 : plus lisse)
EP = 0.045                 # épaisseur de l'anneau


def hauteur(a):
    """Hauteur du bord haut : 5 pointes arrondies, la 1re droit devant (a = 90°, +Y)."""
    phase = ((a - math.pi / 2) / (2 * math.pi / 5)) % 1.0
    t = abs(phase - 0.5) * 2                       # 1 au sommet d'une pointe, 0 au creux
    return BANDE + POINTE * (math.sin(t * math.pi / 2) ** 2.2)


def rayon(z):
    """Les pointes s'évasent vers l'extérieur."""
    return R0 + 0.10 * max(0.0, z - BANDE) / POINTE


def anneau(bm):
    rangs = []
    lignes = 4
    secs = []
    for k in range(N):
        a = 2 * math.pi * k / N
        h = hauteur(a)
        c, s = math.cos(a), math.sin(a)
        ext, inn = [], []
        for j in range(lignes):
            z = h * j / (lignes - 1) * 0.94
            r = rayon(z)
            ext.append(Vector((c * (r + EP / 2), s * (r + EP / 2), z)))
            inn.append(Vector((c * (r - EP / 2), s * (r - EP / 2), z)))
        r = rayon(h)
        # le bord haut : un boudin arrondi (3 points)
        haut = [Vector((c * (r + EP * 0.35), s * (r + EP * 0.35), h * 0.985)),
                Vector((c * r, s * r, h + 0.012)),
                Vector((c * (r - EP * 0.35), s * (r - EP * 0.35), h * 0.985))]
        sec = ext + haut + list(reversed(inn))
        secs.append(sec)
    m = len(secs[0])
    vs = [[bm.verts.new(p) for p in sec] for sec in secs]
    faces = []
    for k in range(N):
        A, B = vs[k], vs[(k + 1) % N]
        for j in range(m):
            u = (j + 1) % m             # le dernier relie l'intérieur bas à l'extérieur bas
            f = bm.faces.new((A[j], B[j], B[u], A[u]))
            f.material_index = 0
            faces.append(f)
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    return [v for r in vs for v in r]


def bandeau(bm):
    """Bandeau en relief (demi-jonc) au milieu de l'anneau + frise de losanges."""
    v = []
    prof = [(R0 + EP / 2 - 0.005, 0.025), (R0 + EP / 2 + 0.018, 0.04),
            (R0 + EP / 2 + 0.025, 0.065), (R0 + EP / 2 + 0.018, 0.09),
            (R0 + EP / 2 - 0.005, 0.105)]
    rr = C.bm_revolution(bm, prof, N, mats=[0] * 4)
    v += [x for r in rr for x in r]
    for k in range(0):         # V7 : frise de losanges retirée (absente de l'icône, plus épuré)
        a = math.pi / 2 + 2 * math.pi * (k + 0.5) / 10
        d = Vector((math.cos(a), math.sin(a), 0))
        if abs(a - math.pi / 2) % (2 * math.pi) < 0.4:
            continue
        p = d * (R0 + EP / 2 + 0.03) + Vector((0, 0, 0.065))
        los = [(0.0, -0.03), (0.022, 0.0), (0.0, 0.03), (-0.022, 0.0)]
        v += C.bm_coussin(bm, los, 0.015, 0.03, C.repere(p, d), mat=0, anneaux=1, dos_plat=True)
    return v


def construire():
    C.nouvelle_scene()
    base = C.U(BX, BY, BZ)
    bm = bmesh.new()
    v = anneau(bm) + bandeau(bm)
    # V7 (point 17) : plus de bonnet de velours violet dedans. Vu du jeu, il remplissait la
    # couronne d'une masse sombre ; l'icône est une couronne d'or ouverte.
    # perles de nacre au bout des pointes, petits rubis sur le bandeau
    eclats = []
    for k in range(5):
        a = math.pi / 2 + 2 * math.pi * k / 5
        h = hauteur(a)
        p = Vector((math.cos(a), math.sin(a), 0)) * rayon(h) + Vector((0, 0, h + 0.04))
        v += C.bm_sphere(bm, 0.045, p, segments=10, anneaux=6, mat=0)   # V7 : boules d'or (icône)
        if k in (0, 1, 4):
            eclats.append(base + p + Vector((0, 0, 0.04)))
    for k in (1, 2, 3, 4):
        a = math.pi / 2 + 2 * math.pi * k / 5
        d = Vector((math.cos(a), math.sin(a), 0))
        p = d * (R0 + EP / 2 + 0.03) + Vector((0, 0, 0.065))
        v += C.bm_coussin(bm, C.contour_ellipse(0.035, 0.035, n=6), 0.02, 0.05, C.repere(p, d),
                          mat=2, anneaux=1, dos_plat=True)
    bmesh.ops.translate(bm, vec=base, verts=list(set(v)))
    couronne = C.piece("Anim_Couronne", bm, MATS, lissage="angle", pivot=base)

    # le gros rubis avant, taillé (facettes), serti d'un anneau d'or
    bm = bmesh.new()
    d = Vector((0, 1, 0))
    p = d * (R0 + EP / 2 + 0.045) + Vector((0, 0, 0.07))
    m = C.repere(base + p, d)
    pierre = C.bm_coussin(bm, C.contour_ellipse(0.08, 0.08, n=12), 0.05, 0.10, m, mat=2,
                          anneaux=2, dos_plat=True)
    serti = C.bm_tore(bm, 0.085, 0.016, segments=20, segments_tube=6, mat=0)
    bmesh.ops.transform(bm, matrix=m @ Matrix.Rotation(math.radians(90), 4, "X"), verts=serti)
    rubis = C.piece("Rubis", bm, MATS, lissage="angle", pivot=base + p)
    C.parenter(rubis, couronne)
    for e in C.reperes_effets(eclats, fx=base + Vector((0, 0, 0.45))):
        C.parenter(e, couronne)
    return C.racine_v5(NOM, POINT, [couronne])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT)
