"""
animal_baleine_celeste.py — Baleine céleste (boîte « Animaux : accessoires » :
`baleine_celeste`, SECRET, Dos). docs/specs/v5-catalogue.md §4.1, bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/animal_baleine_celeste.py
Sortie : blender/exports/baleine_celeste.fbx et docs/captures/v5_cheval_baleine_celeste.png.

L'idée : une petite baleine qui NAGE DANS L'AIR autour du cheval, le dos couvert d'étoiles.
Le modèle est livré « en pause » sur sa trajectoire (à droite de l'écran, à hauteur de la
tête) ; c'est le code (composant OrbitPath, mode « 8 ») qui la fait voyager.

Trois niveaux de forme :
  - primaire   : un corps de baleine « mascotte » (0,80 de long) : grosse tête ronde, corps
                 qui s'affine vers la queue ; une grande nageoire caudale à 2 lobes ;
  - secondaire : deux larges nageoires pectorales, le ventre clair RAYÉ (sillons de baleine
                 bleue, 7 bandes), l'évent sur le dessus ;
  - tertiaire  : 11 étoiles en relief sur le dos (émission 1,2 : elles scintillent comme la
                 Galaxie), gros yeux doux vernis avec reflet, sourire, petites joues claires ;
  - la pièce « magique » du Secret : une traînée de 7 étoiles en S, qui flotte derrière la
    queue sans la toucher (et que le code fait scintiller).
Matières (5) : dos bleu nuit satiné 0,70, ventre bleu clair 0,55, étoiles émissives 1,2
(le code fait osciller leur émission 0,6 ↔ 1,2 en décalé), noir verni, reflet.

La trajectoire en « 8 » (pour le programmeur) : centre = le vide `Trajectoire`, en
(0 ; 1,75 ; 0,05) local à Joueur ; 8 couché de 2,2 m de large (x de −1,1 à +1,1) qui passe
DEVANT le cheval au-dessus de la tête (y 2,3) et DERRIÈRE à y 1,2 ; un tour en 9 s ; la
baleine regarde toujours vers l'avant de la courbe et ondule (queue ±20°, 1,2 s). Elle ne
reste jamais plus de 0,5 s devant les yeux (elle passe au-dessus quand elle est devant).

Hiérarchie :
    baleine_celeste            vide, origine = PointDos (attachScale 1, attachOffset 0)
    └── Trajectoire            vide FIXE : centre du « 8 » (repère du chemin)
        └── Anim_Baleine       le corps ; origine = son centre (le code le déplace sur le 8,
            │                  bond en looping au saut de joie, vient se poser contre la
            │                  joue du cheval après un piège)
            ├── Anim_Queue           nageoire caudale ; origine = la racine (±20°, 1,2 s)
            ├── Anim_NageoireGauche  origine = la racine (battement ±15°, 1,2 s, décalé)
            ├── Anim_NageoireDroite
            ├── Anim_Yeux            les 2 yeux ; origine = entre les yeux (clignement)
            ├── Etoiles              les 11 étoiles du dos (matière émissive M5_EtoileBaleine)
            │   (Anim_Queue a un enfant : Anim_Traine, 7 étoiles en S qui suivent la queue)
            ├── Eclat_1..3           vides sur 3 étoiles (Secret : 1 éclat / s, arc-en-ciel)
            ├── Fx_Emetteur          vide derrière la queue : traînée de 30 étoiles et bulles
            │                        qui s'effacent en 1,2 s (réserve réutilisée)
            └── Fx_Event             vide sur l'évent : jet de 20 étoiles et confettis au saut
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_animaux as A  # noqa: E402

NOM = "baleine_celeste"
POINT = "PointDos"
MATS = ["M5_BaleineDos", "M5_BaleineVentre", "M5_EtoileBaleine", "M5_AnimNoirVerni",
        "M5_Reflet"]  # 0..4
CENTRE_8 = (0.0, 1.75, 0.05)
POSE = (0.92, 1.92, 0.12)          # Unity : où elle est « en pause » (droite de l'écran)
CAP = Vector((1.0, -0.35, 0.30))   # Blender : elle nage vers la gauche de l'image, en
                                   # s'éloignant un peu (on la voit de 3/4)
SEG = 14


def matrice_pose():
    """Repère local de la baleine : +X = vers où elle nage, +Z = son dos."""
    x = CAP.normalized()
    z = Vector((0, 0, 1))
    y = z.cross(x).normalized()
    z = x.cross(y)
    return Matrix.Translation(C.U(*POSE)) @ Matrix((x, y, z)).transposed().to_4x4()


def corps(bm):
    """Corps par révolution autour de X (nez en +X), ventre rayé."""
    profil = [(0.0, 0.40), (0.11, 0.385), (0.18, 0.33), (0.215, 0.22), (0.215, 0.10),
              (0.195, -0.04), (0.155, -0.17), (0.105, -0.27), (0.065, -0.34), (0.045, -0.39),
              (0.0, -0.405)]
    av = set(bm.verts)
    C.bm_revolution(bm, profil, SEG, mats=[0] * (len(profil) - 1), angle_depart=math.pi / SEG)
    nv = [v for v in bm.verts if v not in av]
    # l'axe du profil est Z : on le couche sur X, puis on aplatit un peu (dos plus bombé)
    m = Matrix.Diagonal((1.0, 0.92, 0.86, 1)) @ Matrix.Rotation(math.radians(90), 4, "Y")
    bmesh.ops.transform(bm, matrix=m, verts=nv)
    for v in nv:                    # corps en « banane » : la queue remonte (elle nage)
        v.co.z += 0.03 * max(0.0, v.co.x) / 0.4 + 0.16 * (min(0.0, v.co.x) / 0.4) ** 2
    # ventre : faces du dessous (z local < −0,05) sur l'avant, en bandes alternées
    for f in {f for v in nv for f in v.link_faces}:
        c = f.calc_center_median()
        ang = math.atan2(c.y, -c.z)                # 0 = pile dessous
        if c.z < -0.04 and c.x > -0.28 and abs(ang) < 1.25:
            bande = int((ang + 1.25) / (2.5 / 7))
            f.material_index = 1 if bande % 2 == 0 else 0
    # évent : une petite bosse
    A.bm_ellipsoide(bm, (0.17, 0.0, 0.2), (0.04, 0.035, 0.02), segments=6, anneaux=4, mat=0)
    # sourire : un arc noir verni de chaque côté de la tête
    for s in (-1, 1):
        arc = []
        for k in range(5):
            t = k / 4
            arc.append(Vector((0.36 - 0.17 * t, s * (0.07 + 0.105 * math.sin(t * 1.4)),
                               -0.075 - 0.03 * math.sin(t * math.pi) + 0.02 * t)))
        C.bm_tube(bm, arc, 0.009, cotes=4, mat=3)
    # joues claires
    for s in (-1, 1):
        A.bm_ellipsoide(bm, (0.24, s * 0.155, -0.06), (0.05, 0.02, 0.035), segments=6, anneaux=4,
                        mat=1, rot=Matrix.Rotation(math.radians(-s * 25), 4, "Z"))


def etoiles(arbre):
    """11 étoiles à 5 branches posées sur le dos (rayons vers le corps)."""
    bm = bmesh.new()
    rnd = [(0.22, 0.0, 0.035), (0.08, 0.08, 0.03), (0.06, -0.09, 0.028), (-0.05, 0.02, 0.034),
           (-0.12, 0.09, 0.025), (-0.14, -0.07, 0.027), (-0.24, 0.0, 0.024), (0.14, -0.04, 0.02),
           (0.13, 0.12, 0.022), (-0.02, -0.14, 0.022), (-0.3, 0.05, 0.018)]
    centres = []
    for x, y, r in rnd:
        p, n = C.poser_sur(arbre, Vector((x, y, 0.0)), Vector((0, y * 1.2, 1.0)), 0.004)
        C.bm_etoile(bm, r * 1.25, 0.012, C.repere(p, n, Vector((1, 0, 0))), mat=2, bombe=0.006)
        centres.append(p)
    return bm, centres


def queue(bm):
    """Nageoire caudale à 2 lobes, horizontale (comme une vraie baleine)."""
    lobes = []
    for k in range(13):
        t = k / 12
        a = math.pi * (0.5 - t)                    # de +90° à −90°
        r = 0.20 * (0.55 + 0.45 * abs(math.sin(a)) ** 0.6)
        x = -0.02 - r * math.cos(a) * 0.6 + 0.07 * abs(math.sin(a)) ** 3
        lobes.append((x, r * math.sin(a) * 1.15))
    lobes += [(0.03, -0.05), (0.03, 0.05)]
    cx = sum(p[0] for p in lobes) / len(lobes)
    # le contour est dans (x, y) : bm_coussin travaille dans (x, z) -> on tourne de 90° sur X
    C.bm_coussin(bm, [(x - cx, y) for x, y in lobes][::-1], 0.03, 0.05,
                 Matrix.Translation((-0.38 + cx, 0, 0.0)) @ Matrix.Rotation(math.radians(90), 4, "X"),
                 mat=0, anneaux=2)


def construire():
    C.nouvelle_scene()
    M = matrice_pose()
    traj = C.vide("Trajectoire", C.U(*CENTRE_8))

    bm = bmesh.new()
    corps(bm)
    arbre = C.arbre_bm(bm)
    bmesh.ops.transform(bm, matrix=M, verts=bm.verts)
    centre = M @ Vector((0, 0, 0))
    baleine = C.piece("Anim_Baleine", bm, MATS, lissage="tout", pivot=centre)
    C.parenter(baleine, traj)

    bm = bmesh.new()
    queue(bm)
    bmesh.ops.transform(bm, matrix=M, verts=bm.verts)
    C.parenter(C.piece("Anim_Queue", bm, MATS, lissage="tout", pivot=M @ Vector((-0.36, 0, 0))),
               baleine)

    for s, nom in ((1, "Anim_NageoireGauche"), (-1, "Anim_NageoireDroite")):
        bm = bmesh.new()
        racine = Vector((0.10, s * 0.16, -0.09))
        cont = A.contour_goutte(0.16, 0.34, n=12)
        # plaque couchée, longue vers l'extérieur, qui tombe de 12° et file vers l'arrière
        m = Matrix.Translation(racine) @ Matrix.Rotation(math.radians(s * 25), 4, "Z") \
            @ Matrix.Rotation(math.radians(-s * 102), 4, "X") @ Matrix.Translation((0, 0, 0.15))
        C.bm_coussin(bm, cont, 0.03, 0.05, m, mat=0, mat_dos=1, anneaux=1)
        bmesh.ops.transform(bm, matrix=M, verts=bm.verts)
        C.parenter(C.piece(nom, bm, MATS, lissage="tout", pivot=M @ racine), baleine)

    bm = bmesh.new()
    for s in (-1, 1):
        p = Vector((0.25, s * 0.165, 0.0))
        A.oeil_mignon(bm, p, Vector((0.45, s * 1.0, 0.1)), 0.05, 3, 4, hauteur=1.25)
    bmesh.ops.transform(bm, matrix=M, verts=bm.verts)
    C.parenter(C.piece("Anim_Yeux", bm, MATS, lissage="tout", pivot=M @ Vector((0.3, 0, 0.0))),
               baleine)

    # traînée d'étoiles modelée : 7 étoiles de plus en plus petites sur une courbe en S
    # derrière la queue (elle suit la queue ; le code peut la faire scintiller en décalé,
    # en plus des 30 particules de Fx_Emetteur)
    bm = bmesh.new()
    for k in range(7):
        t = (k + 1) / 7
        p = Vector((-0.45 - 0.75 * t, 0.12 * math.sin(t * 5.0), 0.10 + 0.22 * t * t))
        r = 0.055 * (1.0 - 0.6 * t)
        C.bm_etoile(bm, r, 0.02, C.repere(p, Vector((0, -1, 0)).lerp(Vector((0, 0, 1)), 0.3)),
                    mat=2, bombe=0.01, branches=4, creux=0.4)
    bmesh.ops.transform(bm, matrix=M, verts=bm.verts)
    queue_obj = next(o for o in baleine.children if o.name == "Anim_Queue")
    C.parenter(C.piece("Anim_Traine", bm, MATS, lissage="angle", pivot=M @ Vector((-0.45, 0, 0.1))),
               queue_obj)

    bm, centres = etoiles(arbre)
    bmesh.ops.transform(bm, matrix=M, verts=bm.verts)
    C.parenter(C.piece("Etoiles", bm, MATS, lissage="angle", pivot=centre), baleine)

    eff = C.reperes_effets([M @ (centres[i] + Vector((0, 0, 0.03))) for i in (0, 3, 6)],
                           fx=M @ Vector((-0.55, 0, 0)))
    evt = C.vide("Fx_Event", M @ Vector((0.17, 0.0, 0.24)))
    evt.empty_display_size = 0.05
    for o in eff + [evt]:
        C.parenter(o, baleine)
    return C.racine_v5(NOM, POINT, [traj])


if __name__ == "__main__":
    A.finir_animal(construire(), NOM, POINT, y_min=0.45)
