# Lot 10 — Icônes à régénérer avec Gemini (V7, point 17 : qualité 3D)

> **À quoi sert ce fichier ?** Chaque bloc ci-dessous = **une image**. On copie le prompt
> (le texte en anglais dans le cadre gris) et on le colle tel quel dans Gemini, dans le
> navigateur. On enregistre ensuite l'image sous le **nom de fichier indiqué**, dans
> `art/icones/`, puis on lance `blender/detourer_icones.py` avec `ICONES="<nom>"`.
>
> **Pourquoi ces 6 icônes ?** En faisant les planches « icône | 3D » du point 17
> (`docs/captures/v7_qualite_accessoires_*.png`), l'artiste 3D a trouvé qu'elles ne sont pas
> « incroyables » : ce sont d'anciens rendus Blender des modèles v5, plats et sans charme,
> à côté des icônes Gemini des autres objets. L'icône étant la **référence** du modèle 3D,
> il faut qu'elle soit belle d'abord ; le modèle sera ensuite repris pour lui ressembler.
>
> C'est le **chef de projet** qui génère. Règle : ne jamais changer de compte Gemini.

---

## Mode d'emploi

1. **Une seule conversation Gemini** « M — Accessoires Animaux refaits ». Joindre au premier
   message (bouton « + ») `art/icones/bonnet_grenouille.png` et `art/icones/lunettes_hibou.png` :
   ce sont deux icônes du même lot qui ont le bon rendu « jouet verni ».
2. Le premier prompt est long ; les suivants commencent par *« Same style as the previous
   image… »* pour garder le même rendu.
3. Fond **lavande uni #E6E1EE**, **sans ombre portée** (détourage propre, art bible §9.2).

---

## 1. `moustaches.png` — Moustaches de chat (Commun, Visage)

Défaut actuel : de fines baguettes blanches qui flottent autour d'une petite truffe rose ; on ne
comprend pas l'objet.

```
A cute cartoon cat-whisker accessory for a toy game, made of glossy vinyl like a premium
collectible toy: a small rounded pink heart-shaped cat nose in the center, two chubby white
muzzle pads with three tiny dark freckle dots each, and three thick, softly curved white
whiskers on each side with rounded tips. Thick, generous, friendly shapes, no thin lines.
Three-quarter front view, slightly from above. Soft studio lighting, gentle rim light,
saturated but harmonious colors. Solid flat lavender background #E6E1EE, no shadow, no text,
centered, the object fills about 70% of the image. Square 1024x1024.
```

## 2. `truffe.png` — Truffe de chien (Rare, Visage)

Défaut actuel : une tache noire brillante informe.

```
Same style as the previous image: a cute cartoon dog nose accessory, a big glossy black
rounded triangle nose with two clear oval nostrils, a soft highlight on top and a subtle
dark-brown second tone on the lower edge, plus a tiny pink tongue tip peeking below.
Chubby toy proportions, premium vinyl. Three-quarter front view. Solid flat lavender
background #E6E1EE, no shadow, no text, centered, 70% of the image. Square 1024x1024.
```

## 3. `bois_cerf.png` — Bois de cerf fleuris (Super rare, Tête)

Défaut actuel : deux bâtonnets séparés, maigres, avec trois fleurs minuscules.

```
Same style as the previous image: a pair of cute cartoon deer antlers on a thin brown
headband, chunky rounded antlers in warm caramel wood color with soft cream tips, each antler
with three short rounded branches, decorated with little pink five-petal flowers with
yellow centers and two small green leaves. Toy proportions, generous rounded shapes, glossy
vinyl. Three-quarter front view. Solid flat lavender background #E6E1EE, no shadow, no text,
centered, 70% of the image. Square 1024x1024.
```

## 4. `ailes_chauve_souris.png` — Ailes de chauve-souris (Super rare, Dos)

Défaut actuel : deux ailes plates et sombres, presque invisibles sur le fond.

```
Same style as the previous image: a pair of cute cartoon bat wings accessory, purple wings
with a lighter lilac membrane and darker plum bones, three rounded scallops on the lower edge
of each wing, small round joints, connected in the middle by a tiny round purple clasp with
a little golden star. Chubby, friendly shapes, glossy vinyl toy, not scary. Front view,
slightly from above. Solid flat lavender background #E6E1EE, no shadow, no text, centered,
70% of the image. Square 1024x1024.
```

## 5. `chaton_sac.png` — Chaton dans un sac (Légendaire, Dos)

Défaut actuel : la anse flotte, détachée du panier ; le chaton est petit et terne.

```
Same style as the previous image: a cute cartoon wicker basket backpack with a fluffy grey
kitten peeking out, the kitten has big round green eyes, pink inner ears, a tiny pink nose
and a small golden bell collar, its two little paws resting on the rim. Woven caramel
basket with a soft cream rim, two brown leather straps clearly attached to the basket.
Toy proportions, glossy vinyl. Three-quarter front view. Solid flat lavender background
#E6E1EE, no shadow, no text, centered, 70% of the image. Square 1024x1024.
```

## 6. `ruche.png` — Ruche (Légendaire, Tête)

Défaut actuel : un pot blanc à la forme floue et deux abeilles éparpillées.

```
Same style as the previous image: a cute cartoon beehive hat, a rounded golden-yellow
skep beehive made of three stacked soft rings, a small dark round entrance, honey dripping
from the top in glossy amber drops, and two chubby cartoon bees with black and yellow
stripes and white translucent wings flying close to it. Toy proportions, glossy vinyl.
Three-quarter front view. Solid flat lavender background #E6E1EE, no shadow, no text,
centered, 70% of the image. Square 1024x1024.
```
