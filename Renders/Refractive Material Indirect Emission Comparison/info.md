# Refractive Material In/Direct Emission Comparison
Rendered: 14/05/2022 (3970579a)  
Took 20 mins @ 1080x1080, 100 passes, unlimited (8) threads

## Changes
Modify BuiltinScenes.cs line 148  
`new ("Small Box Sphere", new Sphere(new Vector3(212.5f, 265f, 147.5f), 100), new EmissiveRefractiveMaterial(RefractiveMaterial.GlassIndex, White, Blue * __YOUR_INTENSITY__, __DIRECT_EMISSION_ENABLED__)),`