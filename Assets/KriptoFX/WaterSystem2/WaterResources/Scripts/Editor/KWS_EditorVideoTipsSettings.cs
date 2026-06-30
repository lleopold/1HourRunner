using System;


namespace KWS
{
    internal class KWS_EditorVideoTipsSettings
    {
        public class VideoSettings
        {
            public string VideoClipFileName;

            public Description[] Descriptions;

            internal struct Description
            {
                public string TitleText;
                public string DescriptionText;
                public float  StartTime;
                public float  EndTime;

                public Description(string titleText, string descriptionText, float startTime, float endTime)
                {
                    TitleText       = titleText;
                    DescriptionText = descriptionText;
                    StartTime       = startTime;
                    EndTime         = endTime;
                }
            }

            public VideoSettings(string videoFileName, string titleText, string descriptionText)
            {
                VideoClipFileName = videoFileName;

                Descriptions = new[] { new Description(titleText, descriptionText, 0, 10000) };
            }

            public VideoSettings(string videoFileName, params Description[] descriptions)
            {
                VideoClipFileName = videoFileName;

                Descriptions = descriptions;
            }
        }

        public class VideoTips
        {
            public static VideoSettings Transparent    = new VideoSettings("Transparent",    "Transparent",                  "controls how clear or opaque the water is");
            public static VideoSettings WaterColor     = new VideoSettings("WaterColor",     "Water Color (absorption)",     "defines how water absorbs light with depth, giving the liquid its natural tint, like ocean blue or cola brown");
            public static VideoSettings TurbidityColor = new VideoSettings("TurbidityColor", "Turbidity Color (scattering)", "defines how particles scatter light, adding murkiness, like algae-green lake or muddy river");


            public static VideoSettings Reflection = new VideoSettings("Reflection", "Reflection", "controls how water reflects the world. You can use SSR (very fast), Planar (accurate but heavy), or combine both: Planar for key objects, SSR for everything else");

            public static VideoSettings ScreenSpaceReflection       = new VideoSettings("ScreenSpaceReflection", "Screen Space Reflection", "adds real-time reflections based on what’s visible on the screen (very fast but limited: objects outside the view or behind others won’t reflect)");
            public static VideoSettings ReflectionResolutionQuality = new VideoSettings("ResolutionQuality",     "Resolution Quality",      "sets the texture resolution for reflections (higher = sharper and more detailed reflections, lower = faster performance)");

            public static VideoSettings ScreenSpaceReflectionSkybox = new VideoSettings("ScreenSpaceReflectionSkybox", "Screen Space Skybox", "when enabled, the sky is reflected from screen space. If objects (like a palm tree) block the sky, " +
                                                                                                                                              "their silhouette may be falsely reflected instead of clouds. When disabled, reflections use the skybox cubemap, which removes such artifacts but may leave gaps");

            public static VideoSettings ScreenSpaceReflectionBorderStretching = new VideoSettings("ScreenSpaceReflectionBorderStretching", "Border Stretching", "extends reflections near screen edges to reduce visible gaps. Helps hide SSR cut-off, but may cause stretched or distorted reflections");

            public static VideoSettings PlanarReflection = new VideoSettings("PlanarReflection", "Planar Reflection", "renders the scene twice (can cut FPS in half). Recommended to include only important objects (mountains, trees) and ignore small details (rocks, grass). " +
                                                                                                                      "Can be combined with SSR: large objects are always reflected, while small details are added from SSR");

            public static VideoSettings PlanarReflectionShadows = new VideoSettings("PlanarReflectionShadows", "Planar Reflection Shadows", "adds real-time shadows into planar reflections. Improves realism, but increases performance cost.");
            public static VideoSettings ClipPlaneOffset         = new VideoSettings("ClipPlaneOffset",         "Clip Plane Offset",         "shifts the reflection camera’s clipping plane. Helps hide artifacts near the water surface, but too high values may cut off reflected objects");
            public static VideoSettings AnisotropicReflection   = new VideoSettings("AnisotropicReflection",   "Anisotropic Reflection",    "makes distant reflections look rough and stretched, simulating how countless small waves blur reflections in reality");

