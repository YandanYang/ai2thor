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
    public string NormalMapName;
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
        Debug.Log("dddddddddddddddddd 11111");
        Mesh m = mf.sharedMesh;
        
        Debug.Log("dddddddddddddddddd 22222");
        Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;
        Debug.Log("dddddddddddddddddd 33333");
        var debug = mf.GetComponent<Renderer>();
        Debug.Log("dddddddddddddddddd 44444");

        StringBuilder sb = new StringBuilder();
        if (m==null){
            return sb.ToString();
        }
        sb.Append("g ").Append(mf.name).Append("\n");
        Debug.Log("dddddddddddddddddd 55555");
        if (m.name.Contains("Statue"))
        {
            sb = sb;
        }
        Debug.Log("dddddddddddddddddd " + m.name);
        foreach (Vector3 lv in m.vertices) {
            Vector3 wv = mf.transform.TransformPoint(lv);

            //This is sort of ugly - inverting x-component since we're in
            // a different coordinate system than "everyone" is "used to".
            sb.Append(string.Format("v {0} {1} {2}\n", -wv.x, wv.y, wv.z));
            // sb.Append(string.Format("v {0} {2} {1}\n", -wv.x, wv.y, wv.z));
        }
        sb.Append("\n");
        Debug.Log("dddddddddddddddddd 1");

        foreach (Vector3 lv in m.normals) {
            Vector3 wv = mf.transform.TransformDirection(lv);

            // sb.Append(string.Format("vn {0} {2} {1}\n", -wv.x, wv.y, wv.z));
            sb.Append(string.Format("vn {0} {1} {2}\n", -wv.x, wv.y, wv.z));
        }
        sb.Append("\n");
        Debug.Log("dddddddddddddddddd 2");
        foreach (Vector3 v in m.uv) {
            sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
        }
        Debug.Log("dddddddddddddddddd 3");

        for (int material = 0; material < m.subMeshCount; material++) {
            Debug.Log("dddddddddddddddddd 4 " + material);
            if (mats[material]==null){
                continue;
            }
            if (mats[material].name.Contains("Placeable_Surface_Mat")) {
                    continue;
                }
            sb.Append("\n");
            
            sb.Append("usemtl ").Append(mats[material].name).Append("\n");
            sb.Append("usemap ").Append(mats[material].name).Append("\n");

            //See if this material is already in the materiallist.
            try {
                ObjMaterial objMaterial = new ObjMaterial();

                objMaterial.name = mats[material].name;
                var flag = mats[material].HasProperty("_MainTex");
                if  (flag){

                    if (mats[material].mainTexture){
                        string[] filepath = Directory.GetFiles(targetFolder+"/material/",mats[material].mainTexture.name +".*");
                        Debug.Log(targetFolder+"\\material\\"+mats[material].mainTexture.name );
                        objMaterial.textureName = filepath[0].Replace("\\","/");
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
                        string destinationFile = objMaterial.textureName;
                        int stripIndex = destinationFile.LastIndexOf('/');//FIXME: Should be Path.PathSeparator;

                        if (stripIndex >= 0)
                            destinationFile = destinationFile.Substring(stripIndex + 1).Trim();


                        destinationFile = destinationFile.Replace(".tif",".png");
                        destinationFile = destinationFile.Replace(".jpg",".png");
                        destinationFile = destinationFile.Replace(".JPG",".png");
                        destinationFile = destinationFile.Replace(".tga",".png");
                        string relativeFile = destinationFile;

                        destinationFile = folder + "/" + filename.Replace("|", "_") + "_" + destinationFile;
                        File.WriteAllBytes(destinationFile,result.EncodeToPNG());
                        // string array1 = FindFile(Path.GetTempPath(), mats[material].mainTexture.name +"*");
                    //     string[] array1 = Directory.GetFiles(Path.GetTempPath(), mats[material].mainTexture.name +"*",SearchOption.AllDirectories);
                        // objMaterial.textureName = AssetDatabase.GetAssetPath(mats[material].mainTexture);
                    }
                    else
                    objMaterial.textureName = null;
                }
                else{
                    material = material;
                }
                flag = mats[material].HasProperty("_Color");
                if  (flag){
                    objMaterial.color_red = mats[material].color.r;
                    objMaterial.color_green = mats[material].color.g;
                    objMaterial.color_blue = mats[material].color.b;
                    objMaterial.transparent = mats[material].color.a;
                }
                else {
                    material = material;
                }
                //objMaterial.NormalMapName = null;

                materialList.Add(objMaterial.name, objMaterial);
                material = material;
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
                sw.Write("newmtl {0}\n", kvp.Key);
                sw.Write("Ka  {0} {1} {2}\n",r,g,b);
                sw.Write("Kd  {0} {1} {2}\n",r,g,b);
                sw.Write("Ks  {0} {1} {2}\n",r,g,b);
                sw.Write("d  {0}\n", d);
                sw.Write("Ns  0.0\n");
                sw.Write("illum 1\n");

                if (kvp.Value.textureName != null) {
                    string destinationFile = kvp.Value.textureName;


                    int stripIndex = destinationFile.LastIndexOf('/');//FIXME: Should be Path.PathSeparator;

                    if (stripIndex >= 0)
                        destinationFile = destinationFile.Substring(stripIndex + 1).Trim();


                    destinationFile = destinationFile.Replace(".tif",".png");
                    destinationFile = destinationFile.Replace(".jpg",".png");
                    destinationFile = destinationFile.Replace(".JPG",".png");
                    destinationFile = destinationFile.Replace(".tga",".png");
                    destinationFile = filename.Replace("|", "_") + "_" + destinationFile;
                    string relativeFile = destinationFile;

                    destinationFile = folder + "/" +  destinationFile;

                    Debug.Log("Copying texture from " + kvp.Value.textureName + " to " + destinationFile);

                    // try {
                    //     // byte[] imageBytes = File.ReadAllBytes(kvp.Value.textureName);
                    //     // Texture2D tex = new Texture2D(2,2);
                    //     // tex.LoadImage(imageBytes);
                    //     // File.WriteAllBytes(destinationFile,imageBytes);
                    //     //Copy the source file
                    //     // if (kvp.Value.textureName.EndsWith(".tif")){
                    //     //     destinationFile = destinationFile.Replace(".tif",".png");
                    //         // string newfilename = kvp.Value.textureName.Replace(".tif",".png");
                    //         // System.Drawing.Bitmap.FromFile(kvp.Value.textureName).Save(newfilename, System.Drawing.Imaging.ImageFormat.Png);
                    //     // }
                    //     File.Copy(kvp.Value.textureName, destinationFile);
                    // } catch {

                    // }


                    sw.Write("map_Kd {0}", relativeFile);
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


        for (int i = 0; i < objects.Length; i++) {
            Debug.Log(i.ToString());
            // if (i==53){
            //     i = i;
            // }
            // if (i==177){
            //     i = i;
            // }
            var gameobject = objects[i];
            if (gameobject.transform.name.Contains("CD|surface")){
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

            MeshesToFile(mf, targetFolder+"/"+subFolder, gameobject.transform.name);
        }

        // if (exportedObjects > 0) {
        //     EditorUtility.DisplayDialog("Objects exported", "Exported " + exportedObjects + " .obj files of "+ parentType, "");
        // } else
        //     EditorUtility.DisplayDialog("Objects not exported", "Make sure at least some of your selected objects have mesh filters!", "");
    }
    
}