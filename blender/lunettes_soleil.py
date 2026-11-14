"""
lunettes_soleil.py — Lunettes de soleil (boîte d'accessoires n°1 : `lunettes_soleil`, Commun, Visage).
VERSION v5 « plus jeu moderne » (docs/art-bible.md §13.8 fiche 4), faites SUR le cheval.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/lunettes_soleil.py
Sortie : blender/exports/lunettes_soleil.fbx et docs/captures/v5_cheval_lunettes_soleil.png.

Défaut corrigé : les branches étaient droites et s'écartaient dans le vide. Elles suivent
maintenant le côté de la tête, 1,5 cm au-dessus de la peau : chaque point de la branche est
posé sur la VRAIE surface du cheval par un rayon « lissé » (commun.sur_cheval_lisse, qui
passe par-dessus le creux derrière l'œil au lieu d'y plonger), puis la branche rentre sous
la crinière, derrière l'œil.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : la monture épaisse (6 cm) qui déborde des verres, avec un « sourcil » plus
                 épais en haut ;
  - secondaire : les verres SÉPARÉS de la monture, violet nuit très sombre, vernis 0,95
                 (on voit que ce sont des verres) ; le pont ; les branches ;
  - tertiaire  : 2 reflets peints par verre (barre + point, en haut à gauche) ; 2 charnières
                 dorées (cubes biseautés de 6 cm) aux coins extérieurs.
Matières : monture satinée 0,50, verres 0,95, doré déco, reflet.

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    lunettes_soleil     vide, origine = PointVisage (attachScale 1, attachOffset 0)
    └── Anim_Lunettes   toutes les lunettes ; origine = le pont (ressort très léger, 0,02 m :
                        elles « sautillent » sur le museau au saut)
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

NOM = "lunettes_soleil"
POINT = "PointVisage"
# V7 (lot 4) : monture noire vernie et verres noirs (l'icône), plus de verres violets
MATS = ["M7_MontureNoire", "M7_VerreNoir", "M5_DoreDeco", "M5_Reflet"]  # 0..3
LARGEUR, HAUTEUR = 0.25, 0.22      # verre (couvre l'œil du cheval, 0,17 × 0,21)
MONTEE = 0.02
AVANCE = 0.05                      # le verre est 5 cm devant l'œil
OUVERTURE = 12.0                   # degrés : chaque verre tourne vers son côté
BANDE = 0.04                       # largeur de la monture autour du verre
SOURCIL = 0.035                    # en plus, en haut
PROF = 0.06                        # épaisseur (profondeur) de la monture
ARBRE = None


def repere_verre(x_unity):
    cote = 1 if x_unity < 0 else -1          # côté Blender (+X = gauche de l'image)
    a = math.radians(OUVERTURE)
    centre = C.U(x_unity, C.CHEVAL_YEUX_Y + MONTEE, C.CHEVAL_YEUX_AVANT - AVANCE)
    return C.repere(centre, Vector((cote * math.sin(a), math.cos(a), 0.0))), cote


def cadre(bm, m):
    """Monture d'un verre : un cadre épais dont la section est arrondie (balayage fermé
    autour du verre). Le haut est plus large (le « sourcil »)."""
    contour = C.contour_rectangle_arrondi(LARGEUR + BANDE, HAUTEUR + BANDE, 0.08, par_coin=2)
    chemin, hauts = [], []
    for x, z in contour:
        dz = SOURCIL * max(0.0, z / (HAUTEUR / 2)) * 0.5
        chemin.append(m @ Vector((x, 0.0, z + dz)))
        hauts.append(m.to_3x3() @ Vector((0, 1, 0)))
    n = len(chemin)
    anneaux = []
    for i in range(n):
        t = (chemin[(i + 1) % n] - chemin[i - 1]).normalized()
        avant = hauts[i]
        dehors = t.cross(avant).normalized()
        zloc = contour[i][1] / (HAUTEUR / 2)
        w = BANDE / 2 + SOURCIL * max(0.0, zloc) * 0.5 + 0.004
        sec = [(-w, -PROF / 2), (w, -PROF / 2), (w + 0.006, 0.0), (w, PROF / 2),
               (-w, PROF / 2), (-w - 0.006, 0.0)]
        anneaux.append([bm.verts.new(chemin[i] + dehors * a + avant * b) for a, b in sec])
    faces = []
    for i in range(n):
        A, B = anneaux[i], anneaux[(i + 1) % n]
        for s in range(6):
            u = (s + 1) % 6
            f = bm.faces.new((A[s], B[s], B[u], A[u]))
            f.material_index = 0
            faces.append(f)
    bmesh.ops.recalc_face_normals(bm, faces=faces)


def verre(bm, m):
    contour = C.contour_rectangle_arrondi(LARGEUR, HAUTEUR, 0.075, par_coin=3)
    C.bm_coussin(bm, contour, 0.035, 0.05, m, mat=1, anneaux=2)
    # Reflet peint (barre + point = 2 reflets) : en haut à gauche (+X local), sur le devant
    C.bm_reflet(bm, m @ Matrix.Translation((0.055, 0.026, 0.045)), mat=3)


def branche(bm, m, cote):
    """La branche part du coin extérieur du cadre, longe le côté de la tête (rayons lissés)
    et finit enfoncée sous la crinière, derrière l'œil."""
    depart = m @ Vector((cote * (LARGEUR / 2 + BANDE * 0.9), -0.01, HAUTEUR / 2 - 0.03))
    chemin = [depart]
    centre = (0.0, C.CHEVAL_YEUX_Y + MONTEE + 0.04, 0.03)
    for ang, ecart in ((58, 0.02), (72, 0.016), (86, 0.015), (100, 0.015), (114, -0.02)):
        p, _ = C.sur_cheval_lisse(ARBRE, centre, math.radians(cote * ang), 0.0, ecart,
                                  pente=0.5, fenetre=(0.14, 0.14), pas=0.07)
        chemin.append(p)
    C.bm_tube(bm, chemin, 0.022, cotes=6, mat=0)
    return depart


