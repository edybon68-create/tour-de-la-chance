"""
boite_cadeau.py — Les boîtes cadeaux en 3D (animation d'ouverture v5), d'après les icônes
art/icones/boite_skins_1.png et boite_accessoires_1.png (bible §2.8).

Lancer (Blender fermé, ou en tout cas pas besoin de l'ouvrir) :
    /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/boite_cadeau.py
Une seule boîte :
    BOITES="boite_skins_1" /Applications/Blender.app/.../Blender --background --python ...
Sorties : blender/exports/<id>.fbx (dossier d'attente) et docs/captures/v5_<id>.png
(à gauche fermée, à droite ouverte : on vérifie que les pivots tournent bien).

UN SCRIPT, PLUSIEURS BOÎTES
    Tout ce qui change d'une boîte à l'autre est dans le tableau BOITES (en bas) : couleurs,
    matières (métal, brillance), emblème du médaillon et forme. Pour une nouvelle boîte
    (« Pirates », « Animaux »…), on ajoute une ligne, on relance, c'est tout.
    - `forme` : "cadeau" (couvercle plat, ruban en croix, gros nœud) ou "coffre" (couvercle
      bombé de coffre de pirate, deux sangles, pas de nœud, médaillon = serrure) ;
    - `embleme` : un nom du tableau EMBLEMES ("goutte", "haut_de_forme", "etoile", "patte").
      Pour un nouvel emblème, on écrit une petite fonction qui le dessine et on l'ajoute au
      tableau : le reste du script ne change pas.

HIÉRARCHIE (les noms sont ceux que le code Unity cherche), coordonnées Unity :
    <id>                 racine, origine au pied de la boîte, au centre
    ├── Corps            la boîte, CREUSE (on voit l'intérieur quand le couvercle s'ouvre),
    │                    fond intérieur lumineux (lueur dorée, la même pour tous les tirages)
    ├── Ruban            le ruban du corps (passe dessous, remonte sur les 4 faces)
    ├── Medaillon        pivot au centre de son dos, posé sur le ruban, face avant
    │   └── Embleme      pivot au centre de la face du médaillon (interchangeable)
    ├── Couvercle        pivot sur l'ARÊTE ARRIÈRE DU BAS (la charnière) : rotation X locale
    │   │                POSITIVE dans Unity (ex. +110°) = il s'ouvre vers l'arrière ;
    │   │                une translation en Y le fait sauter
    │   ├── RubanCouvercle  le ruban du couvercle (suit le couvercle)
    │   └── Noeud        pivot au centre du nœud : il peut s'envoler seul (forme "cadeau")
    └── PointSortie      objet vide au milieu du creux intérieur : d'où sort l'objet gagné

Low-poly moderne : arrondis réels (bevel bmesh), ~1 500 triangles par boîte.
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
# Mesures communes (mètres). Avant de la boîte = +Y Blender (= vers la caméra dans Unity).
# ---------------------------------------------------------------------------------------
L, P, H = 0.80, 0.80, 0.60      # corps : largeur, profondeur, hauteur
R_CORPS = 0.055                  # rayon des arrondis du corps
MARGE_COUV = 0.04                # le couvercle déborde de 4 cm de chaque côté
H_COUV = 0.22                    # hauteur du couvercle
RECOUVRE = 0.05                  # le couvercle descend de 5 cm sur le corps
H_BORD = 0.075                   # bande foncée en bas du couvercle (le « bord »)
R_COUV = 0.05
PAROI = 0.035                    # épaisseur des parois du corps creux
# Profondeur du creux : un « double fond » à 25 cm sous le bord. Pourquoi pas jusqu'en bas ?
# Vue d'en haut à ~35° (caméra du jeu), un creux de 55 cm cache entièrement son fond : on ne
# verrait jamais la lueur. Avec 25 cm, la lueur dorée apparaît dès que le couvercle s'ouvre.
CREUX = 0.25
RUBAN_L, RUBAN_E = 0.16, 0.022   # ruban : largeur, épaisseur

LC, PC = L + 2 * MARGE_COUV, P + 2 * MARGE_COUV   # couvercle : largeur, profondeur
Z_COUV = H - RECOUVRE                             # bas du couvercle
Z_HAUT = Z_COUV + H_COUV                          # dessus du couvercle


# ---------------------------------------------------------------------------------------
# Matériaux : on les crée à la volée à partir des paramètres de la boîte.
# ---------------------------------------------------------------------------------------
def matiere(nom, spec):
    """Enregistre le matériau `nom` dans la palette commune. `spec` = "#hex" ou
    (hex, metallic, roughness[, émission])."""
    if isinstance(spec, str):
        spec = (spec, 0.0, 0.45)
    C.MATERIAUX[nom] = tuple(spec)
    return nom


# ---------------------------------------------------------------------------------------
# Briques
# ---------------------------------------------------------------------------------------
def bm_boite_arrondie(bm, taille, centre, rayon, segments=3, mat=0):
    """Boîte aux arêtes arrondies (le Bevel est fait directement dans le bmesh)."""
    verts = C.bm_boite(bm, taille, centre, mat)
    aretes = list({e for v in verts for e in v.link_edges})
    bmesh.ops.bevel(bm, geom=verts + aretes, offset=rayon, offset_type="OFFSET",
                    segments=segments, profile=0.5, affect="EDGES", clamp_overlap=True)
    return verts


def couper(bm, z):
    """Coupe tout le maillage par le plan horizontal z (ajoute une rangée d'arêtes) :
    sert à peindre une bande d'une autre couleur avec des bords nets."""
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=(0, 0, z), plane_no=(0, 0, 1))


