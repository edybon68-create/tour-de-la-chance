"""
planche_qualite.py — V7, point 17 : planches de COMPARAISON « icône | rendu 3/4 | sur le cheval ».

L'auteur trouve que beaucoup de modèles 3D ne ressemblent pas à leur icône. Pour en juger
honnêtement, on met côte à côte, pour chaque objet :
    1. son icône détourée (art/icones_detourees/), posée sur le fond de rendu ;
    2. le modèle seul, en vue 3/4 (orthographique, on juge les formes) ;
    3. le modèle porté par le cheval, vu par la caméra du jeu (ce que voit le joueur).

Trois usages :
  a) Accessoires : on rend d'abord les vues avec revue_modeles.py (dossier « cache »), puis
         PLANCHE=accessoires CACHE=<cache> IDS="..." SORTIE=<png> Blender --background --python ...
     Les vues _1 (3/4) et _0 (jeu) de chaque objet sont reprises.
  b) Skins : le cheval est habillé de chaque skin, comme le fait PlayerAvatar dans Unity
     (couleur, métal, brillance, texture dessinée, ou motif calculé reproduit ici) :
         SKINS=1 CACHE=<cache> [IDS="animal_zebre ..."] Blender --background --python ...
     puis PLANCHE=skins CACHE=<cache> IDS="..." SORTIE=<png>.
     Les motifs calculés (SkinPatterns.cs) sont reproduits en version simplifiée : la
     comparaison porte sur les couleurs et l'allure, pas sur le pixel près.
  c) Avant / après : PLANCHE=avant_apres AVANT=<cache> APRES=<cache> IDS="..." SORTIE=<png>
     → une ligne par objet : icône | avant 3/4 | avant jeu | après 3/4 | après jeu.

Ce script ne modifie ni n'exporte aucun modèle. Données des skins : SKINS_JSON (fichier
extrait des fiches ShopItem de Unity, en lecture seule).
"""

import json
import math
import os
import sys

import bpy
import numpy as np
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402

RACINE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ICONES = os.path.join(RACINE, "art", "icones_detourees")
TEXTURES = os.path.join(RACINE, "TourDeLaChance-Unity", "Assets", "Art", "SkinsTextures")
CASE = 300
FOND = np.array([0x1F, 0x17, 0x30], np.float32) / 255.0      # fond des rendus (#1F1730)


