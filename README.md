# Planetary Terrain
Kerbal Space Program inspired, SharpDX planet renderer

#Features
Capable of rendering full-scale celestial bodies, using 64 bit (double precision) vectors and a origin-centered view matrix, and scaling down/moving distant objects into the view frustum of the camera.

Bodies are rendered using a quadtree LOD system, mapping the sphere to a cube of quadtrees. The edges between LODs are handled using quad-fanning, making the edges of higher-detail nodes "fan out" to conform to the edges of the less-detailed node (seen [here] (https://www.youtube.com/watch?v=mXTxQko-JH0) at 36:48.

The atmosphere is rendered using Sean Oneal's amospheric scattering, from [here](http://http.developer.nvidia.com/GPUGems2/gpugems2_chapter16.html)

Shaders are compiled with my very own batch script that reads .manifest and runs fxc based off what is read in .manifest

#Problems
* When a node is neighbored by a lower-detail node, and the lower-detail node is two or more detail levels lower, cracks still appear. This is due to the function that tells nodes when to split creating situations where nodes are neighbored by much lower level modes.
* Planet rotation causes the GetHeight method to return wrong, I suspect due to the innacuracy of inverting a matrix of floats.
* The atmosphere is very dark... I have no idea what causes this but my solution will eventually include either using a different atmosphere model, or making the skybox turn blue when in an atmosphere (currently working on the skybox method)
* Cubemaps seem to not work... I've ignored this for now and separated the skybox into 6 images, but a cubemap is ideal

##WIP
* Physics is simulated with euler integration
* Make orbits simulated via keplerian math instead of cartesian math when no external forces are applied (more accurate, and can handle time warp!)
* Friction

##TODO
* Networked multiplayer
* Optimizations!
* Fix/complete physics
* Add ability to re-enter the vehicle
* Deferred rendering
* Shadows
* Water shaders
* Fix atmosphere being so dark (probably just use a different model)