            public static VideoSettings OverrideSkyColor = new VideoSettings("OverrideSkyColor", "Override Sky Color", "lets you replace the fallback sky color used in reflections. Useful in dark areas (like caves), where SSR/Planar may lack data and show unwanted bright sky reflections.");
            public static VideoSettings ReflectSun       = new VideoSettings("ReflectSun",       "Sun reflection",     "draws the sun path on water using physically based shading. The reflection comes from the light source position, so it’s visible even if the sun itself is hidden by clouds or geometry");
            public static VideoSettings SunCloudiness    = new VideoSettings("SunCloudiness",    "Sun Cloudiness",     "adjusts the roughness of the sun reflection. Simulates the sun being blurred by clouds or bloom: instead of a sharp disk, it appears as a softer glowing spot");
            public static VideoSettings SunStrength      = new VideoSettings("SunStrength",      "Sun Strength",       "controls the intensity of the sun reflection on water. Uses HDR range values, so high intensity works well with bloom, making the sun path glow naturally");


            public static VideoSettings RefractionMode = new VideoSettings("RefractionMode", "Refraction Mode", "controls how underwater objects bend and shift. Physical mode approximates real light refraction, more depth-based distortion but can show artifacts.");
            public static VideoSettings Dispersion     = new VideoSettings("Dispersion",     "Dispersion",      "enables chromatic dispersion: each color refracts differently, creating subtle rainbow fringes and colored caustics (prism effect). More realistic, extra cost; overuse can exaggerate fringing");


            public static VideoSettings DynamicSimulation = new VideoSettings("DynamicSimulation", "Dynamic Waves Simulation", "real-time simulation for rivers, lakes, and other local water zones. It calculates flow, ripples, and interactions with terrain and objects.");


            public static VideoSettings WetEffect = new VideoSettings("WetEffect", "Wet Effect", "adds a wet surface effect on objects touching the water, making them look darker and shinier.");

            public static VideoSettings WetnessHeightAboveWater = new VideoSettings("WetnessHeightAboveWater", "Wetness Height Above Water", "sets how far above the waterline the wet surface effect appears. " +
                                                                                                                                             "Higher values make objects look wet at greater heights, lower values limit wetness to just near the waterline");

            public static VideoSettings WetStrength = new VideoSettings("WetStrength", "Wet Strength", "controls how intense the wet surface effect is. Higher values make objects look darker and shinier when wet, lower values make the effect more subtle");


            public static VideoSettings OceanFoam = new VideoSettings("OceanFoam", "Ocean Foam", "adds foam on wave crests");


            public static VideoSettings VolumetricLighting            = new VideoSettings("VolumetricLighting",            "Volumetric Lighting", "adds light rays, shadows, and caustics from the sun, point lights, and spotlights. Creates deep, atmospheric scattering, but at high quality it becomes performance-heavy");
            public static VideoSettings VolumetricLightingResolution  = new VideoSettings("VolumetricLightingResolution",  "Volumetric Lighting Resolution", "a critical setting for performance. Higher resolution gives sharper rays and caustics, but cost grows drastically with more lights and especially with shadows");
            public static VideoSettings VolumetricLightingIterations  = new VideoSettings("VolumetricLightingIterations",  "Volumetric Lighting Iterations", "number of raymarch slices. Each slice calculates lighting again, so more iterations mean less noise but much higher cost");
            public static VideoSettings VolumetricLightingBlur        = new VideoSettings("VolumetricLightingBlur",        "Volumetric Lighting Blur", "blurs the volumetric lighting to smooth out noise. Makes the effect softer and cleaner, but can also create noticeable light halos around objects");
            public static VideoSettings VolumetricLightingFullCaustic = new VideoSettings("VolumetricLightingFullCaustic", "Volumetric Lighting Caustic", "controls which lights create caustics. Directional-only mode is faster; All Lights mode adds point/spotlights too but can heavily impact performance");

            public static VideoSettings VolumetricLightTemporalAccumulationFactor = new VideoSettings("VolumetricLightTemporalAccumulationFactor", "Volumetric Light Temporal Accumulation Factor", "controls blending of volumetric lighting across frames. Higher values = smoother, " +
                                                                                                                                                                                                    "less noisy result but more motion blur/ghosting; lower values = sharper but noisier.");