def ruban_profil(profil, decalage, axe="Y", x_ou_y=0.0, mat=0, bm=None, ecart=0.0):
    """Fait passer un ruban sur un profil 2D (liste de (a, z, na, nz) : point et normale
    vers l'extérieur dans le plan vertical). axe="Y" : le profil est dans le plan YZ (le
    ruban va de l'avant vers l'arrière) ; axe="X" : plan XZ (de gauche à droite)."""
    chemin, hauts = [], []
    for a, z, na, nz in profil:
        n = Vector((0, na, nz)) if axe == "Y" else Vector((na, 0, nz))
        base = Vector((x_ou_y, a, z)) if axe == "Y" else Vector((a, x_ou_y, z))
        q = base + n * (decalage + ecart)
        # Sous la boîte, le ruban rentre dans le fond : la boîte repose bien sur son
        # origine (y = 0 dans Unity) et pas sur 2 cm de ruban.
        q.z = max(q.z, RUBAN_E / 2 + 0.001)
        chemin.append(q)
        hauts.append(n)
    s = [(-RUBAN_L / 2, -RUBAN_E / 2), (RUBAN_L / 2, -RUBAN_E / 2),
         (RUBAN_L / 2 - 0.006, RUBAN_E / 2), (-RUBAN_L / 2 + 0.006, RUBAN_E / 2)]
    C.bm_balayage_surface(bm, chemin, hauts, s, mat=mat)


def profil_boite(demi, z_bas, z_haut, rayon, coins=("haut",), pas=3, depart_bas=None):
    """Profil (a, z, na, nz) d'une boîte arrondie vue de côté, d'un bord à l'autre :
    face avant (a = +demi) -> coin(s) -> dessus ou dessous -> coin(s) -> face arrière.
    coins=("haut",) : on passe par-dessus (couvercle) ; ("bas",) : par-dessous (corps)."""
    pts = []
    if "haut" in coins:
        pts.append((demi, z_bas, 1, 0))
        cz, z_ext = z_haut - rayon, 1
    else:
        pts.append((demi, z_haut, 1, 0))
        cz, z_ext = z_bas + rayon, -1
    ca = demi - rayon
    for k in range(pas + 1):
        ang = (math.pi / 2) * k / pas
        na, nz = math.cos(ang), math.sin(ang) * z_ext
        pts.append((ca + rayon * na, cz + rayon * nz, na, nz))
    for k in range(pas + 1):
        ang = math.pi / 2 + (math.pi / 2) * k / pas
        na, nz = math.cos(ang), math.sin(ang) * z_ext
        pts.append((-ca + rayon * na, cz + rayon * nz, na, nz))
    pts.append((-demi, z_bas if "haut" in coins else z_haut, -1, 0))
    return pts


