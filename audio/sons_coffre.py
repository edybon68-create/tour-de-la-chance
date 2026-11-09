# -*- coding: utf-8 -*-
"""
Sons des coffres (catégorie « coffre ») — spec docs/specs/sons-animations.md §5.2.
Bois, charnières, petits chocs : des coffres-jouets, jamais inquiétants.
Lancer : <python de Blender> audio/sons_coffre.py
"""
import numpy as np

from synth import (ajoute, bruit, ecris_wav, filtre, finalise, glisse, perc, silence,
                   sinus, temps, vibrato, triangle)


def impulsion_bois(rng, centre=1200, q=6.0, chute=0.03, d=0.06):
    """Un petit choc de bois : bruit très bref passé dans un filtre résonant (le « corps » du bois)."""
    b = bruit(d, rng) * perc(d, 0.0005, chute * 0.4)
    return filtre(b, "passe_bande", centre, q) * 3.0


def coffre_survol(rng):
    """Le coffre frétille : deux petites impulsions de bois rapprochées vers 1 200 Hz."""
    d = 0.12
    x = silence(d)
    ajoute(x, impulsion_bois(rng, 1200, 6, 0.05), 0.0, 1.0)
    ajoute(x, impulsion_bois(rng, 1350, 6, 0.05), 0.045, 0.75)
    return finalise(x, d, 0.001, 0.01)


def coffre_tremble(rng):
    """
    Le cœur qui bat : cliquetis de bois à 12 par seconde, de plus en plus forts,
    sur un sinus grave qui monte de 110 à 220 Hz (la tension).
    """
    d = 1.0
    x = silence(d)
    for k in range(12):
        t0 = k / 12.0
        g = 0.35 + 0.65 * (k / 11.0) ** 1.3
        centre = 800 * (1.0 + rng.uniform(-0.08, 0.08))
        ajoute(x, impulsion_bois(rng, centre, 5, 0.06, 0.07), t0, g)
        # petit « toc » grave du coffre qui retombe sur le sol
        toc = sinus(glisse(260, 180, 0.04), 0.04) * perc(0.04, 0.001, 0.03)
        ajoute(x, toc, t0, 0.35 * g)
    t = temps(d)
    tension = sinus(glisse(110, 220, d), d) + 0.25 * sinus(glisse(220, 440, d), d)
    env = 0.15 + 0.35 * t / d
    x = x + 0.5 * tension * env
    return finalise(x, d, 0.01, 0.01)


def coffre_ouvre(rng):
    """Couvercle qui s'ouvre d'un coup : grincement 400 -> 700 Hz (vibrato rapide) puis « boum » sourd."""
    d = 0.35
    x = silence(d)
    dg = 0.15
    f = vibrato(glisse(400, 700, dg), dg, vitesse=38, demi_tons=0.8)
    grince = (triangle(f, dg) + 0.3 * sinus(2 * f, dg))
    env = np.sin(np.pi * np.linspace(0, 1, len(grince))) ** 0.7
    ajoute(x, 0.5 * grince * env, 0.0)
    db = 0.2
    boum = sinus(glisse(135, 105, db), db) * perc(db, 0.002, 0.15)
    choc = filtre(bruit(db, rng), "passe_bas", 900, ordre=2) * perc(db, 0.001, 0.04)
    ajoute(x, boum + 1.2 * choc, 0.15)
    return finalise(x, d, 0.003, 0.01)


def coffre_ferme(rng):
    """Couvercle qui retombe : « boum » sourd 140 -> 90 Hz + petit bruit étouffé."""
    d = 0.2
    boum = sinus(glisse(140, 90, d), d) * perc(d, 0.002, 0.16)
    choc = filtre(bruit(d, rng), "passe_bas", 700, ordre=2) * perc(d, 0.001, 0.05)
    return finalise(boum + 1.3 * choc, d, 0.002, 0.01)


def genere():
    rng = np.random.default_rng(202)
    print("Sons coffre :")
    ecris_wav("coffre", "coffre_survol", coffre_survol(rng))
    ecris_wav("coffre", "coffre_tremble", coffre_tremble(rng))
    ecris_wav("coffre", "coffre_ouvre", coffre_ouvre(rng))
    ecris_wav("coffre", "coffre_ferme", coffre_ferme(rng))


if __name__ == "__main__":
    genere()
