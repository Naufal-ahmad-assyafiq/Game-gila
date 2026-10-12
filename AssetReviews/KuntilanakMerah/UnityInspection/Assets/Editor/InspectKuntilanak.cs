using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class InspectKuntilanak
{
    const string ModelPath = "Assets/Model/kuntilanak_merah_and_rig.fbx";
    [Serializable] public class MeshInfo { public string name; public int vertices, triangles, subMeshes, blendShapes, bindPoses, uvCount; }
    [Serializable] public class MaterialInfo { public string name, shader, baseColorTexture, emissionTexture, normalTexture; }
    [Serializable] public class ClipInfo { public string name; public float length, frameRate; public int curveBindings, maximumKeysPerCurve; public bool isHumanMotion; }
    [Serializable] public class RigInfo { public string animationType; public bool avatarPresent, avatarValid, avatarHuman; public string[] humanMappings; public string error; }
    [Serializable] public class Report
    {
        public string unityVersion;
        public float globalScale, fileScale;
        public MeshInfo[] meshes;
        public MaterialInfo[] materials;
        public ClipInfo[] clips;
        public int skinnedRenderers, meshRenderers;
        public int[] skinBones;
        public string[] transformNames;
        public Vector3 modelBoundsCenter, modelBoundsSize, headPosition, headFrontDirection;
        public Vector3 bakedBoundsCenter, bakedBoundsSize;
        public RigInfo genericRig, humanoidRig, humanoidWithoutBoneOptimization, humanoidWithManualFootMapping;
    }
    public static void Run()
    {
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        var report = new Report { unityVersion = Application.unityVersion, globalScale = importer.globalScale, fileScale = importer.fileScale };
        importer.isReadable = true;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;
        importer.SaveAndReimport();
        var assets = AssetDatabase.LoadAllAssetsAtPath(ModelPath);
        report.meshes = assets.OfType<Mesh>().Select(m => new MeshInfo {
            name=m.name, vertices=m.vertexCount, triangles=m.triangles.Length/3, subMeshes=m.subMeshCount,
            blendShapes=m.blendShapeCount, bindPoses=m.bindposes.Length, uvCount=m.uv.Length
        }).ToArray();
        report.materials = assets.OfType<Material>().Select(m => new MaterialInfo {
            name=m.name, shader=m.shader != null ? m.shader.name : null,
            baseColorTexture=TexturePath(m,"_MainTex"), emissionTexture=TexturePath(m,"_EmissionMap"), normalTexture=TexturePath(m,"_BumpMap")
        }).ToArray();
        report.clips = assets.OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).Select(c => {
            var bindings=AnimationUtility.GetCurveBindings(c);
            return new ClipInfo {name=c.name,length=c.length,frameRate=c.frameRate,isHumanMotion=c.isHumanMotion,curveBindings=bindings.Length,
                maximumKeysPerCurve=bindings.Length==0 ? 0 : bindings.Max(b=>AnimationUtility.GetEditorCurve(c,b).length)};
        }).ToArray();
        report.genericRig = Rig(importer);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var instance=UnityEngine.Object.Instantiate(model);
        try {
            var skins=instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            report.skinnedRenderers=skins.Length;
            report.meshRenderers=instance.GetComponentsInChildren<MeshRenderer>(true).Length;
            report.skinBones=skins.Select(s=>s.bones.Length).ToArray();
            var transforms=instance.GetComponentsInChildren<Transform>(true);
            report.transformNames=transforms.Select(t=>t.name).ToArray();
            var renderers=instance.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                report.modelBoundsCenter=bounds.center;report.modelBoundsSize=bounds.size;}
            var head=transforms.FirstOrDefault(t=>t.name=="Head");var front=transforms.FirstOrDefault(t=>t.name=="headfront");
            if(head!=null) report.headPosition=head.position;
            if(head!=null&&front!=null)report.headFrontDirection=(front.position-head.position).normalized;
            var points=new List<Vector3>();
            foreach(var skin in skins){var baked=new Mesh();skin.BakeMesh(baked);points.AddRange(baked.vertices.Select(v=>skin.transform.TransformPoint(v)));UnityEngine.Object.DestroyImmediate(baked);}
            if(points.Count>0){var exact=new Bounds(points[0],Vector3.zero);foreach(var p in points)exact.Encapsulate(p);report.bakedBoundsCenter=exact.center;report.bakedBoundsSize=exact.size;}
        } finally { UnityEngine.Object.DestroyImmediate(instance); }
        var messages=new List<string>();
        Application.LogCallback capture=(condition,stack,type)=>{if(type==LogType.Error||type==LogType.Warning)messages.Add(condition);};
        Application.logMessageReceived+=capture;
        try {
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
            report.humanoidRig=Rig(importer);
            report.humanoidRig.error=string.Join("\n",messages);
            messages.Clear();
            importer.optimizeBones=false;
            importer.preserveHierarchy=true;
            importer.SaveAndReimport();
            report.humanoidWithoutBoneOptimization=Rig(importer);
            report.humanoidWithoutBoneOptimization.error=string.Join("\n",messages);
        }catch(Exception e){report.humanoidRig=new RigInfo{animationType="Human",error=e.Message};}
        finally {Application.logMessageReceived-=capture;}
        File.WriteAllText("../unity-import-analysis.json",JsonUtility.ToJson(report,true));
        Debug.Log("KUNTILANAK_INSPECTION_OK: "+JsonUtility.ToJson(report));
    }
    static RigInfo Rig(ModelImporter importer) {
        var avatar=AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
        return new RigInfo{animationType=importer.animationType.ToString(),avatarPresent=avatar!=null,
            avatarValid=avatar!=null&&avatar.isValid,avatarHuman=avatar!=null&&avatar.isHuman,
            humanMappings=importer.humanDescription.human.Select(h=>h.humanName+"="+h.boneName).ToArray()};
    }
    public static void TestManualMapping() {
        var report=JsonUtility.FromJson<Report>(File.ReadAllText("../unity-import-analysis.json"));
        var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
        var description=importer.humanDescription;
        var human=description.human.Where(h=>h.humanName!="LeftFoot"&&h.humanName!="Jaw").ToList();
        human.Add(new HumanBone{humanName="LeftFoot",boneName="LeftFoot",limit=new HumanLimit{useDefaultValues=true}});
        description.human=human.ToArray();importer.humanDescription=description;
        var messages=new List<string>();
        Application.LogCallback capture=(condition,stack,type)=>{if(type==LogType.Error||type==LogType.Warning)messages.Add(condition);};
        Application.logMessageReceived+=capture;
        try{importer.SaveAndReimport();report.humanoidWithManualFootMapping=Rig(importer);report.humanoidWithManualFootMapping.error=string.Join("\n",messages);}
        finally{Application.logMessageReceived-=capture;}
        File.WriteAllText("../unity-import-analysis.json",JsonUtility.ToJson(report,true));
        Debug.Log("KUNTILANAK_MANUAL_MAPPING: "+JsonUtility.ToJson(report.humanoidWithManualFootMapping));
    }
    static string TexturePath(Material material,string property) {
        return material.HasProperty(property) ? AssetDatabase.GetAssetPath(material.GetTexture(property)) : null;
    }
}
