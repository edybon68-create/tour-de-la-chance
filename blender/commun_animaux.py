"""
commun_animaux.py — Outils partagés par les scripts des objets des « Boîtes Animaux » (v5).

Pourquoi un fichier à part, au lieu d'ajouter ces fonctions à commun.py ?
    Un autre artiste fabrique EN MÊME TEMPS les objets Pirates et utilise commun.py. Si nous
    modifiions tous les deux ce fichier, l'un écraserait le travail de l'autre. Ici, on
    « complète » commun.py sans le toucher : on importe commun, puis on ajoute nos matières
    (préfixe M5_, noms bien distincts pour ne jamais tomber sur un nom des Pirates) et nos
    briques de forme (yeux mignons, ellipsoïdes, gouttes de flamme, colliers qui épousent le cou).

Utilisation dans un script d'objet :
    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    import commun as C
    import commun_animaux as A

Références : docs/specs/v5-catalogue.md §3 et §4 (fiches Animaux), docs/art-bible.md §13
(formes à trois niveaux, biseaux, matières M5_, éléments Anim_* / Eclat_* / Fx_Emetteur).
"""

import math
import os
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

# ---------------------------------------------------------------------------------------
# 1. Matières des objets Animaux (bible §13.4 : roughness Blender = 1 − smoothness Unity)
# ---------------------------------------------------------------------------------------
# nom : (couleur, metallic, roughness[, émission]). Émission SEULEMENT pour le Légendaire
# (bocal) et les Secrets (baleine, lion), et ≤ 0,5 sur une petite surface au Super rare.
C.MATERIAUX.update({
    # communs à plusieurs objets
    "M5_AnimNoirVerni": ("#1F1730", 0.0, 0.10),      # pupilles, truffes : « noir » de la bible
    "M5_AnimRoseMat": ("#FF9FC8", 0.0, 0.70),        # intérieur des oreilles, petits nez
    # oreilles de chat
    "M5_SerreTeteNoir": ("#2A2438", 0.0, 0.45),
    "M5_OreilleChat": ("#2A2438", 0.0, 0.85),
    # oreilles de lapin
    "M5_LapinBlanc": ("#F4EEE6", 0.0, 0.85),
    "M5_SerreTeteBlanc": ("#F4EEE6", 0.0, 0.30),
    # truffe
    "M5_TruffeNoire": ("#2A2438", 0.0, 0.10),
    "M5_TruffeNarine": ("#1F1730", 0.0, 0.70),
    # collier à médaille
    "M5_CuirRouge": ("#D64545", 0.0, 0.50),
    # moustaches
    "M5_Moustache": ("#F4EEE6", 0.0, 0.45),
    "M5_NezRose": ("#FF9FC8", 0.0, 0.15),
    # bonnet grenouille
    "M5_TissuGrenouille": ("#4CC26A", 0.0, 0.85),
    # lunettes hibou
    "M5_MontureHibou": ("#8C5933", 0.0, 0.45),
    "M5_PlumeHibou": ("#F28C38", 0.0, 0.85),
    "M5_VerreAmbre": ("#F2D49A", 0.0, 0.05),
    # écharpe renard
    "M5_FourrureRenard": ("#E8732A", 0.0, 0.85),
    "M5_FourrureBlanche": ("#F4EEE6", 0.0, 0.85),
    # carapace
    "M5_Carapace": ("#5A8A3A", 0.0, 0.25),
    "M5_BordCarapace": ("#C8D86A", 0.0, 0.55),
    "M5_SangleBrune": ("#8C5933", 0.0, 0.60),
    # ailes de chauve-souris
    "M5_OsChauveSouris": ("#3A2858", 0.0, 0.25),
    "M5_MembraneChauveSouris": ("#6A4A98", 0.0, 0.65),
    # bois de cerf
    "M5_BoisCerf": ("#B89060", 0.0, 0.50),
    "M5_PetaleCerf": ("#FF8FB0", 0.0, 0.70),
    "M5_FeuilleVerte": ("#4CC26A", 0.0, 0.40),
    # poussin
    "M5_DuvetPoussin": ("#FFE070", 0.0, 0.80),
    "M5_BecOrange": ("#F28C38", 0.0, 0.15),
    "M5_CoquilleOeuf": ("#F4EEE6", 0.0, 0.20),
    # chaton dans son panier
    "M5_Osier": ("#C99A6A", 0.0, 0.70),
    "M5_PelageChaton": ("#9AA0B0", 0.0, 0.85),
    "M5_YeuxChaton": ("#7FD8A0", 0.0, 0.05, 0.4),     # yeux verts qui luisent (Super rare)
    # abeilles et pot de miel
    "M5_AbeilleJaune": ("#FFC83A", 0.0, 0.35),
    "M5_AbeilleBrune": ("#2A1F1A", 0.0, 0.55),
    "M5_AileAbeille": ("#E8F4FF", 0.0, 0.10),         # transparente (voir TRANSPARENTS)
    "M5_PotMiel": ("#F4EEE6", 0.0, 0.30),
    "M5_Miel": ("#FFA81C", 0.0, 0.05, 0.4),           # miel « pierre précieuse » qui luit
    # bocal
    "M5_VerreBocal": ("#DDF4FF", 0.0, 0.05),          # transparent 0,3
    "M5_EauBocal": ("#5FB8F0", 0.0, 0.10, 0.5),       # transparente 0,55, luit 0,5
    "M5_PoissonRouge": ("#FF7A1C", 0.0, 0.25),
    "M5_Algue": ("#4CC26A", 0.0, 0.50),
    # baleine céleste
    "M5_BaleineDos": ("#2E3A8A", 0.0, 0.30),
    "M5_BaleineVentre": ("#BFD4FF", 0.0, 0.45),
    "M5_EtoileBaleine": ("#FFF1D6", 0.0, 0.20, 1.2),
    # roue du paon (extras du skin animal_paon)
    "M5_PlumePaon": ("#1E6EB0", 0.35, 0.20),
    "M5_OcelleOr": ("#FFC83A", 0.0, 0.20, 0.8),
    "M5_OcelleVert": ("#2E9E5B", 0.0, 0.20, 0.8),
    # crinière du lion solaire (extras du skin animal_lion_soleil)
    # (3 matières au plus pour des extras, catalogue §0.2 : le disque en prend une, les
    # flammes passent donc du jaune à l'orange en 2 bandes, sans 3e bande rouge)
    "M5_FlammeBase": ("#FFF1A8", 0.0, 0.30, 1.2),
    "M5_FlammeOrange": ("#FF7A1C", 0.0, 0.30, 0.8),
    "M5_DisqueSolaire": ("#FFF1A8", 0.0, 0.30, 0.5),  # transparent 0,25
})