# ------------------------------------------------------------------------- images (numpy)
def lire(chemin, taille=CASE):
    img = bpy.data.images.load(chemin)
    w, h = img.size
    px = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(px)
    bpy.data.images.remove(img)
    px = px.reshape(h, w, 4)
    # réduction par moyenne de blocs puis plus proche voisin (assez propre pour une planche)
    f = max(1, min(w, h) // taille)
    if f > 1:
        px = px[:h - h % f, :w - w % f].reshape(h // f, f, w // f, f, 4).mean(axis=(1, 3))
        h, w = px.shape[:2]
    iy = (np.arange(taille) * h / taille).astype(int)
    ix = (np.arange(taille) * w / taille).astype(int)
    return px[iy][:, ix]


def icone(id_):
    """Icône détourée posée sur le fond des rendus (prémultipliée à la main)."""
    ch = os.path.join(ICONES, id_ + ".png")
    if not os.path.exists(ch):
        return np.concatenate([np.tile(FOND * 0.6, (CASE, CASE, 1)), np.ones((CASE, CASE, 1))], 2)
    px = lire(ch)
    a = px[..., 3:4]
    rgb = px[..., :3] * a + FOND * (1 - a)
    return np.concatenate([rgb, np.ones_like(a)], axis=2).astype(np.float32)


def bande(couleur=(0.9, 0.85, 0.3), largeur=10):
    return np.ones((CASE, largeur, 4), np.float32) * np.array([*couleur, 1.0], np.float32)


def ecrire(img, sortie):
    h, w = img.shape[:2]
    out = bpy.data.images.new("Planche", w, h, alpha=True)
    out.pixels.foreach_set(np.ascontiguousarray(img, dtype=np.float32).ravel())
    out.filepath_raw = sortie
    out.file_format = "PNG"
    out.save()
    print("PLANCHE", sortie)


def assembler(lignes, sortie, par_ligne=2):
    """`lignes` : une liste de listes de cases (une par objet). On met `par_ligne` objets
    côte à côte, séparés par une bande jaune ; les objets de haut en bas."""
    rangs = []
    for i in range(0, len(lignes), par_ligne):
        morceaux = []
        for j, cases in enumerate(lignes[i:i + par_ligne]):
            if j:
                morceaux.append(bande())
            morceaux.append(np.concatenate(cases, axis=1))
        rangs.append(np.concatenate(morceaux, axis=1))
    w = max(r.shape[1] for r in rangs)
    rangs = [np.pad(r, ((0, 6), (0, w - r.shape[1]), (0, 0)), constant_values=1.0) for r in rangs]
    # Blender range les pixels de bas en haut : le premier rang va donc en dernier.
    ecrire(np.concatenate(rangs[::-1], axis=0), sortie)


def cases_cache(cache, id_, ordre=(1, 0)):
    res = []
    for k in ordre:
        ch = os.path.join(cache, f"{id_}_{k}.png")
        res.append(lire(ch) if os.path.exists(ch) else np.zeros((CASE, CASE, 4), np.float32))
    return res


# ------------------------------------------------------------------------- skins
def lisse(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def motif_calcule(d, n=512):
    """Reproduit (simplifié) SkinPatterns.cs / SkinTextures.cs pour les skins sans image.
    Renvoie (couleurs RGB n x n, émission ou None, tiling (tu, tv))."""
    v, u = np.meshgrid((np.arange(n) + 0.5) / n, (np.arange(n) + 0.5) / n, indexing="ij")
    c1, c2, c3 = (np.array(d[k][:3], np.float32) for k in ("color", "secondaryColor", "accentColor"))
    pat = int(d.get("pattern") or 0)
    style = int(d.get("skinStyle") or 0)
    cnt = int(d.get("patternCount") or 0)
    rng = np.random.default_rng(abs(hash(d["id"])) % 1000)
    img = np.broadcast_to(c1, (n, n, 3)).copy()
    em = None

    def dist(u0, v0):
        du = np.abs(u - u0)
        du = np.minimum(du, 1 - du) * 3.1
        return np.sqrt(du ** 2 + ((v - v0) * 2.0) ** 2)

    if style == 1:                                   # sucre d'orge : 8 bandes
        img[(np.floor(v * 8) % 2) == 1] = c2
    elif style == 2:                                 # pois (tuiles 10 x 7)
        fu, fv = (u * 10) % 1, (v * 7) % 1
        img[np.hypot(fu - 0.5, fv - 0.5) < 0.22] = c2
    elif style == 4:                                 # arc-en-ciel vertical (moitié visible)
        import colorsys
        for i in range(n):
            img[i, :] = colorsys.hsv_to_rgb((i / n) * 0.5, 0.75, 1.0)
        em = img * 0.5
    elif style == 5:                                 # galaxie : nuit + nébuleuses + étoiles
        base = np.array([0.05, 0.03, 0.15])
        neb = lisse(0.3, 1.0, np.sin(u * 12 + np.sin(v * 7) * 2) * np.cos(v * 9 + u * 3))
        img = base + neb[..., None] * np.array([0.45, 0.15, 0.55])
        etoiles = rng.random((n, n)) > 0.996
        img[etoiles] = 1.0
        em = img * 0.6
    elif pat == 1:                                   # bandes
        k = cnt or 8
        img[(np.floor(v * k) % 2) == 1] = c2
    elif pat == 5:                                   # points (coccinelle)
        pts = [(0.5, 0.9, 0.08)] + [(rng.random(), 0.12 + 0.63 * rng.random(),
                                     0.08 + 0.03 * rng.random()) for _ in range((cnt or 7) - 1)]
        for pu, pv, r in pts:
            img[dist(pu, pv) < r] = c2
    elif pat == 13:                                  # panda
        lv = np.where(v < 0.82, 0.82 + (v - 0.82) * 0.55, v)
        du = lambda u0: np.minimum(np.abs(u - u0), 1 - np.abs(u - u0)) * 3.1  # noqa: E731
        dk = (v < 0.2) | (v > 0.93) | (np.hypot(du(0.45), (lv - 0.82) * 2) < 0.12) | \
             (np.hypot(du(0.55), (lv - 0.82) * 2) < 0.12)
        img[dk] = c2
    elif pat == 14:                                  # dégradé
        t = lisse(0, 1, v)[..., None]
        img = c2 * (1 - t) + c1 * t
    elif pat == 8:                                   # écailles (caméléon)
        rows, cols = 16, 26
        ry = v * rows
        row = np.floor(ry)
        cx = u * cols + np.where(row % 2 == 0, 0, 0.5)
        fx, fy = cx % 1 - 0.5, ry % 1
        r = np.sqrt(fx * fx + fy * fy)
        basec = c2 * (1 - v[..., None]) + c1 * v[..., None]
        img = np.where((r <= 0.42)[..., None], basec * (0.92 + 0.08 * fy[..., None]),
                       np.where(((r > 0.42) & (r < 0.5))[..., None], basec * 0.4 + c3 * 0.6, basec * 0.82))
    elif pat == 16:                                  # ventouses (kraken)
        img = c1 * (1 - lisse(0.75, 0.98, v)[..., None]) + c2 * lisse(0.75, 0.98, v)[..., None]
        for row in range(3):
            for vv in np.arange(0.3, 0.74, 0.07):
                m = dist(0.44 + 0.06 * row, vv + 0.02 * row)
                img[m < 0.035] = c3 * 0.75
                img[m < 0.0245] = c3
    return img.astype(np.float32), (None if em is None else em.astype(np.float32)), (1, 1)


def image_numpy(nom, rgb):
    n = rgb.shape[0]
    im = bpy.data.images.new(nom, n, n, alpha=True)
    px = np.concatenate([rgb.astype(np.float32), np.ones((n, n, 1), np.float32)], axis=2)
    im.pixels.foreach_set(np.ascontiguousarray(px).ravel())
    im.colorspace_settings.name = "sRGB"
    im.pack()
    return im


def lin(c):
    c = np.asarray(c[:3], np.float32)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def materiau_skin(d):
    m = bpy.data.materials.new("Skin_" + d["id"])
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Metallic"].default_value = float(d.get("metallic") or 0)
    bsdf.inputs["Roughness"].default_value = 1.0 - float(d.get("smoothness") or 0.3) * 0.85
    style = int(d.get("skinStyle") or 0)
    bsdf.inputs["Base Color"].default_value = (*lin(d["color"]), 1)
    rgb = em = None
    if d.get("tex"):
        img = nt.nodes.new("ShaderNodeTexImage")
        img.image = bpy.data.images.load(os.path.join(TEXTURES, d["tex"] + ".png"))
        nt.links.new(img.outputs["Color"], bsdf.inputs["Base Color"])
        if d.get("texEm"):
            li = nt.nodes.new("ShaderNodeTexImage")
            li.image = bpy.data.images.load(os.path.join(TEXTURES, d["texEm"] + ".png"))
            nt.links.new(li.outputs["Color"], bsdf.inputs["Emission Color"])
            bsdf.inputs["Emission Strength"].default_value = float(d.get("emissionStrength") or 1)
    elif style in (1, 2, 4, 5) or int(d.get("pattern") or 0) in (1, 5, 8, 13, 14, 16):
        rgb, em, _ = motif_calcule(d)
        img = nt.nodes.new("ShaderNodeTexImage")
        img.image = image_numpy("Motif_" + d["id"], rgb)
        img.interpolation = "Closest" if style in (1, 2) else "Linear"
        nt.links.new(img.outputs["Color"], bsdf.inputs["Base Color"])
        if em is not None:
            li = nt.nodes.new("ShaderNodeTexImage")
            li.image = image_numpy("Lum_" + d["id"], em)
            nt.links.new(li.outputs["Color"], bsdf.inputs["Emission Color"])
            bsdf.inputs["Emission Strength"].default_value = 1.0
    es = float(d.get("emissionStrength") or 0)
    if es > 0 and not d.get("texEm") and em is None:
        bsdf.inputs["Emission Color"].default_value = (*lin(d["glowColor"]), 1)
        bsdf.inputs["Emission Strength"].default_value = es * 0.4
    if style == 3:                                   # cristal / méduse / fantôme : transparent
        bsdf.inputs["Alpha"].default_value = max(0.55, d["color"][3])
        try:
            m.surface_render_method = "BLENDED"
        except AttributeError:
            m.blend_method = "BLEND"
    return m


def rendre_skins(ids, cache, donnees):
    os.makedirs(cache, exist_ok=True)
    C.MONDE_DEGRADE = True
    C.nouvelle_scene()
    objs = C.importer_fbx(os.path.join(C.DOSSIER_MODELES, "personnage_cheval.fbx"), (0, 0, 0))
    corps = next(o for o in objs if o.name.startswith("Corps"))
    scene = bpy.context.scene
    extras_fbx = {"pirate_kraken": "pirate_kraken_extras", "pirate_abysses": "pirate_abysses_extras",
                  "animal_paon": "animal_paon_extras", "animal_lion_soleil": "animal_lion_soleil_extras"}
    for id_ in ids:
        d = donnees[id_]
        corps.data.materials.clear()
        corps.data.materials.append(materiau_skin(d))
        extras = []
        if id_ in extras_fbx:
            extras = C.importer_fbx(os.path.join(C.DOSSIER_MODELES, extras_fbx[id_] + ".fbx"))
            for mod in ("commun_pirates", "commun_animaux"):
                try:
                    __import__(mod).appliquer_transparence()
                except Exception:   # noqa: BLE001
                    pass
        tout = objs + extras
        # 1) 3/4 orthographique
        C.monde_nuit(scene, ambiance=1.0)
        C.regler_rendu(scene, CASE, CASE)
        C.lumieres_jeu(scene)
        d34 = Vector((0.75, 0.75, 0.35)).normalized()
        cd = bpy.data.cameras.new("Cam")
        cd.type = "ORTHO"
        cd.ortho_scale = 2.5
        cam = bpy.data.objects.new("Cam", cd)
        scene.collection.objects.link(cam)
        cam.location = Vector((0, 0, 1.05)) + d34 * 8
        cam.rotation_euler = (-d34).to_track_quat("-Z", "Y").to_euler()
        scene.camera = cam
        ch = os.path.join(cache, f"{id_}_1.png")
        scene.render.filepath = ch
        bpy.ops.render.render(write_still=True)
        bpy.data.objects.remove(cam)
        for o in list(scene.objects):
            if o.type == "LIGHT":
                bpy.data.objects.remove(o)
        # 2) caméra du jeu, cheval entier
        jeu = C.rendre_camera_jeu(tout, f"_tmp_skin_{id_}.png", taille=CASE)
        os.replace(jeu, os.path.join(cache, f"{id_}_0.png"))
        for o in extras:
            bpy.data.objects.remove(o, do_unlink=True)
        print("SKIN", id_)


# ------------------------------------------------------------------------- principal
def principal():
    mode = os.environ.get("PLANCHE")
    ids = os.environ.get("IDS", "").split()
    if os.environ.get("SKINS"):
        donnees = json.load(open(os.environ["SKINS_JSON"]))
        rendre_skins(ids or list(donnees), os.environ["CACHE"], donnees)
    elif mode in ("accessoires", "skins"):
        lignes = [[icone(i)] + cases_cache(os.environ["CACHE"], i) for i in ids]
        assembler(lignes, os.environ["SORTIE"], par_ligne=int(os.environ.get("PAR_LIGNE", 2)))
    elif mode == "avant_apres":
        lignes = [[icone(i)] + cases_cache(os.environ["AVANT"], i) + [bande((0.3, 0.9, 0.4), 6)]
                  + cases_cache(os.environ["APRES"], i) for i in ids]
        assembler(lignes, os.environ["SORTIE"], par_ligne=1)


if __name__ == "__main__":
    principal()
