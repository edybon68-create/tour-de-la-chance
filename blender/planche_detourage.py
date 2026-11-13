"""Planches de contrôle du détourage : on compare chaque icône à son original.

Lancer avec :
  /Applications/Blender.app/Contents/MacOS/Blender --background --python blender/planche_detourage.py

Chaque ligne montre une icône en 4 cases :
  original (fond lavande) | AVANT sur fond sombre | APRÈS sur fond sombre | APRÈS sur fond rose
Le fond sombre fait ressortir les trous et les liserés clairs ; le fond rose (celui de la
Boutique) fait ressortir les restes de lavande et les ombres violettes.

Variables d'environnement (toutes facultatives) :
  AVANT=<dossier>   icônes détourées d'avant (sinon la colonne AVANT est vide)
  APRES=<dossier>   icônes détourées d'après (défaut : art/icones_detourees)
  ORIGINAL=<dossier> originaux (défaut : art/icones)
  ICONES="piece zebre"  seulement ces icônes (sans .png)
  CASE=256          taille d'une case en pixels
  LIGNES=6          nombre d'icônes par planche
  SORTIE=<préfixe>  chemin des planches, sans « _00.png » (défaut : docs/captures/qa_v7_detourage)

On utilise le Python de Blender parce qu'il sait lire et écrire les PNG (et contient numpy).
"""
import os
import bpy
import numpy as np

RACINE = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ORIGINAL = os.environ.get("ORIGINAL") or os.path.join(RACINE, "art", "icones")
APRES = os.environ.get("APRES") or os.path.join(RACINE, "art", "icones_detourees")
AVANT = os.environ.get("AVANT", "")
CASE = int(os.environ.get("CASE", "256"))
LIGNES = int(os.environ.get("LIGNES", "6"))
SORTIE = os.environ.get("SORTIE") or os.path.join(RACINE, "docs", "captures", "qa_v7_detourage")
SOMBRE = np.array([0x1E, 0x1A, 0x2E], dtype=np.float32) / 255.0
ROSE = np.array([0xFF, 0x7E, 0xB6], dtype=np.float32) / 255.0
VIDE = np.array([0.5, 0.5, 0.5], dtype=np.float32)
MARGE = 6


def lire(chemin):
    """Renvoie l'image (h, w, 4) en nombres 0..1, ou None si elle n'existe pas."""
    if not chemin or not os.path.exists(chemin):
        return None
    img = bpy.data.images.load(chemin)
    w, h = img.size
    px = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(px)
    bpy.data.images.remove(img)
    return px.reshape(h, w, 4)


def reduire(rgb, taille):
    """Réduit une image carrée à `taille` px (moyenne de blocs, puis plus proche voisin)."""
    h, w = rgb.shape[:2]
    f = max(1, h // taille)
    hh, ww = (h // f) * f, (w // f) * f
    r = rgb[:hh, :ww].reshape(hh // f, f, ww // f, f, 3).mean(axis=(1, 3))
    iy = (np.arange(taille) * r.shape[0] / taille).astype(int)
    ix = (np.arange(taille) * r.shape[1] / taille).astype(int)
    return r[iy][:, ix]


def case(px, fond):
    if px is None:
        return np.tile(VIDE, (CASE, CASE, 1))
    a = px[:, :, 3:4]
    return reduire(px[:, :, :3] * a + fond * (1 - a), CASE)


def ecrire(chemin, rgb):
    h, w = rgb.shape[:2]
    out = np.concatenate([rgb, np.ones((h, w, 1), dtype=np.float32)], axis=2)
    img = bpy.data.images.new("planche", w, h, alpha=False)
    img.pixels.foreach_set(out[::-1].ravel())  # Blender range les lignes de bas en haut
    img.filepath_raw = chemin
    img.file_format = "PNG"
    img.save()
    bpy.data.images.remove(img)
    print("PLANCHE", chemin)


def main():
    seules = os.environ.get("ICONES", "").split()
    noms = sorted(n[:-4] for n in os.listdir(APRES) if n.endswith(".png"))
    if seules:
        noms = [n for n in seules if n in noms]
    pages = [noms[i:i + LIGNES] for i in range(0, len(noms), LIGNES)]
    for p, page in enumerate(pages):
        largeur = 4 * CASE + 5 * MARGE
        hauteur = len(page) * CASE + (len(page) + 1) * MARGE
        planche = np.ones((hauteur, largeur, 3), dtype=np.float32)
        for i, nom in enumerate(page):
            # Les pixels Blender sont rangés de bas en haut : on retourne chaque image.
            def charge(d):
                px = lire(os.path.join(d, nom + ".png")) if d else None
                return px[::-1] if px is not None else None
            orig, av, ap = charge(ORIGINAL), charge(AVANT), charge(APRES)
            cases = [case(orig, VIDE) if orig is not None else case(None, VIDE),
                     case(av, SOMBRE), case(ap, SOMBRE), case(ap, ROSE)]
            y = MARGE + i * (CASE + MARGE)
            for j, c in enumerate(cases):
                x = MARGE + j * (CASE + MARGE)
                planche[y:y + CASE, x:x + CASE] = c
            print(f"  {p:02d} ligne {i + 1} : {nom}")
        ecrire(f"{SORTIE}_{p:02d}.png", planche)


main()