# Opacité des matières transparentes (Unity : Surface Type = Transparent, alpha = valeur).
TRANSPARENTS = {
    "M5_AileAbeille": 0.45,
    "M5_VerreBocal": 0.30,
    "M5_EauBocal": 0.55,
    "M5_DisqueSolaire": 0.25,
}


def appliquer_transparence():
    """Règle l'opacité des matières transparentes déjà créées (le FBX la transporte ; dans
    Unity le programmeur met Surface Type = Transparent, comme pour le skin Cristal)."""
    for mat in bpy.data.materials:
        if mat.name in TRANSPARENTS:
            bsdf = mat.node_tree.nodes.get("Principled BSDF")
            bsdf.inputs["Alpha"].default_value = TRANSPARENTS[mat.name]
            try:
                mat.surface_render_method = "BLENDED"
            except AttributeError:
                mat.blend_method = "BLEND"


# ---------------------------------------------------------------------------------------
# 2. Le cheval (surface mesurée par rayons) et ses repères
# ---------------------------------------------------------------------------------------
_ARBRE = None


def arbre():
    """BVH du corps du cheval (chargé une seule fois par script)."""
    global _ARBRE
    if _ARBRE is None:
        _ARBRE = C.arbre_cheval()
    return _ARBRE


def surface(centre_unity, angle, elevation, ecart, pente=0.5, fenetre=(0.2, 0.14), pas=0.07):
    """Raccourci de C.sur_cheval_lisse : point Blender sur la peau du cheval (lissée)."""
    return C.sur_cheval_lisse(arbre(), centre_unity, angle, elevation, ecart, pente=pente,
                              fenetre=fenetre, pas=pas)