            public static VideoSettings Caustic           = new VideoSettings("Caustic",           "Caustic",                  "light patterns projected onto surfaces under water (like moving sun spots on the seabed). Adds realism but costs performance at higher quality");
            public static VideoSettings CausticResolution = new VideoSettings("CausticResolution", "Ocean Caustic Resolution", "sets the texture resolution for ocean caustics. Higher resolution gives sharper, more detailed light patterns, but costs more performance");
            public static VideoSettings CausticFiltering  = new VideoSettings("CausticFiltering",  "Ocean Caustic Filtering",  "smooths caustic edges using bicubic filtering. Improves visual quality with softer edges, but increases performance cost");
            public static VideoSettings CausticDispersion = new VideoSettings("CausticDispersion", "Ocean Caustic Dispersion", "splits caustics into color channels, creating subtle rainbow-like patterns (like light through a prism). Improves realism but adds extra cost");
            public static VideoSettings CausticStrength   = new VideoSettings("CausticStrength",   "Caustic Strength",         "controls the intensity of caustic light patterns under water. Higher values make caustics brighter and more pronounced");


            public static VideoSettings Underwater = new VideoSettings("Underwater", "Underwater", "enables full underwater rendering: water tint, refraction, caustics, fog, and light scattering when the camera goes below the surface");

            public static VideoSettings UnderwaterReflectionMode = new VideoSettings("UnderwaterReflectionMode", "Underwater Reflection Mode",
                                                                                     "sets how reflections are rendered underwater. Simple mode uses only normal refraction. Physical mode applies Snell’s law for angle-based refraction and adds internal reflections via SSR");

            public static VideoSettings UnderwaterHalfLineTensionEffect = new VideoSettings("UnderwaterHalfLineTensionEffect", "Underwater Half Line Tension Effect", "toggles the bright transition line effect (tension) visible near the water surface when looking from underwater");
            public static VideoSettings UnderwaterHalfLineTensionScale  = new VideoSettings("UnderwaterHalfLineTensionScale",  "Underwater Half Line Tension Scale",  "controls the size of the tension line");
            public static VideoSettings WaterDropsEffect                = new VideoSettings("WaterDropsEffect",                "Water Drops Effect",                  "adds water drop distortion on the camera lens when entering/exiting the surface, and also spawns drops from dynamic splash simulation");


            public static VideoSettings QuadTree              = new VideoSettings("QuadTree", "QuadTree", "the entire ocean is rendered using a quadtree: the mesh is split into chunks with dynamic LOD (less detail at distance) and invisible parts culled. All chunks are drawn with instancing in a single draw call.");
            public static VideoSettings QuadTreeMeshDetailing = new VideoSettings("QuadTreeMeshDetailing", "Mesh Detailing", "sets how many polygons each ocean chunk has. Higher values give more detailed waves, but greatly increase polygon count and reduce performance");

            public static VideoSettings QuadTreeMeshDetailingFarDistance = new VideoSettings("QuadTreeMeshDetailingFarDistance", "Mesh Detailing Far Distance", "sets how far the quadtree subdivides the ocean mesh. Up to this distance, the mesh has detailed polygons and waves; " +
                                                                                                                                                                "beyond it, the ocean continues as a flat stretched surface with no extra detail.");


            public static VideoSettings TransparentSortingPriority = new VideoSettings("TransparentSortingPriority", "Transparent Sorting Priority",
                                                                                       "controls the render order of transparent water relative to other transparent objects. Note: regular transparent geometry doesn’t render correctly underwater (no fog/lighting, drawn after water). " +
                                                                                       "To fix, you can use semi-transparent geometry with the opaque dithered shader KriptoFX/KWS2/TransparentLit_Dithered");

            public static VideoSettings DrawToDepth                  = new VideoSettings("DrawToDepth",                  "Draw To Depth",                    "writes water depth into the scene depth buffer before post-processing, so effects like Depth of Field work correctly with water");
            public static VideoSettings WideAngleCameraRenderingMode = new VideoSettings("WideAngleCameraRenderingMode", "Wide Angle Camera Rendering Mode", "disables occlusion culling for the ocean quadtree, allowing it to render from all angles (useful for 360° video capture).");


            public static VideoSettings SimulationFPSMultiplier = new VideoSettings("SimulationFPSMultiplier", "Simulation FPS Multiplier", "scales the update rate for all water simulations, including ocean and dynamic zones. Higher values make waves update faster and smoother but cost more performance; " +
                                                                                                                                            "lower values slow down updates and save resources");


            ////////////////////////////////////////////////////////////////////////////////  ocean module ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            public static VideoSettings WindSpeed       = new VideoSettings("WindSpeed",       "Wind Speed",      "controls the size and speed of ocean waves. Higher values make waves bigger and faster, and automatically increase mesh detail");
            public static VideoSettings WindRotation    = new VideoSettings("WindRotation",    "Wind Rotation",   "sets the wind direction in degrees, which defines the main orientation of the ocean waves.");
            public static VideoSettings WindTurbulence  = new VideoSettings("WindTurbulence",  "Wind Turbulence", "adds randomness to wind direction, making waves less uniform and more chaotic");
            public static VideoSettings FftWavesQuality = new VideoSettings("FftWavesQuality", "Waves Quality",   "sets the resolution of FFT textures used for ocean waves. Higher quality gives more detailed waves, but increases GPU memory use and lowers performance");

