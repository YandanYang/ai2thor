/*
Based on ObjExporter.cs, this "wrapper" lets you export to .OBJ directly from the editor menu.
This should be put in your "Editor"-folder. Use by selecting the objects you want to export, and select
the appropriate menu item from "Custom->Export". Exported models are put in a folder called
"ExportedObj" in the root of your Unity-project. Textures should also be copied and placed in the
same folder. */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using System.Drawing;
struct ObjMaterial {
    public string name;
    public string textureName;
    public string textureLoadPath;
    public string textureSavePath;
    public string NormalMapName;
    public string NormalMapLoadPath;
    public string NormalMapSavePath;
    public double color_red;
    public double color_green;
    public double color_blue;
    public double transparent;

}

public class EditorObjExporter : ScriptableObject {
    private static int vertexOffset = 0;
    private static int normalOffset = 0;
    private static int uvOffset = 0;


    //User should probably be able to change this. It is currently left as an excercise for
    //the reader.
    private static string targetFolder = "ExportedObj";

    public static Texture2D TextureToTexture2D(Texture texture)
    {
        Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 32);
        Graphics.Blit(texture, renderTexture);
    
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture2D.Apply();
    
        RenderTexture.active = currentRT;
        RenderTexture.ReleaseTemporary(renderTexture);
        return texture2D;
    }

