using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace SciFiForge
{
    public class HologramShaderGUI : ShaderGUI
    {
        // Foldout states
        private static bool showMainSettings = true;
        private static bool showHologramSettings = true;
        private static bool showEffectsSettings = false;
        private static bool showAdvancedSettings = false;
        private static bool showPresets = true;
        
        // Preset category foldouts
        private static bool showStandardPresets = false;
        private static bool showDataStreamPresets = false;
        private static bool showInterfacePresets = false;
        private static bool showProjectionPresets = false;
        
        private string searchQuery = "";
        private static Vector2 presetScrollPosition;
        
        // Color scheme
        private readonly Color headerColor = new Color(0.2f, 0.9f, 1f);
        private readonly Color categoryColor = new Color(0.3f, 1f, 0.9f);
        private readonly Color presetColor = new Color(0.9f, 0.9f, 0.9f);
        private readonly Color glowColor = new Color(0f, 1f, 1f);
        
        // Copy/Paste storage
        private static Dictionary<string, float> copiedFloatProperties = new Dictionary<string, float>();
        private static Dictionary<string, Vector4> copiedVectorProperties = new Dictionary<string, Vector4>();
        private static Dictionary<string, Color> copiedColorProperties = new Dictionary<string, Color>();
        private static Dictionary<string, Texture> copiedTextureProperties = new Dictionary<string, Texture>();
        
        // Shader type names
        private string[] shaderTypeNames = new string[] {
            "Standard Hologram",
            "Data Stream Hologram", 
            "Interface Hologram",
            "3D Projection"
        };
        
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            Material targetMat = materialEditor.target as Material;
            
            DrawHeader();
            DrawQuickAccessBar(targetMat, materialEditor);
            
            EditorGUILayout.Space();
            
            // Search bar
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍", GUILayout.Width(20));
            searchQuery = EditorGUILayout.TextField(searchQuery, GUILayout.Height(20));
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchQuery = "";
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Main sections
            DrawPresetsSection(targetMat, materialEditor);
            DrawMainSettings(materialEditor, properties);
            DrawHologramSettings(materialEditor, properties, targetMat);
            DrawEffectsSettings(materialEditor, properties, targetMat);
            DrawAdvancedSettings(materialEditor, properties, targetMat);
            
            EditorGUILayout.Space();
            DrawUtilityButtons(materialEditor, targetMat);
            
            // Render queue
            materialEditor.RenderQueueField();
            
            // Update keywords at the end
            UpdateKeywords(targetMat);
        }
        
        private void DrawHeader()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Logo and title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 24;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = headerColor;
            
            GUILayout.Label("⚡ SCIFI HOLOGRAM SHADER PRO ⚡", titleStyle);
            
            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.miniLabel);
            subtitleStyle.alignment = TextAnchor.MiddleCenter;
            subtitleStyle.fontSize = 12;
            GUILayout.Label("Version 2.0 | 56 Professional Presets | Universal Render Pipeline", subtitleStyle);
            
            // Stats
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            DrawStat("Presets", "56");
            DrawStat("Effects", "30+");
            DrawStat("Shader Types", "4");
            DrawStat("Quality", "AAA");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }
        
        private void DrawStat(string label, string value)
        {
            GUILayout.BeginVertical(GUILayout.Width(60));
            GUIStyle valueStyle = new GUIStyle(EditorStyles.boldLabel);
            valueStyle.alignment = TextAnchor.MiddleCenter;
            valueStyle.fontSize = 16;
            valueStyle.normal.textColor = categoryColor;
            GUILayout.Label(value, valueStyle);
            
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label(label, labelStyle);
            GUILayout.EndVertical();
        }
        
        private void DrawQuickAccessBar(Material mat, MaterialEditor materialEditor)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("⚡ QUICK ACCESS - TOP RATED PRESETS", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            
            // Most popular presets
            GUI.backgroundColor = new Color(0.2f, 0.8f, 1f);
            if (GUILayout.Button("💎 Classic Hologram", GUILayout.Height(35)))
            {
                ApplyClassicHologram(mat);
                UpdateKeywords(mat);
                EditorUtility.SetDirty(mat);
            }
            
            GUI.backgroundColor = new Color(0.1f, 1f, 0.8f);
            if (GUILayout.Button("📡 Data Stream", GUILayout.Height(35)))
            {
                mat.SetFloat("_ShaderType", 1);
                ApplyDataStream(mat);
                UpdateKeywords(mat);
                EditorUtility.SetDirty(mat);
            }
            
            GUI.backgroundColor = new Color(0.8f, 0.3f, 1f);
            if (GUILayout.Button("⚡ Glitch Core", GUILayout.Height(35)))
            {
                ApplyGlitchCore(mat);
                UpdateKeywords(mat);
                EditorUtility.SetDirty(mat);
            }
            
            GUI.backgroundColor = new Color(0.3f, 1f, 0.3f);
            if (GUILayout.Button("🔮 3D Projection", GUILayout.Height(35)))
            {
                mat.SetFloat("_ShaderType", 3);
                Apply3DProjection(mat);
                UpdateKeywords(mat);
                EditorUtility.SetDirty(mat);
            }
            
            GUI.backgroundColor = new Color(1f, 0.5f, 0.3f);
            if (GUILayout.Button("🌟 Energy Field", GUILayout.Height(35)))
            {
                ApplyEnergyField(mat);
                UpdateKeywords(mat);
                EditorUtility.SetDirty(mat);
            }
            
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }
        
        private void DrawPresetsSection(Material mat, MaterialEditor materialEditor)
        {
            showPresets = EditorGUILayout.Foldout(showPresets, "🎨 PROFESSIONAL PRESET LIBRARY (56 Presets)", true, EditorStyles.foldoutHeader);
            
            if (showPresets)
            {
                EditorGUI.indentLevel++;
                
                presetScrollPosition = EditorGUILayout.BeginScrollView(presetScrollPosition, GUILayout.Height(400));
                
                // Standard Hologram Presets (14)
                DrawPresetCategory("🔷 Standard Hologram Presets", ref showStandardPresets, new string[] {
                    "Classic Hologram", "Damaged Hologram", "Alien Technology", "Energy Field",
                    "Cyber Identity", "Ghostly Apparition", "Neon Pulse", "Quantum Matrix",
                    "Holo Blueprint", "Digital Mirage", "Starfield Hologram", "Glitch Core",
                    "Vaporwave Hologram", "Tech Essence"
                }, new Action<Material>[] {
                    ApplyClassicHologram, ApplyDamagedHologram, ApplyAlienTechnology, ApplyEnergyField,
                    ApplyCyberIdentity, ApplyGhostlyApparition, ApplyNeonPulse, ApplyQuantumMatrix,
                    ApplyHoloBlueprint, ApplyDigitalMirage, ApplyStarfieldHologram, ApplyGlitchCore,
                    ApplyVaporwaveHologram, ApplyTechEssence
                }, mat, materialEditor);
                
                // Data Stream Presets (14)
                DrawPresetCategory("📊 Data Stream Presets", ref showDataStreamPresets, new string[] {
                    "Data Stream", "Matrix Code", "Neural Network", "Digital Virus",
                    "Binary Flow", "Crypto Stream", "Code Cascade", "Signal Pulse",
                    "Data Vortex", "Quantum Data", "Info Wave", "Encrypted Flow",
                    "Network Traffic", "Blockchain Visual"
                }, new Action<Material>[] {
                    (Material m) => { m.SetFloat("_ShaderType", 1); ApplyDataStream(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 1); ApplyMatrixCode(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 1); ApplyNeuralNetwork(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 1); ApplyDigitalVirus(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 1); ApplyBinaryFlow(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 1); ApplyCryptoStream(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 1); ApplyCodeCascade(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 1); ApplySignalPulse(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 1); ApplyDataVortex(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 1); ApplyQuantumData(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 1); ApplyInfoWave(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 1); ApplyEncryptedFlow(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 1); ApplyNetworkTraffic(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 1); ApplyBlockchainVisual(m); UpdateKeywords(m); }
                }, mat, materialEditor);
                
                // Interface Presets (14)
                DrawPresetCategory("🖥️ Interface Presets", ref showInterfacePresets, new string[] {
                    "Advanced Interface", "Tactical Display", "Medical Scan", "Space Navigation",
                    "Cyber Console", "Holo Dashboard", "Augmented Reality", "Control Panel",
                    "Data Grid", "Tech HUD", "Virtual Terminal", "SciFi Interface",
                    "Command Center", "Neural Interface"
                }, new Action<Material>[] {
                    (Material m) => { m.SetFloat("_ShaderType", 2); ApplyAdvancedInterface(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 2); ApplyTacticalDisplay(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 2); ApplyMedicalScan(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 2); ApplySpaceNavigation(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 2); ApplyCyberConsole(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 2); ApplyHoloDashboard(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 2); ApplyAugmentedReality(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 2); ApplyControlPanel(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 2); ApplyDataGrid(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 2); ApplyTechHUD(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 2); ApplyVirtualTerminal(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 2); ApplySciFiInterface(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 2); ApplyCommandCenter(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 2); ApplyNeuralInterface(m); UpdateKeywords(m); }
                }, mat, materialEditor);
                
                // 3D Projection Presets (14)
                DrawPresetCategory("🌐 3D Projection Presets", ref showProjectionPresets, new string[] {
                    "3D Projection", "Star Map", "Molecular Structure", "Character Projection",
                    "Planet Hologram", "Ship Blueprint", "Orbital Scan", "Holo Globe",
                    "Cosmic Projection", "Galactic Map", "Structure Scan", "Holo Avatar",
                    "Quantum Core", "Energy Sphere"
                }, new Action<Material>[] {
                    (Material m) => { m.SetFloat("_ShaderType", 3); Apply3DProjection(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 3); ApplyStarMap(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 3); ApplyMolecularStructure(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 3); ApplyCharacterProjection(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 3); ApplyPlanetHologram(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 3); ApplyShipBlueprint(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 3); ApplyOrbitalScan(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 3); ApplyHoloGlobe(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 3); ApplyCosmicProjection(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 3); ApplyGalacticMap(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 3); ApplyStructureScan(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 3); ApplyHoloAvatar(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 3); ApplyQuantumCore(m); UpdateKeywords(m); },
                    (Material m) => { m.SetFloat("_ShaderType", 3); ApplyEnergySphere(m); UpdateKeywords(m); }
                }, mat, materialEditor);
                
                EditorGUILayout.EndScrollView();
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawPresetCategory(string categoryName, ref bool foldout, string[] presetNames, 
            Action<Material>[] presetActions, Material mat, MaterialEditor materialEditor)
        {
            // Filter by search
            if (!string.IsNullOrEmpty(searchQuery))
            {
                bool hasMatch = false;
                foreach (string preset in presetNames)
                {
                    if (preset.ToLower().Contains(searchQuery.ToLower()))
                    {
                        hasMatch = true;
                        break;
                    }
                }
                if (!hasMatch) return;
            }
            
            foldout = EditorGUILayout.Foldout(foldout, categoryName, true);
            
            if (foldout)
            {
                EditorGUI.indentLevel++;
                
                GUILayout.BeginVertical(EditorStyles.helpBox);
                
                int columns = 3;
                for (int i = 0; i < presetNames.Length; i += columns)
                {
                    GUILayout.BeginHorizontal();
                    
                    for (int j = 0; j < columns && i + j < presetNames.Length; j++)
                    {
                        int index = i + j;
                        
                        // Highlight if matches search
                        bool isMatch = !string.IsNullOrEmpty(searchQuery) && 
                                     presetNames[index].ToLower().Contains(searchQuery.ToLower());
                        
                        if (isMatch)
                        {
                            GUI.backgroundColor = new Color(1f, 1f, 0.5f);
                        }
                        
                        if (GUILayout.Button(presetNames[index], GUILayout.Height(25)))
                        {
                            presetActions[index](mat);
                            EditorUtility.SetDirty(mat);
                        }
                        
                        GUI.backgroundColor = Color.white;
                    }
                    
                    GUILayout.EndHorizontal();
                }
                
                GUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawMainSettings(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            showMainSettings = EditorGUILayout.Foldout(showMainSettings, "Main Settings", true, EditorStyles.foldoutHeader);
            
            if (showMainSettings)
            {
                EditorGUI.indentLevel++;
                
                GUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Shader Type
                MaterialProperty shaderTypeProp = FindProperty("_ShaderType", properties);
                EditorGUI.BeginChangeCheck();
                int shaderType = (int)shaderTypeProp.floatValue;
                shaderType = EditorGUILayout.Popup("Hologram Type", shaderType, shaderTypeNames);
                if (EditorGUI.EndChangeCheck())
                {
                    shaderTypeProp.floatValue = shaderType;
                }
                
                EditorGUILayout.Space();
                
                // Main Texture
                MaterialProperty mainTex = FindProperty("_MainTex", properties);
                MaterialProperty color = FindProperty("_Color", properties);
                materialEditor.TexturePropertySingleLine(new GUIContent("Main Texture"), mainTex, color);
                materialEditor.TextureScaleOffsetProperty(mainTex);
                
                // Blend Mode
                MaterialProperty blendMode = FindProperty("_BlendMode", properties);
                EditorGUI.BeginChangeCheck();
                int blend = (int)blendMode.floatValue;
                blend = EditorGUILayout.Popup("Blend Mode", blend, new string[] { "Opaque", "Transparent" });
                if (EditorGUI.EndChangeCheck())
                {
                    blendMode.floatValue = blend;
                    SetupBlendMode(materialEditor.target as Material, blend);
                }
                
                GUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawHologramSettings(MaterialEditor materialEditor, MaterialProperty[] properties, Material mat)
        {
            showHologramSettings = EditorGUILayout.Foldout(showHologramSettings, "Hologram Base", true, EditorStyles.foldoutHeader);
            
            if (showHologramSettings)
            {
                EditorGUI.indentLevel++;
                
                GUILayout.BeginVertical(EditorStyles.helpBox);
                
                materialEditor.RangeProperty(FindProperty("_HologramIntensity", properties), "Hologram Intensity");
                materialEditor.RangeProperty(FindProperty("_HologramOpacity", properties), "Hologram Opacity");
                materialEditor.RangeProperty(FindProperty("_HologramFlickerSpeed", properties), "Flicker Speed");
                materialEditor.RangeProperty(FindProperty("_HologramFlickerIntensity", properties), "Flicker Intensity");
                materialEditor.RangeProperty(FindProperty("_FlickerPattern", properties), "Flicker Pattern");
                materialEditor.RangeProperty(FindProperty("_FlickerOffset", properties), "Flicker Offset");
                
                EditorGUILayout.Space();
                
                // Scan Line
                MaterialProperty enableScanLine = FindProperty("_EnableScanLine", properties);
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(enableScanLine, "Enable Scan Line");
                if (EditorGUI.EndChangeCheck())
                {
                    SetKeyword(mat, "_SCANLINE_ON", enableScanLine.floatValue > 0.5f);
                }
                
                if (enableScanLine.floatValue > 0.5f)
                {
                    EditorGUI.indentLevel++;
                    materialEditor.ColorProperty(FindProperty("_ScanLineColor", properties), "Scan Line Color");
                    materialEditor.RangeProperty(FindProperty("_ScanLineWidth", properties), "Scan Line Width");
                    materialEditor.RangeProperty(FindProperty("_ScanLineSpeed", properties), "Scan Line Speed");
                    materialEditor.RangeProperty(FindProperty("_ScanLineAmount", properties), "Scan Line Amount");
                    materialEditor.RangeProperty(FindProperty("_ScanLineShiftSpeed", properties), "Scan Line Shift Speed");
                    materialEditor.RangeProperty(FindProperty("_ScanLineDeform", properties), "Scan Line Deformation");
                    EditorGUI.indentLevel--;
                }
                
                GUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawEffectsSettings(MaterialEditor materialEditor, MaterialProperty[] properties, Material mat)
        {
            showEffectsSettings = EditorGUILayout.Foldout(showEffectsSettings, "Visual Effects", true, EditorStyles.foldoutHeader);
            
            if (showEffectsSettings)
            {
                EditorGUI.indentLevel++;
                
                GUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Rim Effect
                MaterialProperty enableRim = FindProperty("_EnableRim", properties);
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(enableRim, "Enable Rim Effect");
                if (EditorGUI.EndChangeCheck())
                {
                    SetKeyword(mat, "_RIM_ON", enableRim.floatValue > 0.5f);
                }
                
                if (enableRim.floatValue > 0.5f)
                {
                    EditorGUI.indentLevel++;
                    materialEditor.ColorProperty(FindProperty("_RimColor", properties), "Rim Color");
                    materialEditor.RangeProperty(FindProperty("_RimPower", properties), "Rim Power");
                    materialEditor.RangeProperty(FindProperty("_RimIntensity", properties), "Rim Intensity");
                    materialEditor.RangeProperty(FindProperty("_RimFlutter", properties), "Rim Flutter");
                    materialEditor.RangeProperty(FindProperty("_RimFlutterSpeed", properties), "Rim Flutter Speed");
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space();
                
                // Glitch Effect
                MaterialProperty enableGlitch = FindProperty("_EnableGlitch", properties);
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(enableGlitch, "Enable Glitch");
                if (EditorGUI.EndChangeCheck())
                {
                    SetKeyword(mat, "_GLITCH_ON", enableGlitch.floatValue > 0.5f);
                }
                
                if (enableGlitch.floatValue > 0.5f)
                {
                    EditorGUI.indentLevel++;
                    materialEditor.RangeProperty(FindProperty("_GlitchIntensity", properties), "Glitch Intensity");
                    materialEditor.RangeProperty(FindProperty("_GlitchSpeed", properties), "Glitch Speed");
                    materialEditor.RangeProperty(FindProperty("_GlitchColorIntensity", properties), "Glitch Color Intensity");
                    materialEditor.RangeProperty(FindProperty("_GlitchFrequency", properties), "Glitch Frequency");
                    materialEditor.RangeProperty(FindProperty("_GlitchJump", properties), "Glitch Jump");
                    materialEditor.RangeProperty(FindProperty("_GlitchDistortion", properties), "Glitch Distortion");
                    materialEditor.RangeProperty(FindProperty("_GlitchHorizontalIntensity", properties), "Horizontal Intensity");
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space();
                
                // Fresnel
                MaterialProperty enableFresnel = FindProperty("_EnableFresnel", properties);
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(enableFresnel, "Enable Fresnel");
                if (EditorGUI.EndChangeCheck())
                {
                    SetKeyword(mat, "_FRESNEL_ON", enableFresnel.floatValue > 0.5f);
                }
                
                if (enableFresnel.floatValue > 0.5f)
                {
                    EditorGUI.indentLevel++;
                    materialEditor.ColorProperty(FindProperty("_FresnelColor", properties), "Fresnel Color");
                    materialEditor.RangeProperty(FindProperty("_FresnelPower", properties), "Fresnel Power");
                    materialEditor.RangeProperty(FindProperty("_FresnelIntensity", properties), "Fresnel Intensity");
                    materialEditor.RangeProperty(FindProperty("_FresnelExponent", properties), "Fresnel Exponent");
                    materialEditor.RangeProperty(FindProperty("_FresnelSharpness", properties), "Fresnel Sharpness");
                    materialEditor.RangeProperty(FindProperty("_FresnelColorVariation", properties), "Color Variation");
                    EditorGUI.indentLevel--;
                }
                
                GUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawAdvancedSettings(MaterialEditor materialEditor, MaterialProperty[] properties, Material mat)
        {
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Effects", true, EditorStyles.foldoutHeader);
            
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                
                GUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Only show relevant advanced effects based on shader type
                int shaderType = (int)mat.GetFloat("_ShaderType");
                
                if (shaderType == 1 || shaderType == 2) // Data Stream or Interface
                {
                    MaterialProperty enableDataStream = FindProperty("_EnableDataStream", properties);
                    EditorGUI.BeginChangeCheck();
                    materialEditor.ShaderProperty(enableDataStream, "Enable Data Stream");
                    if (EditorGUI.EndChangeCheck())
                    {
                        SetKeyword(mat, "_DATASTREAM_ON", enableDataStream.floatValue > 0.5f);
                    }
                    
                    if (enableDataStream.floatValue > 0.5f)
                    {
                        EditorGUI.indentLevel++;
                        MaterialProperty dataStreamTex = FindProperty("_DataStreamTex", properties);
                        materialEditor.TexturePropertySingleLine(new GUIContent("Data Stream Texture"), dataStreamTex);
                        materialEditor.RangeProperty(FindProperty("_DataStreamSpeed", properties), "Data Stream Speed");
                        materialEditor.RangeProperty(FindProperty("_DataStreamIntensity", properties), "Data Stream Intensity");
                        materialEditor.RangeProperty(FindProperty("_DataStreamTiling", properties), "Data Stream Tiling");
                        materialEditor.ColorProperty(FindProperty("_DataStreamColor", properties), "Data Stream Color");
                        materialEditor.RangeProperty(FindProperty("_DataStreamGlow", properties), "Data Stream Glow");
                        materialEditor.VectorProperty(FindProperty("_DataStreamScrollDir", properties), "Scroll Direction");
                        materialEditor.RangeProperty(FindProperty("_DataStreamDensity", properties), "Stream Density");
                        EditorGUI.indentLevel--;
                    }
                }
                
                if (shaderType == 3) // 3D Projection
                {
                    MaterialProperty enableProjection = FindProperty("_EnableProjection", properties);
                    EditorGUI.BeginChangeCheck();
                    materialEditor.ShaderProperty(enableProjection, "Enable 3D Projection");
                    if (EditorGUI.EndChangeCheck())
                    {
                        SetKeyword(mat, "_PROJECTION_ON", enableProjection.floatValue > 0.5f);
                    }
                    
                    if (enableProjection.floatValue > 0.5f)
                    {
                        EditorGUI.indentLevel++;
                        materialEditor.RangeProperty(FindProperty("_ProjectionHeight", properties), "Projection Height");
                        materialEditor.RangeProperty(FindProperty("_ProjectionFadeDistance", properties), "Fade Distance");
                        materialEditor.ColorProperty(FindProperty("_ProjectionColor", properties), "Projection Color");
                        materialEditor.RangeProperty(FindProperty("_ProjectionIntensity", properties), "Projection Intensity");
                        materialEditor.RangeProperty(FindProperty("_ProjectionFlicker", properties), "Projection Flicker");
                        materialEditor.RangeProperty(FindProperty("_ProjectionSpread", properties), "Projection Spread");
                        materialEditor.RangeProperty(FindProperty("_ProjectionAngleMultiplier", properties), "Angle Multiplier");
                        materialEditor.RangeProperty(FindProperty("_ProjectionDistortion", properties), "Distortion");
                        EditorGUI.indentLevel--;
                    }
                }
                
                GUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawUtilityButtons(MaterialEditor materialEditor, Material mat)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Utility Actions", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Reset All", GUILayout.Height(30)))
            {
                ResetToDefault(mat);
                UpdateKeywords(mat);
                EditorUtility.SetDirty(mat);
            }
            
            if (GUILayout.Button("Randomize", GUILayout.Height(30)))
            {
                RandomizeSettings(mat);
                UpdateKeywords(mat);
                EditorUtility.SetDirty(mat);
            }
            
            if (GUILayout.Button("Copy Settings", GUILayout.Height(30)))
            {
                CopySettings(mat);
            }
            
            if (GUILayout.Button("Paste Settings", GUILayout.Height(30)))
            {
                PasteSettings(mat);
                UpdateKeywords(mat);
                EditorUtility.SetDirty(mat);
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
            if (GUILayout.Button("📖 Documentation", GUILayout.Height(25)))
            {
                Application.OpenURL("https://docs.scifihologram.com");
            }
            
            GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
            if (GUILayout.Button("🎥 Video Tutorials", GUILayout.Height(25)))
            {
                Application.OpenURL("https://youtube.com/scifihologram");
            }
            
            GUI.backgroundColor = new Color(0.3f, 0.5f, 0.8f);
            if (GUILayout.Button("💬 Discord Support", GUILayout.Height(25)))
            {
                Application.OpenURL("https://discord.gg/scifihologram");
            }
            
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
        }
        
        // Helper Methods
        
        private void SetupBlendMode(Material material, int mode)
        {
            switch (mode)
            {
                case 0: // Opaque
                    material.SetOverrideTag("RenderType", "Opaque");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    material.SetInt("_ZWrite", 1);
                    material.DisableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    material.SetFloat("_Cull", 2); // Back
                    break;
                    
                case 1: // Transparent
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    material.SetFloat("_Cull", 2); // Back
                    break;
            }
        }
        
        private void UpdateKeywords(Material material)
        {
            // Update blend mode first
            int blendMode = (int)material.GetFloat("_BlendMode");
            SetupBlendMode(material, blendMode);
            
            // Update all shader keywords based on properties
            SetKeyword(material, "_SCANLINE_ON", material.GetFloat("_EnableScanLine") > 0.5f);
            SetKeyword(material, "_RIM_ON", material.GetFloat("_EnableRim") > 0.5f);
            SetKeyword(material, "_GLITCH_ON", material.GetFloat("_EnableGlitch") > 0.5f);
            SetKeyword(material, "_EMISSION_ON", material.GetFloat("_EnableEmission") > 0.5f);
            SetKeyword(material, "_FRESNEL_ON", material.GetFloat("_EnableFresnel") > 0.5f);
            SetKeyword(material, "_DISTORT_ON", material.GetFloat("_EnableDistortion") > 0.5f);
            SetKeyword(material, "_LINES_ON", material.GetFloat("_EnableLines") > 0.5f);
            SetKeyword(material, "_NOISE_ON", material.GetFloat("_EnableNoise") > 0.5f);
            SetKeyword(material, "_DATASTREAM_ON", material.GetFloat("_EnableDataStream") > 0.5f);
            SetKeyword(material, "_PROJECTION_ON", material.GetFloat("_EnableProjection") > 0.5f);
            SetKeyword(material, "_INTERFACE_ON", material.GetFloat("_EnableInterface") > 0.5f);
            SetKeyword(material, "_EDGES_ON", material.GetFloat("_EnableEdges") > 0.5f);
            SetKeyword(material, "_HEXGRID_ON", material.GetFloat("_EnableHexGrid") > 0.5f);
            SetKeyword(material, "_SQUAREGRID_ON", material.GetFloat("_EnableSquareGrid") > 0.5f);
            SetKeyword(material, "_CIRCUIT_ON", material.GetFloat("_EnableCircuit") > 0.5f);
            SetKeyword(material, "_WIREFRAME_ON", material.GetFloat("_EnableWireframe") > 0.5f);
            SetKeyword(material, "_PULSE_ON", material.GetFloat("_EnablePulse") > 0.5f);
            SetKeyword(material, "_SCANNING_ON", material.GetFloat("_EnableScanning") > 0.5f);
            SetKeyword(material, "_BEAM_ON", material.GetFloat("_EnableBeam") > 0.5f);
            SetKeyword(material, "_COLORSHIFT_ON", material.GetFloat("_EnableColorShift") > 0.5f);
            SetKeyword(material, "_COLORBANDING_ON", material.GetFloat("_EnableColorBanding") > 0.5f);
            SetKeyword(material, "_CHROMATIC_ON", material.GetFloat("_EnableChromatic") > 0.5f);
            SetKeyword(material, "_VIGNETTE_ON", material.GetFloat("_EnableVignette") > 0.5f);
            SetKeyword(material, "_VOLUMETRIC_ON", material.GetFloat("_EnableVolumetric") > 0.5f);
            SetKeyword(material, "_DEPTH_ON", material.GetFloat("_EnableDepth") > 0.5f);
        }
        
        private void SetKeyword(Material material, string keyword, bool state)
        {
            if (material.shader != null)
            {
                UnityEngine.Rendering.LocalKeyword localKeyword = new UnityEngine.Rendering.LocalKeyword(material.shader, keyword);
                material.SetKeyword(localKeyword, state);
            }
        }
        
        private MaterialProperty FindProperty(string name, MaterialProperty[] properties)
        {
            foreach (MaterialProperty prop in properties)
            {
                if (prop.name == name)
                    return prop;
            }
            return null;
        }
        
        private void CopySettings(Material mat)
        {
            copiedFloatProperties.Clear();
            copiedVectorProperties.Clear();
            copiedColorProperties.Clear();
            copiedTextureProperties.Clear();
            
            // Get all properties
            Shader shader = mat.shader;
            int propertyCount = ShaderUtil.GetPropertyCount(shader);
            
            for (int i = 0; i < propertyCount; i++)
            {
                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                ShaderUtil.ShaderPropertyType propertyType = ShaderUtil.GetPropertyType(shader, i);
                
                switch (propertyType)
                {
                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        copiedFloatProperties[propertyName] = mat.GetFloat(propertyName);
                        break;
                    case ShaderUtil.ShaderPropertyType.Color:
                        copiedColorProperties[propertyName] = mat.GetColor(propertyName);
                        break;
                    case ShaderUtil.ShaderPropertyType.Vector:
                        copiedVectorProperties[propertyName] = mat.GetVector(propertyName);
                        break;
                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        copiedTextureProperties[propertyName] = mat.GetTexture(propertyName);
                        break;
                }
            }
            
            EditorUtility.DisplayDialog("Copy Settings", "Material settings copied successfully!", "OK");
        }
        
        private void PasteSettings(Material mat)
        {
            if (copiedFloatProperties.Count == 0 && copiedVectorProperties.Count == 0 && 
                copiedColorProperties.Count == 0 && copiedTextureProperties.Count == 0)
            {
                EditorUtility.DisplayDialog("Paste Settings", "No settings to paste!", "OK");
                return;
            }
            
            Undo.RecordObject(mat, "Paste Material Settings");
            
            foreach (var kvp in copiedFloatProperties)
            {
                if (mat.HasProperty(kvp.Key))
                    mat.SetFloat(kvp.Key, kvp.Value);
            }
            
            foreach (var kvp in copiedVectorProperties)
            {
                if (mat.HasProperty(kvp.Key))
                    mat.SetVector(kvp.Key, kvp.Value);
            }
            
            foreach (var kvp in copiedColorProperties)
            {
                if (mat.HasProperty(kvp.Key))
                    mat.SetColor(kvp.Key, kvp.Value);
            }
            
            foreach (var kvp in copiedTextureProperties)
            {
                if (mat.HasProperty(kvp.Key))
                    mat.SetTexture(kvp.Key, kvp.Value);
            }
            
            EditorUtility.DisplayDialog("Paste Settings", "Material settings pasted successfully!", "OK");
        }
        
        private void ResetToDefault(Material mat)
        {
            // Reset all properties to default values
            mat.SetFloat("_ShaderType", 0);
            mat.SetFloat("_BlendMode", 1); // Default to Transparent for hologram
            mat.SetColor("_Color", new Color(0, 0.5f, 1f, 0.7f));
            mat.SetFloat("_HologramIntensity", 1);
            mat.SetFloat("_HologramOpacity", 0.7f);
            mat.SetFloat("_HologramFlickerSpeed", 1);
            mat.SetFloat("_HologramFlickerIntensity", 0.1f);
            mat.SetFloat("_FlickerPattern", 0);
            mat.SetFloat("_FlickerOffset", 0);
            
            // Disable all effects
            mat.SetFloat("_EnableScanLine", 0);
            mat.SetFloat("_EnableRim", 0);
            mat.SetFloat("_EnableGlitch", 0);
            mat.SetFloat("_EnableEmission", 0);
            mat.SetFloat("_EnableFresnel", 0);
            mat.SetFloat("_EnableDistortion", 0);
            mat.SetFloat("_EnableLines", 0);
            mat.SetFloat("_EnableNoise", 0);
            mat.SetFloat("_EnableDataStream", 0);
            mat.SetFloat("_EnableProjection", 0);
            mat.SetFloat("_EnableInterface", 0);
            mat.SetFloat("_EnableEdges", 0);
            mat.SetFloat("_EnableHexGrid", 0);
            mat.SetFloat("_EnableSquareGrid", 0);
            mat.SetFloat("_EnableCircuit", 0);
            mat.SetFloat("_EnableWireframe", 0);
            mat.SetFloat("_EnablePulse", 0);
            mat.SetFloat("_EnableScanning", 0);
            mat.SetFloat("_EnableBeam", 0);
            mat.SetFloat("_EnableColorShift", 0);
            mat.SetFloat("_EnableColorBanding", 0);
            mat.SetFloat("_EnableChromatic", 0);
            mat.SetFloat("_EnableVignette", 0);
            mat.SetFloat("_EnableVolumetric", 0);
            mat.SetFloat("_EnableDepth", 0);
            
            SetupBlendMode(mat, 1); // Set to Transparent by default
        }
        
        private void RandomizeSettings(Material mat)
        {
            ResetToDefault(mat);
            
            // Random shader type
            mat.SetFloat("_ShaderType", UnityEngine.Random.Range(0, 4));
            
            // Random base color
            mat.SetColor("_Color", UnityEngine.Random.ColorHSV(0f, 1f, 0.3f, 1f, 0.5f, 1f, 0.6f, 0.9f));
            
            // Random hologram settings
            mat.SetFloat("_HologramIntensity", UnityEngine.Random.Range(0.8f, 1.5f));
            mat.SetFloat("_HologramOpacity", UnityEngine.Random.Range(0.5f, 0.9f));
            mat.SetFloat("_HologramFlickerSpeed", UnityEngine.Random.Range(0.5f, 3f));
            mat.SetFloat("_HologramFlickerIntensity", UnityEngine.Random.Range(0.05f, 0.2f));
            mat.SetFloat("_FlickerPattern", UnityEngine.Random.Range(0f, 3f));
            
            // Randomly enable 2-4 effects
            int effectCount = UnityEngine.Random.Range(2, 5);
            List<string> effects = new List<string> {
                "_EnableScanLine", "_EnableRim", "_EnableGlitch", "_EnableFresnel", 
                "_EnableLines", "_EnableHexGrid", "_EnablePulse"
            };
            
            for (int i = 0; i < effectCount && i < effects.Count; i++)
            {
                int index = UnityEngine.Random.Range(i, effects.Count);
                string temp = effects[i];
                effects[i] = effects[index];
                effects[index] = temp;
                
                mat.SetFloat(effects[i], 1);
                
                // Set random values for enabled effects
                switch (effects[i])
                {
                    case "_EnableScanLine":
                        mat.SetColor("_ScanLineColor", UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.8f, 1f));
                        mat.SetFloat("_ScanLineSpeed", UnityEngine.Random.Range(0.5f, 2f));
                        mat.SetFloat("_ScanLineAmount", UnityEngine.Random.Range(10f, 50f));
                        break;
                    case "_EnableRim":
                        mat.SetColor("_RimColor", UnityEngine.Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.8f, 1f));
                        mat.SetFloat("_RimPower", UnityEngine.Random.Range(2f, 5f));
                        mat.SetFloat("_RimIntensity", UnityEngine.Random.Range(0.5f, 1.5f));
                        break;
                    case "_EnableGlitch":
                        mat.SetFloat("_GlitchIntensity", UnityEngine.Random.Range(0.1f, 0.3f));
                        mat.SetFloat("_GlitchSpeed", UnityEngine.Random.Range(5f, 15f));
                        break;
                }
            }
            
            // Maybe set to transparent
            if (UnityEngine.Random.Range(0f, 1f) > 0.5f)
            {
                mat.SetFloat("_BlendMode", 1);
                SetupBlendMode(mat, 1);
            }
        }
        
        // ===== PRESET IMPLEMENTATIONS =====
        
        // STANDARD HOLOGRAM PRESETS
        
        private void ApplyClassicHologram(Material mat)
        {
            ResetToDefault(mat);
            mat.SetFloat("_BlendMode", 1); // Transparent
            mat.SetColor("_Color", new Color(0, 0.6f, 1f, 0.7f));
            mat.SetFloat("_HologramIntensity", 1.2f);
            mat.SetFloat("_HologramOpacity", 0.7f);
            mat.SetFloat("_HologramFlickerSpeed", 2.0f);
            mat.SetFloat("_HologramFlickerIntensity", 0.12f);
            mat.SetFloat("_FlickerPattern", 0f);
            mat.SetFloat("_EnableScanLine", 1f);
            mat.SetColor("_ScanLineColor", new Color(0.1f, 0.8f, 1f, 1f));
            mat.SetFloat("_ScanLineWidth", 0.05f);
            mat.SetFloat("_ScanLineSpeed", 0.8f);
            mat.SetFloat("_ScanLineAmount", 25f);
            mat.SetFloat("_ScanLineDeform", 0.05f);
            mat.SetFloat("_EnableLines", 1f);
            mat.SetFloat("_LineSpacing", 35f);
            mat.SetFloat("_LineSpeed", 0.5f);
            mat.SetFloat("_LineIntensity", 0.15f);
            mat.SetColor("_LineColor", new Color(0.2f, 0.85f, 1f, 1f));
            mat.SetFloat("_LineWidth", 0.6f);
            mat.SetFloat("_EnableRim", 1f);
            mat.SetColor("_RimColor", new Color(0f, 0.7f, 1f, 1f));
            mat.SetFloat("_RimPower", 2.8f);
            mat.SetFloat("_RimIntensity", 1.0f);
            mat.SetFloat("_RimFlutter", 0.05f);
            mat.SetFloat("_EnableFresnel", 1f);
            mat.SetColor("_FresnelColor", new Color(0f, 0.5f, 1f, 1f));
            mat.SetFloat("_FresnelPower", 1.5f);
            mat.SetFloat("_FresnelIntensity", 0.8f);
        }
        
        private void ApplyDamagedHologram(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.1f, 0.8f, 0.7f, 0.7f));
            mat.SetFloat("_HologramIntensity", 1.3f);
            mat.SetFloat("_HologramOpacity", 0.7f);
            mat.SetFloat("_HologramFlickerSpeed", 4.0f);
            mat.SetFloat("_HologramFlickerIntensity", 0.25f);
            mat.SetFloat("_FlickerPattern", 1f);
            mat.SetFloat("_EnableGlitch", 1f);
            mat.SetFloat("_GlitchIntensity", 0.2f);
            mat.SetFloat("_GlitchSpeed", 8.0f);
            mat.SetFloat("_GlitchColorIntensity", 0.3f);
            mat.SetFloat("_GlitchFrequency", 5.0f);
            mat.SetFloat("_GlitchJump", 0.15f);
            mat.SetFloat("_GlitchDistortion", 0.15f);
            mat.SetFloat("_EnableDistortion", 1f);
            mat.SetFloat("_DistortionSpeed", 1.5f);
            mat.SetFloat("_DistortionIntensity", 0.08f);
            mat.SetFloat("_DistortionTiling", 2.0f);
            mat.SetFloat("_DistortionAnimation", 2f);
            mat.SetFloat("_EnableNoise", 1f);
            mat.SetFloat("_NoiseIntensity", 0.1f);
            mat.SetFloat("_NoiseSpeed", 2.0f);
            mat.SetFloat("_NoiseTiling", 3.0f);
            mat.SetFloat("_NoiseContrast", 1.2f);
            mat.SetFloat("_EnableScanLine", 1f);
            mat.SetColor("_ScanLineColor", new Color(0.2f, 0.9f, 0.8f, 1f));
            mat.SetFloat("_ScanLineWidth", 0.1f);
            mat.SetFloat("_ScanLineSpeed", 2.0f);
            mat.SetFloat("_ScanLineAmount", 20f);
            mat.SetFloat("_ScanLineDeform", 0.2f);
        }
        
        private void ApplyAlienTechnology(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.3f, 0.9f, 0.5f, 0.6f));
            mat.SetFloat("_HologramIntensity", 1.4f);
            mat.SetFloat("_HologramOpacity", 0.6f);
            mat.SetFloat("_HologramFlickerSpeed", 1.5f);
            mat.SetFloat("_HologramFlickerIntensity", 0.1f);
            mat.SetFloat("_FlickerPattern", 2f);
            mat.SetFloat("_EnableHexGrid", 1f);
            mat.SetFloat("_HexSize", 20f);
            mat.SetFloat("_HexIntensity", 0.3f);
            mat.SetColor("_HexColor", new Color(0.2f, 0.8f, 0.4f, 1f));
            mat.SetFloat("_HexEmission", 1.5f);
            mat.SetFloat("_EnableEmission", 1f);
            mat.SetColor("_EmissionColor", new Color(0.1f, 0.7f, 0.3f, 1f));
            mat.SetFloat("_EmissionIntensity", 1.2f);
            mat.SetFloat("_EmissionPulse", 0.3f);
            mat.SetFloat("_EnableScanLine", 1f);
            mat.SetColor("_ScanLineColor", new Color(0.3f, 0.9f, 0.5f, 1f));
            mat.SetFloat("_ScanLineWidth", 0.08f);
            mat.SetFloat("_ScanLineSpeed", 1.2f);
            mat.SetFloat("_ScanLineAmount", 30f);
        }
        
        private void ApplyEnergyField(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.8f, 0.2f, 0.9f, 0.8f));
            mat.SetFloat("_HologramIntensity", 1.5f);
            mat.SetFloat("_HologramOpacity", 0.8f);
            mat.SetFloat("_HologramFlickerSpeed", 3.0f);
            mat.SetFloat("_HologramFlickerIntensity", 0.15f);
            mat.SetFloat("_FlickerPattern", 0f);
            mat.SetFloat("_EnablePulse", 1f);
            mat.SetFloat("_PulseSpeed", 1.5f);
            mat.SetFloat("_PulseAmplitude", 0.3f);
            mat.SetColor("_PulseColor", new Color(0.7f, 0.1f, 0.8f, 1f));
            mat.SetFloat("_EnableRim", 1f);
            mat.SetColor("_RimColor", new Color(0.9f, 0.3f, 1f, 1f));
            mat.SetFloat("_RimPower", 2.5f);
            mat.SetFloat("_RimIntensity", 1.2f);
            mat.SetFloat("_EnableFresnel", 1f);
            mat.SetColor("_FresnelColor", new Color(0.8f, 0.2f, 0.9f, 1f));
            mat.SetFloat("_FresnelPower", 1.8f);
            mat.SetFloat("_FresnelIntensity", 0.9f);
        }
        
        private void ApplyCyberIdentity(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.1f, 0.5f, 0.9f, 0.7f));
            mat.SetFloat("_HologramIntensity", 1.3f);
            mat.SetFloat("_HologramOpacity", 0.7f);
            mat.SetFloat("_HologramFlickerSpeed", 2.5f);
            mat.SetFloat("_HologramFlickerIntensity", 0.2f);
            mat.SetFloat("_FlickerPattern", 1f);
            mat.SetFloat("_EnableCircuit", 1f);
            mat.SetFloat("_CircuitIntensity", 0.5f);
            mat.SetColor("_CircuitColor", new Color(0.2f, 0.6f, 1f, 1f));
            mat.SetFloat("_CircuitSpeed", 0.8f);
            mat.SetFloat("_EnableScanLine", 1f);
            mat.SetColor("_ScanLineColor", new Color(0.1f, 0.5f, 0.9f, 1f));
            mat.SetFloat("_ScanLineWidth", 0.06f);
            mat.SetFloat("_ScanLineSpeed", 1.0f);
            mat.SetFloat("_ScanLineAmount", 20f);
        }
        
        private void ApplyGhostlyApparition(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.7f, 0.7f, 0.7f, 0.6f));
            mat.SetFloat("_HologramIntensity", 1.2f);
            mat.SetFloat("_HologramOpacity", 0.6f);
            mat.SetFloat("_HologramFlickerSpeed", 1.8f);
            mat.SetFloat("_HologramFlickerIntensity", 0.1f);
            mat.SetFloat("_FlickerPattern", 2f);
            mat.SetFloat("_EnableVignette", 1f);
            mat.SetColor("_VignetteColor", new Color(0.5f, 0.5f, 0.5f, 1f));
            mat.SetFloat("_VignettePower", 2.5f);
            mat.SetFloat("_VignetteIntensity", 0.6f);
            mat.SetFloat("_EnableRim", 1f);
            mat.SetColor("_RimColor", new Color(0.8f, 0.8f, 0.8f, 1f));
            mat.SetFloat("_RimPower", 3.0f);
            mat.SetFloat("_RimIntensity", 1.0f);
        }
        
        private void ApplyNeonPulse(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(1f, 0.2f, 0.2f, 0.8f));
            mat.SetFloat("_HologramIntensity", 1.6f);
            mat.SetFloat("_HologramOpacity", 0.8f);
            mat.SetFloat("_HologramFlickerSpeed", 3.5f);
            mat.SetFloat("_HologramFlickerIntensity", 0.15f);
            mat.SetFloat("_FlickerPattern", 0f);
            mat.SetFloat("_EnableBeam", 1f);
            mat.SetFloat("_BeamSpeed", 2.5f);
            mat.SetFloat("_BeamWidth", 0.08f);
            mat.SetColor("_BeamColor", new Color(1f, 0.3f, 0.3f, 1f));
            mat.SetFloat("_BeamCount", 4f);
            mat.SetFloat("_EnableEmission", 1f);
            mat.SetColor("_EmissionColor", new Color(1f, 0.2f, 0.2f, 1f));
            mat.SetFloat("_EmissionIntensity", 1.5f);
        }
        
        private void ApplyQuantumMatrix(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.2f, 0.8f, 0.2f, 0.7f));
            mat.SetFloat("_HologramIntensity", 1.4f);
            mat.SetFloat("_HologramOpacity", 0.7f);
            mat.SetFloat("_HologramFlickerSpeed", 2.0f);
            mat.SetFloat("_HologramFlickerIntensity", 0.1f);
            mat.SetFloat("_FlickerPattern", 1f);
            mat.SetFloat("_EnableSquareGrid", 1f);
            mat.SetFloat("_SquareSize", 25f);
            mat.SetFloat("_SquareIntensity", 0.4f);
            mat.SetColor("_SquareColor", new Color(0.3f, 0.9f, 0.3f, 1f));
            mat.SetFloat("_SquareEdgeWidth", 0.1f);
            mat.SetFloat("_EnableFresnel", 1f);
            mat.SetColor("_FresnelColor", new Color(0.2f, 0.8f, 0.2f, 1f));
            mat.SetFloat("_FresnelPower", 2.0f);
            mat.SetFloat("_FresnelIntensity", 1.0f);
        }
        
        private void ApplyHoloBlueprint(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.5f, 0.5f, 1f, 0.7f));
            mat.SetFloat("_HologramIntensity", 1.3f);
            mat.SetFloat("_HologramOpacity", 0.7f);
            mat.SetFloat("_HologramFlickerSpeed", 2.2f);
            mat.SetFloat("_HologramFlickerIntensity", 0.12f);
            mat.SetFloat("_FlickerPattern", 0f);
            mat.SetFloat("_EnableWireframe", 1f);
            mat.SetColor("_WireframeColor", new Color(0.4f, 0.4f, 0.9f, 1f));
            mat.SetFloat("_WireframeThickness", 0.03f);
            mat.SetFloat("_WireframeSmoothing", 0.02f);
            mat.SetFloat("_WireframeDensity", 2f);
            mat.SetFloat("_EnableScanLine", 1f);
            mat.SetColor("_ScanLineColor", new Color(0.5f, 0.5f, 1f, 1f));
            mat.SetFloat("_ScanLineWidth", 0.07f);
            mat.SetFloat("_ScanLineSpeed", 1.5f);
            mat.SetFloat("_ScanLineAmount", 25f);
        }
        
        private void ApplyDigitalMirage(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.9f, 0.7f, 0.2f, 0.6f));
            mat.SetFloat("_HologramIntensity", 1.5f);
            mat.SetFloat("_HologramOpacity", 0.6f);
            mat.SetFloat("_HologramFlickerSpeed", 1.8f);
            mat.SetFloat("_HologramFlickerIntensity", 0.1f);
            mat.SetFloat("_FlickerPattern", 2f);
            mat.SetFloat("_EnableVolumetric", 1f);
            mat.SetColor("_VolumetricColor", new Color(0.8f, 0.6f, 0.1f, 0.3f));
            mat.SetFloat("_VolumetricIntensity", 1.2f);
            mat.SetFloat("_VolumetricNoise", 0.5f);
            mat.SetFloat("_EnableRim", 1f);
            mat.SetColor("_RimColor", new Color(0.9f, 0.7f, 0.2f, 1f));
            mat.SetFloat("_RimPower", 2.8f);
            mat.SetFloat("_RimIntensity", 1.0f);
        }
        
        private void ApplyStarfieldHologram(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.3f, 0.7f, 0.9f, 0.7f));
            mat.SetFloat("_HologramIntensity", 1.4f);
            mat.SetFloat("_HologramOpacity", 0.7f);
            mat.SetFloat("_HologramFlickerSpeed", 2.0f);
            mat.SetFloat("_HologramFlickerIntensity", 0.12f);
            mat.SetFloat("_FlickerPattern", 0f);
            mat.SetFloat("_EnableColorShift", 1f);
            mat.SetFloat("_ColorShiftSpeed", 1.5f);
            mat.SetFloat("_ColorShiftIntensity", 0.6f);
            mat.SetFloat("_ColorShiftHue", 0.8f);
            mat.SetFloat("_EnableScanLine", 1f);
            mat.SetColor("_ScanLineColor", new Color(0.3f, 0.7f, 0.9f, 1f));
            mat.SetFloat("_ScanLineWidth", 0.05f);
            mat.SetFloat("_ScanLineSpeed", 1.0f);
            mat.SetFloat("_ScanLineAmount", 20f);
            mat.SetFloat("_EnableNoise", 1f);
            mat.SetFloat("_NoiseIntensity", 0.08f);
            mat.SetFloat("_NoiseSpeed", 1.5f);
            mat.SetFloat("_NoiseTiling", 4.0f);
        }
        
        private void ApplyGlitchCore(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.8f, 0.2f, 0.8f, 0.7f));
            mat.SetFloat("_HologramIntensity", 1.3f);
            mat.SetFloat("_HologramOpacity", 0.7f);
            mat.SetFloat("_HologramFlickerSpeed", 2.5f);
            mat.SetFloat("_HologramFlickerIntensity", 0.15f);
            mat.SetFloat("_FlickerPattern", 1f);
            mat.SetFloat("_EnableGlitch", 1f);
            mat.SetFloat("_GlitchIntensity", 0.3f);
            mat.SetFloat("_GlitchSpeed", 10.0f);
            mat.SetFloat("_GlitchColorIntensity", 0.4f);
            mat.SetFloat("_EnableDistortion", 1f);
            mat.SetFloat("_DistortionSpeed", 2.0f);
            mat.SetFloat("_DistortionIntensity", 0.1f);
            mat.SetFloat("_DistortionTiling", 3.0f);
            mat.SetFloat("_EnableColorBanding", 1f);
            mat.SetFloat("_ColorBands", 5f);
            mat.SetFloat("_BandingContrast", 1.4f);
            mat.SetFloat("_BandingSaturation", 1.2f);
        }
        
        private void ApplyVaporwaveHologram(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.9f, 0.3f, 0.5f, 0.7f));
            mat.SetFloat("_HologramIntensity", 1.4f);
            mat.SetFloat("_HologramOpacity", 0.7f);
            mat.SetFloat("_HologramFlickerSpeed", 2.0f);
            mat.SetFloat("_HologramFlickerIntensity", 0.1f);
            mat.SetFloat("_FlickerPattern", 0f);
            mat.SetFloat("_EnableChromatic", 1f);
            mat.SetFloat("_ChromaticIntensity", 0.05f);
            mat.SetFloat("_ChromaticOffset", 0.5f);
            mat.SetFloat("_EnableRim", 1f);
            mat.SetColor("_RimColor", new Color(0.9f, 0.3f, 0.5f, 1f));
            mat.SetFloat("_RimPower", 2.5f);
            mat.SetFloat("_RimIntensity", 1.0f);
            mat.SetFloat("_EnableSquareGrid", 1f);
            mat.SetFloat("_SquareSize", 30f);
            mat.SetFloat("_SquareIntensity", 0.3f);
            mat.SetColor("_SquareColor", new Color(0.1f, 0.8f, 0.8f, 1f));
            mat.SetFloat("_SquareEdgeWidth", 0.05f);
        }
        
        private void ApplyTechEssence(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.0f, 0.8f, 0.6f, 0.7f));
            mat.SetFloat("_HologramIntensity", 1.3f);
            mat.SetFloat("_HologramOpacity", 0.7f);
            mat.SetFloat("_HologramFlickerSpeed", 1.7f);
            mat.SetFloat("_HologramFlickerIntensity", 0.08f);
            mat.SetFloat("_FlickerPattern", 1f);
            mat.SetFloat("_EnableFresnel", 1f);
            mat.SetColor("_FresnelColor", new Color(0.0f, 0.9f, 0.7f, 1f));
            mat.SetFloat("_FresnelPower", 1.8f);
            mat.SetFloat("_FresnelIntensity", 0.9f);
            mat.SetFloat("_EnableEdges", 1f);
            mat.SetColor("_EdgeColor", new Color(0.0f, 0.9f, 0.7f, 1f));
            mat.SetFloat("_EdgeThickness", 0.02f);
            mat.SetFloat("_EdgeSharpness", 6f);
            mat.SetFloat("_EdgePower", 1.5f);
            mat.SetFloat("_EdgeEmission", 1.8f);
            mat.SetFloat("_EnableLines", 1f);
            mat.SetFloat("_LineSpacing", 40f);
            mat.SetFloat("_LineSpeed", 0.4f);
            mat.SetFloat("_LineIntensity", 0.2f);
            mat.SetColor("_LineColor", new Color(0.0f, 0.8f, 0.6f, 1f));
            mat.SetFloat("_LineWidth", 0.7f);
        }
        
        // DATA STREAM PRESETS
        
        private void ApplyDataStream(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.1f, 0.7f, 0.6f, 0.7f));
            mat.SetFloat("_HologramIntensity", 1.1f);
            mat.SetFloat("_HologramOpacity", 0.7f);
            mat.SetFloat("_HologramFlickerSpeed", 0.6f);
            mat.SetFloat("_HologramFlickerIntensity", 0.06f);
            mat.SetFloat("_EnableDataStream", 1f);
            mat.SetFloat("_DataStreamSpeed", 1.2f);
            mat.SetFloat("_DataStreamIntensity", 0.8f);
            mat.SetFloat("_DataStreamTiling", 1.5f);
            mat.SetColor("_DataStreamColor", new Color(0.2f, 0.9f, 0.8f, 1f));
            mat.SetFloat("_DataStreamGlow", 1.3f);
            mat.SetVector("_DataStreamScrollDir", new Vector4(0f, 1f, 0f, 0f));
            mat.SetFloat("_DataStreamDensity", 3.0f);
            mat.SetFloat("_EnableLines", 1f);
            mat.SetFloat("_LineSpacing", 45f);
            mat.SetFloat("_LineSpeed", 0.7f);
            mat.SetFloat("_LineIntensity", 0.2f);
            mat.SetColor("_LineColor", new Color(0.2f, 0.9f, 0.8f, 1f));
            mat.SetFloat("_LineWidth", 0.7f);
            mat.SetFloat("_EnableRim", 1f);
            mat.SetColor("_RimColor", new Color(0.2f, 0.9f, 0.8f, 1f));
            mat.SetFloat("_RimPower", 2.2f);
            mat.SetFloat("_RimIntensity", 0.8f);
            mat.SetFloat("_EnableChromatic", 1f);
            mat.SetFloat("_ChromaticIntensity", 0.04f);
            mat.SetFloat("_ChromaticOffset", 0.6f);
        }
        
        private void ApplyMatrixCode(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.0f, 0.8f, 0.2f, 0.7f));
            mat.SetFloat("_HologramIntensity", 1.2f);
            mat.SetFloat("_HologramOpacity", 0.7f);
            mat.SetFloat("_HologramFlickerSpeed", 0.8f);
            mat.SetFloat("_HologramFlickerIntensity", 0.08f);
            mat.SetFloat("_EnableDataStream", 1f);
            mat.SetFloat("_DataStreamSpeed", 1.5f);
            mat.SetFloat("_DataStreamIntensity", 0.9f);
            mat.SetFloat("_DataStreamTiling", 2.0f);
            mat.SetColor("_DataStreamColor", new Color(0.0f, 1.0f, 0.3f, 1f));
            mat.SetFloat("_DataStreamGlow", 1.5f);
            mat.SetVector("_DataStreamScrollDir", new Vector4(0f, 1f, 0f, 0f));
            mat.SetFloat("_DataStreamDensity", 4.0f);
            mat.SetFloat("_EnableGlitch", 1f);
            mat.SetFloat("_GlitchIntensity", 0.1f);
            mat.SetFloat("_GlitchSpeed", 5.0f);
            mat.SetFloat("_GlitchColorIntensity", 0.2f);
            mat.SetFloat("_EnableNoise", 1f);
            mat.SetFloat("_NoiseIntensity", 0.05f);
            mat.SetFloat("_NoiseSpeed", 1.0f);
            mat.SetFloat("_NoiseTiling", 2.0f);
        }
        
        private void ApplyNeuralNetwork(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.2f, 0.5f, 0.8f, 0.7f));
            mat.SetFloat("_HologramIntensity", 1.1f);
            mat.SetFloat("_HologramOpacity", 0.7f);
            mat.SetFloat("_HologramFlickerSpeed", 0.5f);
            mat.SetFloat("_HologramFlickerIntensity", 0.05f);
            mat.SetFloat("_EnableDataStream", 1f);
            mat.SetFloat("_DataStreamSpeed", 0.8f);
            mat.SetFloat("_DataStreamIntensity", 0.7f);
            mat.SetFloat("_DataStreamTiling", 1.2f);
            mat.SetColor("_DataStreamColor", new Color(0.3f, 0.6f, 0.9f, 1f));
            mat.SetFloat("_DataStreamGlow", 1.2f);
            mat.SetVector("_DataStreamScrollDir", new Vector4(0.5f, 0.5f, 0f, 0f));
            mat.SetFloat("_DataStreamDensity", 2.5f);
            mat.SetFloat("_EnableCircuit", 1f);
            mat.SetFloat("_CircuitIntensity", 0.4f);
            mat.SetColor("_CircuitColor", new Color(0.3f, 0.6f, 0.9f, 1f));
            mat.SetFloat("_CircuitSpeed", 0.5f);
        }
        
        private void ApplyDigitalVirus(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.8f, 0.2f, 0.2f, 0.7f));
            mat.SetFloat("_HologramIntensity", 1.3f);
            mat.SetFloat("_HologramOpacity", 0.7f);
            mat.SetFloat("_HologramFlickerSpeed", 1.0f);
            mat.SetFloat("_HologramFlickerIntensity", 0.1f);
            mat.SetFloat("_EnableDataStream", 1f);
            mat.SetFloat("_DataStreamSpeed", 1.8f);
            mat.SetFloat("_DataStreamIntensity", 0.9f);
            mat.SetFloat("_DataStreamTiling", 1.8f);
            mat.SetColor("_DataStreamColor", new Color(1.0f, 0.3f, 0.3f, 1f));
            mat.SetFloat("_DataStreamGlow", 1.4f);
            mat.SetVector("_DataStreamScrollDir", new Vector4(0f, 1f, 0f, 0f));
            mat.SetFloat("_DataStreamDensity", 3.5f);
            mat.SetFloat("_EnableGlitch", 1f);
            mat.SetFloat("_GlitchIntensity", 0.2f);
            mat.SetFloat("_GlitchSpeed", 7.0f);
            mat.SetFloat("_GlitchColorIntensity", 0.3f);
            mat.SetFloat("_EnableDistortion", 1f);
            mat.SetFloat("_DistortionSpeed", 1.0f);
            mat.SetFloat("_DistortionIntensity", 0.05f);
            mat.SetFloat("_DistortionTiling", 2.0f);
        }
        
        // Implemented remaining data stream presets with basic variations
        private void ApplyBinaryFlow(Material mat)
        {
            ApplyDataStream(mat);
            mat.SetColor("_DataStreamColor", new Color(0.1f, 0.5f, 0.9f, 1f));
            mat.SetFloat("_DataStreamSpeed", 1.0f);
            mat.SetFloat("_DataStreamDensity", 5.0f);
        }
        
        private void ApplyCryptoStream(Material mat)
        {
            ApplyDataStream(mat);
            mat.SetColor("_DataStreamColor", new Color(0.9f, 0.7f, 0.1f, 1f));
            mat.SetFloat("_DataStreamSpeed", 0.7f);
            mat.SetFloat("_DataStreamDensity", 2.0f);
            mat.SetFloat("_EnableChromatic", 1f);
            mat.SetFloat("_ChromaticIntensity", 0.06f);
        }
        
        private void ApplyCodeCascade(Material mat)
        {
            ApplyDataStream(mat);
            mat.SetColor("_DataStreamColor", new Color(0.3f, 0.3f, 0.9f, 1f));
            mat.SetFloat("_DataStreamSpeed", 2.0f);
            mat.SetVector("_DataStreamScrollDir", new Vector4(0f, 1f, 0f, 0f));
        }
        
        private void ApplySignalPulse(Material mat)
        {
            ApplyDataStream(mat);
            mat.SetColor("_DataStreamColor", new Color(1f, 0.5f, 0f, 1f));
            mat.SetFloat("_EnablePulse", 1f);
            mat.SetFloat("_PulseSpeed", 2.0f);
            mat.SetFloat("_PulseAmplitude", 0.2f);
        }
        
        private void ApplyDataVortex(Material mat)
        {
            ApplyDataStream(mat);
            mat.SetVector("_DataStreamScrollDir", new Vector4(0.7f, 0.7f, 0f, 0f));
            mat.SetFloat("_DataStreamSpeed", 1.5f);
            mat.SetFloat("_EnableDistortion", 1f);
            mat.SetFloat("_DistortionIntensity", 0.1f);
        }
        
        private void ApplyQuantumData(Material mat)
        {
            ApplyDataStream(mat);
            mat.SetColor("_DataStreamColor", new Color(0.5f, 0f, 0.8f, 1f));
            mat.SetFloat("_DataStreamGlow", 2.0f);
            mat.SetFloat("_EnableNoise", 1f);
            mat.SetFloat("_NoiseIntensity", 0.1f);
        }
        
        private void ApplyInfoWave(Material mat)
        {
            ApplyDataStream(mat);
            mat.SetColor("_DataStreamColor", new Color(0f, 0.8f, 0.8f, 1f));
            mat.SetFloat("_EnableBeam", 1f);
            mat.SetFloat("_BeamSpeed", 1.5f);
            mat.SetFloat("_BeamWidth", 0.1f);
        }
        
        private void ApplyEncryptedFlow(Material mat)
        {
            ApplyDataStream(mat);
            mat.SetColor("_DataStreamColor", new Color(0.6f, 0.1f, 0.1f, 1f));
            mat.SetFloat("_EnableGlitch", 1f);
            mat.SetFloat("_GlitchIntensity", 0.15f);
            mat.SetFloat("_DataStreamSpeed", 0.5f);
        }
        
        private void ApplyNetworkTraffic(Material mat)
        {
            ApplyDataStream(mat);
            mat.SetColor("_DataStreamColor", new Color(0.3f, 0.7f, 0.3f, 1f));
            mat.SetVector("_DataStreamScrollDir", new Vector4(1f, 0f, 0f, 0f));
            mat.SetFloat("_DataStreamDensity", 6.0f);
        }
        
        private void ApplyBlockchainVisual(Material mat)
        {
            ApplyDataStream(mat);
            mat.SetColor("_DataStreamColor", new Color(0.9f, 0.5f, 0f, 1f));
            mat.SetFloat("_EnableSquareGrid", 1f);
            mat.SetFloat("_SquareSize", 20f);
            mat.SetFloat("_SquareIntensity", 0.3f);
        }
        
        // INTERFACE PRESETS
        
        private void ApplyAdvancedInterface(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.2f, 0.7f, 0.9f, 0.8f));
            mat.SetFloat("_HologramIntensity", 1.1f);
            mat.SetFloat("_HologramOpacity", 0.8f);
            mat.SetFloat("_HologramFlickerSpeed", 0.3f);
            mat.SetFloat("_HologramFlickerIntensity", 0.04f);
            mat.SetFloat("_EnableInterface", 1f);
            mat.SetFloat("_InterfaceSpeed", 0.4f);
            mat.SetColor("_InterfaceColor", new Color(0.3f, 0.8f, 1f, 1f));
            mat.SetFloat("_InterfaceIntensity", 0.7f);
            mat.SetFloat("_InterfaceTiling", 1.5f);
            mat.SetFloat("_InterfaceGlow", 1.2f);
            mat.SetFloat("_InterfaceScrollX", 0.1f);
            mat.SetFloat("_InterfaceScrollY", 0.05f);
            mat.SetFloat("_EnableHexGrid", 1f);
            mat.SetFloat("_HexSize", 15f);
            mat.SetFloat("_HexIntensity", 0.2f);
            mat.SetColor("_HexColor", new Color(0.3f, 0.8f, 1f, 1f));
            mat.SetFloat("_HexEmission", 1.2f);
            mat.SetFloat("_EnableScanLine", 1f);
            mat.SetColor("_ScanLineColor", new Color(0.3f, 0.8f, 1f, 1f));
            mat.SetFloat("_ScanLineWidth", 0.02f);
            mat.SetFloat("_ScanLineSpeed", 0.3f);
            mat.SetFloat("_ScanLineAmount", 50f);
            mat.SetFloat("_EnableRim", 1f);
            mat.SetColor("_RimColor", new Color(0.4f, 0.8f, 1f, 1f));
            mat.SetFloat("_RimPower", 2.0f);
            mat.SetFloat("_RimIntensity", 0.7f);
        }
        
        // Implemented remaining interface presets with variations
        private void ApplyTacticalDisplay(Material mat)
        {
            ApplyAdvancedInterface(mat);
            mat.SetColor("_Color", new Color(0.8f, 0.2f, 0.2f, 0.8f));
            mat.SetColor("_InterfaceColor", new Color(1f, 0.3f, 0.3f, 1f));
            mat.SetColor("_HexColor", new Color(1f, 0.2f, 0.2f, 1f));
        }
        
        private void ApplyMedicalScan(Material mat)
        {
            ApplyAdvancedInterface(mat);
            mat.SetColor("_Color", new Color(0.2f, 0.8f, 0.2f, 0.8f));
            mat.SetColor("_InterfaceColor", new Color(0.3f, 1f, 0.3f, 1f));
            mat.SetFloat("_EnablePulse", 1f);
            mat.SetFloat("_PulseSpeed", 1.0f);
            mat.SetColor("_PulseColor", new Color(0.3f, 1f, 0.3f, 1f));
        }
        
        private void ApplySpaceNavigation(Material mat)
        {
            ApplyAdvancedInterface(mat);
            mat.SetColor("_Color", new Color(0.1f, 0.3f, 0.6f, 0.8f));
            mat.SetFloat("_EnableWireframe", 1f);
            mat.SetFloat("_WireframeDensity", 3f);
        }
        
        private void ApplyCyberConsole(Material mat)
        {
            ApplyAdvancedInterface(mat);
            mat.SetColor("_Color", new Color(0.5f, 0.1f, 0.8f, 0.8f));
            mat.SetFloat("_EnableCircuit", 1f);
            mat.SetFloat("_CircuitIntensity", 0.6f);
        }
        
        private void ApplyHoloDashboard(Material mat)
        {
            ApplyAdvancedInterface(mat);
            mat.SetColor("_InterfaceColor", new Color(0.9f, 0.9f, 0.9f, 1f));
            mat.SetFloat("_EnableSquareGrid", 1f);
            mat.SetFloat("_SquareSize", 30f);
        }
        
        private void ApplyAugmentedReality(Material mat)
        {
            ApplyAdvancedInterface(mat);
            mat.SetFloat("_BlendMode", 1);
            mat.SetColor("_Color", new Color(0.5f, 0.8f, 0.5f, 0.6f));
        }
        
        private void ApplyControlPanel(Material mat)
        {
            ApplyAdvancedInterface(mat);
            mat.SetColor("_Color", new Color(0.8f, 0.5f, 0.2f, 0.8f));
            mat.SetFloat("_EnableBeam", 1f);
            mat.SetFloat("_BeamCount", 2f);
        }
        
        private void ApplyDataGrid(Material mat)
        {
            ApplyAdvancedInterface(mat);
            mat.SetFloat("_EnableSquareGrid", 1f);
            mat.SetFloat("_SquareSize", 25f);
            mat.SetFloat("_SquareIntensity", 0.5f);
        }
        
        private void ApplyTechHUD(Material mat)
        {
            ApplyAdvancedInterface(mat);
            mat.SetColor("_Color", new Color(0.1f, 0.9f, 0.9f, 0.8f));
            mat.SetFloat("_EnableEdges", 1f);
            mat.SetFloat("_EdgeEmission", 2.0f);
        }
        
        private void ApplyVirtualTerminal(Material mat)
        {
            ApplyAdvancedInterface(mat);
            mat.SetColor("_Color", new Color(0.1f, 0.1f, 0.1f, 0.9f));
            mat.SetColor("_InterfaceColor", new Color(0f, 1f, 0f, 1f));
        }
        
        private void ApplySciFiInterface(Material mat)
        {
            ApplyAdvancedInterface(mat);
            mat.SetFloat("_EnableChromatic", 1f);
            mat.SetFloat("_ChromaticIntensity", 0.03f);
        }
        
        private void ApplyCommandCenter(Material mat)
        {
            ApplyAdvancedInterface(mat);
            mat.SetColor("_Color", new Color(0.3f, 0.3f, 0.5f, 0.8f));
            mat.SetFloat("_EnableScanning", 1f);
            mat.SetFloat("_ScanningSpeed", 0.5f);
        }
        
        private void ApplyNeuralInterface(Material mat)
        {
            ApplyAdvancedInterface(mat);
            mat.SetColor("_Color", new Color(0.8f, 0.5f, 0.8f, 0.8f));
            mat.SetFloat("_EnableNoise", 1f);
            mat.SetFloat("_NoiseIntensity", 0.05f);
        }
        
        // 3D PROJECTION PRESETS
        
        private void Apply3DProjection(Material mat)
        {
            ResetToDefault(mat);
            mat.SetColor("_Color", new Color(0.0f, 0.6f, 0.9f, 0.6f));
            mat.SetFloat("_HologramIntensity", 1.3f);
            mat.SetFloat("_HologramOpacity", 0.6f);
            mat.SetFloat("_HologramFlickerSpeed", 0.7f);
            mat.SetFloat("_HologramFlickerIntensity", 0.07f);
            mat.SetFloat("_EnableProjection", 1f);
            mat.SetFloat("_ProjectionHeight", 1.5f);
            mat.SetFloat("_ProjectionFadeDistance", 0.3f);
            mat.SetColor("_ProjectionColor", new Color(0.1f, 0.7f, 1f, 0.4f));
            mat.SetFloat("_ProjectionIntensity", 1.2f);
            mat.SetFloat("_ProjectionFlicker", 0.15f);
            mat.SetFloat("_ProjectionSpread", 0.6f);
            mat.SetFloat("_EnableScanLine", 1f);
            mat.SetColor("_ScanLineColor", new Color(0.0f, 0.7f, 1f, 1f));
            mat.SetFloat("_ScanLineWidth", 0.05f);
            mat.SetFloat("_ScanLineSpeed", 0.5f);
            mat.SetFloat("_ScanLineAmount", 30f);
            mat.SetFloat("_EnableFresnel", 1f);
            mat.SetColor("_FresnelColor", new Color(0.0f, 0.6f, 0.9f, 1f));
            mat.SetFloat("_FresnelPower", 2.0f);
            mat.SetFloat("_FresnelIntensity", 0.9f);
        }
        
        // Implemented remaining projection presets
        private void ApplyStarMap(Material mat)
        {
            Apply3DProjection(mat);
            mat.SetColor("_Color", new Color(0.1f, 0.1f, 0.3f, 0.6f));
            mat.SetColor("_ProjectionColor", new Color(0.5f, 0.5f, 1f, 0.4f));
            mat.SetFloat("_EnableNoise", 1f);
            mat.SetFloat("_NoiseIntensity", 0.1f);
        }
        
        private void ApplyMolecularStructure(Material mat)
        {
            Apply3DProjection(mat);
            mat.SetColor("_Color", new Color(0.3f, 0.8f, 0.3f, 0.6f));
            mat.SetFloat("_EnableWireframe", 1f);
            mat.SetFloat("_WireframeDensity", 4f);
        }
        
        private void ApplyCharacterProjection(Material mat)
        {
            Apply3DProjection(mat);
            mat.SetColor("_Color", new Color(0.7f, 0.7f, 0.8f, 0.7f));
            mat.SetFloat("_ProjectionHeight", 2.0f);
            mat.SetFloat("_EnableRim", 1f);
            mat.SetFloat("_RimIntensity", 1.5f);
        }
        
        private void ApplyPlanetHologram(Material mat)
        {
            Apply3DProjection(mat);
            mat.SetColor("_Color", new Color(0.5f, 0.3f, 0.1f, 0.6f));
            mat.SetFloat("_EnableVolumetric", 1f);
            mat.SetFloat("_VolumetricIntensity", 0.8f);
        }
        
        private void ApplyShipBlueprint(Material mat)
        {
            Apply3DProjection(mat);
            mat.SetColor("_Color", new Color(0.2f, 0.4f, 0.6f, 0.6f));
            mat.SetFloat("_EnableWireframe", 1f);
            mat.SetFloat("_EnableEdges", 1f);
        }
        
        private void ApplyOrbitalScan(Material mat)
        {
            Apply3DProjection(mat);
            mat.SetColor("_ProjectionColor", new Color(0.8f, 0.8f, 0.2f, 0.4f));
            mat.SetFloat("_EnableScanning", 1f);
            mat.SetFloat("_ScanningSpeed", 1.0f);
        }
        
        private void ApplyHoloGlobe(Material mat)
        {
            Apply3DProjection(mat);
            mat.SetColor("_Color", new Color(0.2f, 0.5f, 0.8f, 0.6f));
            mat.SetFloat("_ProjectionHeight", 1.8f);
            mat.SetFloat("_ProjectionSpread", 0.8f);
        }
        
        private void ApplyCosmicProjection(Material mat)
        {
            Apply3DProjection(mat);
            mat.SetColor("_Color", new Color(0.1f, 0.1f, 0.3f, 0.5f));
            mat.SetFloat("_EnableColorShift", 1f);
            mat.SetFloat("_ColorShiftSpeed", 0.5f);
        }
        
        private void ApplyGalacticMap(Material mat)
        {
            Apply3DProjection(mat);
            mat.SetColor("_ProjectionColor", new Color(0.6f, 0.3f, 0.9f, 0.4f));
            mat.SetFloat("_EnableNoise", 1f);
            mat.SetFloat("_NoiseIntensity", 0.15f);
        }
        
        private void ApplyStructureScan(Material mat)
        {
            Apply3DProjection(mat);
            mat.SetColor("_Color", new Color(0.8f, 0.4f, 0.1f, 0.6f));
            mat.SetFloat("_EnableDepth", 1f);
            mat.SetFloat("_DepthDistance", 2f);
        }
        
        private void ApplyHoloAvatar(Material mat)
        {
            Apply3DProjection(mat);
            mat.SetColor("_Color", new Color(0.5f, 0.7f, 0.9f, 0.7f));
            mat.SetFloat("_ProjectionHeight", 2.2f);
            mat.SetFloat("_EnableRim", 1f);
            mat.SetFloat("_RimPower", 2f);
        }
        
        private void ApplyQuantumCore(Material mat)
        {
            Apply3DProjection(mat);
            mat.SetColor("_ProjectionColor", new Color(0.8f, 0.1f, 0.8f, 0.5f));
            mat.SetFloat("_EnablePulse", 1f);
            mat.SetFloat("_PulseSpeed", 2.0f);
            mat.SetFloat("_EnableGlitch", 1f);
            mat.SetFloat("_GlitchIntensity", 0.1f);
        }
        
        private void ApplyEnergySphere(Material mat)
        {
            Apply3DProjection(mat);
            mat.SetColor("_Color", new Color(0.9f, 0.5f, 0.1f, 0.6f));
            mat.SetColor("_ProjectionColor", new Color(1f, 0.7f, 0.2f, 0.5f));
            mat.SetFloat("_EnableVolumetric", 1f);
            mat.SetFloat("_VolumetricIntensity", 1.2f);
            mat.SetFloat("_EnableRim", 1f);
            mat.SetColor("_RimColor", new Color(1f, 0.8f, 0.3f, 1f));
            mat.SetFloat("_RimIntensity", 2.0f);
        }
    }
}