"""
personnage_cheval.py — Le personnage version « cavalier d'échecs » : une tête et un cou de
cheval en bois peint, posés sur le socle rond d'un pion. Exporte en FBX.

Lancer (Blender fermé, ou en tout cas pas besoin de l'ouvrir) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/personnage_cheval.py

Sortie : blender/exports/personnage_cheval.fbx (dossier d'attente, voir commun.DOSSIER_EXPORTS)
et la miniature docs/captures/cheval_3_4.png.

Hiérarchie produite (mêmes noms que la capsule : le code Unity retrouve tout par son nom) :
    Joueur              origine à la base, au centre (0 ; 0 ; 0)
    ├── Corps           socle + cou + tête + museau + oreilles + crinière + toupet :
    │                   UN SEUL matériau (M_PersoCorps, repeint par le shop), UV cylindriques
    ├── OeilGauche / OeilDroit       yeux noirs brillants, sur le devant du front
    ├── RefletGauche / RefletDroit   points blancs dans les yeux
    ├── Narines         deux petites narines sombres au bout du museau (ne changent pas de couleur)
    ├── PointChapeau    sommet du crâne, entre les oreilles
    ├── PointVisage     devant les yeux (lunettes, masque)
    ├── PointCou        centre du cou, là où l'on noue une écharpe / un collier
    └── PointDos        derrière le cou (ailes, planète)

Pourquoi le cheval regarde la caméra (et pas de profil comme un vrai cavalier d'échecs) ?
    De profil, on ne verrait qu'UN œil depuis la caméra du jeu, et les lunettes ou le masque
    n'auraient pas de sens. Face à la caméra, les deux yeux sont lisibles et le museau qui
    avance donne quand même la silhouette « cheval ». Les rendus vue jeu ont tranché.

Pourquoi le Corps est fait de plusieurs volumes qui se chevauchent ?
    Un jouet en bois est souvent assemblé de pièces (socle tourné, tête sculptée, crinière
    découpée). Garder des volumes simples qui s'emboîtent est plus facile à régler qu'un seul
    maillage « sculpté », et les arêtes vives aux jonctions renforcent l'aspect jouet.
    Ils sont FUSIONNÉS en un seul objet `Corps`, avec un seul matériau : le shop repeint tout.
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

# Coordonnées Blender dans tout ce fichier : X = gauche de l'image (= −x Unity),
# Y = AVANT (vers la caméra, = −z Unity), Z = haut (= y Unity). Voir commun.py.

# ---------------------------------------------------------------------------------------
# Mesures (en mètres). Les modifier ici puis relancer le script.
# ---------------------------------------------------------------------------------------
RAYON_SOCLE = 0.525           # socle de pion Ø 1,05, comme la base de la capsule
SEGMENTS_SOCLE = 18

# Cou : anneaux horizontaux (hauteur z, avancée y du centre, demi-largeur, demi-profondeur).
# Le cou recule un peu en montant : la tête qui avance garde l'équilibre, comme un cavalier.
ANNEAUX_COU = [
    (0.28, 0.00, 0.34, 0.34),   # caché dans le socle
    (0.45, 0.05, 0.37, 0.39),   # poitrail bombé vers l'avant
    (0.65, 0.07, 0.35, 0.385),
    (0.85, 0.04, 0.315, 0.345),
    (1.05, -0.02, 0.28, 0.305),
    (1.25, -0.06, 0.265, 0.29), # le plus fin : sous la mâchoire
    (1.45, -0.07, 0.28, 0.29),  # caché dans la tête
]
SEGMENTS_COU = 16

# Crâne et museau : « boîtes arrondies » (superellipsoïdes). Exposant < 1 = plus carré.
CRANE_CENTRE = Vector((0.0, -0.03, 1.58))
CRANE_DEMI = Vector((0.31, 0.30, 0.28))
MUSEAU_CENTRE = Vector((0.0, 0.32, 1.38))
MUSEAU_DEMI = Vector((0.205, 0.33, 0.205))
MUSEAU_PENTE = 18.0           # degrés : le nez pique un peu vers le bas, air doux
ARRONDI = 0.72

# Yeux : sur le devant du front, au-dessus du museau, écartés comme ceux de la capsule.
OEIL_X = 0.165                # Unity x = ±0,165
OEIL_Z = 1.68                 # Unity y
OEIL_TAILLE = (0.085, 0.032, 0.105)  # demi-axes : 17 cm × 21 cm, 6 cm d'épaisseur

NARINE_X = 0.085
NARINE_Z = 1.32

# Où le cheval regarde : 0 = droit vers la caméra. Pour essayer un autre angle sans
# toucher au fichier : CHEVAL_REGARD=28 Blender --background --python ... (TOUT le
# personnage tourne, points d'accroche compris ; testé à 28° : moins lisible, on garde 0).
REGARD_DEGRES = float(os.environ.get("CHEVAL_REGARD", "0"))


def sp(t, e):
    """Puissance « signée » : garde le signe de t. Sert à arrondir une sphère en boîte."""
    return math.copysign(abs(t) ** e, t)


def bm_superellipsoide(bm, centre, demi, e=ARRONDI, segments=12, anneaux=8, rot=None):
    """Boîte arrondie : sphère dont chaque coordonnée est « poussée » vers les coins
    (puissance e < 1), puis étirée aux demi-tailles `demi`, tournée et placée."""
    verts = C.bm_sphere(bm, 1.0, (0, 0, 0), segments=segments, anneaux=anneaux)
    m = Matrix.Translation(centre) @ (rot or Matrix.Identity(4))
    for v in verts:
        p = Vector((sp(v.co.x, e) * demi.x, sp(v.co.y, e) * demi.y, sp(v.co.z, e) * demi.z))
        v.co = m @ p
    return verts


def profil_socle():
    """Profil (rayon, hauteur) du socle de pion, du centre du dessous vers le haut :
    pied large arrondi, gorge creuse, petite bague, puis le départ du cou."""
    R = RAYON_SOCLE
    return [(0.0, 0.0), (R - 0.025, 0.0), (R, 0.035), (R, 0.08), (R - 0.04, 0.125),
            (0.42, 0.17), (0.41, 0.21), (0.445, 0.24), (0.425, 0.285), (0.0, 0.285)]


def anneau_cou(z, yc, wx, wy, n=SEGMENTS_COU, e=0.85):
    """Anneau horizontal un peu carré (superellipse) ; le premier point est à l'arrière."""
    pts = []
    for k in range(n):
        a = -math.pi / 2 + 2 * math.pi * k / n
        c, s = math.cos(a), math.sin(a)
        pts.append(Vector((sp(c, e) * wx, yc + sp(s, e) * wy, z)))
    return pts


