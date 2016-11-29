# Planetary Terrain
SharpDX planetary terrain achieved through 3d noise and quadtrees

Capable of rendering full-scale planets, acheived by using 64 bit (double precision) vectors and a zero-centered view matrix, and scaling down/moving distant objects into the view frustum of the camera.

Bodies are rendered using a quadtree LOD system, mapping the sphere to a cube of quadtrees. The edges between LODs are handled using quad-fanning, making the edges of higher-detail nodes "fan out" to conform to the edges of the less-detailed node (seen here https://www.youtube.com/watch?v=mXTxQko-JH0 at 36:48.

Still, when a node is neighbored by a lower-detail node, and the lower-detail node is two or more detail levels higher, cracks still appear. This is due to the function that tells nodes when to split and when to combine being too rough (it needs to be re written)

##WIP:
Physics is simulated
