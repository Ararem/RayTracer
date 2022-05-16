# Todos

## Misc
* Swap floats to doubles and test the performance loss (might be preferable to use doubles everywhere instead of casting all the time)

## Materials/Textures
* Implement some actual textures other than plain colours
  * Images
  * Procedural (Noise)
* Add some sort of texturing to sky-boxes (HDRI images?)
* Shaders (Materials) should be able to modify some the surface normal (see [HitRecord](Hittables/HitRecord.cs))
* Create some mechanism to allow the hittable to communicate with the material (such as how far the ray went for ConstantDensityMedium)

# Models/Shapes
* Import 3D models

## Debugging/Development
* Cancel render and return to main menu
* Reload scene from [Builtin Scenes](Core/BuiltinScenes.cs) while running (no restart app)
* Store scenes as files rather than C# properties
* Custom shader/shape import from dll's rather than builtin?