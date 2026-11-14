"""
pirate_perroquet.py — Perroquet (boîte d'accessoires Pirates : `perroquet`, LÉGENDAIRE, Dos).
Fiche : docs/specs/v5-catalogue.md §2 n° 09 ; règles : art-bible §13.

Lancer (Blender fermé) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/pirate_perroquet.py
Sortie : blender/exports/perroquet.fbx et docs/captures/v5_cheval_perroquet.png.

Un ara rouge perché sur le garrot, côté DROIT de l'image (Unity x ≈ +0,30), un peu en
arrière du cou : il dépasse bien de la silhouette vue de face, regarde vers la caméra et
ne touche ni le foulard ni la boussole (emplacement Cou, plus bas et devant). Ses pattes
s'enfoncent de 1 cm dans le cheval (surface mesurée par un rayon) : il est bien posé.

Les trois niveaux de forme (bible §13.2) :
  - primaire   : le corps en goutte et la grosse tête ronde (0,68 m de haut en tout, ×1,35 :
                 exagéré, c'est une récompense Légendaire) ;
  - secondaire : les ailes bleues à épaule jaune (en deux couches), la longue queue de
                 3 plumes, le gros bec crème recourbé (le détail-signature) ;
  - tertiaire  : masque blanc autour des yeux, gros yeux vernis avec 2 reflets, 3 plumes
                 de huppe, pattes qui agrippent.
Matières (5, budget Légendaire de la bible §13.7) : rouge, bleu, jaune (plumes satinées
0,55), crème verni (bec, masque, reflets des yeux, 0,85), noir verni (yeux, griffes).

Hiérarchie (le code Unity retrouve les pièces par leur nom) :
    perroquet           vide, origine = PointDos (attachScale 1, attachOffset 0)
    └── Corps           corps + pattes ; origine = entre les pattes (le perchoir)
        ├── Anim_Tete   tête, bec, yeux, huppe ; origine = le cou. Tourne autour de Y
        │   │           (Unity) vers le coffre survolé ; se tasse après un piège
        │   ├── Anim_PaupiereGauche  vides au centre de chaque œil : le code écrase l'œil
        │   └── Anim_PaupiereDroite  en Y (échelle 1 -> 0,1 -> 1 en 0,15 s) pour cligner
        ├── Anim_AileGauche   ailes ; origine = l'épaule. Battent au saut de joie (±60°,
        ├── Anim_AileDroite   4 battements en 0,8 s, son `perroquet_cri`)
        ├── Anim_Queue  la queue ; origine = sa base (ressort A)
        ├── Eclat_1..3  vides : bec, épaule, queue (1 éclat / 1,5 s, émission 1,2)
        └── Fx_Emetteur vide : au-dessus de la tête (plumes / étoiles au saut de joie, ≤ 10)
NB : les yeux sont des MAILLAGES dans Anim_Tete (matière noire) ; pour cligner, le code
peut aussi simplement écraser `Anim_OeilGauche` / `Anim_OeilDroit` (objets séparés).
"""

import math
import os
import sys

import bmesh
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import commun_pirates as P  # noqa: E402

NOM = "perroquet"
POINT = "PointDos"
MATS = ["M5_PirAraRouge", "M5_PirAraBleu", "M5_PirAraJaune", "M5_PirBecCreme",
        "M5_PirOeilNoir"]   # 0..4
ANGLE_PERCHOIR = math.radians(-102)    # vers la droite de l'image (V7 : moins en arrière, on le voit)
Y_PERCHOIR = 0.95
ECHELLE = 1.55      # V7 : 1,35 -> 1,55, il était caché par la tête (exagéré, bible §13.2)


def grandir(racine, centre, k):
    """Agrandit une hiérarchie de pièces autour de `centre` (sans échelle sur les objets :
    la géométrie est agrandie, chaque pivot est éloigné du centre du même facteur)."""
    import bpy
    bpy.context.view_layer.update()
    objs = C.descendants(racine)
    cibles = {o.name: centre + (o.matrix_world.translation - centre) * k for o in objs}
    for o in objs:
        if o.type == "MESH":
            o.data.transform(Matrix.Scale(k, 4))
    for o in objs:                       # parents d'abord (ordre de descendants)
        m = o.matrix_world.copy()
        m.translation = cibles[o.name]
        o.matrix_world = m
        bpy.context.view_layer.update()


