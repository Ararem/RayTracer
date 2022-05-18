# Todos

## Misc

* Swap floats to doubles and test the performance loss (might be preferable to use doubles everywhere instead of casting all the time)
* Separate the main project (RayTracer.Core) into two separate projects - one that defines the code and types (e.g. `AsyncRenderJob` and `Material`,
  and a separate one for the actual implementation (e.g. `StandardMaterial` and `XYPlane`)

## Materials/Textures

* Implement some actual textures other than plain colours
    * Images
    * Procedural (Noise)
* Add some sort of texturing to sky-boxes (HDRI images?)
* Shaders (Materials) should be able to modify some the surface normal (see [HitRecord](Hittables/HitRecord.cs))
* Create some mechanism to allow the hittable to communicate with the material (such as how far the ray went for ConstantDensityMedium)

## Models/Shapes

* Import 3D models

## Performance

* Optimise `TryHit()` functions to cache as much as possible in the `.ctor`, instead of recalculating each time (e.g. `Capsule` and `ba`, `baba`)
* For planar shapes - find out what the fastest way to calculate `t` is and change it in all the `TryHit` functions
    * Quad
    * InfinitePlane
    * Disk
* See if there are any shared variables/calculations per ray that are used across multiple hittables, and see if we can pass that into the `TryHit`
  functions (e.g. `1/rayDirection`) for some of the IQ hittables

## Debugging/Development

* Cancel render and return to main menu
* Reload scene from [Builtin Scenes](Core/BuiltinScenes.cs) while running (no restart app)
* Store scenes as files rather than C# properties
* Custom shader/shape import from dll's rather than builtin?