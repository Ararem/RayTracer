# RayTracer

---

## Overview

This is my personal ray-tracer (path-tracer?) project, built to be as realistic as possible (at the cost of performance). Rays are traced recursively (starting at
the camera), spawning new rays each bounce until a limit is reached, whereupon the colours are calculated in reverse (starting at the last bounce).
Since each material can *completely* modify how the colour changes, this allows for some fancy materials that (while technically unrealistic) look
much better than simply using raw emission and albedo -
see [Refractive Material In/Direct Emission Comparison](Renders/Refractive%20Material%20Indirect%20Emission%20Comparison) (only emits when not looking
at it directly).

## How to run

1. Clone the `git` repo, making sure to include submodules (the repo references my helper
   project [LibArarem](LibArarem/LibArarem.Core/LibArarem.Core.csproj))
2. Open the solution file in your IDE (I use Rider)
3. Run the raytracer using one of the `RayTracer.Display.XXX` projects
    * If using the `SpectreConsole` runner, use command-line arguments to change render options, and select the scene using the console
    * If using the `EtoForms` runner, everything is controlled from the UI
    * If using the `Dev` runner (which you shouldn't), everything is hardcoded in the `MainForm` class. Note that this won't save an image on
      completion
4. Once the render is complete, the image should be saved to a file (where depends on which runner), and ***might*** open in your image viewer
   program\*  
   \**Opening in default programs is complicated; I test it on POP OS!*

## Project Explanation

### LibEternal

Helper library that contains non-project-specific code (e.g. logging related helper classes)

### Ararem.RayTracer.Core

Main Ray-Tracing library, contains all the code that is needed to be able to create/render scenes. Doesn't contain any actual implementations for
anything like scenes or hittables (shapes), just the definitions. If you bring your own implementations for everything, this is all you need.

### Ararem.RayTracer.Impl

Library containing the implementations for [RayTracer.Core](#araremraytracercore). Contains materials, hittables, textures, some builtin scenes, lights and
skyboxes.

### Ararem.RayTracer.Display.SpectreConsole

Console-based implementation of the 'engine' - allows selection of some of the builtin scenes from [RayTracer.Impl](#araremraytracerimpl). Render options
are passed in via command line arguments (run with `--help` to see them), scene is selected using arrow keys.

### Ararem.RayTracer.Display.EtoForms

Same as [RayTracer.Display.SpectreConsole](#araremraytracerdisplayspectreconsole) but with a proper GUI. Render options and scene are selected using GUI
controls shown when the app starts. Also implements a logger to the console.

### Ararem.RayTracer.Display.Dev

A copy of [RayTracer.Display.EtoForms](#araremraytracerdisplayetoforms) but modified for ease of development. It's meant to be hardcoded and modifiable at
runtime through [Hot Reload](https://devblogs.microsoft.com/dotnet/introducing-net-hot-reload/). You'll probably only want to use this when you're
prototyping/designing implementations of hittables, materials, etc.

### Ararem.RayTracer.Benchmarks

A not-very used project that I use (rarely) to test multiple ways of doing something to see which is the most efficient/fastest. This is for
development/debugging only, you probably should just ignore & unload it

## Important types

| Type                                                                     | What it does                                                                                                                 |
|:-------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------|
| [Async Render Job](Ararem.RayTracer.Core/AsyncRenderJob.cs)              | The renderer - handles recursive ray tracing, colour calculation, lighting, etc                                              | 
| [Camera](Ararem.RayTracer.Core/Camera.cs)                                | Handles generation of view rays                                                                                              |
| [Materials](Ararem.RayTracer.Core/Base%20Type%20Definitions/Material.cs) | Materials handle how the light scatters when the object is hit, as well as changes in colour when rays bounce                |
| [Textures](Ararem.RayTracer.Core/Base%20Type%20Definitions/Texture.cs)   | Textures return a colour when an input hit is passed in (this allows for textures based on world-space coordinates and UV's) |
| [Hittable](Ararem.RayTracer.Core/Base%20Type%20Definitions/Hittable.cs)  | Hittables are in charge of calculating whether a given ray intersects with itself                                            |
| [Scene](Ararem.RayTracer.Core/Scene.cs)                                  | Scenes store a record of a group of objects and lights, as well as the camera and skybox                                     |
| [Light](Ararem.RayTracer.Core/Base%20Type%20Definitions/Light.cs)        | Lights increase the brightness of a hit, normally by checking shadow rays                                                    |
| [Render Options](Ararem.RayTracer.Core/RenderOptions.cs)                 | Stores the settings used when rendering, such as render width/height, and number of passes to render                         |
| [Builtin Scenes](Ararem.RayTracer.Impl/Builtin/BuiltinScenes.cs)         | Static class containing properties that store pre-made scenes.                                                               |