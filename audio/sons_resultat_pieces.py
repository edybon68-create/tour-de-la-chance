# -*- coding: utf-8 -*-
"""
Sons de résultat (catégorie « resultat ») et de pièces (catégorie « pieces »)
— spec docs/specs/sons-animations.md §5.2.
Le trésor et l'encaissement doivent donner envie de rejouer ; le piège reste drôle.
Lancer : <python de Blender> audio/sons_resultat_pieces.py
"""
import numpy as np

from synth import (ajoute, carre, clochette, ecris_wav, finalise, glisse, glockenspiel, hz,
                   orgue, passe_bas_variable, perc, reverb, scintillement, silence, sinus,
                   temps, ting, vibrato, adsr, n_ech)


# --------------------------- résultats ---------------------------

def resultat_tresor(rng):
    """« Ta-daa ! » : arpège de glockenspiel fa5-la5-do6-fa6 (0,07 s d'écart) + scintillement."""
    d = 0.9
    x = silence(d)
    for i, note in enumerate(["F5", "A5", "C6", "F6"]):
        ajoute(x, glockenspiel(hz(note), 0.75, 0.6 if i < 3 else 0.7), 0.07 * i, 0.7 + 0.1 * i)
    x += scintillement(d, rng, 16, 4000, 6000, debut=0.2, fin=0.75, chute=0.03, volume=0.35)
    x = reverb(x, 0.15, 0.7)
    return finalise(x, d, 0.002, 0.06)


def resultat_piege():
    """
    « Boïng... wah-wah » : un ressort (300 -> 150 Hz, vibrato large à 12 Hz), puis deux notes
    de trombone cartoon (carré filtré mi4 -> ré#4) qui finit en glissant vers le bas.
    Volontairement comique : notes proches, pas de grave menaçant.
    """
    d = 1.0
    x = silence(d)
    # 1) le ressort
    db = 0.35
    f = vibrato(glisse(300, 150, db), db, vitesse=12, demi_tons=2.5)
    ressort = sinus(f, db) + 0.25 * sinus(2 * f, db)
    ajoute(x, ressort * perc(db, 0.003, 0.45), 0.0, 0.9)

    # 2) le trombone « wah-wah » : à chaque note, le filtre s'ouvre puis se referme
    def note_trombone(f0, duree, glisse_fin=False):
        n = n_ech(duree)
        freq = np.full(n, f0)
        if glisse_fin:  # les 0,25 dernières secondes glissent d'une tierce vers le bas
            ng = n_ech(0.25)
            freq[-ng:] = f0 * 2 ** (-4 * np.linspace(0, 1, ng) ** 1.5 / 12)
        freq = vibrato(freq, duree, vitesse=5, demi_tons=0.25)
        son = carre(freq, duree, douceur=2500)
        t = temps(duree)
        wah = 350 + 1300 * np.sin(np.pi * np.clip(t / min(duree, 0.3), 0, 1)) ** 2
        if glisse_fin:
            wah = np.where(t > 0.3, 350 + 900 * np.exp(-(t - 0.3) * 4), wah)
        son = passe_bas_variable(son, wah, q=2.0)
        return son * adsr(duree, 0.02, 0.05, 0.85, 0.06 if not glisse_fin else 0.12)

    ajoute(x, note_trombone(hz("E4"), 0.22), 0.35, 0.55)
    ajoute(x, note_trombone(hz("D#4"), 0.36, glisse_fin=True), 0.58, 0.55)
    x = reverb(x, 0.08, 0.5)
    # cible -13 dB : le piège ne doit pas paraître plus fort que le trésor (-12,6 dB)
    return finalise(x, d, 0.003, 0.03, cible_db=-13.0)


def resultat_encaisse(rng):
    """
    Petite fanfare d'orgue de barbarie : accords fa - si bémol - do - fa (le dernier tenu
    0,6 s) avec trémolo 5 Hz, puis scintillement final. Une victoire de fête foraine.
    """
    d = 1.4
    x = silence(d)
    accords = [
        (["F3", "C5", "F5", "A5"], 0.00, 0.15),
        (["Bb2", "D5", "F5", "Bb5"], 0.17, 0.15),
        (["C3", "E5", "G5", "C6"], 0.34, 0.15),
        (["F3", "F4", "A5", "C6", "F6"], 0.51, 0.62),
    ]
    for notes, t0, duree in accords:
        for note in notes:
            f = hz(note)
            g = 0.5 if f < 200 else 0.3
            ajoute(x, orgue(f, duree, a=0.008, r=0.05 if duree < 0.3 else 0.15,
                            coupure=1800), t0, g)
    # scintillement final : quelques notes de glockenspiel aiguës et des étincelles
    for i, note in enumerate(["C6", "F6", "A6", "C7"]):
        ajoute(x, glockenspiel(hz(note), 0.4, 0.35), 0.75 + 0.06 * i, 0.22)
    x += scintillement(d, rng, 14, 4500, 6500, debut=0.8, fin=1.2, chute=0.03, volume=0.18)
    x = reverb(x, 0.14, 0.8)
    return finalise(x, d, 0.003, 0.12)


# --------------------------- pièces ---------------------------

def pieces_jaillissement(rng):
    """14 « ting » métalliques sur 0,6 s, de plus en plus espacés : une gerbe de pièces."""
    d = 0.8
    x = silence(d)
    for i in range(14):
        t0 = 0.6 * (i / 13.0) ** 1.8 + rng.uniform(-0.005, 0.005)
        f = rng.uniform(2000, 5000)
        ajoute(x, ting(f, 0.1, 0.06), max(t0, 0.0), rng.uniform(0.6, 1.0) * (1 - 0.3 * i / 13))
    x = reverb(x, 0.1, 0.4)
    return finalise(x, d, 0.001, 0.03)


def pieces_tic():
    """Une seule pièce qui tombe dans la bourse : « ting » 3 200 Hz + partiel x2,7, chute 30 ms."""
    d = 0.04
    return finalise(ting(3200, d, 0.03), d, 0.0008, 0.004)


def pieces_achat():
    """« Kaching » de conte : deux « ting » rapprochés puis une clochette tenue (do6, sinus + x4)."""
    d = 0.5
    x = silence(d)
    ajoute(x, ting(2900, 0.1, 0.06), 0.0, 0.7)
    ajoute(x, ting(3700, 0.1, 0.06), 0.06, 0.7)
    ajoute(x, clochette(hz("C6"), 0.4, 0.38, harmonique=4, poids=0.35), 0.12, 1.0)
    x = reverb(x, 0.08, 0.4)
    return finalise(x, d, 0.001, 0.03)


def genere():
    rng = np.random.default_rng(303)
    print("Sons resultat / pieces :")
    ecris_wav("resultat", "resultat_tresor", resultat_tresor(rng))
    ecris_wav("resultat", "resultat_piege", resultat_piege())
    ecris_wav("resultat", "resultat_encaisse", resultat_encaisse(rng))
    ecris_wav("pieces", "pieces_jaillissement", pieces_jaillissement(rng))
    ecris_wav("pieces", "pieces_tic", pieces_tic())
    ecris_wav("pieces", "pieces_achat", pieces_achat())


if __name__ == "__main__":
    genere()
