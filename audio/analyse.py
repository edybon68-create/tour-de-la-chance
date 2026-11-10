# -*- coding: utf-8 -*-
"""
Contrôle des sons, puisqu'on ne peut pas les écouter : on les MESURE.
Pour chaque WAV du dossier de sortie (Assets/Audio par défaut, ou audio/exports) : durée, pic (dBFS), RMS (volume moyen, dBFS),
volume ressenti (RMS max sur 0,4 s) et volume « à l'oreille » (le même après pondération K, ~LUFS),
échantillons saturés, premier/dernier échantillon (doivent être ~0 pour un effet : pas de clic).
Pour les musiques : contrôle du raccord de boucle (le saut entre le dernier et le premier
échantillon doit ressembler à un saut ordinaire entre deux échantillons voisins).
Lancer : <python de Blender> audio/analyse.py
"""
import glob
import os
import sys
import wave

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import synth
from synth import volume_court_terme, volume_pondere


def lis(chemin):
    with wave.open(chemin, "rb") as w:
        assert w.getframerate() == 44100 and w.getsampwidth() == 2, chemin
        c = w.getnchannels()
        x = np.frombuffer(w.readframes(w.getnframes()), dtype="<i2").astype(float) / 32768
    return x.reshape(-1, c), c


def db(v):
    return 20 * np.log10(max(v, 1e-12))


def main(dossier=None):
    """Mesure tous les WAV de `dossier` (par défaut : le dossier de sortie de synth.py)."""
    dossier = dossier or synth.DOSSIER_AUDIO
    print("\nAnalyse de :", os.path.relpath(dossier, synth.RACINE))
    print("\n%-36s %3s %7s %7s %7s %7s %7s %4s %8s %8s" % (
        "fichier", "can", "durée", "pic", "RMS", "ressenti", "oreille", "sat", "début", "fin"))
    ok = True
    for chemin in sorted(glob.glob(os.path.join(dossier, "*", "*.wav"))):
        x, c = lis(chemin)
        nom = os.path.relpath(chemin, dossier)
        pic = np.max(np.abs(x))
        rms = np.sqrt(np.mean(x ** 2))
        sat = int(np.sum(np.abs(x) >= 32767 / 32768))
        debut, fin = np.max(np.abs(x[0])), np.max(np.abs(x[-1]))
        print("%-36s %3d %6.3fs %6.2f %7.2f %7.2f %7.2f %4d %8.5f %8.5f" % (
            nom, c, len(x) / 44100, db(pic), db(rms), volume_court_terme(x), volume_pondere(x),
            sat, debut, fin))
        musique = "musique" in nom
        if sat or db(pic) > -0.95 or db(pic) < -12 or (not musique and (debut > 0.002 or fin > 0.002)):
            print("   !! à vérifier"); ok = False
        if musique:
            # Raccord de boucle : saut fin -> début comparé aux sauts habituels
            sauts = np.abs(np.diff(x, axis=0))
            saut_boucle = np.abs(x[0] - x[-1])
            p999 = np.percentile(sauts, 99.9, axis=0)
            # volume juste avant et juste après le raccord (50 ms)
            n = 2205
            avant = db(np.sqrt(np.mean(x[-n:] ** 2)))
            apres = db(np.sqrt(np.mean(x[:n] ** 2)))
            print("   boucle : saut fin->début = %s (99,9 %% des sauts voisins < %s) ; "
                  "RMS 50 ms avant/après raccord = %.1f / %.1f dBFS" % (
                      np.round(saut_boucle, 4), np.round(p999, 4), avant, apres))
            # Le raccord doit ressembler à n'importe quelle barre de mesure du morceau :
            # on compare le volume des 50 ms qui PRÉCÈDENT chaque début de mesure.
            mesure = int(1.5 * 44100)
            fins = [db(np.sqrt(np.mean(x[m * mesure - n:m * mesure] ** 2))) for m in range(1, 32)]
            print("   volume des 50 ms avant les autres barres de mesure : de %.1f à %.1f dBFS "
                  "(raccord : %.1f)" % (min(fins), max(fins), avant))
            if np.any(saut_boucle > p999) or not (min(fins) - 3 <= avant <= max(fins) + 3):
                print("   !! raccord suspect"); ok = False
    print("\nBILAN :", "tout est conforme" if ok else "des points sont à vérifier")


if __name__ == "__main__":
    # Option : un dossier à analyser, ex. « audio/exports »
    main(os.path.join(synth.RACINE, sys.argv[1]) if len(sys.argv) > 1 else None)
