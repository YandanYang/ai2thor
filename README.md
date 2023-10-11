### This repository aims to export 3D model in [ProcTHOR-10k](https://github.com/allenai/procthor-10k) into .obj files.
## What is Procthor?

[**ProcTHOR**](https://procthor.allenai.org/#explore) uses procedural generation to **sample massively diverse, realistic, interactive, customizable, and performant 3D environments** to train simulated embodied agents. 

### Dataset diversity
- 18 different Semantic Asset groups and 1633 assets across 108(95) categories.    
- 16 different scene specifications to seed the scene generation process.
- 10k scene provided in Procthor-10k Dataset.

### Usable data for 3D grounding  
We could catch 3D model and multi-view data in the following format:
- RGB
-  depth
-  Instance segmentation 
-  Instance mask
-  Instance Detection 

## Open & export the whole scene in Unity Editor
### Load scene
Download Unity-Editor.<br />
Open folder ai2thor/unity as a Unity project.<br />
Take the House json and place it in the directory `Assets/Resources/rooms, such as house3.json`.<br />
Then, go to `Assets/Scenes/Procedural` and open up the `Procedural.unity scene`.<br />
While in this scene, hit Play and then navigate your cursor to the text box that shows up to the bottom left of the Game window that reads `Enter text...`<br />
This box allows actions to be executed directly in the editor without needing to link to the python controller, and all actions are defined in the script `DebugInputField.cs` located in `Assets/Scripts`.<br />
To start, type `init` into this box to initialize a default agent and press enter to execute. Then, type `chp json file's name>` and hit enter again to generate a house based on the json file you saved to the `Assets/Resources/rooms` directory.

### Export scene into .obj files
Our modified version **remove robot** in the scene and add a button to **export .obj** files easily.
First you need to copy the texture files. Run 
```
cp -r ai2thor/ExportedObj/material ai2thor/unity/ExportedObj/material
```
then click the button as shown in the following image, Files will be exported in `ai2thor/unity/ExportedObj/debug/`

<img src="https://github.com/YandanYang/ai2thor/blob/main/images/SaveObj.jpeg" width="500px">

## Procthor-10k 3D Dataset 


### 3D Models & originial Json file in Lambda

.obj Models: `/scratch/masaccio/Procthor/ExportedObj-good/House_xxx`

.json files: `/scratch/masaccio/Procthor/House_Json/House_xxx.json`
    
### File organization
Each **House_xxx** folder contains the following files of k objects:
```
        - objectname1.obj      (mesh file)
        - objectname1.mtl      (material file)
        - objectname1.png/jpg/…     (texture image)
        - ……
        - objectnamek.obj
        - objectnamek.mtl
        - objectnamek.png/jpg/…
```
### Visualize a single house
Try Meshlab or Blender. Import .obj files and you will see the entire house.



## For developer
###  Installation
This repository is forked from [AI2-THOR](https://github.com/allenai/ai2thor). Please refer to it for installation.
```
conda create --name ai2thor -y python=3.8
conda activate ai2thor
python setup.py install
pip install prior
pip install scipy
pip install matplotlib
```
### Compile executable file for Unity Project 
Open The project in Unity Editor.
Compile the project into an executable file and save it into `unity/Build/local-build-procthor.x86_64`.

### Generate data for each house
####  3D Model
To build and export 3D model for each house in batches, you should first prepare the required environment and run : 
```
python prothor_data_gen.py --datatype 3D
```
You can also change the json file in the code to export different house.
<br />

#### TopDown images 
To generate topdown viewport image for each house in batches, you should first prepare the required environment and run : 
```
python prothor_data_gen.py --datatype topdown
```
<img src="https://github.com/YandanYang/ai2thor/blob/main/images/topdown.png" width="500px">

#### MultiView images
To generate multi-view RGB-D image with camera pose for each house, you should first prepare the required environment and run : 
```
python prothor_data_gen.py --datatype multiview 
```
We can see recheable positions of robot and path to collect data

<img src="https://github.com/YandanYang/ai2thor/blob/main/images/reachable_position.png" width="300px">

Then you can use [nerfstudio](https://docs.nerf.studio/en/latest/index.html) to check the 3D reconstruction result from the multiview data. Run
```
ns-train nerfacto --data ai2thor/Output/Multiview/{house_number}/ --pipeline.model.predict-normals True --viewer.websocket-port 7008
```
and you will see the result in the websocket viewer:

<img src="https://github.com/YandanYang/ai2thor/blob/main/images/multiview-nerf.png" width="500px">


## Official Tutorial
[This website](https://ai2thor.allenai.org/) shows more details to use procthor with demo and documentation.
You can go to [Procthor Colab](https://github.com/allenai/) to get started with [ProcTHOR](https://procthor.allenai.org/#explore).