# ---------------------------------------------------------------------------------------
# Emblèmes : chacun dessine dans le repère du médaillon (origine au centre de sa face,
# avant = +Y, haut = +Z, rayon utile 0,09) et renvoie la liste de ses matériaux.
# ---------------------------------------------------------------------------------------
def embleme_goutte(bm, couleurs):
    """Goutte de peinture en 3 bandes (rouge, vert, bleu du haut vers le bas)."""
    c, r, pointe = Vector((0, 0, -0.018)), 0.05, 0.072
    d = pointe - c.z
    a_t = math.acos(r / d)                       # angle des points de tangence
    contour = [(0.0, pointe)]
    a0, a1 = math.pi / 2 + a_t, math.pi / 2 - a_t + 2 * math.pi
    for k in range(13):
        a = a0 + (a1 - a0) * k / 12
        contour.append((r * math.cos(a), c.z + r * math.sin(a)))
    C.bm_prisme(bm, contour, 0.026, axe="Y", decalage=-0.004)
    couper(bm, 0.024)
    couper(bm, -0.022)
    for f in bm.faces:
        z = f.calc_center_median().z
        f.material_index = 0 if z > 0.024 else (1 if z > -0.022 else 2)
    return couleurs or [("#D64545", 0.0, 0.35), ("#3CC48A", 0.0, 0.35), ("#4A90D9", 0.0, 0.35)]


def embleme_haut_de_forme(bm, couleurs):
    """Mini haut-de-forme noir à ruban rouge, debout, vu de face."""
    y = 0.026
    calotte = [(0.0, -0.032), (0.04, -0.032), (0.042, 0.04), (0.038, 0.046), (0.0, 0.046)]
    v_av = set(bm.verts)
    C.bm_revolution(bm, calotte, 12)
    rebord = [(0.0, -0.046), (0.07, -0.046), (0.075, -0.039), (0.07, -0.032), (0.0, -0.032)]
    C.bm_revolution(bm, rebord, 14)
    bande = [(0.0, -0.032), (0.0445, -0.032), (0.0445, -0.014), (0.0, -0.014)]
    v_b = set(bm.verts)
    C.bm_revolution(bm, bande, 12, mats=[1, 1, 1])
    nouveaux = [v for v in bm.verts if v not in v_av]
    # Le rebord est aplati d'avant en arrière (sinon il sortirait trop du médaillon).
    bmesh.ops.transform(bm, matrix=Matrix.Translation((0, y, 0)) @ Matrix.Diagonal((1, 0.55, 1, 1)),
                        verts=nouveaux)
    for f in bm.faces:
        if not all(v in v_b for v in f.verts) and f.material_index != 1:
            f.material_index = 0
    return couleurs or [("#1E1E1E", 0.0, 0.6), ("#C0392B", 0.0, 0.45)]


def embleme_etoile(bm, couleurs):
    """Étoile à 5 branches (exemple pour d'autres boîtes)."""
    contour = []
    for k in range(10):
        a = math.pi / 2 + math.pi * k / 5
        r = 0.075 if k % 2 == 0 else 0.032
        contour.append((r * math.cos(a), r * math.sin(a)))
    C.bm_prisme(bm, contour, 0.024, axe="Y", decalage=-0.004)
    return couleurs or [("#FFD54A", 0.6, 0.3)]


def embleme_patte(bm, couleurs):
    """Empreinte de patte (exemple pour les boîtes « Animaux ») : un coussinet, 4 doigts."""
    C.bm_plaque(bm, C.contour_ellipse(0.04, 0.032, n=12), 0.022,
                Matrix.Translation((0, 0.007, -0.025)))
    for cx, cz in ((-0.05, 0.012), (-0.019, 0.042), (0.019, 0.042), (0.05, 0.012)):
        C.bm_plaque(bm, C.contour_ellipse(0.017, 0.021, n=8), 0.022,
                    Matrix.Translation((cx, 0.007, cz)))
    return couleurs or [("#3A2416", 0.0, 0.6)]


EMBLEMES = {
    "goutte": embleme_goutte,
    "haut_de_forme": embleme_haut_de_forme,
    "etoile": embleme_etoile,
    "patte": embleme_patte,
    # Boîtes Animaux : l'os doré est dessiné dans commun_animaux.py (pour ne pas toucher au
    # reste de ce fichier, partagé avec les boîtes Pirates).
    "os": lambda bm, couleurs: __import__("commun_animaux").embleme_os(bm, couleurs),
    # Boîtes Pirates : l'ancre et le gouvernail sont dessinés dans commun_pirates.py (même
    # raison : on n'ajoute ici qu'une entrée par emblème).
    "ancre": lambda bm, couleurs: __import__("commun_pirates").embleme_ancre(bm, couleurs),
    "gouvernail": lambda bm, couleurs: __import__("commun_pirates").embleme_gouvernail(bm, couleurs),
}