# Mesures du cheval (personnage_cheval.py), en Unity local à Joueur :
OREILLE_X = 0.175          # base des oreilles, x = ±0,175, z = +0,10, y = 1,76 ; pointe
OREILLE_POINTE = (0.25, 2.05, 0.175)   # vers (±0,25 ; 2,05 ; 0,175)
MUSEAU_BOUT = (0.0, 1.30, -0.63)       # bout du museau (devant les narines)
NARINES = (0.085, 1.32)                # narines en x = ±0,085, y = 1,32


def sur_museau(x_unity, y_unity, ecart=0.0):
    """Point du devant du museau (rayon lancé depuis la caméra vers l'arrière) en
    (x, y) Unity, décollé de `ecart` le long de la normale. Renvoie (point, normale)."""
    depart = C.U(x_unity, y_unity, -3.0)
    hit, n, _, _ = arbre().ray_cast(depart, Vector((0, -1, 0)))
    return hit + n * ecart, n


def racine_joueur(nom, enfants):
    """Racine d'EXTRAS de skin : origine = la base du cheval (enfant direct de Joueur,
    position et rotation locales nulles, échelle 1)."""
    r = C.vide(nom, (0, 0, 0))
    for e in enfants:
        if e.parent is None:
            C.parenter(e, r)
    return r


# ---------------------------------------------------------------------------------------
# 3. Briques de forme
# ---------------------------------------------------------------------------------------
def matrice_vers(origine, direction, haut=Vector((0, 0, 1))):
    """Matrice qui envoie l'axe +Z local sur `direction` (et +Y local au plus près de
    `haut` projeté), origine en `origine`. Pour poser une pièce construite « debout »."""
    z = Vector(direction).normalized()
    h = Vector(haut)
    if abs(z.dot(h.normalized())) > 0.98:
        h = Vector((0, 1, 0)) if abs(z.y) < 0.9 else Vector((1, 0, 0))
    x = h.cross(z).normalized()
    y = z.cross(x)
    return Matrix.Translation(Vector(origine)) @ Matrix((x, y, z)).transposed().to_4x4()


def bm_ellipsoide(bm, centre, demi, segments=10, anneaux=7, mat=0, rot=None):
    """Ellipsoïde (demi-axes `demi`) : corps d'animaux, têtes, joues, yeux."""
    v = C.bm_sphere(bm, 1.0, (0, 0, 0), segments=segments, anneaux=anneaux, mat=mat)
    m = Matrix.Translation(Vector(centre)) @ (rot or Matrix.Identity(4)) \
        @ Matrix.Diagonal((*demi, 1.0))
    bmesh.ops.transform(bm, matrix=m, verts=v)
    return v


def bm_goutte(bm, longueur, rayon, segments=8, bandes=((1.0, 0),), courbe=1.0,
              matrice=None, aplati=1.0, pas=(0.0, 0.08, 0.2, 0.35, 0.5, 0.65, 0.8, 0.92)):
    """Goutte pointue debout le long de +Z (base ronde en z = 0, pointe en z = longueur) :
    mèche de flamme, plume, pétale, épine. `bandes` = ((fraction de hauteur, matériau), …)
    pour peindre des bandes (jaune à la base, rouge en pointe). `courbe` > 1 : plus
    renflée en bas. `aplati` < 1 : écrasée en Y local (une flamme « plate » de jouet)."""
    profil = [(0.0, -rayon * 0.35)]
    for t in pas:                      # `pas` = hauteurs des anneaux (moins = plus léger)
        r = rayon * math.sin(min(1.0, (t + 0.12) / 0.4) * math.pi / 2) * (1 - t) ** (0.8 * courbe)
        profil.append((max(r, 0.004), t * longueur))
    profil.append((0.0, longueur))
    mats = []
    for (r0, z0), (r1, z1) in zip(profil, profil[1:]):
        f = 0.5 * (z0 + z1) / longueur
        mats.append(next(m for lim, m in bandes if f <= lim + 1e-6) if f >= 0 else bandes[0][1])
    avant = set(bm.verts)
    C.bm_revolution(bm, profil, segments, mats=mats)
    v = [x for x in bm.verts if x not in avant]
    m = Matrix.Diagonal((1.0, aplati, 1.0, 1.0))
    if matrice is not None:
        m = matrice @ m
    bmesh.ops.transform(bm, matrix=m, verts=v)
    return v


