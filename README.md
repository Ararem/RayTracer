# RayTracer

---

## Overview

This is my personal ray-tracer project, built to be as realistic as possible (at the cost of performance). Rays are traced recursively (starting at
the camera), spawning new rays each bounce until a limit is reached, whereupon the colours are calculated in reverse (starting at the last bounce).
Since each material can *completely* modify how the colour changes, this allows for some fancy materials that (while technically unrealistic) look
much better than simply using raw emission and albedo -
see [Refractive Material In/Direct Emission Comparison](Renders/Refractive%20Material%20Indirect%20Emission%20Comparison) (only emits when not looking
at it directly).

## How to run

1. Clone the `git` repo, making sure to include submodules (the repo references my helper
   project [LibEternal](LibEternal/LibEternal.Core/LibEternal.Core.csproj))
2. Open the solution file in your IDE (I use Rider)
3. Run the raytracer using one of the `RayTracer.Display.XXX` projects
    * If using the `SpectreConsole` runner, use command-line arguments to change render options, and select the scene using the console
    * If using the `EtoForms` runner, everything is controlled from the UI
4. Once the render is complete, the image should be saved to a file (where depends on which runner), and ***might*** open in your image viewer
   program\*  
   \**Opening in default programs is complicated; I test it on POP OS!*

## Project Explanation

### LibEternal

Helper library that contains non-project-specific code (e.g. logging related helper classes)

### RayTracer.Core

Main Ray-Tracing library, contains all the code that is needed to be able to create/render scenes. Doesn't contain any actual implementations for
anything like scenes or hittables (shapes)

### RayTracer.Impl

Library containing the implementations for [RayTracer.Core](#raytracercore). Contains materials, hittables, textures, some builtin scenes, lights and
skyboxes

### RayTracer.Display.SpectreConsole

Console-based implementation of the 'engine' - allows selection of some of the builtin scenes from [RayTracer.Impl](#raytracerimpl). Render options
are passed in via command line arguments (run with `--help` to see them), scene is selected using arrow keys.

### RayTracer.Display.EtoForms

Same as [RayTracer.Display.SpectreConsole](#raytracerdisplayspectreconsole) but with a proper GUI. Render options and scene are selected using GUI
controls shown when the app starts. Also implements a logger to the console.

### RayTracer.Benchmarks

A not-very used project that I use (rarely) to test multiple ways of doing something to see which is the most efficient/fastest. This is for
development/debugging only, you probably should just ignore & unload it

## Important types

| Type                                                              | What it does                                                                                                                 |
|-------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------|
| [Async Render Job](RayTracer.Core/AsyncRenderJob.cs)              | The renderer - handles recursive ray tracing, colour calculation, lighting, etc                                              | 
| [Camera](RayTracer.Core/Camera.cs)                                | Handles generation of view rays                                                                                              |
| [Materials](RayTracer.Core/Base%20Type%20Definitions/Material.cs) | Materials handle how the light scatters when the object is hit, as well as changes in colour when rays bounce                |
| [Textures](RayTracer.Core/Base%20Type%20Definitions/Texture.cs)   | Textures return a colour when an input hit is passed in (this allows for textures based on world-space coordinates and UV's) |
| [Hittable](RayTracer.Core/Base%20Type%20Definitions/Hittable.cs)  | Hittables are in charge of calculating whether a given ray intersects with itself                                            |
| [Scene](RayTracer.Core/Scene.cs)                                  | Scenes store a record of a group of objects and lights, as well as the camera and skybox                                     |
| [Light](RayTracer.Core/Base%20Type%20Definitions/Light.cs)        | Lights increase the brightness of a hit, normally by checking shadow rays                                                    |
| [Render Options](RayTracer.Core/RenderOptions.cs)                 | Stores the settings used when rendering, such as render width/height, and number of passes to render                         |