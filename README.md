# RayTracer

---

## Overview
This is my personal ray-tracer project, built to be as realistic as possible (at the cost of performance). Rays are traced recursively (starting at the camera), spawning new rays each bounce until a limit is reached, whereupon the colours are calculated in reverse (starting at the last bounce). Since each material can *completely* modify how the colour changes, this allows for some fancy materials that (while technically unrealistic) look much better than simply using raw emission and albedo - see [Refractive Material In/Direct Emission Comparison](Renders/Refractive%20Material%20Indirect%20Emission%20Comparison) (only emits when not looking at it directly)

## How to run
1. Clone the `git` repo, making sure to include submodules (the repo references my helper project [LibEternal](LibEternal/LibEternal.Core/LibEternal.Core.csproj))
2. Open the solution file in your IDE (I use Rider)
3. Run the raytracer using one of the `RayTracer.Display.XXX` projects
   * If using the `SpectreConsole` runner, use command-line arguments to change render options, and select the scene using the console
   * If using the `EtoForms` runner, everything is controlled from the UI
4. Once the render is complete, the image should be saved to a file (where depends on which runner), and ***might*** open in your image viewer program\*  
\**i test it on POP OS!*

## API Explanation
| File/Type                                                 | What it does                                                                                                                 |
|-----------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------|
| [Async Render Job](RayTracer.Core/Core/AsyncRenderJob.cs) | The renderer - handles recursive ray tracing, colour calculation, lighting, etc                                              | 
| [Camera](RayTracer.Core/Core/Camera.cs)                   | Handles generation of view rays                                                                                              |
| [Materials](RayTracer.Core/Materials)                     | Materials handle how the light scatters when the object is hit, as well as changes in colour when rays bounce                |
| [Textures](RayTracer.Core/Textures)                       | Textures return a colour when an input hit is passed in (this allows for textures based on world-space coordinates and UV's) |
| [Hittable](RayTracer.Core/Hittables)                      | Hittables are in charge of calculating whether a given ray intersects with itself                                            |
| [Scene](RayTracer.Core/Core/Scene.cs)                     | Scenes store a record of a group of objects and lights, as well as the camera and skybox                                     |
| [Light](RayTracer.Core/Environment/Light.cs)              | Lights increase the brightness of a hit, normally by checking shadow rays                                                    |