            public static VideoSettings FftWavesCascades = new VideoSettings("FftWavesCascades", "Waves Cascades",
                                                                             "number of wave layers with different sizes and speeds. More cascades reduce tiling and make the ocean more detailed, but require more performance. " +
                                                                             "For calm water or small waves, fewer cascades are enough since the difference is hardly visible");

            public static VideoSettings WavesAreaScale              = new VideoSettings("WavesAreaScale", "Waves Area Scale", "sets the overall size of simulated waves. Larger values make waves cover a wider area and look more massive, smaller values give tighter, denser wave patterns");
            public static VideoSettings OceanFoamStrength           = new VideoSettings("OceanFoamStrength", "Ocean Foam Strength", "sets the threshold for foam generation based on wave speed. Higher values make more waves cross the threshold, producing more foam; lower values limit foam to the strongest waves.");
            public static VideoSettings OceanFoamLifetimeMultiplier = new VideoSettings("OceanFoamLifetimeMultiplier", "Ocean Foam Lifetime Multiplier", "multiplies the foam lifetime. Higher values = foam stays longer, lower values = foam disappears faster");
            public static VideoSettings FoamType                    = new VideoSettings("FoamType", "Foam Texture Type", @"selects one of four foam texture styles. The foam texture is located at Assets\KriptoFX\WaterSystem2\WaterResources\Resources\Textures\FluidsFoamTex You can edit it to change the foam pattern");
            public static VideoSettings FoamContrast                = new VideoSettings("FoamContrast", "Foam Texture Contrast", "adjusts foam exposure using a power function (pow(x, Value)). Higher values increase contrast, lower values flatten it.");
            public static VideoSettings FoamScale                   = new VideoSettings("FoamScale", "Foam Texture Scale", "sets the tiling of the foam texture");


            ////////////////////////////////////////////////////////////////////////////////  zone module ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            public static VideoSettings ZoneType = new VideoSettings("ZoneType", new[]
            {
                new VideoSettings.Description("Static Zone Type", "a non-moving zone used for rivers, lakes, and local water flows. At startup it captures terrain depth and geometry, so water dynamically adapts and flows around obstacles. " +
                                                                  "Supports caching for faster loading, while still keeping dynamic effects like sources, drains, and objects.", 0, 14.0f),

                new VideoSettings.Description("Baked Zone Type", "a cached zone frozen into textures after simulation. It’s extremely cheap at runtime (no CPU/GPU simulation), making it ideal for static rivers, waterfalls, or places where flow never changes. " +
                                                                 "After baking, the water no longer reacts to terrain or forces, and foam is not supported", 14.5f, 26.0f),

                new VideoSettings.Description("Movable Zone Type", "a zone that can move or follow objects, for example player-triggered simulations. It doesn’t cache terrain depth, so it won’t wrap around static geometry, " +
                                                                   "but still fully interacts with dynamic objects, forces, and emitters. Useful for moving gameplay-driven effects", 26.5f, 41)
            });

            public static VideoSettings ZoneIntersectionLayerMask = new VideoSettings("ZoneIntersectionLayerMask", "Intersection Layer Mask", "defines which scene layers are used when capturing depth and geometry for water zones. Only objects on these layers will affect water flow, intersections, and foam generation");

            public static VideoSettings ZoneSimulationResolutionPerMeter = new VideoSettings("ZoneSimulationResolutionPerMeter", "Simulation Resolution Per Meter", "sets how many simulation pixels are used per world unit. Simulation runs at ~60 FPS (≈60 steps per second), " +
                                                                                                                                                                    "so higher pixel density makes water appear slower and smoother, while lower density makes it flow faster but with less detail");

            public static VideoSettings ZoneEvaporationRate     = new VideoSettings("ZoneEvaporationRate",     "Zone Evaporation Rate", "controls how quickly water disappears inside a simulation zone. Higher values make water dry out faster, lower values let it persist longer");
            public static VideoSettings ZoneFlowSpeedMultiplier = new VideoSettings("ZoneFlowSpeedMultiplier", "Flow Speed Multiplier", "adjusts the speed of the flow map, not the physical simulation. Works like faster scrolling of flow textures and also speeds up foam and splash particles");