def chemin_criniere():
    """Ligne du dos (dans le plan x = 0), du sommet de la tête vers le bas du cou."""
    return [Vector((0, y, z)) for y, z in (
        (-0.08, 1.885), (-0.21, 1.85), (-0.315, 1.75), (-0.36, 1.55), (-0.37, 1.32),
        (-0.355, 1.08), (-0.335, 0.85), (-0.33, 0.62), (-0.30, 0.42))]


def bm_criniere(bm, epaisseur=0.16, lobes=6, par_lobe=3):
    """Crinière « découpée dans une planche » : une bande épaisse qui suit le dos, bord
    extérieur festonné en `lobes` mèches arrondies, bord intérieur caché dans le corps."""
    ligne = chemin_criniere()
    # Rééchantillonne la ligne à pas régulier (une mèche = `par_lobe` pas).
    longueurs = [0.0]
    for a, b in zip(ligne, ligne[1:]):
        longueurs.append(longueurs[-1] + (b - a).length)
    total = longueurs[-1]
    n = lobes * par_lobe
    interieur, exterieur = [], []
    for k in range(n + 1):
        d = total * k / n
        i = max(j for j in range(len(ligne) - 1) if longueurs[j] <= d + 1e-9)
        i = min(i, len(ligne) - 2)
        t = (d - longueurs[i]) / (longueurs[i + 1] - longueurs[i])
        p = ligne[i].lerp(ligne[i + 1], t)
        tg = (ligne[i + 1] - ligne[i]).normalized()
        normale = Vector((0, tg.z, -tg.y))        # vers l'extérieur (arrière / haut)
        phase = (k % par_lobe) / par_lobe            # 0 = creux entre deux mèches
        bosse = math.sin(math.pi * phase) if k % par_lobe else 0.0
        fin = 1.0 - 0.45 * (k / n) ** 2              # les mèches rapetissent vers le bas
        interieur.append(p - normale * 0.07)
        exterieur.append((p + normale * (0.05 + 0.10 * bosse) * fin, normale))
    h = epaisseur / 2
    anneaux = []
    # Section en 5 points : le bord extérieur est chanfreiné (aspect bois poncé).
    for pi_, (pe, nrm) in zip(interieur, exterieur):
        anneaux.append([pi_ + Vector((h, 0, 0)), pe - nrm * 0.03 + Vector((h, 0, 0)), pe,
                        pe - nrm * 0.03 + Vector((-h, 0, 0)), pi_ + Vector((-h, 0, 0))])
    rangs = C.bm_anneaux(bm, anneaux)
    for r in (rangs[0], rangs[-1]):
        bm.faces.new(r)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])


