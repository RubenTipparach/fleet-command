"""Sample a palette from reference screenshots by k-means over their saturated pixels.

Used for mockup 4 (console-skins) to take the Concord skin's colours from Starfleet Command and Starfleet Academy
screens (owner, 2026-10-30: "extract colorpallete from starfleet command and starfleet academy"). The screenshots are
someone else's art and are never committed; only the sampled numbers go into the design.

  python3 tools/mockups/sample_palette.py -k 8 shot1.jpg shot2.jpg:0,0,0.2,1
  (an optional box after a colon crops to fractions x0,y0,x1,y1 of the image, such as a console column)
"""

import argparse
import colorsys

import numpy as np
from PIL import Image


def pixels(spec, min_sat, min_val):
    """Saturated, lit pixels of one image (optionally cropped), as floats 0-255."""
    path, _, box = spec.partition(':')
    im = Image.open(path).convert('RGB')
    if box:
        x0, y0, x1, y1 = (float(v) for v in box.split(','))
        w, h = im.size
        im = im.crop((int(x0 * w), int(y0 * h), int(x1 * w), int(y1 * h)))
    a = np.asarray(im).reshape(-1, 3).astype(float)
    hsv = np.array([colorsys.rgb_to_hsv(*(p / 255)) for p in a])
    return a[(hsv[:, 1] > min_sat) & (hsv[:, 2] > min_val)]


def kmeans(a, k, seed=2, iterations=40):
    """Cluster colours; returns (hex, share in percent) largest first. Seeded, so a run repeats exactly."""
    rng = np.random.default_rng(seed)
    c = a[rng.choice(len(a), k, replace=False)]
    for _ in range(iterations):
        labels = ((a[:, None] - c[None]) ** 2).sum(2).argmin(1)
        c = np.array([a[labels == i].mean(0) if (labels == i).any() else c[i] for i in range(k)])
    n = np.bincount(labels, minlength=k)
    return [('#%02x%02x%02x' % tuple(int(v) for v in c[i]), round(n[i] / len(a) * 100, 1)) for i in n.argsort()[::-1]]


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument('images', nargs='+', help='image paths, each optionally path:x0,y0,x1,y1')
    ap.add_argument('-k', type=int, default=8, help='number of colours')
    ap.add_argument('--min-sat', type=float, default=0.35, help='skip greys below this HSV saturation (0-1)')
    ap.add_argument('--min-val', type=float, default=0.3, help='skip darks below this HSV value (0-1)')
    args = ap.parse_args()
    a = np.vstack([pixels(s, args.min_sat, args.min_val) for s in args.images])
    for hex_, share in kmeans(a, args.k):
        print(f'{hex_}  {share:5.1f} %')


if __name__ == '__main__':
    main()