# ---------------------------------------------------------------------------------------
# Les pièces de la boîte
# ---------------------------------------------------------------------------------------
def corps(mats):
    """Corps creux : boîte arrondie dont le dessus est creusé (parois de 3,5 cm), fond
    intérieur lumineux. mats = [corps, intérieur, lueur]."""
    bm = bmesh.new()
    bm_boite_arrondie(bm, (L, P, H), (0, 0, H / 2), R_CORPS, segments=3, mat=0)
    dessus = max(bm.faces, key=lambda f: (f.normal.z > 0.99, f.calc_center_median().z))
    res = bmesh.ops.inset_region(bm, faces=[dessus], thickness=PAROI, depth=0.0)
    ext = bmesh.ops.extrude_face_region(bm, geom=[dessus])
    # bmesh garde la face d'origine (elle boucherait le trou) : on l'enlève.
    bmesh.ops.delete(bm, geom=[dessus], context="FACES_ONLY")
    nv = [g for g in ext["geom"] if isinstance(g, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=nv, vec=(0, 0, -CREUX))
    fond = [g for g in ext["geom"] if isinstance(g, bmesh.types.BMFace)]
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    for f in bm.faces:
        c = f.calc_center_median()
        if abs(c.x) < L / 2 - PAROI - 0.01 and abs(c.y) < P / 2 - PAROI - 0.01 and 0.005 < c.z < H - 0.01:
            f.material_index = 1                 # parois intérieures
    for f in fond:
        f.material_index = 2                     # fond lumineux
    for f in res["faces"]:
        f.material_index = 0
    o = C.objet_depuis_bmesh("Corps", bm, mats)
    C.lisser_angle(o, 35)
    return o


def couvercle_cadeau(bm):
    bm_boite_arrondie(bm, (LC, PC, H_COUV), (0, 0, Z_COUV + H_COUV / 2), R_COUV, segments=3)
    couper(bm, Z_COUV + H_BORD)
    for f in bm.faces:
        if f.calc_center_median().z < Z_COUV + H_BORD:
            f.material_index = 1


def profil_arche(n=8):
    """Profil (y, z) du couvercle de coffre : côtés droits puis une arche bombée."""
    pts = [(PC / 2, Z_COUV), (PC / 2, Z_COUV + H_BORD + 0.02)]
    z0 = Z_COUV + H_BORD + 0.02
    for k in range(1, n):
        a = math.pi * k / n
        pts.append((PC / 2 * math.cos(a), z0 + (H_COUV + 0.08) * math.sin(a)))
    pts += [(-PC / 2, Z_COUV + H_BORD + 0.02), (-PC / 2, Z_COUV)]
    return pts


def couvercle_coffre(bm):
    contour = profil_arche()
    verts = C.bm_prisme(bm, contour[::-1], LC, axe="X", decalage=-LC / 2)
    aretes = [e for e in bm.edges if abs(abs(e.verts[0].co.x) - LC / 2) < 1e-4
              and abs(abs(e.verts[1].co.x) - LC / 2) < 1e-4]
    bmesh.ops.bevel(bm, geom=aretes, offset=0.035, offset_type="OFFSET", segments=2,
                    profile=0.5, affect="EDGES", clamp_overlap=True)
    couper(bm, Z_COUV + H_BORD)
    for f in bm.faces:
        f.material_index = 1 if f.calc_center_median().z < Z_COUV + H_BORD else 0


def profil_coffre_ruban():
    """Profil (y, z, ny, nz) de l'arche (pour les sangles du coffre)."""
    pts = profil_arche()
    res = []
    for i, (y, z) in enumerate(pts):
        a = pts[max(i - 1, 0)]
        b = pts[min(i + 1, len(pts) - 1)]
        t = Vector((b[0] - a[0], b[1] - a[1])).normalized()
        n = Vector((t.y, -t.x))
        if n.dot(Vector((y, z - Z_COUV))) < 0:
            n = -n
        res.append((y, z, n.x, n.y))
    return res


def noeud(mats):
    """Gros nœud : un nœud central arrondi et deux boucles de ruban plat."""
    bm = bmesh.new()
    centre = Vector((0, 0, Z_HAUT + 0.045))
    bm_boite_arrondie(bm, (0.14, 0.13, 0.10), centre, 0.035, segments=2)
    section = [(-0.05, -0.011), (0.05, -0.011), (0.046, 0.011), (-0.046, 0.011)]
    for cote in (-1, 1):
        # Boucle = ellipse penchée dans le plan XZ, qui part du nœud vers l'extérieur.
        chemin, hauts = [], []
        n = 12
        c_l = Vector((cote * 0.15, 0, Z_HAUT + 0.12))
        incl = math.radians(28) * cote
        for k in range(n):
            a = 2 * math.pi * k / n
            u, v = 0.13 * math.cos(a), 0.085 * math.sin(a)
            # rotation de l'ellipse (elle remonte vers l'extérieur)
            x = u * math.cos(incl) - v * math.sin(incl)
            z = u * math.sin(incl) + v * math.cos(incl)
            p = c_l + Vector((x, 0, z))
            chemin.append(p)
            nrm = Vector((math.cos(a) / 0.13, 0, math.sin(a) / 0.085))
            nrm = Vector((nrm.x * math.cos(incl) - nrm.z * math.sin(incl), 0,
                          nrm.x * math.sin(incl) + nrm.z * math.cos(incl))).normalized()
            hauts.append(nrm)
        C.bm_balayage_surface(bm, chemin, hauts, section, mat=0, ferme=True)
    o = C.objet_depuis_bmesh("Noeud", bm, mats)
    C.lisser_angle(o, 50)
    C.deplacer_origine(o, centre)
    return o


def medaillon(mats, z, y_face):
    """Médaillon rond en relief (bord relevé, centre creusé), face vers l'avant (+Y).
    Renvoie (objet, profondeur de la face où se pose l'emblème)."""
    bm = bmesh.new()
    profil = [(0.0, 0.0), (0.145, 0.0), (0.147, 0.03), (0.132, 0.046), (0.113, 0.046),
              (0.10, 0.03), (0.0, 0.03)]
    C.bm_revolution(bm, profil, 20)
    bmesh.ops.transform(bm, matrix=Matrix.Translation((0, y_face, z))
                        @ Matrix.Rotation(math.radians(-90), 4, "X"), verts=bm.verts)
    o = C.objet_depuis_bmesh("Medaillon", bm, mats)
    C.lisser_angle(o, 40)
    C.deplacer_origine(o, (0, y_face, z))
    return o, 0.03


def embleme(nom, couleurs, prefixe):
    bm = bmesh.new()
    specs = EMBLEMES[nom](bm, couleurs)
    mats = [matiere(f"M_{prefixe}_Embleme{i + 1}", s) for i, s in enumerate(specs)]
    o = C.objet_depuis_bmesh("Embleme", bm, mats)
    C.lisser_angle(o, 40)
    return o


# ---------------------------------------------------------------------------------------
# Assemblage
# ---------------------------------------------------------------------------------------
def construire(id_, p):
    C.nouvelle_scene()
    pre = p.get("prefixe", id_)
    m_corps = matiere(f"M_{pre}_Corps", p["corps"])
    m_bord = matiere(f"M_{pre}_Bord", p["bord"])
    m_int = matiere(f"M_{pre}_Interieur", p.get("interieur", p["bord"]))
    m_lueur = matiere(f"M_{pre}_Lueur", p.get("lueur", ("#FFD98A", 0.0, 0.5, 0.6)))
    m_ruban = matiere(f"M_{pre}_Ruban", p["ruban"])
    m_med = matiere(f"M_{pre}_Medaillon", p.get("medaillon", p["ruban"]))
    forme = p.get("forme", "cadeau")

    racine = C.vide(id_)
    o_corps = corps([m_corps, m_int, m_lueur])

    # Ruban du corps : passe dessous et remonte jusque sous le couvercle.
    bm = bmesh.new()
    z_top = Z_COUV + 0.01
    if forme == "cadeau":
        ruban_profil(profil_boite(P / 2, 0.0, z_top, R_CORPS, coins=("bas",)), RUBAN_E / 2 - 0.004,
                     axe="Y", x_ou_y=0.0, bm=bm)
        ruban_profil(profil_boite(L / 2, 0.0, z_top, R_CORPS, coins=("bas",)), RUBAN_E / 2 - 0.001,
                     axe="X", x_ou_y=0.0, bm=bm)
    else:  # coffre : deux sangles d'avant en arrière
        for x in (-0.24, 0.24):
            ruban_profil(profil_boite(P / 2, 0.0, z_top, R_CORPS, coins=("bas",)),
                         RUBAN_E / 2 - 0.004, axe="Y", x_ou_y=x, bm=bm)
    o_ruban = C.objet_depuis_bmesh("Ruban", bm, [m_ruban])
    C.lisser_angle(o_ruban, 40)

    # Médaillon sur le ruban avant (au milieu du corps ; serrure en haut pour le coffre).
    z_med = H * 0.47 if forme == "cadeau" else Z_COUV - 0.14
    y_face = P / 2 + RUBAN_E - 0.004
    o_med, prof = medaillon([m_med], z_med, y_face)
    o_emb = embleme(p["embleme"], p.get("couleurs_embleme"), pre)
    o_emb.location = (0, y_face + prof, z_med)

    # Couvercle (pivot = charnière, arête arrière du bas).
    bm = bmesh.new()
    if forme == "cadeau":
        couvercle_cadeau(bm)
    else:
        couvercle_coffre(bm)
    o_couv = C.objet_depuis_bmesh("Couvercle", bm, [m_corps, m_bord])
    C.lisser_angle(o_couv, 35)
    C.deplacer_origine(o_couv, (0, -PC / 2, Z_COUV))

    bm = bmesh.new()
    if forme == "cadeau":
        ruban_profil(profil_boite(PC / 2, Z_COUV - 0.002, Z_HAUT, R_COUV), RUBAN_E / 2 - 0.004,
                     axe="Y", x_ou_y=0.0, bm=bm)
        ruban_profil(profil_boite(LC / 2, Z_COUV - 0.002, Z_HAUT, R_COUV), RUBAN_E / 2 - 0.001,
                     axe="X", x_ou_y=0.0, bm=bm)
    else:
        for x in (-0.24, 0.24):
            ruban_profil(profil_coffre_ruban(), RUBAN_E / 2 - 0.004, axe="Y", x_ou_y=x, bm=bm)
    o_rc = C.objet_depuis_bmesh("RubanCouvercle", bm, [m_ruban])
    C.lisser_angle(o_rc, 40)
    C.deplacer_origine(o_rc, (0, -PC / 2, Z_COUV))   # même pivot que le couvercle

    enfants_couv = [o_rc]
    if p.get("noeud", forme == "cadeau"):
        enfants_couv.append(noeud([m_ruban]))

    point = C.vide("PointSortie", (0, 0, H - CREUX / 2))   # milieu du creux

    for o in (o_corps, o_ruban, o_med, o_couv, point):
        C.parenter(o, racine)
    C.parenter(o_emb, o_med)
    for o in enfants_couv:
        C.parenter(o, o_couv)
    return racine


def nettoyer_rendu():
    """Retire caméra et lumières d'un rendu (pour en refaire un autre proprement)."""
    for o in list(bpy.data.objects):
        if o.type in {"CAMERA", "LIGHT"}:
            bpy.data.objects.remove(o, do_unlink=True)


def miniature(racine, id_):
    """Deux rendus côte à côte : fermée, puis ouverte (couvercle tourné de 110° vers
    l'arrière autour de sa charnière), pour vérifier les pivots. Écrit docs/captures/v5_<id>.png."""
    import numpy as np
    couv = bpy.data.objects["Couvercle"]
    chemins = []
    direction = (0.55, 1.0, 0.65)
    chemins.append(C.rendre_miniature(racine, f"_tmp_{id_}_ferme.png", direction=direction))
    nettoyer_rendu()
    couv.rotation_euler.x = math.radians(110)
    chemins.append(C.rendre_miniature(racine, f"_tmp_{id_}_ouvert.png", direction=direction))
    nettoyer_rendu()
    couv.rotation_euler.x = 0.0
    bpy.context.view_layer.update()
    imgs = []
    for ch in chemins:
        img = bpy.data.images.load(ch)
        w, h = img.size
        imgs.append(np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4))
        bpy.data.images.remove(img)
        os.remove(ch)
    grille = np.concatenate(imgs, axis=1)
    out = bpy.data.images.new("Planche", grille.shape[1], grille.shape[0], alpha=True)
    out.pixels = grille.ravel()
    chemin = os.path.join(C.DOSSIER_CAPTURES, f"v5_{id_}.png")
    out.filepath_raw = chemin
    out.file_format = "PNG"
    out.save()
    print("Miniature :", chemin)


