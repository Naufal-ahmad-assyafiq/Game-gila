using System;
using System.IO;
using FpsHorrorKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HarnessTools
{
    const string Folder = "Assets/HorrorPrototype/";
    const string ScenePath = "Assets/FpsHorrorKit/Scenes/MainScene.unity";
    static int step, openings;
    static double deadline;
    static DoorJumpscare trigger;
    static DoorSystem door;
    static ITOKey key;
    static float closed;
    static bool atTarget;

    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var doorRoot = new GameObject("Single_Door_1");
        var leaf = new GameObject("Door_Leaf"); leaf.transform.SetParent(doorRoot.transform, false);
        leaf.transform.position = new Vector3(-5.5094f,1.2807f,-14.5377f);
        var door = leaf.AddComponent<DoorSystem>(); door.rotationSpeed = 75; door.endRotation = 90;
        var cameraObject = new GameObject("MainCamera"); cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<Camera>().transform.position = new Vector3(-6.8f,1.7f,-13.8f);
        var yellow = Mat("Placeholder_Key_Yellow"); var white = Mat("Placeholder_Ghost_White");
        var red = Mat("Placeholder_Ghost_Red"); var dark = Mat("Placeholder_Ghost_Dark");
        var stage = new GameObject("StageOne_LockedDoor");
        var pickup = new GameObject("Temporary_Key_Yellow"); pickup.transform.SetParent(stage.transform,false);
        pickup.transform.position = new Vector3(-7,1.25f,-1.7f);
        var collider = pickup.AddComponent<BoxCollider>(); collider.size = new Vector3(.65f,.65f,.25f); collider.center = new Vector3(0,-.1f,0);
        var key = pickup.AddComponent<ITOKey>(); key.compatibleDoor = door;
        var keySettings = new SerializedObject(key); keySettings.FindProperty("interactText").stringValue = "Ambil kunci kuning [E]"; keySettings.ApplyModifiedPropertiesWithoutUndo();
        for(int i=0;i<12;i++) {
            float a = i*Mathf.PI*2/12;
            Shape(pickup.transform,"Key_Ring_"+i,PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.14f,Mathf.Sin(a)*.14f+.08f,0),Vector3.one*.085f,yellow);
        }
        Shape(pickup.transform,"Key_Shaft",PrimitiveType.Cube,new Vector3(0,-.2f,0),new Vector3(.07f,.4f,.07f),yellow);
        Shape(pickup.transform,"Key_Tooth_1",PrimitiveType.Cube,new Vector3(.07f,-.32f,0),new Vector3(.15f,.06f,.07f),yellow);
        Shape(pickup.transform,"Key_Tooth_2",PrimitiveType.Cube,new Vector3(.07f,-.22f,0),new Vector3(.15f,.06f,.07f),yellow);
        var ghostObject = new GameObject("Temporary_Ghost"); ghostObject.transform.SetParent(stage.transform,false); ghostObject.layer=2;
        ghostObject.transform.position = new Vector3(-5.5f,1,-13.8f);
        var ghost = ghostObject.AddComponent<GhostAppearance>();
        var model = new GameObject("Ghost_Model_ReplaceMe"); model.layer=2; model.transform.SetParent(ghostObject.transform,false);
        Shape(model.transform,"Body",PrimitiveType.Capsule,new Vector3(0,.25f,0),new Vector3(.55f,.43f,.35f),white);
        Shape(model.transform,"Head",PrimitiveType.Sphere,new Vector3(0,.93f,0),Vector3.one*.5f,white);
        Shape(model.transform,"Eye_Left",PrimitiveType.Sphere,new Vector3(-.09f,.98f,-.22f),new Vector3(.075f,.1f,.05f),red);
        Shape(model.transform,"Eye_Right",PrimitiveType.Sphere,new Vector3(.09f,.98f,-.22f),new Vector3(.075f,.1f,.05f),red);
        Shape(model.transform,"Mouth",PrimitiveType.Cube,new Vector3(0,.82f,-.235f),new Vector3(.12f,.16f,.045f),dark);
        Shape(model.transform,"Arm_Left",PrimitiveType.Capsule,new Vector3(-.35f,.45f,-.12f),new Vector3(.13f,.3f,.13f),white);
        Shape(model.transform,"Arm_Right",PrimitiveType.Capsule,new Vector3(.35f,.45f,-.12f),new Vector3(.13f,.3f,.13f),white);
        ghost.ghostModel=model; model.SetActive(false);
        ghost.audioSource=ghostObject.AddComponent<AudioSource>(); ghost.audioSource.playOnAwake=false; ghost.audioSource.spatialBlend=0;
        ghost.scareClip=Sound();
        var trigger=stage.AddComponent<DoorJumpscare>(); trigger.door=door; trigger.ghost=ghost;
        door.isLocked=true; door.hasKey=false;
        EditorSceneManager.SaveScene(scene,ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("STAGE_ONE_SETUP_OK");
    }
    static Material Mat(string name) {
        var m=new Material(Shader.Find("Unlit/Color")) {name=name}; AssetDatabase.CreateAsset(m,Folder+name+".mat"); return m;
    }
    static void Shape(Transform parent,string name,PrimitiveType type,Vector3 p,Vector3 s,Material m) {
        var o=GameObject.CreatePrimitive(type); o.name=name; o.layer=parent.gameObject.layer;
        o.transform.SetParent(parent,false); o.transform.localPosition=p; o.transform.localScale=s;
        UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
        var r=o.GetComponent<MeshRenderer>(); r.sharedMaterial=m; r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    static AudioClip Sound() {
        string path=Folder+"Placeholder_Scare.wav"; const int rate=44100; int n=(int)(rate*.65f); var random=new System.Random(71);
        using(var w=new BinaryWriter(File.Create(path))) {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36+n*2); w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16);
            w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate*2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(n*2);
            for(int i=0;i<n;i++){float t=(float)i/rate;float e=Mathf.Min(1,t/.015f)*Mathf.Exp(-t*6);double v=(Math.Sin(t*110*Math.PI*2)*.3+(random.NextDouble()*2-1)*.16)*e;w.Write((short)(v*short.MaxValue));}
        }
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport); return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    public static void Run() {
        EditorSceneManager.OpenScene(ScenePath); FpsHorrorKit.Editor.StageOneTools.ValidateScene(); SessionState.SetBool("HorrorCheck",true); Resume(); EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Resume() {
        if(!SessionState.GetBool("HorrorCheck",false))return;
        step=0; deadline=EditorApplication.timeSinceStartup+60; EditorApplication.update-=Probe; EditorApplication.update+=Probe;
    }
    static void Probe() {
        try {
            Check(EditorApplication.timeSinceStartup<deadline,"Runtime timeout: "+step);
            if(!EditorApplication.isPlaying || Time.frameCount<2)return;
            if(step==0){trigger=GameObject.Find("StageOne_LockedDoor").GetComponent<DoorJumpscare>();door=trigger.door;key=GameObject.Find("Temporary_Key_Yellow").GetComponent<ITOKey>();closed=door.transform.localEulerAngles.y;openings=0;atTarget=true;
                door.onDoorOpened.AddListener(()=>{openings++;atTarget &= Mathf.Abs(Mathf.DeltaAngle(door.transform.localEulerAngles.y,door.endRotation))<.11f;});step=1;return;}
            if(step==1){door.Interact();Check(Mathf.Abs(Mathf.DeltaAngle(door.transform.localEulerAngles.y,closed))<.01f && !trigger.HasPlayed,"Locked door moved");
                Check(Physics.Raycast(key.transform.position+Vector3.forward,Vector3.back,out RaycastHit hit,1.5f)&&hit.collider.GetComponent<ITOKey>()==key,"Pickup raycast failed");
                var bad=new GameObject("InvalidKey");bad.AddComponent<ITOKey>().Interact();Check(bad.activeSelf,"Invalid key consumed");UnityEngine.Object.Destroy(bad);
                key.Interact();Check(door.hasKey&&!key.gameObject.activeSelf,"Pickup failed");door.Interact();Check(!door.isLocked&&!trigger.HasPlayed,"Premature scare");step=2;}
            else if(step==2&&trigger.HasPlayed){Check(openings==1&&atTarget&&trigger.ghost.IsAppearing,"Opening event timing");step=3;}
            else if(step==3&&!trigger.ghost.IsAppearing){Check(!trigger.ghost.ghostModel.activeSelf,"Ghost remained visible");door.Interact();step=4;}
            else if(step==4&&Mathf.Abs(Mathf.DeltaAngle(door.transform.localEulerAngles.y,closed))<.01f){Check(openings==1,"Close fired opening event");door.Interact();step=5;}
            else if(step==5&&openings==2){Check(atTarget&&!trigger.ghost.IsAppearing&&trigger.HasPlayed,"Scare replayed");Debug.Log("STAGE_ONE_PLAYMODE_OK: lock, pickup collider, invalid key guard, opening completion, ghost cleanup and one-shot passed.");Finish(0);}
        }catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
    static void Finish(int code){SessionState.SetBool("HorrorCheck",false);EditorApplication.update-=Probe;EditorApplication.Exit(code);}
}
