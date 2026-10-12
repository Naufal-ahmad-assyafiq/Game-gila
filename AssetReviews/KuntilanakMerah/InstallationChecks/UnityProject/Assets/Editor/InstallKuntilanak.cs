using System;
using System.IO;
using System.Linq;
using FpsHorrorKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class InstallKuntilanak
{
    const string Root = "Assets/HorrorPrototype/KuntilanakMerah/";
    const string ScenePath = "Assets/FpsHorrorKit/Scenes/MainScene.unity";
    static int step, openings, captureFrames;
    static double deadline;
    static DoorJumpscare trigger;
    static DoorSystem door;
    static ITOKey key;
    static float closed, firstDistance, lastDistance;
    static bool atTarget;
    static Camera camera;
    static RenderTexture target;
    static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }

    public static void Build()
    {
        try {
            foreach (string name in new[]{"BaseColor","Normal","MaskMap","Metallic","Roughness"}) {
                string path = Root + "Textures/KuntilanakMerah_" + name + ".png";
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.textureType = name=="Normal" ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.sRGBTexture = name=="BaseColor";
                ti.alphaSource = name=="MaskMap" ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                ti.alphaIsTransparency=false; ti.mipmapEnabled=true; ti.maxTextureSize=2048;
                ti.textureCompression=TextureImporterCompression.CompressedHQ;
                ti.SaveAndReimport();
            }
            var importer = (ModelImporter)AssetImporter.GetAtPath(Root+"Models/KuntilanakMerah.fbx");
            importer.animationType=ModelImporterAnimationType.Generic;
            importer.importAnimation=false; importer.importCameras=false; importer.importLights=false;
            importer.optimizeBones=false; importer.preserveHierarchy=true;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
            var shader=Shader.Find("HDRP/Lit"); Check(shader!=null,"HDRP/Lit unavailable");
            Directory.CreateDirectory(Root+"Materials"); Directory.CreateDirectory(Root+"Prefabs");
            var mat = new Material(shader) { name="KuntilanakMerah_HDRP" };
            mat.SetTexture("_BaseColorMap", Texture("BaseColor")); mat.SetColor("_BaseColor",Color.white);
            mat.SetTexture("_NormalMap",Texture("Normal")); mat.SetFloat("_NormalScale",1);
            mat.SetTexture("_MaskMap",Texture("MaskMap"));
            mat.SetFloat("_MetallicRemapMin",0); mat.SetFloat("_MetallicRemapMax",.15f);
            mat.SetFloat("_SmoothnessRemapMin",0); mat.SetFloat("_SmoothnessRemapMax",.65f);
            mat.SetFloat("_SurfaceType",0); mat.SetFloat("_AlphaCutoffEnable",0);
            mat.SetColor("_EmissiveColor",Color.black); mat.SetColor("_EmissiveColorLDR",Color.black);
            mat.SetTexture("_EmissiveColorMap",null);
            HDMaterial.ValidateMaterial(mat);
            AssetDatabase.CreateAsset(mat,Root+"Materials/KuntilanakMerah_HDRP.mat");
            var wrapper=new GameObject("KuntilanakMerah_Visual");
            var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Models/KuntilanakMerah.fbx"));
            model.transform.SetParent(wrapper.transform,false); model.transform.localRotation=Quaternion.Euler(0,180,0);
            foreach(var t in wrapper.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=2;
            foreach(var a in wrapper.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(a);
            foreach(var r in wrapper.GetComponentsInChildren<SkinnedMeshRenderer>(true)) {
                r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
                r.updateWhenOffscreen=true;
            }
            var head=wrapper.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Head");
            var face=new GameObject("FaceAnchor"); face.layer=2; face.transform.SetParent(wrapper.transform,false);
            face.transform.position=head.position+Vector3.up*.12f;
            float offset=-face.transform.localPosition.y;
            PrefabUtility.SaveAsPrefabAsset(wrapper,Root+"Prefabs/KuntilanakMerah_Visual.prefab");
            UnityEngine.Object.DestroyImmediate(wrapper);
            var scene=EditorSceneManager.OpenScene(ScenePath);
            var ghost=UnityEngine.Object.FindFirstObjectByType<GhostAppearance>();
            var holder=ghost.ghostModel.transform;
            while(holder.childCount>0) UnityEngine.Object.DestroyImmediate(holder.GetChild(0).gameObject);
            holder.name="KuntilanakMerah_Model";
            var visual=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/KuntilanakMerah_Visual.prefab"),holder);
            visual.transform.localPosition=Vector3.zero; visual.transform.localRotation=Quaternion.identity; visual.transform.localScale=Vector3.one;
            ghost.verticalOffset=offset; ghost.ghostModel.SetActive(false);
            EditorSceneManager.SaveScene(scene,ScenePath); AssetDatabase.SaveAssets();
            Debug.Log("KUNTILANAK_BUILD_OK: face offset="+offset+"; prefab="+AssetDatabase.AssetPathToGUID(Root+"Prefabs/KuntilanakMerah_Visual.prefab"));
            EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
    static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/KuntilanakMerah_"+name+".png");
    public static void Run() {
        EditorSceneManager.OpenScene(ScenePath); FpsHorrorKit.Editor.StageOneTools.ValidateScene();
        SessionState.SetBool("KuntilanakTest",true); Resume(); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Resume() {
        if(!SessionState.GetBool("KuntilanakTest",false))return;
        step=0; deadline=EditorApplication.timeSinceStartup+120;
        EditorApplication.update-=Probe; EditorApplication.update+=Probe;
    }
    static void Probe() {
        try {
            Check(EditorApplication.timeSinceStartup<deadline,"Runtime timeout step="+step);
            if(!EditorApplication.isPlaying || Time.frameCount<2)return;
            if(step==0) {
                trigger=GameObject.Find("StageOne_LockedDoor").GetComponent<DoorJumpscare>(); door=trigger.door;
                key=GameObject.Find("Temporary_Key_Yellow").GetComponent<ITOKey>(); closed=door.transform.localEulerAngles.y;
                openings=0; atTarget=true; firstDistance=0;lastDistance=0;
                door.onDoorOpened.AddListener(()=>{openings++;atTarget &= Mathf.Abs(Mathf.DeltaAngle(door.transform.localEulerAngles.y,door.endRotation))<.11f;});
                var renderer=trigger.ghost.ghostModel.GetComponentInChildren<SkinnedMeshRenderer>(true);
                Check(renderer!=null && renderer.sharedMesh.vertexCount==30614,"Missing imported skinned model");
                Check(renderer.sharedMaterial.shader.name=="HDRP/Lit","Wrong shader");
                Check(renderer.sharedMaterial.IsKeywordEnabled("_NORMALMAP_TANGENT_SPACE")&&renderer.sharedMaterial.IsKeywordEnabled("_MASKMAP"),"HDRP texture keywords missing");
                Check(trigger.ghost.ghostModel.GetComponentsInChildren<Collider>(true).Length==0,"Ghost blocks interactions");
                camera=Camera.main; Check(camera!=null,"Camera missing");
                if(camera.GetComponent<AudioListener>()==null)camera.gameObject.AddComponent<AudioListener>();
                step=1; return;
            }
            if(step==1) {
                door.Interact(); Check(Mathf.Abs(Mathf.DeltaAngle(door.transform.localEulerAngles.y,closed))<.01f&&!trigger.HasPlayed,"Locked door moved");
                Check(Physics.Raycast(key.transform.position+Vector3.forward,Vector3.back,out RaycastHit hit,1.5f)&&hit.collider.GetComponent<ITOKey>()==key,"Key raycast failed");
                var bad=new GameObject("InvalidKey");bad.AddComponent<ITOKey>().Interact();Check(bad.activeSelf,"Invalid key consumed");UnityEngine.Object.Destroy(bad);
                key.Interact(); Check(door.hasKey&&!key.gameObject.activeSelf,"Pickup failed");
                door.Interact(); Check(!door.isLocked&&!trigger.HasPlayed,"Scare before door complete");step=2;
            }
            else if(step==2&&trigger.HasPlayed) {
                Check(openings==1&&atTarget&&trigger.ghost.IsAppearing,"Opening event timing failed");
                Check(trigger.ghost.ghostModel.activeSelf,"Model not visible");
                firstDistance=Vector3.Dot(trigger.ghost.transform.position-camera.transform.position,camera.transform.forward);
                Check(firstDistance>2,"Approach did not start at distance"); step=3;
            }
            else if(step==3&&trigger.ghost.IsAppearing) {
                lastDistance=Vector3.Dot(trigger.ghost.transform.position-camera.transform.position,camera.transform.forward);
                var face=trigger.ghost.ghostModel.GetComponentsInChildren<Transform>().Single(t=>t.name=="FaceAnchor");
                var uv=camera.WorldToViewportPoint(face.position);
                Check(Mathf.Abs(uv.x-.5f)<.02f&&Mathf.Abs(uv.y-.5f)<.02f,"Head not centered: "+uv);
                var model=trigger.ghost.ghostModel.GetComponentsInChildren<Transform>().Single(t=>t.name=="Head");
                var front=trigger.ghost.ghostModel.GetComponentsInChildren<Transform>().Single(t=>t.name=="headfront");
                Check(Vector3.Dot((front.position-model.position).normalized,camera.transform.forward)<-.5f,"Face points away");
            }
            else if(step==3&&!trigger.ghost.IsAppearing) {
                Check(Mathf.Abs(lastDistance-trigger.ghost.endDistance)<.02f,"Approach end incorrect: "+lastDistance);
                Check(!trigger.ghost.ghostModel.activeSelf,"Ghost remained visible");door.Interact();step=4;
            }
            else if(step==4&&Mathf.Abs(Mathf.DeltaAngle(door.transform.localEulerAngles.y,closed))<.01f) {
                Check(openings==1,"Closing fired opening event");door.Interact();step=5;
            }
            else if(step==5&&openings==2) {
                Check(atTarget&&!trigger.ghost.IsAppearing&&trigger.HasPlayed,"One-shot failed");
                Debug.Log("KUNTILANAK_PLAYMODE_OK: locked door, physics pickup, invalid reference guard, completed opening, approach "+firstDistance+" -> "+lastDistance+", face alignment, cleanup, one-shot, HDRP materials passed.");
                SetupPortrait();step=6;
            }
            else if(step==6&&++captureFrames>20) {
                RenderTexture.active=target;var picture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                picture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);picture.Apply();RenderTexture.active=null;
                File.WriteAllBytes("../kuntilanak-hdrp-preview.png",picture.EncodeToPNG());
                Debug.Log("KUNTILANAK_RENDER_OK"); Finish(0);
            }
        } catch(Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void SetupPortrait() {
        trigger.ghost.enabled=false;trigger.ghost.ghostModel.SetActive(true);
        trigger.ghost.transform.SetPositionAndRotation(new Vector3(0,-1.5627f,1.3f),Quaternion.identity);
        camera.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity); camera.fieldOfView=60;
        camera.nearClipPlane=.03f;camera.farClipPlane=30;camera.cullingMask=1<<2;
        var hd=camera.GetComponent<HDAdditionalCameraData>()??camera.gameObject.AddComponent<HDAdditionalCameraData>();
        hd.clearColorMode=HDAdditionalCameraData.ClearColorMode.Color;hd.backgroundColorHDR=new Color(.025f,.025f,.025f,1);
        target=new RenderTexture(800,800,24,RenderTextureFormat.ARGBHalf);target.Create();camera.targetTexture=target;
        var light=new GameObject("TestOnly_KeyLight").AddComponent<Light>();light.type=LightType.Directional;
        light.gameObject.AddComponent<HDAdditionalLightData>();light.lightUnit=LightUnit.Lux;light.intensity=3000;
        light.transform.rotation=Quaternion.Euler(15,15,0);light.cullingMask=1<<2;
        var v=new GameObject("TestOnly_Exposure").AddComponent<Volume>();v.isGlobal=true;v.priority=1000;v.profile=ScriptableObject.CreateInstance<VolumeProfile>();
        var exposure=v.profile.Add<Exposure>(true);exposure.mode.Override(ExposureMode.Fixed);exposure.fixedExposure.Override(8);
        var tone=v.profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.Neutral);
    }
    static void Finish(int code) {SessionState.SetBool("KuntilanakTest",false);EditorApplication.update-=Probe;EditorApplication.Exit(code);}
}