// 调用方法：FindFile(@"G:\xq\", "test.txt");
    private static string MeshToString(MeshFilter mf, Dictionary<string, ObjMaterial> materialList,String folder, String filename) {
        Mesh m = mf.sharedMesh;
        
        Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
        var debug = mf.GetComponent<Renderer>();
        
        StringBuilder sb = new StringBuilder();
        if (m==null){
            return sb.ToString();
        }
        sb.Append("g ").Append(mf.name).Append("\n");
        // if (m.name.Contains("Statue"))
        // {
        //     sb = sb;
        // }
        foreach (Vector3 lv in m.vertices) {
            Vector3 wv = mf.transform.TransformPoint(lv);

            //This is sort of ugly - inverting x-component since we're in
            // a different coordinate system than "everyone" is "used to".
            sb.Append(string.Format("v {0} {1} {2}\n", -wv.x, -wv.z, wv.y));
            // sb.Append(string.Format("v {0} {1} {2}\n", -wv.x, wv.y, wv.z));
        }
        sb.Append("\n");

        foreach (Vector3 lv in m.normals) {
            Vector3 wv = mf.transform.TransformDirection(lv);

            // sb.Append(string.Format("vn {0} {1} {2}\n", -wv.x, wv.y, wv.z));
            sb.Append(string.Format("vn {0} {1} {2}\n", -wv.x, -wv.z, wv.y));
        }
        sb.Append("\n");
        foreach (Vector3 v in m.uv) {
            sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
        }

        for (int material = 0; material < m.subMeshCount; material++) {
            if (mats[material]==null){
                continue;
            }
           
            if (mats[material].name.Contains("Placeable_Surface_Mat")) {
                    continue;
                }
            sb.Append("\n");
            string name = mats[material].name.Replace(" (Instance)", string.Empty);
            mats[material].name = name;
            sb.Append("usemtl ").Append(mats[material].name).Append("\n");
            sb.Append("usemap ").Append(mats[material].name).Append("\n");
            bool bWriteTexture = false;

            //See if this material is already in the materiallist.
            try {
                ObjMaterial objMaterial = new ObjMaterial();

                objMaterial.name = mats[material].name;
                //maintexture
                var flag = mats[material].HasProperty("_MainTex");
                string destinationFile = null;
                if  (flag){

                    if (mats[material].mainTexture){
                        bWriteTexture = true;
                        string[] filepath = Directory.GetFiles(targetFolder+"/material/",mats[material].mainTexture.name +".*");
                        Debug.Log(targetFolder+"\\material\\"+mats[material].mainTexture.name );
                        objMaterial.textureLoadPath = filepath[0].Replace("\\","/");
                        Texture2D result = new Texture2D(mats[material].mainTexture.width,mats[material].mainTexture.height,TextureFormat.RGB24,false);
                        Texture2D tex2D = TextureToTexture2D(mats[material].mainTexture);
                        for (int i = 0;i<result.height;i++){
                            for (int j = 0;j<result.width;j++){
                                Color newColor = tex2D.GetPixelBilinear((float)j/(float)result.width,(float)i/(float)result.height);
                                newColor.r = newColor.r * mats[material].color.r;
                                newColor.g = newColor.g * mats[material].color.g;
                                newColor.b = newColor.b * mats[material].color.b;
                                result.SetPixel(j,i,newColor);
                            }
                        }
                        result.Apply();
                        destinationFile = objMaterial.textureLoadPath;
                        int stripIndex = destinationFile.LastIndexOf('/');//FIXME: Should be Path.PathSeparator;

                        if (stripIndex >= 0)
                            destinationFile = destinationFile.Substring(stripIndex + 1).Trim();


                        destinationFile = destinationFile.Replace(".tif",".png");
                        destinationFile = destinationFile.Replace(".jpg",".png");
                        destinationFile = destinationFile.Replace(".JPG",".png");
                        destinationFile = destinationFile.Replace(".tga",".png");

                        destinationFile = filename.Replace("|", "_") + "_" + destinationFile;
                        objMaterial.textureName = destinationFile;
                        objMaterial.textureSavePath = folder + "/" + destinationFile;
                        File.WriteAllBytes(objMaterial.textureSavePath,result.EncodeToPNG());


                        
                        // string array1 = FindFile(Path.GetTempPath(), mats[material].mainTexture.name +"*");
                    //     string[] array1 = Directory.GetFiles(Path.GetTempPath(), mats[material].mainTexture.name +"*",SearchOption.AllDirectories);
                        // objMaterial.textureName = AssetDatabase.GetAssetPath(mats[material].mainTexture);
                    }
                    else
                    objMaterial.textureName = null;
                }
                if (bWriteTexture == false){
                    bWriteTexture = true;
                    Color newColor = new Color();
                    Texture2D result = new Texture2D(8,8,TextureFormat.RGB24,false);
                    for (int i = 0;i<result.height;i++){ 
                        for (int j = 0;j<result.width;j++){
                            
                            newColor.r = mats[material].color.r;
                            newColor.g = mats[material].color.g;
                            newColor.b = mats[material].color.b;
                            result.SetPixel(j,i,newColor);
                        }
                    }
                    result.Apply();
                    destinationFile = "R"+(int)(newColor.r*100)+"_G"+(int)(newColor.g*100)+"_B"+(int)(newColor.b*100)+".png";
                    int stripIndex = destinationFile.LastIndexOf('/');//FIXME: Should be Path.PathSeparator;

                    if (stripIndex >= 0)
                        destinationFile = destinationFile.Substring(stripIndex + 1).Trim();

                    destinationFile = filename.Replace("|", "_") + "_" + destinationFile;
                    objMaterial.textureName = destinationFile;
                    objMaterial.textureSavePath = folder + "/" + destinationFile;
                    File.WriteAllBytes(objMaterial.textureSavePath,result.EncodeToPNG());
                }              
                // normal map
                flag = mats[material].HasProperty("_BumpMap");
                if  (flag){
                    Texture2D normal = (Texture2D)mats[material].GetTexture("_BumpMap");
                    if (normal){
                        string[] filepath = Directory.GetFiles(targetFolder+"/material/",normal.name +".*");
                        Debug.Log(targetFolder+"\\material\\"+normal.name);
                        objMaterial.NormalMapLoadPath = filepath[0].Replace("\\","/");
                        
                        // Texture2D tex2D_n  = normal;
                        Texture2D result_n = new Texture2D(normal.width,normal.height,TextureFormat.RGB24,false);
                        byte[] normalData = System.IO.File.ReadAllBytes(objMaterial.NormalMapLoadPath);
                        result_n.LoadImage(normalData);
                        
                        // for (int i = 0;i<result_n.height;i++){
                        //     for (int j = 0;j<result_n.width;j++){
                        //         Color newColor = tex2D_n.GetPixelBilinear((float)j/(float)result_n.width,(float)i/(float)result_n.height);
                        //         result_n.SetPixel(j,i,newColor);
                        //     }
                        // }
                        // result_n.Apply();
                        // Texture2D result = new Texture2D(mats[material].mainTexture.width,mats[material].mainTexture.height,TextureFormat.RGB24,false);
                        // Texture2D tex2D = TextureToTexture2D(mats[material].mainTexture);                  
                        // normal.Apply();       
                        destinationFile = objMaterial.NormalMapLoadPath;
                        int stripIndex = destinationFile.LastIndexOf('/');//FIXME: Should be Path.PathSeparator;

                        if (stripIndex >= 0)
                            destinationFile = destinationFile.Substring(stripIndex + 1).Trim();

                        destinationFile = destinationFile.Replace(".tif",".png");
                        destinationFile = destinationFile.Replace(".jpg",".png");
                        destinationFile = destinationFile.Replace(".JPG",".png");
                        destinationFile = destinationFile.Replace(".tga",".png");

                        destinationFile = filename.Replace("|", "_") + "_" + destinationFile;
                        objMaterial.NormalMapName = destinationFile;
                        objMaterial.NormalMapSavePath = folder + "/" + destinationFile;
                        
                        File.WriteAllBytes(objMaterial.NormalMapSavePath,result_n.EncodeToPNG());
                        
                        // result_n.Save(destinationFile, System.Drawing.Imaging.ImageFormat.Png);
                        // Texture2D tex = Resources.Load(objMaterial.NormalMapName) as Texture2D;
                        // var img = System.Drawing.Bitmap.FromFile(targetFolder+"\\material\\"+objMaterial.NormalMapName);
                        // .Save(destinationFile, System.Drawing.Imaging.ImageFormat.Png);
                            // string array1 = FindFile(Path.GetTempPath(), mats[material].mainTexture.name +"*");
                        //     string[] array1 = Directory.GetFiles(Path.GetTempPath(), mats[material].mainTexture.name +"*",SearchOption.AllDirectories);
                            // objMaterial.textureName = AssetDatabase.GetAssetPath(mats[material].mainTexture);
                    }
                    
                }
                // else{
                //     material = material;
                // }
                flag = mats[material].HasProperty("_Color");
                // Color color = mats[material].GetColor("_Color");
                // Color speccolor = mats[material].GetColor("_SpecColor");
                // if (speccolor.r >0){
                //     flag = flag;
                // }
                if  (flag){
               
                    objMaterial.color_red = mats[material].color.r;
                    objMaterial.color_green = mats[material].color.g;
                    objMaterial.color_blue = mats[material].color.b;
                    objMaterial.transparent = mats[material].color.a;
                }
                // else {
                //     material = material;
                // }
                //objMaterial.NormalMapName = null;
                materialList.Add(objMaterial.name, objMaterial);
            } catch (ArgumentException) {
                material = material;
                //Already in the dictionary
            }


            int[] triangles = m.GetTriangles(material);
            for (int i = 0; i < triangles.Length; i += 3) {
                //Because we inverted the x-component, we also needed to alter the triangle winding.
                sb.Append(string.Format("f {1}/{1}/{1} {0}/{0}/{0} {2}/{2}/{2}\n",
                    triangles[i] + 1 + vertexOffset, triangles[i + 1] + 1 + vertexOffset, triangles[i + 2] + 1 + vertexOffset));
            }
        }
        vertexOffset += m.vertices.Length;
        normalOffset += m.normals.Length;
        uvOffset += m.uv.Length;

        return sb.ToString();
    }

    private static void Clear() {
        vertexOffset = 0;
        normalOffset = 0;
        uvOffset = 0;
    }

    private static Dictionary<string, ObjMaterial> PrepareFileWrite() {
        Clear();

        return new Dictionary<string, ObjMaterial>();
    }

    private static void MaterialsToFile(Dictionary<string, ObjMaterial> materialList, string folder, string filename) {
        using (StreamWriter sw = new StreamWriter(folder + "/" + filename.Replace("|", "_") + ".mtl")) {
            foreach (KeyValuePair<string, ObjMaterial> kvp in materialList) {

                if (kvp.Key.Contains("Placeable_Surface_Mat")) {
                    continue;
                }
                string r = Convert.ToString(kvp.Value.color_red);
                string g = Convert.ToString(kvp.Value.color_green);
                string b = Convert.ToString(kvp.Value.color_blue);
                string d = Convert.ToString(kvp.Value.transparent);
                
                sw.Write("\n");
                string name = kvp.Key.Replace(" (Instance)", string.Empty);
                sw.Write("newmtl {0}\n", name);
                sw.Write("Ka  {0} {1} {2}\n",r,g,b);
                sw.Write("Kd  {0} {1} {2}\n",r,g,b);
                sw.Write("Ks  0.0 0.0 0.0\n",r,g,b);
                sw.Write("Ke  0.0 0.0 0.0\n",r,g,b);
                // sw.Write("Ks  {0} {1} {2}\n",r,g,b);
                sw.Write("d  {0}\n", d);
                sw.Write("Ns  0.0\n");
                sw.Write("illum 1\n");
                 

                  
                if (kvp.Value.textureName != null) {
                    string destinationFile = kvp.Value.textureName;
                    sw.Write("map_Kd {0}", destinationFile);
                }

                if (kvp.Value.NormalMapName != null) {
                    if (kvp.Value.textureName != null){
                        sw.Write("\n");
                    }
                    string destinationFile = kvp.Value.NormalMapName;
                    sw.Write("map_Bump -bm 1.000000 {0}", destinationFile);
                }

                sw.Write("\n\n\n");
            }
        }
    }

    private static void MeshToFile(MeshFilter mf, string folder, string filename) {
        Dictionary<string, ObjMaterial> materialList = PrepareFileWrite();

        using (StreamWriter sw = new StreamWriter(folder + "/" + filename.Replace("|", "_") + ".obj")) {
            sw.Write("mtllib ./" + filename.Replace("|", "_") + ".mtl\n");

            sw.Write(MeshToString(mf, materialList, folder, filename));
        }

        MaterialsToFile(materialList, folder, filename.Replace("|", "_"));
    }

    private static void MeshesToFile(MeshFilter[] mf, string folder, string filename) {
        Dictionary<string, ObjMaterial> materialList = PrepareFileWrite();


        using (StreamWriter sw = new StreamWriter(folder + "/" + filename.Replace("|", "_") + ".obj")) {
            sw.Write("mtllib ./" + filename.Replace("|", "_") + ".mtl\n");

            for (int i = 0; i < mf.Length; i++) {
                sw.Write(MeshToString(mf[i], materialList, folder, filename));
            }
        }

        MaterialsToFile(materialList, folder, filename.Replace("|", "_"));
    }

    private static bool CreateTargetFolder(String subFolder) {
        try {
            System.IO.Directory.CreateDirectory(targetFolder+"/"+subFolder);
        } catch {
            // EditorUtility.DisplayDialog("Error!", "Failed to create target folder!", "");
            return false;
        }

        return true;
    }

    // [MenuItem("Custom/Export/ExportObject_Z each object to single OBJ")]
    public static void ExportEachObectToSingle(int index = 0) {  //String subFolder
        String subFolder = "House_" + index.ToString();
        if (!CreateTargetFolder(subFolder))
            return;
        ExportEachObjectToSingle("Walls",subFolder);
        ExportEachObjectToSingle("Objects",subFolder);
        ExportEachObjectToSingle("Ceiling",subFolder);
        ExportEachObjectToSingle("Floor",subFolder);
    }
    
    
    static void ExportEachObjectToSingle(String parentType, String subFolder) {
        if (!CreateTargetFolder(subFolder))
            return;
        MonoBehaviour[] objects = null;
        if (parentType.Equals("Ceiling")){
            objects = GameObject.Find(parentType).GetComponentsInChildren<StructureObject>();
        }
        else{
            objects = GameObject.Find(parentType).GetComponentsInChildren<SimObjPhysics>();
        }

        int exportedObjects = 0;

        bool tongverse = false;
        for (int i = 0; i < objects.Length; i++) {
           
            Debug.Log(i.ToString());
            // if (i!=89){
            //     continue;
            // }
            // if (i==177){
            //     i = i;
            // }
            var gameobject = objects[i];
            if (gameobject.transform.name.Contains("CD|surface")){
                continue;
            }
            if (gameobject.transform.name.Contains("door|1|3")){
                i = i ;
                // if (!gameobject.IsOpen && !gameobject.IsOpennable){
                gameobject.transform.name = "Outside" + gameobject.transform.name;
                // }
            }
            if (tongverse && gameobject.transform.name.Contains("GarbageCan|9|3")){
                continue;
            }
            if (!gameobject.transform.parent.name.Equals(parentType)){
                continue;
            }
            //exclude external wall
            if (gameobject.transform.name.Contains("exterior")){
                continue;
            }
            Component[] meshfilter = gameobject.transform.GetComponentsInChildren(typeof(MeshFilter));

            MeshFilter[] mf = new MeshFilter[meshfilter.Length];

            for (int m = 0; m < meshfilter.Length; m++) {
                exportedObjects++;
                mf[m] = (MeshFilter)meshfilter[m];
            }
            if (gameobject.transform.name.Contains("Television")){
                i = i;
            }
            string path = targetFolder+"/"+subFolder;
            // if (i!=53){
            //     continue;
            // }
            string filename = gameobject.transform.name.ToLower();

            MeshesToFile(mf, targetFolder+"/"+subFolder, filename);
        }

        // if (exportedObjects > 0) {
        //     EditorUtility.DisplayDialog("Objects exported", "Exported " + exportedObjects + " .obj files of "+ parentType, "");
        // } else
        //     EditorUtility.DisplayDialog("Objects not exported", "Make sure at least some of your selected objects have mesh filters!", "");
    }
    
}