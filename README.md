<p align="center">
<img src="images/weltfern_wecreatesoftware.png" width="320" height="120">
</p>

# Cognition Design - Phase 01
This is the weltfern documentation for the first phase of Cognition Design. Everything we did while working on this project is listed here.

## 1 -  What needed to happen?
We completed each of the jointly-agreed goals which consisted of two major milestones:

    - The overall believability of the scenes is improved.
    - The project runs steady at 90FPS.

What we did exactly to achieve these milestones will be explained in the following paragraphs.

## 2 - Revamping the assets
Going into the project we started to work on improving its believability. In order to do that we reworked all the given assets. We remodeled them, added missing details and improved their visual quality. For each object we created completely new textures to ensure a realistic digital reproduction. A list of all these asset is written down below:

    - 1_Fahrstuhl
    - 2_Fahrkartenautomat (+ four different variations)
    - 3_Rolltreppe
    - 4_GrosseTreppe
    - 5_Bank
    - 7_Vitrine
    - 9_Abfallbehaelter
    - 10_Uhr
    - 13_LichtanlageLang
    - 14_LichteranlageKreis
    - 15_Werbetafel
    - 19_Gleistunnel
    - 19_Gleistunnel_Endstueck
    - 20_Gleis (+ three different variations)
    - 21_Geldautomat
    - 23_Feuerloescher
    - 24_Zigarettenautomat
    - 33_VitrineAnWand
    - 34_DigitaleAnzeige
    - 37_Notrufsaeule
    - 38_Telefonsaeule
    - 39_Abzug
    - 40_Feuerloescher
    - 41_Liniennetzplan
    - 42_WerbetafelKlein (+ two different variations)
    - 43_WerbetafelGross (+ two different variations)
    - 44_Gleishaeuschen
    - 50_RoundCeiling_Light
    - 59_Mesh_B_Boden (Version 0)
    - 89_Mesh_B_Boden (Version 0)
    - 99_Bahnsteig
    - 254_WerbeschildEingang

## 3 - Achieving realism
In addition to the assets populating the scenes we also worked on improving their environment in general. In order to achieve a realistic look we did a couple more things:

    - Completely reworked the light setup for every scene.
        - Improved the exposure of each scene to create a reasonable variation in their illumination.
        - Added HDRs to every light to reproduce the physical properties of real world light.
        - Implemented sunlight to create the illusion of a living outside world.
    - Restructured the Reflection Probes to guarantee sharp and plausible reflections.
    - Added Anti Aliasing to prevent jagged edges when viewing the project in VR.

## 4 - Increasing the performance
Finally yet importantly we increased the performance of the project to make it more cost efficient. To accomplish the second major milestone we implemented the following changes:

    - Realised LOD (Level of Detail) meshes for every revamped asset. This guarantees a reasonable quality based on the distance between the viewer and the rendered object.
    - Reworked the light maps for a more stable representation of the scenes lighting.
    - Optimised the current realtime light setup to be used more efficiently.
    - Restructured the mesh colliders to improve not just the performance but also the way users can move inside VR.

## 5 - Additional
After a quick feedback from Robin we also applied the requested changes to these three assets:

    - 3_Rolltreppe: We added the lights at the side panels and reworked the stair texture.
    - 4_GrosseTreppe: We reworked the granite texture and replaced the glass with the previously used metal to correctly represent the real world counterpart.
    - 7_Vitrine: We added an emission to the content display, the logo and the actual font.

<br>