def bm_oreille(bm, cote):
    """Oreille en feuille pointue (cône aplati), un peu écartée et penchée en arrière."""
    profil = [(0.0, 0.31), (0.055, 0.22), (0.085, 0.10), (0.075, 0.0), (0.0, -0.05)]
    verts = []
    avant = set(bm.verts)
    C.bm_revolution(bm, profil, 6)
    verts = [v for v in bm.verts if v not in avant]
    m = (Matrix.Translation((cote * 0.175, -0.10, 1.76))
         @ Matrix.Rotation(math.radians(cote * -14), 4, "Y")    # s'écarte vers l'extérieur
         @ Matrix.Rotation(math.radians(-14), 4, "X")           # penche vers l'arrière
         @ Matrix.Diagonal((1.0, 0.6, 1.0, 1.0)))               # aplatie d'avant en arrière
    bmesh.ops.transform(bm, matrix=m, verts=verts)


def uv_cylindriques(obj, hauteur):
    """Projection cylindrique autour de l'axe vertical (bible §6.2) :
    U fait le tour (0,5 pile devant, couture à l'arrière que la caméra ne voit pas),
    V va de la base (0) au sommet (1). Les motifs du shop (rayures, pois) s'y appuient."""
    me = obj.data
    uv = me.uv_layers.new(name="UVMap")
    for poly in me.polygons:
        us, vs = [], []
        for li in poly.loop_indices:
            co = me.vertices[me.loops[li].vertex_index].co
            if abs(co.x) < 1e-6 and abs(co.y) < 1e-6:
                us.append(None)                       # sommet sur l'axe : U décidé après
            else:
                us.append(0.5 + math.atan2(co.x, co.y) / (2 * math.pi))
            vs.append(co.z / hauteur)
        connus = [u for u in us if u is not None]
        # Face à cheval sur la couture (U saute de 1 à 0) : on « déroule » ses U.
        if connus and max(connus) - min(connus) > 0.5:
            us = [None if u is None else (u + 1.0 if u < 0.5 else u) for u in us]
            connus = [u for u in us if u is not None]
        moyenne = sum(connus) / len(connus) if connus else 0.5
        for li, u, v in zip(poly.loop_indices, us, vs):
            uv.data[li].uv = (moyenne if u is None else u, v)