def oeil_mignon(bm, centre, normale, rayon, mat_noir, mat_reflet, haut=Vector((0, 0, 1)),
                hauteur=1.15):
    """Gros œil brillant de mascotte : un ovale noir verni bombé + un point de reflet blanc
    en haut à gauche de l'image (bible §13.4). `normale` = vers où il regarde."""
    n = Vector(normale).normalized()
    m = matrice_vers(centre, n, haut)          # +Z local = la normale
    v = bm_ellipsoide(bm, (0, 0, 0), (rayon, rayon * hauteur, rayon * 0.5), segments=8,
                      anneaux=5, mat=mat_noir)
    bmesh.ops.transform(bm, matrix=m, verts=v)
    # reflet : vers la gauche de l'image (+X Blender) et le haut
    gauche = Vector((1, 0, 0)) - n * n.x
    gauche = gauche.normalized() if gauche.length > 1e-3 else Vector((0, 0, 1))
    p = Vector(centre) + n * rayon * 0.42 + gauche * rayon * 0.35 + Vector(haut) * rayon * 0.4
    v += C.bm_sphere(bm, rayon * 0.3, p, segments=5, anneaux=3, mat=mat_reflet)
    return v


def bm_aile_plate(bm, contour, epaisseur, matrice, mat=0):
    """Plaque bombée légère (aile, feuille, oreille) : coussin à 1 anneau."""
    return C.bm_coussin(bm, contour, epaisseur, epaisseur * 1.6, matrice, mat, anneaux=1)


def contour_goutte(largeur, hauteur, n=12, pointe_haut=True):
    """Contour 2D (x, z) d'une goutte / d'un pétale : rond en bas, pointu en haut."""
    pts = []
    for k in range(n):
        t = 2 * math.pi * k / n
        x = math.sin(t) * (0.5 + 0.5 * math.cos(t)) ** 0.9 * largeur / 2 * 1.25
        z = -math.cos(t) * hauteur / 2
        pts.append((x, z if pointe_haut else -z))
    if not pointe_haut:
        pts.reverse()
    return pts[::-1]


# ---------------------------------------------------------------------------------------
# 4. Anneaux qui épousent le cou (collier, écharpe renard) — même méthode que echarpe.py
# ---------------------------------------------------------------------------------------
def rayon_cou(a, y0, demi_hauteur, axe_z=0.02):
    """Distance de l'axe du cou à la surface la plus éloignée (lissée « tente ») sur toute
    la hauteur d'un anneau, dans la direction a (0 = devant, + = gauche de l'image)."""
    r = 0.0
    for dy in (-demi_hauteur, 0.0, demi_hauteur):
        centre = (0.0, y0 + dy, axe_z)
        p, d = C.sur_cheval_lisse(arbre(), centre, a, 0.0, 0.0, pente=0.6,
                                  fenetre=(0.42, 0.0), pas=0.14)
        r = max(r, (p - C.depuis_unity(*centre)).dot(d))
    return r


def anneau_cou(bm, y_milieu, pente, section, segments=24, mat_fn=None, axe_z=0.02,
               bosse_fn=None):
    """Anneau autour du cou : plus bas devant (−pente), plus haut derrière (+pente).
    `section` = [(épaisseur vers l'extérieur, hauteur)] (premier/dernier point un peu SOUS
    la peau : aucun jour). `mat_fn(i)` = matériau de la tranche i. `bosse_fn(a, j)` =
    gonflement en plus (fourrure). Renvoie une fonction a -> (point du bord extérieur
    au milieu, direction) pour poser des détails (boucle, tête de renard…)."""
    demi_h = max(abs(z) for _, z in section)
    infos = {}
    rangs = []
    for i in range(segments):
        a = 2 * math.pi * i / segments
        d = C.direction_angles(a)
        y0 = y_milieu - pente * math.cos(a)
        r = rayon_cou(a, y0, demi_h, axe_z)
        axe = C.depuis_unity(0.0, y0, axe_z)
        infos[i] = (axe, d, r)
        rang = []
        for j, (dr, dz) in enumerate(section):
            extra = bosse_fn(a, j) if bosse_fn else 0.0
            rang.append(bm.verts.new(axe + d * (r + dr + (extra if dr > 0 else 0.0))
                                     + Vector((0, 0, dz))))
        rangs.append(rang)
    m = len(section)
    faces = []
    for i in range(segments):
        A_, B_ = rangs[i], rangs[(i + 1) % segments]
        for s in range(m):
            u = (s + 1) % m
            f = bm.faces.new((A_[s], B_[s], B_[u], A_[u]))
            f.material_index = mat_fn(i) if mat_fn else 0
            faces.append(f)
    bmesh.ops.recalc_face_normals(bm, faces=faces)
    ep = max(dr for dr, _ in section)

    def bord(a):
        d = C.direction_angles(a)
        y0 = y_milieu - pente * math.cos(a)
        r = rayon_cou(a, y0, demi_h, axe_z)
        return C.depuis_unity(0.0, y0, axe_z) + d * (r + ep), d
    return bord


