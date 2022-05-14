# RayTracer

---

## Overview
This is my personal ray-tracer project, built to be as realistic as possible (at the cost of performance).

# API Explanation
| File/Type                                                 | What it does                                                                                                        |
|-----------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------|
| [Async Render Job](RayTracer.Core/Core/AsyncRenderJob.cs) | The renderer - handles ray tracing, colour calculation, lighting, etc                                               | 
| [Camera](RayTracer.Core/Core/Camera.cs)                   | Handles generation of view rays                                                                                     |
| [Materials](RayTracer.Core/Materials)                     | Materials handle how the light scatters when the object is hit, as well as changes in colour when rays bounce       |
| [Textures](RayTracer.Core/Textures)                       | Textures return a colour when an input hit is passed in (this allows for textures based on world-space coordinates) |
| [Hittable](RayTracer.Core/Hittables)                      | Hittables are in charge of calculating whether a given ray intersects with itself                                   |
| [Scene](RayTracer.Core/Core/Scene.cs)                     | Scenes store a record of a group of objects and lights, as well as the camera and skybox                            |