            public static VideoSettings ZoneFlowType = new VideoSettings("ZoneFlowType", "Zone Flow Type", "Flow Map Foam is cheaper and just follows the static flow texture, good for performance but less realistic. Advected Foam is carried by the actual simulation, " +
                                                                                                           "creating dynamic swirling and stretching around obstacles, but costs more performance");

            public static VideoSettings ZoneFoamStrength = new VideoSettings("ZoneFoamStrength", "Foam Strength", "sets the threshold for foam generation. Shallow (River) Foam raises or lowers the chance of foam appearing in rivers and near shore, " +
                                                                                                                  "while Ocean Foam does the same for the open ocean (starting roughly ~5 meters from the coast). Higher values = more foam appears at lower flow speeds");

            public static VideoSettings ZoneFoamDisappearSpeed = new VideoSettings("ZoneFoamDisappearSpeed", "Foam Disappear Speed", "controls how fast foam fades away. Shallow (River) affects rivers and shorelines, Ocean controls the open ocean (about 5m from the coast). " +
                                                                                                                                     "Higher values = foam vanishes quicker, lower values = foam lingers longer");

            public static VideoSettings ZoneFoamTexture = new VideoSettings("ZoneFoamTexture", "Zone Foam Texture", "enables or disables foam rendering in the zone. When enabled, foam is drawn as a scrolling texture (using flowmap or advection flow)");
            public static VideoSettings ZoneFoamAlpha   = new VideoSettings("ZoneFoamAlpha",   "Foam Alpha",        "controls the transparency of foam");

            public static VideoSettings ZoneFoamParticles = new VideoSettings("ZoneFoamParticles", "Foam Particles", "renders small foam particles in zones. Adds detail and realism, but high particle counts can be performance-heavy, especially when many are visible at once");

            public static VideoSettings ZoneMaxFoamParticles = new VideoSettings("ZoneMaxFoamParticles", "Max Foam Particles", "defines the maximum number of foam particles stored in the GPU buffer. Even if few particles are visible, the entire buffer is allocated in memory, " +
                                                                                                                               "which still consumes VRAM and adds a small processing overhead. Use lower values if you don’t need dense foam detail to save performance and memory");

            public static VideoSettings ZoneFoamRenderingDistance = new VideoSettings("ZoneFoamRenderingDistance", "Foam Rendering Distance",
                                                                                      "limits how far foam particles are rendered from the camera. Increasing the distance makes foam visible farther away but costs more performance; lowering it reduces load by culling distant particles");

            public static VideoSettings ZoneFoamParticlesScale =
                new VideoSettings("ZoneFoamParticlesScale", "Foam Particles Scale", "controls the size of foam particles. Larger particles appear softer and more blurred but increase overdraw and reduce performance; smaller particles look sharper and cost less to render");

            public static VideoSettings ZoneFoamParticlesAlpha = new VideoSettings("ZoneFoamParticlesAlpha", "Foam Particles Alpha", "controls the transparency of foam particles");
            public static VideoSettings ZoneEmissionRate = new VideoSettings("ZoneEmissionRate", "Emission Rate", "controls how many foam particles are spawned per second. Higher values create denser, more detailed foam trails/splashes, but also increase GPU load; lower values reduce density and improve performance");
            public static VideoSettings ZonePhytoplanktonEmission = new VideoSettings("ZonePhytoplanktonEmission", "Phytoplankton Emission", "makes foam emit light, simulating bioluminescent plankton");


            public static VideoSettings ZoneSplashes = new VideoSettings("ZoneSplashes", "Splashes", "enables rendering of splash particles in zones. Particles can be performance-heavy, especially in large numbers near the camera, causing pixel overdraw. Use with caution");

            public static VideoSettings ZoneMaxSplashesParticles = new VideoSettings("ZoneMaxSplashesParticles", "Max Splashes Particles", "defines the maximum number of splash particles stored in the GPU buffer. Even if few particles are visible, the entire buffer is allocated in memory, " +
                                                                                                                                           "which still consumes VRAM and adds a small processing overhead. Use lower values if you don’t need dense foam detail to save performance and memory");

            public static VideoSettings ZoneSplashesRenderingDistance = new VideoSettings("ZoneSplashesRenderingDistance", "Rendering Distance",
                                                                                          "limits how far foam particles are rendered from the camera. Increasing the distance makes foam visible farther away but costs more performance; lowering it reduces load by culling distant particles");