# ---------------------------------------------------------------------------------------
# 5. Fin des scripts et réglages Unity
# ---------------------------------------------------------------------------------------
def finir_animal(racine, nom, point, y_min=1.0):
    """Transparence, puis la fin commune v5 (rapport, rendu porté, export FBX dans
    blender/exports/, vérification)."""
    appliquer_transparence()
    return C.finir_v5(racine, nom, point, y_min=y_min)


# Réglages Unity de chaque objet : (point d'accroche, attachScale, attachOffset, dossier).
# Tous les modèles sont construits SUR le cheval : origine = point d'accroche, donc échelle 1
# et décalage nul. Les extras de skins sont des enfants directs de « Joueur ».
REGLAGES = {
    "oreilles_chat":       ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "oreilles_lapin":      ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "truffe":              ("PointVisage", 1.0, (0.0, 0.0, 0.0), "exports"),
    "collier_medaille":    ("PointCou", 1.0, (0.0, 0.0, 0.0), "exports"),
    "moustaches":          ("PointVisage", 1.0, (0.0, 0.0, 0.0), "exports"),
    "bonnet_grenouille":   ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "lunettes_hibou":      ("PointVisage", 1.0, (0.0, 0.0, 0.0), "exports"),
    "echarpe_renard":      ("PointCou", 1.0, (0.0, 0.0, 0.0), "exports"),
    "carapace":            ("PointDos", 1.0, (0.0, 0.0, 0.0), "exports"),
    "ailes_chauve_souris": ("PointDos", 1.0, (0.0, 0.0, 0.0), "exports"),
    "bois_cerf":           ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "poussin":             ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "chaton_sac":          ("PointDos", 1.0, (0.0, 0.0, 0.0), "exports"),
    "ruche":               ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "bocal_poisson":       ("PointChapeau", 1.0, (0.0, 0.0, 0.0), "exports"),
    "baleine_celeste":     ("PointDos", 1.0, (0.0, 0.0, 0.0), "exports"),
    "animal_paon_extras":  ("Joueur", 1.0, (0.0, 0.0, 0.0), "exports"),
    "animal_lion_soleil_extras": ("Joueur", 1.0, (0.0, 0.0, 0.0), "exports"),
}


# ---------------------------------------------------------------------------------------
# 6. Emblème « os » des boîtes 3D Animaux (utilisé par boite_cadeau.py, tableau EMBLEMES)
# ---------------------------------------------------------------------------------------
def embleme_os(bm, couleurs):
    """Os doré penché de 30°, dans le repère du médaillon (origine au centre de sa face,
    avant = +Y, haut = +Z, rayon utile 0,09). Même épaisseur que l'emblème « patte »."""
    rot = Matrix.Rotation(math.radians(30), 4, "Y")
    C.bm_plaque(bm, C.contour_rectangle_arrondi(0.10, 0.032, 0.012, par_coin=2), 0.022,
                rot @ Matrix.Translation((0, 0.007, 0)))
    for sx in (-1, 1):
        for sz in (-1, 1):
            C.bm_plaque(bm, C.contour_ellipse(0.022, 0.022, n=10), 0.022,
                        rot @ Matrix.Translation((sx * 0.056, 0.007, sz * 0.019)))
    return couleurs or [("#E8B830", 1.0, 0.2)]
