"""
revue_modeles.py — V7, point 8 : planches AVANT / APRÈS de la revue des modèles 3D.

Pour chaque accessoire, on le pose sur le cheval EXACTEMENT comme Unity (point d'accroche,
attachScale = 1, attachOffset = 0 pour tous : les objets sont modélisés à leur place), puis
on rend 3 vues :
    jeu     la vue du joueur (caméra du jeu, cadrée sur la tête / le cou / le dos)
    3/4     vue orthographique de près, de 3/4 avant (on juge les formes)
    profil  vue orthographique de côté (on voit ce qui flotte ou traverse la tête)
Pour les objets qui ne se portent pas (coffres, boîtes, diamant, cheval), une vue 3/4 et une
vue de face, seuls.

Étape 1 — rendre les vues d'un dossier de FBX dans un dossier « cache » :
    FBX=<dossier des .fbx> CACHE=<dossier> FAMILLE=pirates \\
        /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/revue_modeles.py
Étape 2 — assembler la planche avant / après :
    PLANCHE=1 AVANT=<cache avant> APRES=<cache après> FAMILLE=pirates SORTIE=docs/captures/v7_modeles_pirates.png ...
Options : ACCESSOIRES="longue_vue ancre_doree" (seulement ceux-là).

Ce script ne modifie ni n'exporte aucun modèle.
"""

import math
import os
import sys

import bpy
import numpy as np
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import commun as C  # noqa: E402
import cheval_accessoires as A  # noqa: E402
import essayage_angles as E  # noqa: E402

CASE = 300


def familles():
    import commun_animaux as AN
    import essayage_pirates as P
    return {
        "v5": {k: v[0] for k, v in A.REGLAGES.items()},
        "pirates": {k: v[0] for k, v in P.REGLAGES.items()},
        "animaux": {k: v[0] for k, v in AN.REGLAGES.items()},
        # objets posés seuls (pas sur le cheval)
        "objets": {n: None for n in ("coffre", "coffre_bronze", "coffre_argent", "coffre_or",
                                     "coffre_diamant", "diamant", "boite_skins_1",
                                     "boite_accessoires_1", "boite_skins_pirates",
                                     "boite_accessoires_pirates", "boite_skins_animaux",
                                     "boite_accessoires_animaux", "personnage_cheval")},
    }


Y_MIN = {"PointChapeau": 1.0, "PointVisage": 1.0, "PointCou": 0.5, "PointDos": 0.3, "Joueur": -0.3}


def appliquer_transparences():
    for mod in ("commun_pirates", "commun_animaux"):
        try:
            __import__(mod).appliquer_transparence()
        except Exception as e:      # noqa: BLE001 (un module sans verre : rien à faire)
            print("transparence", mod, e)


def vues_ortho(objs, prefixe, vues):
    ancien = E.VUES
    E.VUES = vues
    try:
        return E.rendre_vues(objs, prefixe)
    finally:
        E.VUES = ancien


def rendre_famille(fam, ids, fbx, cache):
    os.makedirs(cache, exist_ok=True)
    C.MONDE_DEGRADE = True
    E.CASE = CASE
    C.nouvelle_scene()
    if fam != "objets":
        _, cheval = C.importer_cheval(position_unity=(0, 0, 0))
    for id_ in ids:
        chemin = os.path.join(fbx, f"{id_}.fbx")
        if not os.path.exists(chemin):
            print("ABSENT", chemin)
            continue
        objs = C.importer_fbx(chemin)
        appliquer_transparences()
        racines = [o for o in objs if o.parent is None]
        sorties = []
        if fam == "objets":
            sorties += vues_ortho(objs, id_, [("trois_quarts", (0.75, 0.75, 0.45)),
                                              ("face", (0.0, 1.0, 0.25))])
        else:
            point = familles()[fam][id_]
            C.poser_sur_point(racines, cheval[point], 1.0, (0, 0, 0))
            jeu = C.rendre_camera_jeu(list(cheval.values()) + objs, f"_tmp_rv_{id_}_jeu.png",
                                      taille=CASE, y_min=Y_MIN[point])
            sorties.append(jeu)
            sorties += vues_ortho(objs, id_, [("trois_quarts", (0.75, 0.75, 0.40)),
                                              ("profil", (1.0, 0.0, 0.05))])
        for i, s in enumerate(sorties):
            os.replace(s, os.path.join(cache, f"{id_}_{i}.png"))
        for o in objs:
            bpy.data.objects.remove(o, do_unlink=True)
        print("VUES", id_)


def lire(chemin):
    img = bpy.data.images.load(chemin)
    w, h = img.size
    px = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(px)
    bpy.data.images.remove(img)
    px = px.reshape(h, w, 4)
    if (w, h) != (CASE, CASE):        # simple plus-proche-voisin
        iy = (np.arange(CASE) * h / CASE).astype(int)
        ix = (np.arange(CASE) * w / CASE).astype(int)
        px = px[iy][:, ix]
    return px


def planche(ids, avant, apres, sortie):
    """Une ligne par objet : les vues AVANT (à gauche) puis APRÈS (à droite), séparées par
    une bande claire. Les lignes sont rangées de haut en bas."""
    lignes = []
    for id_ in ids:
        cases = []
        for d in (avant, apres):
            k = 0
            while os.path.exists(os.path.join(d, f"{id_}_{k}.png")):
                cases.append(lire(os.path.join(d, f"{id_}_{k}.png")))
                k += 1
            cases.append(np.ones((CASE, 12, 4), dtype=np.float32) * np.array([0.9, 0.85, 0.3, 1.0]))
        if len(cases) > 2:
            lignes.append(np.concatenate(cases[:-1], axis=1))
    if not lignes:
        return
    w = max(l.shape[1] for l in lignes)
    lignes = [np.pad(l, ((0, 4), (0, w - l.shape[1]), (0, 0)), constant_values=1.0) for l in lignes]
    img = np.concatenate(lignes[::-1], axis=0)      # Blender range les lignes de bas en haut
    h = img.shape[0]
    out = bpy.data.images.new("Planche", w, h, alpha=True)
    out.pixels.foreach_set(np.ascontiguousarray(img, dtype=np.float32).ravel())
    out.filepath_raw = sortie
    out.file_format = "PNG"
    out.save()
    print("PLANCHE", sortie)


def principal():
    fam = os.environ.get("FAMILLE", "v5")
    ids = os.environ.get("ACCESSOIRES", "").split() or list(familles()[fam])
    if os.environ.get("PLANCHE"):
        planche(ids, os.environ["AVANT"], os.environ["APRES"], os.environ["SORTIE"])
    else:
        rendre_famille(fam, ids, os.environ.get("FBX", C.DOSSIER_EXPORTS), os.environ["CACHE"])


if __name__ == "__main__":
    principal()