def construire():
    global ARBRE
    C.nouvelle_scene()
    ARBRE = C.arbre_cheval()
    bm = bmesh.new()          # pièces lisses (cadres, verres, branches, pont)
    bm_dur = bmesh.new()      # charnières (biseau v5)
    reperes = []
    for x_unity in (-C.CHEVAL_YEUX_X, +C.CHEVAL_YEUX_X):
        m, cote = repere_verre(x_unity)
        reperes.append(m)
        cadre(bm, m)
        verre(bm, m)
        depart = branche(bm, m, cote)
        C.bm_boite(bm_dur, (0.065, 0.065, 0.065), depart + Vector((0, 0.01, 0)), mat=0)   # V7 : charnières noires
    # Pont : un arc entre le haut des deux cadres, au-dessus du chanfrein.
    a, b = reperes[0], reperes[1]
    pa = a @ Vector((-(LARGEUR / 2 + BANDE / 2) * 1.0, 0, HAUTEUR / 2 - 0.02))
    pb = b @ Vector(((LARGEUR / 2 + BANDE / 2) * 1.0, 0, HAUTEUR / 2 - 0.02))
    milieu = (pa + pb) / 2 + Vector((0, 0.02, 0.025))
    C.bm_tube(bm, [pa, pa.lerp(milieu, 0.6), milieu, pb.lerp(milieu, 0.6), pb], 0.022, cotes=6)
    lisse = C.piece("Monture", bm, MATS, lissage="angle")
    dur = C.piece("Charnieres", bm_dur, MATS, biseau=0.015, segments=2)
    obj = C.joindre([lisse, dur], "Anim_Lunettes")
    C.deplacer_origine(obj, milieu)
    return C.racine_v5(NOM, POINT, [obj])


if __name__ == "__main__":
    C.finir_v5(construire(), NOM, POINT)