def construire():
    C.nouvelle_scene()
    p, d = C.sur_cheval_lisse(P.arbre(), (0.0, Y_PERCHOIR, P.AXE_COU_Z), ANGLE_PERCHOIR, 0.0,
                              -0.01, pente=0.6, fenetre=(0.2, 0.0), pas=0.1)
    F = p + d * 0.13                    # le perchoir : centre entre les pattes
    out = Vector((d.x, d.y, 0)).normalized()     # vers l'extérieur (droite de l'image)
    av = Vector((0, 1, 0))                         # il regarde vers l'avant (la caméra)
    cote = av.cross(Vector((0, 0, 1)))             # = −X Blender = droite de l'image

    # --- corps + pattes
    bm = bmesh.new()
    rot = Matrix.Rotation(math.radians(-14), 3, "X")    # un peu penché en arrière
    P.bm_ellipsoide(bm, F + Vector((0, 0.0, 0.17)), (0.11, 0.115, 0.17), segments=14,
                    anneaux=9, mat=0, rot=rot)   # V7 : plus de segments, plus dodu
    for s in (-1, 1):                    # pattes : 2 doigts devant, 1 derrière
        base = F + Vector((s * 0.05, 0.02, 0.04))
        for dy, dz in ((0.07, -0.03), (0.05, -0.04), (-0.06, -0.035)):
            C.bm_tube(bm, [base, base + Vector((s * 0.01, dy, dz))], 0.018, cotes=4, mat=4,
                      rayons=[1.0, 0.6])
    corps = C.piece("Corps", bm, MATS, lissage="tout", pivot=F)

    # --- tête
    cou = F + Vector((0, 0.03, 0.30))
    T = F + Vector((0, 0.05, 0.38))
    bm = bmesh.new()
    P.bm_ellipsoide(bm, T, (0.112, 0.115, 0.108), segments=14, anneaux=9, mat=0)   # V7 : grosse tête ronde (icône)
    yeux = []
    for s in (-1, 1):
        # masque blanc (une plaque bombée sur la joue) puis l'œil par-dessus
        n = Vector((s * 0.80, 0.55, 0.12)).normalized()
        pm = T + n * 0.10
        C.bm_coussin(bm, C.contour_ellipse(0.058, 0.052, n=12), 0.012, 0.03,
                     C.repere(pm, n), mat=3, anneaux=1, dos_plat=True)
        yeux.append((pm + n * 0.02, n))
    # bec : mandibule du haut recourbée (crème), mandibule du bas (noire)
    haut_bec = [T + Vector((0, 0.10, 0.02)), T + Vector((0, 0.15, 0.01)),
                T + Vector((0, 0.18, -0.03)), T + Vector((0, 0.17, -0.08))]
    C.bm_tube(bm, haut_bec, 0.055, cotes=10, mat=3, rayons=[1.0, 0.8, 0.5, 0.15])
    P.bm_ellipsoide(bm, T + Vector((0, 0.115, -0.05)), (0.035, 0.03, 0.025), segments=6,
                    anneaux=4, mat=4)
    # V7 : plus de huppe en pointes (l'icône a une tête toute ronde, plus « peluche »)
    tete = C.piece("Anim_Tete", bm, MATS, lissage="tout", pivot=cou)
    for (pe, n), nom in zip(yeux, ("Anim_OeilGauche", "Anim_OeilDroit")):
        bm = bmesh.new()
        P.oeil_mignon(bm, pe, n, 0.038, 4, 3)
        o = C.piece(nom, bm, MATS, lissage="tout", pivot=pe)
        C.parenter(o, tete)
    C.parenter(tete, corps)

    # --- ailes (2 couches : bleu dessous, épaule jaune dessus)
    for s, nom in ((1, "Anim_AileGauche"), (-1, "Anim_AileDroite")):
        epaule = F + Vector((s * 0.095, 0.0, 0.25))
        bm = bmesh.new()
        n = Vector((s, -0.15, 0.0)).normalized()
        goutte = [(0.0, 0.06), (0.05, 0.03), (0.055, -0.06), (0.035, -0.17), (0.0, -0.25),
                  (-0.03, -0.17), (-0.05, -0.06), (-0.045, 0.03)]
        m = C.repere(epaule + n * 0.015, n) @ Matrix.Rotation(math.radians(-25 * s), 4, "Y")
        C.bm_coussin(bm, goutte, 0.02, 0.05, m @ Matrix.Translation((0, 0, -0.06)), mat=1,
                     anneaux=1)
        C.bm_coussin(bm, C.contour_ellipse(0.045, 0.06, n=8), 0.02, 0.04,
                     m @ Matrix.Translation((0, 0.022, -0.01)), mat=2, anneaux=1)
        aile = C.piece(nom, bm, MATS, lissage="tout", pivot=epaule)
        C.parenter(aile, corps)

    # --- queue : 3 longues plumes (rouge au milieu, bleues autour)
    bq = F + Vector((0, -0.07, 0.06))
    bm = bmesh.new()
    for dx, mat, lg in ((0.0, 0, 0.36), (0.045, 1, 0.30), (-0.045, 1, 0.30)):
        P.bm_plume(bm, bq + Vector((dx, 0, 0)), Vector((dx * 2.0, -0.35, -1.0)), lg, 0.07,
                   courbe=Vector((0, -0.06, 0.02)), mat=mat, cotes=4, n=4,
                   haut=Vector((0, -1, 0.3)), plat=0.5)
    queue = C.piece("Anim_Queue", bm, MATS, lissage="tout", pivot=bq)
    C.parenter(queue, corps)

    eclats = [T + Vector((0, 0.15, 0.0)), F + Vector((0.1, 0.03, 0.27)),
              bq + Vector((0, -0.10, -0.25))]
    for e in C.reperes_effets(eclats, fx=T + Vector((0, 0, 0.16))):
        C.parenter(e, corps)
    grandir(corps, F, ECHELLE)
    return C.racine_v5(NOM, POINT, [corps])


if __name__ == "__main__":
    P.finir_pirate(construire(), NOM, POINT, y_min=0.45)
