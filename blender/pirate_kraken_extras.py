"""
pirate_kraken_extras.py — Les « extras » 3D du skin Kraken (`pirate_kraken`, SECRET) :
les 4 tentacules qui sortent du socle. Fiche : docs/specs/v5-catalogue.md §1.1 et §0.4.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/pirate_kraken_extras.py
Sortie : blender/exports/pirate_kraken_extras.fbx et docs/captures/v5_cheval_pirate_kraken_extras.png.

Ce n'est PAS un accessoire : c'est le `skinExtrasPrefab` du skin Kraken, posé comme enfant
direct de `Joueur` (origine = la base du cheval, échelle 1, décalage nul). On peut le porter
avec 2 accessoires (catalogue §0.4).

Les 4 tentacules (base Ø 22 cm, pointe Ø 4,4 cm, ~1,1 m ; un peu plus épais que la fiche pour la lisibilité) sortent du socle : deux devant, un
peu écartés (±38°), deux derrière sur les côtés (±128°, visibles de face). Chacun rampe sur
le sol, puis se dresse et s'enroule vers l'intérieur (pointe à 0,7 m de haut) ; aucun point
à plus de 1,0 m du centre (enveloppe §0.2 : 1,1 m au plus). Ventouses roses sur la face
intérieure de l'enroulement, 2 taches bioluminescentes cyan par tentacule.

ANIMATION (pour le programmeur) : chaque tentacule est une CHAÎNE DE 8 SEGMENTS emboîtés
(un segment = un objet, enfant du précédent, origine = sa base). Chaque segment déborde
un peu dans le suivant (bout arrondi) : on peut plier chaque articulation de ±15° sans
qu'un jour apparaisse. Ondulation : rotation autour de l'axe X LOCAL de chaque segment
(axes = ceux du cheval ; il suffit de tourner autour de l'axe horizontal perpendiculaire
au tentacule, donné ci-dessous), sinus avec un retard de phase par segment (vague de la
base vers la pointe, 15°, 2,2 s), phase différente par tentacule.

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    pirate_kraken_extras          vide, origine = base du cheval (enfant de Joueur)
    ├── Anim_TentaculeAvantGauche_1   segment 1 (base, dans le socle) ; origine = sa base
    │   └── Anim_TentaculeAvantGauche_2  … jusqu'à _8 (pointe)
    │       └── … └── Anim_TentaculeAvantGauche_8
    │                  └── Fx_Goutte_1   vide : la pointe (gouttes d'eau, 1 / 0,5 s)
    ├── Anim_TentaculeAvantDroit_1..8   (Fx_Goutte_2)
    ├── Anim_TentaculeArriereGauche_1..8 (Fx_Goutte_3)
    ├── Anim_TentaculeArriereDroit_1..8  (Fx_Goutte_4)
    └── Fx_Emetteur               vide : devant le socle (bulles, 3 toutes les ~6 s)
Matières (3) : peau violette mouillée (M5_PirKrakenPeau, smoothness 0,85 ; le code peut
la remplacer par la matière calculée du skin), ventouses roses, taches cyan (émission 1,3,
allumées tour à tour par le code).
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_pirates as P  # noqa: E402

NOM = "pirate_kraken_extras"
MATS = ["M5_PirKrakenPeau", "M5_PirVentouse", "M5_PirBiolum"]   # 0, 1, 2
# (nom, angle en degrés vu de dessus : 0 = devant, + = gauche de l'image, sens d'enroulement)
TENTACULES = [("AvantGauche", 38, 1), ("AvantDroit", -38, -1),
              ("ArriereGauche", 128, -1), ("ArriereDroit", -128, 1)]
# Profil (distance au centre, hauteur) : rampe au sol, se dresse, s'enroule vers l'intérieur
PROFIL = [(0.30, 0.13), (0.45, 0.09), (0.60, 0.10), (0.75, 0.16), (0.87, 0.29),
          (0.93, 0.45), (0.90, 0.60), (0.81, 0.69), (0.72, 0.66)]
R0, R1 = 0.11, 0.022
COTES = 6


def segment(bm, a, b, ra, rb, mat=0):
    """Un segment : tube de a vers b (3 anneaux, un peu renflé au milieu) qui DÉBORDE de
    ra vers l'arrière (il rentre dans le segment précédent) ; bouts arrondis."""
    d = (b - a).normalized()
    m = P.matrice_vers(Vector((0, 0, 0)), d, haut=Vector((0, 0, 1)))
    x, y = m.col[0].xyz, m.col[1].xyz
    centres = [a - d * ra * 0.6, a, (a + b) / 2, b]
    rayons = [ra * 0.55, ra, (ra + rb) / 2 * 1.06, rb]
    anneaux = []
    for c, r in zip(centres, rayons):
        anneaux.append([bm.verts.new(c + (x * math.cos(2 * math.pi * k / COTES)
                                          + y * math.sin(2 * math.pi * k / COTES)) * r)
                        for k in range(COTES)])
    faces = []
    for i in range(len(anneaux) - 1):
        A, B = anneaux[i], anneaux[i + 1]
        for k in range(COTES):
            j = (k + 1) % COTES
            faces.append(bm.faces.new((A[k], A[j], B[j], B[k])))
    for anneau, pole in ((anneaux[0], centres[0] - d * ra * 0.4),
                         (anneaux[-1], b + d * rb * 0.7)):
        pv = bm.verts.new(pole)
        for k in range(COTES):
            faces.append(bm.faces.new((anneau[k], anneau[(k + 1) % COTES], pv)))
    for f in faces:
        f.material_index = mat
    bmesh.ops.recalc_face_normals(bm, faces=faces)