            public static VideoSettings ZoneSplashesParticlesScale = new VideoSettings("ZoneSplashesParticlesScale", "Particles Scale",
                                                                                       "controls the size of splash particles and is the most performance-critical parameter. Larger or more numerous particles greatly increase overdraw and reduce FPS; smaller, fewer particles render much faster");

            public static VideoSettings ZoneSplashesParticlesAlpha = new VideoSettings("ZoneSplashesParticlesAlpha", "Particles Alpha", "controls the transparency of splashes particles");


            ////////////////////////////////////////////////////////////////////////////////  zone effectors  ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            public static VideoSettings EffectorType = new VideoSettings("EffectorType", new[]
            {
                new VideoSettings.Description("Water Source", "continuously adds water into the simulation", 0, 6.0f),

                new VideoSettings.Description("Water Drain", "removes water from the simulation, simulating sinks, drains, or holes where water flows out", 6.4f, 12.5f),

                new VideoSettings.Description("Force Object", "applies directional force to the water surface, creating ripples and flow disturbances. Useful for simulating movement from boats, ships, characters, or any objects moving through water. " +
                                                              "Note: water reacts to the applied force but doesn’t flow around the object itself. Use Obstacle Objects when you need water to flow around geometry", 13.05f, 22),

                new VideoSettings.Description("Obstacle Object", "creates a physical barrier that blocks and redirects water flow. Designed for dynamically appearing or slowly moving obstacles like bridges, dams, logs, rocks, etc. If the object moves too fast (e.g., a boat), results may look incorrect. " +
                                                                 "For fast-moving objects, use Force Object instead", 22.35f, 34.05f)
            });

            public static VideoSettings EffectorSourceMeshType = new VideoSettings("EffectorSourceMeshType", "Mesh Type", "defines the shape of the effector’s influence area. You can choose between simple primitives: sphere, cube, or triangle, " +
                                                                                                                          "depending on how you want the force or interaction to spread through the water.");

            public static VideoSettings EffectorForceMeshType = new VideoSettings("EffectorForceMeshType", "Mesh Type", "defines the shape of the effector’s influence area. You can choose between simple primitives: sphere, cube, or triangle, " +
                                                                                                                        "depending on how you want the force or interaction to spread through the water.");

            public static VideoSettings EffectorMotionForce            = new VideoSettings("EffectorMotionForce",   "Motion Force",   "adds force only when the effector object moves. Useful for effects like ripples or trails behind moving objects (e.g., a boat wake)");
            public static VideoSettings EffectorConstantForce          = new VideoSettings("EffectorConstantForce", "Constant Force", "applies continuous force even when the object is stationary, like a drain or water outlet. Works only if Effector Directional Velocity is set to a specific direction");
            public static VideoSettings EffectorSourceConstantFlowRate = new VideoSettings("EffectorSourceConstantFlowRate", "Constant Force", "applies continuous force even when the object is stationary, like a drain or water outlet. Works only if Effector Directional Velocity is set to a specific direction");

            public static VideoSettings EffectorDirectionalVelocity = new VideoSettings("EffectorDirectionalVelocity", "Directional Velocity", "defines the direction of the applied force, based on the object’s forward axis. The flow direction doesn’t depend on object movement and can even oppose it " +
                                                                                                                                               "(e.g., a boat engine pushing water backward)");
            public static VideoSettings EffectorSourceDirectionalVelocity = new VideoSettings("EffectorSourceDirectionalVelocity", "Directional Velocity", "defines the direction of the applied force, based on the object’s forward axis. The flow direction doesn’t depend on object movement and can even oppose it " +
                                                                                                                                                           "(e.g., a boat engine pushing water backward)");

            public static VideoSettings EffectorUseRotationForce = new VideoSettings("EffectorUseRotationForce", "Use Rotation Force", "adds additional force during object rotation, simulating circular water disturbance when the effector turns");
            public static VideoSettings EffectorWaterSurfaceIntersection =  new VideoSettings("EffectorWaterSurfaceIntersection",  "Water Surface Intersection", "controls how the effector interacts with the water surface. When enabled, it limits the effect to areas where the effector actually intersects the water, " +
                                                                                                                                                                  "improving realism but adding extra GPU cost. When disabled, the effector influences the water regardless of its height above or below the surface");
            
            //public static VideoSettings   =  new VideoSettings("",  "", "");
        }
    }
}