# ---------------------------------------------------------------------------------------
# LES BOÎTES. Couleurs : bible §2.8. ruban/medaillon : (hex, metallic, roughness).
# ---------------------------------------------------------------------------------------
LAITON = ("#C99A1E", 1.0, 0.40)   # = M_Laiton de la bible (§4)
BOITES = {
    "boite_skins_1": {
        "corps": ("#8E5BD6", 0.0, 0.45), "bord": ("#6A4C9C", 0.0, 0.45),
        "ruban": LAITON, "embleme": "goutte", "forme": "cadeau",
    },
    "boite_accessoires_1": {
        "corps": ("#2BB8C4", 0.0, 0.45), "bord": ("#1F8A94", 0.0, 0.45),
        "ruban": LAITON, "embleme": "haut_de_forme", "forme": "cadeau",
    },
    # Boîtes Animaux (v5.md §17.1) : « panier » d'osier à ruban vert feuille et patte rose
    # (skins) ; boîte vert sapin à ruban et nœud dorés, os doré (accessoires).
    "boite_skins_animaux": {
        "corps": ("#C99A6A", 0.0, 0.70), "bord": ("#A8784A", 0.0, 0.70),
        "ruban": ("#4CC26A", 0.0, 0.50), "medaillon": ("#E8B830", 1.0, 0.20),
        "embleme": "patte", "couleurs_embleme": [("#FF8FB0", 0.0, 0.30)], "forme": "cadeau",
    },
    "boite_accessoires_animaux": {
        "corps": ("#2E6B4A", 0.0, 0.45), "bord": ("#1F4E36", 0.0, 0.45),
        "ruban": ("#E8B830", 1.0, 0.25), "medaillon": ("#F4EEE6", 0.0, 0.30),
        "embleme": "os", "forme": "cadeau",
    },
    # Boîtes Pirates (v5.md §17.1) : caisse-coffre en bois sombre ; skins = sangles de
    # laiton, cadenas doré, ancre gravée ; accessoires = même caisse, sangles et bord du
    # couvercle bleu marine (le bleu des foulards du catalogue), gouvernail gravé.
    "boite_skins_pirates": {
        "corps": ("#5A3A28", 0.0, 0.65), "bord": ("#3A2416", 0.0, 0.70),
        "ruban": LAITON, "medaillon": ("#E8B830", 1.0, 0.20),
        "embleme": "ancre", "forme": "coffre",
    },
    "boite_accessoires_pirates": {
        "corps": ("#5A3A28", 0.0, 0.65), "bord": ("#1F3A6E", 0.0, 0.50),
        "interieur": ("#3A2416", 0.0, 0.70),
        "ruban": ("#1F3A6E", 0.0, 0.50), "medaillon": ("#E8B830", 1.0, 0.20),
        "embleme": "gouvernail", "forme": "coffre",
    },
}
# Démonstrations (rendu seulement, pas d'export) : prouvent que le script sait faire un
# coffre de pirate ou changer d'emblème. Couleurs provisoires, à choisir par le DA.
DEMOS = {
    "demo_coffre_pirate": {
        "corps": ("#8C5933", 0.0, 0.7), "bord": ("#6B4226", 0.0, 0.7),
        "ruban": ("#4A4E5A", 0.8, 0.5), "medaillon": LAITON,
        "embleme": "etoile", "forme": "coffre",
    },
    "demo_boite_animaux": {
        "corps": ("#7BC67B", 0.0, 0.45), "bord": ("#4E9A55", 0.0, 0.45),
        "ruban": LAITON, "embleme": "patte", "couleurs_embleme": [("#3A2416", 0.0, 0.6)],
    },
}


def main():
    choix = os.environ.get("BOITES", "").split() or list(BOITES) + list(DEMOS)
    for id_ in choix:
        demo = id_ in DEMOS
        p = DEMOS[id_] if demo else BOITES[id_]
        racine = construire(id_, p)
        total = C.rapport(racine)
        miniature(racine, id_)
        if not demo:
            chemin = C.exporter_fbx(racine, f"{id_}.fbx", dossier=C.DOSSIER_EXPORTS)
            C.verifier_fbx(chemin)
        print(f"RESUME {id_} triangles={total}")


if __name__ == "__main__":
    main()
