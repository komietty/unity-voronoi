# unity-voronoi
- Delauney diagram (2D/3D) with bistellar flip algolithm.
- Voronoi diagram (2D/3D) from delauney triangles above.
- [CSG(Constructive Solid Geometry)](https://en.wikipedia.org/wiki/Constructive_solid_geometry) for an atbitrary mesh.


![img](Imgs/crackin.png)
![img](Imgs/anim.gif)

### Voronoi Diagram
Both 2D/3D versions are avairable. As for an algorithm please see the reference below.
![img](Imgs/voronoi2d.png)
![img](Imgs/voronoi3d.png)

### CSG
CSG module has **Intersection**, **Subtruction**, **Union** functions. The Image below shows subtruction demo.
![img](Imgs/csg.png)

## Usage

using a [simplex geom submodule](https://github.com/komietty/unity-simplex-geometry), so update submodule first.

## reference
[Computing the 3D Voronoi Diagram Robustly: An Easy Explanation, Hugo Ledoux, 2007](http://www.gdmc.nl/publications/2007/Computing_3D_Voronoi_Diagram.pdf)

[evanw's csg.js](https://evanw.github.io/csg.js/)