def tentacule(nom, angle_deg, sens, racine):
    a = math.radians(angle_deg)
    dirh = Vector((math.sin(a), math.cos(a), 0.0))          # vers l'extérieur (Blender)
    lat = Vector((0, 0, 1)).cross(dirh)
    pts = []
    for k, (r, h) in enumerate(PROFIL):
        # léger vrillage sur le côté (le tentacule ne reste pas dans un plan) : vivant
        torsion = sens * 0.06 * math.sin(math.pi * k / (len(PROFIL) - 1))
        pts.append(dirh * r + lat * torsion + Vector((0, 0, h)))
    n = len(pts) - 1
    parent = racine
    objs = []
    for k in range(n):
        ra = R0 + (R1 - R0) * (k / n) ** 0.9
        rb = R0 + (R1 - R0) * ((k + 1) / n) ** 0.9
        bm = bmesh.new()
        segment(bm, pts[k], pts[k + 1], ra, rb)
        d = (pts[k + 1] - pts[k]).normalized()
        # face « intérieure » de l'enroulement : vers le centre de la boucle (vers le haut
        # et l'intérieur), là où vont les ventouses
        inter = (Vector((0, 0, 1)) - dirh * 0.6)
        inter = (inter - d * inter.dot(d)).normalized()
        if 1 <= k <= 5:
            c = (pts[k] + pts[k + 1]) / 2 + inter * ((ra + rb) / 2 * 0.95)
            C.bm_disque(bm, (ra + rb) / 2 * 0.45, 0.012, C.repere(c, inter), cotes=5, mat=1)
        if k in (2, 5):                                     # taches bioluminescentes
            ext = -inter
            c = (pts[k] + pts[k + 1]) / 2 + ext * ((ra + rb) / 2 * 0.95) + lat * 0.0
            C.bm_disque(bm, (ra + rb) / 2 * 0.40, 0.012, C.repere(c, ext), cotes=5, mat=2)
        o = C.piece(f"Anim_Tentacule{nom}_{k + 1}", bm, MATS, lissage="tout", pivot=pts[k])
        C.parenter(o, parent)
        parent = o
        objs.append(o)
    return objs[-1], pts[-1]


def construire():
    C.nouvelle_scene()
    racine = C.vide(NOM, (0, 0, 0))
    for i, (nom, ang, sens) in enumerate(TENTACULES):
        bout, p = tentacule(nom, ang, sens, racine)
        g = C.vide(f"Fx_Goutte_{i + 1}", p)
        g.empty_display_size = 0.05
        C.parenter(g, bout)
    fx = C.vide("Fx_Emetteur", C.U(0.0, 0.15, -0.45))
    fx.empty_display_size = 0.05
    C.parenter(fx, racine)
    bpy.context.view_layer.update()
    return racine


if __name__ == "__main__":
    P.finir_pirate(construire(), NOM, "Joueur", y_min=-0.3)