def corps():
    """Construit l'objet Corps (un seul maillage, un seul matériau)."""
    pieces = []

    bm = bmesh.new()
    C.bm_revolution(bm, profil_socle(), SEGMENTS_SOCLE, angle_depart=math.pi / 2)
    pieces.append(C.objet_depuis_bmesh("Socle", bm, ["M_PersoCorps"]))

    bm = bmesh.new()
    rangs = C.bm_anneaux(bm, [anneau_cou(*a) for a in ANNEAUX_COU])
    bm.faces.new(rangs[0][::-1])
    bm.faces.new(rangs[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    pieces.append(C.objet_depuis_bmesh("Cou", bm, ["M_PersoCorps"]))

    bm = bmesh.new()
    bm_superellipsoide(bm, CRANE_CENTRE, CRANE_DEMI, segments=14, anneaux=8)
    rot = Matrix.Rotation(math.radians(MUSEAU_PENTE), 4, "X").inverted()
    bm_superellipsoide(bm, MUSEAU_CENTRE, MUSEAU_DEMI, segments=12, anneaux=8, rot=rot)
    # Toupet : petite mèche sur le front, entre les oreilles.
    rot_t = Matrix.Rotation(math.radians(-32), 4, "X")
    bm_superellipsoide(bm, Vector((0, 0.12, 1.84)), Vector((0.095, 0.15, 0.055)), e=1.0,
                       segments=10, anneaux=5, rot=rot_t)
    for cote in (1, -1):
        bm_oreille(bm, cote)
    pieces.append(C.objet_depuis_bmesh("Tete", bm, ["M_PersoCorps"]))

    bm = bmesh.new()
    bm_criniere(bm)
    pieces.append(C.objet_depuis_bmesh("Criniere", bm, ["M_PersoCorps"]))

    obj = C.joindre(pieces, "Corps")
    bpy.context.view_layer.update()
    hauteur = max(v.co.z for v in obj.data.vertices)
    uv_cylindriques(obj, hauteur)
    C.lisser_angle(obj, 60)  # rond partout, mais arêtes nettes aux jonctions des pièces
    return obj, hauteur


def lancer_rayon(arbre, depart, direction):
    hit, normale, _, _ = arbre.ray_cast(depart, direction)
    return hit, normale


def oeil(arbre, nom, x_blender):
    """Œil ovale noir à moitié enfoncé dans le front, orienté selon la surface touchée
    par un rayon lancé depuis l'avant. Renvoie (oeil, reflet)."""
    p, n = lancer_rayon(arbre, Vector((x_blender, 3.0, OEIL_Z)), Vector((0, -1, 0)))
    # On adoucit la normale vers l'avant : les deux yeux regardent la caméra, pas les côtés.
    n = (n.normalized() + Vector((0, 1.0, 0))).normalized()
    rot = n.to_track_quat("Y", "Z").to_matrix().to_4x4()
    bm = bmesh.new()
    C.bm_sphere(bm, 1.0, (0, 0, 0), segments=10, anneaux=5, echelle=OEIL_TAILLE)
    bmesh.ops.transform(bm, matrix=Matrix.Translation(p) @ rot, verts=bm.verts)
    o = C.objet_depuis_bmesh(nom, bm, ["M_Yeux"])
    C.deplacer_origine(o, p)
    C.lisser_tout(o)

    # Reflet blanc (4 cm) en haut à gauche de l'œil, les deux du même côté (lumière).
    local = Vector((0.032, OEIL_TAILLE[1] * 0.85, 0.045))
    centre = Matrix.Translation(p) @ rot @ local
    bm = bmesh.new()
    C.bm_sphere(bm, 1.0, (0, 0, 0), segments=6, anneaux=4, echelle=(0.022, 0.01, 0.022))
    bmesh.ops.transform(bm, matrix=Matrix.Translation(centre) @ rot, verts=bm.verts)
    r = C.objet_depuis_bmesh(nom.replace("Oeil", "Reflet"), bm, ["M_Reflet"])
    C.deplacer_origine(r, centre)
    C.lisser_tout(r)
    return o, r, p


def narines(arbre):
    """Deux narines ovales sombres au bout du museau (un seul objet `Narines`, matériau des
    yeux) : c'est le petit détail qui fait dire « cheval » et pas « canard »."""
    bm = bmesh.new()
    for cote in (1, -1):
        p, n = lancer_rayon(arbre, Vector((cote * NARINE_X, 3.0, NARINE_Z)), Vector((0, -1, 0)))
        rot = n.normalized().to_track_quat("Y", "Z").to_matrix().to_4x4()
        penche = Matrix.Rotation(math.radians(cote * 20), 4, "Y")   # en « virgule »
        v = C.bm_sphere(bm, 1.0, (0, 0, 0), segments=8, anneaux=4, echelle=(0.03, 0.012, 0.045))
        bmesh.ops.transform(bm, matrix=Matrix.Translation(p) @ rot @ penche, verts=v)
    o = C.objet_depuis_bmesh("Narines", bm, ["M_Yeux"])
    C.lisser_tout(o)
    return o


def mesurer_accroches(arbre):
    """Mesure les 4 points d'accroche SUR le modèle (rayons lancés vers le Corps)."""
    pts = {}
    # Chapeau : le dessus du crâne, à la verticale de son centre (entre les oreilles).
    # Un rayon toucherait le toupet, la crinière ou une oreille : on prend le sommet de la
    # boîte arrondie du crâne (sa hauteur est exacte au centre).
    pts["PointChapeau"] = Vector((0, CRANE_CENTRE.y, round(CRANE_CENTRE.z + CRANE_DEMI.z, 3)))
    # Visage : au milieu des deux yeux, sur la surface du front.
    z = OEIL_Z
    p, _ = lancer_rayon(arbre, Vector((0, 3.0, z)), Vector((0, -1, 0)))
    pts["PointVisage"] = Vector((0, round(p.y, 3), z))
    # Cou : centre de la section du cou, sous la mâchoire.
    z_cou = 1.05
    av, _ = lancer_rayon(arbre, Vector((0, 3.0, z_cou)), Vector((0, -1, 0)))
    # Rayon arrière décalé de 12 cm sur le côté : il passe à côté de la crinière.
    ar, _ = lancer_rayon(arbre, Vector((0.12, -3.0, z_cou)), Vector((0, 1, 0)))
    cx, _ = lancer_rayon(arbre, Vector((3.0, (av.y + ar.y) / 2, z_cou)), Vector((-1, 0, 0)))
    pts["PointCou"] = Vector((0, round((av.y + ar.y) / 2, 3), z_cou))
    rayons_cou = (round(av.y - (av.y + ar.y) / 2, 3), round(cx.x, 3))
    # Dos : surface arrière du cou (sous la crinière on vise le corps : rayon décalé de
    # côté puis ramené au centre), à hauteur d'épaules.
    z_dos = 0.95
    cote, _ = lancer_rayon(arbre, Vector((0.12, -3.0, z_dos)), Vector((0, 1, 0)))
    pts["PointDos"] = Vector((0, round(cote.y, 3), z_dos))
    return pts, rayons_cou


def construire():
    C.nouvelle_scene()
    racine = C.vide("Joueur")
    obj, hauteur = corps()

    bpy.context.view_layer.update()
    depsgraph = bpy.context.evaluated_depsgraph_get()
    arbre = BVHTree.FromObject(obj, depsgraph)

    # « Gauche » = côté x négatif dans Unity = x positif dans Blender (voir commun.py).
    oeil_g, reflet_g, _ = oeil(arbre, "OeilGauche", +OEIL_X)
    oeil_d, reflet_d, _ = oeil(arbre, "OeilDroit", -OEIL_X)
    accroches, rayons_cou = mesurer_accroches(arbre)

    enfants = [obj, oeil_g, oeil_d, reflet_g, reflet_d, narines(arbre)]
    for nom, p in accroches.items():
        enfants.append(C.vide(nom, p))
    for o in enfants:
        C.parenter(o, racine)

    if REGARD_DEGRES:
        racine.rotation_euler = (0, 0, math.radians(REGARD_DEGRES))
    bpy.context.view_layer.update()

    print("\n=== Points d'accroche (Unity, locaux à Joueur) ===")
    for nom in accroches:
        p = bpy.data.objects[nom].matrix_world.translation
        print(f"  {nom:<13} ({-p.x:+.3f} ; {p.z:+.3f} ; {-p.y:+.3f})")
    print(f"  hauteur totale = {hauteur:.3f}   cou : demi-profondeur, demi-largeur = {rayons_cou}")
    return racine


if __name__ == "__main__":
    racine = construire()
    C.rapport(racine)
    # 1) Vue « face » = exactement la caméra du jeu, cheval posé sur son estrade.
    racine.location = C.depuis_unity(*C.CHEVAL_POSITION)
    socle = C.estrade()
    C.rendre_camera_jeu(C.descendants(racine) + [socle], "cheval_face.png", taille=720)
    bpy.data.objects.remove(socle, do_unlink=True)
    racine.location = (0, 0, 0)
    # 2) Vue de 3/4 bien marquée (45°) : on y voit le profil du cavalier et la crinière.
    C.rendre_miniature(racine, "cheval_3_4.png", direction=(1.0, 0.9, 0.55))
    chemin = C.exporter_fbx(racine, "personnage_cheval.fbx", dossier=C.DOSSIER_EXPORTS)
    C.verifier_fbx(chemin)
