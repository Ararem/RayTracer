# Todos

## Misc

* Swap floats to doubles and test the performance loss (might be preferable to use doubles everywhere instead of casting all the time)
* Update all docs for new API and ensure everything is correct
* Redo logging to ensure everything is logged correctly, at the appropriate levels (mainly UI things)

## Materials/Textures

* ***IMPORTANT***: Fix refraction calculation
* Implement some actual textures other than plain colours
    * Images
    * Procedural (Noise)
* Add some sort of texturing to sky-boxes (HDRI images?)
* Shaders (Materials) should be able to modify some the surface normal (see [HitRecord](HitRecord.cs))

## Models/Shapes

* Import 3D models

## Performance

* See if there are any shared variables/calculations per ray that are used across multiple hittables, and see if we can pass that into the `TryHit`
  functions (e.g. `1/rayDirection`) for some of the IQ hittables

## Debugging/Development

* Cancel render and return to main menu
* Store scenes as files rather than C# properties
* Custom shader/shape import from dll's rather than builtin?